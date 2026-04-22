using UnityEngine;
using System.Collections.Generic;

public class LoadGameMenu : MonoBehaviour, IPopupWindow
{
    [SerializeField] private List<CustomButton> slotButtons = new List<CustomButton>();
    [SerializeField] private Button loadButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button deleteButton;

    private SaveSlot[] slots;
    private SaveSlot currentlySelectedSlot;

    public Button GetChainButton() => null;

    public void Initialize()
    {
        DataManager manager = DataManager.Instance;
        if (manager == null)
        {
            Debug.LogError("LoadGameMenu: DataManager.Instance not found");
            return;
        }

        slots = manager.GetAllSlots();
        int buttonCount = Mathf.Min(slots.Length, slotButtons.Count);

        for (int i = 0; i < buttonCount; i++)
        {
            CustomButton button = slotButtons[i];
            if (button == null)
                continue;

            button.Initialize(slots[i]);
            button.onClick -= OnSlotButtonClicked;
            button.onClick += OnSlotButtonClicked;
        }

        if (loadButton != null)
        {
            loadButton.onClick -= OnLoadButtonClicked;
            loadButton.onClick += OnLoadButtonClicked;
        }

        if (newGameButton != null)
        {
            newGameButton.onClick -= OnNewGameButtonClicked;
            newGameButton.onClick += OnNewGameButtonClicked;
        }

        if (deleteButton != null)
        {
            deleteButton.onClick -= OnDeleteButtonClicked;
            deleteButton.onClick += OnDeleteButtonClicked;
        }

        RefreshDisplay();
    }

    private void OnSlotButtonClicked(Button clickedButton)
    {
        CustomButton customButton = clickedButton as CustomButton;
        if (customButton == null)
        {
            Debug.LogError("LoadGameMenu: Clicked button is not a CustomButton");
            return;
        }

        SaveSlot clickedSlot = customButton.displayedObject as SaveSlot;
        if (clickedSlot == null)
        {
            Debug.LogError("LoadGameMenu: CustomButton displayedObject is not a SaveSlot");
            return;
        }

        currentlySelectedSlot = clickedSlot;
        RefreshDisplay();
        Debug.Log($"LoadGameMenu: Selected slot {clickedSlot.id}");
    }

    private void OnLoadButtonClicked(Button _)
    {
        LoadSelectedSlot();
    }

    private void OnNewGameButtonClicked(Button _)
    {
        CreateNewGameInSelectedSlot();
    }

    private void OnDeleteButtonClicked(Button _)
    {
        DeleteSelectedSlot();
    }

    public void LoadSelectedSlot()
    {
        if (currentlySelectedSlot == null)
        {
            Debug.LogWarning("LoadGameMenu: No slot selected");
            return;
        }

        DataManager manager = DataManager.Instance;
        if (manager == null)
        {
            Debug.LogError("LoadGameMenu: DataManager.Instance not found");
            return;
        }

        manager.Load(currentlySelectedSlot);
        Debug.Log($"LoadGameMenu: Loaded game from slot {currentlySelectedSlot.id}");
    }

    public void CreateNewGameInSelectedSlot()
    {
        if (currentlySelectedSlot == null)
        {
            Debug.LogWarning("LoadGameMenu: No slot selected");
            return;
        }

        DataManager manager = DataManager.Instance;
        if (manager == null)
        {
            Debug.LogError("LoadGameMenu: DataManager.Instance not found");
            return;
        }

        manager.NewGame(currentlySelectedSlot);
        RefreshDisplay();
        Debug.Log($"LoadGameMenu: Created new game in slot {currentlySelectedSlot.id}");
    }

    public void DeleteSelectedSlot()
    {
        if (currentlySelectedSlot == null)
        {
            Debug.LogWarning("LoadGameMenu: No slot selected");
            return;
        }

        DataManager manager = DataManager.Instance;
        if (manager == null)
        {
            Debug.LogError("LoadGameMenu: DataManager.Instance not found");
            return;
        }

        manager.DeleteGame(currentlySelectedSlot);
        currentlySelectedSlot = null;
        RefreshDisplay();
        Debug.Log("LoadGameMenu: Deleted save slot");
    }

    private void RefreshDisplay()
    {
        if (slots == null)
            return;

        int buttonCount = Mathf.Min(slotButtons.Count, slots.Length);
        for (int i = 0; i < buttonCount; i++)
        {
            RefreshSlotDisplay(i);
        }

        bool hasSelectedSlot = currentlySelectedSlot != null;
        bool hasSelectedSave = hasSelectedSlot && currentlySelectedSlot.ExistsOnDisk();

        if (newGameButton != null)
        {
            newGameButton.SetState(hasSelectedSlot ? Button.BUTTON_STATE.ACTIVE : Button.BUTTON_STATE.INACTIVE);
        }

        if (loadButton != null)
        {
            loadButton.SetState(hasSelectedSave ? Button.BUTTON_STATE.ACTIVE : Button.BUTTON_STATE.INACTIVE);
        }

        if (deleteButton != null)
        {
            deleteButton.SetState(hasSelectedSave ? Button.BUTTON_STATE.ACTIVE : Button.BUTTON_STATE.INACTIVE);
        }
    }

    private void RefreshSlotDisplay(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotButtons.Count || slotIndex >= slots.Length)
            return;

        CustomButton button = slotButtons[slotIndex];
        SaveSlot slot = slots[slotIndex];

        if (button == null)
            return;

        button.Initialize(slot);
        button.ChangeIconColor(currentlySelectedSlot == slot ? Color.yellow : Color.white);
    }

    public SaveSlot GetCurrentlySelectedSlot()
    {
        return currentlySelectedSlot;
    }
}
