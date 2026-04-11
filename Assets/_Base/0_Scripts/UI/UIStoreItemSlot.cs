using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편의점 슬롯 하나.
/// - 구매 후 SetSoldOut()으로 즉시 품절 상태로 전환한다.
/// - maxCount 개념 없음. 당일 1회 구매 = 품절.
/// </summary>
public class UIStoreItemSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button   buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private ItemBase  currentItem;
    private UIStore   parentStore;
    private int       slotIndex;
    private bool      isSoldOut;

    // ── 초기화 ───────────────────────────────────────────────────────────

    public void Setup(ItemBase itemBase, UIStore store, int index)
    {
        currentItem = itemBase;
        parentStore = store;
        slotIndex   = index;
        isSoldOut   = false;

        if (currentItem == null) return;

        if (iconImage != null)
        {
            iconImage.sprite  = currentItem.icon;
            iconImage.enabled = currentItem.icon != null;
        }
        if (nameText        != null) nameText.text        = currentItem.itemName;
        if (descriptionText != null) descriptionText.text = currentItem.description;
        if (priceText       != null) priceText.text       = $"{currentItem.price}원";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnClickBuy);
            buyButton.onClick.AddListener(OnClickBuy);
        }

        Refresh();
    }

    // ── 상태 갱신 ────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (parentStore == null || currentItem == null) return;

        // 품절 상태이면 버튼 비활성 + "품절" 텍스트
        if (isSoldOut)
        {
            SetButtonState(false, "품절");
            return;
        }

        bool canBuy = parentStore.Inventory != null &&
                      parentStore.Inventory.CanBuyItem(currentItem);

        SetButtonState(canBuy, canBuy ? "" : "구매불가");
    }

    /// <summary>구매 성공 직후 UIStore가 호출 — 즉시 품절 처리.</summary>
    public void SetSoldOut()
    {
        isSoldOut = true;
        Refresh();
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────

    private void SetButtonState(bool interactable, string label)
    {
        if (buyButton   != null) buyButton.interactable = interactable;
        if (buyButtonText != null) buyButtonText.text   = label;
    }

    private void OnClickBuy()
    {
        if (parentStore != null)
            parentStore.TryBuyItem(currentItem, slotIndex);
    }
}
