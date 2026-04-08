using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 월드 2D에서 드래그/드롭 가능한 데스크 오브젝트 기반 클래스.
///
/// 동작:
///   - MouseDown → 드래그 시작 대기
///   - MouseDown 후 dragThreshold 이상 이동 → 드래그 모드
///   - 드래그 모드에서 MouseUp → 드롭 (TakeObjectZone 위면 반납 판정)
///   - dragThreshold 미만 이동 후 MouseUp → 클릭 판정 → OnItemClicked 호출
///   - 드래그 중 ObjectManagerBox Bounds 밖으로 나가지 못함 (Clamp)
///
/// 하위 클래스에서 OnItemClicked()를 override해서 클릭 동작을 정의한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class DeskObjectItem : MonoBehaviour
{
    private const string TAG = "[DeskItem]";

    [Header("드래그 설정")]
    [SerializeField] private float dragThreshold = 0.1f;   // 월드 단위
    [SerializeField] private float dragZOffset   = -5f;    // 드래그 중 Z 깊이
    [SerializeField] private bool  showDebugLog  = true;

    // ── 런타임 참조 (ObjectManagerBox가 Spawn 시 주입) ──────────────────
    private ObjectManagerBox managerBox;
    private TakeObjectZone   takeZone;
    private Camera           targetCamera;

    // ── 드래그 상태 ───────────────────────────────────────────────────────
    private bool    isBeingDragged;
    private bool    dragStarted;        // threshold를 넘어 실제 드래그 중
    private Vector3 dragOffset;
    private Vector3 mouseDownWorldPos;
    private float   originalZ;

    /// <summary>현재 이 오브젝트가 TakeObjectZone 안에 있는가</summary>
    public bool IsInTakeZone => takeZone != null && takeZone.Contains(transform.position);

    // ── 초기화 ───────────────────────────────────────────────────────────
    /// <summary>ObjectManagerBox가 Spawn 직후 호출해서 참조를 주입한다.</summary>
    public void Initialize(ObjectManagerBox box, TakeObjectZone zone, Camera cam)
    {
        managerBox   = box;
        takeZone     = zone;
        targetCamera = cam;
        originalZ    = transform.position.z;
    }

    // ── 입력 처리 ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            OnMouseDown();

        if (isBeingDragged && Mouse.current.leftButton.isPressed)
            OnMouseDrag();

        if (isBeingDragged && Mouse.current.leftButton.wasReleasedThisFrame)
            OnMouseUp();
    }

    private void OnMouseDown()
    {
        // 이 오브젝트의 Collider2D 위에서 클릭했는지 확인
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld  = targetCamera != null
            ? (Vector2)targetCamera.ScreenToWorldPoint(mouseScreen)
            : mouseScreen;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.OverlapPoint(mouseWorld)) return;

        isBeingDragged   = true;
        dragStarted      = false;
        mouseDownWorldPos = new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);
        dragOffset       = transform.position - mouseDownWorldPos;
        Log($"{TAG} 마우스 다운: {gameObject.name}");
    }

    private void OnMouseDrag()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld2D = targetCamera != null
            ? (Vector2)targetCamera.ScreenToWorldPoint(mouseScreen)
            : mouseScreen;

        Vector3 mouseWorld = new Vector3(mouseWorld2D.x, mouseWorld2D.y, transform.position.z);

        // threshold 초과 시 드래그 시작
        if (!dragStarted)
        {
            if (Vector3.Distance(mouseWorld, mouseDownWorldPos) < dragThreshold)
                return;
            dragStarted = true;
            // 드래그 중 Z를 앞으로 당겨 다른 오브젝트 위에 렌더링
            Vector3 pos = transform.position;
            pos.z = dragZOffset;
            transform.position = pos;
            Log($"{TAG} 드래그 시작: {gameObject.name}");
        }

        Vector3 targetPos = new Vector3(mouseWorld.x + dragOffset.x,
                                         mouseWorld.y + dragOffset.y,
                                         dragZOffset);

        // ObjectManagerBox Bounds 안으로 Clamp
        if (managerBox != null)
            targetPos = managerBox.ClampToBounds(targetPos);

        transform.position = targetPos;
    }

    private void OnMouseUp()
    {
        isBeingDragged = false;

        if (dragStarted)
        {
            // 드롭 — Z를 원래 깊이로 복구
            Vector3 pos = transform.position;
            pos.z = originalZ;
            transform.position = pos;

            Log($"{TAG} 드롭: {gameObject.name} / TakeZone={IsInTakeZone}");
            OnItemDropped();
        }
        else
        {
            // 클릭 판정
            Log($"{TAG} 클릭: {gameObject.name}");
            OnItemClicked();
        }

        dragStarted = false;
    }

    // ── 하위 클래스 훅 ────────────────────────────────────────────────────
    /// <summary>클릭(드래그 없이 MouseUp)됐을 때 호출</summary>
    protected virtual void OnItemClicked() { }

    /// <summary>드롭됐을 때 호출. IsInTakeZone으로 반납 영역 여부를 확인할 수 있다.</summary>
    protected virtual void OnItemDropped() { }

    /// <summary>현재 드래그 중인가 (ObjectClickRaycaster가 참조)</summary>
    public bool IsDragging => isBeingDragged && dragStarted;

    private void Log(string msg) { if (showDebugLog) Debug.Log(msg); }
}
