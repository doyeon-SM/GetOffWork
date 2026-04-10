using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주민등록등본/초본 발급 — 대리인 신청 메뉴얼.
///
/// applicantType == Proxy 인 민원에 배정된다.
/// Self와 분리되어 있어 대리인 전용 추가 절차(위임장 확인 등)를
/// 향후 BuildSteps()나 Execute()에 독립적으로 확장할 수 있다.
///
/// 절차 구성은 ManualDataSO(manualData)에서 읽는다.
///
/// 보상/패널티 기준 (ManualDataSO에서 설정):
///   completionReward         : 모든 절차 정상 완료 시 보상
///   abnormalRejectionPenalty : 비정상 반려 패널티
///   missedRejectionPenalty   : 반려사항 놓침 패널티
/// </summary>
public class M_FullID_Proxy : Manual
{
    private readonly UserRecordDatabase userDatabase;

    // ── SO 데이터 연결 ────────────────────────────────────────────────────
    // 외부(ServiceDeskManager)에서 주입. null이면 하드코딩 폴백.
    public ManualDataSO manualData;

    public M_FullID_Proxy(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    public override string GetManualTitle() => "주민등록등본/초본 발급 (대리인)";

    // ── UI 버튼 목록 ─────────────────────────────────────────────────────
    protected override void BuildCommandList()
    {
        commandList = new List<QuestionData>
        {
            new QuestionData(ManualCommandIds.AskSubmitId,      "신분증 제시 요청"),
            new QuestionData(ManualCommandIds.AskPrintOrMobile, "인쇄/전자 전달 질문"),
            // TODO: 대리인 전용 절차 버튼 추가 예정 (예: 위임장 확인 등)
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
            new ManualStepEntry(ManualCommandIds.AskSubmitId,        true,  new StepPenalty(kindness: 1),     new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.SearchRecordByInput, true,  new StepPenalty(reliability: 1),  new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.AskPrintOrMobile,    false, new StepPenalty(kindness: 1),     default),
            new ManualStepEntry(ManualCommandIds.PrintDocument,       true,  new StepPenalty(kindness: 1),     new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.SendMobile,          true,  new StepPenalty(kindness: 1),     new StepPenalty(reliability: 1)),
            // 인쇄 선택 시 서류 반납 필수 (시스템 전용 — 평가에서 제외되는 commandId)
            new ManualStepEntry(ManualCommandIds.ReturnPrintedDoc,    true,  new StepPenalty(reliability: 1),  default),
            // TODO: 대리인 전용 Step 추가 예정
        };
    }

    // ── 반납 필수 목록 ────────────────────────────────────────────────────
    protected override void BuildReturnItems() { }

    // ── Execute ──────────────────────────────────────────────────────────
    public override ResponseResult Execute(string commandId, string payload = null)
    {
        if (isCompleted && commandId != ManualCommandIds.OpenMonitor)
            return WrongOrder("이미 처리가 완료된 민원입니다.");

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
            // TODO: 대리인 전용 commandId 핸들러 추가 예정
            default:                                     return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

    private ResponseResult HandleAskSubmitId()
    {
        if (context.idCardSpawned)
            return WrongOrder(customerMessage: "이미 제출했습니다.");
        RecordAction(ManualCommandIds.AskSubmitId);
        return CorrectResponse(customerMessage: "네, 여기 있습니다.", shouldSpawnIdCard: true);
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
            return WrongOrder(customerMessage: "신분증을 아직 드리지 않았는데요.");
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
        if (context.deliveryAsked)
        {
            RecordAction(ManualCommandIds.AskPrintOrMobile);
            string repeat = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
                ? "전자 발송이라고 말씀드렸는데요."
                : "인쇄로 말씀드렸는데요.";
            return CorrectResponse(customerMessage: repeat);
        }
        RecordAction(ManualCommandIds.AskPrintOrMobile);
        context.deliveryAsked = true;
        string reply = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
            ? "전자 발송 부탁드립니다."
            : "인쇄 부탁드립니다.";
        return CorrectResponse(customerMessage: reply);
    }

    private ResponseResult HandleSelectPrint()
    {
        if (!context.deliveryAsked) return WrongOrder(customerMessage: "");
        context.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
        return CorrectResponse();
    }

    private ResponseResult HandleSelectMobile()
    {
        if (!context.deliveryAsked) return WrongOrder(customerMessage: "");
        context.requestedDeliveryType = ComplaintContext.DeliveryType.Mobile;
        return CorrectResponse();
    }

private ResponseResult HandlePrintDocument()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongOrder(customerMessage: "");
        RecordAction(ManualCommandIds.PrintDocument);
        // 출력된 서류를 필수 반납 목록에 동적 추가
        //AddRequiredReturnItem(DeskObjectType.PrintedDoc);
        isCompleted = true; context.completed = true;
        return CorrectResponse(customerMessage: "감사합니다.", completeNow: false);
    }

    private ResponseResult HandleSendMobile()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder(customerMessage: "");
        RecordAction(ManualCommandIds.SendMobile);
        isCompleted = true; context.completed = true;
        return CorrectResponse(customerMessage: "감사합니다.", completeNow: false);
    }
}
