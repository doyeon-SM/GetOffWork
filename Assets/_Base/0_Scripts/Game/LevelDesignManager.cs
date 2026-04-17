using System.Collections.Generic;
using UnityEngine;

// ── 정규 레벨 데이터 ───────────────────────────────────────────────────────────
[System.Serializable]
public class DayLevelData
{
    [Header("적용 Day (GameFlowManager.CurrentDay)")]
    public int targetDay;

    [Header("최대 손님 수 (0이면 PlayerLevel * 3 기본값 사용)")]
    public int maxCustomerPerDay;

    [Header("시간 설정 (초, 0이면 기존 값 유지)")]
    public float morningDuration;
    public float afternoonDuration;

    [Header("이 날 사용할 메뉴얼 스폰 목록")]
    public List<ManualSpawnEntry> manualEntries = new();
}

// ── 이벤트 레벨 데이터 ────────────────────────────────────────────────────────
public enum EventDayType
{
    Weekend,    // 주말
}

[System.Serializable]
public class EventDayLevelData
{
    [Header("이벤트 타입")]
    public EventDayType eventType;

    [Header("최소 등장 Day (이 Day 이상부터 발동 가능)")]
    public int minDay = 3;

    [Header("최대 손님 수 (0이면 기본값 사용)")]
    public int maxCustomerPerDay;

    [Header("오전 시간 (초, 0이면 기존 값 유지)")]
    public float morningDuration;

    [Header("오후 시간 (초, 0이면 기존 값 유지)")]
    public float afternoonDuration;

    [Tooltip("true면 해금된 메뉴얼 전체를 동일 확률로 사용. false면 아래 manualEntries를 사용")]
    public bool useAllUnlockedEqualWeight = true;

    [Header("useAllUnlockedEqualWeight가 false일 때 사용할 목록")]
    public List<ManualSpawnEntry> manualEntries = new();
}

// ── LevelDesignManager ────────────────────────────────────────────────────────
public class LevelDesignManager : MonoBehaviour
{
    private const string TAG = "[LevelDesignManager]";

    public static LevelDesignManager Instance { get; private set; }

    [Header("정규 레벨 리스트 (Day별 설정)")]
    [SerializeField] private List<DayLevelData> dayLevels = new();

    [Header("이벤트 레벨 리스트")]
    [SerializeField] private List<EventDayLevelData> eventLevels = new();

    [Header("참조")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private WorkDayManager     workDayManager;
    [SerializeField] private ManualDataManager  manualDataManager;

    // ── 생명주기 ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResolveReferences();
    }

    // ── 외부 호출 진입점 ──────────────────────────────────────────────────────
    /// <summary>
    /// WorkDayManager.StartMorningWork() 직전에 호출한다.
    /// GameFlowManager.CurrentDay를 읽어 해당 날짜의 레벨 설정을 각 매니저에 주입하고,
    /// 이 날 새로 등장하는 메뉴얼을 GameFlowManager에 해금 등록한다.
    /// </summary>
    public void ApplyLevelForCurrentDay()
    {
        int currentDay = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentDay : 1;

        // 이벤트 레벨 우선 확인
        var eventData = FindEventLevel(currentDay);
        if (eventData != null)
        {
            ApplyEventLevel(eventData, currentDay);
            Debug.Log(TAG + $" [이벤트] Day {currentDay} → {eventData.eventType} 적용");
            return;
        }

        // 정규 레벨 적용
        var dayData = FindDayLevel(currentDay);
        if (dayData != null)
        {
            UnlockManualsForDay(dayData);
            ApplyDayLevel(dayData);
            Debug.Log(TAG + $" [정규] Day {currentDay} 레벨 적용");
        }
        else
        {
            Debug.LogWarning(TAG + $" Day {currentDay}에 해당하는 레벨 데이터가 없습니다. 기존 설정 유지.");
        }
    }

    // ── 정규 레벨 적용 ────────────────────────────────────────────────────────
    private DayLevelData FindDayLevel(int day)
    {
        // targetDay가 정확히 일치하는 항목 우선, 없으면 가장 가까운 이전 날짜
        DayLevelData best = null;
        foreach (var data in dayLevels)
        {
            if (data.targetDay > day) continue;
            if (best == null || data.targetDay > best.targetDay)
                best = data;
        }
        return best;
    }

    /// <summary>
    /// 해당 날짜 DayLevelData의 manualEntries를 GameFlowManager에 해금 등록한다.
    /// targetDay가 currentDay와 정확히 일치하는 날만 새로 해금한다.
    /// </summary>
    private void UnlockManualsForDay(DayLevelData data)
    {
        int currentDay = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentDay : 1;

        // targetDay가 정확히 오늘인 경우에만 신규 해금
        if (data.targetDay != currentDay) return;

        var gfm = GameFlowManager.Instance;
        if (gfm == null) return;

        foreach (var entry in data.manualEntries)
        {
            if (entry.manualData == null) continue;
            bool isNew = gfm.UnlockManual(entry.manualData);
            if (isNew)
                Debug.Log(TAG + $" Day {currentDay} 신규 해금: {entry.manualData.manualTitle}");
        }
    }

    private void ApplyDayLevel(DayLevelData data)
    {
        serviceDeskManager?.SetMaxCustomerPerDay(data.maxCustomerPerDay);
        workDayManager?.SetDurations(data.morningDuration, data.afternoonDuration);
        manualDataManager?.SetEntries(data.manualEntries);
    }

    // ── 이벤트 레벨 적용 ─────────────────────────────────────────────────────
    private EventDayLevelData FindEventLevel(int day)
    {
        foreach (var ev in eventLevels)
        {
            if (day < ev.minDay) continue;
            if (IsEventDay(day, ev.eventType))
                return ev;
        }
        return null;
    }

    private bool IsEventDay(int day, EventDayType type)
    {
        switch (type)
        {
            case EventDayType.Weekend:
                // 1~5 = 평일, 6 = 주말, 7~11 = 평일, 12 = 주말...
                int weekPos = ((day - 1) % 6); // 0~4: 평일, 5: 주말
                return weekPos == 5;
            default:
                return false;
        }
    }

    private void ApplyEventLevel(EventDayLevelData data, int currentDay)
    {
        serviceDeskManager?.SetMaxCustomerPerDay(data.maxCustomerPerDay);
        workDayManager?.SetDurations(data.morningDuration, data.afternoonDuration);

        if (data.useAllUnlockedEqualWeight)
        {
            // GameFlowManager의 해금 목록 전체를 동일 가중치로 사용
            var equalEntries = BuildEqualWeightEntriesFromUnlocked();
            manualDataManager?.SetEntries(equalEntries);
        }
        else
        {
            manualDataManager?.SetEntries(data.manualEntries);
        }
    }

    /// <summary>
    /// GameFlowManager.UnlockedManuals 기반으로
    /// 해금된 메뉴얼 전체를 동일 가중치(1.0f)로 ManualSpawnEntry 리스트를 만든다.
    /// </summary>
    private List<ManualSpawnEntry> BuildEqualWeightEntriesFromUnlocked()
    {
        var result = new List<ManualSpawnEntry>();
        var gfm    = GameFlowManager.Instance;

        if (gfm == null || gfm.UnlockedManuals == null || gfm.UnlockedManuals.Count == 0)
        {
            Debug.LogWarning(TAG + " BuildEqualWeightEntries: 해금된 메뉴얼이 없습니다.");
            return result;
        }

        foreach (var manual in gfm.UnlockedManuals)
        {
            if (manual == null) continue;
            result.Add(new ManualSpawnEntry
            {
                manualData  = manual,
                spawnWeight = 1f
            });
        }

        Debug.Log(TAG + $" 이벤트 데이: 해금 메뉴얼 {result.Count}개 동일 가중치 적용");
        return result;
    }

    // ── 참조 자동 연결 ────────────────────────────────────────────────────────
    private void ResolveReferences()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        if (workDayManager == null)
            workDayManager = FindFirstObjectByType<WorkDayManager>();
        if (manualDataManager == null)
            manualDataManager = FindFirstObjectByType<ManualDataManager>();
    }
}
