using System.Collections.Generic;

/// <summary>
/// 주민등록등본/초본 발급 메뉴얼 (FullID).
///
/// RequiredSteps 순서:
///   1. AskSubmitId        (신분증 제시 요청)   — 순서 강제
///   2. OpenIdCardDetail   (신분증 확인)         — 순서 강제
///   3. OpenMonitor        (모니터 열기)         — 순서 강제
///   4. SearchRecordByInput(ID 조회)             — 순서 강제
///   5. CompareCardAndMonitor (비교)             — 순서 강제
///   6. AskPrintOrMobile   (전달 방식 질문)      — 순서 강제
///   7. PrintDocument / SendMobile (전달)        — 순서 강제
///      RejectAddressMismatch (반려)             — 순서 강제, 주소 불일치 시 대체 경로
///
/// SpawnIdCard는 AskSubmitId 처리 후 내부적으로 자동 호출되는 시스템 명령이므로
/// RequiredSteps에 포함하지 않는다(UI 버튼 없음, Queue 기록 없음).
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
            new QuestionData(ManualCommandIds.RejectAddressMismatch, "주소 불일치 반려",
                             QuestionData.CommandVisualType.ActionButton),
        };
    }

    // ── 메뉴얼 절차 정의 ─────────────────────────────────────────────────
    protected override void BuildSteps()
    {
        // Self(본인): 성과 +3 / Proxy(대리): 성과 +6
        int perfReward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;

        requiredSteps = new List<ManualStepEntry>
        {
            // 1. 신분증 제시 요청
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskSubmitId,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 1, stress: 1),
                orderPenalty:     new StepPenalty(kindness: 1),
                completionReward: default
            ),

            // 2. 신분증 확인 (열람)
            new ManualStepEntry(
                commandId:        ManualCommandIds.OpenIdCardDetail,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 2),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),

            // 3. 모니터 열기
            new ManualStepEntry(
                commandId:        ManualCommandIds.OpenMonitor,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 1),
                orderPenalty:     new StepPenalty(kindness: 1),
                completionReward: default
            ),

            // 4. ID 조회
            new ManualStepEntry(
                commandId:        ManualCommandIds.SearchRecordByInput,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 2, performance: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),

            // 5. 카드-모니터 비교
            new ManualStepEntry(
                commandId:        ManualCommandIds.CompareCardAndMonitor,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 3, performance: 2),
                orderPenalty:     new StepPenalty(reliability: 2),
                completionReward: default
            ),

            // 6. 전달 방식 질문
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskPrintOrMobile,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     new StepPenalty(kindness: 1),
                completionReward: default
            ),

            // 7-A. 인쇄 전달 (PrintDocument) — 선택 경로
            //      전자 전달(SendMobile)은 별도 entry. 평가 시 둘 중 하나만 수행하면 정상.
            new ManualStepEntry(
                commandId:        ManualCommandIds.PrintDocument,
                isOrdered:        true,
                omissionPenalty:  default,          // SendMobile이 있으면 패널티 없음 (평가기가 처리)
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),

            // 7-B. 전자 전달 (SendMobile) — 선택 경로
            new ManualStepEntry(
                commandId:        ManualCommandIds.SendMobile,
                isOrdered:        true,
                omissionPenalty:  default,
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),
        };
    }

    // ── Execute ──────────────────────────────────────────────────────────
    public override ResponseResult Execute(string commandId, string payload = null)
    {
        if (isCompleted || context.completed)
            return WrongOrder("이미 민원이 완료되었습니다.");

        switch (commandId)
        {
            case ManualCommandIds.AskSubmitId:          return HandleAskSubmitId();
            case ManualCommandIds.SpawnIdCard:           return HandleSpawnIdCard();
            case ManualCommandIds.OpenIdCardDetail:      return HandleOpenIdCardDetail();
            case ManualCommandIds.OpenMonitor:           return HandleOpenMonitor();
            case ManualCommandIds.SearchRecordByInput:   return HandleSearchRecordByInput(payload);
            case ManualCommandIds.CompareCardAndMonitor: return HandleCompareCardAndMonitor();
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
        RecordAction(ManualCommandIds.AskSubmitId);

        context.lastPlayerMessage   = "신분증을 제시해주세요.";
        context.lastCustomerMessage = "네, 여기 있습니다.";

        return CorrectResponse(
            playerMessage:   context.lastPlayerMessage,
            customerMessage: context.lastCustomerMessage,
            shouldSpawnIdCard: true
        );
    }

    // SpawnIdCard는 ServiceDeskManager가 AskSubmitId 처리 후 내부 호출하는 시스템 명령.
    // 플레이어 행동이 아니므로 RecordAction을 호출하지 않는다.
    private ResponseResult HandleSpawnIdCard()
    {
        context.idCardSpawned = true;
        return CorrectResponse("신분증이 등록되었습니다.");
    }

    private ResponseResult HandleOpenIdCardDetail()
    {
        if (!context.idCardSpawned)
            return WrongOrder("신분증을 먼저 받아야 합니다.");

        RecordAction(ManualCommandIds.OpenIdCardDetail);
        context.idCardInspected = true;

        return CorrectResponse(
            playerMessage:          "신분증 정보를 확인합니다.",
            shouldOpenIdCardDetail: true
        );
    }

    private ResponseResult HandleOpenMonitor()
    {
        if (!context.idCardSpawned)
            return WrongOrder("먼저 신분증을 받아야 합니다.");

        RecordAction(ManualCommandIds.OpenMonitor);
        context.monitorOpened = true;

        return CorrectResponse(
            playerMessage:     "모니터 화면을 엽니다.",
            shouldOpenMonitor: true
        );
    }

    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        if (!context.monitorOpened)
            return WrongOrder("먼저 모니터를 열어야 합니다.");

        if (string.IsNullOrWhiteSpace(inputId))
            return WrongOrder("조회할 ID를 입력해야 합니다.");

        context.searchedInputId    = inputId;
        context.searchedByInputId  = true;

        RecordAction(ManualCommandIds.SearchRecordByInput);

        if (!userDatabase.TryGetRecord(inputId, out _))
        {
            return CorrectResponse(
                playerMessage: $"ID {inputId}에 해당하는 기록이 없습니다.",
                shouldRefreshMonitorData: true
            );
        }

        return CorrectResponse(
            playerMessage:           $"ID {inputId} 기록을 조회합니다.",
            shouldRefreshMonitorData: true
        );
    }

    private ResponseResult HandleCompareCardAndMonitor()
    {
        if (!context.idCardInspected)
            return WrongOrder("먼저 신분증 상세를 확인해야 합니다.");

        if (!context.searchedByInputId)
            return WrongOrder("먼저 모니터 조회를 해야 합니다.");

        string targetId = context.EffectiveTargetRecordId;

        if (!userDatabase.TryGetRecord(targetId, out var cardRecord))
            return WrongOrder("카드 기록 조회에 실패했습니다.");

        if (!userDatabase.TryGetRecord(context.searchedInputId, out var monitorRecord))
            return WrongOrder("모니터 기록 조회에 실패했습니다.");

        RecordAction(ManualCommandIds.CompareCardAndMonitor);

        context.recordCompared = true;
        context.addressMatched = cardRecord.address == monitorRecord.address;

        if (!context.addressMatched)
        {
            return CorrectResponse(
                playerMessage:           "주소 정보가 일치하지 않습니다.",
                customerMessage:         "아, 그렇군요?",
                shouldRefreshMonitorData: true
            );
        }

        return CorrectResponse(playerMessage: "주소와 정보가 일치합니다.");
    }

    private ResponseResult HandleAskPrintOrMobile()
    {
        if (!context.recordCompared)
            return WrongOrder("먼저 신분증과 모니터 정보를 비교해야 합니다.");

        if (!context.addressMatched)
            return WrongOrder("주소 불일치 상태입니다. 반려 또는 추가 안내가 필요합니다.");

        RecordAction(ManualCommandIds.AskPrintOrMobile);
        context.deliveryAsked = true;

        string customerReply = context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
            ? "전자 발송 부탁드립니다."
            : "인쇄 부탁드립니다.";

        return CorrectResponse(
            playerMessage:   "인쇄해드릴까요? 전자 발송해드릴까요?",
            customerMessage: customerReply
        );
    }

    private ResponseResult HandleSelectPrint()
    {
        if (!context.deliveryAsked)
            return WrongOrder("먼저 전달 방식을 질문해야 합니다.");

        context.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
        return CorrectResponse("인쇄 발급으로 선택합니다.");
    }

    private ResponseResult HandleSelectMobile()
    {
        if (!context.deliveryAsked)
            return WrongOrder("먼저 전달 방식을 질문해야 합니다.");

        context.requestedDeliveryType = ComplaintContext.DeliveryType.Mobile;
        return CorrectResponse("전자 발급으로 선택합니다.");
    }

    private ResponseResult HandlePrintDocument()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongOrder("인쇄 발급이 선택되어 있지 않습니다.");

        if (!context.addressMatched)
            return WrongOrder("주소 확인이 완료되지 않았습니다.");

        RecordAction(ManualCommandIds.PrintDocument);
        context.completed = true;

        return CorrectResponse(
            playerMessage:   "발급이 완료되었습니다.",
            customerMessage: "감사합니다.",
            completeNow:     true
        );
    }

    private ResponseResult HandleSendMobile()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongOrder("전자 발급이 선택되어 있지 않습니다.");

        if (!context.addressMatched)
            return WrongOrder("주소 확인이 완료되지 않았습니다.");

        RecordAction(ManualCommandIds.SendMobile);
        context.completed = true;

        return CorrectResponse(
            playerMessage:   "전자 발송이 완료되었습니다.",
            customerMessage: "감사합니다.",
            completeNow:     true
        );
    }

    private ResponseResult HandleRejectAddressMismatch()
    {
        if (!context.recordCompared)
            return WrongOrder("비교 완료 후에만 반려할 수 있습니다.");

        if (context.addressMatched)
            return WrongOrder("주소가 일치하므로 주소 불일치 반려 사유가 아닙니다.");

        RecordAction(ManualCommandIds.RejectAddressMismatch);
        context.rejected  = true;
        context.completed = true;

        return CorrectResponse(
            playerMessage:   "주소가 달라 발급이 어렵습니다.",
            customerMessage: "알겠습니다.",
            completeNow:     true
        );
    }

    public override string GetManualTitle() => "FULLID 등본/초본 발급 메뉴얼";
}
