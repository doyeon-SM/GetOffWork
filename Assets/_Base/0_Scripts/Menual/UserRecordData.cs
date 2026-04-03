using UnityEngine;

[CreateAssetMenu(fileName = "UserRecord", menuName = "Game/Manual/User Record")]
public class UserRecordData : ScriptableObject
{
    [Header("기본 식별 정보")]
    public string recordId;          // 주민 식별용 ID
    public string fullName;
    public Sprite portrait;
    public string address;

    [Header("확장 정보")]
    public string birthDate;
    public string phoneNumber;
    public string email;

    [Header("검증/이상 데이터")]
    public bool isTampered;
    public bool hasMovedAddress;

    [TextArea]
    public string note;
}