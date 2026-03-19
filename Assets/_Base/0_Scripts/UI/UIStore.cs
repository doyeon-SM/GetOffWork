using UnityEngine;
using UnityEngine.UI;

public class UIStore : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    private UIHomeController uihomecontroller;

    public void Initialize(UIHomeController controller)
    {
        uihomecontroller = controller;
        if(closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClickClose);
            closeButton.onClick.AddListener(OnClickClose);
        }
    }

    private void OnClickClose()
    {
        if(uihomecontroller != null)
        {
            uihomecontroller.OnConvenienceStoreClosed();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
