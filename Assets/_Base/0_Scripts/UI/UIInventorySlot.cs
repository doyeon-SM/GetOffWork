using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 버튼 하나를 담당.
/// 호버 시 UIItemTooltip.ShowTooltip(item, slotRectTransform) 호출.
/// </summary>
public class UIInventorySlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image  iconImage;

    [Header("슬롯 인덱스 (0~2)")]
    [SerializeField] private int slotIndex;

    private PlayerInventory inventory;
    private RectTransform   _rect;

    private void Awake()
    {
        inventory = PlayerInventory.Instance;
        _rect     = GetComponent<RectTransform>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickUse);
            button.onClick.AddListener(OnClickUse);
        }
    }

    private void OnEnable()
    {
        if (inventory == null) inventory = PlayerInventory.Instance;
        if (_rect     == null) _rect     = GetComponent<RectTransform>();
        Refresh();
    }

    private void OnDisable()
    {
        UIItemTooltip.Instance?.HideTooltip();
    }

    // ── 갱신 ─────────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (inventory == null)
        {
            inventory = PlayerInventory.Instance;
            if (inventory == null) return;
        }

        ItemBase item = inventory.GetSlot(slotIndex);
        bool hasItem  = item != null;

        if (button != null)
            button.interactable = hasItem;

        if (iconImage != null)
        {
            iconImage.sprite  = hasItem ? item.icon : null;
            iconImage.enabled = hasItem && item.icon != null;
        }
    }

    // ── 클릭 ─────────────────────────────────────────────────────────────

    private void OnClickUse()
    {
        if (inventory == null) return;
        bool success = inventory.UseItem(slotIndex);
        if (success)
        {
            UIItemTooltip.Instance?.HideTooltip();
            Refresh();
        }
    }

    // ── 호버 툴팁 ────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventory == null) return;
        ItemBase item = inventory.GetSlot(slotIndex);
        if (item == null) return;

        // 슬롯 자신의 RectTransform을 넘긴다 → 툴팁이 슬롯 y-100 에 위치
        UIItemTooltip.Instance?.ShowTooltip(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIItemTooltip.Instance?.HideTooltip();
    }
}
