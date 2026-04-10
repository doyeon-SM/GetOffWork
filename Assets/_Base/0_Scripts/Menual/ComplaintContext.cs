using System;
using UnityEngine;

[Serializable]
public class ComplaintContext
{
    public enum ComplaintType  { FullID }
    public enum ApplicantType  { Self, Proxy }
    public enum DeliveryType   { None, Print, Mobile }

    public enum NuisanceType { None, Rudely }

    // ── 민원 기본 정보 ────────────────────────────────────────────────────
    [Header("민원 기본 정보")]
    public ComplaintType complaintType        = ComplaintType.FullID;
    public ApplicantType applicantType        = ApplicantType.Self;
    public DeliveryType  requestedDeliveryType = DeliveryType.None;
    public NuisanceType nuisanceType = NuisanceType.None;

    [Header("민원인 레코드 정보")]
    public string applicantRecordId; // 창구에 온 사람
    public string targetRecordId;    // 발급 대상자 (본인 발급이면 applicant와 동일)

    // ── UI/오브젝트 연동 상태 ────────────────────────────────────────────
    [Header("UI 연동 상태")]
    public bool idCardSpawned;    // 신분증 오브젝트가 생성됐는가
    public bool idCardInspected;  // 신분증 상세 열람이 완료됐는가

    // ── 절차 흐름 결과값 ─────────────────────────────────────────────────
    [Header("절차 결과값")]
    public bool   searchedByInputId; // 모니터에서 ID 조회를 수행했는가
    public string searchedInputId;   // 조회에 사용한 ID 문자열
    public bool   deliveryAsked;     // 전달 방식(인쇄/전자)을 물어봤는가
    public bool   completed;         // 민원이 정상 종료됐는가
public bool   rejected;          // 반려됐는가
    public bool   isAddressMismatch; // 반려 시 주소 불일치가 실제로 존재하는가 (판정 기준)
    public bool   mobileNumberAsked;    // 전화번호를 질문했는가
    public bool   mobileNumberVerified;  // 전화번호 일치 확인이 완료됐는가 (Send 성공)

    // ── 대화 임시 보관 ────────────────────────────────────────────────────
    [Header("마지막 대사")]
    public string lastPlayerMessage;
    public string lastCustomerMessage;

    // ── 인내심 ────────────────────────────────────────────────────────────
    [Header("민원인 인내심")]
    public float maxPatience     = 30f;
    public float currentPatience = 30f;

    public void ResetPatience() => currentPatience = maxPatience;

    /// <summary>실제 발급 대상 레코드 ID (본인/대리 구분)</summary>
    public string EffectiveTargetRecordId =>
        applicantType == ApplicantType.Self ? applicantRecordId : targetRecordId;
}
