using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 추가

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
        // 기존 Input → Input System으로 변경
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
                Debug.Log("[ObjectClickRaycaster] UI 위 클릭 → 무시");
            return;
        }

        // 마우스 위치도 Input System 사용
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableLayerMask))
        {
            IClickableObject clickable = hit.collider.GetComponentInParent<IClickableObject>();

            if (clickable != null)
            {
                if (showDebugLog)
                    Debug.Log($"[Raycaster] 클릭 성공: {clickable.GetDisplayName()}");

                clickable.OnClicked();
            }
        }
    }
}