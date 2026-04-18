using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class TileReplayViewer : MonoBehaviour
{
    private const string ViewerObjectName = "[TileReplayViewer]";

    private enum ReplayViewMode
    {
        Global,
        Tile
    }

    private class TurnSlide
    {
        // The UI steps turn-by-turn, so several same-turn events share one slide
        public int turnNumber;
        public List<ReplayEvent> events = new List<ReplayEvent>();
    }

    [SerializeField] private UnityEngine.Canvas targetCanvas;

    private GameObject panelRoot;
    private UnityEngine.UI.Text titleText;
    private UnityEngine.UI.Text bodyText;
    private UnityEngine.UI.Text footerText;
    private UnityEngine.UI.ScrollRect bodyScrollRect;
    private UnityEngine.UI.Button previousButton;
    private UnityEngine.UI.Button nextButton;
    private UnityEngine.UI.Button globalViewButton;
    private UnityEngine.UI.Button tileViewButton;
    private UnityEngine.UI.Button closeButton;
    private CustomButton openButton;

    private ReplayManager.TileReplayLog currentLog;
    private readonly List<TurnSlide> currentSlides = new List<TurnSlide>();
    private int currentIndex;
    private bool subscribedToGridClicks;
    private Tile selectedTile;
    private Tile displayedTile;
    private ReplayViewMode currentViewMode = ReplayViewMode.Global;
    private string currentTitle = "Match Replay";

    public static TileReplayViewer EnsureExists()
    {
        TileReplayViewer existing = FindFirstObjectByType<TileReplayViewer>();
        if (existing != null)
        {
            return existing;
        }

        GameObject viewerObject = new GameObject(ViewerObjectName);
        DontDestroyOnLoad(viewerObject);
        return viewerObject.AddComponent<TileReplayViewer>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SetPanelVisible(false);
    }

    private void Start()
    {
        ReplayManager.EnsureExists();
        TrySubscribeToGridClicks();
    }

    private void OnDestroy()
    {
        if (subscribedToGridClicks && GridEventHandler.Instance != null)
        {
            GridEventHandler.Instance.onTileClicked -= OnTileClicked;
        }
    }

    private void Update()
    {
        TrySubscribeToGridClicks();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleReplayPanel();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            OpenGlobalReplay();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            OpenReplayForSelectedTile();
        }

        TryCreateOpenButton();

        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            StepForward();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            StepBackward();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPanelVisible(false);
        }
    }

    private void OnTileClicked(Tile tile)
    {
        // If the player changes selection, hide the stale replay instead of leaving it open.
        if (currentViewMode == ReplayViewMode.Tile && displayedTile != null && tile != displayedTile)
        {
            SetPanelVisible(false);
        }

        selectedTile = tile;
    }

    private void ToggleReplayPanel()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            SetPanelVisible(false);
            return;
        }

        if (currentViewMode == ReplayViewMode.Tile)
        {
            OpenReplayForSelectedTile();
            return;
        }

        OpenGlobalReplay();
    }

    private void OpenGlobalReplay()
    {
        ReplayManager replayManager = ReplayManager.EnsureExists();
        List<ReplayEvent> globalEvents = replayManager.GetEventsVisibleToCurrentPlayer(replayManager.GetGlobalEvents());
        currentLog = null;
        displayedTile = null;
        currentViewMode = ReplayViewMode.Global;
        currentTitle = "Match Replay";

        OpenReplayFromEvents(globalEvents);
    }

    private void OpenReplayForSelectedTile()
    {
        Tile tile = selectedTile;
        if (tile == null && GridEventHandler.Instance != null)
        {
            tile = GridEventHandler.Instance.SelectedTile;
        }

        OpenReplayForTile(tile);
    }

    private void OpenReplayForTile(Tile tile)
    {
        if (tile == null)
        {
            return;
        }

        ReplayManager replayManager = ReplayManager.EnsureExists();
        currentLog = replayManager.GetLogForTile(tile);
        if (currentLog == null || currentLog.events == null || currentLog.events.Count == 0)
        {
            currentSlides.Clear();
            displayedTile = null;
            SetPanelVisible(false);
            return;
        }

        currentViewMode = ReplayViewMode.Tile;
        currentTitle = $"Tile Replay {currentLog.tile}";
        displayedTile = tile;
        OpenReplayFromEvents(replayManager.GetEventsVisibleToCurrentPlayer(currentLog.events));
    }

    private void OpenReplayFromEvents(IReadOnlyList<ReplayEvent> sourceEvents)
    {
        RebuildSlides(sourceEvents);
        if (currentSlides.Count == 0)
        {
            SetPanelVisible(false);
            return;
        }

        currentIndex = currentSlides.Count - 1;
        SetPanelVisible(true);
        RefreshView();
    }

    private void StepForward()
    {
        if (currentSlides.Count == 0)
        {
            return;
        }

        currentIndex = Mathf.Min(currentIndex + 1, currentSlides.Count - 1);
        RefreshView();
    }

    private void StepBackward()
    {
        if (currentSlides.Count == 0)
        {
            return;
        }

        currentIndex = Mathf.Max(currentIndex - 1, 0);
        RefreshView();
    }

    private void RefreshView()
    {
        if (currentSlides.Count == 0)
        {
            SetPanelVisible(false);
            return;
        }

        TurnSlide currentSlide = currentSlides[currentIndex];

        titleText.text = currentTitle;
        bodyText.text = BuildSlideText(currentSlide);
        footerText.text = $"Turn {currentSlide.turnNumber}    Slide {currentIndex + 1}/{currentSlides.Count}    R: Toggle    M: Match    T: Tile    A/D: Prev/Next    Esc: Close";

        previousButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < currentSlides.Count - 1;
        globalViewButton.interactable = currentViewMode != ReplayViewMode.Global;
        tileViewButton.interactable = selectedTile != null && currentViewMode != ReplayViewMode.Tile;

        UnityEngine.Canvas.ForceUpdateCanvases();
        if (bodyScrollRect != null)
        {
            bodyScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void TrySubscribeToGridClicks()
    {
        if (subscribedToGridClicks || GridEventHandler.Instance == null)
        {
            return;
        }

        GridEventHandler.Instance.onTileClicked += OnTileClicked;
        subscribedToGridClicks = true;
    }

    private void RebuildSlides(IReadOnlyList<ReplayEvent> sourceEvents)
    {
        currentSlides.Clear();

        if (sourceEvents == null)
        {
            return;
        }

        TurnSlide activeSlide = null;
        foreach (ReplayEvent replayEvent in sourceEvents)
        {
            if (replayEvent == null)
            {
                continue;
            }

            // Slides are grouped by turn so the text log lines up with the planned slideshow model.
            if (activeSlide == null || activeSlide.turnNumber != replayEvent.turnNumber)
            {
                activeSlide = new TurnSlide
                {
                    turnNumber = replayEvent.turnNumber
                };
                currentSlides.Add(activeSlide);
            }

            activeSlide.events.Add(replayEvent);
        }
    }

    private string BuildSlideText(TurnSlide slide)
    {
        if (slide == null || slide.events == null || slide.events.Count == 0)
        {
            return string.Empty;
        }

        List<ReplayEvent> orderedEvents = new List<ReplayEvent>(slide.events);
        orderedEvents.Sort(CompareReplayEventsForDisplay);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < orderedEvents.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append("- ");
            builder.Append(ReplayManager.EnsureExists().GetEventBodyText(orderedEvents[i]));
        }

        return builder.ToString();
    }

    private int CompareReplayEventsForDisplay(ReplayEvent left, ReplayEvent right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        // Death reads better after the hit that reduced HP to zero
        if (left.type == ReplayEventType.UnitDiedOnTile && right.type != ReplayEventType.UnitDiedOnTile)
        {
            return 1;
        }

        if (left.type != ReplayEventType.UnitDiedOnTile && right.type == ReplayEventType.UnitDiedOnTile)
        {
            return -1;
        }

        return left.sequenceNumber.CompareTo(right.sequenceNumber);
    }
    private void EnsureUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        // The replay UI builds itself at runtime so it can work in the demo scene without extra setup
        UnityEngine.Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("[ReplayTileLogCanvas]");
            DontDestroyOnLoad(canvasObject);

            canvas = canvasObject.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("[ReplayEventSystem]");
            DontDestroyOnLoad(eventSystemObject);
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        GameObject panelObject = CreateUiObject("ReplayPanel", canvas.transform);
        panelRoot = panelObject;

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.sizeDelta = new Vector2(500f, 240f);
        panelRect.anchoredPosition = new Vector2(-20f, 190f);

        UnityEngine.UI.Image panelImage = panelObject.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.92f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        titleText = CreateText("Title", panelObject.transform, font, 20, TextAnchor.UpperLeft);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(16f, -40f);
        titleRect.offsetMax = new Vector2(-16f, -10f);

        GameObject scrollRootObject = CreateUiObject("BodyScroll", panelObject.transform);
        RectTransform scrollRootRect = scrollRootObject.AddComponent<RectTransform>();
        scrollRootRect.anchorMin = new Vector2(0f, 0f);
        scrollRootRect.anchorMax = new Vector2(1f, 1f);
        scrollRootRect.offsetMin = new Vector2(16f, 58f);
        scrollRootRect.offsetMax = new Vector2(-16f, -48f);
        bodyScrollRect = scrollRootObject.AddComponent<UnityEngine.UI.ScrollRect>();
        bodyScrollRect.horizontal = false;
        bodyScrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

        GameObject viewportObject = CreateUiObject("Viewport", scrollRootObject.transform);
        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image viewportImage = viewportObject.AddComponent<UnityEngine.UI.Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        UnityEngine.UI.Mask viewportMask = viewportObject.AddComponent<UnityEngine.UI.Mask>();
        viewportMask.showMaskGraphic = false;

        bodyText = CreateText("Body", viewportObject.transform, font, 16, TextAnchor.UpperLeft);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(0f, 0f);
        bodyRect.offsetMax = new Vector2(0f, 0f);
        UnityEngine.UI.ContentSizeFitter contentSizeFitter = bodyText.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        contentSizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        bodyScrollRect.viewport = viewportRect;
        bodyScrollRect.content = bodyRect;

        footerText = CreateText("Footer", panelObject.transform, font, 12, TextAnchor.LowerLeft);
        RectTransform footerRect = footerText.rectTransform;
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = new Vector2(16f, 32f);
        footerRect.offsetMax = new Vector2(-16f, 52f);

        previousButton = CreateButton("PrevButton", panelObject.transform, font, "Prev", new Vector2(16f, 8f));
        previousButton.onClick.AddListener(StepBackward);

        nextButton = CreateButton("NextButton", panelObject.transform, font, "Next", new Vector2(116f, 8f));
        nextButton.onClick.AddListener(StepForward);

        globalViewButton = CreateButton("GlobalButton", panelObject.transform, font, "Match", new Vector2(216f, 8f));
        globalViewButton.onClick.AddListener(OpenGlobalReplay);

        tileViewButton = CreateButton("TileButton", panelObject.transform, font, "Tile", new Vector2(316f, 8f));
        tileViewButton.onClick.AddListener(OpenReplayForSelectedTile);

        closeButton = CreateButton("CloseButton", panelObject.transform, font, "Close", new Vector2(396f, 8f));
        closeButton.onClick.AddListener(() => SetPanelVisible(false));

    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static UnityEngine.UI.Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        UnityEngine.UI.Text text = textObject.AddComponent<UnityEngine.UI.Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static UnityEngine.UI.Button CreateButton(string name, Transform parent, Font font, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 0f);
        buttonRect.pivot = new Vector2(0f, 0f);
        buttonRect.sizeDelta = new Vector2(88f, 24f);
        buttonRect.anchoredPosition = anchoredPosition;

        UnityEngine.UI.Image image = buttonObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        UnityEngine.UI.Button button = buttonObject.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        colors.pressedColor = new Color(0.20f, 0.20f, 0.20f, 1f);
        colors.disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.7f);
        button.colors = colors;

        UnityEngine.UI.Text buttonText = CreateText("Label", buttonObject.transform, font, 14, TextAnchor.MiddleCenter);
        RectTransform labelRect = buttonText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        buttonText.text = label;

        return button;
    }
    private void TryCreateOpenButton()
    {
        if (openButton != null)
        {
            return;
        }

        if (HUDCanvas.Instance == null)
        {
            return;
        }

        CustomButton passTurnTemplate = FindPassTurnButton();
        if (passTurnTemplate == null)
        {
            return;
        }

        GameObject replayButtonObject = Instantiate(passTurnTemplate.gameObject, passTurnTemplate.transform.parent);
        replayButtonObject.name = "Replay Button";

        openButton = replayButtonObject.GetComponent<CustomButton>();
        if (openButton == null)
        {
            return;
        }

        openButton.ClearActions();
        openButton.onClick += (_) => OpenGlobalReplay();
        openButton.SetText("Replay");
        openButton.SetState(Button.BUTTON_STATE.ACTIVE);
        CopyButtonVisuals(passTurnTemplate.gameObject, replayButtonObject);

        RectTransform templateRect = passTurnTemplate.GetComponent<RectTransform>();
        RectTransform replayRect = replayButtonObject.GetComponent<RectTransform>();
        if (templateRect != null && replayRect != null)
        {
            // Clone the existing HUD button layout so Replay feels like part of the same bar.
            replayRect.anchorMin = templateRect.anchorMin;
            replayRect.anchorMax = templateRect.anchorMax;
            replayRect.pivot = templateRect.pivot;
            replayRect.sizeDelta = templateRect.sizeDelta;
            replayRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(templateRect.rect.width + 20f, 0f);
        }
    }

    private static void CopyButtonVisuals(GameObject templateButton, GameObject targetButton)
    {
        if (templateButton == null || targetButton == null)
        {
            return;
        }

        UnityEngine.UI.Graphic[] templateGraphics = templateButton.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        UnityEngine.UI.Graphic[] targetGraphics = targetButton.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        int count = Mathf.Min(templateGraphics.Length, targetGraphics.Length);

        for (int i = 0; i < count; i++)
        {
            if (templateGraphics[i] == null || targetGraphics[i] == null)
            {
                continue;
            }

            targetGraphics[i].color = templateGraphics[i].color;
            targetGraphics[i].material = templateGraphics[i].material;
        }

        TextMeshProUGUI[] templateTexts = templateButton.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI[] targetTexts = targetButton.GetComponentsInChildren<TextMeshProUGUI>(true);
        count = Mathf.Min(templateTexts.Length, targetTexts.Length);

        for (int i = 0; i < count; i++)
        {
            if (templateTexts[i] == null || targetTexts[i] == null)
            {
                continue;
            }

            targetTexts[i].font = templateTexts[i].font;
            targetTexts[i].fontSharedMaterial = templateTexts[i].fontSharedMaterial;
            targetTexts[i].fontSize = templateTexts[i].fontSize;
            targetTexts[i].fontStyle = templateTexts[i].fontStyle;
            targetTexts[i].alignment = templateTexts[i].alignment;
            targetTexts[i].color = templateTexts[i].color;
            targetTexts[i].enableAutoSizing = templateTexts[i].enableAutoSizing;
            targetTexts[i].characterSpacing = templateTexts[i].characterSpacing;
            targetTexts[i].wordSpacing = templateTexts[i].wordSpacing;
            targetTexts[i].lineSpacing = templateTexts[i].lineSpacing;
            targetTexts[i].margin = templateTexts[i].margin;
            targetTexts[i].raycastTarget = templateTexts[i].raycastTarget;
        }
    }

    private static CustomButton FindPassTurnButton()
    {
        if (HUDCanvas.Instance == null)
        {
            return null;
        }

        CustomButton[] buttons = HUDCanvas.Instance.GetComponentsInChildren<CustomButton>(true);
        foreach (CustomButton button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (button.gameObject.name.Contains("Pass Turn"))
            {
                return button;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && label.text == "Pass Turn")
            {
                return button;
            }
        }

        return null;
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(isVisible);
        }

        if (!isVisible)
        {
            displayedTile = null;
        }
    }
}
