using System;

[Serializable]
public struct ResponseResult
{
    public bool   IsValid;
    public bool   IsCompleted;
    public string PlayerMessage;
    public string CustomerMessage;

    public bool ShouldSpawnIdCard;
    public bool ShouldSpawnProxyIdCard;   // 대리인 신분증 스폰
    public bool ShouldOpenIdCardDetail;
    public bool ShouldOpenMonitor;
    public bool ShouldRefreshMonitorData;

    public int PerformanceDelta;
    public int KindnessDelta;
    public int StressDelta;
    public int ReliabilityDelta;
    public int PayDelta;

    public static ResponseResult Create(
        bool   isValid,
        bool   isCompleted,
        string playerMessage          = "",
        string customerMessage        = "",
        bool   shouldSpawnIdCard      = false,
        bool   shouldSpawnProxyIdCard = false,
        bool   shouldOpenIdCardDetail = false,
        bool   shouldOpenMonitor      = false,
        bool   shouldRefreshMonitorData = false,
        int    performanceDelta       = 0,
        int    kindnessDelta          = 0,
        int    stressDelta            = 0,
        int    reliabilityDelta       = 0,
        int    payDelta               = 0)
    {
        return new ResponseResult
        {
            IsValid                = isValid,
            IsCompleted            = isCompleted,
            PlayerMessage          = playerMessage,
            CustomerMessage        = customerMessage,
            ShouldSpawnIdCard      = shouldSpawnIdCard,
            ShouldSpawnProxyIdCard = shouldSpawnProxyIdCard,
            ShouldOpenIdCardDetail = shouldOpenIdCardDetail,
            ShouldOpenMonitor      = shouldOpenMonitor,
            ShouldRefreshMonitorData = shouldRefreshMonitorData,
            PerformanceDelta       = performanceDelta,
            KindnessDelta          = kindnessDelta,
            StressDelta            = stressDelta,
            ReliabilityDelta       = reliabilityDelta,
            PayDelta               = payDelta
        };
    }
}
