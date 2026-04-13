using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하루 정산 UI.
/// WorkDayManager가 Open(data, onConfirm)을 호출하면 패널을 표시하고,
/// 확인 버튼을 누를 때 일급을 PlayerBase에 적용한 뒤 onConfirm 콜백을 실행한다.
/// </summary>
public class UIDayResultView : MonoBehaviour
{
    // ── Inspector 연결 ──────────────────────────────────────────────────────

    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("성과 텍스트")]
    [SerializeField] private TMP_Text performanceText;

    [Header("스트레스 텍스트")]
    [SerializeField] private TMP_Text stressText;

    [Header("친절함 텍스트")]
    [SerializeField] private TMP_Text kindnessText;

    [Header("신뢰도 텍스트")]
    [SerializeField] private TMP_Text reliabilityText;

    [Header("일급 텍스트")]
    [SerializeField] private TMP_Text wageText;

    [Header("확인 버튼")]
    [SerializeField] private Button confirmButton;

    // ── 내부 상태 ─────────────────────────────────────────────────────────
    private Action        _onConfirm;
    private DayResultData _data;

    // ── Unity 생명주기 ────────────────────────────────────────────────────
    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // ── 공개 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 정산 UI를 열고 데이터를 표시한다.
    /// </summary>
    /// <param name="data">하루 결과 데이터</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행될 콜백 (씬 이동 등)</param>
    public void Open(DayResultData data, Action onConfirm)
    {
        _data      = data;
        _onConfirm = onConfirm;

        RefreshUI();

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // ── UI 갱신 ───────────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (_data == null) return;

        // 성과: "성과: 30 (+5) / 목표(승진조건 50) 60%"
        if (performanceText != null)
        {
            int   delta  = _data.PerformanceDelta;
            float pct    = _data.maxPerformance > 0
                ? (float)_data.endPerformance / _data.maxPerformance * 100f
                : 0f;
            string sign  = delta >= 0 ? "+" : "";
            performanceText.text =
                $"성과: {_data.startPerformance} ({sign}{delta}) / {_data.maxPerformance} ({pct:F0}%)";
        }

        // 스트레스: "스트레스: 20% (+5%)"
        if (stressText != null)
        {
            int startPct = Mathf.RoundToInt(_data.startStress * 100f);
            int deltaPct = Mathf.RoundToInt(_data.StressDelta  * 100f);
            string sign  = deltaPct >= 0 ? "+" : "-";
            stressText.text = $"스트레스: {startPct}% ({sign}{deltaPct}%)";
        }

        // 친절함: "친절함: 50% (+10%)"
        if (kindnessText != null)
        {
            int startPct = Mathf.RoundToInt(_data.startKindness * 100f);
            int deltaPct = Mathf.RoundToInt(_data.KindnessDelta  * 100f);
            string sign  = deltaPct >= 0 ? "+" : "-";
            kindnessText.text = $"친절함: {startPct}% ({sign}{deltaPct}%)";
        }

        // 신뢰도: "신뢰도: 30% (+5%)"
        if (reliabilityText != null)
        {
            int startPct = Mathf.RoundToInt(_data.startReliability * 100f);
            int deltaPct = Mathf.RoundToInt(_data.ReliabilityDelta  * 100f);
            string sign  = deltaPct >= 0 ? "+" : "-";
            reliabilityText.text = $"신뢰도: {startPct}% ({sign}{deltaPct}%)";
        }

        // 일급: "일급: 1000원 (+50원)"
        if (wageText != null)
        {
            wageText.text = $"일급: {_data.startPay}원 (+{_data.DailyWage}원)";
        }
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────────────────
    private void OnConfirmClicked()
    {
        // 일급을 플레이어에게 적용
        if (_data != null && _data.DailyWage > 0)
        {
            var player = PlayerBase.Instance;
            if (player != null)
                player.AddPay(_data.DailyWage);
        }

        Close();
        _onConfirm?.Invoke();
    }
}
