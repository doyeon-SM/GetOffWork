using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStoreItemSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private ItemBase currentItem;
    private UIStore parentStore;
    private int currentCount;
    private int slotIndex;

    public void Setup(ItemBase itemBase, UIStore store, int index)
    {
        currentItem = itemBase;
        parentStore = store;
        currentCount = currentItem.maxCount;
        slotIndex = index;

        if (currentItem == null) return;

        if (iconImage != null)
        {
            iconImage.sprite = currentItem.icon;
            iconImage.enabled = currentItem.icon != null;
        }

        if (nameText != null)
            nameText.text = currentItem.itemName;

        if (descriptionText != null)
            descriptionText.text = currentItem.description;

        if (priceText != null)
            priceText.text = $"{currentItem.price}원";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnClickBuy);
            buyButton.onClick.AddListener(OnClickBuy);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (parentStore == null || currentItem == null)
            return;

        PlayerInventory inventory = parentStore.Inventory;
        if (inventory == null)
            return;

       

        if (countText != null)
            countText.text = $"{currentCount} / {currentItem.maxCount}";

        bool canBuy = inventory.CanBuyItem(currentItem);

        if (buyButton != null)
            buyButton.interactable = canBuy;

        if (buyButtonText != null)
        {
            if (currentCount <= 0)
                buyButtonText.text = "품절";
            else if (!canBuy)
                buyButtonText.text = "구매불가";
            else
                buyButtonText.text = "";
        }
    }

    private void OnClickBuy()
    {
        if (parentStore != null)
            parentStore.TryBuyItem(currentItem, slotIndex);
    }

    public void SuccessBuy()
    {
        currentCount--;
    }
}