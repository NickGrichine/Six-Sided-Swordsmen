using System.IO;
using UnityEngine;

public class SaveSlot : IButtonDisplayable
{
    public readonly int id;
    public readonly string path;

    private SaveData data;
    private static readonly Sprite DEFAULT_ICON = null; // Can be set to a save icon sprite if desired

    public SaveData Data
    {
        get { return data; }
        set { data = value; }
    }

    public SaveSlot(int id)
    {
        this.id = id;
        this.path = Path.Combine(Application.persistentDataPath, $"save_{id}.json");
        this.data = null;

        // Hydrate slot state as soon as the slot object is created.
        if (File.Exists(path))
        {
            ReadFromDisk();
        }
    }

    public Sprite GetIcon()
    {
        return DEFAULT_ICON;
    }

    public string GetTextDescription()
    {
        if (data == null)
        {
            return "Empty";
        }

        return $"{data.GetName()}\n[turn {data.GetTurnNumber()}]";
    }

    public void WriteToDisk()
    {
        if (data == null)
        {
            Debug.LogError($"SaveSlot {id}: Cannot write null SaveData to disk.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"SaveSlot {id}: Saved to {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSlot {id}: Failed to write to disk: {ex.Message}");
        }
    }

    public bool ReadFromDisk()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"SaveSlot {id}: File does not exist at {path}");
            data = null;
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogError($"SaveSlot {id}: Failed to deserialize SaveData from {path}");
                return false;
            }
            Debug.Log($"SaveSlot {id}: Loaded from {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSlot {id}: Failed to read from disk: {ex.Message}");
            data = null;
            return false;
        }
    }

    public void DeleteFromDisk()
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                data = null;
                Debug.Log($"SaveSlot {id}: Deleted save file at {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveSlot {id}: Failed to delete file: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"SaveSlot {id}: No file to delete at {path}");
        }
    }

    public bool ExistsOnDisk()
    {
        return File.Exists(path);
    }
}
