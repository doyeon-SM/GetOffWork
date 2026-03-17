using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverHighlightUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.85f, 1f);

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    private void Awake()
    {
        if (targetGraphic != null)
            targetGraphic.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetGraphic != null)
            targetGraphic.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetGraphic != null)
            targetGraphic.color = normalColor;
    }
}