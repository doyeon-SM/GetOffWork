using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주소 변경 메뉴얼에서 사용할 요청 주소 목록 ScriptableObject.
/// ComplaintFactory가 게임 시작 시 이 리스트를 Queue로 변환해 순서대로 사용한다.
/// </summary>
[CreateAssetMenu(
    fileName = "AddressList",
    menuName  = "Game/Setting/Address List",
    order     = 10)]
public class AddressListSO : ScriptableObject
{
    [Header("주소 목록 (순서대로 사용됨)")]
    [Tooltip("민원인이 요청할 새 주소 목록. 소진 시 처음부터 반복한다.")]
    public List<string> addresses = new List<string>();
}
