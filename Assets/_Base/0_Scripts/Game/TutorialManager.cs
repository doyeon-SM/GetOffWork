using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("튜토리얼 흐름 — Print 경로")]
    [SerializeField] private TutorialFlowSO tutorialFlowPrint;

    [Header("튜토리얼 흐름 — Mobile 경로")]
    [SerializeField] private TutorialFlowSO tutorialFlowMobile;

    [Header("연결")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("이벤트")]
    public UnityEvent<string> OnStepChanged;
    public UnityEvent         OnTutorialEnd;

    private TutorialFlowSO _currentFlow;
    private int            _currentStepIndex = 0;
    private bool           _isActive         = false;
    private bool           _flowDecided      = false;

    private const string BranchCommandPrint  = "select_print";
    private const string BranchCommandMobile = "select_mobile";

    public string CurrentStep => GetCurrentStepId();
    public bool   IsActive    => _isActive;
    public bool   IsComplete  => !_isActive;

    // ── 생명주기 ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 씬 전환 감지: MainScene 진입 시 새 ServiceDeskManager를 찾아 재구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        RebindServiceDesk();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeDeskEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀔 때마다 ServiceDeskManager를 새로 찾아서 재구독
        RebindServiceDesk();
    }

    private void RebindServiceDesk()
    {
        UnsubscribeDeskEvents();

        var desk = FindFirstObjectByType<ServiceDeskManager>();
        if (desk != null)
        {
            serviceDeskManager = desk;
            SubscribeDeskEvents();
            Debug.Log($"[TutorialManager] ServiceDeskManager 연결: {desk.gameObject.name}");
        }
    }

    // ── 이벤트 구독 ───────────────────────────────────────────────────────
    private void SubscribeDeskEvents()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnCommandExecuted += HandleCommandExecuted;
        serviceDeskManager.OnCustomerCalled  += HandleCustomerCalled;
    }

    private void UnsubscribeDeskEvents()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnCommandExecuted -= HandleCommandExecuted;
        serviceDeskManager.OnCustomerCalled  -= HandleCustomerCalled;
    }

    // ── 튜토리얼 시작 / 종료 ─────────────────────────────────────────────
    public void StartTutorial()
    {
        if (tutorialFlowPrint == null)
        // StartTutorial 시점에 구독이 확실히 완료되도록 재확인
        if (serviceDeskManager == null)
            RebindServiceDesk();

        {
            Debug.LogWarning("[TutorialManager] tutorialFlowPrint SO가 연결되지 않았습니다.");
            return;
        }

        _currentFlow      = tutorialFlowPrint;
        _currentStepIndex = 0;
        _flowDecided      = false;
        _isActive         = true;

        Debug.Log("[TutorialManager] 튜토리얼 시작");
        ApplyCurrentStep();
    }

    public void EndTutorial()
    {
        _isActive = false;
        TutorialHighlighter.Instance?.ClearAll();
        TutorialHintUI.Instance?.Hide();
        OnTutorialEnd?.Invoke();
        Debug.Log("[TutorialManager] 튜토리얼 완료");
    }

    // ── 커맨드 hook ───────────────────────────────────────────────────────
    private void HandleCommandExecuted(string commandId)
    {
        if (!_isActive) return;

        // 분기 결정: select_print / select_mobile 수신 시 flow 교체 후 다음 단계 진행
        if (!_flowDecided)
        {
            if (commandId == BranchCommandPrint)
            {
                SwitchFlow(tutorialFlowPrint, commandId);
                return;
            }
            if (commandId == BranchCommandMobile)
            {
                SwitchFlow(tutorialFlowMobile, commandId);
                return;
            }
        }

        // 일반 단계 완료 확인
        var step = GetCurrentStep();
        if (step == null || step.completedByCall) return;

        if (step.expectedCommandId == commandId)
            AdvanceStep();
    }

    private void HandleCustomerCalled(ComplaintContext complaint)
    {
        if (!_isActive) return;
        var step = GetCurrentStep();
        if (step == null || !step.completedByCall) return;
        AdvanceStep();
    }

    /// <summary>
    /// 분기 커맨드(select_print/mobile) 수신 시:
    /// 1. targetFlow로 교체
    /// 2. 현재 인덱스의 다음 단계로 즉시 진행 (분기 커맨드 자체는 별도 SO가 없으므로)
    /// </summary>
    private void SwitchFlow(TutorialFlowSO targetFlow, string branchCommandId)
    {
        if (targetFlow == null)
        {
            Debug.LogWarning($"[TutorialManager] {branchCommandId} 에 해당하는 FlowSO가 null입니다.");
            return;
        }

        _flowDecided = true;

        // 현재 stepId를 targetFlow에서 찾아 같은 위치로 교체
        string currentStepId  = GetCurrentStepId();
        int    idxInTarget    = targetFlow.FindStepIndex(currentStepId);

        if (idxInTarget >= 0)
        {
            _currentFlow      = targetFlow;
            _currentStepIndex = idxInTarget;
        }
        else
        {
            _currentFlow = targetFlow;
            Debug.LogWarning($"[TutorialManager] '{currentStepId}'를 {targetFlow.flowName}에서 찾지 못했습니다.");
        }

        Debug.Log($"[TutorialManager] Flow 전환: {targetFlow.flowName} (branch={branchCommandId})");

        // 분기 커맨드는 항상 다음 단계로 진행 (select_print/mobile SO가 별도로 있는 경우도 처리)
        var step = GetCurrentStep();
        if (step != null && step.expectedCommandId == branchCommandId)
        {
            // select_print/select_mobile이 현재 단계의 완료 커맨드인 경우
            AdvanceStep();
        }
        else
        {
            // 현재 단계(TUT_ASK_DELIVERY)를 완료하고 분기 이후 첫 단계로 이동
            AdvanceStep();
        }
    }

    // ── 단계 진행 ─────────────────────────────────────────────────────────
    private void AdvanceStep()
    {
        if (_currentFlow == null) return;

        _currentStepIndex++;
        Debug.Log($"[TutorialManager] Step → index={_currentStepIndex} ({CurrentStep})");
        OnStepChanged?.Invoke(CurrentStep);

        if (_currentStepIndex >= _currentFlow.steps.Count)
        {
            EndTutorial();
            return;
        }

        ApplyCurrentStep();
    }

    private void ApplyCurrentStep()
    {
        var step = GetCurrentStep();
        if (step == null) return;

        TutorialHighlighter.Instance?.Highlight(step);
        TutorialHintUI.Instance?.Show(step.hintText);
        OnStepChanged?.Invoke(step.stepId);
    }

    // ── 레거시 호환 API ───────────────────────────────────────────────────
    public void SetStep(string stepId)
    {
        if (_currentFlow == null) return;
        int idx = _currentFlow.FindStepIndex(stepId);
        if (idx < 0) return;
        _currentStepIndex = idx;
        ApplyCurrentStep();
        OnStepChanged?.Invoke(CurrentStep);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────
    private TutorialStepSO GetCurrentStep()
    {
        if (_currentFlow == null || _currentStepIndex >= _currentFlow.steps.Count) return null;
        return _currentFlow.steps[_currentStepIndex];
    }

    private string GetCurrentStepId()
    {
        var step = GetCurrentStep();
        return step != null ? step.stepId : string.Empty;
    }
}
