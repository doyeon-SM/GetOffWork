using UnityEngine;
using System;

[System.Serializable]
public struct ItemStatEffect
{
    public Stat effectType;
    public int value;
}

[CreateAssetMenu(fileName ="Item_", menuName ="Game/ItemBase")]
public class ItemBase : ScriptableObject
{
    [Header("기본 정보")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("구매 정보")]
    public int price;
    public int maxCount = 1;

    [Header("사용 효과")]
    public ItemStatEffect[] effects;
}

[Serializable]
public class PlayerItemData
{
    public ItemBase itemBase;
    public int currentCount;

    public PlayerItemData(ItemBase itemBase, int currentCount)
    {
        this.itemBase = itemBase;
        this.currentCount = currentCount;
    }
}
