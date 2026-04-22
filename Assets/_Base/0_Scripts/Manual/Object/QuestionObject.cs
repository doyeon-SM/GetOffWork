using System;
using UnityEngine;

public class QuestionObject : ClickableWorldObject
{
    [SerializeField] private UIQuestionPanel manualPanel;

    /// <summary>포스트잇이 클릭됐을 때 TutorialManager가 구독하는 이벤트</summary>
    public static event Action OnPostitClicked;

    protected override void Awake()
    {
        if (manualPanel == null)
            manualPanel = FindFirstObjectByType<UIQuestionPanel>();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[QuestionObject] 포스트잇 클릭");

        OnPostitClicked?.Invoke();

        if (manualPanel != null)
            manualPanel.Toggle();
    }
}
