using System.Collections.Generic;

public abstract class Manual
{
    protected ComplaintContext context;
    protected List<QuestionData> commandList = new();

    protected bool isCompleted;
    protected int wrongCount;

    public IReadOnlyList<QuestionData> CommandList => commandList;
    public bool IsCompleted => isCompleted;
    public int WrongCount => wrongCount;

    public virtual void Initialize(ComplaintContext newContext)
    {
        context = newContext;
        isCompleted = false;
        wrongCount = 0;
        commandList.Clear();
        BuildCommandList();
    }

    protected abstract void BuildCommandList();

    public abstract ResponseResult Execute(string commandId, string payload = null);

    protected ResponseResult WrongResponse(
        string playerMessage,
        string customerMessage = "",
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
            playerMessage: playerMessage,
            customerMessage: customerMessage,
            performanceDelta: performancePenalty,
            kindnessDelta: kindnessPenalty,
            stressDelta: stressIncrease,
            reliabilityDelta: reliabilityPenalty,
            payDelta: payDelta
        );
    }

    protected ResponseResult CorrectResponse(
        string playerMessage,
        string customerMessage = "",
        bool completeNow = false,
        bool shouldSpawnIdCard = false,
        bool shouldOpenIdCardDetail = false,
        bool shouldOpenMonitor = false,
        bool shouldRefreshMonitorData = false,
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
            playerMessage: playerMessage,
            customerMessage: customerMessage,
            shouldSpawnIdCard: shouldSpawnIdCard,
            shouldOpenIdCardDetail: shouldOpenIdCardDetail,
            shouldOpenMonitor: shouldOpenMonitor,
            shouldRefreshMonitorData: shouldRefreshMonitorData,
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