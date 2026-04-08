/// <summary>
/// 데스크 위에 올라오는 물리 오브젝트의 종류.
/// Manual이 "반납 필수 목록"을 이 enum으로 선언하고,
/// TakeObjectArea가 반납 여부를 이 enum으로 추적한다.
/// </summary>
public enum DeskObjectType
{
    None        = 0,
    IDCard      = 1,
    PrintedDoc  = 2,   // 미래 확장: 출력 등본
    // 이후 추가 서류를 여기에 열거
}
