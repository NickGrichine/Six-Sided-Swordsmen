using UnityEngine;

public class PopupWindow : MonoBehaviour
{
    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);

}
