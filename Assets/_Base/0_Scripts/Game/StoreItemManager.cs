using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 편의점에 등록할 전체 아이템 풀을 관리하는 ScriptableObject.
/// UIStore는 이 SO를 참조해 런타임에 랜덤 4개를 딕셔너리로 가져온다.
///
/// 사용법:
///   Project 우클릭 → Create → Game/StoreItemManager
///   allItems에 판매할 ItemBase SO를 모두 등록
///   UIStore의 storeItemManager 슬롯에 이 에셋 연결
/// </summary>
[CreateAssetMenu(fileName = "StoreItemManager", menuName = "Game/StoreItemManager")]
public class StoreItemManager : ScriptableObject
{
    [Header("판매 가능한 전체 아이템 목록")]
    [SerializeField] private List<ItemBase> allItems = new List<ItemBase>();

    public IReadOnlyList<ItemBase> AllItems => allItems;

    /// <summary>
    /// 전체 목록에서 랜덤으로 최대 count개를 골라
    /// 슬롯 인덱스(0~count-1)를 키로 하는 딕셔너리로 반환한다.
    /// </summary>
    public Dictionary<int, ItemBase> GetRandomItems(int count = 4)
    {
        var result   = new Dictionary<int, ItemBase>();
        if (allItems == null || allItems.Count == 0) return result;

        // 풀 셔플 (Fisher-Yates)
        var pool = new List<ItemBase>(allItems);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int take = Mathf.Min(count, pool.Count);
        for (int i = 0; i < take; i++)
        {
            if (pool[i] != null)
                result[i] = pool[i];
        }

        return result;
    }
}
