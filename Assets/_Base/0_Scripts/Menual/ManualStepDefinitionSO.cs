using UnityEngine;

/// <summary>
/// 메뉴얼 절차 한 단계를 ScriptableObject로 정의.
/// Inspector에서 commandId, isOrdered, 패널티/보상 수치를 직접 편집할 수 있다.
///
/// 사용법:
///   1. Project 창 우클릭 → Create → Manual → Step Definition
///   2. commandId에 ManualCommandIds의 const 문자열 입력
///   3. ManualDataSO의 steps 리스트에 드래그하여 추가
/// </summary>
[CreateAssetMenu(
    fileName = "Step_New",
    menuName  = "Manual/Step Definition",
    order     = 1)]
public class ManualStepDefinitionSO : ScriptableObject
{
    [Header("절차 식별")]
    [Tooltip("ManualCommandIds의 const 문자열과 일치해야 한다.")]
    public string commandId;

    [Header("순서 강제 여부")]
    [Tooltip("true: 이전 Ordered 단계 완료 후에만 유효 / false: 언제든 수행 가능")]
    public bool isOrdered = true;
    
    [Header("누락 패널티")]
    [Tooltip("이 단계를 아예 건너뛰었을 때 적용되는 패널티")]
    public StepPenalty omissionPenalty;

    [Header("순서 위반 패널티")]
    [Tooltip("isOrdered = true인 단계를 잘못된 순서로 수행했을 때 패널티")]
    public StepPenalty orderPenalty;

    /// <summary>
    /// SO 데이터를 기존 ManualStepEntry 구조체로 변환.
    /// M_FullID.BuildSteps()에서 호출된다.
    /// </summary>
    public ManualStepEntry ToStepEntry()
    {
        return new ManualStepEntry(
            commandId:       commandId,
            isOrdered:       isOrdered,
            omissionPenalty: omissionPenalty,
            orderPenalty:    orderPenalty
        );
    }
}
