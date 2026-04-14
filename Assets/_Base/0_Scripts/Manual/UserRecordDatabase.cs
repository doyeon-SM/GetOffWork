using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UserRecordDatabase", menuName = "Game/Setting/User Record Database")]
public class UserRecordDatabase : ScriptableObject
{
    [SerializeField] private List<UserRecordData> records = new();

    private Dictionary<string, UserRecordData> cache;

    public void BuildCache()
    {
        cache = new Dictionary<string, UserRecordData>();

        foreach (var record in records)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.recordId))
                continue;

            if (!cache.ContainsKey(record.recordId))
                cache.Add(record.recordId, record);
            else
                Debug.LogWarning($"중복 ResidentRecord ID 발견: {record.recordId}");
        }
    }

    public bool TryGetRecord(string recordId, out UserRecordData record)
    {
        if (cache == null)
            BuildCache();

        return cache.TryGetValue(recordId, out record);
    }

    public IReadOnlyList<UserRecordData> Records => records;
}