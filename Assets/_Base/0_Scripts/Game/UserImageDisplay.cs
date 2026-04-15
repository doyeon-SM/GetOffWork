using UnityEngine;

/// <summary>
/// 씬의 UserImage SpriteRenderer에 방문객 초상화를 표시한다.
///
/// [동작 방식]
/// - OnCustomerCalled: 민원 타입이 NewID이면 M_NewID.GetVisitorPortrait()로 portrait를 가져와 표시
/// - OnCustomerCleared: sprite를 null로 초기화
///
/// ServiceDeskManager.OnCustomerCalled 이벤트 시점에는 M_NewID.Initialize()가
/// 이미 완료되어 _runtimeData가 생성된 상태이므로 portrait를 즉시 읽을 수 있다.
/// </summary>
public class UserImageDisplay : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private ServiceDeskManager _deskManager;

private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            Debug.LogError("[UserImageDisplay] SpriteRenderer null. Inspector spriteRenderer 필드를 확인하세요.");
    }

private void Start()
    {
        _deskManager = FindFirstObjectByType<ServiceDeskManager>();
        if (_deskManager == null)
        {
            Debug.LogWarning("[UserImageDisplay] ServiceDeskManager를 찾을 수 없습니다.");
            return;
        }
        _deskManager.OnCustomerCalled  += HandleCustomerCalled;
        _deskManager.OnCustomerCleared += HandleCustomerCleared;

        // 구독 시점에 이미 NewID 손님이 호출된 상태일 수 있으므로 즉시 반영
        if (_deskManager.HasActiveCustomer &&
            _deskManager.CurrentComplaint?.complaintType == ComplaintContext.ComplaintType.NewID)
        {
            var manual = _deskManager.CurrentManual as M_NewID;
            if (manual != null)
                SetSprite(manual.GetVisitorPortrait());
        }
    }

    private void OnDestroy()
    {
        if (_deskManager == null) return;
        _deskManager.OnCustomerCalled  -= HandleCustomerCalled;
        _deskManager.OnCustomerCleared -= HandleCustomerCleared;
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────

private void HandleCustomerCalled(ComplaintContext context)
    {
        if (context.complaintType != ComplaintContext.ComplaintType.NewID)
        {
            SetSprite(null);
            return;
        }

        var manual = _deskManager.CurrentManual as M_NewID;
        if (manual == null)
        {
            Debug.LogWarning("[UserImageDisplay] CurrentManual이 M_NewID가 아닙니다.");
            SetSprite(null);
            return;
        }

        var portrait = manual.GetVisitorPortrait();
        if (portrait == null)
            Debug.LogWarning("[UserImageDisplay] GetVisitorPortrait()가 null입니다. PortraitListSO가 할당되어 있는지 확인하세요.");

        SetSprite(portrait);
    }

    private void HandleCustomerCleared()
    {
        SetSprite(null);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }
}
