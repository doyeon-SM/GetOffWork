using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private PlayerBase playerBase;
    [SerializeField] private List<PlayerItemData> ownedItems = new List<PlayerItemData>();

    public IReadOnlyList<PlayerItemData> OwnedItems => ownedItems;

    private void Awake()
    {
        if (playerBase == null)
            playerBase = GetComponent<PlayerBase>();
    }

    public int GetItemCount(ItemBase itemBase)
    {
        if (itemBase == null) return 0;

        PlayerItemData data = ownedItems.Find(x => x.itemBase == itemBase);
        return data != null ? data.currentCount : 0;
    }

    public bool CanBuyItem(ItemBase itemBase)
    {
        if (itemBase == null || playerBase == null)
            return false;

        if (playerBase.Pay < itemBase.price)
            return false;

        int currentCount = GetItemCount(itemBase);
        if (currentCount >= itemBase.maxCount)
            return false;

        return true;
    }

    public bool TryBuyItem(ItemBase itemBase)
    {
        if (!CanBuyItem(itemBase))
            return false;

        playerBase.AddPay(-itemBase.price);

        PlayerItemData data = ownedItems.Find(x => x.itemBase == itemBase);
        if (data != null)
        {
            data.currentCount++;
        }
        else
        {
            ownedItems.Add(new PlayerItemData(itemBase, 1));
        }

        return true;
    }

    public bool UseItem(ItemBase itemBase)
    {
        if (itemBase == null || playerBase == null)
            return false;

        PlayerItemData data = ownedItems.Find(x => x.itemBase == itemBase);
        if (data == null || data.currentCount <= 0)
            return false;

        ApplyItemEffect(itemBase);
        data.currentCount--;

        if (data.currentCount <= 0)
            ownedItems.Remove(data);

        return true;
    }

    public void ApplyItemEffect(ItemBase itemBase)
    {
        if (itemBase == null || playerBase == null || itemBase.effects == null)
            return;

        foreach (var effect in itemBase.effects)
        {
            playerBase.AddStat(effect.effectType, effect.value);
        }
    }
}