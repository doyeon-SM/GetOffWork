using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// HomeScene에서 전날의 신문을 표시하는 UI.
/// 신문은 HomeScene으로 이동되었으므로 WorkDayManager와 무관하게 동작한다.
/// onClosed 콜백으로 MorningHomeController에 닫힘을 알린다.
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
    /// 신문을 열고 onClosed 콜백을 등록한다.
    /// HomeScene에서는 WorkDayManager가 없으므로 시계 제어를 하지 않는다.
    /// </summary>
    public void Open(Action onClosed = null)
    {
        this.onClosed = onClosed;

        int currentDay = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentDay : 1;
        EventDayType? eventType = GetTodayEventType(currentDay);

        Sprite sprite = newspaperData != null ? newspaperData.Resolve(currentDay, eventType) : null;
        if (newspaperImage != null)
        {
            newspaperImage.sprite = sprite;
            newspaperImage.gameObject.SetActive(sprite != null);
        }

        string headline = newspaperData != null ? newspaperData.ResolveHeadline(currentDay, eventType) : string.Empty;
        if (newsheadlineText != null)
        {
            newsheadlineText.text = headline;
            newsheadlineText.gameObject.SetActive(!string.IsNullOrEmpty(headline));
        }

        newspaperPanel.SetActive(true);
    }

    private void OnCloseClicked()
    {
        newspaperPanel.SetActive(false);
        onClosed?.Invoke();
    }

    private EventDayType? GetTodayEventType(int day)
    {
        int weekPos = (day - 1) % 6;
        if (weekPos == 5) return EventDayType.Weekend;
        return null;
    }
}