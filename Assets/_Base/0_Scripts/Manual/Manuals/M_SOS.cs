using UnityEngine;

/// <summary>
/// 진상 퇴치 메뉴얼 - SOS
///
/// 발동 시:
///   1. 현재 진행 중인 응대를 평가 없이 강제 종료한다.
///   2. 스트레스 -5, 성과 -2, 친절도 -5를 적용하고 StatChangeEvent에 기록한다.
///
/// 강제 종료 처리는 ServiceDeskManager.ExecuteAntiNuisanceManual()이 담당하고,
/// 이 클래스는 스탯 변화량 정의와 적용만 책임진다.
/// </summary>
public class M_SOS : AntiNuisanceManual
{
    private const string TAG = "[M_SOS]";

    // ── 스탯 변화량 ───────────────────────────────────────────────────────
    // 음수 = 감소 (스트레스는 감소가 이득, 성과/친절도는 감소가 손해)
    private const int   PerformanceDelta = -2;
    private const float StressDelta      = -5f;
    private const float KindnessDelta    = -5f;
    private const float ReliabilityDelta =  0f;

    // ── AntiNuisanceManual 구현 ───────────────────────────────────────────
    public override string CommandId  => AntiNuisanceManualIds.SOS;
    public override string GetTitle() => "진상퇴치 - SOS";

    /// <summary>
    /// SOS 발동.
    /// 강제 종료 처리는 ServiceDeskManager가 먼저 수행하고,
    /// 이 메서드에서 스탯 변화를 적용한다.
    /// </summary>
    public override void Activate(
        PlayerBase     playerBase,
        WorkDayManager workDayManager,
        bool           hasActiveCustomer)
    {
        if (playerBase == null)
        {
            Debug.LogWarning(TAG + " PlayerBase가 null입니다.");
            return;
        }
        // 스탯 즉시 적용
        playerBase.AddPerformance(PerformanceDelta);
        playerBase.AddStat(Stat.Stress,   Mathf.RoundToInt(StressDelta));
        playerBase.AddStat(Stat.Kindness, Mathf.RoundToInt(KindnessDelta));


        Debug.Log(TAG + $" 발동 — 민원응대 중:{hasActiveCustomer} / " +
                  $"P:{PerformanceDelta} S:{StressDelta} K:{KindnessDelta}");

        // StatChangeEvent 기록 (정산 UI 반영)
        EnqueueStatChange(
            workDayManager,
            performanceDelta: PerformanceDelta,
            stressDelta:      StressDelta,
            kindnessDelta:    KindnessDelta,
            reliabilityDelta: ReliabilityDelta
        );
    }
}
