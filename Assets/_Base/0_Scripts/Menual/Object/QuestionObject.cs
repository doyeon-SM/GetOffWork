using UnityEngine;

public class QuestionObject : ClickableWorldObject
{
    [SerializeField] private UIQuestionPanel manualPanel;

    protected override void Awake()
    {
        if (manualPanel == null)
            manualPanel = FindFirstObjectByType<UIQuestionPanel>();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[QuestionObject] 질문 오브젝트 클릭");

        if (manualPanel != null)
        {
            manualPanel.Toggle();
        }
    }
}