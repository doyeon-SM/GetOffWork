using UnityEngine;

/// <summary>
/// HomeScene 진입 시 실행되는 아침 시퀀스 관리자.
/// 흐름: background_morning → 신문 표시 → 대화 → background_street + 버튼 활성화
/// </summary>
public class MorningHomeController : MonoBehaviour
{
    [SerializeField] private PlayerBase playerBase;

    [Header("아침 시퀀스 UI")]
    [Tooltip("HomeScene에 배치된 신문 UI")]
    [SerializeField] private UINewspaper newspaperUI;

    [Tooltip("신문 종료 후 자동 발동할 대화 트리거")]
    [SerializeField] private DialogueTrigger introDiaTrigger;

    [Tooltip("버튼/배경 전환을 담당하는 UIHomeController")]
    [SerializeField] private UIHomeController homeController;

    private void Start()
    {
        ResolvePlayerBase();

        if (GameFlowManager.Instance != null && playerBase != null)
            GameFlowManager.Instance.ApplySavedStateToPlayer(playerBase);

        ResolveReferences();
        StartMorningSequence();
    }

    private void ResolvePlayerBase()
    {
        if (playerBase != null) return;
        playerBase = PlayerBase.Instance;
        if (playerBase == null)
            Debug.LogError("[MorningHomeController] PlayerBase Instance가 없습니다!");
    }

    private void ResolveReferences()
    {
        if (homeController == null)
            homeController = FindFirstObjectByType<UIHomeController>();
        if (newspaperUI == null)
            newspaperUI = FindFirstObjectByType<UINewspaper>();
    }

    /// <summary>신문 → 대화 → 완료 순서로 아침 시퀀스를 시작한다.</summary>
    private void StartMorningSequence()
    {
        if (newspaperUI != null)
        {
            // 신문 닫힌 뒤 대화 시작
            newspaperUI.Open(OnNewspaperClosed);
            Debug.Log("[MorningHomeController] 신문 표시");
        }
        else
        {
            // 신문 없으면 바로 대화로
            Debug.LogWarning("[MorningHomeController] UINewspaper 없음 → 대화로 바로 진행");
            TryStartIntroDialogue();
        }
    }

    private void OnNewspaperClosed()
    {
        Debug.Log("[MorningHomeController] 신문 닫힘 → 대화 시작");
        TryStartIntroDialogue();
    }

    private void TryStartIntroDialogue()
    {
        if (introDiaTrigger != null)
        {
            // 대화 종료 이벤트에 완료 콜백 등록
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEnd.AddListener(OnIntroDialogueEnd);

            introDiaTrigger.TryStartDialogue();
        }
        else
        {
            // 대화 트리거 없으면 바로 완료
            Debug.LogWarning("[MorningHomeController] DialogueTrigger 없음 → 시퀀스 완료");
            OnSequenceCompleted();
        }
    }

    private void OnIntroDialogueEnd()
    {
        // 이벤트 중복 방지: 한 번만 받고 해제
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnd.RemoveListener(OnIntroDialogueEnd);

        Debug.Log("[MorningHomeController] 대화 종료 → 시퀀스 완료");
        OnSequenceCompleted();
    }

    private void OnSequenceCompleted()
    {
        homeController?.OnMorningSequenceCompleted();
    }
}