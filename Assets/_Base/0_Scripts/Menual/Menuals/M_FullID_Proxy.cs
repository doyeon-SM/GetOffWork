using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주민등록등본/초본 발급 — 대리인 신청 메뉴얼.
///
/// applicantType == Proxy 인 민원에 배정된다.
/// 대사는 ManualDataSO(manualData.dialogues)의 CommandDialogueSO에서 조회한다.
/// SO에 대사가 없으면 하드코딩 폴백 문자열을 사용한다.
/// </summary>
public class M_FullID_Proxy : Manual
{
    private readonly UserRecordDatabase userDatabase;

    /// <summary>외부(ServiceDeskManager)에서 주입. null이면 하드코딩 폴백.</summary>
    public ManualDataSO manualData;

    public M_FullID_Proxy(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    protected override ManualDataSO GetManualDataSO() => manualData;

    public override string GetManualTitle() => "주민등록등본/초본 발급 (대리인)";

    // ── UI 버튼 목록 ─────────────────────────────────────────────────────
    protected override void BuildCommandList()
    {
        commandList = new List<QuestionData>
        {
            new QuestionData(ManualCommandIds.AskSubmitId,      "신분증 제시 요청"),
            new QuestionData(ManualCommandIds.AskPrintOrMobile, "인쇄/전자 전달 질문"),
        };
    }

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
            new ManualStepEntry(ManualCommandIds.AskSubmitId,        true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.SearchRecordByInput, true,  new StepPenalty(reliability: 1), new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.AskPrintOrMobile,    false, new StepPenalty(kindness: 1),    default),
            new ManualStepEntry(ManualCommandIds.PrintDocument,       true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.SendMobile,          true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
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
            case ManualCommandIds.AskSubmitId:          return HandleAskSubmitId();
            case ManualCommandIds.SpawnIdCard:           return HandleSpawnIdCard();
            case ManualCommandIds.OpenIdCardDetail:      return HandleOpenIdCardDetail();
            case ManualCommandIds.OpenMonitor:           return HandleOpenMonitor();
            case ManualCommandIds.SearchRecordByInput:   return HandleSearchRecordByInput(payload);
            case ManualCommandIds.AskPrintOrMobile:      return HandleAskPrintOrMobile();
            case ManualCommandIds.SelectPrint:           return HandleSelectPrint();
            case ManualCommandIds.SelectMobile:          return HandleSelectMobile();
            case ManualCommandIds.PrintDocument:         return HandlePrintDocument();
            case ManualCommandIds.SendMobile:            return HandleSendMobile();
            default:                                     return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

    private ResponseResult HandleAskSubmitId()
    {
        if (context.idCardSpawned)
            return WrongOrderFromSO(ManualCommandIds.AskSubmitId, "이미 제출했습니다.");

        RecordAction(ManualCommandIds.AskSubmitId);
        return CorrectResponseFromSO(
            ManualCommandIds.AskSubmitId,
            fallback: "네, 여기 있습니다.",
            shouldSpawnIdCard: true);
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

    private ResponseResult HandlePrintDocument()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongOrder();
        RecordAction(ManualCommandIds.PrintDocument);
        isCompleted = true;
        context.completed = true;
        return CorrectResponseFromSO(ManualCommandIds.PrintDocument, completeNow: false);
    }

    private ResponseResult HandleSendMobile()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder();
        RecordAction(ManualCommandIds.SendMobile);
        isCompleted = true;
        context.completed = true;
        return CorrectResponseFromSO(ManualCommandIds.SendMobile, completeNow: false);
    }
}
