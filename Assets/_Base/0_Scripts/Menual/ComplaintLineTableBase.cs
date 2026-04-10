using System;
using System.Collections.Generic;
using UnityEngine;

// ── 공통 라인 엔트리 ──────────────────────────────────────────────────────
/// <summary>
/// 민원 컨텍스트(complaintType + applicantType + nuisanceType) 조합에
/// 대응하는 대사 풀 하나.
/// ComplaintOpeningLineTable / ComplaintClosingLineTable 공통으로 사용한다.
/// </summary>
[Serializable]
public class ComplaintLineEntry
{
    [Tooltip("민원 유형")]
    public ComplaintContext.ComplaintType complaintType;

    [Tooltip("신청인 유형 (본인 / 대리)")]
    public ComplaintContext.ApplicantType applicantType;

    [Tooltip("진상 유형 (None = 일반)")]
    public ComplaintContext.NuisanceType nuisanceType;

    [Tooltip("대사 후보 목록. 랜덤 선택. 비어있으면 사용 안 함.")]
    public List<string> lines = new List<string>();
}

// ── 공통 베이스 ScriptableObject ──────────────────────────────────────────
/// <summary>
/// 입장/퇴장 대사 테이블의 공통 베이스.
/// complaintType + applicantType + nuisanceType 세 키로 대사를 조회한다.
///
/// 조회 우선순위:
///   1. complaintType + applicantType + nuisanceType 완전 일치
///   2. 없으면 nuisanceType = None으로 폴백
///   3. 둘 다 없으면 null
/// </summary>
public abstract class ComplaintLineTableBase : ScriptableObject
{
    [SerializeField] protected List<ComplaintLineEntry> entries = new List<ComplaintLineEntry>();

    /// <summary>
    /// 세 키에 맞는 대사를 랜덤으로 반환한다.
    /// nuisanceType 일치 항목이 없으면 None 항목으로 폴백.
    /// 후보가 없으면 null.
    /// </summary>
    public string GetLine(
        ComplaintContext.ComplaintType complaintType,
        ComplaintContext.ApplicantType applicantType,
        ComplaintContext.NuisanceType  nuisanceType)
    {
        // 1차: nuisanceType 완전 일치
        var result = CollectLines(complaintType, applicantType, nuisanceType);
        if (result.Count > 0)
            return result[UnityEngine.Random.Range(0, result.Count)];

        // 2차: None 폴백 (진상 타입 전용 대사가 없을 때)
        if (nuisanceType != ComplaintContext.NuisanceType.None)
        {
            result = CollectLines(complaintType, applicantType, ComplaintContext.NuisanceType.None);
            if (result.Count > 0)
                return result[UnityEngine.Random.Range(0, result.Count)];
        }

        return null;
    }

    private List<string> CollectLines(
        ComplaintContext.ComplaintType complaintType,
        ComplaintContext.ApplicantType applicantType,
        ComplaintContext.NuisanceType  nuisanceType)
    {
        var candidates = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.complaintType != complaintType) continue;
            if (entry.applicantType != applicantType) continue;
            if (entry.nuisanceType  != nuisanceType)  continue;
            foreach (var line in entry.lines)
                if (!string.IsNullOrWhiteSpace(line))
                    candidates.Add(line);
        }
        return candidates;
    }

    /// <summary>에디터 도구용 — 전체 엔트리 읽기 전용 접근</summary>
    public IReadOnlyList<ComplaintLineEntry> Entries => entries;
}
