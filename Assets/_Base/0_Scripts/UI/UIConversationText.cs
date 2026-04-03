using TMPro;
using UnityEngine;

public class UIConversationText : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("≈ÿΩ∫∆Æ")]
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private TMP_Text customerText;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        ClearAll();
    }

    private void OnEnable()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.OnPlayerText += HandlePlayerText;
        serviceDeskManager.OnCustomerText += HandleCustomerText;
        serviceDeskManager.OnCustomerCleared += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.OnPlayerText -= HandlePlayerText;
        serviceDeskManager.OnCustomerText -= HandleCustomerText;
        serviceDeskManager.OnCustomerCleared -= HandleCustomerCleared;
    }

    private void HandlePlayerText(string message)
    {
        if (playerText != null)
            playerText.text = message;
    }

    private void HandleCustomerText(string message)
    {
        if (customerText != null)
            customerText.text = message;
    }

    private void HandleCustomerCleared()
    {
        ClearAll();
    }

    private void ClearAll()
    {
        if (playerText != null)
            playerText.text = string.Empty;

        if (customerText != null)
            customerText.text = string.Empty;
    }
}