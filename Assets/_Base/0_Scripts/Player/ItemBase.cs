using UnityEngine;
using System;

[Serializable]
public struct ItemStatEffect
{
    public Stat effectType;
    public int  value;
}

/// <summary>
/// 아이템 데이터 ScriptableObject.
/// maxCount 제거 — 편의점 방문 1회당 1개 구매 가능.
/// </summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Game/ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("기본 정보")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("가격")]
    public int price;

    [Header("사용 효과")]
    public ItemStatEffect[] effects;
}

/// <summary>
/// 플레이어 인벤토리에서 관리되는 아이템 인스턴스 데이터.
/// </summary>
[Serializable]
public class PlayerItemData
{
    public ItemBase itemBase;
    public int      currentCount;

    public PlayerItemData(ItemBase itemBase, int currentCount)
    {
        this.itemBase     = itemBase;
        this.currentCount = currentCount;
    }
}
