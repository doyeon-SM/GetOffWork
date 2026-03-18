using System.Collections.Generic;

public abstract class Manual
{
    protected ComplaintContext context;
    protected List<QuestionData> questionList = new List<QuestionData>();

    protected bool isCompleted;
    protected int wrongCount;

    public IReadOnlyList<QuestionData> QuestionList => questionList;
    public bool IsCompleted => isCompleted;
    public int WrongCount => wrongCount;

    public virtual void Initialize(ComplaintContext newContext)
    {
        context = newContext;
        isCompleted = false;
        wrongCount = 0;
        questionList.Clear();
        BuildQuestionList();
    }

    protected abstract void BuildQuestionList();

    public abstract ResponseResult AskQuestion(string questionId);

    protected ResponseResult WrongResponse(
        string message,
        int performancePenalty = 0,
        int kindnessPenalty = 0,
        int stressIncrease = 0,
        int reliabilityPenalty = 0,
        int payDelta = 0)
    {
        wrongCount++;

        return ResponseResult.Create(
            isValid: false,
            isCompleted: false,
            message: message,
            performanceDelta: performancePenalty,
            kindnessDelta: kindnessPenalty,
            stressDelta: stressIncrease,
            reliabilityDelta: reliabilityPenalty,
            payDelta: payDelta
        );
    }

    protected ResponseResult CorrectResponse(
        string message,
        bool completeNow = false,
        int performanceReward = 0,
        int kindnessReward = 0,
        int stressDelta = 0,
        int reliabilityReward = 0,
        int payDelta = 0)
    {
        if (completeNow)
            isCompleted = true;

        return ResponseResult.Create(
            isValid: true,
            isCompleted: completeNow,
            message: message,
            performanceDelta: performanceReward,
            kindnessDelta: kindnessReward,
            stressDelta: stressDelta,
            reliabilityDelta: reliabilityReward,
            payDelta: payDelta
        );
    }

    public virtual string GetManualTitle()
    {
        return "공통 메뉴얼";
    }
}