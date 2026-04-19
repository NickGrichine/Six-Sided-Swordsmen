using UnityEngine;

public class CacheManager : Singleton<CacheManager>
{
    private SaveData cachedSaveData;

    public void Write(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("CacheManager: Cannot write null SaveData");
            cachedSaveData = null;
            return;
        }

        cachedSaveData = saveData;
        Debug.Log("CacheManager: SaveData cached to memory");
    }

    public bool TryRead(HexGridManager grid)
    {
        if (grid == null)
        {
            Debug.LogError("CacheManager: Cannot read into null HexGridManager");
            return false;
        }

        if (cachedSaveData == null)
        {
            Debug.LogWarning("CacheManager: No cached SaveData available");
            return false;
        }

        GridData gridData = cachedSaveData.GetGridData();
        if (gridData == null)
        {
            Debug.LogError("CacheManager: Cached SaveData does not contain GridData");
            return false;
        }

        GridAdapter.FromData(grid, gridData);
        Debug.Log("CacheManager: Cached grid state restored");
        return true;
    }

    public bool HasCachedData()
    {
        return cachedSaveData != null;
    }

    public SaveData GetCachedSaveData()
    {
        return cachedSaveData;
    }

    public void Clear()
    {
        cachedSaveData = null;
        Debug.Log("CacheManager: Cache cleared");
    }
}
