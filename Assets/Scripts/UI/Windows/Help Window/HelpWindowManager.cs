using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HelpWindowManager : MonoBehaviour, IPopupWindow
{
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI textSection;
    [SerializeField] private Image imageSection;
    [SerializeField] private TextMeshProUGUI bottomText;

    [SerializeField] private HelpPageSO helpPageStart;

    private HelpPageSO currentPage;
    private string bottom_message_with_next = "Click anywhere to continue";
    private string bottom_message_without_next = "Click anywhere to exit";


    public void Initialize()
    {
        imageSection.preserveAspect = true;
        currentPage = helpPageStart;
        InitializeCurrentPage();
        nextButton.onClick += (_) => NextPage();
    }

    public Button GetChainButton() => nextButton;

    public void NextPage()
    {
        if (!currentPage) throw new System.Exception("Current help page can't be null");
        if (!currentPage.next)
        {
            Destroy(gameObject);
            return;
        }

        currentPage = currentPage.next;
        InitializeCurrentPage();
    }

    private void InitializeCurrentPage()
    {
        titleText.text = currentPage.title;
        textSection.text = currentPage.description;
        imageSection.sprite = currentPage.sprite;
        bottomText.text = currentPage.next ? bottom_message_with_next : bottom_message_without_next;
    }
}
