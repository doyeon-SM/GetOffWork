using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하루 정산 UI.
/// Open(data, onConfirm) 호출 시 statChangeProgress 큐를 0.5초 간격으로 소진하며
/// 성과/스탯 수치를 애니메이션으로 갱신한다.
/// 큐 소진 후 일급 합치기 연출(1초)을 실행하고, 완료 후 확인 버튼을 활성화한다.
/// </summary>
public class UIDayResultView : MonoBehaviour
{
    // ── Inspector 연결 ──────────────────────────────────────────────────────

    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("성과")]
    [SerializeField] private TMP_Text performanceText;
    [Tooltip("응대당 성과 변화량 (+N / -N) 을 잠깐 표시할 텍스트")]
    [SerializeField] private TMP_Text performanceDeltaText;

    [Header("스탯")]
    [SerializeField] private TMP_Text stressText;
    [SerializeField] private TMP_Text stressDeltaText;

    [SerializeField] private TMP_Text kindnessText;
    [SerializeField] private TMP_Text kindnessDeltaText;

    [SerializeField] private TMP_Text reliabilityText;
    [SerializeField] private TMP_Text reliabilityDeltaText;

    [Header("일급")]
    [Tooltip("현재 보유 급여 (startPay 에서 합치기 연출 후 DailyWage 합산)")]
    [SerializeField] private TMP_Text payText;
    [Tooltip("대기 중인 일급 — 모든 응대 정산 후 startPay 쪽으로 합쳐진다")]
    [SerializeField] private TMP_Text pendingWageText;

    [Header("확인 버튼")]
    [SerializeField] private Button confirmButton;

    [Header("연출 설정")]
    [Tooltip("이벤트 1건 소비 간격 (초)")]
    [SerializeField] private float eventInterval       = 0.5f;
    [Tooltip("델타 텍스트 표시 지속 시간 (초)")]
    [SerializeField] private float deltaDisplayDuration = 0.4f;
    [Tooltip("일급 합치기 연출 총 시간 (초)")]
    [SerializeField] private float wageMergeDuration   = 1f;

    [Header("색상")]
    [SerializeField] private Color positiveColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color negativeColor = new Color(0.9f, 0.2f, 0.2f);

    // ── 내부 상태 ─────────────────────────────────────────────────────────

    private Action        _onConfirm;
    private DayResultData _data;

    // 애니메이션용 현재 표시 수치
    private int   _dispPerformance;
    private float _dispStress;
    private float _dispKindness;
    private float _dispReliability;
    private int   _dispPay;
    private int   _pendingWage;

    // ── Unity 생명주기 ────────────────────────────────────────────────────

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // ── 공개 API ──────────────────────────────────────────────────────────

    public void Open(DayResultData data, Action onConfirm)
    {
        _data      = data;
        _onConfirm = onConfirm;

        // 표시 수치를 시작값으로 초기화
        _dispPerformance = data.startPerformance;
        _dispStress      = data.startStress * 100f;
        _dispKindness    = data.startKindness*100f;
        _dispReliability = data.startReliability*100f;
        _dispPay         = data.startPay;
        _pendingWage     = 0;

        if (confirmButton != null) confirmButton.interactable = false;
        ClearDeltaTexts();
        RefreshStaticUI();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        StartCoroutine(PlayResultSequence());
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // ── 메인 연출 시퀀스 ──────────────────────────────────────────────────

    private IEnumerator PlayResultSequence()
    {
        // Phase 1: 큐를 eventInterval 간격으로 소진
        while (_data.statChangeProgress.Count > 0)
        {
            var evt = _data.statChangeProgress.Dequeue();
            ApplyEvent(evt);
            yield return new WaitForSeconds(eventInterval);
        }

        // Phase 2: 일급 합치기 연출
        yield return StartCoroutine(PlayWageMerge());

        // Phase 3: 확인 버튼 활성화
        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    // ── 이벤트 1건 적용 ───────────────────────────────────────────────────

    private void ApplyEvent(StatChangeEvent evt)
    {
        if (evt.performanceDelta != 0)
        {
            _dispPerformance += evt.performanceDelta;
            // 이벤트별 성과 변화에 따른 일급 누적 (음수 성과 변화는 0)
            _pendingWage += Mathf.Max(0, evt.performanceDelta * 10);
            Debug.Log($"[UIDayResultView] 성과 변화량 {evt.performanceDelta} 일급 결과: {_pendingWage}");
            ShowDelta(performanceDeltaText, evt.performanceDelta, isPercent: false);
        }
        if (evt.stressDelta != 0)
        {
            _dispStress += evt.stressDelta;
            ShowDelta(stressDeltaText, evt.stressDelta, isPercent: true);
        }
        if (evt.kindnessDelta != 0)
        {
            _dispKindness += evt.kindnessDelta;
            ShowDelta(kindnessDeltaText, evt.kindnessDelta, isPercent: true);
        }
        if (evt.reliabilityDelta != 0)
        {
            _dispReliability += evt.reliabilityDelta;
            ShowDelta(reliabilityDeltaText, evt.reliabilityDelta, isPercent: true);
        }

        RefreshPerformanceText();
        RefreshStatTexts();
        RefreshWageTexts();
    }

    // ── 일급 합치기 연출 ──────────────────────────────────────────────────

    private IEnumerator PlayWageMerge()
    {
        if (_pendingWage <= 0) yield break;

        int   startPay     = _dispPay;
        int   startPending = _pendingWage;
        float elapsed      = 0f;

        while (elapsed < wageMergeDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / wageMergeDuration);

            int transferred = Mathf.RoundToInt(startPending * t);
            _dispPay     = startPay    + transferred;
            _pendingWage = startPending - transferred;

            RefreshWageTexts();
            yield return null;
        }

        // 정확한 최종값으로 고정
        _dispPay     = startPay + startPending;
        _pendingWage = 0;
        RefreshWageTexts();
    }

    // ── UI 갱신 헬퍼 ─────────────────────────────────────────────────────

    private void RefreshStaticUI()
    {
        RefreshPerformanceText();
        RefreshStatTexts();
        RefreshWageTexts();
    }

    private void RefreshPerformanceText()
    {
        if (performanceText == null) return;
        int   max = _data.maxPerformance;
        float pct = max > 0 ? (float)_dispPerformance / max : 0f;
        performanceText.text = $"{_dispPerformance} / {max} ({pct:F0}%)";
    }

    private void RefreshStatTexts()
    {
        if (stressText      != null) stressText.text      = $"{Mathf.RoundToInt(_dispStress)}%";
        if (kindnessText    != null) kindnessText.text    = $"{Mathf.RoundToInt(_dispKindness)}%";
        if (reliabilityText != null) reliabilityText.text = $"{Mathf.RoundToInt(_dispReliability)}%";
    }

    private void RefreshWageTexts()
    {
        if (payText != null)
            payText.text = $"{_dispPay}원";

        if (pendingWageText != null)
        {
            pendingWageText.gameObject.SetActive(_pendingWage > 0);
            pendingWageText.text = $"+{_pendingWage}원";
        }
    }

    // ── 델타 텍스트 표시 ─────────────────────────────────────────────────

    private void ShowDelta(TMP_Text target, float delta, bool isPercent)
    {
        if (target == null) return;
        StartCoroutine(ShowDeltaCoroutine(target, delta, isPercent));
    }

    private IEnumerator ShowDeltaCoroutine(TMP_Text target, float delta, bool isPercent)
    {
        if (target == null) yield break;

        bool   positive = delta >= 0f;
        string sign     = positive ? "+" : "";
        target.color    = positive ? positiveColor : negativeColor;

        if (isPercent)
        {
            int pct     = Mathf.RoundToInt(delta);
            target.text = $"{sign}{pct}%";
        }
        else
        {
            int val     = Mathf.RoundToInt(delta);
            target.text = $"{sign}{val}";
        }

        yield return new WaitForSeconds(deltaDisplayDuration);

        // 아직 이 코루틴이 마지막으로 설정한 값이면 비운다
        // (다음 이벤트가 이미 덮어썼다면 그대로 두기 위해 비교)
        // 단순하게 항상 비우는 방식으로 처리 (0.4초 후 다음 이벤트 텍스트로 덮어씌워짐)
        target.text = "";
    }

    private void ClearDeltaTexts()
    {
        TMP_Text[] deltas = { performanceDeltaText, stressDeltaText, kindnessDeltaText, reliabilityDeltaText };
        foreach (var t in deltas)
            if (t != null) t.text = "";
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        if (_data != null)
        {
            var player = PlayerBase.Instance;
            if (player != null)
                player.AddPay(_data.DailyWage);
        }

        Close();
        _onConfirm?.Invoke();
    }
}
