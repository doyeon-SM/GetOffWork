using UnityEngine;

public class CallDisplayObject : ClickableWorldObject
{
    [Header("참조")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("SFX")]
    [SerializeField] private AudioClip callSFX;

    protected override void Awake()
    {
        base.Awake();
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        if (serviceDeskManager == null) return;

        // OnClickCallNextCustomer() 호출:
        //   1. ObjectManagerBox.TryFinishAndReturn() — 반납 검사
        //      → 미반납 : 민원인 대사 출력 후 return (호출 방어)
        //      → 반납 완료 : 아이템 삭제
        //   2. HasActiveCustomer면 FinishCurrentCustomer() — 정산
        //   3. CallNextCustomer() — 다음 대기자 호출
        bool hadActiveCustomer = serviceDeskManager.HasActiveCustomer;
        serviceDeskManager.OnClickCallNextCustomer();

        // 호출이 실제로 진행됐을 때만 SFX 재생
        // (반납 방어로 return된 경우 HasActiveCustomer 상태가 그대로이므로 구분 가능)
        bool nowHasCustomer = serviceDeskManager.HasActiveCustomer;
        bool callProceeded  = nowHasCustomer || !hadActiveCustomer;
        if (callProceeded && callSFX != null && SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.PlaySfxOneShot(callSFX);

        if (showDebugLog)
            Debug.Log("[CallDisplay] 호출 클릭");
    }
}
