using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NuisanceType별 출현 확률 및 패널티를 에디터에서 조절하는 ScriptableObject.
/// ServiceDataManager에 연결하여 사용한다.
///
/// 사용법:
///   Project 창 우클릭 > Create > Game > Nuisance > Nuisance Type Settings
/// </summary>
[CreateAssetMenu(
    fileName = "NuisanceTypeSettings",
    menuName  = "Game/Nuisance/Nuisance Type Settings")]
public class NuisanceTypeSO : ScriptableObject
{
    [SerializeField] private List<NuisanceTypeEntry> entries = new List<NuisanceTypeEntry>();

    /// <summary>
    /// 민원 생성 시 NuisanceType을 랜덤으로 결정한다.
    /// 확률 총합이 100%를 넘으면 먼저 매칭되는 타입이 우선 적용된다.
    /// 아무것도 매칭되지 않으면 None을 반환한다.
    /// </summary>
    public ComplaintContext.NuisanceType RollNuisanceType()
    {
        float roll = UnityEngine.Random.value; // 0.0 ~ 1.0

        foreach (var entry in entries)
        {
            if (entry.nuisanceType == ComplaintContext.NuisanceType.None) continue;
            if (roll < entry.spawnChance)
                return entry.nuisanceType;
            roll -= entry.spawnChance;
        }

        return ComplaintContext.NuisanceType.None;
    }

    /// <summary>지정 타입의 설정을 반환한다. 없으면 기본값을 반환한다.</summary>
    public NuisanceTypeEntry GetEntry(ComplaintContext.NuisanceType type)
    {
        foreach (var entry in entries)
            if (entry.nuisanceType == type)
                return entry;

        return NuisanceTypeEntry.Default(type);
    }

    public IReadOnlyList<NuisanceTypeEntry> Entries => entries;
}

/// <summary>
/// 하나의 NuisanceType에 대한 설정 블록.
/// 에디터에서 타입별로 독립적으로 조절할 수 있다.
/// </summary>
[Serializable]
public class NuisanceTypeEntry
{
    [Tooltip("진상 타입")]
    public ComplaintContext.NuisanceType nuisanceType;

    [Tooltip("0.0 ~ 1.0 (예: 0.1 = 10%)")]
    [Range(0f, 1f)]
    public float spawnChance = 0.1f;

    [Header("응대 중 customerMessage 출력마다 적용되는 패널티")]
    public NuisancePenalty perMessagePenalty;

    [Header("인내심 배율 (1.0 = 기본, 0.5 = 절반)")]
    [Tooltip("이 타입의 민원인이 생성될 때 maxPatience에 곱해지는 배율")]
    [Range(0.1f, 3f)]
    public float patienceMultiplier = 1f;

    [Header("민원 종료 시 추가 패널티 (해당 타입을 상대한 경우)")]
    public NuisancePenalty onFinishPenalty;

    public static NuisanceTypeEntry Default(ComplaintContext.NuisanceType type)
    {
        return new NuisanceTypeEntry
        {
            nuisanceType       = type,
            spawnChance        = 0f,
            perMessagePenalty  = default,
            patienceMultiplier = 1f,
            onFinishPenalty    = default,
        };
    }
}

/// <summary>
/// 진상 민원인과 관련하여 발생하는 스탯 변화량.
/// 양수 = 패널티(나쁜 방향), 음수 = 이득.
/// Stress는 AddStat(Stat.Stress, +amount) 방향이므로 양수가 스트레스 증가이다.
/// </summary>
[Serializable]
public struct NuisancePenalty
{
    [Tooltip("Stress 변화량 (양수 = 증가)")]
    public int stress;

    [Tooltip("Kindness 변화량 (양수 = 감소)")]
    public int kindness;

    [Tooltip("Reliability 변화량 (양수 = 감소)")]
    public int reliability;

    [Tooltip("Performance 변화량 (양수 = 감소)")]
    public int performance;

    public bool IsEmpty =>
        stress == 0 && kindness == 0 && reliability == 0 && performance == 0;

    public NuisancePenalty(int stress = 0, int kindness = 0, int reliability = 0, int performance = 0)
    {
        this.stress      = stress;
        this.kindness    = kindness;
        this.reliability = reliability;
        this.performance = performance;
    }
}
