using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UserRecordDatabase", menuName = "Game/Setting/User Record Database")]
public class UserRecordDatabase : ScriptableObject
{
    [SerializeField] private List<UserRecordData> records = new();

    private Dictionary<string, UserRecordData> cache;

    // ── 런타임 등록 레코드 추적 (게임 종료 시 정리용) ────────────────────
    private readonly List<string> _runtimeRegisteredIds = new();

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
                Debug.LogWarning($"중복 ResidentRecord ID 추가: {record.recordId}");
        }
    }

    public bool TryGetRecord(string recordId, out UserRecordData record)
    {
        if (cache == null)
            BuildCache();

        return cache.TryGetValue(recordId, out record);
    }

    public IReadOnlyList<UserRecordData> Records => records;

    // ── 런타임 등록 / 해제 ────────────────────────────────────────────────

    /// <summary>
    /// 런타임에 생성된 UserRecordData를 캐시에 등록한다.
    /// 게임 종료 / 세션 종료 시 RemoveRuntimeRecord 또는 ClearRuntimeRecords로 정리해야 한다.
    /// </summary>
    /// <returns>등록 성공 여부. ID 중복이면 false.</returns>
    public bool RegisterRuntimeRecord(UserRecordData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.recordId))
        {
            Debug.LogWarning("[UserRecordDatabase] 유효하지 않은 런타임 레코드입니다.");
            return false;
        }

        if (cache == null) BuildCache();

        if (cache.ContainsKey(data.recordId))
        {
            Debug.LogWarning($"[UserRecordDatabase] 런타임 등록 실패 — ID 중복: {data.recordId}");
            return false;
        }

        cache.Add(data.recordId, data);
        _runtimeRegisteredIds.Add(data.recordId);
        Debug.Log($"[UserRecordDatabase] 런타임 등록 완료: {data.recordId} ({data.fullName})");
        return true;
    }

/// <summary>
    /// 이미 DB에 등록된 런타임 레코드의 이름과 주소를 수정한다.
    /// edit 모드(ID는 동일, 이름/주소만 변경)에서 사용한다.
    /// </summary>
    public bool UpdateRuntimeRecord(string recordId, string newName, string newAddress)
    {
        if (cache == null) BuildCache();
        if (!cache.TryGetValue(recordId, out UserRecordData data))
        {
            Debug.LogWarning($"[UserRecordDatabase] 수정 실패 — ID를 찾을 수 없음: {recordId}");
            return false;
        }
        data.fullName = newName;
        data.address  = newAddress;
        Debug.Log($"[UserRecordDatabase] 런타임 레코드 수정 완료: {recordId} — {newName} / {newAddress}");
        return true;
    }


    /// <summary>특정 런타임 레코드를 캐시에서 제거하고 메모리를 해제한다.</summary>
    public void RemoveRuntimeRecord(string recordId)
    {
        if (cache == null) return;
        if (cache.TryGetValue(recordId, out var data))
        {
            cache.Remove(recordId);
            _runtimeRegisteredIds.Remove(recordId);
            if (data != null)
                Object.DestroyImmediate(data);
            Debug.Log($"[UserRecordDatabase] 런타임 레코드 제거: {recordId}");
        }
    }

    /// <summary>등록된 모든 런타임 레코드를 캐시에서 제거하고 메모리를 해제한다.</summary>
    public void ClearRuntimeRecords()
    {
        if (cache == null) return;
        foreach (var id in _runtimeRegisteredIds)
        {
            if (cache.TryGetValue(id, out var data))
            {
                cache.Remove(id);
                if (data != null)
                    Object.DestroyImmediate(data);
            }
        }
        _runtimeRegisteredIds.Clear();
        Debug.Log("[UserRecordDatabase] 모든 런타임 레코드 정리 완료");
    }
}