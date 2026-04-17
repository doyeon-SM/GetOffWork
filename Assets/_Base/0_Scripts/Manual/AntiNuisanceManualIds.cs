/// <summary>
/// 진상 퇴치 메뉴얼 전용 커맨드 ID 상수 모음.
/// UI 버튼과 ServiceDeskManager.ExecuteAntiNuisanceManual()을 연결할 때 사용한다.
/// </summary>
public static class AntiNuisanceManualIds
{
    /// <summary>진상퇴치 메뉴얼 - SOS: 현재 응대를 강제 종료하고 스탯을 적용한다.</summary>
    public const string SOS = "AntiNuisance_SOS";

    // 추후 확장 예시
    // public const string TalkBack = "AntiNuisance_TalkBack";  // 막말하기
}
