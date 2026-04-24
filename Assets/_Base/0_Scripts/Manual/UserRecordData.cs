using UnityEngine;

[CreateAssetMenu(fileName = "UserRecord", menuName = "Game/Record/User Record")]
public class UserRecordData : ScriptableObject
{
    [Header("기본 식별 정보")]
    public string recordId;
    public string fullName;
    public Sprite portrait;

    [Header("실제 정보 (모니터/DB 기준)")]
    public string address;

    [Header("확인 정보")]
    public string birthDate;
    public string phoneNumber;
    public string email;

    [Header("신분증 위변조 정보 (비워두면 정상)")]
    [Tooltip("값이 있으면 ID카드에 이 주소가 표시됨 (모니터 DB 주소와 다름)")]
    public string fakeAddress;
    [Tooltip("값이 있으면 ID카드에 이 ID가 표시됨 (모니터 DB ID와 다름)")]
    public string fakeID;
    [Tooltip("할당되면 ID카드에 이 초상화가 표시됨 (모니터 DB 사진과 다름)")]
    public Sprite fakePortrait;

    [TextArea]
    public string note;

    // ── 불일치 여부 ────────────────────────────────────────────────────────

    /// <summary>fakeAddress가 있으면 주소 불일치 케이스.</summary>
    public bool HasAddressMismatch  => !string.IsNullOrEmpty(fakeAddress);

    /// <summary>fakeID가 있으면 ID 불일치 케이스.</summary>
    public bool HasIdMismatch       => !string.IsNullOrEmpty(fakeID);

    /// <summary>fakePortrait가 있으면 사진 불일치 케이스.</summary>
    public bool HasPortraitMismatch => fakePortrait != null;

    // ── Spawn 시점 표시값 계산 헬퍼 ────────────────────────────────────────

    /// <summary>
    /// Spawn 시점에 신분증에 표시할 ID를 반환한다.
    /// SO에 쓰지 않으므로 공유 오염이 없다.
    /// </summary>
    public string ResolveDisplayId(bool useFake)
        => (useFake && !string.IsNullOrEmpty(fakeID)) ? fakeID : recordId;

    public string ResolveDisplayAddress(bool useFake)
        => (useFake && !string.IsNullOrEmpty(fakeAddress)) ? fakeAddress : address;

    public Sprite ResolveDisplayPortrait(bool useFake)
        => (useFake && fakePortrait != null) ? fakePortrait : portrait;
}
