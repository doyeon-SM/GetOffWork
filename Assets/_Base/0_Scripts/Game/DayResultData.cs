using UnityEngine;

/// <summary>
/// 하루 정산에 필요한 스탯 스냅샷 + 변화량 데이터.
/// WorkDayManager가 하루 시작 시 스냅샷을 찍고,
/// FinishDay() 직전에 변화량을 계산해 UIDayResultView에 넘긴다.
/// </summary>
[System.Serializable]
public class DayResultData
{
    // ── 하루 시작 스냅샷 ──────────────────────────────────────────────────
    public int   startPerformance;
    public float startStress;       // 0~1
    public float startKindness;     // 0~1
    public float startReliability;  // 0~1
    public int   startPay;

    // ── 하루 종료 스냅샷 ──────────────────────────────────────────────────
    public int   endPerformance;
    public float endStress;
    public float endKindness;
    public float endReliability;
    public int   endPay;

    // ── 승진 조건 ─────────────────────────────────────────────────────────
    public int maxPerformance;   // GetMaxPerformance() 값
    public int goalPerformance;  // 하루 목표 성과

    // ── 계산 프로퍼티 ─────────────────────────────────────────────────────
    public int   PerformanceDelta  => endPerformance - startPerformance;
    public float StressDelta       => endStress      - startStress;
    public float KindnessDelta     => endKindness    - startKindness;
    public float ReliabilityDelta  => endReliability - startReliability;

    /// <summary>일급 = 성과 증가량 * 10 (음수면 0)</summary>
    public int DailyWage => Mathf.Max(0, PerformanceDelta * 10);
}
