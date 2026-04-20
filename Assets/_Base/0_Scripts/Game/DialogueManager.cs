using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [StoryDialogue] 스토리/설명용 대화 흐름을 전체 관리하는 싱글턴 매니저.
/// CommandDialogueSO(민원 시스템)와는 완전히 별개입니다.
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
        currentData = data;
        lineIndex = 0;
        isPlaying = true;
        dialogueUI.SetPanelVisible(true);
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
        OnDialogueEnd?.Invoke();
    }
}