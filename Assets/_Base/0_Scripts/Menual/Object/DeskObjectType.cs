/// <summary>
/// 데스크 위에 올라오는 물리 오브젝트의 종류.
/// Manual이 "반납 필수 목록"을 이 enum으로 선언하고,
/// ObjectManagerBox가 반납 여부를 이 enum으로 추적한다.
/// </summary>
public enum DeskObjectType
{
    None        = 0,
    IDCard      = 1,   // 방문객 신분증
    ProxyIDCard = 2,   // 대리인 신분증
    PrintedDoc  = 3,   // 출력 등본
}
