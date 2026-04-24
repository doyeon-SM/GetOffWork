using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Canvas 내에서 RectTransform을 마우스 드래그로 이동시킨다.
///
/// [사용법]
/// - 드래그 전체 패널이 대상이면 루트 오브젝트에 부착한다.
/// - 버튼 등 하위 인터랙션이 있으면 드래그 전용 헤더 오브젝트를 만들고
///   해당 오브젝트에 부착한다. dragTarget에 실제로 이동시킬 RectTransform을 연결한다.
///
/// [Canvas 경계 클램핑]
/// clampToCanvas = true 시 Canvas 밖으로 나가지 않는다.
/// </summary>
public class UIDraggable : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Tooltip("실제 이동시킬 RectTransform. 비워두면 이 오브젝트의 RectTransform을 사용한다.")]
    [SerializeField] private RectTransform dragTarget;

    [Tooltip("Canvas 영역 밖으로 나가지 않도록 클램핑한다.")]
    [SerializeField] private bool clampToCanvas = true;

    private RectTransform _target;
    private Canvas        _rootCanvas;
    private RectTransform _canvasRect;
    private Vector2       _dragOffset;

    private void Awake()
    {
        _target      = dragTarget != null ? dragTarget : GetComponent<RectTransform>();
        _rootCanvas  = GetComponentInParent<Canvas>();
        if (_rootCanvas != null)
            _canvasRect = _rootCanvas.GetComponent<RectTransform>();
    }

    // ── 드래그 시작: 클릭 위치와 anchoredPosition의 차이를 오프셋으로 보관 ──────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_target == null || _rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            GetEventCamera(eventData),
            out Vector2 localPoint);

        _dragOffset = _target.anchoredPosition - localPoint;
    }

    // ── 드래그 중: 마우스 위치 + 오프셋으로 anchoredPosition 갱신 ───────────────

    public void OnDrag(PointerEventData eventData)
    {
        if (_target == null || _rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            GetEventCamera(eventData),
            out Vector2 localPoint);

        Vector2 newPos = localPoint + _dragOffset;

        if (clampToCanvas && _canvasRect != null)
            newPos = ClampToCanvas(newPos);

        _target.anchoredPosition = newPos;
    }

    // ── Canvas 경계 클램핑 ────────────────────────────────────────────────────

    private Vector2 ClampToCanvas(Vector2 pos)
    {
        Vector2 canvasHalf  = _canvasRect.rect.size * 0.5f;
        Vector2 targetHalf  = _target.rect.size * 0.5f;
        Vector2 pivot       = _target.pivot;

        // pivot에 따른 실제 오프셋 보정
        float xMin = -canvasHalf.x + targetHalf.x * (1f - pivot.x * 2f + 1f) * 0.5f;
        float xMax =  canvasHalf.x - targetHalf.x * (1f + pivot.x * 2f - 1f) * 0.5f;
        float yMin = -canvasHalf.y + targetHalf.y * (1f - pivot.y * 2f + 1f) * 0.5f;
        float yMax =  canvasHalf.y - targetHalf.y * (1f + pivot.y * 2f - 1f) * 0.5f;

        // pivot (0.5, 0.5) 기준 단순 클램핑
        float halfW = _target.rect.width  * 0.5f;
        float halfH = _target.rect.height * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -canvasHalf.x + halfW, canvasHalf.x - halfW);
        pos.y = Mathf.Clamp(pos.y, -canvasHalf.y + halfH, canvasHalf.y - halfH);

        return pos;
    }

    // ── Canvas RenderMode에 따른 카메라 반환 ─────────────────────────────────

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (_rootCanvas == null) return null;
        return _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;
    }
}
