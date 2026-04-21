using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 하이라이트 오버레이를 관리한다.
/// - WorldObject: ClickableWorldObject.OnHoverEnter/Exit를 강제 호출
/// - QuestionButton: UIQuestionButton 위에 노란 테두리 Image를 오버레이
/// - UIGameObject: 씬에서 이름으로 찾은 GameObject 위에 오버레이
/// </summary>
public class TutorialHighlighter : MonoBehaviour
{
    public static TutorialHighlighter Instance { get; private set; }

    [Header("UI 하이라이트 오버레이 설정")]
    [Tooltip("Canvas 안에 생성할 하이라이트 테두리 프리팹 (Image 컴포넌트 포함). null이면 자동 생성.")]
    [SerializeField] private GameObject highlightOverlayPrefab;

    [Tooltip("오버레이를 올릴 Canvas (null이면 씬에서 자동 탐색)")]
    [SerializeField] private Canvas targetCanvas;

    [Header("하이라이트 색상")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.016f, 1f); // 노란색
    [SerializeField] private float borderThickness = 4f;

    [Header("펄스 애니메이션")]
    [SerializeField] private bool usePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinAlpha = 0.4f;
    [SerializeField] private float pulseMaxAlpha = 1f;

    // 현재 하이라이트 중인 WorldObject
    private ClickableWorldObject _currentWorldObj;

    // 생성된 UI 오버레이 목록
    private readonly List<GameObject> _overlays = new();
    private Coroutine _pulseCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();
    }

    // ── 공개 API ──────────────────────────────────────────────────────────

    /// <summary>TutorialStepSO에 정의된 대상을 하이라이트한다.</summary>
    public void Highlight(TutorialStepSO step)
    {
        ClearAll();
        if (step == null) return;

        switch (step.targetType)
        {
            case TutorialHighlightTargetType.WorldObject:
                HighlightWorldObject(step.worldObjectName);
                break;
            case TutorialHighlightTargetType.QuestionButton:
                HighlightQuestionButton(step.uiCommandId);
                break;
            case TutorialHighlightTargetType.UIGameObject:
                HighlightUIGameObject(step.uiGameObjectName);
                break;
            case TutorialHighlightTargetType.None:
            default:
                break;
        }
    }

    /// <summary>모든 하이라이트를 제거한다.</summary>
    public void ClearAll()
    {
        // WorldObject 하이라이트 해제
        if (_currentWorldObj != null)
        {
            _currentWorldObj.OnHoverExit();
            _currentWorldObj = null;
        }

        // UI 오버레이 제거
        foreach (var ov in _overlays)
            if (ov != null) Destroy(ov);
        _overlays.Clear();

        // 펄스 중단
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    // ── WorldObject 하이라이트 ────────────────────────────────────────────

    private void HighlightWorldObject(string goName)
    {
        if (string.IsNullOrEmpty(goName)) return;

        var all = FindObjectsByType<ClickableWorldObject>(FindObjectsSortMode.None);
        foreach (var obj in all)
        {
            if (obj.gameObject.name == goName)
            {
                _currentWorldObj = obj;
                obj.OnHoverEnter();
                return;
            }
        }
        Debug.LogWarning($"[TutorialHighlighter] WorldObject '{goName}' 를 찾지 못했습니다.");
    }

    // ── QuestionButton 하이라이트 ─────────────────────────────────────────

    private void HighlightQuestionButton(string commandId)
    {
        if (string.IsNullOrEmpty(commandId)) return;

        // UIQuestionButton은 동적으로 생성되므로 매 프레임 탐색 대신 지연 탐색
        StartCoroutine(FindAndHighlightQuestionButton(commandId));
    }

    private IEnumerator FindAndHighlightQuestionButton(string commandId)
    {
        // QuestionPanel이 열릴 때까지 최대 2초 대기
        float waited = 0f;
        UIQuestionButton found = null;

        while (waited < 2f)
        {
            var buttons = FindObjectsByType<UIQuestionButton>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn.CommandId == commandId)
                {
                    found = btn;
                    break;
                }
            }
            if (found != null) break;
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }

        if (found == null)
        {
            Debug.LogWarning($"[TutorialHighlighter] QuestionButton commandId='{commandId}' 를 찾지 못했습니다.");
            yield break;
        }

        var rectTransform = found.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        CreateUIOverlay(rectTransform);
    }

    // ── UIGameObject 하이라이트 ───────────────────────────────────────────

    private void HighlightUIGameObject(string goName)
    {
        if (string.IsNullOrEmpty(goName)) return;
        StartCoroutine(FindAndHighlightUIGO(goName));
    }

    private IEnumerator FindAndHighlightUIGO(string goName)
    {
        float waited = 0f;
        GameObject found = null;

        while (waited < 2f)
        {
            found = GameObject.Find(goName);
            if (found != null) break;
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }

        if (found == null)
        {
            Debug.LogWarning($"[TutorialHighlighter] UIGameObject '{goName}' 를 찾지 못했습니다.");
            yield break;
        }

        var rect = found.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning($"[TutorialHighlighter] '{goName}' 에 RectTransform이 없습니다.");
            yield break;
        }

        CreateUIOverlay(rect);
    }

    // ── UI 오버레이 생성 ──────────────────────────────────────────────────

    private void CreateUIOverlay(RectTransform targetRect)
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("[TutorialHighlighter] targetCanvas가 없어 오버레이를 생성할 수 없습니다.");
            return;
        }

        GameObject overlay;

        if (highlightOverlayPrefab != null)
        {
            overlay = Instantiate(highlightOverlayPrefab, targetCanvas.transform);
        }
        else
        {
            // 프리팹 없으면 코드로 생성 (4개의 테두리 Image)
            overlay = BuildBorderOverlay(targetRect);
        }

        if (overlay == null) return;

        // 대상 RectTransform과 위치/크기 동기화
        var overlayRect = overlay.GetComponent<RectTransform>();
        if (overlayRect != null)
            SyncOverlayToTarget(overlayRect, targetRect);

        _overlays.Add(overlay);

        // 펄스 시작
        if (usePulse && _pulseCoroutine == null)
            _pulseCoroutine = StartCoroutine(PulseOverlays());
    }

    /// <summary>4개의 얇은 Image로 테두리 오버레이를 코드로 생성한다.</summary>
    private GameObject BuildBorderOverlay(RectTransform targetRect)
    {
        var root = new GameObject("TutHighlightBorder", typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);

        // 대상 rectTransform의 월드 코너를 Canvas 로컬로 변환
        var corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        // Canvas의 RectTransform으로 로컬 변환
        var canvasRect = targetCanvas.GetComponent<RectTransform>();
        for (int i = 0; i < 4; i++)
            corners[i] = canvasRect.InverseTransformPoint(corners[i]);

        float left   = corners[0].x;
        float bottom = corners[0].y;
        float right  = corners[2].x;
        float top    = corners[2].y;
        float w = right - left;
        float h = top - bottom;
        float cx = left + w * 0.5f;
        float cy = bottom + h * 0.5f;

        // 상/하/좌/우 테두리 4개
        CreateBorderSegment(root.transform, "Top",    new Vector2(cx, top    - borderThickness * 0.5f), new Vector2(w + borderThickness * 2, borderThickness));
        CreateBorderSegment(root.transform, "Bottom", new Vector2(cx, bottom + borderThickness * 0.5f), new Vector2(w + borderThickness * 2, borderThickness));
        CreateBorderSegment(root.transform, "Left",   new Vector2(left  + borderThickness * 0.5f, cy), new Vector2(borderThickness, h));
        CreateBorderSegment(root.transform, "Right",  new Vector2(right - borderThickness * 0.5f, cy), new Vector2(borderThickness, h));

        // 루트는 RaycastTarget 끄기
        root.transform.SetAsLastSibling();
        return root;
    }

    private void CreateBorderSegment(Transform parent, string segName, Vector2 center, Vector2 size)
    {
        var go  = new GameObject(segName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var img        = go.GetComponent<Image>();
        img.color      = highlightColor;
        img.raycastTarget = false;

        var rect       = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta        = size;
    }

    private void SyncOverlayToTarget(RectTransform overlayRect, RectTransform targetRect)
    {
        // 프리팹 기반 오버레이일 때 위치/크기 동기화
        var corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        var canvasRect = targetCanvas.GetComponent<RectTransform>();
        for (int i = 0; i < 4; i++)
            corners[i] = canvasRect.InverseTransformPoint(corners[i]);

        float w = corners[2].x - corners[0].x;
        float h = corners[2].y - corners[0].y;
        float cx = corners[0].x + w * 0.5f;
        float cy = corners[0].y + h * 0.5f;

        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.zero;
        overlayRect.pivot     = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = new Vector2(cx, cy);
        overlayRect.sizeDelta        = new Vector2(w, h);
        overlayRect.SetAsLastSibling();
    }

    // ── 펄스 애니메이션 ───────────────────────────────────────────────────

    private IEnumerator PulseOverlays()
    {
        while (true)
        {
            float t     = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);

            foreach (var ov in _overlays)
            {
                if (ov == null) continue;
                foreach (var img in ov.GetComponentsInChildren<Image>())
                {
                    var c = img.color;
                    c.a       = alpha;
                    img.color = c;
                }
            }
            yield return null;
        }
    }
}
