using UnityEngine;

/// <summary>
/// 출력된 서류(Paper) UI의 추상 기반 클래스.
/// 민원 유형별 구현체(UIFullIDPaperView 등)가 상속받아
/// 민원 내용에 맞는 필드를 채운다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPaperView : MonoBehaviour
{
    protected CanvasGroup canvasGroup;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    /// <summary>
    /// 민원 데이터를 받아 UI 필드를 채우고 표시한다.
    /// printedRecordId: 인쇄 시점 기록된 RecordId — 이 값으로 레코드 조회해 표시.
    /// </summary>
    public abstract void Show(ComplaintContext complaint, UserRecordDatabase database,
        string printedRecordId = null);

    public virtual void Hide()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnClickClose() => Hide();
}
