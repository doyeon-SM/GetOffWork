using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>스탯 변화 이벤트의 출처 구분</summary>
public enum StatChangeSource
{
    ServiceSuccess,        // 응대 성공
    ServiceFail,           // 응대 실패
    Lunch,                 // 점심시간
    ItemUse,               // 아이템 사용
    CustomerCancelledAtClose, // 영업 종료 시 대기 민원인 강제 취소 패널티
}

/// <summary>
/// 한 번의 스탯 변화 이벤트를 담는 구조체.
/// 같은 응대 내에서 누적된 패널티는 합산해 하나의 이벤트로 Enqueue한다.
/// </summary>
[System.Serializable]
public struct StatChangeEvent
{
    public StatChangeSource source;
    public int performanceDelta;  // 정수 (성과 단위)
    public int stressDelta;       // 정수 % 단위 (예: +3 = +3%)
    public int kindnessDelta;     // 정수 % 단위
    public int reliabilityDelta;  // 정수 % 단위
    public int payDelta;          // 원 단위
}

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

    // ── 하루 과정 큐 ────────────────────────────────────────────────────
    /// <summary>
    /// 하루 동안 발생한 스탯 변화 이벤트 목록.
    /// 같은 응대 내 누적 패널티는 합산해 하나의 이벤트로 저장된다.
    /// </summary>
    public Queue<StatChangeEvent> statChangeProgress = new Queue<StatChangeEvent>();

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
