using UnityEngine;

/// <summary>
/// 진상 퇴치 메뉴얼의 추상 기반 클래스.
///
/// 기존 Manual(민원 절차 메뉴얼)과 완전히 독립적인 구조로,
/// 현재 진행 중인 민원과 무관하게 발동된다.
///
/// - Activate()   : 메뉴얼 발동. ServiceDeskManager가 호출한다.
/// - GetTitle()   : UI에 표시할 메뉴얼 이름.
/// - GetCommandId : 이 메뉴얼을 식별하는 커맨드 ID (AntiNuisanceManualIds 상수).
///
/// 스탯 변화는 반드시 WorkDayManager.EnqueueStatChangeEvent를 통해
/// StatChangeEvent로 기록해야 UI 정산에 반영된다.
/// </summary>
public abstract class AntiNuisanceManual
{
    private const string TAG = "[AntiNuisanceManual]";

    /// <summary>이 메뉴얼의 커맨드 ID. AntiNuisanceManualIds 상수를 반환한다.</summary>
    public abstract string CommandId { get; }

    /// <summary>UI에 표시할 메뉴얼 이름.</summary>
    public abstract string GetTitle();

    /// <summary>
    /// 메뉴얼을 발동한다. ServiceDeskManager.ExecuteAntiNuisanceManual()에서 호출된다.
    /// <param name="playerBase">스탯 변화를 적용할 PlayerBase</param>
    /// <param name="workDayManager">StatChangeEvent를 등록할 WorkDayManager</param>
    /// <param name="hasActiveCustomer">현재 응대 중인 민원인이 있는지 여부</param>
    /// </summary>
    public abstract void Activate(
        PlayerBase     playerBase,
        WorkDayManager workDayManager,
        bool           hasActiveCustomer);

    /// <summary>
    /// StatChangeEvent를 생성해서 WorkDayManager에 등록하는 헬퍼.
    /// 모든 자식 클래스에서 공통으로 사용한다.
    /// </summary>
    protected void EnqueueStatChange(
        WorkDayManager workDayManager,
        int performanceDelta,
        int stressDelta,       // 정수 % 단위 (예: 3 = +3%)
        int kindnessDelta,     // 정수 % 단위
        int reliabilityDelta)  // 정수 % 단위
    {
        if (workDayManager == null) return;

        var evt = new StatChangeEvent
        {
            source           = StatChangeSource.ServiceFail,
            performanceDelta = performanceDelta,
            stressDelta      = stressDelta,
            kindnessDelta    = kindnessDelta,
            reliabilityDelta = reliabilityDelta,
        };

        workDayManager.EnqueueStatChangeEvent(evt);
        Debug.Log($"{TAG} [{GetTitle()}] StatChangeEvent 등록 — " +
                  $"P:{performanceDelta} S:{stressDelta} K:{kindnessDelta} R:{reliabilityDelta}");
    }
}
