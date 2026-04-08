using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 조작 영역(ObjectManagerBox).
/// DeskObjectItem 프리팹 Spawn/Destroy, Bounds Clamp, 반납 검사,
/// UI 프리팹 런타임 생성을 담당.
///
/// UI 처리 방식:
///   씬에 미리 배치하지 않고, Awake 시 Canvas를 찾아 UI 프리팹을 Instantiate한다.
///   이렇게 하면 오브젝트 비활성화로 인한 FindFirstObjectByType 탐색 실패를 방지한다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ObjectManagerBox : MonoBehaviour
{
    private const string TAG = "[ObjBox]";

    [Header("참조")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private TakeObjectZone     takeZone;
    [SerializeField] private Camera             targetCamera;

    [Header("월드 오브젝트 프리팹")]
    [SerializeField] private GameObject idCardPrefab;
    [SerializeField] private Vector3    spawnOffsetInTake = Vector3.zero;

    [Header("UI 프리팹 (Canvas에 런타임 Instantiate)")]
    [SerializeField] private GameObject idCardViewPrefab; // UIIDCardView가 붙은 프리팹

    [Header("중복 제출 방어 대사")]
    [SerializeField] private List<string> alreadySubmittedLines = new List<string>
    {
        "이미 제출했습니다.",
        "방금 드렸잖아요.",
        "이미 드렸는데요."
    };

    [Header("반납 실패 대사")]
    [SerializeField] private List<string> returnReminderLines = new List<string>
    {
        "신분증 돌려주세요.",
        "신분증 주시면 가겠습니다.",
        "잠깐, 신분증이요."
    };

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    // ── 런타임 상태 ───────────────────────────────────────────────────────
    private BoxCollider2D          bounds2D;
    private UIIDCardView           runtimeCardView;   // 런타임 생성된 UI 인스턴스
    private bool                   idCardSpawned;     // 중복 제출 방어 플래그

    private readonly List<DeskObjectItem> spawnedItems = new List<DeskObjectItem>();

    // ── 초기화 ───────────────────────────────────────────────────────────
    private void Awake()
    {
        bounds2D = GetComponent<BoxCollider2D>();

        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        if (targetCamera == null)
            targetCamera = Camera.main;

        SpawnUIViews();
    }

    /// <summary>UI 프리팹을 씬의 Canvas에 런타임으로 생성한다.</summary>
    private void SpawnUIViews()
    {
        if (idCardViewPrefab == null)
        {
            Debug.LogWarning($"{TAG} idCardViewPrefab이 비어있습니다.");
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{TAG} Canvas를 찾을 수 없습니다.");
            return;
        }

        GameObject go = Instantiate(idCardViewPrefab, canvas.transform);
        runtimeCardView = go.GetComponent<UIIDCardView>();

        if (runtimeCardView == null)
            Debug.LogError($"{TAG} idCardViewPrefab에 UIIDCardView가 없습니다.");
        else
            Log($"{TAG} UIIDCardView 생성 완료");
    }

    // ── 이벤트 구독 ───────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnSpawnIdCardRequested += HandleSpawnIdCard;
        serviceDeskManager.OnCustomerCleared      += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnSpawnIdCardRequested -= HandleSpawnIdCard;
        serviceDeskManager.OnCustomerCleared      -= HandleCustomerCleared;
    }

    // ── 스폰 ─────────────────────────────────────────────────────────────
    private void HandleSpawnIdCard(ComplaintContext complaint)
    {
        // ── 중복 제출 방어 ──
        if (idCardSpawned)
        {
            string line = GetRandomLine(alreadySubmittedLines, "이미 제출했습니다.");
            Log($"{TAG} IDCard 중복 제출 시도 → {line}");
            serviceDeskManager?.BroadcastCustomerText(line);
            return;
        }

        if (idCardPrefab == null)
        {
            Debug.LogWarning($"{TAG} idCardPrefab이 비어있습니다.");
            return;
        }

        Vector3 spawnPos = takeZone != null
            ? takeZone.GetCenterWorldPos() + spawnOffsetInTake
            : transform.position;

        GameObject go   = Instantiate(idCardPrefab, spawnPos, Quaternion.identity);
        DeskObjectItem item = go.GetComponent<DeskObjectItem>();

        if (item == null)
        {
            Debug.LogError($"{TAG} idCardPrefab에 DeskObjectItem이 없습니다.");
            Destroy(go);
            return;
        }

        item.Initialize(this, takeZone, targetCamera);

        // IDCardItem에 complaint + UI 참조 주입
        if (item is IDCardItem idItem)
            idItem.SetComplaint(complaint, serviceDeskManager, runtimeCardView);

        spawnedItems.Add(item);
        idCardSpawned = true;
        Log($"{TAG} IDCard Spawn 완료");
    }

    private void HandleCustomerCleared()
    {
        foreach (var item in spawnedItems)
            if (item != null) Destroy(item.gameObject);

        spawnedItems.Clear();
        idCardSpawned = false;

        // UI도 닫기
        runtimeCardView?.Hide();

        Log($"{TAG} 오브젝트 전체 정리");
    }

    // ── 반납 검사 ────────────────────────────────────────────────────────
    /// <summary>
    /// 호출 버튼 클릭 시 반납 검사.
    /// 모두 반납됐으면 true + Destroy, 미반납 있으면 false + 대사 출력.
    /// spawnedItems가 비어있으면 true.
    /// </summary>
    public bool TryFinishAndReturn()
    {
        if (spawnedItems.Count == 0)
            return true;

        var notReturned = new List<DeskObjectItem>();
        foreach (var item in spawnedItems)
            if (item != null && !item.IsInTakeZone)
                notReturned.Add(item);

        if (notReturned.Count > 0)
        {
            string line = GetRandomLine(returnReminderLines, "신분증 돌려주세요.");
            Log($"{TAG} 반납 미완료 {notReturned.Count}개 → {line}");
            serviceDeskManager?.BroadcastCustomerText(line);
            return false;
        }

        foreach (var item in spawnedItems)
            if (item != null) Destroy(item.gameObject);

        spawnedItems.Clear();
        idCardSpawned = false;
        runtimeCardView?.Hide();
        Log($"{TAG} 반납 완료 → 삭제");
        return true;
    }

    public void UnregisterItem(DeskObjectItem item) => spawnedItems.Remove(item);

    // ── Clamp ────────────────────────────────────────────────────────────
    public Vector3 ClampToBounds(Vector3 pos)
    {
        if (bounds2D == null) return pos;
        Bounds b = bounds2D.bounds;
        pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
        pos.y = Mathf.Clamp(pos.y, b.min.y, b.max.y);
        return pos;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private string GetRandomLine(List<string> lines, string fallback)
    {
        if (lines == null || lines.Count == 0) return fallback;
        return lines[UnityEngine.Random.Range(0, lines.Count)];
    }

    private void Log(string msg) { if (showDebugLog) Debug.Log(msg); }
}
