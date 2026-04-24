using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ComplaintContext 생성 전담 클래스.
/// ServiceDeskManager에서 분리되어 손님 데이터 롤 로직만 책임진다.
/// </summary>
public static class ComplaintFactory
{
    private const string TAG = "[ComplaintFactory]";

    // ── 주소 큐 ───────────────────────────────────────────────────────────
    private static Queue<string> _addressQueue = new Queue<string>();

    /// <summary>
    /// ServiceDataManager의 AddressListSO를 읽어 주소 큐를 초기화한다.
    /// WorkDayManager.StartMorningWork() 등 하루 시작 시 호출할 것.
    /// </summary>
    public static void InitializeAddressQueue()
    {
        _addressQueue.Clear();
        var so = ServiceDataManager.Instance?.AddressListSO;
        if (so == null || so.addresses == null || so.addresses.Count == 0)
        {
            Debug.LogWarning(TAG + " AddressListSO가 비어있습니다. 주소 큐 초기화 실패.");
            return;
        }
        foreach (var addr in so.addresses)
            _addressQueue.Enqueue(addr);
        Debug.Log(TAG + $" 주소 큐 초기화 완료: {_addressQueue.Count}개");
    }

    /// <summary>주소 큐에서 하나 꺼낸다. 소진 시 소스를 다시 로드해 반복한다.</summary>
    private static string DequeueAddress()
    {
        if (_addressQueue.Count == 0)
        {
            Debug.LogWarning(TAG + " 주소 큐 소진 — SO를 다시 로드합니다.");
            InitializeAddressQueue();
        }
        return _addressQueue.Count > 0 ? _addressQueue.Dequeue() : "(주소 없음)";
    }

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
        var c   = new ComplaintContext();
        var mmd = ServiceDataManager.Instance?.ManualDataManager;

        // ManualDataManager에서 확률 기반으로 메뉴얼 선택
        var manual = mmd?.GetRandomManual();
        if (manual == null)
        {
            Debug.LogWarning(TAG + " ManualDataManager에서 메뉴얼을 가져오지 못했습니다. FullID/Self/Print로 폴백.");
            c.complaintType         = ComplaintContext.ComplaintType.FullID;
            c.applicantType         = ComplaintContext.ApplicantType.Self;
            c.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
            return c;
        }

        // manualTitle로 complaintType, applicantType, deliveryType 결정
        c.assignedManualData = manual;
        string title = manual.manualTitle ?? "";

        if (title.IndexOf("AddressChange", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            c.complaintType         = ComplaintContext.ComplaintType.AddressChange;
            c.applicantType         = ComplaintContext.ApplicantType.Self;
            c.requestedDeliveryType = ComplaintContext.DeliveryType.None;
            c.requestedNewAddress   = DequeueAddress();
        }
        else if (title.IndexOf("NewID", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            c.complaintType         = ComplaintContext.ComplaintType.NewID;
            c.applicantType         = ComplaintContext.ApplicantType.Self;
            c.requestedDeliveryType = ComplaintContext.DeliveryType.None;
        }
        else // FullID
        {
            c.complaintType = ComplaintContext.ComplaintType.FullID;

            c.applicantType = title.IndexOf("Proxy", StringComparison.OrdinalIgnoreCase) >= 0
                ? ComplaintContext.ApplicantType.Proxy
                : ComplaintContext.ApplicantType.Self;

            if (title.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) >= 0)
                c.requestedDeliveryType = ComplaintContext.DeliveryType.Mobile;
            else
                c.requestedDeliveryType = ComplaintContext.DeliveryType.Print;
        }

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
        int ai = UnityEngine.Random.Range(0, ub.Records.Count);
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
                int ti = UnityEngine.Random.Range(0, ub.Records.Count - 1);
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
        var mismatchSO       = ServiceDataManager.Instance?.MismatchSetting;
        float addrChance     = mismatchSO != null ? mismatchSO.AddressspawnChance  : 0f;
        float idChance       = mismatchSO != null ? mismatchSO.IDspawnChance       : 0f;
        float portraitChance = mismatchSO != null ? mismatchSO.PortraitspawnChance : 0f;

        ub.TryGetRecord(c.applicantRecordId, out UserRecordData aRec);
        ub.TryGetRecord(c.targetRecordId,    out UserRecordData tRec);

        // Self: 방문객(aRec) 신분증만 존재 → aRec 기준으로 mismatch 판정 및 SetIdCard
        // Proxy: 방문객(aRec)과 대상자(tRec) 신분증 모두 존재
        //        → 방문객 신분증은 aRec, 대상자 신분증은 tRec 기준으로 각각 판정 및 SetIdCard
        bool isProxy = c.applicantType == ComplaintContext.ApplicantType.Proxy;

        // 방문객 신분증(aRec) 불일치 판정 — Self/Proxy 공통
        c.isAddressMismatch  = UnityEngine.Random.value < addrChance
            && aRec != null && aRec.HasAddressMismatch;
        c.isIdMismatch       = UnityEngine.Random.value < idChance
            && aRec != null && aRec.HasIdMismatch;
        c.isPortraitMismatch = UnityEngine.Random.value < portraitChance
            && aRec != null && aRec.HasPortraitMismatch;

        if (aRec != null)
            aRec.SetIdCard(c.isAddressMismatch, c.isIdMismatch, c.isPortraitMismatch);

        // 대상자 신분증(tRec) 불일치 판정 — Proxy 전용
        if (isProxy && tRec != null)
        {
            bool tAddrMismatch    = UnityEngine.Random.value < addrChance    && tRec.HasAddressMismatch;
            bool tIdMismatch      = UnityEngine.Random.value < idChance      && tRec.HasIdMismatch;
            bool tPortraitMismatch = UnityEngine.Random.value < portraitChance && tRec.HasPortraitMismatch;
            tRec.SetIdCard(tAddrMismatch, tIdMismatch, tPortraitMismatch);
            Debug.Log(TAG + $" [Proxy] 대상자 불일치: addr={tAddrMismatch} id={tIdMismatch} portrait={tPortraitMismatch}");
        }

        Debug.Log(TAG + $" 불일치: addr={c.isAddressMismatch} id={c.isIdMismatch} portrait={c.isPortraitMismatch}");
    }

    // ── 인내심 계산 ───────────────────────────────────────────────────────

    private static void CalculatePatience(ComplaintContext c)
    {
        // 대기열 추가 시 이미 결정된 assignedManualData의 인내심 설정을 사용한다
        var patienceSO = c.assignedManualData;
        float pMin = DEFAULT_PATIENCE_MIN;
        float pMax = DEFAULT_PATIENCE_MAX;

        if (patienceSO != null && patienceSO.HasPatienceOverride)
        {
            if (patienceSO.patienceMin > 0f) pMin = patienceSO.patienceMin;
            if (patienceSO.patienceMax > 0f) pMax = patienceSO.patienceMax;
            if (pMax < pMin) pMax = pMin;
        }

        float basePatience = UnityEngine.Random.Range(pMin, pMax);

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

}
