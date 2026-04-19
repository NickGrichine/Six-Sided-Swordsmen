using UnityEngine;


public class TitleWindow : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject loadGameMenuPrefab;

    private void Start()
    {
        startButton.SetState(Button.BUTTON_STATE.ACTIVE);
        startButton.onClick += OpenLoadMenu;
    }

    private void OpenLoadMenu(Button button)
    {

        
    }
}
