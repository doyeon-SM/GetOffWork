using UnityEngine;

/// <summary>
/// 응대 종료 시 케이스별 스탯 정산 로직 전담 클래스.
/// ServiceDeskManager에서 분리되어 PlayerBase 스탯 적용과
/// StatChangeEvent 누적만 책임진다.
/// </summary>
public static class ServiceEvaluator
{
    private const string TAG = "[ServiceEvaluator]";

    // ── 진입점 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 응대 종료 시 호출. 공통 패널티 계산 후 케이스를 분기해
    /// StatChangeEvent를 반환한다. isSuccess도 out으로 함께 반환한다.
    /// </summary>
    public static StatChangeEvent Evaluate(
        PlayerBase       playerBase,
        ComplaintContext complaint,
        Manual           manual,
        bool             patienceExpired,
        bool             isRejection,
        out bool         isSuccess)
    {
        bool hasAnyMismatch     = complaint.HasAnyMismatch;
        bool isCompleted        = manual.IsCompleted;
        bool isValidRejection   = isRejection && hasAnyMismatch;
        bool isInvalidRejection = isRejection && !hasAnyMismatch;
        bool isMissedRejection  = !isRejection && hasAnyMismatch && isCompleted;

        // 공통 선처리: 인내심 소진 + 진상 종료 패널티
        var evt = BuildCommonPenalties(playerBase, complaint, patienceExpired);

        // 케이스 분기
        if (isValidRejection)
        {
            isSuccess = EvaluateValidRejection(playerBase, complaint, manual, ref evt);
        }
        else if (isMissedRejection)
        {
            EvaluateMissedRejection(playerBase, manual, ref evt);
            isSuccess = false;
        }
        else
        {
            isSuccess = EvaluateNormalCase(playerBase, complaint, manual, ref evt, isInvalidRejection, isCompleted, isRejection);
        }

        return evt;
    }

    // ── 공통 선처리 ───────────────────────────────────────────────────────

    /// <summary>인내심 소진 패널티 + 진상 종료 추가결과(onFinishResult)를 누적한 초기 이벤트를 반환한다.</summary>
    private static StatChangeEvent BuildCommonPenalties(
        PlayerBase       playerBase,
        ComplaintContext complaint,
        bool             patienceExpired)
    {
        var evt = new StatChangeEvent { source = StatChangeSource.ServiceFail };

        if (patienceExpired)
        {
            playerBase.AddStat(Stat.Stress, 2);
            evt.stressDelta += 2;
            Debug.Log(TAG + " 인내심 소진 → Stress+2");
        }

        var nuisanceSO = ServiceDataManager.Instance?.NuisanceSettings;
        if (nuisanceSO != null && complaint.nuisanceType != ComplaintContext.NuisanceType.None)
        {
            var nEntry = nuisanceSO.GetEntry(complaint.nuisanceType);
            if (!nEntry.onFinishResult.IsEmpty)
            {
                ApplyNuisanceResult(playerBase, nEntry.onFinishResult);
                evt.stressDelta      += nEntry.onFinishResult.stress;
                evt.kindnessDelta    += nEntry.onFinishResult.kindness;
                evt.reliabilityDelta += nEntry.onFinishResult.reliability;
                evt.performanceDelta += nEntry.onFinishResult.performance;
                Debug.Log(TAG + " [NuisanceResult/finish] type:" + complaint.nuisanceType);
            }
        }

        return evt;
    }

    // ── Case 1 ────────────────────────────────────────────────────────────

    /// <summary>불일치 + 정상 반려. 성공 여부(true)를 반환한다.</summary>
    private static bool EvaluateValidRejection(
        PlayerBase       playerBase,
        ComplaintContext complaint,
        Manual           manual,
        ref StatChangeEvent evt)
    {
        // AddressChange case3: 주소 변경 후(isAddressChangeCommitted=true) 반려 허용되지 않음 → 반려실패
        if (complaint.complaintType == ComplaintContext.ComplaintType.AddressChange
            && complaint.isAddressChangeCommitted)
        {
            Debug.Log(TAG + " [AddressChange/case3] 주소 변경 후 반려 시도 → 반려실패 처리");
            var penaltyData = GetManualData(manual);
            if (penaltyData != null && !penaltyData.abnormalRejectionPenalty.IsEmpty)
            {
                ApplyPenaltyFromSO(playerBase, penaltyData.abnormalRejectionPenalty);
                evt.performanceDelta -= penaltyData.abnormalRejectionPenalty.Performance;
                evt.kindnessDelta    -= penaltyData.abnormalRejectionPenalty.Kindness;
                evt.stressDelta      += penaltyData.abnormalRejectionPenalty.Stress;
                evt.reliabilityDelta -= penaltyData.abnormalRejectionPenalty.Reliability;
                evt.payDelta         -= penaltyData.abnormalRejectionPenalty.Pay;
            }
            else
            {
                playerBase.AddPerformance(-2);
                playerBase.AddStat(Stat.Reliability, -1);
                evt.performanceDelta -= 2;
                evt.reliabilityDelta -= 1;
            }
            return false; // 실패
        }

        // 일반 정상 반려 (case2: AddressChange 변경 전 또는 FullID 불일치)
        string mismatchLog = $"addr={complaint.isAddressMismatch} id={complaint.isIdMismatch} portrait={complaint.isPortraitMismatch}";
        Debug.Log(TAG + " 정상 반려(불일치) [" + mismatchLog + "] — 절차 평가 무시");

        var soData = GetManualData(manual);
        if (soData != null && !soData.completionReward.IsEmpty)
        {
            ApplyReward(playerBase, soData.completionReward);
            evt.performanceDelta += soData.completionReward.Performance;
            evt.kindnessDelta    += soData.completionReward.Kindness;
            evt.reliabilityDelta += soData.completionReward.Reliability;
            evt.payDelta         += soData.completionReward.Pay;
            Debug.Log(TAG + " completionReward 적용");
        }
        else
        {
            int perfReward = complaint.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;
            playerBase.AddPerformance(perfReward);
            evt.performanceDelta += perfReward;
            Debug.Log(TAG + " Performance+" + perfReward + " [폴백]");
        }

        playerBase.AddStat(Stat.Stress, 1);
        evt.stressDelta += 1;
        return true;
    }

    // ── Case 2 ────────────────────────────────────────────────────────────

    /// <summary>불일치인데 반려하지 않고 처리 완료한 경우.</summary>
    private static void EvaluateMissedRejection(
        PlayerBase playerBase,
        Manual     manual,
        ref StatChangeEvent evt)
    {
        Debug.Log(TAG + " 반려사항 놓침(불일치인데 정상응대 완료)");

        var soData = GetManualData(manual);
        if (soData != null && !soData.abnormalRejectionPenalty.IsEmpty)
        {
            ApplyPenaltyFromSO(playerBase, soData.abnormalRejectionPenalty);
            evt.performanceDelta -= soData.abnormalRejectionPenalty.Performance;
            evt.kindnessDelta    -= soData.abnormalRejectionPenalty.Kindness;
            evt.stressDelta      += soData.abnormalRejectionPenalty.Stress;
            evt.reliabilityDelta -= soData.abnormalRejectionPenalty.Reliability;
            evt.payDelta         -= soData.abnormalRejectionPenalty.Pay;
            Debug.Log(TAG + " [반려사항 놓침] → abnormalRejectionPenalty 적용");
        }
        else
        {
            playerBase.AddPerformance(-2);
            playerBase.AddStat(Stat.Reliability, -1);
            evt.performanceDelta -= 2;
            evt.reliabilityDelta -= 1;
            Debug.Log(TAG + " [반려사항 놓침] → Performance-2, Reliability-1 [폴백]");
        }
    }

    // ── Case 3 ────────────────────────────────────────────────────────────

    /// <summary>불일치 없는 정상 평가 케이스. 성공 여부를 반환한다.</summary>
    private static bool EvaluateNormalCase(
        PlayerBase       playerBase,
        ComplaintContext complaint,
        Manual           manual,
        ref StatChangeEvent evt,
        bool isInvalidRejection,
        bool isCompleted,
        bool isRejection)
    {
        var eval = ManualEvaluator.Evaluate(
            manual.RequiredSteps,
            manual.ActionQueue,
            isAddressMismatch: false);
        Debug.Log(TAG + " 평가 — " + eval);

        bool isAbnormal = isInvalidRejection
                       || (isCompleted && !eval.IsClean)
                       || (!isCompleted && !isRejection);

        if (isAbnormal)
        {
            string reason  = isInvalidRejection ? "비정상 반려" : "정상 응대 실패";
            var    soData  = GetManualData(manual);
            if (soData != null && !soData.abnormalRejectionPenalty.IsEmpty)
            {
                ApplyPenaltyFromSO(playerBase, soData.abnormalRejectionPenalty);
                evt.performanceDelta -= soData.abnormalRejectionPenalty.Performance;
                evt.kindnessDelta    -= soData.abnormalRejectionPenalty.Kindness;
                evt.stressDelta      += soData.abnormalRejectionPenalty.Stress;
                evt.reliabilityDelta -= soData.abnormalRejectionPenalty.Reliability;
                evt.payDelta         -= soData.abnormalRejectionPenalty.Pay;
                Debug.Log(TAG + " [" + reason + "] → abnormalRejectionPenalty 적용");
            }
            else
            {
                playerBase.AddPerformance(-2);
                playerBase.AddStat(Stat.Reliability, -1);
                evt.performanceDelta -= 2;
                evt.reliabilityDelta -= 1;
                Debug.Log(TAG + " [" + reason + "] → Performance-2, Reliability-1 [폴백]");
            }
        }

        if (eval.PerformanceDelta != 0) { playerBase.AddPerformance(eval.PerformanceDelta);              evt.performanceDelta += eval.PerformanceDelta; }
        if (eval.KindnessDelta    != 0) { playerBase.AddStat(Stat.Kindness,    eval.KindnessDelta);      evt.kindnessDelta    += eval.KindnessDelta; }
        if (eval.StressDelta      != 0) { playerBase.AddStat(Stat.Stress,      eval.StressDelta);        evt.stressDelta      += eval.StressDelta; }
        if (eval.ReliabilityDelta != 0) { playerBase.AddStat(Stat.Reliability, eval.ReliabilityDelta);   evt.reliabilityDelta += eval.ReliabilityDelta; }
        if (eval.PayDelta         != 0) { playerBase.AddPay(eval.PayDelta);                              evt.payDelta         += eval.PayDelta; }

        bool isSuccess = false;
        if (isCompleted && eval.IsClean)
        {
            var soData = GetManualData(manual);
            if (soData != null && !soData.completionReward.IsEmpty)
            {
                ApplyReward(playerBase, soData.completionReward);
                evt.performanceDelta += soData.completionReward.Performance;
                evt.kindnessDelta    += soData.completionReward.Kindness;
                evt.reliabilityDelta += soData.completionReward.Reliability;
                evt.payDelta         += soData.completionReward.Pay;
                Debug.Log(TAG + " 정상 응대 보상 → completionReward 적용");
            }
            isSuccess = true;
        }

        return isSuccess;
    }

    // ── 스탯 적용 헬퍼 ───────────────────────────────────────────────────

    private static void ApplyReward(PlayerBase playerBase, StepReward reward)
    {
        if (reward.Performance != 0) playerBase.AddPerformance(reward.Performance);
        if (reward.Kindness    != 0) playerBase.AddStat(Stat.Kindness,    reward.Kindness);
        if (reward.Reliability != 0) playerBase.AddStat(Stat.Reliability, reward.Reliability);
        if (reward.Pay         != 0) playerBase.AddPay(reward.Pay);
    }

    private static void ApplyPenaltyFromSO(PlayerBase playerBase, StepPenalty penalty)
    {
        if (penalty.Performance != 0) playerBase.AddPerformance(-penalty.Performance);
        if (penalty.Kindness    != 0) playerBase.AddStat(Stat.Kindness,    -penalty.Kindness);
        if (penalty.Stress      != 0) playerBase.AddStat(Stat.Stress,       penalty.Stress);
        if (penalty.Reliability != 0) playerBase.AddStat(Stat.Reliability, -penalty.Reliability);
        if (penalty.Pay         != 0) playerBase.AddPay(-penalty.Pay);
    }

    private static void ApplyNuisancePenalty(PlayerBase playerBase, NuisancePenalty penalty)
    {
        if (penalty.stress      != 0) playerBase.AddStat(Stat.Stress,       penalty.stress);
        if (penalty.kindness    != 0) playerBase.AddStat(Stat.Kindness,      penalty.kindness);
        if (penalty.reliability != 0) playerBase.AddStat(Stat.Reliability,   penalty.reliability);
        if (penalty.performance != 0) playerBase.AddPerformance(-penalty.performance);
    }

    /// <summary>진상 종료 결과: stress/kindness/reliability는 그대로, performance는 양수=증가(보상 방향).</summary>
    private static void ApplyNuisanceResult(PlayerBase playerBase, NuisancePenalty result)
    {
        if (result.stress      != 0) playerBase.AddStat(Stat.Stress,       result.stress);
        if (result.kindness    != 0) playerBase.AddStat(Stat.Kindness,      result.kindness);
        if (result.reliability != 0) playerBase.AddStat(Stat.Reliability,   result.reliability);
        if (result.performance != 0) playerBase.AddPerformance(result.performance);
    }

    // ── ManualDataSO 헬퍼 ────────────────────────────────────────────────

    /// <summary>Manual 인스턴스에서 ManualDataSO를 꺼낸다.</summary>
private static ManualDataSO GetManualData(Manual manual)
    {
        if (manual is M_FullID_Self    self)    return self.manualData;
        if (manual is M_FullID_Proxy   proxy)   return proxy.manualData;
        if (manual is M_AddressChange  addrChg) return addrChg.manualData;
        if (manual is M_NewID          newId)   return newId.manualData;
        return null;
    }
}
