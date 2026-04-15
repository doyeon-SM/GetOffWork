using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주민 등록 메뉴얼에서 사용할 초상화 리스트 ScriptableObject.
/// RuntimeUserGenerator가 방문객 데이터 생성 시 이 리스트에서 랜덤으로 선택한다.
/// </summary>
[CreateAssetMenu(
    fileName = "PortraitList",
    menuName  = "Game/Setting/Portrait List",
    order     = 11)]
public class PortraitListSO : ScriptableObject
{
    [Header("정상 초상화 목록")]
    [Tooltip("방문객의 실제 얼굴 사진. UserImage에 표시되며 모니터 DB 기준 사진이다.")]
    public List<Sprite> portraits = new List<Sprite>();

    [Header("가짜 초상화 목록")]
    [Tooltip("신분증에 표시될 가짜 사진. 이것이 선택되면 초상화 반려 사항에 해당한다.")]
    public List<Sprite> fakePortraits = new List<Sprite>();

    /// <summary>정상 초상화 리스트에서 랜덤 1개 반환. 없으면 null.</summary>
    public Sprite GetRandomPortrait()
    {
        if (portraits == null || portraits.Count == 0)
        {
            Debug.LogWarning("[PortraitListSO] portraits 리스트가 비어있습니다.");
            return null;
        }
        return portraits[Random.Range(0, portraits.Count)];
    }

    /// <summary>가짜 초상화 리스트에서 랜덤 1개 반환. 없으면 null.</summary>
    public Sprite GetRandomFakePortrait()
    {
        if (fakePortraits == null || fakePortraits.Count == 0)
        {
            Debug.LogWarning("[PortraitListSO] fakePortraits 리스트가 비어있습니다.");
            return null;
        }
        return fakePortraits[Random.Range(0, fakePortraits.Count)];
    }
}
