using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MouseInteractableUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("기본 정보")]
    [SerializeField] private string interactableId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string tooltipText;

    [Header("상태")]
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private bool useTooltip = true;

    [Header("이벤트")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onLeftClick;
    public UnityEvent onRightClick;

    public string InteractableId => interactableId;
    public string DisplayName => displayName;
    public string TooltipText => tooltipText;
    public bool IsInteractable => isInteractable;
    public bool UseTooltip => useTooltip;

    public void SetTooltip(string newTooltip)
    {
        tooltipText = newTooltip;
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;

        MouseUIManager.Instance?.HandleHoverEnter(this);
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;

        MouseUIManager.Instance?.HandleHoverExit(this);
        onHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;

        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                MouseUIManager.Instance?.HandleLeftClick(this);
                onLeftClick?.Invoke();
                break;

            case PointerEventData.InputButton.Right:
                MouseUIManager.Instance?.HandleRightClick(this);
                onRightClick?.Invoke();
                break;
        }
    }
}