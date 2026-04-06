using System;
using UnityEngine;

[Serializable]
public class ComplaintContext
{
    public enum ComplaintType  { FullID }
    public enum ApplicantType  { Self, Proxy }
    public enum DeliveryType   { None, Print, Mobile }

    // ── 민원 기본 정보 ────────────────────────────────────────────────────
    [Header("민원 기본 정보")]
    public ComplaintType complaintType   = ComplaintType.FullID;
    public ApplicantType applicantType   = ApplicantType.Self;
    public DeliveryType  requestedDeliveryType = DeliveryType.None;

    [Header("민원인 레코드 정보")]
    public string applicantRecordId; // 창구에 온 사람
    public string targetRecordId;    // 발급 대상자 (본인 발급이면 applicant와 동일)

    // ── UI/오브젝트 연동에 필요한 상태 (외부 참조 존재) ──────────────────
    // 이 필드들은 Manual의 Handle 메서드가 직접 세팅하며,
    // UI나 스프라이트 오브젝트가 읽어 시각 상태를 결정한다.
    [Header("UI 연동 상태")]
    public bool idCardSpawned;    // 신분증 오브젝트가 생성됐는가
    public bool idCardInspected;  // 신분증 상세 열람이 완료됐는가
    public bool monitorOpened;    // 모니터가 열렸는가

    // ── 절차 흐름 제어에 필요한 최소 상태 ────────────────────────────────
    // bool 남용을 피하되, 분기(주소 일치 여부, 전달 방식)처럼
    // 런타임 결과에 의존하는 값은 유지한다.
    [Header("절차 결과값")]
    public bool   searchedByInputId;  // 모니터에서 ID 조회를 수행했는가
    public string searchedInputId;    // 조회에 사용한 ID 문자열
    public bool   recordCompared;     // 카드-모니터 비교를 수행했는가
    public bool   addressMatched;     // 비교 결과: 주소 일치 여부
    public bool   deliveryAsked;      // 전달 방식(인쇄/전자)을 물어봤는가
    public bool   completed;          // 민원이 정상 종료됐는가
    public bool   rejected;           // 주소 불일치로 반려됐는가

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
