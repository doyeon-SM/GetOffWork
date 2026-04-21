using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 한 가지 흐름(Print 또는 Mobile)의 단계를 순서대로 담는 컨테이너 SO.
/// Print 흐름과 Mobile 흐름은 각각 별도 SO로 분리한다.
/// 공통 단계(손님 호출~발급방식 선택)는 두 SO 모두에 동일하게 등록한다.
/// </summary>
[CreateAssetMenu(fileName = "TutorialFlow_New", menuName = "Game/Tutorial/Tutorial Flow", order = 1)]
public class TutorialFlowSO : ScriptableObject
{
    [Header("흐름 이름 (참조용)")]
    public string flowName;

    [Header("튜토리얼 단계 목록 (순서대로)")]
    [Tooltip("위에서 아래 순서대로 실행된다.")]
    public List<TutorialStepSO> steps = new();

    /// <summary>stepId로 단계 인덱스를 찾는다.</summary>
    public int FindStepIndex(string stepId)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].stepId == stepId)
            {
                Debug.Log($"[TutorialFlowSO] FindStepIndex() stepId = {stepId} | stepId index = {i}");
                return i;
            }            
        }
        return -1;
    }
}
