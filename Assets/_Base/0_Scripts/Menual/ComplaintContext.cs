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
        Print,
        Mobile
    }

    [Header("민원 기본 정보")]
    public ComplaintType complaintType = ComplaintType.FullID;
    public ApplicantType applicantType = ApplicantType.Self;
    public DeliveryType deliveryType = DeliveryType.Print;

    [Header("진행 상태")]
    public bool idCardSubmitted;

    public bool selfPhotoChecked;
    public bool selfIdChecked;
    public bool selfAddressChecked;

    public bool proxyPhotoChecked;
    public bool proxyIdChecked;
    public bool proxyAddressChecked;

    public bool targetPhotoChecked;
    public bool targetIdChecked;
    public bool targetAddressChecked;

    public bool phoneNumberReceived;
    public bool emailReceived;

    [Header("민원인 인내심")]
    public float maxPatience = 30f;
    public float currentPatience = 30f;

    public void ResetPatience()
    {
        currentPatience = maxPatience;
    }
}