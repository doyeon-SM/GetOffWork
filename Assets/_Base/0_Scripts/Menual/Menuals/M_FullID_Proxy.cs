using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주민등록등본/초본 발급 — 대리인 신청 메뉴얼.
///
/// [절차 순서]
/// 1. AskSubmitId        — 방문객(대리인) 신분증 요청
/// 2. AskSubmitProxyId   — 대리인 신분증 요청 (IsOrdered=false, 자유 단계)
/// 3. SearchRecordByInput     — 방문객 ID 모니터 조회 (주소불일치 확인용)
/// 4. SearchProxyRecordByInput — 대리인(발급대상) ID 모니터 조회
/// 5. AskPrintOrMobile   — 인쇄/전자 전송 질문
/// 6. [인쇄] PrintDocument — 대리인 ID 기준으로 인쇄
///            방문객 ID로 인쇄 시 WrongOrder(불필요+1)
/// 6. [모바일] AskMobileNumber → MobileNumberByInput
///            전화번호는 방문객(PhoneRecipientRecordId) 번호와 대조
///
/// [반납 필수 물품]
///   방문객 신분증(IDCard) + 대리인 신분증(ProxyIDCard) + 인쇄물(PrintedDoc)
///
/// [비정상 반려]
///   방문객 ID 기준 인쇄물이 반납될 경우 → ServiceDeskManager에서 비정상 판정
/// </summary>
public class M_FullID_Proxy : Manual
{
    private readonly UserRecordDatabase userDatabase;

    public ManualDataSO manualData;

    public M_FullID_Proxy(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    protected override ManualDataSO GetManualDataSO() => manualData;

    public override string GetManualTitle() => "주민등록등본/초본 발급 (대리인)";

    // ── 절차 정의 ────────────────────────────────────────────────────────
protected override void BuildSteps()
    {
        if (manualData != null)
        {
            requiredSteps = manualData.ToStepEntries();
            return;
        }

        Debug.LogWarning("[M_FullID_Proxy] manualData SO가 연결되지 않았습니다. 하드코딩 기본값을 사용합니다.");
        requiredSteps = new List<ManualStepEntry>
        {
            new ManualStepEntry(ManualCommandIds.AskSubmitId,         true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.AskSubmitProxyId,    false, new StepPenalty(kindness: 1),    default),
            // SearchRecordByInput 하나로 방문객 ID와 대상자 ID를 순차 조회
            new ManualStepEntry(ManualCommandIds.SearchRecordByInput, true,  new StepPenalty(reliability: 1), new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.SearchRecordByInput, true,  new StepPenalty(reliability: 1), new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.AskPrintOrMobile,    false, new StepPenalty(kindness: 1),    default),
            new ManualStepEntry(ManualCommandIds.AskMobileNumber,     false, new StepPenalty(kindness: 1),    default),
            new ManualStepEntry(ManualCommandIds.MobileNumberByInput, true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.PrintDocument,       true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.ReturnPrintedDoc,    true,  new StepPenalty(reliability: 1), default),
        };
    }

    protected override void BuildReturnItems() { }

    // ── Execute ──────────────────────────────────────────────────────────
public override ResponseResult Execute(string commandId, string payload = null)
    {
        if (isCompleted && commandId != ManualCommandIds.OpenMonitor)
            return WrongOrderFromSO(commandId, "이미 처리가 완료된 민원입니다.");

        switch (commandId)
        {
            case ManualCommandIds.AskSubmitId:      return HandleAskSubmitId();
            case ManualCommandIds.SpawnIdCard:      return HandleSpawnIdCard();
            case ManualCommandIds.AskSubmitProxyId: return HandleAskSubmitProxyId();
            case ManualCommandIds.SpawnProxyIdCard: return HandleSpawnProxyIdCard();
            case ManualCommandIds.OpenIdCardDetail: return HandleOpenIdCardDetail();
            case ManualCommandIds.OpenMonitor:      return HandleOpenMonitor();
            case ManualCommandIds.SearchRecordByInput: return HandleSearchRecordByInput(payload);
            case ManualCommandIds.AskPrintOrMobile: return HandleAskPrintOrMobile();
            case ManualCommandIds.SelectPrint:      return HandleSelectPrint();
            case ManualCommandIds.SelectMobile:     return HandleSelectMobile();
            case ManualCommandIds.AskMobileNumber:  return HandleAskMobileNumber();
            case ManualCommandIds.MobileNumberByInput: return HandleMobileNumberByInput(payload);
            case ManualCommandIds.PrintDocument:    return HandlePrintDocument(payload);
            default:                                return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

    /// <summary>방문객(대리인 본인) 신분증 요청</summary>
    private ResponseResult HandleAskSubmitId()
    {
        if (context.idCardSpawned)
            return WrongOrderFromSO(ManualCommandIds.AskSubmitId, "이미 제출했습니다.");
        RecordAction(ManualCommandIds.AskSubmitId);
        return CorrectResponseFromSO(ManualCommandIds.AskSubmitId, fallback: "네, 여기 있습니다.", shouldSpawnIdCard: true);
    }

    private ResponseResult HandleSpawnIdCard()
    {
        context.idCardSpawned = true;
        AddRequiredReturnItem(DeskObjectType.IDCard);
        return CorrectResponse();
    }

    /// <summary>대리인(발급 대상자) 신분증 요청 — IsOrdered=false 자유 단계</summary>
    private ResponseResult HandleAskSubmitProxyId()
    {
        if (context.proxyIdCardSpawned)
            return WrongOrderFromSO(ManualCommandIds.AskSubmitProxyId, "이미 제출했습니다.");
        RecordAction(ManualCommandIds.AskSubmitProxyId);
        return CorrectResponseFromSO(ManualCommandIds.AskSubmitProxyId, fallback: "네, 여기 있습니다.", shouldSpawnProxyIdCard: true);
    }

    private ResponseResult HandleSpawnProxyIdCard()
    {
        context.proxyIdCardSpawned = true;
        AddRequiredReturnItem(DeskObjectType.ProxyIDCard);
        return CorrectResponse();
    }

    private ResponseResult HandleOpenIdCardDetail()
    {
        if (!context.idCardSpawned)
            return WrongOrderFromSO(ManualCommandIds.OpenIdCardDetail, "신분증을 아직 드리지 않았는데요.");
        context.idCardInspected = true;
        return CorrectResponse(shouldOpenIdCardDetail: true);
    }

    private ResponseResult HandleOpenMonitor()
    {
        return CorrectResponse(shouldOpenMonitor: true);
    }

    /// <summary>방문객 ID 조회 — 주소불일치 확인용</summary>
/// <summary>
    /// 모니터 ID 조회 통합 핸들러.
    /// 1차 호출: 방문객(applicantRecordId) ID 조회 — 주소불일치 판별
    /// 2차 호출: 대리 대상자(targetRecordId) ID 조회 — proxySearchedInputId 기록
    /// 이후 호출: 불필요 절차로 체덕될 수 있음 (ManualEvaluator가 중복 집계)
    /// </summary>
    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        if (!context.searchedByInputId)
        {
            // 1차 조회: 방문객 ID — 주소불일치 판볔
            context.searchedInputId   = inputId;
            context.searchedByInputId = true;
            if (userDatabase.TryGetRecord(inputId, out UserRecordData record))
                context.isAddressMismatch = record.hasMovedAddress;
            else
                context.isAddressMismatch = false;
            RecordAction(ManualCommandIds.SearchRecordByInput);
            Debug.Log($"[M_FullID_Proxy] 1차 조회(방문객): {inputId} / 주소불일치={context.isAddressMismatch}");
            return CorrectResponse(customerMessage: "", shouldRefreshMonitorData: true);
        }
        else if (!context.proxySearched)
        {
            // 2차 조회: 대리 대상자 ID 기록
            context.proxySearchedInputId = inputId;
            context.proxySearched        = true;
            RecordAction(ManualCommandIds.SearchRecordByInput);
            Debug.Log($"[M_FullID_Proxy] 2차 조회(대상자): {inputId}");
            return CorrectResponse(customerMessage: "", shouldRefreshMonitorData: true);
        }
        else
        {
            // 3차 이후: 불필요 절차 (ManualEvaluator가 중복으로 체덕)
            RecordAction(ManualCommandIds.SearchRecordByInput);
            Debug.Log($"[M_FullID_Proxy] 추가 조회(불필요): {inputId}");
            return CorrectResponse(customerMessage: "", shouldRefreshMonitorData: true);
        }
    }

    /// <summary>대리인(발급 대상) ID 조회</summary>


    private ResponseResult HandleAskPrintOrMobile()
    {
        RecordAction(ManualCommandIds.AskPrintOrMobile);
        if (context.deliveryAsked)
        {
            string repeat = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
                ? "전자 발송이라고 말씀드렸는데요."
                : "인쇄로 말씀드렸는데요.";
            return CorrectResponseFromSO(ManualCommandIds.AskPrintOrMobile, repeat);
        }
        context.deliveryAsked = true;
        string reply = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
            ? "전자 발송 부탁드립니다."
            : "인쇄 부탁드립니다.";
        return CorrectResponseFromSO(ManualCommandIds.AskPrintOrMobile, reply);
    }

    private ResponseResult HandleSelectPrint()
    {
        if (!context.deliveryAsked) return WrongOrder();
        context.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
        return CorrectResponse();
    }

    private ResponseResult HandleSelectMobile()
    {
        if (!context.deliveryAsked) return WrongOrder();
        context.requestedDeliveryType = ComplaintContext.DeliveryType.Mobile;
        return CorrectResponse();
    }

    /// <summary>
    /// 인쇄 처리.
    /// payload로 넘어온 ID가 대리인(targetRecordId)이면 정상.
    /// 방문객(applicantRecordId)이면 불필요 절차 +1.
    /// payload 없으면 마지막으로 조회된 ID(proxySearchedInputId)를 사용.
    /// </summary>
    private ResponseResult HandlePrintDocument(string inputId = null)
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongOrder();

        // 어떤 ID로 인쇄할지 결정
        string printId = string.IsNullOrEmpty(inputId)
            ? context.proxySearchedInputId
            : inputId;

        bool isCorrectId = !string.IsNullOrEmpty(printId)
                           && printId == context.targetRecordId;
        bool isApplicantId = !string.IsNullOrEmpty(printId)
                             && printId == context.applicantRecordId;

        if (isCorrectId)
        {
            // 대리인 ID로 정상 인쇄
            RecordAction(ManualCommandIds.PrintDocument);
            isCompleted       = true;
            context.completed = true;
            return CorrectResponseFromSO(ManualCommandIds.PrintDocument);
        }
        else if (isApplicantId)
        {
            // 방문객 ID로 인쇄 시도 → 불필요 절차 +1, 완료되지 않음
            Debug.Log("[M_FullID_Proxy] 방문객 ID로 인쇄 시도 → 불필요 절차 +1");
            return WrongOrderFromSO(ManualCommandIds.PrintDocument,
                fallback: "죄송합니다, 발급 대상자 정보로 인쇄해 주세요.");
        }
        else
        {
            // ID 조회 없이 인쇄 시도
            RecordAction(ManualCommandIds.PrintDocument);
            isCompleted       = true;
            context.completed = true;
            return CorrectResponseFromSO(ManualCommandIds.PrintDocument);
        }
    }

    private ResponseResult HandleAskMobileNumber()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder("전자 발송을 선택하지 않으셨는데요.");
        RecordAction(ManualCommandIds.AskMobileNumber);
        context.mobileNumberAsked = true;

        // 전화번호는 방문객(창구에 온 사람)의 번호
        string phone    = GetApplicantPhoneNumber();
        string fallback = string.IsNullOrEmpty(phone) ? "010-0000-0000 입니다." : $"{phone} 입니다.";
        string raw      = GetCorrectLine(ManualCommandIds.AskMobileNumber, fallback);
        string line     = ResolvePlaceholders(raw, new System.Collections.Generic.Dictionary<string, string> { { "phone", phone } });
        return CorrectResponse(customerMessage: line);
    }

    /// <summary>
    /// MobilePanel Send — 방문객 전화번호와 대조.
    ///   일치   → isCompleted=true (CallDisplay 시 종료)
    ///   불일치 → WrongOrder 대사만, 절차 미집계
    /// </summary>
    private ResponseResult HandleMobileNumberByInput(string inputPhone)
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder("전자 발송을 선택하지 않으셨는데요.");

        // 전화번호는 방문객 번호와 대조
        string correctPhone = GetApplicantPhoneNumber();
        bool matched = !string.IsNullOrEmpty(correctPhone)
                       && NormalizePhone(inputPhone) == NormalizePhone(correctPhone);
        var placeholders = new System.Collections.Generic.Dictionary<string, string> { { "phone", correctPhone } };

        if (matched)
        {
            RecordAction(ManualCommandIds.MobileNumberByInput);
            context.mobileNumberVerified = true;
            isCompleted       = true;
            context.completed = true;
            string raw = GetCorrectLine(ManualCommandIds.MobileNumberByInput, "전송 완료되었습니다.");
            return CorrectResponse(customerMessage: ResolvePlaceholders(raw, placeholders));
        }
        else
        {
            context.mobileNumberVerified = false;
            string fallback = string.IsNullOrEmpty(correctPhone)
                ? "죄송합니다, 번호가 맞지 않는 것 같습니다."
                : $"전화번호를 다시 확인해 주세요. 제 번호는 {correctPhone} 입니다.";
            string raw = GetWrongOrderLine(ManualCommandIds.MobileNumberByInput, fallback);
            return WrongOrder(ResolvePlaceholders(raw, placeholders));
        }
    }

    // ── 유틸리티 ─────────────────────────────────────────────────────────

    /// <summary>방문객(창구에 온 사람)의 전화번호 — 모바일 전송 수신처</summary>
    private string GetApplicantPhoneNumber()
    {
        string recordId = context.PhoneRecipientRecordId; // 항상 방문객
        if (string.IsNullOrEmpty(recordId)) return string.Empty;
        if (userDatabase.TryGetRecord(recordId, out UserRecordData record))
            return record.phoneNumber ?? string.Empty;
        return string.Empty;
    }

    private static string NormalizePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return string.Empty;
        return phone.Replace("-", "").Replace(" ", "").Trim();
    }
}
