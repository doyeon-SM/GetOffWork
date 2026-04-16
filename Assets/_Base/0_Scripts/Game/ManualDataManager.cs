using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ManualSpawnEntry
{
    public ManualDataSO manualData;
    [Range(0f, 1f)]
    public float spawnWeight;
}

public class ManualDataManager : MonoBehaviour
{
    public static ManualDataManager Instance { get; private set; }

    [Header("메뉴얼 스폰 목록")]
    [SerializeField] private List<ManualSpawnEntry> manualEntries = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// spawnWeight 기반 가중치 랜덤으로 ManualDataSO를 반환합니다.
    /// 모든 weight가 0이거나 목록이 비어있으면 null을 반환합니다.
    /// </summary>
    public ManualDataSO GetRandomManual()
    {
        float totalWeight = 0f;
        foreach (var entry in manualEntries)
        {
            if (entry.manualData != null)
                totalWeight += entry.spawnWeight;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[ManualDataManager] 유효한 spawnWeight가 없습니다.");
            return null;
        }

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in manualEntries)
        {
            if (entry.manualData == null) continue;
            cumulative += entry.spawnWeight;
            if (random <= cumulative)
                return entry.manualData;
        }

        return null;
    }

    /// <summary>
    /// 등록된 전체 ManualSpawnEntry 목록을 반환합니다.
    /// </summary>
    public IReadOnlyList<ManualSpawnEntry> GetAllEntries() => manualEntries;
}
