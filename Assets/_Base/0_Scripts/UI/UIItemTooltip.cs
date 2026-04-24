using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 인벤토리 슬롯 호버 시 표시되는 아이템 툴팁 UI.
/// ShowTooltip(item, slotRect) 호출 시 슬롯 y - 100 위치에 앵커를 고정한다.
/// </summary>
public class UIItemTooltip : MonoBehaviour
{
    public static UIItemTooltip Instance { get; private set; }

    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("텍스트")]
    [SerializeField] private TMP_Text effectsText;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ApplyVisible(false);
    }

    // ── 공개 API ──────────────────────────────────────────────────────────

    /// <summary>slotRect : 호버 중인 UIInventorySlot 의 RectTransform</summary>
    public void ShowTooltip(ItemBase item)
    {
        if (item == null || item.effects == null || item.effects.Length == 0)
        {
            ApplyVisible(false);
            return;
        }

        // 효과 텍스트 빌드
        var sb = new StringBuilder();
        for (int i = 0; i < item.effects.Length; i++)
        {
            var e = item.effects[i];
            string sign = e.value >= 0 ? "+" : "";
            if (i > 0) sb.AppendLine();
            sb.Append($"{EffectTypeText(e.effectType)}: {sign}{e.value}%");
        }
        if (effectsText != null)
            effectsText.text = sb.ToString();

        ApplyVisible(true);
    }

    public void HideTooltip()
    {
        ApplyVisible(false);
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    private void ApplyVisible(bool v)
    {
        if (panelRoot != null) panelRoot.SetActive(v);
    }

    private string EffectTypeText(Stat s)
    {
        switch(s)
        {
            case Stat.Stress:
                return "스트레스";
            case Stat.Kindness:
                return "친절함";
            case Stat.Reliability:
                return "신뢰도";
            default:
                return "Error";
        }
    }
}
