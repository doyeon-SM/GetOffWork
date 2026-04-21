using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// [StoryDialogue] 스토리/설명용 대화 흐름을 전체 관리하는 싱글턴 매니저.
/// CommandDialogueSO(민원 시스템)와는 완전히 별개입니다.
/// DontDestroyOnLoad로 씬을 넘어가며, 씬 로드 시 새 UIDialogue를 자동으로 연결합니다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private UIDialogue dialogueUI;

    [Header("이벤트")]
    public UnityEvent OnDialogueStart;
    public UnityEvent OnDialogueEnd;

    private DialogueData currentData;
    private int lineIndex;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        BindUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드될 때마다 호출 — 새 씬의 UIDialogue를 찾아 재연결합니다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindUI();
    }

    /// <summary>
    /// 현재 로드된 씬에서 UIDialogue를 찾아 dialogueUI에 연결합니다.
    /// 직렬화된 참조가 살아 있으면 그대로 사용하고,
    /// null이면 씬에서 새로 탐색합니다.
    /// </summary>
    private void BindUI()
    {
        if (dialogueUI == null)
            dialogueUI = FindFirstObjectByType<UIDialogue>();

        if (dialogueUI == null)
        {
            Debug.LogWarning("[DialogueManager] 현재 씬에서 UIDialogue를 찾지 못했습니다.");
            return;
        }

        dialogueUI.OnClickContinue = HandleContinue;
        dialogueUI.SetPanelVisible(false);
    }

    // ═══════════════════════════════
    //  공개 API
    // ═══════════════════════════════

    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] 대화 데이터가 비어 있습니다.");
            return;
        }

        if (dialogueUI == null) BindUI();
        if (dialogueUI == null)
        {
            Debug.LogError("[DialogueManager] UIDialogue가 없어 대화를 시작할 수 없습니다.");
            return;
        }

        currentData = data;
        lineIndex = 0;
        isPlaying = true;
        dialogueUI.SetPanelVisible(true);
        WorkDayManager.Instance?.PauseTimer();
        OnDialogueStart?.Invoke();
        ShowCurrentLine();
    }

    public void ForceEnd() => FinishDialogue();

    // ═══════════════════════════════
    //  내부 흐름
    // ═══════════════════════════════

    private void ShowCurrentLine()
    {
        if (currentData == null || lineIndex >= currentData.lines.Length)
        {
            FinishDialogue();
            return;
        }
        dialogueUI.ShowLine(currentData.lines[lineIndex]);
        dialogueUI.OnClickContinue = HandleContinue;
    }

    private void HandleContinue()
    {
        lineIndex++;
        ShowCurrentLine();
    }

    private void FinishDialogue()
    {
        isPlaying = false;
        currentData = null;
        dialogueUI.SetPanelVisible(false);
        WorkDayManager.Instance?.ResumeTimer();
        OnDialogueEnd?.Invoke();
    }
}