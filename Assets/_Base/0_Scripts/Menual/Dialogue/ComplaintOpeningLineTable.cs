/// <summary>
/// 민원인 입장 대사 테이블.
/// ComplaintLineTableBase를 상속하며, 로직은 베이스에서 처리한다.
///
/// 사용법:
///   Project 우클릭 → Create → Game/Dialogue → Complaint Opening Lines
///   ServiceDataManager의 openingLineTable 슬롯에 연결한다.
/// </summary>
[UnityEngine.CreateAssetMenu(
    fileName = "ComplaintOpeningLineTable",
    menuName  = "Game/Dialogue/Complaint Opening Lines")]
public class ComplaintOpeningLineTable : ComplaintLineTableBase { }
