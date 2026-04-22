using UnityEngine;

/// <summary>
/// 튜토리얼 한 단계를 정의하는 ScriptableObject.
/// 어떤 커맨드를 기다리는지, 어떤 대상을 하이라이트할지, 설명 텍스트를 담는다.
/// </summary>
[CreateAssetMenu(fileName = "TutStep_New", menuName = "Game/Tutorial/Tutorial Step", order = 0)]
public class TutorialStepSO : ScriptableObject
{
    [Header("단계 식별")]
    [Tooltip("이 단계를 식별하는 고유 ID")]
    public string stepId;

    [Header("진행 조건")]
    [Tooltip("이 단계를 완료로 인정할 커맨드 ID (ManualCommandIds 상수). 비워두면 커맨드 없이 수동 진행.")]
    public string expectedCommandId;

    [Tooltip("CallDisplay 클릭으로 완료되는 단계인지 (OnCustomerCalled 이벤트 사용)")]
    public bool completedByCall = false;

    [Header("하이라이트 대상")]
    [Tooltip("하이라이트할 대상의 종류")]
    public TutorialHighlightTargetType targetType = TutorialHighlightTargetType.None;

    [Tooltip("하이라이트할 WorldObject의 GameObject 이름 (씬에서 탐색)")]
    public string worldObjectName;

    [Tooltip("하이라이트할 UI 버튼의 commandId (UIQuestionButton에서 탐색)")]
    public string uiCommandId;

    [Tooltip("하이라이트할 임의 UI GameObject 이름 (씬에서 탐색, Monitor 패널 버튼 등)")]
    public string uiGameObjectName;

    [Header("설명 텍스트")]
    [Header("설명 텍스트")]
    [Tooltip("튜토리얼 힌트 텍스트 (화면에 표시)")]
    [TextArea(2, 4)]
    public string hintText;

    [Header("자동 진행")]
    [Tooltip("true이면 일정 시간 후 자동으로 다음 단계로 넘어간다.")]
    public bool  autoAdvance      = false;
    [Tooltip("자동 진행까지 대기 시간 (초)")]
    public float autoAdvanceDelay = 1f;

    [Header("포스트잇 클릭 완료")]
    [Tooltip("Postit 클릭으로 완료되는 단계인지")]
    public bool completedByPostit = false;
}

public enum TutorialHighlightTargetType
{
    None,           // 하이라이트 없음
    WorldObject,    // ClickableWorldObject (CallDisplay, Printer 등)
    QuestionButton, // UIQuestionPanel 내 버튼 (commandId 기준)
    UIGameObject,   // 임의 UI GameObject 이름 기준
}
