using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 민원이 종료될 때 플레이어 행동 Queue와 메뉴얼 RequiredSteps List를 비교해
/// 성과/스탯 변화량을 계산하는 평가기.
///
/// 평가 기준:
///   정상 완료  : Queue에 해당 commandId가 올바른 순서로 존재 → CompletionReward 적용
///   누락       : RequiredSteps에 있는데 Queue에 없음 → OmissionPenalty 적용
///   순서 위반  : IsOrdered인 단계가 선행 단계보다 먼저 수행됨 → OrderPenalty 적용
///   불필요 절차: Queue에 있는데 RequiredSteps에 없거나, 동일 commandId 중복 수행
///               → kindness -0.1 (kindness는 float, 단 PlayerBase.AddStat이 int이므로
///                  누적 후 정수로 변환해 일괄 적용)
/// </summary>
public static class ManualEvaluator
{
    /// <summary>불필요한 절차 한 건당 감소하는 kindness 양 (float 누적)</summary>
    private const float UnnecessaryKindnessPenaltyPerAction = 0.1f;

    /// <summary>
    /// 메뉴얼 절차를 평가하고 적용할 총 스탯 변화를 반환한다.
    /// ServiceDeskManager가 민원 종료 시 이 결과를 PlayerBase에 적용한다.
    /// </summary>
    public static EvaluationResult Evaluate(
        IReadOnlyList<ManualStepEntry> requiredSteps,
        IReadOnlyCollection<PlayerActionRecord> actionQueue)
    {
        var result = new EvaluationResult();

        // Queue를 commandId 순서 목록으로 변환
        var actionList = new List<PlayerActionRecord>(actionQueue);

        // 각 requiredStep이 Queue에서 발견된 인덱스를 추적
        // (순서 검증용: i번 step의 Queue 인덱스 < i+1번 step의 Queue 인덱스 여야 한다)
        var stepQueueIndex = new int[requiredSteps.Count];
        for (int i = 0; i < stepQueueIndex.Length; i++)
            stepQueueIndex[i] = -1; // -1 = 미발견

        // ── 1단계: 각 RequiredStep이 Queue에서 첫 번째로 등장하는 위치를 찾는다 ──
        for (int si = 0; si < requiredSteps.Count; si++)
        {
            string targetId = requiredSteps[si].CommandId;
            for (int ai = 0; ai < actionList.Count; ai++)
            {
                if (actionList[ai].CommandId == targetId)
                {
                    stepQueueIndex[si] = ai;
                    break;
                }
            }
        }

        // ── 2단계: 각 RequiredStep을 평가 ──
        for (int si = 0; si < requiredSteps.Count; si++)
        {
            var step = requiredSteps[si];
            int foundAt = stepQueueIndex[si];

            // 누락
            if (foundAt == -1)
            {
                ApplyPenalty(ref result, step.OmissionPenalty);
                if (step.IsOrdered)
                    Debug.Log($"[Evaluator] 누락: {step.CommandId} → OmissionPenalty 적용");
                continue;
            }

            // 순서 위반 검사 (IsOrdered인 단계만)
            bool orderViolated = false;
            if (step.IsOrdered && si > 0)
            {
                // 선행 단계 중 IsOrdered인 가장 가까운 단계의 Queue 인덱스보다
                // 현재 단계 인덱스가 작으면 순서 위반
                for (int prev = si - 1; prev >= 0; prev--)
                {
                    if (!requiredSteps[prev].IsOrdered) continue;
                    int prevFound = stepQueueIndex[prev];
                    if (prevFound == -1) continue; // 선행이 누락된 경우 순서 비교 불가

                    if (foundAt < prevFound)
                    {
                        orderViolated = true;
                        ApplyPenalty(ref result, step.OrderPenalty);
                        Debug.Log($"[Evaluator] 순서 위반: {step.CommandId} (qi={foundAt}) < 선행 {requiredSteps[prev].CommandId} (qi={prevFound})");
                    }
                    break; // 가장 가까운 선행 기준만 검사
                }
            }

            // 정상 완료 (누락도 순서 위반도 아님)
            if (!orderViolated)
            {
                ApplyReward(ref result, step.CompletionReward);
            }
        }

        // ── 3단계: 불필요한 절차 집계 ──
        // 필요한 commandId 집합 구성
        var requiredIds = new HashSet<string>();
        foreach (var step in requiredSteps)
            requiredIds.Add(step.CommandId);

        // Queue 내 commandId 등장 횟수 집계
        var actionCount = new Dictionary<string, int>();
        foreach (var action in actionList)
        {
            if (!actionCount.ContainsKey(action.CommandId))
                actionCount[action.CommandId] = 0;
            actionCount[action.CommandId]++;
        }

        int unnecessaryCount = 0;

        foreach (var kv in actionCount)
        {
            string cmdId = kv.Key;
            int count    = kv.Value;

            if (!requiredIds.Contains(cmdId))
            {
                // 메뉴얼에 없는 행동 전부 불필요
                unnecessaryCount += count;
                Debug.Log($"[Evaluator] 불필요(미정의): {cmdId} x{count}");
            }
            else if (count > 1)
            {
                // 필요한 행동이지만 중복 수행 — 첫 번째 제외 나머지가 불필요
                unnecessaryCount += count - 1;
                Debug.Log($"[Evaluator] 불필요(중복): {cmdId} x{count - 1}");
            }
        }

        // kindness 패널티 누적 후 반올림 적용
        if (unnecessaryCount > 0)
        {
            float rawKindness = unnecessaryCount * UnnecessaryKindnessPenaltyPerAction;
            // 내림 처리: 0.1 * 9 = 0.9 → -0, 0.1 * 10 = 1.0 → -1
            result.KindnessDelta -= Mathf.RoundToInt(rawKindness);
            result.UnnecessaryActionCount = unnecessaryCount;
            Debug.Log($"[Evaluator] 불필요 행동 {unnecessaryCount}건 → kindness {-rawKindness:F1}");
        }

        return result;
    }

    private static void ApplyPenalty(ref EvaluationResult result, StepPenalty penalty)
    {
        result.PerformanceDelta -= penalty.Performance;
        result.KindnessDelta    -= penalty.Kindness;
        result.StressDelta      += penalty.Stress;
        result.ReliabilityDelta -= penalty.Reliability;
        result.PayDelta         -= penalty.Pay;
    }

    private static void ApplyReward(ref EvaluationResult result, StepReward reward)
    {
        result.PerformanceDelta += reward.Performance;
        result.KindnessDelta    += reward.Kindness;
        result.ReliabilityDelta += reward.Reliability;
        result.PayDelta         += reward.Pay;
    }
}

/// <summary>
/// ManualEvaluator.Evaluate의 반환값.
/// ServiceDeskManager가 이 값을 PlayerBase에 일괄 적용한다.
/// </summary>
public struct EvaluationResult
{
    public int PerformanceDelta;
    public int KindnessDelta;
    public int StressDelta;
    public int ReliabilityDelta;
    public int PayDelta;
    public int UnnecessaryActionCount;

    public bool IsAllZero =>
        PerformanceDelta    == 0 &&
        KindnessDelta       == 0 &&
        StressDelta         == 0 &&
        ReliabilityDelta    == 0 &&
        PayDelta            == 0;

    public override string ToString()
    {
        return $"Perf:{PerformanceDelta:+0;-0} Kind:{KindnessDelta:+0;-0} " +
               $"Stress:{StressDelta:+0;-0} Rel:{ReliabilityDelta:+0;-0} " +
               $"Pay:{PayDelta:+0;-0} Unnecessary:{UnnecessaryActionCount}";
    }
}
