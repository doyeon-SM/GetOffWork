using UnityEngine;

public class UIIDCardSpawner : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private GameObject idCardButtonObject;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        if (idCardButtonObject != null)
            idCardButtonObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.OnSpawnIdCardRequested += HandleSpawnIdCard;
        serviceDeskManager.OnCustomerCleared += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.OnSpawnIdCardRequested -= HandleSpawnIdCard;
        serviceDeskManager.OnCustomerCleared -= HandleCustomerCleared;
    }

    private void HandleSpawnIdCard(ComplaintContext complaint)
    {
        if (idCardButtonObject != null)
            idCardButtonObject.SetActive(true);
    }

    private void HandleCustomerCleared()
    {
        if (idCardButtonObject != null)
            idCardButtonObject.SetActive(false);
    }
}