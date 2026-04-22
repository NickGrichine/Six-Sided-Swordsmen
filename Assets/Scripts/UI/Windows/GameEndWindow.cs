using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameEndWindow : MonoBehaviour, IPopupWindow
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Button closeButton;


    public Button GetChainButton() => closeButton;

    public void Initialize()
    {
        string winnerText = $"Player {(int)GameManager.Instance.TurnPlayer} Wins!";
        string turnText = $"Game ended on turn {GameManager.Instance.TurnNumber}";
        text.text = $"{winnerText}\n{turnText}";

        if (closeButton != null)
        {
            closeButton.onClick += HandleClose;
            closeButton.SetState(Button.BUTTON_STATE.ACTIVE);
        }
    }

    private void HandleClose(Button _)
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene("Title");
        }
    }

}
