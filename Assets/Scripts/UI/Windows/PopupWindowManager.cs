using UnityEngine;

public class PopupWindowManager : MonoBehaviour
{
    [SerializeField] bool withTransition = false;
    [SerializeField] bool disableCameraWhenOpened = true;
    [SerializeField] Button openButton;
    [SerializeField] GameObject windowPrefab;
    [SerializeField] IPopupWindow windowScript;
    [SerializeField] Canvas canvasObject;
    [Header("Chain Another Popup Window Upon Closing")]
    [SerializeField] PopupWindowManager windowInitializerFollowUp;

    private bool disabled = false;

    void Start()
    {
        openButton.onClick += HandleButtonClick;
    }

    public void Disable() => disabled = true;
    public void Enable() => disabled = false;

    public void SubscribeOnClickAction()
    {
        openButton.onClick += HandleButtonClick;
    }

    public void SetOpenButton(Button button) => openButton = button;

    private void HandleButtonClick(Button button)
    {
        if (disabled) return;
        if (disableCameraWhenOpened && CameraController.Instance)
            CameraController.Instance.DisableCamera();
        if (withTransition)
            InitializeWithTransition();
        else
            Initialize();
    }

    public void InitializeWithTransition()
    {
        Curtain.Instance.ShortTransitionWithCallback(Initialize);
    }

    public void Initialize()
    {
        GameObject window_object = Instantiate(windowPrefab, canvasObject.transform);
        IPopupWindow window_script = window_object.GetComponent<IPopupWindow>();
        window_object.transform.SetAsLastSibling();
        window_script.Initialize();
        if (windowInitializerFollowUp)
        {
            windowInitializerFollowUp.SetOpenButton(window_script.GetChainButton());
            windowInitializerFollowUp.SubscribeOnClickAction();
        }
    }
}

