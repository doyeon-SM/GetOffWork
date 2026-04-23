using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class LunchStatChange
{
    public Stat stat;
    public int amount;
}

[CreateAssetMenu(fileName = "LunchOption_", menuName = "Game/Record/Lunch Option")]
public class LunchOptionData : ScriptableObject
{
    [Header("기본 정보")]
    public string optionName;
    [TextArea(2, 4)] public string description;
    [TextArea(2, 4)] public string effectDescription;

    [Header("스탯 변화")]
    public List<LunchStatChange> statChanges = new List<LunchStatChange>();
}

