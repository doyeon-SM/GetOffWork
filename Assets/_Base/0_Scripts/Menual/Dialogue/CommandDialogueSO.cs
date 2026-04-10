using System;
using System.Collections.Generic;
using UnityEngine;

// ── 진상 타입별 대사 블록 ──────────────────────────────────────────────────
/// <summary>
/// 특정 NuisanceType에 대응하는 대사 세트.
/// correctLines / wrongOrderLines 각각 랜덤 풀로 관리한다.
/// </summary>
[Serializable]
public class NuisanceDialogueEntry
{
    [Tooltip("이 대사 블록이 적용될 진상 타입")]
    public ComplaintContext.NuisanceType nuisanceType;

    [Tooltip("정상 응답 대사 풀 (랜덤 선택)")]
    public List<string> correctLines = new List<string>();

    [Tooltip("잘못된 순서/상태 대사 풀 (랜덤 선택)")]
    public List<string> wrongOrderLines = new List<string>();
}

// ── 커맨드 대사 SO ────────────────────────────────────────────────────────
/// <summary>
/// CommandId 하나에 대응하는 모든 대사를 관리하는 ScriptableObject.
///
/// 대사 4종:
///   correctLines      : 일반 정상 응답 대사 풀
///   wrongOrderLines   : 일반 WrongOrder 대사 풀
///   nuisanceDialogues : 진상 타입별 대사 블록 (correctLines + wrongOrderLines 각각)
///
/// 조회 우선순위:
///   진상 타입 대사 있으면 → 해당 타입 사용
///   없으면              → 일반 대사 사용
///   둘 다 없으면        → null 반환 (호출측 폴백 처리)
///
/// 사용법:
///   Project 우클릭 → Create → Manual/Dialogue → Command Dialogue
/// </summary>
[CreateAssetMenu(
    fileName = "Dialogue_CommandId",
    menuName  = "Manual/Dialogue/Command Dialogue",
    order     = 10)]
public class CommandDialogueSO : ScriptableObject
{
    [Header("대상 커맨드 ID")]
    [Tooltip("ManualCommandIds의 const 문자열과 일치해야 한다.")]
    public string commandId;

    [Header("일반 대사")]
    [Tooltip("정상 응답 대사 풀 (랜덤 선택). 비어있으면 null 반환.")]
    public List<string> correctLines = new List<string>();

    [Tooltip("잘못된 순서/상태 대사 풀 (랜덤 선택). 비어있으면 null 반환.")]
    public List<string> wrongOrderLines = new List<string>();

    [Header("진상 타입별 대사")]
    [Tooltip("NuisanceType마다 독립적인 correctLines / wrongOrderLines를 설정한다.")]
    public List<NuisanceDialogueEntry> nuisanceDialogues = new List<NuisanceDialogueEntry>();

    // ── 조회 API ─────────────────────────────────────────────────────────

    /// <summary>
    /// 정상 응답 대사를 반환한다.
    /// nuisanceType에 맞는 블록이 있으면 우선 사용, 없으면 일반 대사 풀 사용.
    /// 후보가 없으면 null.
    /// </summary>
    public string GetCorrectLine(ComplaintContext.NuisanceType nuisanceType)
    {
        // 진상 타입 대사 우선
        var nuisanceEntry = FindNuisanceEntry(nuisanceType);
        if (nuisanceEntry != null && nuisanceEntry.correctLines.Count > 0)
            return PickRandom(nuisanceEntry.correctLines);

        // 일반 대사 폴백
        if (correctLines.Count > 0)
            return PickRandom(correctLines);

        return null;
    }

    /// <summary>
    /// WrongOrder 대사를 반환한다.
    /// nuisanceType에 맞는 블록이 있으면 우선 사용, 없으면 일반 대사 풀 사용.
    /// 후보가 없으면 null.
    /// </summary>
    public string GetWrongOrderLine(ComplaintContext.NuisanceType nuisanceType)
    {
        var nuisanceEntry = FindNuisanceEntry(nuisanceType);
        if (nuisanceEntry != null && nuisanceEntry.wrongOrderLines.Count > 0)
            return PickRandom(nuisanceEntry.wrongOrderLines);

        if (wrongOrderLines.Count > 0)
            return PickRandom(wrongOrderLines);

        return null;
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    private NuisanceDialogueEntry FindNuisanceEntry(ComplaintContext.NuisanceType nuisanceType)
    {
        if (nuisanceType == ComplaintContext.NuisanceType.None) return null;
        foreach (var entry in nuisanceDialogues)
            if (entry.nuisanceType == nuisanceType)
                return entry;
        return null;
    }

    private static string PickRandom(List<string> lines)
    {
        if (lines == null || lines.Count == 0) return null;
        var filtered = lines.FindAll(l => !string.IsNullOrWhiteSpace(l));
        if (filtered.Count == 0) return null;
        return filtered[UnityEngine.Random.Range(0, filtered.Count)];
    }
}
