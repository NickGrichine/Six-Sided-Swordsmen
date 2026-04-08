// Quick test script - add to scene to debug button setup
using UnityEngine;

public class TestButtonSetup : MonoBehaviour
{
    void Start()
    {
        var unitConsole = UnitConsole.Instance;
        if (!unitConsole)
        {
            Debug.Log("[TEST] UnitConsole not found");
            return;
        }

        Debug.Log($"[TEST] UnitConsole found");
        Debug.Log($"[TEST] Command buttons count: {unitConsole.commandButtons.Length}");
        
        for (int i = 0; i < unitConsole.commandButtons.Length; i++)
        {
            var btn = unitConsole.commandButtons[i];
            if (!btn)
            {
                Debug.Log($"[TEST] commandButtons[{i}] = NULL");
            }
            else
            {
                Debug.Log($"[TEST] commandButtons[{i}] = {btn.gameObject.name}, State={btn.State}");
            }
        }
    }
}
