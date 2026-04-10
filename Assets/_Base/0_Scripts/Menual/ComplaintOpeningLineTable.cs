using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 민원인이 입장할 때 첫 마디로 할 대사 한 줄.
/// 같은 민원 유형/신청인 유형이라도 여러 대사를 등록해 랜덤 출력할 수 있다.
/// </summary>
[Serializable]
public class ComplaintOpeningLine
{
    [Tooltip("민원 유형")]
    public ComplaintContext.ComplaintType complaintType;

    [Tooltip("신청인 유형 (본인 / 대리)")]
    public ComplaintContext.ApplicantType applicantType;

    [Tooltip("진상 유형")]
    public ComplaintContext.NuisanceType nuisanceType;

    [Tooltip("대사 후보 목록 (비어있으면 사용 안 함)")]
    public List<string> lines = new List<string>();
}

/// <summary>
/// 민원 입장 대사 전체 테이블. ScriptableObject로 에디터에서 관리한다.
/// ServiceDeskManager의 openingLineTable 필드에 연결한다.
///
/// 사용법:
///   Project 창 우클릭 > Create > Game > Dialogue > Complaint Opening Lines
/// </summary>
[CreateAssetMenu(
    fileName = "ComplaintOpeningLineTable",
    menuName  = "Game/Dialogue/Complaint Opening Lines")]
public class ComplaintOpeningLineTable : ScriptableObject
{
    [SerializeField] private List<ComplaintOpeningLine> entries = new List<ComplaintOpeningLine>();

    /// <summary>
    /// 민원 유형과 신청인 유형에 맞는 대사 후보 중 하나를 랜덤으로 반환한다.
    /// 후보가 없으면 null을 반환한다.
    /// </summary>
    public string GetLine(
        ComplaintContext.ComplaintType complaintType,
        ComplaintContext.ApplicantType applicantType,
        ComplaintContext.NuisanceType nuisanceType)
    {
        // 조건에 맞는 entry 수집
        var candidates = new List<string>();

        foreach (var entry in entries)
        {
            if (entry.complaintType != complaintType) continue;
            if (entry.applicantType != applicantType) continue;
            if (entry.nuisanceType != nuisanceType) continue;
            foreach (var line in entry.lines)
                if (!string.IsNullOrWhiteSpace(line))
                    candidates.Add(line);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>특정 entry의 모든 대사 목록 반환 (에디터 도구용)</summary>
    public IReadOnlyList<ComplaintOpeningLine> Entries => entries;
}
