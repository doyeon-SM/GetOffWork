using UnityEngine;

/// <summary>
/// 제출/반납 서브 영역(TakeObjectZone).
/// ObjectManagerBox 안에 자식으로 배치한다.
/// DeskObjectItem이 이 영역 안에 있는지 판정하는 데 사용된다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TakeObjectZone : MonoBehaviour
{
    private BoxCollider2D zone;

    private void Awake()
    {
        zone = GetComponent<BoxCollider2D>();
    }

    /// <summary>월드 좌표 pos가 이 영역 안에 있으면 true</summary>
    public bool Contains(Vector3 pos)
    {
        if (zone == null) return false;
        return zone.bounds.Contains(pos);
    }

    /// <summary>영역 중심 월드 좌표</summary>
    public Vector3 GetCenterWorldPos()
    {
        if (zone == null) return transform.position;
        return zone.bounds.center;
    }
}
