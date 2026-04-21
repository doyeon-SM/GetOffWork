using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 튜토리얼 단계를 관리하는 싱글턴.
///
/// [흐름]
/// - StartTutorial() 호출 시 tutorialFlowPrint(기본)로 시작
/// - select_print / select_mobile 커맨드 감지 시점에 activeFlow를 확정하고
///   해당 FlowSO의 이후 단계를 이어서 진행한다.
/// - 각 FlowSO는 공통 단계(손님 호출 ~ 발급방식 선택)를 앞쪽에 동일하게 포함한다.
///   분기 이후 단계만 Print SO / Mobile SO가 다르다.
/// </summary>
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

    // ── 내부 상태 ─────────────────────────────────────────────────────────
    private TutorialFlowSO _currentFlow;
    private int            _currentStepIndex = 0;
    private bool           _isActive         = false;
    private bool           _flowDecided      = false; // print/mobile 분기 결정 여부

    // 분기 직전 단계의 expectedCommandId — 이 커맨드가 올 때까지는 flow를 교체하지 않는다.
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
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        SubscribeDeskEvents();
    }

    private void OnDestroy() => UnsubscribeDeskEvents();

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
        {
            Debug.LogWarning("[TutorialManager] tutorialFlowPrint SO가 연결되지 않았습니다.");
            return;
        }

        // Print FlowSO로 시작 (공통 앞부분이 동일하므로 어느 쪽이든 무관)
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

        // ── 분기 결정 ─────────────────────────────────────────────────────
        // select_print / select_mobile 이 오면 flow를 확정하고 해당 flow의
        // 같은 인덱스부터 이어서 진행한다.
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

        // ── 일반 단계 완료 확인 ───────────────────────────────────────────
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
    /// 분기 커맨드 수신 시 호출.
    /// 현재 인덱스를 유지한 채 targetFlow로 교체하고, 해당 커맨드 단계를 완료 처리한다.
    /// </summary>
    private void SwitchFlow(TutorialFlowSO targetFlow, string branchCommandId)
    {
        if (targetFlow == null)
        {
            Debug.LogWarning($"[TutorialManager] {branchCommandId} 에 해당하는 FlowSO가 null입니다.");
            return;
        }

        _flowDecided = true;

        // 현재 스텝 ID를 기준으로 targetFlow에서 같은 위치를 찾아 교체
        string currentStepId = GetCurrentStepId();
        int idxInTarget = targetFlow.FindStepIndex(currentStepId);

        if (idxInTarget >= 0)
        {
            _currentFlow      = targetFlow;
            _currentStepIndex = idxInTarget;
        }
        else
        {
            // 현재 단계가 targetFlow에 없으면 그냥 교체 후 인덱스 유지
            _currentFlow = targetFlow;
            Debug.LogWarning($"[TutorialManager] 현재 stepId '{currentStepId}'를 {targetFlow.flowName}에서 찾지 못했습니다. 인덱스를 유지합니다.");
        }

        Debug.Log($"[TutorialManager] Flow 전환: {targetFlow.flowName} (branch={branchCommandId})");

        // 분기 커맨드 자체가 현재 단계의 완료 조건인지 확인
        var step = GetCurrentStep();
        if (step != null && step.expectedCommandId == branchCommandId)
            AdvanceStep();
    }

    // ── 단계 진행 ─────────────────────────────────────────────────────────
    private void AdvanceStep()
    {
        if (_currentFlow == null) return;

        _currentStepIndex++;
        Debug.Log($"[TutorialManager] Step → index={_currentStepIndex} / {CurrentStep}");
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
