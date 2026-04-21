using UnityEngine;

public class MainSceneBootstrap : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private WorkDayManager workDayManager;

    private void Awake()
    {
        BindAll();
    }

    private void Start()
    {
        // 신문은 HomeScene에서 표시되므로 MainScene 진입 시 즉시 타이머를 재개한다.
        workDayManager?.ResumeTimer();

        // 1일차에만 튜토리얼 시작
        TryStartTutorial();
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

    private void TryStartTutorial()
    {
        // GameFlowManager가 없으면 에디터 직접 실행 중으로 간주하고 튜토리얼 건너덧.
        if (GameFlowManager.Instance == null) return;

        // 1일차에만 튜토리얼 실행
        if (GameFlowManager.Instance.CurrentDay != 1) return;

        var tutorial = TutorialManager.Instance;
        if (tutorial == null)
        {
            Debug.LogWarning("[MainSceneBootstrap] TutorialManager Instance가 없습니다.");
            return;
        }

        tutorial.StartTutorial();
        Debug.Log("[MainSceneBootstrap] 1일차 튜토리얼 시작");
    }
}