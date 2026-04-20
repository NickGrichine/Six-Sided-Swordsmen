using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    private const string SetupSceneName = "Setup Scene";
    private const string GameSceneName = "Game Scene";

    private SaveSlot[] slots = new SaveSlot[3];
    private SaveSlot selectedSaveSlot;
    private GameManager observedGameManager;

    protected override void Awake()
    {
        base.Awake();
        
        // Initialize all 3 save slots
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new SaveSlot(i);
        }
    }

    public SaveSlot GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogError($"DataManager: Invalid slot index {slotIndex}");
            return null;
        }
        return slots[slotIndex];
    }

    public SaveSlot[] GetAllSlots()
    {
        return slots;
    }

    public void ObserveGameManager(GameManager gameManager)
    {
        if (observedGameManager != null)
        {
            observedGameManager.OnTurnEnd -= OnTurnEnd;
        }

        observedGameManager = gameManager;

        if (observedGameManager != null)
        {
            observedGameManager.OnTurnEnd += OnTurnEnd;
        }
    }

    public void StopObservingGameManager(GameManager gameManager)
    {
        if (observedGameManager == gameManager && observedGameManager != null)
        {
            observedGameManager.OnTurnEnd -= OnTurnEnd;
            observedGameManager = null;
        }
    }

    public void Load(SaveSlot slot)
    {
        if (slot == null)
        {
            Debug.LogError("DataManager: Cannot load from null slot");
            return;
        }

        selectedSaveSlot = slot;

        // Read SaveData from disk
        if (!slot.ReadFromDisk())
        {
            Debug.LogError($"DataManager: Failed to load from slot {slot.id}");
            return;
        }

        GridData gridData = slot.Data != null ? slot.Data.GetGridData() : null;
        if (gridData == null)
        {
            Debug.LogError($"DataManager: Slot {slot.id} did not contain GridData");
            return;
        }

        CacheManager cacheManager = CacheManager.Instance;
        if (cacheManager != null)
        {
            cacheManager.Write(slot.Data);
        }
        else
        {
            Debug.LogError("DataManager: CacheManager.Instance not found");
            return;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null)
        {
            Debug.LogError("DataManager: SceneLoader.Instance not found");
            return;
        }

        sceneLoader.LoadScene(GameSceneName);
    }

    public void Save(SaveSlot slot, HexGridManager grid)
    {
        if (slot == null)
        {
            Debug.LogError("DataManager: Cannot save to null slot");
            return;
        }

        selectedSaveSlot = slot;

        if (grid == null)
        {
            Debug.LogError("DataManager: Cannot save null grid");
            return;
        }

        // Capture current grid state into SaveData
        SaveData saveData = new SaveData($"Save_{slot.id}", slot.id, grid);
        slot.Data = saveData;

        // Write to disk
        slot.WriteToDisk();
    }

    public void NewGame(SaveSlot slot)
    {
        if (slot == null)
        {
            Debug.LogError("DataManager: Cannot create new game in null slot");
            return;
        }

        selectedSaveSlot = slot;

        CacheManager cacheManager = CacheManager.Instance;
        if (cacheManager != null)
        {
            cacheManager.Clear();
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null)
        {
            Debug.LogError("DataManager: SceneLoader.Instance not found");
            return;
        }

        sceneLoader.LoadScene(SetupSceneName);
    }

    public void DeleteGame(SaveSlot slot)
    {
        if (slot == null)
        {
            Debug.LogError("DataManager: Cannot delete from null slot");
            return;
        }

        slot.DeleteFromDisk();

        if (selectedSaveSlot == slot)
        {
            selectedSaveSlot = null;
        }
    }

    private void OnTurnEnd(int turnNumber)
    {
        if (selectedSaveSlot == null)
        {
            return;
        }

        Save(selectedSaveSlot, HexGridManager.Instance);
    }
}
