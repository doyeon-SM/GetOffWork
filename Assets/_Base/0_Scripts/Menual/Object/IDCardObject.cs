using UnityEngine;

/// <summary>
/// 신분증 월드 오브젝트.
///
/// 구조 전제:
///   - 이 컴포넌트는 항상 씬에 활성 상태로 존재하는 부모 오브젝트에 붙어있다.
///   - [idCardVisual] 필드는 실제로 보여줄 자식 오브젝트를 참조한다.
///     (스프라이트, 메시 등 시각 요소가 담긴 자식 GameObject)
///   - idCardVisual만 켜고 끄기 때문에, 이 컴포넌트 자신은 항상 활성 상태를
///     유지하고 이벤트 구독이 끊기지 않는다.
///
/// 인스펙터 설정:
///   - Id Card Visual : 신분증 이미지/스프라이트를 가진 자식 오브젝트
///   - (구 idCardObject 필드가 자기 자신이나 부모를 가리키고 있었다면
///      자식 오브젝트를 새로 만들거나 분리해서 여기에 연결할 것)
/// </summary>
public class IDCardObject : ClickableWorldObject
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private UIIDCardView       cardView;

    [Header("시각 요소 (자식 오브젝트)")]
    [Tooltip("실제로 보이고/숨길 자식 GameObject. 이 컴포넌트가 붙은 오브젝트 자체나 부모를 지정하면 안 됩니다.")]
    [SerializeField] private GameObject idCardVisual;

    // ── 초기화 ───────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        if (cardView == null)
            cardView = FindFirstObjectByType<UIIDCardView>();

        // 시각 요소만 숨긴다 — 이 컴포넌트 자신은 활성 유지
        HideVisual();
    }

    // ── 이벤트 구독/해제 ─────────────────────────────────────────────────
    // OnEnable/OnDisable은 이 GameObject 자체가 활성/비활성 될 때만 호출된다.
    // idCardVisual(자식)만 껐다 켜는 한 이쪽은 영향받지 않는다.
    private void OnEnable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnSpawnIdCardRequested += HandleSpawnIdCard;
        serviceDeskManager.OnCustomerCleared      += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnSpawnIdCardRequested -= HandleSpawnIdCard;
        serviceDeskManager.OnCustomerCleared      -= HandleCustomerCleared;
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────
    private void HandleSpawnIdCard(ComplaintContext complaint)
    {
        ShowVisual();
        Debug.Log("[IDCardObject] 신분증 오브젝트 표시");
    }

    private void HandleCustomerCleared()
    {
        HideVisual();
    }

    // ── 클릭 ─────────────────────────────────────────────────────────────
    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[IDCardObject] 신분증 오브젝트 클릭");

        if (serviceDeskManager == null || serviceDeskManager.CurrentComplaint == null)
            return;

        var complaint = serviceDeskManager.CurrentComplaint;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.OpenIdCardDetail);

        // 발급 대상자 레코드로 카드 뷰를 표시
        string recordId = complaint.EffectiveTargetRecordId;
        if (serviceDeskManager.TryGetResidentRecord(recordId, out UserRecordData record))
        {
            if (cardView != null)
                cardView.Show(record);
        }
    }

    // ── 시각 헬퍼 ────────────────────────────────────────────────────────
    private void ShowVisual()
    {
        if (idCardVisual != null)
            idCardVisual.SetActive(true);
    }

    private void HideVisual()
    {
        if (idCardVisual != null)
            idCardVisual.SetActive(false);
    }
}
