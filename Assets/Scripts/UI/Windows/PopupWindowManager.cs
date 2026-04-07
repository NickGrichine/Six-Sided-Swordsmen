using UnityEngine;

public class PopupWindowManager : MonoBehaviour
{
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;
    [SerializeField] PopupWindow windowObject;

    void Start()
    {
        openButton.onClick += (_) => { windowObject.Open(); };
        closeButton.onClick += (_) => { windowObject.Close(); };
    }
}

