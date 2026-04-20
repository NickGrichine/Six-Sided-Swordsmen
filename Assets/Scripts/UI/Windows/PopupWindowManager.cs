using UnityEngine;

public class PopupWindowManager : MonoBehaviour
{
    [SerializeField] bool withTransition = false;
    [SerializeField] Button openButton;
    [SerializeField] GameObject windowPrefab;
    [SerializeField] IPopupWindow windowScript;
    [SerializeField] Canvas canvasObject; // NOTE: this is needed to make sure that 
                                          // the popup window appears on top of every element in the canvas.

    void Start()
    {
        openButton.onClick += HandleButtonClick;
            
    }

    private void HandleButtonClick(Button button)
    {
        if (withTransition)
            Curtain.Instance.ShortTransitionWithCallback(Initialize);
        else
            Initialize();
    }

    private void Initialize()
    {
        GameObject window_object = Instantiate(windowPrefab, canvasObject.transform);
        IPopupWindow window_script = window_object.GetComponent<IPopupWindow>();
        window_script.Initialize();
    }
}

