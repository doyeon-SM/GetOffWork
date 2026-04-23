using System;
using UnityEngine;

[Serializable]
public class ComplaintContext
{
    public enum ComplaintType  { FullID, AddressChange, NewID }
    public enum ApplicantType  { Self, Proxy }
    public enum DeliveryType   { None, Print, Mobile }
    public enum NuisanceType   { None, Rudely, Talkative }

    // ── 민원 기본 정보 ────────────────────────────────────────────────────
    [Header("민원 기본 정보")]
    public ComplaintType complaintType         = ComplaintType.FullID;
    public ApplicantType applicantType         = ApplicantType.Self;
    public DeliveryType  requestedDeliveryType = DeliveryType.None;
    public NuisanceType  nuisanceType          = NuisanceType.None;

    [Header("민원인 레코드 정보")]
    public string applicantRecordId; // 창구에 온 사람 (방문객)
    public string targetRecordId;    // 발급 대상자 (본인 발급이면 applicant와 동일, 대리인이면 대리 대상)

    // ── UI/오브젝트 연동 상태 ────────────────────────────────────────────
    [Header("UI 연동 상태")]
    public bool idCardSpawned;      // 방문객 신분증 오브젝트가 생성됐는가
    public bool idCardInspected;    // 방문객 신분증 상세 열람이 완료됐는가
    public bool proxyIdCardSpawned; // 대리인 신분증 오브젝트가 생성됐는가

    // ── 절차 흐름 결과값 ─────────────────────────────────────────────────
    [Header("절차 결과값")]
    public bool   searchedByInputId;      // 방문객 ID 조회를 수행했는가
    public string searchedInputId;        // 방문객 ID 조회에 사용한 문자열
    public bool   proxySearched;          // 대리인 ID 조회를 수행했는가
    public string proxySearchedInputId;   // 대리인 ID 조회에 사용한 문자열
    public bool   deliveryAsked;          // 전달 방식을 물어봤는가
    public bool   completed;              // 민원이 정상 종료됐는가
    public bool   rejected;               // 반려됐는가
    public bool   isAddressMismatch;  // 주소 불일치 여부 (applicant 또는 target의 fakeAddress)
    public bool   isIdMismatch;       // ID 불일치 여부 (applicant 또는 target의 fakeID)
    public bool   isPortraitMismatch; // 사진 불일치 여부 (applicant 또는 target의 fakePortrait)
    public bool   mobileNumberAsked;      // 전화번호를 질문했는가
public bool   mobileNumberVerified;   // 전화번호 일치 확인 완료됐는가

    // ── 주소 변경 (AddressChange 메뉴얼 전용) ──────────────────────────────
    /// <summary>ComplaintFactory가 큐에서 꺼낸 민원인의 새 주소 (변경 요청 주소)</summary>
    public string requestedNewAddress;
    /// <summary>현재 주소를 질문했는가</summary>
    public bool   newAddressAsked;
    /// <summary>모니터 주소 변경창에 입력한 주소</summary>
    public string enteredAddress;
    /// <summary>새 ID카드(주소변경본)가 프린터에서 출력됐는가</summary>
    public bool   newIdCardPrinted;
    /// <summary>새 ID카드가 TakeZone에 반납됐는가</summary>
    public bool   newIdCardReturned;
    /// <summary>
    /// SubmitNewAddress가 실행된 이후 true.
    /// 주소 불일치 반려 판정 시 변경 전/후를 구분하는 데 사용한다.
    /// false: 변경 전 → 불일치이면 정상 반려 가능 / true: 변경 후 → 반려실패
    /// </summary>
    public bool   isAddressChangeCommitted;

    // ── 주민 등록 (NewID 메뉴얼 전용) ───────────────────────────────────────────
    /// <summary>방문객 신분증 ID를 모니터에서 조회했는가</summary>
    public bool   newIdSearched;
    /// <summary>조회된 ID가 미등록 상태였는가</summary>
    public bool   wasUnregistered;
    /// <summary>모니터 ID탭에 입력한 ID 문자열</summary>
    public string enteredNewId;
    /// <summary>NewID탭에서 이름 입력 완료되었는가</summary>
    public bool   newNameEntered;
    /// <summary>NewID탭에서 주소 입력 완료되었는가</summary>
    public bool   newAddressEntered;
    /// <summary>초상화 등록 완료되었는가</summary>
    public bool   portraitRegistered;
    /// <summary>주민 등록(DB 저장)이 완료되었는가</summary>
    public bool   newUserRegistered;
    /// <summary>등록된 런타임 UserRecordData (M_NewID 내부에서 저장, 종료 시 정리용)</summary>
    public UnityEngine.Object runtimeUserData;
    /// <summary>ID입력이 오타로 재등록 흐름인가 (수정 뺄튼 사용)</summary>
    public bool   isEditMode;


    // ── 대화 임시 보관 ────────────────────────────────────────────────────
    [Header("마지막 대사")]
    //public string lastPlayerMessage;
    public string lastCustomerMessage;

    // ── 인내심 ────────────────────────────────────────────────────────────
    // ── 배정된 메뉴얼 ──────────────────────────────────────────────────────
    /// <summary>대기열 추가 시 확률로 결정된 ManualDataSO. 호출 시 그대로 사용한다.</summary>
    public ManualDataSO assignedManualData;

    [Header("민원인 인내심")]
    public float maxPatience     = 30f;
    public float currentPatience = 30f;

    public void ResetPatience() => currentPatience = maxPatience;

    // ── 레코드 ID 헬퍼 ───────────────────────────────────────────────────

    /// <summary>발급 대상 레코드 ID (Self=방문객, Proxy=대리 대상자)</summary>
    public string EffectiveTargetRecordId =>
        applicantType == ApplicantType.Self ? applicantRecordId : targetRecordId;

    /// <summary>전화번호 수신 대상 ID — 항상 창구에 온 방문객(applicant)</summary>
        public string PhoneRecipientRecordId => applicantRecordId;

    /// <summary>
    /// 주소/ID/사진 중 하나라도 불일치하면 true.
    /// 이 민원인이 반려 대상인지 여부를 판단할 때 사용한다.
    /// </summary>
    public bool HasAnyMismatch => isAddressMismatch || isIdMismatch || isPortraitMismatch;
}
