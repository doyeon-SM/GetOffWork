using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDialogue : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("화자 정보 UI")]
    [SerializeField] private GameObject speakerInfoRoot;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Image portraitImage;

    [Header("대사 UI")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("진행 버튼 (타이핑 완료 → 표시)")]
    [SerializeField] private Button continueButton;

    [Header("타이핑 속도 (초/글자)")]
    [SerializeField] private float typingSpeed = 0.04f;

    private Coroutine typingRoutine;
    private bool isTyping;
    private string fullText;
    private readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();

    public System.Action OnClickContinue;

    private void Awake()
    {
        continueButton.gameObject.SetActive(false);
        continueButton.onClick.AddListener(OnContinueButtonClicked);
    }

    public void SetPanelVisible(bool v) { dialoguePanel.SetActive(v); }

    public void ShowLine(DialogueLine line)
    {
        bool hasSpeaker = !string.IsNullOrEmpty(line.ResolvedSpeakerName);
        speakerInfoRoot.SetActive(hasSpeaker);
        if (hasSpeaker)
        {
            speakerNameText.text = line.ResolvedSpeakerName;
            bool hasPic = line.speakerPortrait != null;
            portraitImage.gameObject.SetActive(hasPic);
            if (hasPic) portraitImage.sprite = line.speakerPortrait;
        }
        continueButton.gameObject.SetActive(false);
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        fullText = line.dialogueText;
        typingRoutine = StartCoroutine(TypingAnim(fullText));
    }

    // 버튼 클릭 시 호출
    private void OnContinueButtonClicked()
    {
        if (isTyping)
        {
            // 타이핑 중 → 즉시 완성
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;
            isTyping = false;
            dialogueText.text = fullText;
            // 버튼은 그대로 유지 (다음 클릭에 진행)
        }
        else
        {
            // 타이핑 완료 → 다음 대사 또는 대화 종료
            continueButton.gameObject.SetActive(false);
            OnClickContinue?.Invoke();
        }
    }

    private IEnumerator TypingAnim(string text)
    {
        isTyping = true;
        sb.Clear();
        dialogueText.text = string.Empty;
        WaitForSeconds w = new WaitForSeconds(typingSpeed);
        foreach (char c in text) { sb.Append(c); dialogueText.text = sb.ToString(); yield return w; }
        isTyping = false;
        typingRoutine = null;
        continueButton.gameObject.SetActive(true);  // 타이핑 완료 시 버튼 표시
    }
}