using UnityEngine;

public class PopupWindowManager : MonoBehaviour
{
    [SerializeField] Button openButton;
    [SerializeField] GameObject windowPrefab;
    [SerializeField] IPopupWindow windowScript;

    void Start()
    {
        openButton.onClick += (_) =>
        {
            GameObject window_object = Instantiate(windowPrefab, this.transform);
            IPopupWindow window_script = window_object.GetComponent<IPopupWindow>();
            window_script.Initialize();
        };
    }
}

