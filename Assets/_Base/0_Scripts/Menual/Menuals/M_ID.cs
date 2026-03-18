using System.Collections.Generic;

public class M_ID : Manual
{
    private enum IDStep
    {
        SubmitIDCard,
        CheckApplicantType,
        CheckSelfIdentity,
        CheckProxyIdentity,
        AskDeliveryType,
        HandlePrint,
        HandleMobile,
        Finish
    }

    private IDStep currentStep;

    public override void Initialize(ComplaintContext newContext)
    {
        base.Initialize(newContext);
        currentStep = IDStep.SubmitIDCard;
    }

    protected override void BuildQuestionList()
    {
        questionList = new List<QuestionData>
        {
            new QuestionData("submit_id", "신분증 제출해주세요."),
            new QuestionData("ask_proxy", "대리인이신가요?"),
            new QuestionData("check_photo", "사진 확인하겠습니다."),
            new QuestionData("check_idinfo", "ID 확인하겠습니다."),
            new QuestionData("check_address", "주소 확인하겠습니다."),
            new QuestionData("ask_print", "인쇄해드릴까요?"),
            new QuestionData("ask_mobile", "모바일로 전송해드릴까요?"),
            new QuestionData("input_phone", "전화번호를 입력해주세요."),
            new QuestionData("input_email", "이메일을 입력해주세요."),
            new QuestionData("retry_submit", "다시 제출해주세요.")
        };
    }

    public override string GetManualTitle()
    {
        return "FULLID 발급 메뉴얼";
    }

    public override ResponseResult AskQuestion(string questionId)
    {
        if (isCompleted)
            return WrongResponse("이미 응대가 완료된 민원입니다.");

        switch (currentStep)
        {
            case IDStep.SubmitIDCard:
                return HandleSubmitIDCard(questionId);

            case IDStep.CheckApplicantType:
                return HandleApplicantType(questionId);

            case IDStep.CheckSelfIdentity:
                return HandleSelfIdentity(questionId);

            case IDStep.CheckProxyIdentity:
                return HandleProxyIdentity(questionId);

            case IDStep.AskDeliveryType:
                return HandleDeliveryType(questionId);

            case IDStep.HandlePrint:
                return HandlePrint(questionId);

            case IDStep.HandleMobile:
                return HandleMobile(questionId);

            case IDStep.Finish:
                return CorrectResponse("응대가 이미 종료되었습니다.", true);
        }

        return WrongResponse("알 수 없는 단계입니다.");
    }

    private ResponseResult HandleSubmitIDCard(string questionId)
    {
        if (questionId != "submit_id")
            return WrongResponse("ID카드 미확인: 신분증 확인이 먼저 필요합니다.", performancePenalty: -3, stressIncrease: 1);

        context.idCardSubmitted = true;
        currentStep = IDStep.CheckApplicantType;
        return CorrectResponse("신분증 제출 확인 완료");
    }

    private ResponseResult HandleApplicantType(string questionId)
    {
        if (questionId != "ask_proxy")
            return WrongResponse("잘못된 질문: 본인/대리 여부 확인이 필요합니다.", kindnessPenalty: -1, stressIncrease: 1);

        if (context.applicantType == ComplaintContext.ApplicantType.Self)
            currentStep = IDStep.CheckSelfIdentity;
        else
            currentStep = IDStep.CheckProxyIdentity;

        return CorrectResponse("신청 유형 확인 완료");
    }

    private ResponseResult HandleSelfIdentity(string questionId)
    {
        if (questionId == "check_photo")
        {
            context.selfPhotoChecked = true;
            return CorrectResponse("본인 사진 확인 완료");
        }

        if (questionId == "check_idinfo")
        {
            if (!context.selfPhotoChecked)
                return WrongResponse("사진 미확인: 사진 확인이 먼저 필요합니다.", performancePenalty: -1, stressIncrease: 1);

            context.selfIdChecked = true;
            return CorrectResponse("본인 ID 확인 완료");
        }

        if (questionId == "check_address")
        {
            if (!context.selfIdChecked)
                return WrongResponse("ID카드 미확인: ID 확인이 먼저 필요합니다.", performancePenalty: -1, stressIncrease: 1);

            context.selfAddressChecked = true;
            currentStep = IDStep.AskDeliveryType;
            return CorrectResponse("본인 주소 확인 완료");
        }

        return WrongResponse("잘못된 질문: 본인 확인 절차가 아닙니다.", kindnessPenalty: -1, stressIncrease: 1);
    }

    private ResponseResult HandleProxyIdentity(string questionId)
    {
        if (questionId == "check_photo")
        {
            if (!context.proxyPhotoChecked)
            {
                context.proxyPhotoChecked = true;
                return CorrectResponse("대리인 사진 확인 완료");
            }

            context.targetPhotoChecked = true;
            return CorrectResponse("발급대상자 사진 확인 완료");
        }

        if (questionId == "check_idinfo")
        {
            if (!context.proxyPhotoChecked)
                return WrongResponse("대리인 미확인: 대리인 사진 확인이 먼저입니다.", performancePenalty: -1, stressIncrease: 1);

            if (!context.proxyIdChecked)
            {
                context.proxyIdChecked = true;
                return CorrectResponse("대리인 ID 확인 완료");
            }

            if (!context.targetPhotoChecked)
                return WrongResponse("발급대상자 사진 미확인", performancePenalty: -1, stressIncrease: 1);

            context.targetIdChecked = true;
            return CorrectResponse("발급대상자 ID 확인 완료");
        }

        if (questionId == "check_address")
        {
            if (!context.proxyIdChecked)
                return WrongResponse("대리인 ID 확인이 먼저 필요합니다.", performancePenalty: -1, stressIncrease: 1);

            if (!context.proxyAddressChecked)
            {
                context.proxyAddressChecked = true;
                return CorrectResponse("대리인 주소 확인 완료");
            }

            if (!context.targetIdChecked)
                return WrongResponse("발급대상자 ID 확인이 먼저 필요합니다.", performancePenalty: -1, stressIncrease: 1);

            context.targetAddressChecked = true;
            currentStep = IDStep.AskDeliveryType;
            return CorrectResponse("발급대상자 주소 확인 완료");
        }

        return WrongResponse("잘못된 질문: 대리발급 절차가 아닙니다.", kindnessPenalty: -1, stressIncrease: 1);
    }

    private ResponseResult HandleDeliveryType(string questionId)
    {
        if (questionId == "ask_print")
        {
            context.deliveryType = ComplaintContext.DeliveryType.Print;
            currentStep = IDStep.HandlePrint;
            return CorrectResponse("인쇄 발급으로 진행합니다.");
        }

        if (questionId == "ask_mobile")
        {
            context.deliveryType = ComplaintContext.DeliveryType.Mobile;
            currentStep = IDStep.HandleMobile;
            return CorrectResponse("모바일 발급으로 진행합니다.");
        }

        return WrongResponse("발급방법 미확인: 인쇄 또는 모바일 여부를 확인해야 합니다.", kindnessPenalty: -1, stressIncrease: 1);
    }

    private ResponseResult HandlePrint(string questionId)
    {
        if (questionId != "input_email")
            return WrongResponse("잘못된 처리: 인쇄 발급에는 이메일 입력이 필요합니다.", performancePenalty: -2, kindnessPenalty: -1, stressIncrease: 1);

        context.emailReceived = true;
        currentStep = IDStep.Finish;

        int reward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;
        return CorrectResponse("발급이 정상적으로 완료되었습니다.", true, performanceReward: reward, reliabilityReward: 1);
    }

    private ResponseResult HandleMobile(string questionId)
    {
        if (questionId != "input_phone")
            return WrongResponse("잘못된 처리: 모바일 발급에는 전화번호 입력이 필요합니다.", performancePenalty: -2, kindnessPenalty: -1, stressIncrease: 1);

        context.phoneNumberReceived = true;
        currentStep = IDStep.Finish;

        int reward = context.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;
        return CorrectResponse("발급이 정상적으로 완료되었습니다.", true, performanceReward: reward, reliabilityReward: 1);
    }
}