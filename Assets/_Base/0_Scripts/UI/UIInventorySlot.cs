using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 버튼 하나를 담당.
/// - 아이템 있음 → 버튼 활성화, 아이콘/이름 표시
/// - 아이템 없음 → 버튼 비활성화, 비어있음 표시
/// - 버튼 클릭 → PlayerInventory.UseItem(slotIndex)
/// </summary>
public class UIInventorySlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button   button;
    [SerializeField] private Image    iconImage;
    //[SerializeField] private TMP_Text nameText;

    [Header("슬롯 인덱스 (0~2)")]
    [SerializeField] private int slotIndex;

    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = PlayerInventory.Instance;

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickUse);
            button.onClick.AddListener(OnClickUse);
        }
    }

    private void OnEnable()
    {
        // 씬 복귀 등으로 활성화될 때 Instance를 다시 잡음
        if (inventory == null)
            inventory = PlayerInventory.Instance;
        Refresh();
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

        // 버튼 활성/비활성
        if (button != null)
            button.interactable = hasItem;

        // 아이콘
        if (iconImage != null)
        {
            iconImage.sprite  = hasItem ? item.icon : null;
            iconImage.enabled = hasItem && item.icon != null;
        }

        // 이름 텍스트
        //if (nameText != null)
        //    nameText.text = hasItem ? item.itemName : "-";
    }

    // ── 클릭 ─────────────────────────────────────────────────────────────

    private void OnClickUse()
    {
        if (inventory == null) return;

        bool success = inventory.UseItem(slotIndex);
        if (success)
            Refresh();
    }
}
