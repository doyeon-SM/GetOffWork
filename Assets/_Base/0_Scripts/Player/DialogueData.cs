using UnityEngine;

/// <summary>
/// 대화 데이터 에셋 (ScriptableObject)
/// Project 창에서 우클릭 → Create → Dialogue → Dialogue Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("대화 ID (튜토리얼 연동 등에 활용)")]
    public string dialogueId;

    [Header("대화 목록")]
    public DialogueLine[] lines;
}
