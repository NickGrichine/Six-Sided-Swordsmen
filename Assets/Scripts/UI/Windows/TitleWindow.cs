using UnityEngine;


public class TitleWindow : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Start()
    {
        startButton.SetState(Button.BUTTON_STATE.ACTIVE);
        startButton.onClick += LoadGameScene;
    }

    private void LoadGameScene(Button button)
    {
        SceneLoader.Instance.LoadScene("Game Scene");
    }
}