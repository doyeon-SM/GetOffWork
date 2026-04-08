using System.Collections.Generic;

/// <summary>
/// 주민등록등본/초본 발급 메뉴얼 (FullID).
///
/// 대사 정책:
///   - playerMessage 없음 — 플레이어 행동은 UI 질문지 버튼으로 표현
///   - customerMessage 만 출력 (민원인의 대답)
///   - 같은 절차 반복은 불필요한 행동으로 Queue에 중복 기록되어 패널티 적용
///
/// AskPrintOrMobile 선행 조건:
///   - ID 조회 없이도 질문 가능 (searchedByInputId 조건 제거)
///   - 단, 이미 deliveryAsked == true면 중복 질문 → 불필요 절차 패널티
/// </summary>
public class M_FullID : Manual
{
    private readonly UserRecordDatabase userDatabase;

    public M_FullID(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    // ── UI 버튼 목록 ─────────────────────────────────────────────────────
    protected override void BuildCommandList()
    {
        commandList = new List<QuestionData>
        {
            new QuestionData(ManualCommandIds.AskSubmitId,           "신분증 제시 요청"),
            new QuestionData(ManualCommandIds.AskPrintOrMobile,      "인쇄/전자 전달 질문"),
            new QuestionData(ManualCommandIds.RejectAddressMismatch, "반려",
                             QuestionData.CommandVisualType.ActionButton),
        };
    }

    // ── 절차 정의 ────────────────────────────────────────────────────────
    protected override void BuildSteps()
    {
        int perfReward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;

        requiredSteps = new List<ManualStepEntry>
        {
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskSubmitId,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 1, stress: 1),
                orderPenalty:     new StepPenalty(kindness: 1),
                completionReward: default
            ),
            new ManualStepEntry(
                commandId:        ManualCommandIds.OpenIdCardDetail,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 2),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),
            new ManualStepEntry(
                commandId:        ManualCommandIds.SearchRecordByInput,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 2, performance: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskPrintOrMobile,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     new StepPenalty(kindness: 1),
                completionReward: default
            ),
            new ManualStepEntry(
                commandId:        ManualCommandIds.PrintDocument,
                isOrdered:        true,
                omissionPenalty:  default,
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),
            new ManualStepEntry(
                commandId:        ManualCommandIds.SendMobile,
                isOrdered:        true,
                omissionPenalty:  default,
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),
        };
    }

    // ── 반납 필수 목록 ────────────────────────────────────────────────────
    protected override void BuildReturnItems()
    {
        requiredReturnItems.Add(DeskObjectType.IDCard);
    }

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
            case ManualCommandIds.RejectAddressMismatch: return HandleRejectAddressMismatch();
            default:                                     return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

    private ResponseResult HandleAskSubmitId()
    {
        // 이미 신분증이 제출된 상태라면 중복 — Queue 기록 없이 민원인 대사만
        if (context.idCardSpawned)
            return WrongOrder(customerMessage: "이미 제출했습니다.");

        RecordAction(ManualCommandIds.AskSubmitId);
        return CorrectResponse(
            customerMessage:  "네, 여기 있습니다.",
            shouldSpawnIdCard: true
        );
    }

    // 시스템 내부 호출 — RecordAction 없음
    private ResponseResult HandleSpawnIdCard()
    {
        context.idCardSpawned = true;
        return CorrectResponse();
    }

    private ResponseResult HandleOpenIdCardDetail()
    {
        if (!context.idCardSpawned)
            return WrongOrder(customerMessage: "신분증을 아직 드리지 않았는데요.");

        RecordAction(ManualCommandIds.OpenIdCardDetail);
        context.idCardInspected = true;
        return CorrectResponse(
            shouldOpenIdCardDetail: true
        );
    }

    private ResponseResult HandleOpenMonitor()
    {
        return CorrectResponse(
            shouldOpenMonitor: true
        );
    }

    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        if (!context.idCardSpawned)
            return WrongOrder(customerMessage: "신분증을 아직 드리지 않았는데요.");

        //if (string.IsNullOrWhiteSpace(inputId))
        //    return WrongOrder(playerMessage: "조회할 ID를 입력해야 합니다.");

        context.searchedInputId   = inputId;
        context.searchedByInputId = true;

        RecordAction(ManualCommandIds.SearchRecordByInput);

        if (!userDatabase.TryGetRecord(inputId, out _))
            return CorrectResponse(
                customerMessage:         "",
                shouldRefreshMonitorData: true
            );

        return CorrectResponse(
            customerMessage:         "",
            shouldRefreshMonitorData: true
        );
    }

    private ResponseResult HandleAskPrintOrMobile()
    {
        // 이미 질문한 경우 — 중복 질문은 불필요한 절차
        // Queue에 다시 기록되므로 평가 시 패널티 적용
        if (context.deliveryAsked)
        {
            // 재질문은 허용하되 Queue에 다시 쌓아 불필요 절차로 평가
            RecordAction(ManualCommandIds.AskPrintOrMobile);
            string repeat = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
                ? "전자 발송이라고 말씀드렸는데요."
                : "인쇄로 말씀드렸는데요.";
            return CorrectResponse(customerMessage: repeat);
        }

        // ID 조회 없이도 질문 가능 (searchedByInputId 조건 제거)
        RecordAction(ManualCommandIds.AskPrintOrMobile);
        context.deliveryAsked = true;

        string customerReply = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
            ? "전자 발송 부탁드립니다."
            : "인쇄 부탁드립니다.";

        return CorrectResponse(customerMessage: customerReply);
    }

    private ResponseResult HandleSelectPrint()
    {
        if (!context.deliveryAsked)
            return WrongOrder(customerMessage: "");

        context.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
        return CorrectResponse();
    }

    private ResponseResult HandleSelectMobile()
    {
        if (!context.deliveryAsked)
            return WrongOrder(customerMessage: "");

        context.requestedDeliveryType = ComplaintContext.DeliveryType.Mobile;
        return CorrectResponse();
    }

    private ResponseResult HandlePrintDocument()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongOrder(customerMessage: "");

        RecordAction(ManualCommandIds.PrintDocument);
        isCompleted       = true;
        context.completed = true;

        return CorrectResponse(
            customerMessage: "감사합니다.",
            completeNow:     false
        );
    }

    private ResponseResult HandleSendMobile()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder(customerMessage: "");

        RecordAction(ManualCommandIds.SendMobile);
        isCompleted       = true;
        context.completed = true;

        return CorrectResponse(
            customerMessage: "감사합니다.",
            completeNow:     false
        );
    }

    private ResponseResult HandleRejectAddressMismatch()
    {
        if (!context.searchedByInputId)
            return WrongOrder(customerMessage: "");

        RecordAction(ManualCommandIds.RejectAddressMismatch);
        context.rejected  = true;
        context.completed = true;
        isCompleted       = true;

        return CorrectResponse(
            customerMessage: "알겠습니다.",
            completeNow:     false
        );
    }

    public override string GetManualTitle() => "FULLID 등본/초본 발급 메뉴얼";
}
