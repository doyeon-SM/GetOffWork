using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 튜토리얼 전체 흐름을 관리한다.
///
/// [Phase 1] Prompt   — 예/아니요 팝업 표시 (손님 도착 타이머 정지)
/// [Phase 2] Intro    — 민원인 무관 순차 대사 (autoAdvance, 1초 자동 진행)
///                      마지막 단계: Calldisplay 하이라이트 (completedByCall=true)
/// [Phase 3] Desk     — 강제 Print 손님 → 민원 처리 튜토리얼
///                      이후 강제 Mobile 손님 → 민원 처리 튜토리얼
/// [Phase 4] Outro    — Menual 하이라이트 + 마무리 대사 → EndTutorial()
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Intro 흐름 (민원인 없는 초반 대사)")]
    [SerializeField] private TutorialFlowSO tutorialFlowIntro;

    [Header("Print 민원 흐름")]
    [SerializeField] private TutorialFlowSO tutorialFlowPrint;

    [Header("Mobile 민원 흐름")]
    [SerializeField] private TutorialFlowSO tutorialFlowMobile;

    [Header("Outro 흐름 (마무리 대사)")]
    [SerializeField] private TutorialFlowSO tutorialFlowOutro;

    [Header("연결")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("이벤트")]
    public UnityEvent<string> OnStepChanged;
    public UnityEvent         OnTutorialEnd;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private TutorialFlowSO _currentFlow;
    private int            _currentStepIndex;
    private bool           _isActive;
    private bool           _flowDecided;

    private bool _printCompleted;
    private bool _mobileCompleted;

    private Coroutine _autoAdvanceCoroutine;

    private const string BranchCommandPrint  = "select_print";
    private const string BranchCommandMobile = "select_mobile";

    public string CurrentStep => GetCurrentStepId();
    public bool   IsActive    => _isActive;

    // ── 생명주기 ──────────────────────────────────────────────────────────────
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
        QuestionObject.OnPostitClicked -= HandlePostitClicked;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RebindServiceDesk();

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

    // ── 이벤트 구독 ───────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 0: 외부 진입점
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>MainSceneBootstrap에서 1일차에만 호출한다.</summary>
    public void StartTutorial()
    {
        if (serviceDeskManager == null) RebindServiceDesk();

        // 손님 도착 일시정지 (예/아니요 팝업 동안)
        serviceDeskManager?.PauseArrival();

        // 예/아니요 팝업 표시
        var prompt = FindFirstObjectByType<UITutorialPrompt>(FindObjectsInactive.Include);
        if (prompt != null)
        {
            prompt.Show();
            Debug.Log("[TutorialManager] 튜토리얼 여부 팝업 표시");
        }
        else
        {
            // 팝업 없으면 바로 수락
            Debug.LogWarning("[TutorialManager] UITutorialPrompt 없음 → 자동 수락");
            OnPromptAccepted();
        }
    }

    /// <summary>UITutorialPrompt '예' 클릭 시 호출</summary>
    public void OnPromptAccepted()
    {
        _printCompleted  = false;
        _mobileCompleted = false;

        // Intro FlowSO가 없으면 바로 강제 손님 준비 후 종료
        if (tutorialFlowIntro == null || tutorialFlowIntro.steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] tutorialFlowIntro 없음 → Desk 단계 직행");
            PrepareAndBeginDesk();
            return;
        }

        // 강제 손님을 Intro 시작 전에 미리 대기열에 삽입
        // (Intro 마지막 단계의 Calldisplay 클릭이 성공하려면 대기열에 손님이 있어야 함)
        PrepareQueueForTutorial();

        BeginFlow(tutorialFlowIntro);
        Debug.Log("[TutorialManager] 튜토리얼 시작 — Intro");
    }

    /// <summary>UITutorialPrompt '아니요' 클릭 시 호출</summary>
    public void OnPromptDeclined()
    {
        serviceDeskManager?.ResumeArrival();
        Debug.Log("[TutorialManager] 튜토리얼 거절");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 1: Intro (민원인 없는 순차 대사)
    // ─────────────────────────────────────────────────────────────────────────

    private void BeginFlow(TutorialFlowSO flow)
    {
        if (_autoAdvanceCoroutine != null) { StopCoroutine(_autoAdvanceCoroutine); _autoAdvanceCoroutine = null; }

        _currentFlow      = flow;
        _currentStepIndex = 0;
        _flowDecided      = false;
        _isActive         = true;

        Debug.Log($"[TutorialManager] BeginFlow: {flow?.flowName}");
        ApplyCurrentStep();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2: Desk (강제 손님 → Print → Mobile)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 튜토리얼용 강제 손님 2명(Print→Mobile)을 대기열 앞에 삽입한다.
    /// Intro FlowSO 시작 전에 호출해야 Calldisplay 클릭이 성공한다.
    /// </summary>
    private void PrepareQueueForTutorial()
    {
        if (serviceDeskManager == null) return;

        // Print 손님 삽입
        var printComplaint = CreateForcedComplaint(ComplaintContext.DeliveryType.Print);
        serviceDeskManager.EnqueueForcedComplaint(printComplaint);

        // Mobile 손님 삽입 (Print 다음으로)
        var mobileComplaint = CreateForcedComplaint(ComplaintContext.DeliveryType.Mobile);
        serviceDeskManager.EnqueueForcedComplaint(mobileComplaint);

        Debug.Log("[TutorialManager] 강제 손님 2명 대기열 삽입 완료");
    }

    // 호환성 유지용 (내부에서 사용하지 않지만 기존 참조 대비)
    private void PrepareAndBeginDesk()
    {
        PrepareQueueForTutorial();
        serviceDeskManager?.ResumeArrival();

        if (tutorialFlowPrint == null)
        {
            Debug.LogWarning("[TutorialManager] tutorialFlowPrint 없음 → 튜토리얼 종료");
            EndTutorial();
            return;
        }

        BeginFlow(tutorialFlowPrint);
        Debug.Log("[TutorialManager] Desk 단계 시작 — Print 경로");
    }

    private ComplaintContext CreateForcedComplaint(ComplaintContext.DeliveryType delivery)
    {
        var c = new ComplaintContext();
        c.complaintType         = ComplaintContext.ComplaintType.FullID;
        c.applicantType         = ComplaintContext.ApplicantType.Self;
        c.requestedDeliveryType = delivery;
        c.nuisanceType          = ComplaintContext.NuisanceType.None;

        // 레코드 배정 (UserDatabase 첫 번째 레코드 사용)
        var ub = ServiceDataManager.Instance?.UserDatabase;
        if (ub != null && ub.Records != null && ub.Records.Count > 0)
        {
            c.applicantRecordId = ub.Records[0].recordId;
            c.targetRecordId    = ub.Records[0].recordId;
            ub.Records[0].SetIdCard(false, false, false);
        }

        // ManualData 배정 (delivery에 맞는 ManualDataSO 탐색)
        var mmd = ServiceDataManager.Instance?.ManualDataManager;
        if (mmd != null)
        {
            string keyword = delivery == ComplaintContext.DeliveryType.Print ? "Print" : "Mobile";
            foreach (var entry in mmd.GetAllEntries())
            {
                if (entry.manualData?.manualTitle != null &&
                    entry.manualData.manualTitle.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.assignedManualData = entry.manualData;
                    break;
                }
            }
        }

        c.maxPatience     = 999f;
        c.currentPatience = 999f;
        return c;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 3: Outro
    // ─────────────────────────────────────────────────────────────────────────

    private void BeginOutro()
    {
        if (tutorialFlowOutro == null || tutorialFlowOutro.steps.Count == 0)
        {
            EndTutorial();
            return;
        }
        BeginFlow(tutorialFlowOutro);
        Debug.Log("[TutorialManager] Outro 시작");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 종료
    // ─────────────────────────────────────────────────────────────────────────

    public void EndTutorial()
    {
        _isActive = false;
        if (_autoAdvanceCoroutine != null) { StopCoroutine(_autoAdvanceCoroutine); _autoAdvanceCoroutine = null; }
        TutorialHighlighter.Instance?.ClearAll();
        TutorialHintUI.Instance?.Hide();
        OnTutorialEnd?.Invoke();
        Debug.Log("[TutorialManager] 튜토리얼 완전 종료");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleCommandExecuted(string commandId)
    {
        if (!_isActive) return;

        // 분기 결정
        if (!_flowDecided)
        {
            if (commandId == BranchCommandPrint)  { SwitchFlow(tutorialFlowPrint,  commandId); return; }
            if (commandId == BranchCommandMobile) { SwitchFlow(tutorialFlowMobile, commandId); return; }
        }

        var step = GetCurrentStep();
        if (step == null || step.completedByCall || step.completedByPostit || step.autoAdvance) return;

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

    private void HandlePostitClicked()
    {
        if (!_isActive) return;
        var step = GetCurrentStep();
        if (step == null || !step.completedByPostit) return;
        AdvanceStep();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Flow 전환 (select_print / select_mobile)
    // ─────────────────────────────────────────────────────────────────────────

    private void SwitchFlow(TutorialFlowSO targetFlow, string branchCommandId)
    {
        if (targetFlow == null) { Debug.LogWarning($"[TutorialManager] {branchCommandId} FlowSO null"); return; }

        _flowDecided = true;
        string currentStepId = GetCurrentStepId();
        int    idx           = targetFlow.FindStepIndex(currentStepId);

        _currentFlow      = targetFlow;
        _currentStepIndex = idx >= 0 ? idx : _currentStepIndex;

        Debug.Log($"[TutorialManager] Flow 전환: {targetFlow.flowName} ({branchCommandId})");
        AdvanceStep();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 단계 진행
    // ─────────────────────────────────────────────────────────────────────────

    private void AdvanceStep()
    {
        if (_currentFlow == null) return;
        if (_autoAdvanceCoroutine != null) { StopCoroutine(_autoAdvanceCoroutine); _autoAdvanceCoroutine = null; }

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

    private void OnFlowCompleted()
    {
        TutorialHighlighter.Instance?.ClearAll();
        TutorialHintUI.Instance?.Hide();

        bool isIntro  = (_currentFlow == tutorialFlowIntro);
        bool isPrint  = (_currentFlow == tutorialFlowPrint);
        bool isMobile = (_currentFlow == tutorialFlowMobile);
        bool isOutro  = (_currentFlow == tutorialFlowOutro);

        Debug.Log($"[TutorialManager] Flow 완료 — {_currentFlow?.flowName}");

        if (isIntro)
        {
            // Intro 완료 → Print 경로 시작 (강제 손님은 이미 대기열에 있음)
            if (tutorialFlowPrint == null)
            {
                Debug.LogWarning("[TutorialManager] tutorialFlowPrint 없음 → 튜토리얼 종료");
                EndTutorial();
                return;
            }
            BeginFlow(tutorialFlowPrint);
            Debug.Log("[TutorialManager] Intro 완료 → Print 경로 시작");
            return;
        }

        if (isPrint)  _printCompleted  = true;
        if (isMobile) _mobileCompleted = true;

        if (isPrint && !_mobileCompleted)
        {
            Debug.Log("[TutorialManager] Mobile 경로 시작");
            BeginFlow(tutorialFlowMobile);
            return;
        }

        if (isMobile && !_printCompleted)
        {
            Debug.Log("[TutorialManager] Print 경로 시작");
            BeginFlow(tutorialFlowPrint);
            return;
        }

        if (_printCompleted && _mobileCompleted)
        {
            // Desk 완료 → Outro
            BeginOutro();
            return;
        }

        if (isOutro)
        {
            EndTutorial();
            return;
        }

        EndTutorial();
    }

    private void ApplyCurrentStep()
    {
        var step = GetCurrentStep();
        if (step == null) { Debug.Log("[TutorialManager] ApplyCurrentStep: step null"); return; }

        Debug.Log($"[TutorialManager] ApplyCurrentStep: {step.stepId}");

        // 포스트잇 이벤트 구독 (필요한 단계만)
        QuestionObject.OnPostitClicked -= HandlePostitClicked;
        if (step.completedByPostit)
            QuestionObject.OnPostitClicked += HandlePostitClicked;

        TutorialHighlighter.Instance?.Highlight(step);
        TutorialHintUI.Instance?.Show(step.hintText);
        OnStepChanged?.Invoke(step.stepId);

        // 자동 진행
        if (step.autoAdvance)
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfter(step.autoAdvanceDelay));
    }

    private IEnumerator AutoAdvanceAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _autoAdvanceCoroutine = null;
        AdvanceStep();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────────────────────────────────

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

    public void SetStep(string stepId)
    {
        if (_currentFlow == null) return;
        int idx = _currentFlow.FindStepIndex(stepId);
        if (idx < 0) return;
        _currentStepIndex = idx;
        ApplyCurrentStep();
        OnStepChanged?.Invoke(CurrentStep);
    }
}
