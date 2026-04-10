using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // �߰�

public class ObjectClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask clickableLayerMask = ~0;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool ignoreWhenPointerOverUI = true;
    [SerializeField] private bool showDebugLog = true;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        // ���� Input �� Input System���� ����
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryClickObject();
        }
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

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        // 2D Physics 우선 시도
        RaycastHit2D hit2D = Physics2D.Raycast(
            targetCamera.ScreenToWorldPoint(mousePosition),
            Vector2.zero, maxDistance, clickableLayerMask);

        if (hit2D.collider != null)
        {
            // DeskObjectItem이 드래그 중이면 클릭 이벤트를 전달하지 않음
            DeskObjectItem deskItem = hit2D.collider.GetComponentInParent<DeskObjectItem>();
            if (deskItem != null && deskItem.IsDragging) return;

            IClickableObject clickable = hit2D.collider.GetComponentInParent<IClickableObject>();
            if (clickable != null)
            {
                if (showDebugLog)
                    Debug.Log($"[Raycaster] 2D 클릭: {clickable.GetDisplayName()}");
                clickable.OnClicked();
                return;
            }
        }

        // 3D fallback
        if (Physics.Raycast(ray, out RaycastHit hit3D, maxDistance, clickableLayerMask))
        {
            DeskObjectItem deskItem = hit3D.collider.GetComponentInParent<DeskObjectItem>();
            if (deskItem != null && deskItem.IsDragging) return;

            IClickableObject clickable = hit3D.collider.GetComponentInParent<IClickableObject>();
            if (clickable != null)
            {
                if (showDebugLog)
                    Debug.Log($"[Raycaster] 3D 클릭: {clickable.GetDisplayName()}");
                clickable.OnClicked();
            }
        }
    }
}