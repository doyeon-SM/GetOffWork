using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메뉴얼 전체 절차 목록을 ScriptableObject로 정의.
/// ManualStepDefinitionSO 리스트를 Inspector에서 조립한다.
///
/// 사용법:
///   1. Project 창 우클릭 → Create → Manual → Manual Data
///   2. steps 리스트에 ManualStepDefinitionSO 에셋을 순서대로 드래그하여 추가
///   3. 해당 Manual 하위 클래스의 인스펙터에서 manualData 필드에 이 SO를 연결
/// </summary>
[CreateAssetMenu(
    fileName = "ManualData_New",
    menuName  = "Manual/Manual Data",
    order     = 0)]
public class ManualDataSO : ScriptableObject
{
    [Header("메뉴얼 이름 (참조용)")]
    public string manualTitle;

    [Header("정상 응대 보상")]
    [Tooltip("모든 필수 절차를 누락/순서위반 없이 정상 완료했을 때 적용되는 보상")]
    public StepReward completionReward;

    [Header("비정상 반려 패널티")]
    [Tooltip("주소일치인데 반려하거나 인쇄완료 후 반려시도 등 비정상 반려 패널티")]
    public StepPenalty abnormalRejectionPenalty;

    [Header("반려사항 놀침 패널티")]
    [Tooltip("주소불일치인데 반려하지 않고 인쇄/발송을 완료한 경우의 패널티")]
    public StepPenalty missedRejectionPenalty;

    [Header("절차 목록 (순서대로 등록)")]
    [Tooltip("ManualStepDefinitionSO를 순서대로 드래그하여 추가한다.")]
    public List<ManualStepDefinitionSO> steps = new();

    /// <summary>
    /// 이 SO의 steps를 ManualStepEntry 리스트로 변환.
    /// Manual 하위 클래스의 BuildSteps()에서 호출된다.
    /// null 항목은 건너뛴다.
    /// </summary>
    public List<ManualStepEntry> ToStepEntries()
    {
        var result = new List<ManualStepEntry>(steps.Count);
        foreach (var stepSO in steps)
        {
            if (stepSO == null)
            {
                Debug.LogWarning($"[{name}] steps 리스트에 null 항목이 있습니다. 건너뜁니다.");
                continue;
            }
            result.Add(stepSO.ToStepEntry());
        }
        return result;
    }
}
