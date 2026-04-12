using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 민원 유형과 Paper UI 프리팹을 연결하는 직렬화 구조체.
/// Inspector에서 ComplaintType 별로 UIPaperView 프리팹을 등록한다.
/// </summary>
[Serializable]
public class PaperViewEntry
{
    public ComplaintContext.ComplaintType complaintType;
    public GameObject                     paperViewPrefab; // UIPaperView 컴포넌트가 붙은 프리팹
}

/// <summary>
/// 전체 조작 영역(ObjectManagerBox).
/// DeskObjectItem 프리팹 Spawn/Destroy, Bounds Clamp, 반납 검사,
/// UI 프리팹 런타임 생성을 담당.
///
/// Paper 처리:
///   - PrinterObject 위치에서 PaperItem 프리팹을 Spawn.
///   - 민원 유형에 맞는 UIPaperView(paperViewMap)를 PaperItem에 주입.
///   - PaperItem이 TakeZone에 드롭되면 스스로 Destroy한다.
///   - OnCustomerCleared 시 남아있는 PaperItem도 강제 정리.
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

    [Header("Paper 스폰 설정")]
    [Tooltip("씬에 배치된 PrinterObject. Paper가 이 오브젝트 위치에서 Spawn된다.")]
    [SerializeField] private PrinterObject printerObject;
    [SerializeField] private Vector3       paperSpawnOffset = Vector3.zero;
    [SerializeField] private GameObject    paperPrefab;  // PaperItem 컴포넌트가 붙은 월드 프리팹

    [Header("Paper UI 프리팹 (민원 유형별, Canvas에 런타임 Instantiate)")]
    [SerializeField] private PaperViewEntry[] paperViewEntries;

    [Header("ID카드 UI 프리팹 (Canvas에 런타임 Instantiate)")]
    [SerializeField] private GameObject idCardViewPrefab;

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
    private BoxCollider2D bounds2D;
    private UIIDCardView  runtimeCardView;
    private bool          idCardSpawned;

    /// <summary>현재 씬에 존재하는 PaperItem 인스턴스 (1회 민원당 최대 1개)</summary>
    private PaperItem     spawnedPaper;

    /// <summary>민원 유형 → 런타임 생성된 UIPaperView 인스턴스 딕셔너리</summary>
    private Dictionary<ComplaintContext.ComplaintType, UIPaperView> paperViewMap;

    /// <summary>
    /// 필수 반납 물품 리스트
    /// </summary>
    private readonly List<DeskObjectItem> userspawnedItems = new List<DeskObjectItem>();
    /// <summary>
    /// 소환된 반납 물품 리스트
    /// </summary>
    private readonly List<DeskObjectItem> playerspawnedItems = new List<DeskObjectItem>();

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

    /// <summary>UI 프리팹들을 씬의 Canvas에 런타임으로 생성한다.</summary>
    private void SpawnUIViews()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{TAG} Canvas를 찾을 수 없습니다.");
            return;
        }

        // IDCard View
        if (idCardViewPrefab != null)
        {
            var go = Instantiate(idCardViewPrefab, canvas.transform);
            runtimeCardView = go.GetComponent<UIIDCardView>();
            if (runtimeCardView == null)
                Debug.LogError($"{TAG} idCardViewPrefab에 UIIDCardView가 없습니다.");
            else
                Log($"{TAG} UIIDCardView 생성 완료");
        }
        else
        {
            Debug.LogWarning($"{TAG} idCardViewPrefab이 비어있습니다.");
        }

        // Paper View 딕셔너리 구축
        paperViewMap = new Dictionary<ComplaintContext.ComplaintType, UIPaperView>();
        if (paperViewEntries != null)
        {
            foreach (var entry in paperViewEntries)
            {
                if (entry.paperViewPrefab == null) continue;

                var go   = Instantiate(entry.paperViewPrefab, canvas.transform);
                var view = go.GetComponent<UIPaperView>();

                if (view == null)
                {
                    Debug.LogError($"{TAG} paperViewPrefab({entry.complaintType})에 UIPaperView가 없습니다.");
                    continue;
                }

                paperViewMap[entry.complaintType] = view;
                Log($"{TAG} UIPaperView({entry.complaintType}) 생성 완료");
            }
        }
    }

    // ── 이벤트 구독 ───────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (serviceDeskManager == null) return;
                serviceDeskManager.OnSpawnIdCardRequested      += HandleSpawnIdCard;
        serviceDeskManager.OnSpawnProxyIdCardRequested  += HandleSpawnProxyIdCard;
        serviceDeskManager.OnPrintDocumentRequested     += HandlePrintDocument;
        serviceDeskManager.OnCustomerCleared            += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null) return;
                serviceDeskManager.OnSpawnIdCardRequested      -= HandleSpawnIdCard;
        serviceDeskManager.OnSpawnProxyIdCardRequested  -= HandleSpawnProxyIdCard;
        serviceDeskManager.OnPrintDocumentRequested     -= HandlePrintDocument;
        serviceDeskManager.OnCustomerCleared            -= HandleCustomerCleared;
    }

    // ── IDCard 스폰 ───────────────────────────────────────────────────────
    private void HandleSpawnIdCard(ComplaintContext complaint)
    {
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

        if (item is IDCardItem idItem)
            idItem.SetComplaint(complaint, serviceDeskManager, runtimeCardView);

        userspawnedItems.Add(item);
        idCardSpawned = true;
        Log($"{TAG} IDCard Spawn 완료");
    }

        // ── ProxyIDCard 스폰 ──────────────────────────────────────────────────
    private void HandleSpawnProxyIdCard(ComplaintContext complaint)
    {
        if (idCardPrefab == null)
        {
            Debug.LogWarning($"{TAG} idCardPrefab이 비어있습니다.");
            return;
        }

        // 방문객 신분증에서 약간 오프셋을 주어 겹치지 않게 스폰
        Vector3 spawnPos = takeZone != null
            ? takeZone.GetCenterWorldPos() + spawnOffsetInTake + new Vector3(0.3f, 0f, 0f)
            : transform.position;

        GameObject go = Instantiate(idCardPrefab, spawnPos, Quaternion.identity);
        DeskObjectItem item = go.GetComponent<DeskObjectItem>();

        if (item == null)
        {
            Debug.LogError($"{TAG} idCardPrefab에 DeskObjectItem이 없습니다.");
            Destroy(go);
            return;
        }

        item.Initialize(this, takeZone, targetCamera);
        item.SetObjectType(DeskObjectType.ProxyIDCard);

        if (item is IDCardItem idItem)
            idItem.SetComplaint(complaint, serviceDeskManager, runtimeCardView);

        userspawnedItems.Add(item);
        Log($"{TAG} ProxyIDCard Spawn 완료");
    }

    // ── Paper 스폰────
    private void HandlePrintDocument(ComplaintContext complaint)
    {
        if (paperPrefab == null)
        {
            Debug.LogWarning($"{TAG} paperPrefab이 비어있습니다.");
            return;
        }

        // 스폰 위치: PrinterObject가 있으면 그 위치, 없으면 TakeZone 중심
        Vector3 spawnPos;
        if (printerObject != null)
            spawnPos = printerObject.transform.position + paperSpawnOffset;
        else if (takeZone != null)
            spawnPos = takeZone.GetCenterWorldPos() + paperSpawnOffset;
        else
            spawnPos = transform.position + paperSpawnOffset;

        GameObject go = Instantiate(paperPrefab, spawnPos, Quaternion.identity);
        PaperItem paper = go.GetComponent<PaperItem>();

        if (paper == null)
        {
            Debug.LogError($"{TAG} paperPrefab에 PaperItem이 없습니다.");
            Destroy(go);
            return;
        }

        paper.Initialize(this, takeZone, targetCamera);

        // 민원 유형에 맞는 UIPaperView 주입
        UIPaperView paperView = null;
        paperViewMap?.TryGetValue(complaint.complaintType, out paperView);
        if (paperView == null)
            Debug.LogWarning($"{TAG} {complaint.complaintType}에 대응하는 UIPaperView가 없습니다.");

        paper.SetData(complaint, serviceDeskManager, paperView, serviceDeskManager.UserDatabase, this);

        playerspawnedItems.Add(paper);
        spawnedPaper = paper;
        Log($"{TAG} Paper Spawn 완료 — {complaint.complaintType}");
    }

    // ── 응대 종료 처리 ────────────────────────────────────────────────────
    private void HandleCustomerCleared()
    {
        foreach (var item in userspawnedItems)
            if (item != null) Destroy(item.gameObject);
        foreach (var item in playerspawnedItems)
            if (item != null) Destroy(item.gameObject);

        userspawnedItems.Clear();
        playerspawnedItems.Clear();
        spawnedPaper  = null;
        idCardSpawned = false;

        runtimeCardView?.Hide();

        // 모든 Paper View 닫기
        if (paperViewMap != null)
            foreach (var kv in paperViewMap)
                kv.Value?.Hide();

        Log($"{TAG} 오브젝트 전체 정리");
    }

    // ── 반납 검사 ────────────────────────────────────────────────────────
    /// <summary>
    /// 호출 버튼 클릭 시 반납 검사.
    /// 모두 반납됐으면 true + Destroy, 미반납 있으면 false + 대사 출력.
    /// </summary>
    public bool TryFinishAndReturn(out bool isRejection)
    {
        isRejection = false;
        Manual manual        = serviceDeskManager?.CurrentManual;
        ComplaintContext ctx = serviceDeskManager?.CurrentComplaint;

        if (manual != null && manual.RequiredReturnItems.Count > 0)
        {
            foreach (var requiredType in manual.RequiredReturnItems)
            {
                bool found = false;
                foreach (var item in userspawnedItems)
                {
                    if (item != null && item.ObjectType == requiredType && item.IsInTakeZone)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    string line = GetRandomLine(returnReminderLines, "신분증 돌려주세요.");
                    Log($"{TAG} 필수 반납 미완료({requiredType}) → 호출 방어: {line}");
                    serviceDeskManager?.BroadcastCustomerText(line);
                    return false;
                }
            }
        }
        else
        {
            foreach (var item in userspawnedItems)
            {
                if (item != null && !item.IsInTakeZone)
                {
                    string line = GetRandomLine(returnReminderLines, "신분증 돌려주세요.");
                    Log($"{TAG} 아이템 미반납 → 호출 방어: {line}");
                    serviceDeskManager?.BroadcastCustomerText(line);
                    return false;
                }
            }
        }

        if (ctx != null && manual != null && !manual.IsCompleted)
        {
            isRejection  = true;
            ctx.rejected = true;
            Log($"{TAG} 반려 처리 판정 — 주소불일치={ctx.isAddressMismatch}");
        }

        Log($"{TAG} 반납 완료 → 응대종료 진행");
        return true;
    }

    public bool TryFinishAndReturn() => TryFinishAndReturn(out _);

    /// <summary>PaperItem이 스스로 Destroy될 때 목록에서 제거하기 위해 호출한다.</summary>
    /*public void UnregisterItem(DeskObjectItem item)
    {
        spawnedItems.Remove(item);
        if (item is PaperItem p && spawnedPaper == p)
            spawnedPaper = null;
    }*/

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
