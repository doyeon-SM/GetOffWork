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
        // 기존 방문객(DB 기반)은 UIServiceDesk가 담당하므로 별도 폴백 없음
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
        if (context.complaintType == ComplaintContext.ComplaintType.NewID)
        {
            // NewID: DB에 없는 런타임 데이터에서 직접 가져오기
            var manual = _deskManager.CurrentManual as M_NewID;
            if (manual == null)
            {
                Debug.LogWarning("[UserImageDisplay] CurrentManual이 M_NewID가 아닙니다.");
                SetSprite(null);
                return;
            }
            var portrait = manual.GetVisitorPortrait();
            if (portrait == null)
                Debug.LogWarning("[UserImageDisplay] GetVisitorPortrait()가 null. PortraitListSO 할당 확인.");
            SetSprite(portrait);
        }
        else
        {
            // 기존 방문객 (FullID, AddressChange 등): DB에서 portrait 공백
            // UIServiceDesk도 동일하게 세팅하지만, 충돌 방지를 위해
            // 여기서는 배제 - UIServiceDesk가 담당
            // 대신 쿨리어 시 null 처리만 유지
        }
    }

    private void HandleCustomerCleared()
    {
        SetSprite(null);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private void SetSprite(Sprite sprite)
    {
        // spriteRenderer가 null이면 다시 시도 (레이트 바인딩 대비)
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
        else
            Debug.LogError("[UserImageDisplay] SpriteRenderer가 null — UserImage 오브젝트에 SpriteRenderer가 있는지 확인하세요.");
    }
}
