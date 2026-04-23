using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// ESC 키(Input System)로 옵션 패널을 열고 게임을 일시정지/재개하는 매니저.
/// DontDestroyOnLoad 싱글턴으로, 모든 씬에서 동작한다.
///
/// Input System InputAction 콜백 방식 사용:
///   - Update() 폴링 없음
///   - Time.timeScale = 0 상태에서도 입력 수신 가능 (Input System은 timeScale 영향 없음)
///   - OnEnable/OnDisable에서 Action Enable/Disable 관리
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("옵션 패널 프리팹")]
    [SerializeField] private UIOption optionPanelPrefab;

    [Header("패널을 붙일 Canvas (null=자동 탐색)")]
    [SerializeField] private Transform uiRoot;

    // 0_TitleScene 빌드 인덱스
    private const int TITLE_SCENE_INDEX = 0;

    private UIOption   _optionPanel;
    private bool       _isPaused;

    // Input System: ESC 전용 InputAction
    private InputAction _escAction;

    public bool IsPaused => _isPaused;

    // ── 생명주기 ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // InputAction 생성: 키보드 Escape 바인딩
        _escAction = new InputAction(name: "Pause", type: InputActionType.Button);
        _escAction.AddBinding("<Keyboard>/escape");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // InputAction 활성화 + 콜백 등록
        _escAction.performed += OnEscPerformed;
        _escAction.Enable();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // InputAction 비활성화 + 콜백 해제
        _escAction.performed -= OnEscPerformed;
        _escAction.Disable();
    }

    private void OnDestroy()
    {
        // InputAction은 IDisposable이므로 명시적으로 해제
        _escAction?.Dispose();
    }

    // ── ESC 입력 콜백 ────────────────────────────────────────────────────

    /// <summary>
    /// Input System performed 콜백.
    /// CallbackContext 파라미터가 필요하지만 내용은 사용하지 않는다.
    /// </summary>
    private void OnEscPerformed(InputAction.CallbackContext ctx)
    {
        if (_isPaused) DoResume();
        else           DoPause();
    }

    // ── 씬 전환 대응 ────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 시 패널 참조 초기화 + timeScale 복구
        _optionPanel = null;
        uiRoot       = null; // 다음 씬의 Canvas를 새로 탐색하도록 초기화
        ForceResume();
    }

    // ── 일시정지 / 재개 ─────────────────────────────────────────────────

    public void DoPause()
    {
        if (_isPaused) return;

        BuildPanel();
        if (_optionPanel == null) return;

        _optionPanel.SetupForScene(IsTitle());
        _optionPanel.Open();
        Time.timeScale = 0f;
        _isPaused      = true;
        Debug.Log("[PauseManager] 일시정지 (timeScale=0)");
    }

    public void DoResume()
    {
        if (!_isPaused) return;

        // _isPaused를 먼저 false로 설정해야 Close() 내부에서 재진입했을 때
        // 다시 DoResume()이 호출되어도 가드에 걸려 무한 재귀를 방지할 수 있다.
        _isPaused      = false;
        Time.timeScale = 1f;
        ClosePanelOnly(); // UIOption.Close()를 우회 — 패널 숨김만 수행
        Debug.Log("[PauseManager] 재개 (timeScale=1)");
    }

    /// <summary>
    /// UIOption.Close()를 통하지 않고 패널 오브젝트만 비활성화한다.
    /// DoResume() 안에서 Close() → DoResume() 순환 재귀를 차단하기 위해 사용한다.
    /// </summary>
    private void ClosePanelOnly()
    {
        if (_optionPanel != null)
            _optionPanel.gameObject.SetActive(false);
    }

    private void ForceResume()
    {
        Time.timeScale = 1f;
        _isPaused      = false;
    }

    // ── 게임 포기 ────────────────────────────────────────────────────────

    public void GiveUp()
    {
        ForceResume();
        _optionPanel = null;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ReturnToTitle();
        else
            SceneManager.LoadScene(TITLE_SCENE_INDEX);

        Debug.Log("[PauseManager] 게임 포기 -> 타이틀");
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────

    private void BuildPanel()
    {
        if (_optionPanel != null) return;

        if (optionPanelPrefab == null)
        {
            Debug.LogError("[PauseManager] optionPanelPrefab이 연결되지 않았습니다!");
            return;
        }

        if (uiRoot == null)
        {
            Canvas canvas = FindMainCanvas();
            uiRoot = canvas != null ? canvas.transform : transform;
        }

        _optionPanel = Instantiate(optionPanelPrefab, uiRoot);
        _optionPanel.SetPauseManager(this);
        _optionPanel.gameObject.SetActive(false);
    }

    private bool IsTitle() => SceneManager.GetActiveScene().buildIndex == TITLE_SCENE_INDEX;

    public static Canvas FindMainCanvas()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceCamera ||
                c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        return FindFirstObjectByType<Canvas>(); // 폴백
    }
}