using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerStat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBase playerbase;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Performance UI")]
    [SerializeField] private Slider performanceSlider;
    [SerializeField] private RectTransform goalMarker;
    [SerializeField] private TMP_Text performanceText;

    [Header("Stat Text UI")]
    [SerializeField] private TMP_Text kindnessText;
    [SerializeField] private TMP_Text stressText;
    [SerializeField] private TMP_Text reliabilityText;
    [SerializeField] private TMP_Text payText;

    private RectTransform sliderFillRect;

    private void Start()
    {
        RefreshUI();
    }

    private void Awake()
    {
        if(playerbase == null)
            playerbase = FindFirstObjectByType<PlayerBase>().GetComponent<PlayerBase>();

        if(playerInventory ==null)
            playerInventory = FindFirstObjectByType<PlayerInventory>().GetComponent<PlayerInventory>();

        if (performanceSlider != null && performanceSlider.fillRect != null)
            sliderFillRect = performanceSlider.fillRect.parent.GetComponent<RectTransform>();
    }

    private void RefreshUI()
    {
        if (playerbase == null)
            return;

        UpdatePerformanceUI();
        UpdateStatTexts();
    }

    private void UpdatePerformanceUI()
    {
        int currentPerformance = playerbase.Performance;
        int maxPerformance = playerbase.GetMaxPerformance();
        int goalPerformance = playerbase.GoalPerformance;

        currentPerformance = Mathf.Clamp(currentPerformance, 0, maxPerformance);
        goalPerformance = Mathf.Clamp(goalPerformance, 0, maxPerformance);

        if (performanceSlider != null)
        {
            performanceSlider.minValue = 0;
            performanceSlider.maxValue = maxPerformance;
            performanceSlider.value = currentPerformance;
        }

        if (performanceText != null)
        {
            performanceText.text = $"{currentPerformance} / {maxPerformance}";
        }

        UpdateGoalMarker(goalPerformance, maxPerformance);
    }
    private void UpdateStatTexts()
    {
        UpdateStat_Kindness();
        UpdateStat_Stress();
        UpdateStat_Reliability();
        UpdatePay();
    }
    private void UpdateGoalMarker(int goal, int max)
    {
        if (goalMarker == null || sliderFillRect == null || max <= 0)
            return;
        float ratio = (float)goal / max;
        ratio = Mathf.Clamp01(ratio);

        float width = sliderFillRect.rect.width;
        float xPos = (width * ratio) - (width * 0.5f);

        Vector2 anchoredPos = goalMarker.anchoredPosition;
        anchoredPos.x = xPos;
        goalMarker.anchoredPosition = anchoredPos;
    }
    private string ToPercent(float v)
    {
        int percent = Mathf.RoundToInt(v * 100f);
        percent = Mathf.Clamp(percent, 0, 100);
        return $"{percent}%";
    }
    private void UpdateStat_Kindness()
    {
        if (kindnessText != null)
            kindnessText.text = ToPercent(playerbase.Kindness);
    }
    private void UpdateStat_Stress()
    {
        if (stressText != null)
            stressText.text = ToPercent(playerbase.Stress);
    }
    private void UpdateStat_Reliability()
    {
        if (reliabilityText != null)
            reliabilityText.text = ToPercent(playerbase.Reliability);
    }
    private void UpdatePay()
    {
        if (payText != null)
            payText.text = playerbase.Pay.ToString();
    }
}
