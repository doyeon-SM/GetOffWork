using UnityEngine;

public class MainSceneBootstrap : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private WorkDayManager workDayManager;

    [Header("신문 UI (MainScene 시작 시 자동 표시)")]
    [SerializeField] private UINewspaper newspaperUI;

    private void Awake()
    {
        BindAll();
    }

    private void Start()
    {
        // WorkDayManager.Start()가 isPausedByUI=true 로 이미 정지시킨 상태.
        // 신문이 연결돼 있으면 열고, 닫을 때 ResumeTimer 자동 호출.
        if (newspaperUI != null)
            newspaperUI.Open();
        else
            workDayManager?.ResumeTimer(); // 신문 없으면 즉시 재개
    }

    private void BindAll()
    {
        PlayerBase player = PlayerBase.Instance;

        if (player == null)
        {
            Debug.LogError("[MainSceneBootstrap] PlayerBase Instance가 없습니다!");
            return;
        }

        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        if (workDayManager == null)
            workDayManager = FindFirstObjectByType<WorkDayManager>();

        if (serviceDeskManager != null)
            serviceDeskManager.SetPlayerBase(player);

        if (workDayManager != null)
            workDayManager.SetPlayerBase(player);
    }
}