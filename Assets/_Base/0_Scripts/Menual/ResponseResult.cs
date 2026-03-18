using System;

[Serializable]
public struct ResponseResult
{
    public bool IsValid;
    public bool IsCompleted;
    public string Message;

    public int PerformanceDelta;
    public int KindnessDelta;
    public int StressDelta;
    public int ReliabilityDelta;
    public int PayDelta;

    public static ResponseResult Create(
        bool isValid,
        bool isCompleted,
        string message,
        int performanceDelta = 0,
        int kindnessDelta = 0,
        int stressDelta = 0,
        int reliabilityDelta = 0,
        int payDelta = 0)
    {
        return new ResponseResult
        {
            IsValid = isValid,
            IsCompleted = isCompleted,
            Message = message,
            PerformanceDelta = performanceDelta,
            KindnessDelta = kindnessDelta,
            StressDelta = stressDelta,
            ReliabilityDelta = reliabilityDelta,
            PayDelta = payDelta
        };
    }
}