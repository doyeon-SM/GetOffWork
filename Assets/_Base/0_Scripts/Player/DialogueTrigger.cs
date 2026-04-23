using UnityEngine;

/// <summary>
/// [StoryDialogue] NPC, 오브젝트, 트리거 존 등에 부착해서
/// DialogueManager 를 통해 대화를 시작시키는 컴포넌트.
/// 튜토리얼 단계 조건 체크도 포함.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("대화 데이터")]
    [SerializeField] private DialogueData dialogueData;

    [Header("튜토리얼 연동 (선택)")]
    [Tooltip("비워두면 항상 실행. 입력하면 해당 TutorialStep 에서만 발동.")]
    [SerializeField] private string requiredTutorialStep = "";
    [Tooltip("0이하는 항상 실행. 입력하면 해당 CurrentDay 에서만 발동")]
    [SerializeField] private int requiredDay=0;

    [Header("트리거 설정")]
    [Tooltip("true: 한 번만 발동 / false: 반복 가능")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool triggerOnStart = false;   // 씬 시작 시 자동 발동
    [SerializeField] private bool triggerOnEnter = false;   // OnTriggerEnter 로 발동

    private bool hasTriggered;

    private void Start()
    {
        if (triggerOnStart) TryStartDialogue();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;
        if (other.CompareTag("Player")) TryStartDialogue();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnEnter) return;
        if (other.CompareTag("Player")) TryStartDialogue();
    }

    /// <summary>버튼 연결, 코드, 애니메이션 이벤트 등 어디서든 직접 호출 가능.</summary>
    public void TryStartDialogue()
    {
        if (triggerOnce && hasTriggered) return;
        if (DialogueManager.Instance == null) { Debug.LogWarning("[DialogueTrigger] DialogueManager 없음"); return; }
        if (DialogueManager.Instance.IsPlaying) return;  // 다른 대화 진행 중이면 무시

        // 튜토리얼 단계 조건 체크
        if (!string.IsNullOrEmpty(requiredTutorialStep))
        {
            if (TutorialManager.Instance == null) return;
            if (TutorialManager.Instance.CurrentStep != requiredTutorialStep) return;
        }
        // 요일별 단계 조건 체크
        if(requiredDay > 0)
        {
            if (GameFlowManager.Instance == null) return;
            if (GameFlowManager.Instance.CurrentDay != requiredDay) return;
        }

        hasTriggered = true;
        DialogueManager.Instance.StartDialogue(dialogueData);
    }

    /// <summary>이미 발동된 트리거를 초기화할 때 사용.</summary>
    public void ResetTrigger() => hasTriggered = false;
}