using System;

/// <summary>
/// 플레이어가 실제로 수행한 행동 하나를 기록하는 구조체.
/// Manual이 Execute를 처리하고 결과가 도출될 때 Queue에 쌓인다.
/// </summary>
[Serializable]
public struct PlayerActionRecord
{
    /// <summary>수행한 commandId (ManualCommandIds 참조)</summary>
    public string CommandId;

    /// <summary>행동이 기록된 시각 (민원 시작 기준 경과 시간, 초)</summary>
    public float TimeStamp;

    public PlayerActionRecord(string commandId, float timeStamp)
    {
        CommandId = commandId;
        TimeStamp = timeStamp;
    }

    public override string ToString()
    {
        return $"[{TimeStamp:F1}s] {CommandId}";
    }
}
