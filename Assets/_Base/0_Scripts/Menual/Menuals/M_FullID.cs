using System.Collections.Generic;

/// <summary>
/// 주민등록등본/초본 발급 메뉴얼 (FullID).
///
/// 절차 분류:
///   (필수+순서) AskSubmitId         : 신분증 제시 요청
///   (순서)      SearchRecordByInput  : 주민번호 입력 조회 (선수조건 없음)
///   (필수)      AskPrintOrMobile    : 수령 방법 질문 (순서 강제 없음)
///   (필수+순서) PrintDocument       : 서류 출력 (Print 선택 시)
///   (필수+순서) SendMobile          : 전자 발송 (Mobile 선택 시)
///
/// 패널티 기준:
///   - 필수 누락    → kindness -1  (omissionPenalty)
///   - 순서 위반    → reliability -1 (orderPenalty)
///   - 필수+순서    → 둘 다 적용
///
/// isAddressMismatch == true 시 eval 제외:
///   - AskPrintOrMobile, PrintDocument, SendMobile 누락 패널티 제외
///
/// 필수 반납 물품 동적 관리:
///   - BuildReturnItems()에서 미리 추가하지 않음
///   - AskSubmitId → 신분증 스폰 시 AddRequiredReturnItem(IDCard) 호출
///   - 응대 종료 시 ClearRequiredReturnItems() 호출 (ServiceDeskManager에서)
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
            new QuestionData(ManualCommandIds.AskSubmitId,      "신분증 제시 요청"),
            new QuestionData(ManualCommandIds.AskPrintOrMobile, "인쇄/전자 전달 질문"),
        };
    }

    // ── 절차 정의 ────────────────────────────────────────────────────────
    protected override void BuildSteps()
    {
        int perfReward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;

        requiredSteps = new List<ManualStepEntry>
        {
            // ① 신분증 제시 요청 (필수+순서)
            //   누락 → kindness-1 / 순서위반 → reliability-1
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskSubmitId,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),
            // ② 주민번호 입력 조회 (순서 — 선수조건 없음)
            //   누락 → reliability-1 / 순서위반 → reliability-1
            new ManualStepEntry(
                commandId:        ManualCommandIds.SearchRecordByInput,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(reliability: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: default
            ),
            // ③ 수령 방법 질문 (필수 — 순서 강제 없음)
            //   누락 → kindness-1 / isAddressMismatch==true 시 eval에서 제외
            new ManualStepEntry(
                commandId:        ManualCommandIds.AskPrintOrMobile,
                isOrdered:        false,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     default,
                completionReward: default
            ),
            // ④-A 서류 출력 (필수+순서)
            //   누락 → kindness-1 / 순서위반 → reliability-1
            //   isAddressMismatch==true 시 eval에서 제외
            new ManualStepEntry(
                commandId:        ManualCommandIds.PrintDocument,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),
            // ④-B 전자 발송 (필수+순서)
            //   누락 → kindness-1 / 순서위반 → reliability-1
            //   isAddressMismatch==true 시 eval에서 제외
            new ManualStepEntry(
                commandId:        ManualCommandIds.SendMobile,
                isOrdered:        true,
                omissionPenalty:  new StepPenalty(kindness: 1),
                orderPenalty:     new StepPenalty(reliability: 1),
                completionReward: new StepReward(performance: perfReward, reliability: 1)
            ),
        };
    }

    // ── 반납 필수 목록 ────────────────────────────────────────────────────
    // 미리 채우지 않음 — AskSubmitId 실행 후 신분증 스폰 시 동적으로 추가
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
            default:                                     return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

    private ResponseResult HandleAskSubmitId()
    {
        if (context.idCardSpawned)
            return WrongOrder(customerMessage: "이미 제출했습니다.");

        RecordAction(ManualCommandIds.AskSubmitId);
        return CorrectResponse(
            customerMessage:   "네, 여기 있습니다.",
            shouldSpawnIdCard: true
        );
    }

    // 시스템 내부 호출 — RecordAction 없음
    // 신분증이 실제로 스폰될 때 필수 반납 목록에 동적 추가
    private ResponseResult HandleSpawnIdCard()
    {
        context.idCardSpawned = true;
        // 민원인이 직접 제출하는 물품 → 필수 반납 목록에 추가
        AddRequiredReturnItem(DeskObjectType.IDCard);
        return CorrectResponse();
    }

    // 신분증 열람 — RequiredSteps 제외, RecordAction 없음 (불필요절차 카운트 방지)
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
        // 선수조건 없음: 신분증 없이도 조회 가능
        context.searchedInputId   = inputId;
        context.searchedByInputId = true;

        // 주소불일치 여부 판정 — SearchRecordByInput 시점에 context에 저장
        if (userDatabase.TryGetRecord(inputId, out UserRecordData record))
            context.isAddressMismatch = record.hasMovedAddress;
        else
            context.isAddressMismatch = false;

        RecordAction(ManualCommandIds.SearchRecordByInput);

        return CorrectResponse(
            customerMessage:          "",
            shouldRefreshMonitorData: true
        );
    }

    private ResponseResult HandleAskPrintOrMobile()
    {
        // 중복 질문 — Queue에 다시 쌓아 불필요 절차로 평가
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
}
