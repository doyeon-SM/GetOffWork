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

    // Print/Mobile 각각 완료 여부 추적
    private bool _printCompleted  = false;
    private bool _mobileCompleted = false;

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
        if (serviceDeskManager == null)
            RebindServiceDesk();

        if (tutorialFlowPrint == null)
        {
            Debug.LogWarning("[TutorialManager] tutorialFlowPrint SO가 연결되지 않았습니다.");
            return;
        }

        _printCompleted  = false;
        _mobileCompleted = false;

        BeginFlow(tutorialFlowPrint);
        Debug.Log("[TutorialManager] 튜토리얼 시작");
    }

    /// <summary>지정한 FlowSO를 처음부터 시작한다.</summary>
    private void BeginFlow(TutorialFlowSO flow)
    {
        _currentFlow      = flow;
        _currentStepIndex = 0;
        _flowDecided      = false;
        _isActive         = true;
        Debug.Log($"[TutorialManager] BeginFlow(TutorialFlowSO flow) | currentFlow = {flow.flowName}");

        ApplyCurrentStep();
    }

    public void EndTutorial()
    {
        _isActive = false;
        TutorialHighlighter.Instance?.ClearAll();
        TutorialHintUI.Instance?.Hide();
        OnTutorialEnd?.Invoke();
        Debug.Log("[TutorialManager] 튜토리얼 완전 종료");
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

    private void SwitchFlow(TutorialFlowSO targetFlow, string branchCommandId)
    {
        if (targetFlow == null)
        {
            Debug.LogWarning($"[TutorialManager] {branchCommandId} 에 해당하는 FlowSO가 null입니다.");
            return;
        }

        _flowDecided = true;

        string currentStepId = GetCurrentStepId();
        int    idxInTarget   = targetFlow.FindStepIndex(currentStepId);

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
        AdvanceStep();
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
            OnFlowCompleted();
            return;
        }

        ApplyCurrentStep();
    }

    /// <summary>
    /// 한 경로(Print or Mobile)가 끝났을 때 호출.
    /// 둘 다 완료됐으면 EndTutorial(), 아니면 남은 경로를 시작한다.
    /// </summary>
    private void OnFlowCompleted()
    {
        bool wasPrint  = (_currentFlow == tutorialFlowPrint);
        bool wasMobile = (_currentFlow == tutorialFlowMobile);

        if (wasPrint)  _printCompleted  = true;
        if (wasMobile) _mobileCompleted = true;

        Debug.Log($"[TutorialManager] 경로 완료 — print={_printCompleted} mobile={_mobileCompleted}");

        TutorialHighlighter.Instance?.ClearAll();
        TutorialHintUI.Instance?.Hide();

        // 둘 다 완료 → 진짜 종료
        if (_printCompleted && _mobileCompleted)
        {
            EndTutorial();
            return;
        }

        // 아직 안 한 경로 시작
        if (!_mobileCompleted && tutorialFlowMobile != null)
        {
            Debug.Log("[TutorialManager] Mobile 경로 시작");
            BeginFlow(tutorialFlowMobile);
        }
        else if (!_printCompleted && tutorialFlowPrint != null)
        {
            Debug.Log("[TutorialManager] Print 경로 시작");
            BeginFlow(tutorialFlowPrint);
        }
        else
        {
            // FlowSO 미연결 등 예외
            EndTutorial();
        }
    }

    private void ApplyCurrentStep()
    {
        var step = GetCurrentStep();
        if (step == null)
        {
            Debug.Log($"[TutorialManager] ApplyCurrentStep step null");
            return; 
        }

        Debug.Log($"[TutorialManager] ApplyCurrentStep step = {step.stepId}");
        TutorialHighlighter.Instance?.Highlight(step);
        TutorialHintUI.Instance?.Show(step.hintText);
        OnStepChanged?.Invoke(step.stepId);
    }

    // ── 레거시 호환 API ───────────────────────────────────────────────────
    public void SetStep(string stepId)
    {
        if (_currentFlow == null)
        {
            Debug.Log("[TutorialManager] SetStep _currentFlow Null");
            return; 
        }
        int idx = _currentFlow.FindStepIndex(stepId);
        if (idx < 0) return;
        _currentStepIndex = idx;
        ApplyCurrentStep();
        OnStepChanged?.Invoke(CurrentStep);
        Debug.Log($"[TutorialManager] SetStep stepId = {stepId} | index = {idx}");
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
