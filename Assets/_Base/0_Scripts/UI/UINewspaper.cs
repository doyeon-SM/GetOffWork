using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// MainScene 시작 시 오전 전에 표시되는 신문 UI.
/// WorkDayManager.PauseTimer() / ResumeTimer() 를 통해 시계를 정지/재개합니다.
/// </summary>
public class UINewspaper : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject newspaperPanel;

    [Header("신문 이미지")]
    [SerializeField] private Image newspaperImage;

    [Header("신문 대문 텍스트")]
    [SerializeField] private TMP_Text newsheadlineText;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("신문 데이터 에셋")]
    [SerializeField] private NewspaperData newspaperData;

    private Action onClosed;

    private void Awake()
    {
        closeButton.onClick.AddListener(OnCloseClicked);
        newspaperPanel.SetActive(false);
    }

    /// <summary>
    /// 신문을 열고 시계를 정지시킵니다.
    /// onClosed: 닫기 버튼 후 실행할 콜백 (오전 시작 등).
    /// </summary>
    public void Open(Action onClosed = null)
    {
        this.onClosed = onClosed;

        // 이미지 결정
        int currentDay = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentDay : 1;
        EventDayType? eventType = GetTodayEventType(currentDay);
        Sprite sprite = newspaperData != null ? newspaperData.Resolve(currentDay, eventType) : null;

        if (newspaperImage != null)
        {
            newspaperImage.sprite = sprite;
            newspaperImage.gameObject.SetActive(sprite != null);
        }

        // 헤드라인 텍스트
        if (newsheadlineText != null)
        {
            string headline = newspaperData != null ? newspaperData.ResolveHeadline(currentDay, eventType) : string.Empty;
            newsheadlineText.text = headline;
            newsheadlineText.gameObject.SetActive(!string.IsNullOrEmpty(headline));
        }

        // 시계 정지
        WorkDayManager.Instance?.PauseTimer();

        newspaperPanel.SetActive(true);
    }

    private void OnCloseClicked()
    {
        newspaperPanel.SetActive(false);

        // 시계 재개
        WorkDayManager.Instance?.ResumeTimer();

        onClosed?.Invoke();
    }

    /// <summary>LevelDesignManager와 동일한 판정 로직으로 오늘 이벤트 타입을 반환.</summary>
    private EventDayType? GetTodayEventType(int day)
    {
        // Weekend: 1~5 평일, 6 주말 사이클
        int weekPos = (day - 1) % 6;
        if (weekPos == 5) return EventDayType.Weekend;
        return null;
    }
}