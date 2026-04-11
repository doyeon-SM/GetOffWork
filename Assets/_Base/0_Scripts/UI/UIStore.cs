using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편의점 UI.
/// - storeItemPool에 등록된 아이템 중 랜덤 최대 4개를 진열한다.
/// - 구매 후 해당 슬롯은 "품절" 상태가 된다.
/// - 편의점이 열릴 때 PlayerInventory.ResetDailyPurchase()를 호출해 당일 구매 이력을 초기화한다.
/// </summary>
public class UIStore : MonoBehaviour
{
    [Header("기본 UI")]
    [SerializeField] private Button    closeButton;
    [SerializeField] private TMP_Text  playerPayText;

    [Header("아이템 풀 (전체 목록 — 이 중 랜덤 4개 진열)")]
    [SerializeField] private List<ItemBase> storeItemPool = new List<ItemBase>();

    [Header("슬롯 (Inspector에서 4개 연결)")]
    [SerializeField] private UIStoreItemSlot[] itemSlots = new UIStoreItemSlot[4];

    private UIHomeController uiHomeController;
    private PlayerInventory  playerInventory;
    private PlayerBase       playerBase;

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

    /// <summary>풀에서 랜덤 최대 4개를 골라 슬롯에 배치한다.</summary>
    private void BindSlots()
    {
        // 풀을 셔플해서 앞 4개 선택
        var shuffled = new List<ItemBase>(storeItemPool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int slotCount = Mathf.Min(itemSlots.Length, 4);
        for (int i = 0; i < slotCount; i++)
        {
            if (itemSlots[i] == null) continue;

            if (i < shuffled.Count && shuffled[i] != null)
            {
                itemSlots[i].gameObject.SetActive(true);
                itemSlots[i].Setup(shuffled[i], this, i);
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
        {
            if (slot != null && slot.gameObject.activeSelf)
                slot.Refresh();
        }
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
