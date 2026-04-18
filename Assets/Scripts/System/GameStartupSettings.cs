using UnityEngine;

public class GameStartupSettings : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private HexGridManager hexGridManager;
    [SerializeField] private DataPersistence dataPersistence;
    [SerializeField] private ShowcaseSetup showcaseSetup;
    [SerializeField] private SimpleTestSpawn simpleTestSpawn;

    [Header("Startup Flags")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool saveOnQuit = true;
    [SerializeField] private bool spawnShowcaseOnStart = false;
    [SerializeField] private bool spawnOnStart = false;

    private void Awake()
    {
        if (hexGridManager != null)
        {
            ApplyGenerateOnStart(hexGridManager, generateOnStart);
        }

        if (dataPersistence != null)
        {
            dataPersistence.SetLoadOnStart(loadOnStart);
            dataPersistence.SetSaveOnQuit(saveOnQuit);
        }

        if (showcaseSetup != null)
        {
            showcaseSetup.SetSpawnShowcaseOnStart(spawnShowcaseOnStart);
        }
        if (simpleTestSpawn != null)
        {
            simpleTestSpawn.SetSpawnOnStart(spawnOnStart);
        }
    }

    private void ApplyGenerateOnStart(HexGridManager grid, bool value)
    {
        // call a setter you add to HexGridManager
        grid.SetGenerateOnStart(value);
    }
}