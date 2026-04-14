using UnityEngine;

/// <summary>
/// ComplaintContext 생성 전담 클래스.
/// ServiceDeskManager에서 분리되어 손님 데이터 롤 로직만 책임진다.
/// </summary>
public static class ComplaintFactory
{
    private const string TAG = "[ComplaintFactory]";

    private const float DEFAULT_PATIENCE_MIN = 20f;
    private const float DEFAULT_PATIENCE_MAX = 40f;

    /// <summary>랜덤 민원 컨텍스트를 생성해 반환한다.</summary>
    public static ComplaintContext Create()
    {
        var c = RollBaseType();
        AssignRecords(c);
        CalculatePatience(c);
        return c;
    }

    // ── 기본 타입 롤 ─────────────────────────────────────────────────────

    private static ComplaintContext RollBaseType()
    {
        var c = new ComplaintContext();
        c.complaintType = ComplaintContext.ComplaintType.FullID;
        c.applicantType = Random.value > 0.5f
            ? ComplaintContext.ApplicantType.Self
            : ComplaintContext.ApplicantType.Proxy;
        c.requestedDeliveryType = Random.value > 0.5f
            ? ComplaintContext.DeliveryType.Print
            : ComplaintContext.DeliveryType.Mobile;

        var nuisanceSO = ServiceDataManager.Instance?.NuisanceSettings;
        c.nuisanceType = nuisanceSO != null
            ? nuisanceSO.RollNuisanceType()
            : ComplaintContext.NuisanceType.None;

        return c;
    }

    // ── 레코드 배정 + 불일치 판정 ─────────────────────────────────────────

    private static void AssignRecords(ComplaintContext c)
    {
        var ub = ServiceDataManager.Instance?.UserDatabase;
        if (ub == null || ub.Records == null || ub.Records.Count == 0) return;

        // 신청인 레코드 배정
        int ai = Random.Range(0, ub.Records.Count);
        c.applicantRecordId = ub.Records[ai].recordId;

        // 대상 레코드 배정
        if (c.applicantType == ComplaintContext.ApplicantType.Self)
        {
            c.targetRecordId = c.applicantRecordId;
        }
        else
        {
            if (ub.Records.Count >= 2)
            {
                int ti = Random.Range(0, ub.Records.Count - 1);
                if (ti >= ai) ti++;
                c.targetRecordId = ub.Records[ti].recordId;
            }
            else
            {
                c.applicantType  = ComplaintContext.ApplicantType.Self;
                c.targetRecordId = c.applicantRecordId;
                Debug.LogWarning(TAG + " 레코드 1개만 존재 — Proxy 불가, Self로 대체");
            }
        }

        RollMismatches(c, ub);
    }

    private static void RollMismatches(ComplaintContext c, UserRecordDatabase ub)
    {
        var mismatchSO      = ServiceDataManager.Instance?.MismatchSetting;
        float addrChance    = mismatchSO != null ? mismatchSO.AddressspawnChance   : 0f;
        float idChance      = mismatchSO != null ? mismatchSO.IDspawnChance        : 0f;
        float portraitChance= mismatchSO != null ? mismatchSO.PortraitspawnChance  : 0f;

        ub.TryGetRecord(c.applicantRecordId, out UserRecordData aRec);
        ub.TryGetRecord(c.targetRecordId,    out UserRecordData tRec);

        c.isAddressMismatch = Random.value < addrChance
            && ((aRec != null && aRec.HasAddressMismatch)
                || (tRec != null && tRec.HasAddressMismatch));

        c.isIdMismatch = Random.value < idChance
            && ((aRec != null && aRec.HasIdMismatch)
                || (tRec != null && tRec.HasIdMismatch));

        c.isPortraitMismatch = Random.value < portraitChance
            && ((aRec != null && aRec.HasPortraitMismatch)
                || (tRec != null && tRec.HasPortraitMismatch));

        aRec.SetIdCard(c.isAddressMismatch, c.isIdMismatch, c.isPortraitMismatch);

        Debug.Log(TAG + $" 불일치: addr={c.isAddressMismatch} id={c.isIdMismatch} portrait={c.isPortraitMismatch}");
    }

    // ── 인내심 계산 ───────────────────────────────────────────────────────

    private static void CalculatePatience(ComplaintContext c)
    {
        var patienceSO = GetManualDataSO(c);
        float pMin = DEFAULT_PATIENCE_MIN;
        float pMax = DEFAULT_PATIENCE_MAX;

        if (patienceSO != null && patienceSO.HasPatienceOverride)
        {
            if (patienceSO.patienceMin > 0f) pMin = patienceSO.patienceMin;
            if (patienceSO.patienceMax > 0f) pMax = patienceSO.patienceMax;
            if (pMax < pMin) pMax = pMin;
        }

        float basePatience = Random.Range(pMin, pMax);

        var nuisanceSO = ServiceDataManager.Instance?.NuisanceSettings;
        if (nuisanceSO != null && c.nuisanceType != ComplaintContext.NuisanceType.None)
        {
            var entry = nuisanceSO.GetEntry(c.nuisanceType);
            basePatience *= entry.patienceMultiplier;
            Debug.Log(TAG + " [" + c.nuisanceType + "] 진상 생성 / 인내심 배율: " + entry.patienceMultiplier);
        }

        c.maxPatience     = basePatience;
        c.currentPatience = basePatience;
    }

    /// <summary>ComplaintContext로 해당 ManualDataSO를 조회한다.</summary>
    private static ManualDataSO GetManualDataSO(ComplaintContext c)
    {
        var sd = ServiceDataManager.Instance;
        if (sd == null) return null;

        bool isSelf   = c.applicantType == ComplaintContext.ApplicantType.Self;
        bool isPrint  = c.requestedDeliveryType == ComplaintContext.DeliveryType.Print;
        bool isMobile = c.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile;

        if (isSelf  && isPrint)  return sd.FullSelf_Print;
        if (isSelf  && isMobile) return sd.FullSelf_Mobile;
        if (!isSelf && isPrint)  return sd.FullProxy_Print;
        if (!isSelf && isMobile) return sd.Fullproxy_Mobile;
        return null;
    }
}
