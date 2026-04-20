using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 신문 이미지 데이터 에셋 (ScriptableObject).
/// Project 창 우클릭 → Create → Newspaper → Newspaper Data 로 생성.
///
/// 우선순위: EventType 항목 먼저 탐색 → 없으면 Day 항목 탐색 → 없으면 defaultSprite.
/// </summary>
[CreateAssetMenu(fileName = "NewspaperData", menuName = "Newspaper/Newspaper Data")]
public class NewspaperData : ScriptableObject
{
    [Header("기본 이미지 (day/event 매칭 없을 때)")]
    public Sprite defaultSprite;

    [Header("Day별 이미지 목록")]
    public List<DayNewspaperEntry> dayEntries = new List<DayNewspaperEntry>();

    [Header("이벤트 타입별 이미지 목록")]
    public List<EventNewspaperEntry> eventEntries = new List<EventNewspaperEntry>();

    /// <summary>CurrentDay와 eventType을 받아 표시할 Sprite를 반환.</summary>
    public Sprite Resolve(int currentDay, EventDayType? eventType)
    {
        if (eventType.HasValue)
        {
            foreach (var e in eventEntries)
                if (e.eventType == eventType.Value && e.sprite != null)
                    return e.sprite;
        }

        DayNewspaperEntry best = null;
        foreach (var d in dayEntries)
        {
            if (d.targetDay > currentDay) continue;
            if (best == null || d.targetDay > best.targetDay) best = d;
        }
        if (best != null && best.sprite != null) return best.sprite;

        return defaultSprite;
    }

    /// <summary>CurrentDay와 eventType을 받아 표시할 헤드라인 텍스트를 반환.</summary>
    public string ResolveHeadline(int currentDay, EventDayType? eventType)
    {
        if (eventType.HasValue)
        {
            foreach (var e in eventEntries)
                if (e.eventType == eventType.Value)
                    return e.eventhaedline;
        }

        DayNewspaperEntry best = null;
        foreach (var d in dayEntries)
        {
            if (d.targetDay > currentDay) continue;
            if (best == null || d.targetDay > best.targetDay) best = d;
        }
        if (best != null) return best.haedline;

        return string.Empty;
    }
}

[Serializable]
public class DayNewspaperEntry
{
    [Tooltip("이 Day 이상부터 이 이미지를 사용")]
    public int targetDay;
    public Sprite sprite;
    public string haedline;
}

[Serializable]
public class EventNewspaperEntry
{
    public EventDayType eventType;
    public Sprite sprite;
    public string eventhaedline;
}