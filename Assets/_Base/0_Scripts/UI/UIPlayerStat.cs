using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerStat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBase      playerbase;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Performance UI")]
    [SerializeField] private Slider        performanceSlider;
    [SerializeField] private RectTransform goalMarker;
    [SerializeField] private TMP_Text      performanceText;
    [SerializeField] private RectTransform sliderFillRect;

    [Header("Stat Text UI")]
    //[SerializeField] private TMP_Text kindnessText;
    //[SerializeField] private TMP_Text stressText;
    //[SerializeField] private TMP_Text reliabilityText;
    [SerializeField] private TMP_Text payText;

    [Header("원형 스탯 바 (Radial)")]
    [SerializeField] private UIRadialStatBar kindnessBar;
    [SerializeField] private UIRadialStatBar stressBar;
    [SerializeField] private UIRadialStatBar reliabilityBar;

    [Header("인벤토리 슬롯 (0~2번 버튼)")]
    [SerializeField] private UIInventorySlot inventorySlot0;
    [SerializeField] private UIInventorySlot inventorySlot1;
    [SerializeField] private UIInventorySlot inventorySlot2;

    private void Awake()
    {
        playerbase      = PlayerBase.Instance;
        playerInventory = PlayerInventory.Instance;
    }

    private void OnGUI()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (playerbase == null) return;

        UpdatePerformanceUI();
        UpdateStatTexts();
        UpdateRadialBars();
        RefreshInventorySlots();
    }

    // ── Performance ───────────────────────────────────────────────────────

    private void UpdatePerformanceUI()
    {
        int cur  = Mathf.Clamp(playerbase.Performance, 0, playerbase.GetMaxPerformance());
        int max  = playerbase.GetMaxPerformance();
        int goal = Mathf.Clamp(playerbase.GoalPerformance, 0, max);

        if (performanceSlider != null)
        {
            performanceSlider.minValue = 0;
            performanceSlider.maxValue = max;
            performanceSlider.value    = cur;
        }

        if (performanceText != null)
            performanceText.text = $"{cur} / {max}";

        UpdateGoalMarker(goal, max);
    }

    private void UpdateGoalMarker(int goal, int max)
    {
        if (goalMarker == null || sliderFillRect == null || max <= 0) return;

        float ratio = Mathf.Clamp01((float)goal / max);
        float width = sliderFillRect.rect.width;
        float x     = width * 0.5f - width * ratio;
        goalMarker.anchoredPosition = new Vector2(x, goalMarker.anchoredPosition.y);
    }

    // ── 기존 텍스트 (유지) ────────────────────────────────────────────────

    private void UpdateStatTexts()
    {
        //if (kindnessText    != null) kindnessText.text    = ToPercent(playerbase.Kindness);
        //if (stressText      != null) stressText.text      = ToPercent(playerbase.Stress);
        //if (reliabilityText != null) reliabilityText.text = ToPercent(playerbase.Reliability);
        if (payText         != null) payText.text         = playerbase.Pay.ToString();
    }

    // ── 원형 슬라이더 ─────────────────────────────────────────────────────

    private void UpdateRadialBars()
    {
        kindnessBar?.SetValue(playerbase.Kindness);
        stressBar?.SetValue(playerbase.Stress);
        reliabilityBar?.SetValue(playerbase.Reliability);
    }

    // ── 인벤토리 ─────────────────────────────────────────────────────────

    private void RefreshInventorySlots()
    {
        inventorySlot0?.Refresh();
        inventorySlot1?.Refresh();
        inventorySlot2?.Refresh();
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────

    private string ToPercent(float v)
    {
        return $"{Mathf.Clamp(Mathf.RoundToInt(v * 100f), 0, 100)}%";
    }
}
