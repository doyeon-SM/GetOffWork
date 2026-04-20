using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [StoryDialogue] 튜토리얼 단계를 문자열 ID로 관리하는 싱글턴.
/// DialogueTrigger 의 requiredTutorialStep 조건과 함께 사용합니다.
///
/// 사용 예)
///   TutorialManager.Instance.SetStep("INTRO");     // 인트로 대화 발동 허용
///   TutorialManager.Instance.AdvanceStep();          // 다음 단계로
///   TutorialManager.Instance.SetStep("");           // 튜토리얼 해제
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("튜토리얼 단계 ID 목록 (순서대로)")]
    [SerializeField] private string[] steps;

    [Header("이벤트")]
    public UnityEvent<string> OnStepChanged;  // 단계 변경 시 새 stepId 전달

    private int currentStepIndex = 0;

    public string CurrentStep => (steps != null && currentStepIndex < steps.Length)
        ? steps[currentStepIndex] : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>특정 단계 ID로 바로 이동.</summary>
    public void SetStep(string stepId)
    {
        if (steps == null) return;
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == stepId) { currentStepIndex = i; break; }
        }
        OnStepChanged?.Invoke(CurrentStep);
    }

    /// <summary>다음 단계로 전진.</summary>
    public void AdvanceStep()
    {
        if (steps == null || currentStepIndex >= steps.Length - 1) return;
        currentStepIndex++;
        OnStepChanged?.Invoke(CurrentStep);
        Debug.Log($"[TutorialManager] Step → {CurrentStep}");
    }

    /// <summary>튜토리얼 완료 여부.</summary>
    public bool IsComplete => steps == null || currentStepIndex >= steps.Length;
}