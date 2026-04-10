using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주민등록등본/초본 발급 — 본인 신청 메뉴얼.
///
/// [종료 흐름]
/// 매뉴얼 완료(isCompleted=true) 후 플레이어가 CallDisplay를 눌러야 응대가 종료된다.
/// ExecuteCommand에서는 즉시 종료하지 않는다.
///
/// [모바일 전송 흐름]
/// AskPrintOrMobile → SelectMobile → AskMobileNumber → MobileNumberByInput(Send)
///   일치   → isCompleted=true (CallDisplay 시 정상 종료)
///   불일치 → WrongOrder 대사만, 절차 미집계 (계속 시도 가능)
/// </summary>
public class M_FullID_Self : Manual
{
    private readonly UserRecordDatabase userDatabase;

    public ManualDataSO manualData;

    public M_FullID_Self(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    protected override ManualDataSO GetManualDataSO() => manualData;

    public override string GetManualTitle() => "주민등록등본/초본 발급 (본인)";

    // ── 절차 정의 ────────────────────────────────────────────────────────
    protected override void BuildSteps()
    {
        if (manualData != null)
        {
            requiredSteps = manualData.ToStepEntries();
            return;
        }

        Debug.LogWarning("[M_FullID_Self] manualData SO가 연결되지 않았습니다. 하드코딩 기본값을 사용합니다.");
        requiredSteps = new List<ManualStepEntry>
        {
            new ManualStepEntry(ManualCommandIds.AskSubmitId,        true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
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
            case ManualCommandIds.AskSubmitId:         return HandleAskSubmitId();
            case ManualCommandIds.SpawnIdCard:         return HandleSpawnIdCard();
            case ManualCommandIds.OpenIdCardDetail:    return HandleOpenIdCardDetail();
            case ManualCommandIds.OpenMonitor:         return HandleOpenMonitor();
            case ManualCommandIds.SearchRecordByInput: return HandleSearchRecordByInput(payload);
            case ManualCommandIds.AskPrintOrMobile:    return HandleAskPrintOrMobile();
            case ManualCommandIds.SelectPrint:         return HandleSelectPrint();
            case ManualCommandIds.SelectMobile:        return HandleSelectMobile();
            case ManualCommandIds.AskMobileNumber:     return HandleAskMobileNumber();
            case ManualCommandIds.MobileNumberByInput: return HandleMobileNumberByInput(payload);
            case ManualCommandIds.PrintDocument:       return HandlePrintDocument();
            default:                                   return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

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

    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        context.searchedInputId   = inputId;
        context.searchedByInputId = true;
        if (userDatabase.TryGetRecord(inputId, out UserRecordData record))
            context.isAddressMismatch = record.hasMovedAddress;
        else
            context.isAddressMismatch = false;
        RecordAction(ManualCommandIds.SearchRecordByInput);
        return CorrectResponse(customerMessage: "", shouldRefreshMonitorData: true);
    }

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

    private ResponseResult HandleAskMobileNumber()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder("전자 발송을 선택하지 않으셨는데요.");
        RecordAction(ManualCommandIds.AskMobileNumber);
        context.mobileNumberAsked = true;
        string phone    = GetCustomerPhoneNumber();
        string fallback = string.IsNullOrEmpty(phone) ? "010-0000-0000 입니다." : $"{phone} 입니다.";
        string raw      = GetCorrectLine(ManualCommandIds.AskMobileNumber, fallback);
        string line     = ResolvePlaceholders(raw, new System.Collections.Generic.Dictionary<string, string> { { "phone", phone } });
        return CorrectResponse(customerMessage: line);
    }

    /// <summary>
    /// MobilePanel Send — 번호 대조.
    ///   일치   → RecordAction + isCompleted=true. 종료는 CallDisplay 시.
    ///   불일치 → WrongOrder 대사만, 절차 미집계.
    /// </summary>
    private ResponseResult HandleMobileNumberByInput(string inputPhone)
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder("전자 발송을 선택하지 않으셨는데요.");
        string correctPhone = GetCustomerPhoneNumber();
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

    private ResponseResult HandlePrintDocument()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongOrder();
        RecordAction(ManualCommandIds.PrintDocument);
        isCompleted       = true;
        context.completed = true;
        return CorrectResponseFromSO(ManualCommandIds.PrintDocument);
    }

    // ── 유틸리티 ─────────────────────────────────────────────────────────

    private string GetCustomerPhoneNumber()
    {
        string recordId = context.EffectiveTargetRecordId;
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
