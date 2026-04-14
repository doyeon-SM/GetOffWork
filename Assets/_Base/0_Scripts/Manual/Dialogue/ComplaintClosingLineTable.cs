/// <summary>
/// 민원인 퇴장 대사 테이블.
/// ComplaintLineTableBase를 상속하며, 로직은 베이스에서 처리한다.
///
/// 퇴장 대사는 민원 종료 시(FinishCurrentCustomer) 발화된다.
/// isCompleted(정상 완료), patienceExpired(인내심 소진), isRejection(반려)
/// 등 종료 유형별 대사를 별도 엔트리로 관리하려면 추후 closingType 필드를 추가한다.
///
/// 사용법:
///   Project 우클릭 → Create → Game/Dialogue → Complaint Closing Lines
///   ServiceDataManager의 closingLineTable 슬롯에 연결한다.
/// </summary>
[UnityEngine.CreateAssetMenu(
    fileName = "ComplaintClosingLineTable",
    menuName  = "Game/Setting/Complaint Closing Lines")]
public class ComplaintClosingLineTable : ComplaintLineTableBase { }
