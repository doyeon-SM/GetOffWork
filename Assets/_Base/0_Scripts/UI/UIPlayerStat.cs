using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    [SerializeField] private TMP_Text payText;

    [Header("원형 스탯 바 (Radial)")]
    [SerializeField] private UIRadialStatBar kindnessBar;
    [SerializeField] private UIRadialStatBar stressBar;
    [SerializeField] private UIRadialStatBar reliabilityBar;

    [Header("인벤토리 슬롯 (0~2번 버튼)")]
    [SerializeField] private UIInventorySlot inventorySlot0;
    [SerializeField] private UIInventorySlot inventorySlot1;
    [SerializeField] private UIInventorySlot inventorySlot2;

    [Header("Stat Update Text UI")]
    [SerializeField] private TMP_Text updatePerformance;
    [SerializeField] private TMP_Text updateStress;
    [SerializeField] private TMP_Text updateKindness;
    [SerializeField] private TMP_Text updateReliability;

    private void Awake()
    {
        playerbase      = PlayerBase.Instance;
        playerInventory = PlayerInventory.Instance;
        ResetUI();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (WorkDayManager.Instance != null)
            WorkDayManager.Instance.OnUIPlayerStatUpdate -= HandleUIPlayerStatUpdate;
    }

    private void TrySubscribe()
    {
        if (WorkDayManager.Instance != null)
        {
            // 중복 구독 방지: 먼저 해제 후 재구독
            WorkDayManager.Instance.OnUIPlayerStatUpdate -= HandleUIPlayerStatUpdate;
            WorkDayManager.Instance.OnUIPlayerStatUpdate += HandleUIPlayerStatUpdate;
        }
    }

    private void LateUpdate()
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

    private void ResetUI()
    {
        if (updatePerformance != null) updatePerformance.text = "";
        if (updateStress      != null) updateStress.text      = "";
        if (updateKindness    != null) updateKindness.text    = "";
        if (updateReliability != null) updateReliability.text = "";
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
        if (payText != null) payText.text = playerbase.Pay.ToString() + " 원";
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

    // ── 스탯 변화 표시 ────────────────────────────────────────────────────

    private void HandleUIPlayerStatUpdate(int p, int s, int k, int r)
    {
        // 모든 파라미터는 이미 정수 단위 (성과: 정수, 스탯: 정수 %)
        if (p != 0)
        {
            string v = p > 0 ? $"+{p}" : p.ToString();
            StartCoroutine(ViewUpdatePlayerStat(v, updatePerformance));
        }
        if (s != 0)
        {
            string v = s > 0 ? $"+{s}%" : $"{s}%";
            StartCoroutine(ViewUpdatePlayerStat(v, updateStress));
        }
        if (k != 0)
        {
            string v = k > 0 ? $"+{k}%" : $"{k}%";
            StartCoroutine(ViewUpdatePlayerStat(v, updateKindness));
        }
        if (r != 0)
        {
            string v = r > 0 ? $"+{r}%" : $"{r}%";
            StartCoroutine(ViewUpdatePlayerStat(v, updateReliability));
        }
    }

    IEnumerator ViewUpdatePlayerStat(string value, TMP_Text text)
    {
        if (text == null) yield break;
        text.text = value;
        yield return new WaitForSeconds(1f);
        text.text = "";
    }
}
