using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 인벤토리.
/// - 최대 슬롯 3개 고정 (null = 빈 슬롯)
/// - 구매 조건: 잔액 충분 + 당일 미구매 + 빈 슬롯 존재
/// - 아이템 사용은 2_MainScene에서만 가능
/// - 편의점 열릴 때 ResetDailyPurchase() 호출로 당일 이력 초기화
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public const int MaxSlots = 3;
    private const string MainSceneName = "2_MainScene";

    public static PlayerInventory Instance { get; private set; }

    [SerializeField] private PlayerBase playerBase;

    // 슬롯 고정 배열 (null = 빈 슬롯)
    private ItemBase[] slots = new ItemBase[MaxSlots];

    // 당일 편의점에서 구매한 아이템 ID (편의점 닫히면 초기화)
    private readonly HashSet<string> purchasedTodayIds = new HashSet<string>();

    private void Awake()
    {
        if (playerBase == null)
            playerBase = GetComponent<PlayerBase>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── 슬롯 조회 ─────────────────────────────────────────────────────────

    /// <summary>슬롯 배열 읽기 전용 (null = 빈 슬롯)</summary>
    public IReadOnlyList<ItemBase> Slots => slots;

    /// <summary>해당 슬롯 인덱스의 아이템 반환 (없으면 null)</summary>
    public ItemBase GetSlot(int index)
    {
        if (index < 0 || index >= MaxSlots) return null;
        return slots[index];
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < MaxSlots; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    // ── 당일 구매 이력 ────────────────────────────────────────────────────

    public void ResetDailyPurchase() => purchasedTodayIds.Clear();

    public bool HasPurchasedToday(ItemBase item)
    {
        if (item == null) return false;
        return purchasedTodayIds.Contains(item.itemId);
    }

    // ── 구매 ─────────────────────────────────────────────────────────────

    /// <summary>구매 가능 여부: 잔액 + 당일 미구매 + 빈 슬롯</summary>
    public bool CanBuyItem(ItemBase item)
    {
        if (item == null || playerBase == null) return false;
        if (playerBase.Pay < item.price)        return false;
        if (HasPurchasedToday(item))            return false;
        if (FindEmptySlot() < 0)               return false;
        return true;
    }

    public bool TryBuyItem(ItemBase item)
    {
        if (!CanBuyItem(item)) return false;

        int slot = FindEmptySlot();
        slots[slot] = item;
        playerBase.AddPay(-item.price);
        purchasedTodayIds.Add(item.itemId);

        Debug.Log($"[Inventory] 구매: {item.itemName} → 슬롯 {slot}");
        return true;
    }

    // ── 사용 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 슬롯 인덱스로 아이템 사용.
    /// 2_MainScene에서만 사용 가능, 즉시 스탯 적용 후 슬롯 비움.
    /// </summary>
    public bool UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;

        ItemBase item = slots[slotIndex];
        if (item == null) return false;

        if (SceneManager.GetActiveScene().name != MainSceneName)
        {
            Debug.Log($"[Inventory] 아이템 사용은 {MainSceneName}에서만 가능합니다.");
            return false;
        }

        ApplyItemEffect(item);
        slots[slotIndex] = null;

        Debug.Log($"[Inventory] 사용: {item.itemName} (슬롯 {slotIndex})");
        return true;
    }

    /// <summary>아이템 효과를 PlayerBase 스탯에 즉시 적용.</summary>
    public void ApplyItemEffect(ItemBase item)
    {
        if (item == null || playerBase == null || item.effects == null) return;
        foreach (var effect in item.effects)
            playerBase.AddStat(effect.effectType, effect.value);
    }
}
