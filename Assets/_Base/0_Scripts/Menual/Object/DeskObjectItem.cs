using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 월드 2D에서 드래그/드롭 가능한 데스크 오브젝트 기반 클래스.
///
/// 동작:
///   - MouseDown → 드래그 시작 대기
///   - dragThreshold 이상 이동 → 드래그 모드
///       → 원본 SpriteRenderer 숨김
///       → 스프라이트 복사본(ghost)이 마우스를 따라다님
///   - MouseUp(드래그 후) → ghost Destroy + 원본 위치 확정 + SpriteRenderer 복원
///   - MouseUp(threshold 미만) → 클릭 판정 → OnItemClicked 호출
///   - 드래그 중 ObjectManagerBox Bounds 밖으로 나가지 못함 (Clamp)
///
/// 하위 클래스에서 OnItemClicked() / OnItemDropped()를 override한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class DeskObjectItem : MonoBehaviour
{
    private const string TAG = "[DeskItem]";

    [Header("드래그 설정")]
    [SerializeField] private float dragThreshold  = 0.1f;   // 월드 단위
    [SerializeField] private float dragZOffset    = -5f;    // 드래그 중 ghost Z 깊이
    [SerializeField] private bool  showDebugLog   = true;

    [Header("드래그 시각 효과")]
    [SerializeField] [Range(0f, 1f)] private float ghostAlpha  = 0.7f;  // ghost 투명도
    [SerializeField] private float ghostScale = 1.05f;                   // ghost 크기 배율

    // ── 런타임 참조 (ObjectManagerBox가 Spawn 시 주입) ──────────────────
    private ObjectManagerBox managerBox;
    private TakeObjectZone   takeZone;
    private Camera           targetCamera;

    // ── 드래그 상태 ───────────────────────────────────────────────────────
    private bool    isBeingDragged;
    private bool    dragStarted;
    private Vector3 dragOffset;
    private Vector3 mouseDownWorldPos;
    private float   originalZ;

    // ── 시각 효과 ─────────────────────────────────────────────────────────
    private GameObject     ghost;            // 마우스를 따라다니는 복사본
    private SpriteRenderer originalRenderer; // 원본 스프라이트 렌더러

    [Header("오브젝트 종류")]
    [SerializeField] private DeskObjectType objectType = DeskObjectType.None;

        public DeskObjectType ObjectType => objectType;
    public void SetObjectType(DeskObjectType type) { objectType = type; }

    public bool IsInTakeZone => takeZone != null && takeZone.Contains(transform.position);

    // ── 초기화 ───────────────────────────────────────────────────────────
    public void Initialize(ObjectManagerBox box, TakeObjectZone zone, Camera cam)
    {
        managerBox      = box;
        takeZone        = zone;
        targetCamera    = cam;
        originalZ       = transform.position.z;
        originalRenderer = GetComponent<SpriteRenderer>();
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
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld  = targetCamera != null
            ? (Vector2)targetCamera.ScreenToWorldPoint(mouseScreen)
            : mouseScreen;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.OverlapPoint(mouseWorld)) return;

        isBeingDragged    = true;
        dragStarted       = false;
        mouseDownWorldPos = new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);
        dragOffset        = transform.position - mouseDownWorldPos;
        Log($"{TAG} 마우스 다운: {gameObject.name}");
    }

    private void OnMouseDrag()
    {
        Vector2 mouseScreen  = Mouse.current.position.ReadValue();
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
            Log($"{TAG} 드래그 시작: {gameObject.name}");
            BeginGhost();
        }

        // ghost를 마우스 위치로 이동 (Clamp 적용)
        Vector3 ghostTarget = new Vector3(
            mouseWorld2D.x + dragOffset.x,
            mouseWorld2D.y + dragOffset.y,
            dragZOffset);

        if (managerBox != null)
            ghostTarget = managerBox.ClampToBounds(ghostTarget);

        if (ghost != null)
            ghost.transform.position = ghostTarget;
    }

    private void OnMouseUp()
    {
        isBeingDragged = false;

        if (dragStarted)
        {
            // ghost 위치를 실제 오브젝트 위치로 확정
            Vector3 dropPos = ghost != null ? ghost.transform.position : transform.position;
            EndGhost();

            // Z 복원
            if (IsInTakeZone && takeZone != null)
                dropPos.z = takeZone.transform.position.z - 1f;
            else
                dropPos.z = originalZ;

            transform.position = dropPos;

            Log($"{TAG} 드롭: {gameObject.name} / TakeZone={IsInTakeZone} / Z={dropPos.z}");
            OnItemDropped();
        }
        else
        {
            Log($"{TAG} 클릭: {gameObject.name}");
            OnItemClicked();
        }

        dragStarted = false;
    }

    // ── 시각 효과 (Ghost) ─────────────────────────────────────────────────

    /// <summary>드래그 시작 시 ghost 생성, 원본 숨김</summary>
    private void BeginGhost()
    {
        // 원본 숨김
        if (originalRenderer != null)
            originalRenderer.enabled = false;

        // ghost 생성
        ghost = new GameObject($"{gameObject.name}_Ghost");
        ghost.transform.position   = transform.position;
        ghost.transform.rotation   = transform.rotation;
        ghost.transform.localScale = transform.localScale * ghostScale;

        // SpriteRenderer 복사
        if (originalRenderer != null)
        {
            var ghostSr    = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sprite         = originalRenderer.sprite;
            ghostSr.color          = new Color(
                originalRenderer.color.r,
                originalRenderer.color.g,
                originalRenderer.color.b,
                ghostAlpha);
            ghostSr.sortingLayerID = originalRenderer.sortingLayerID;
            ghostSr.sortingOrder   = originalRenderer.sortingOrder + 1;
            ghostSr.flipX          = originalRenderer.flipX;
            ghostSr.flipY          = originalRenderer.flipY;
        }
    }

    /// <summary>드롭 시 ghost 제거, 원본 복원</summary>
    private void EndGhost()
    {
        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }

        if (originalRenderer != null)
            originalRenderer.enabled = true;
    }

    private void OnDestroy()
    {
        // 만약 드래그 중에 오브젝트가 파괴되면 ghost도 정리
        if (ghost != null)
            Destroy(ghost);
    }

    // ── 하위 클래스 훅 ────────────────────────────────────────────────────
    protected virtual void OnItemClicked() { }
    protected virtual void OnItemDropped() { }

    public bool IsDragging => isBeingDragged && dragStarted;

    private void Log(string msg) { if (showDebugLog) Debug.Log(msg); }
}
