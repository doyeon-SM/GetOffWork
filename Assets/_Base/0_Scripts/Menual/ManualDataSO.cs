using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메뉴얼 전체 데이터를 ScriptableObject로 정의.
/// 절차 목록(steps)과 대사 목록(dialogues)을 Inspector에서 조립한다.
///
/// 사용법:
///   1. Project 창 우클릭 → Create → Manual → Manual Data
///   2. steps 리스트에 ManualStepDefinitionSO 에셋을 순서대로 추가
///   3. dialogues 리스트에 CommandDialogueSO 에셋을 commandId별로 추가
///   4. 해당 Manual 하위 클래스의 manualData 필드에 이 SO를 연결
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

    [Header("반려사항 놓침 패널티")]
    [Tooltip("주소불일치인데 반려하지 않고 인쇄/발송을 완료한 경우의 패널티")]
    public StepPenalty missedRejectionPenalty;

    [Header("대사 목록")]
    [Tooltip("CommandDialogueSO를 commandId별로 등록한다. Manual이 대사를 조회할 때 사용한다.")]
    public List<CommandDialogueSO> dialogues = new();

    [Header("절차 목록 (순서대로 등록)")]
    [Tooltip("ManualStepDefinitionSO를 순서대로 드래그하여 추가한다.")]
    public List<ManualStepDefinitionSO> steps = new();

    // ── 절차 변환 ─────────────────────────────────────────────────────────

    /// <summary>
    /// steps를 ManualStepEntry 리스트로 변환.
    /// Manual 하위 클래스의 BuildSteps()에서 호출된다.
    /// </summary>
    public List<ManualStepEntry> ToStepEntries()
    {
        var result = new List<ManualStepEntry>(steps.Count);
        foreach (var stepSO in steps)
        {
            if (stepSO == null)
            {
                Debug.LogWarning("[" + name + "] steps 리스트에 null 항목이 있습니다. 건너뜁니다.");
                continue;
            }
            result.Add(stepSO.ToStepEntry());
        }
        return result;
    }

    // ── 대사 조회 ─────────────────────────────────────────────────────────

    /// <summary>
    /// commandId에 해당하는 CommandDialogueSO를 반환한다.
    /// 없으면 null.
    /// </summary>
    public CommandDialogueSO FindDialogue(string commandId)
    {
        foreach (var d in dialogues)
            if (d != null && d.commandId == commandId)
                return d;
        return null;
    }

    /// <summary>
    /// commandId + nuisanceType에 맞는 정상 응답 대사를 반환한다.
    /// CommandDialogueSO가 없거나 대사가 비어있으면 null.
    /// </summary>
    public string GetCorrectLine(string commandId, ComplaintContext.NuisanceType nuisanceType)
    {
        var dialogue = FindDialogue(commandId);
        return dialogue?.GetCorrectLine(nuisanceType);
    }

    /// <summary>
    /// commandId + nuisanceType에 맞는 WrongOrder 대사를 반환한다.
    /// CommandDialogueSO가 없거나 대사가 비어있으면 null.
    /// </summary>
    public string GetWrongOrderLine(string commandId, ComplaintContext.NuisanceType nuisanceType)
    {
        var dialogue = FindDialogue(commandId);
        return dialogue?.GetWrongOrderLine(nuisanceType);
    }
}
