using System;
using UnityEngine;

/// <summary>
/// 대화 한 줄의 데이터를 담는 구조체
/// </summary>
[Serializable]
public class DialogueLine
{
    [Header("화자 정보")]
    public string speakerName;
    public Sprite speakerPortrait;

    [Header("대사")]
    [TextArea(2, 5)]
    public string dialogueText;

    /// <summary>
    /// UI에서 실제로 표시할 화자 이름.
    /// speakerName이 비어 있으면 PlayerBase의 이름을 자동으로 사용합니다.
    /// </summary>
    public string ResolvedSpeakerName
    {
        get
        {
            if (!string.IsNullOrEmpty(speakerName)) return speakerName;
            if (PlayerBase.Instance != null) return PlayerBase.Instance.PlayerName;
            return string.Empty;
        }
    }
}