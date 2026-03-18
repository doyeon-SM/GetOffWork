using UnityEngine;

public class MainSceneBootstrap : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private WorkDayManager workDayManager;

    private void Awake()
    {
        BindAll();
    }

    private void BindAll()
    {
        PlayerBase player = PlayerBase.Instance;

        if (player == null)
        {
            Debug.LogError("PlayerBase Instance가 없습니다!");
            return;
        }

        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        if (workDayManager == null)
            workDayManager = FindFirstObjectByType<WorkDayManager>();

        if (serviceDeskManager != null)
        {
            serviceDeskManager.SetPlayerBase(player);
        }

        if (workDayManager != null)
        {
            workDayManager.SetPlayerBase(player);
        }
    }
}