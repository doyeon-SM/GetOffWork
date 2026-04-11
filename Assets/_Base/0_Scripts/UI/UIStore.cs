using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편의점 UI.
/// - StoreItemManager SO에서 랜덤 4개를 딕셔너리로 받아 슬롯에 진열한다.
/// - 구매 후 해당 슬롯은 "품절" 상태가 된다.
/// - 편의점이 열릴 때 PlayerInventory.ResetDailyPurchase()를 호출해 당일 이력을 초기화한다.
/// </summary>
public class UIStore : MonoBehaviour
{
    [Header("기본 UI")]
    [SerializeField] private Button   closeButton;
    [SerializeField] private TMP_Text playerPayText;

    [Header("아이템 풀 관리자 (StoreItemManager SO)")]
    [SerializeField] private StoreItemManager storeItemManager;

    [Header("슬롯 (Inspector에서 4개 연결)")]
    [SerializeField] private UIStoreItemSlot[] itemSlots = new UIStoreItemSlot[4];

    private UIHomeController uiHomeController;
    private PlayerInventory  playerInventory;
    private PlayerBase       playerBase;

    // 이번 방문에서 진열된 아이템 딕셔너리 (슬롯인덱스 → ItemBase)
    private Dictionary<int, ItemBase> displayedItems;

    public PlayerInventory Inventory => playerInventory;

    // ── 초기화 ───────────────────────────────────────────────────────────

    public void Initialize(UIHomeController controller, PlayerInventory inventory, PlayerBase player)
    {
        uiHomeController = controller;
        playerInventory  = inventory;
        playerBase       = player;

        // 당일 구매 이력 초기화
        playerInventory?.ResetDailyPurchase();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClickClose);
            closeButton.onClick.AddListener(OnClickClose);
        }

        BindSlots();
        RefreshUI();
    }

    /// <summary>StoreItemManager에서 랜덤 4개를 받아 슬롯에 배치한다.</summary>
    private void BindSlots()
    {
        if (storeItemManager == null)
        {
            Debug.LogWarning("[UIStore] StoreItemManager가 연결되지 않았습니다.");
            foreach (var slot in itemSlots)
                if (slot != null) slot.gameObject.SetActive(false);
            return;
        }

        displayedItems = storeItemManager.GetRandomItems(4);

        int slotCount = Mathf.Min(itemSlots.Length, 4);
        for (int i = 0; i < slotCount; i++)
        {
            if (itemSlots[i] == null) continue;

            if (displayedItems.TryGetValue(i, out ItemBase item))
            {
                itemSlots[i].gameObject.SetActive(true);
                itemSlots[i].Setup(item, this, i);
            }
            else
            {
                itemSlots[i].gameObject.SetActive(false);
            }
        }
    }

    // ── 구매 ─────────────────────────────────────────────────────────────

    public void TryBuyItem(ItemBase itemBase, int slotIndex)
    {
        if (playerInventory == null || itemBase == null) return;

        bool success = playerInventory.TryBuyItem(itemBase);

        if (success)
        {
            Debug.Log($"[Store] 구매 성공: {itemBase.itemName}");
            if (slotIndex >= 0 && slotIndex < itemSlots.Length)
                itemSlots[slotIndex].SetSoldOut();
        }
        else
        {
            Debug.Log($"[Store] 구매 실패: {itemBase.itemName}");
        }

        RefreshUI();
    }

    // ── UI 갱신 ───────────────────────────────────────────────────────────

    public void RefreshUI()
    {
        if (playerPayText != null && playerBase != null)
            playerPayText.text = $"잔액 : {playerBase.Pay}원";

        foreach (var slot in itemSlots)
            if (slot != null && slot.gameObject.activeSelf)
                slot.Refresh();
    }

    // ── 닫기 ─────────────────────────────────────────────────────────────

    private void OnClickClose()
    {
        if (uiHomeController != null)
            uiHomeController.OnConvenienceStoreClosed();
        else
            Destroy(gameObject);
    }
}
