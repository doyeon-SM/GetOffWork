using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStore : MonoBehaviour
{
    [Header("기본 UI")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text playerPayText;

    [Header("상점 아이템 설정")]
    [SerializeField] private ItemBase[] storeItems = new ItemBase[4];
    [SerializeField] private UIStoreItemSlot[] itemSlots = new UIStoreItemSlot[4];

    private UIHomeController uihomecontroller;
    private PlayerInventory playerInventory;
    private PlayerBase playerBase;

    public PlayerInventory Inventory => playerInventory;

    public void Initialize(UIHomeController controller, PlayerInventory inventory, PlayerBase player)
    {
        uihomecontroller = controller;
        playerInventory = inventory;
        playerBase = player;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClickClose);
            closeButton.onClick.AddListener(OnClickClose);
        }

        BindSlots();
        RefreshUI();
    }

    private void BindSlots()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null)
                continue;

            if (i < storeItems.Length && storeItems[i] != null)
            {
                itemSlots[i].gameObject.SetActive(true);
                itemSlots[i].Setup(storeItems[i], this, i);
            }
            else
            {
                itemSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void TryBuyItem(ItemBase itemBase, int index)
    {
        if (playerInventory == null || itemBase == null)
            return;

        bool success = playerInventory.TryBuyItem(itemBase);

        if (success)
        {
            Debug.Log($"아이템 구매 성공: {itemBase.itemName}");
            itemSlots[index].SuccessBuy();
        }
        else
        {
            Debug.Log($"아이템 구매 실패: {itemBase.itemName}");
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (playerPayText != null && playerBase != null)
            playerPayText.text = $"소지금 : {playerBase.Pay}원";

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null && itemSlots[i].gameObject.activeSelf)
                itemSlots[i].Refresh();
        }
    }

    private void OnClickClose()
    {
        if (uihomecontroller != null)
        {
            uihomecontroller.OnConvenienceStoreClosed();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}