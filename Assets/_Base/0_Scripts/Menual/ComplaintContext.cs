using System;
using UnityEngine;

[Serializable]
public class ComplaintContext
{
    public enum ComplaintType
    {
        FullID
    }

    public enum ApplicantType
    {
        Self,
        Proxy
    }

    public enum DeliveryType
    {
        None,
        Print,
        Mobile
    }

    [Header("민원 기본 정보")]
    public ComplaintType complaintType = ComplaintType.FullID;
    public ApplicantType applicantType = ApplicantType.Self;
    public DeliveryType requestedDeliveryType = DeliveryType.None;

    [Header("민원 대상 정보")]
    public string applicantRecordId; // 창구에 온 사람
    public string targetRecordId;    // 발급 대상자 (본인발급이면 applicant와 동일 가능)

    [Header("진행 상태")]
    public bool idCardSpawned;
    public bool idCardInspected;
    public bool monitorOpened;
    public bool searchedByInputId;
    public bool recordCompared;
    public bool addressMatched;
    public bool deliveryAsked;
    public bool documentPrinted;
    public bool documentSent;
    public bool rejected;
    public bool completed;

    [Header("조회 결과")]
    public string searchedInputId;
    public string lastPlayerMessage;
    public string lastCustomerMessage;

    [Header("민원인 인내심")]
    public float maxPatience = 30f;
    public float currentPatience = 30f;

    public void ResetPatience()
    {
        currentPatience = maxPatience;
    }

    public string EffectiveTargetRecordId
    {
        get
        {
            if (applicantType == ApplicantType.Self)
                return applicantRecordId;
            return targetRecordId;
        }
    }
}