using System.Collections.Generic;

public class M_FullID : Manual
{
    private readonly UserRecordDatabase userDatabase;

    public M_FullID(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    protected override void BuildCommandList()
    {
        commandList = new List<QuestionData>
        {
            new QuestionData(ManualCommandIds.AskSubmitId, "신분증 제시"),
            new QuestionData(ManualCommandIds.AskPrintOrMobile, "인쇄 여부 묻기"),
            new QuestionData(ManualCommandIds.RejectAddressMismatch, "주소 불일치 반려", QuestionData.CommandVisualType.ActionButton)
        };
    }

    public override string GetManualTitle()
    {
        return "FULLID 본인/대리 발급 메뉴얼";
    }

    public override ResponseResult Execute(string commandId, string payload = null)
    {
        if (isCompleted || context.completed)
            return WrongResponse("이미 응대가 완료되었습니다.");

        switch (commandId)
        {
            case ManualCommandIds.AskSubmitId:
                return HandleAskSubmitId();

            case ManualCommandIds.SpawnIdCard:
                return HandleSpawnIdCard();

            case ManualCommandIds.OpenIdCardDetail:
                return HandleOpenIdCardDetail();

            case ManualCommandIds.OpenMonitor:
                return HandleOpenMonitor();

            case ManualCommandIds.SearchRecordByInput:
                return HandleSearchRecordByInput(payload);

            case ManualCommandIds.CompareCardAndMonitor:
                return HandleCompareCardAndMonitor();

            case ManualCommandIds.AskPrintOrMobile:
                return HandleAskPrintOrMobile();

            case ManualCommandIds.SelectPrint:
                return HandleSelectPrint();

            case ManualCommandIds.SelectMobile:
                return HandleSelectMobile();

            case ManualCommandIds.PrintDocument:
                return HandlePrintDocument();

            case ManualCommandIds.SendMobile:
                return HandleSendMobile();

            case ManualCommandIds.RejectAddressMismatch:
                return HandleRejectAddressMismatch();

            default:
                return WrongResponse("알 수 없는 명령입니다.");
        }
    }

    private ResponseResult HandleAskSubmitId()
    {
        context.lastPlayerMessage = "신분증을 보여주세요.";
        context.lastCustomerMessage = "네, 여기 있습니다.";

        return CorrectResponse(
            playerMessage: context.lastPlayerMessage,
            customerMessage: context.lastCustomerMessage,
            shouldSpawnIdCard: true
        );
    }

    private ResponseResult HandleSpawnIdCard()
    {
        context.idCardSpawned = true;
        return CorrectResponse("신분증이 제출되었습니다.");
    }

    private ResponseResult HandleOpenIdCardDetail()
    {
        if (!context.idCardSpawned)
            return WrongResponse("신분증이 아직 제출되지 않았습니다.");

        context.idCardInspected = true;

        return CorrectResponse(
            playerMessage: "신분증 정보를 확인합니다.",
            shouldOpenIdCardDetail: true
        );
    }

    private ResponseResult HandleOpenMonitor()
    {
        if (!context.idCardSpawned)
            return WrongResponse("먼저 신분증을 제출받아야 합니다.");

        context.monitorOpened = true;
        return CorrectResponse(
            playerMessage: "전산 화면을 엽니다.",
            shouldOpenMonitor: true
        );
    }

    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        if (!context.monitorOpened)
            return WrongResponse("먼저 모니터를 열어야 합니다.");

        if (string.IsNullOrWhiteSpace(inputId))
            return WrongResponse("조회할 ID를 입력해야 합니다.");

        context.searchedInputId = inputId;
        context.searchedByInputId = true;

        if (!userDatabase.TryGetRecord(inputId, out _))
        {
            return WrongResponse(
                playerMessage: "해당 ID의 정보가 존재하지 않습니다.",
                stressIncrease: 1
            );
        }

        return CorrectResponse(
            playerMessage: $"ID {inputId} 정보를 조회합니다.",
            shouldRefreshMonitorData: true
        );
    }

    private ResponseResult HandleCompareCardAndMonitor()
    {
        if (!context.idCardInspected)
            return WrongResponse("먼저 신분증 상세를 확인해야 합니다.");

        if (!context.searchedByInputId)
            return WrongResponse("먼저 전산 조회를 해야 합니다.");

        string targetId = context.EffectiveTargetRecordId;

        if (!userDatabase.TryGetRecord(targetId, out var cardRecord))
            return WrongResponse("카드 기준 정보 조회에 실패했습니다.");

        if (!userDatabase.TryGetRecord(context.searchedInputId, out var monitorRecord))
            return WrongResponse("전산 기준 정보 조회에 실패했습니다.");

        context.recordCompared = true;
        context.addressMatched = cardRecord.address == monitorRecord.address;

        if (!context.addressMatched)
        {
            return CorrectResponse(
                playerMessage: "주소 정보가 일치하지 않습니다.",
                customerMessage: "아, 그런가요?",
                shouldRefreshMonitorData: true
            );
        }

        return CorrectResponse(
            playerMessage: "주소와 신원 정보가 일치합니다."
        );
    }

    private ResponseResult HandleAskPrintOrMobile()
    {
        if (!context.recordCompared)
            return WrongResponse("먼저 신분증과 전산 정보를 비교해야 합니다.");

        if (!context.addressMatched)
            return WrongResponse("주소 불일치 상태입니다. 반려 또는 보완 안내가 필요합니다.");

        context.deliveryAsked = true;

        return CorrectResponse(
            playerMessage: "인쇄해드릴까요? 전송해드릴까요?",
            customerMessage: context.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile
                ? "전송 부탁드립니다."
                : "인쇄 부탁드려요."
        );
    }

    private ResponseResult HandleSelectPrint()
    {
        if (!context.deliveryAsked)
            return WrongResponse("먼저 전달 방식을 물어봐야 합니다.");

        context.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
        return CorrectResponse("인쇄 발급으로 진행합니다.");
    }

    private ResponseResult HandleSelectMobile()
    {
        if (!context.deliveryAsked)
            return WrongResponse("먼저 전달 방식을 물어봐야 합니다.");

        context.requestedDeliveryType = ComplaintContext.DeliveryType.Mobile;
        return CorrectResponse("모바일 발급으로 진행합니다.");
    }

    private ResponseResult HandlePrintDocument()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Print)
            return WrongResponse("현재 인쇄 발급으로 선택되어 있지 않습니다.");

        if (!context.addressMatched)
            return WrongResponse("주소 확인이 완료되지 않았습니다.");

        context.documentPrinted = true;
        context.completed = true;

        int reward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;

        return CorrectResponse(
            playerMessage: "출력이 완료되었습니다.",
            customerMessage: "수고하세요.",
            completeNow: true,
            performanceReward: reward,
            reliabilityReward: 1
        );
    }

    private ResponseResult HandleSendMobile()
    {
        if (context.requestedDeliveryType != ComplaintContext.DeliveryType.Mobile)
            return WrongResponse("현재 모바일 발급으로 선택되어 있지 않습니다.");

        if (!context.addressMatched)
            return WrongResponse("주소 확인이 완료되지 않았습니다.");

        context.documentSent = true;
        context.completed = true;

        int reward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;

        return CorrectResponse(
            playerMessage: "모바일 전송이 완료되었습니다.",
            customerMessage: "감사합니다.",
            completeNow: true,
            performanceReward: reward,
            reliabilityReward: 1
        );
    }

    private ResponseResult HandleRejectAddressMismatch()
    {
        if (!context.recordCompared)
            return WrongResponse("비교 완료 후에만 반려할 수 있습니다.");

        if (context.addressMatched)
            return WrongResponse("주소가 일치하므로 주소 불일치 반려 대상이 아닙니다.");

        context.rejected = true;
        context.completed = true;

        return CorrectResponse(
            playerMessage: "주소이전 먼저 하고 오세요.",
            customerMessage: "알겠습니다.",
            completeNow: true
        );
    }
}