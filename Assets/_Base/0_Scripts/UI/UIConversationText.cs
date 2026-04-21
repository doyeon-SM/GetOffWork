using TMPro;
using UnityEngine;

public class UIConversationText : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("텍스트")]
    //[SerializeField] private TMP_Text playerText;
    [SerializeField] private GameObject customerTextBox;
    [SerializeField] private TMP_Text customerText;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        ClearAll();
    }

    private void OnEnable()
    {
        if (serviceDeskManager == null) return;
        //serviceDeskManager.OnPlayerText       += HandlePlayerText;
        serviceDeskManager.OnCustomerText     += HandleCustomerText;
        serviceDeskManager.OnCustomerOpening  += HandleCustomerOpening;
        serviceDeskManager.OnCustomerCleared  += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null) return;
        //serviceDeskManager.OnPlayerText       -= HandlePlayerText;
        serviceDeskManager.OnCustomerText     -= HandleCustomerText;
        serviceDeskManager.OnCustomerOpening  -= HandleCustomerOpening;
        serviceDeskManager.OnCustomerCleared  -= HandleCustomerCleared;
    }

    /*private void HandlePlayerText(string message)
    {
        if (playerText != null) playerText.text = message;
    }*/

    private void HandleCustomerText(string message)
    {
        if (customerTextBox != null) customerTextBox.SetActive(true);
        if (customerText != null) customerText.text = message;
    }

    /// <summary>입장 대사 — 현재는 customerText에 동일하게 표시</summary>
    private void HandleCustomerOpening(string message)
    {
        if (customerTextBox != null) customerTextBox.SetActive(true);
        if (customerText != null) customerText.text = message;
    }

    private void HandleCustomerCleared()
    {
        ClearAll();
    }

    private void ClearAll()
    {
        //if (playerText != null)   playerText.text   = string.Empty;
        if (customerTextBox != null) customerTextBox.SetActive(false);
        if (customerText != null) customerText.text = string.Empty;
    }
}
