using UnityEngine;

/// <summary>
/// 주소불일치(ID카드 위변조) 케이스의 등장 확률 설정.
/// ServiceDataManager에 연결해 사용한다.
///
/// 사용법:
///   Project 창 우클릭 > Create > Game > Manual > Address Mismatch Setting
/// </summary>
[CreateAssetMenu(
    fileName = "MismatchSetting",
    menuName  = "Game/Setting/Mismatch Setting")]
public class MismatchSettingSO : ScriptableObject
{
    [Header("주소불일치 케이스 등장 확률")]
    [Tooltip("0.0 = 절대 등장 안 함 / 1.0 = 항상 등장.\n" +
             "민원 생성 시 이 확률로 해당 민원인의 ID카드 주소를 틀리게 설정한다.")]
    [Range(0f, 1f)]
    public float AddressspawnChance = 0.2f;

    [Header("ID불일치 케이스 등장 확률")]
    [Tooltip("0.0 = 절대 등장 안 함 / 1.0 = 항상 등장.\n" +
             "민원 생성 시 이 확률로 해당 민원인의 ID카드 ID를 틀리게 설정한다.")]
    [Range(0f, 1f)]
    public float IDspawnChance = 0.2f;

    [Header("초상화불일치 케이스 등장 확률")]
    [Tooltip("0.0 = 절대 등장 안 함 / 1.0 = 항상 등장.\n" +
             "민원 생성 시 이 확률로 해당 민원인의 ID카드 초상화를 틀리게 설정한다.")]
    [Range(0f, 1f)]
    public float PortraitspawnChance = 0.2f;
}
