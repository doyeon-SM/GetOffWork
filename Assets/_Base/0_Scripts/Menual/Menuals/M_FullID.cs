using System.Collections.Generic;

/// <summary>
/// 주민등록등본/초본 발급 메뉴얼 (FullID).
///
/// RequiredSteps 순서:
///   1. AskSubmitId         (신분증 제시 요청)  — 순서 강제
///   2. OpenIdCardDetail    (신분증 확인)        — 순서 강제
///   3. SearchRecordByInput (ID 조회)            — 순서 강제
///   4. AskPrintOrMobile    (전달 방식 질문)     — 순서 강제
///   5. PrintDocument / SendMobile (전달)        — 순서 강제 (양자택일)
///      RejectAddressMismatch (반려)             — 플레이어 판단으로 선택
///
/// 제거된 절차:
///   - OpenMonitor        : 모니터는 플레이어가 자유롭게 여는 도구. 절차 강제 없음.
///   - CompareCardAndMonitor : 카드와 모니터 내용 비교는 플레이어가 눈으로 판단.
///                            일치/불일치 판정도 플레이어 몫 (반려 버튼으로 표현).
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

    // ── 메뉴얼 절차 정의 ─────────────────────────────────────────────────
    protected override void BuildSteps()
    {
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

            // 3. ID 조회 (모니터 조회 — 모니터를 여는 것 자체는 절차 아님)
            new ManualStepEntry(
                commandId:        ManualCommandIds.SearchRecordByInput,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 2, performance: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),

            // 4. 전달 방식 질문
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskPrintOrMobile,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     new StepPenalty(kindness: 1),
                completionReward: default
            ),

            // 5-A. 인쇄 전달 (양자택일 — 둘 중 하나만 수행하면 정상)
            new ManualStepEntry(
                commandId:        ManualCommandIds.PrintDocument,
                isOrdered:        true,
                omissionPenalty:  default,
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),

            // 5-B. 전자 전달
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
            playerMessage:    context.lastPlayerMessage,
            customerMessage:  context.lastCustomerMessage,
            shouldSpawnIdCard: true
        );
    }

    // 시스템 내부 호출 — RecordAction 없음
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
            playerMessage:         "신분증 정보를 확인합니다.",
            shouldOpenIdCardDetail: true
        );
    }

    // OpenMonitor는 절차에서 제거됐지만 모니터 오브젝트 클릭 시 호출될 수 있다.
    // 자유롭게 열 수 있도록 조건 없이 허용하되 Queue 기록은 하지 않는다.
    private ResponseResult HandleOpenMonitor()
    {
        return CorrectResponse(
            playerMessage:     "모니터 화면을 엽니다.",
            shouldOpenMonitor: true
        );
    }

    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        if (!context.idCardSpawned)
            return WrongOrder("신분증을 먼저 받아야 합니다.");

        if (string.IsNullOrWhiteSpace(inputId))
            return WrongOrder("조회할 ID를 입력해야 합니다.");

        context.searchedInputId   = inputId;
        context.searchedByInputId = true;

        RecordAction(ManualCommandIds.SearchRecordByInput);

        if (!userDatabase.TryGetRecord(inputId, out _))
        {
            return CorrectResponse(
                playerMessage:            $"ID {inputId}에 해당하는 기록이 없습니다.",
                shouldRefreshMonitorData:  true
            );
        }

        return CorrectResponse(
            playerMessage:            $"ID {inputId} 기록을 조회합니다.",
            shouldRefreshMonitorData:  true
        );
    }

    private ResponseResult HandleAskPrintOrMobile()
    {
        if (!context.searchedByInputId)
            return WrongOrder("먼저 모니터에서 ID를 조회해야 합니다.");

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
        // 플레이어가 눈으로 판단해서 반려. 조회는 완료됐어야 함.
        if (!context.searchedByInputId)
            return WrongOrder("모니터 조회 후 반려할 수 있습니다.");

        RecordAction(ManualCommandIds.RejectAddressMismatch);
        context.rejected  = true;
        context.completed = true;
        return CorrectResponse(
            playerMessage:   "내용이 달라 발급이 어렵습니다.",
            customerMessage: "알겠습니다.",
            completeNow:     true
        );
    }

    public override string GetManualTitle() => "FULLID 등본/초본 발급 메뉴얼";
}
