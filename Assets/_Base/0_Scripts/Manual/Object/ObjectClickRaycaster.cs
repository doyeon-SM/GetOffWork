using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ObjectClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask clickableLayerMask = ~0;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool ignoreWhenPointerOverUI = true;
    [SerializeField] private bool showDebugLog = true;

    private IClickableObject _currentHovered;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        UpdateHover();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryClickObject();
    }

    private void UpdateHover()
    {
        if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetHovered(null);
            return;
        }

        IClickableObject hovered = GetClickableAtMouse();
        SetHovered(hovered);
    }

    private void SetHovered(IClickableObject next)
    {
        if (next == _currentHovered) return;

        _currentHovered?.OnHoverExit();
        _currentHovered = next;
        _currentHovered?.OnHoverEnter();
    }

    private IClickableObject GetClickableAtMouse()
    {
        if (targetCamera == null) return null;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 2D 우선
        RaycastHit2D hit2D = Physics2D.Raycast(
            targetCamera.ScreenToWorldPoint(mousePos),
            Vector2.zero, maxDistance, clickableLayerMask);

        if (hit2D.collider != null)
        {
            DeskObjectItem deskItem = hit2D.collider.GetComponentInParent<DeskObjectItem>();
            if (deskItem != null && deskItem.IsDragging) return null;

            IClickableObject clickable = hit2D.collider.GetComponentInParent<IClickableObject>();
            if (clickable != null) return clickable;
        }

        // 3D fallback
        Ray ray = targetCamera.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit3D, maxDistance, clickableLayerMask))
        {
            DeskObjectItem deskItem = hit3D.collider.GetComponentInParent<DeskObjectItem>();
            if (deskItem != null && deskItem.IsDragging) return null;

            return hit3D.collider.GetComponentInParent<IClickableObject>();
        }

        return null;
    }

    private void TryClickObject()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("[ObjectClickRaycaster] targetCamera가 없습니다.");
            return;
        }

        if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (showDebugLog)
                Debug.Log("[ObjectClickRaycaster] UI 위 클릭 무시");
            return;
        }

        if (_currentHovered != null)
        {
            if (showDebugLog)
                Debug.Log($"[Raycaster] 클릭: {_currentHovered.GetDisplayName()}");
            _currentHovered.OnClicked();
        }
    }
}