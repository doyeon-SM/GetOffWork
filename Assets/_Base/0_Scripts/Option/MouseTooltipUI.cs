using TMPro;
using UnityEngine;

public class MouseTooltipUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform root;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("¼³Á¤")]
    [SerializeField] private Vector2 offset = new Vector2(16f, -16f);
    [SerializeField] private Canvas parentCanvas;

    private Camera uiCamera;

    private void Awake()
    {
        Hide();

        if (parentCanvas != null)
        {
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
                parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                uiCamera = parentCanvas.worldCamera;
            }
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        UpdatePosition();
    }

    public void Show(string message)
    {
        if (tooltipText != null)
            tooltipText.text = message;

        gameObject.SetActive(true);
        UpdatePosition();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePosition()
    {
        if (root == null || parentCanvas == null) return;

        Vector2 screenPos = (Vector2)Input.mousePosition + offset;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 localPos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out localPos))
        {
            root.localPosition = localPos;
        }
    }
}