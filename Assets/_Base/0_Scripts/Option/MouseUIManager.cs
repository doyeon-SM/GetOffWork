using UnityEngine;

public class MouseUIManager : MonoBehaviour
{
    public static MouseUIManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private MouseTooltipUI tooltipUI;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private MouseInteractableUI currentHover;

    public MouseInteractableUI CurrentHover => currentHover;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void HandleHoverEnter(MouseInteractableUI target)
    {
        if (target == null) return;

        currentHover = target;

        if (debugLog)
            Debug.Log($"[MouseUI] Hover Enter : {target.name}");

        if (tooltipUI != null && target.UseTooltip)
        {
            string text = string.IsNullOrEmpty(target.TooltipText)
                ? target.DisplayName
                : target.TooltipText;

            tooltipUI.Show(text);
        }
    }

    public void HandleHoverExit(MouseInteractableUI target)
    {
        if (target == null) return;

        if (currentHover == target)
            currentHover = null;

        if (debugLog)
            Debug.Log($"[MouseUI] Hover Exit : {target.name}");

        tooltipUI?.Hide();
    }

    public void HandleLeftClick(MouseInteractableUI target)
    {
        if (target == null) return;

        if (debugLog)
            Debug.Log($"[MouseUI] Left Click : {target.name}");
    }

    public void HandleRightClick(MouseInteractableUI target)
    {
        if (target == null) return;

        if (debugLog)
            Debug.Log($"[MouseUI] Right Click : {target.name}");
    }
}