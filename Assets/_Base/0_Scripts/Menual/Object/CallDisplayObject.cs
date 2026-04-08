using TMPro;
using UnityEngine;

public class CallDisplayObject : ClickableWorldObject
{
    [Header("연결")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [SerializeField] private AudioClip CallSFX;


    protected override void Awake()
    {
        base.Awake();

        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

    }

    public override void OnClicked()
    {
        base.OnClicked();

        if (serviceDeskManager == null)
            return;

        bool success = serviceDeskManager.CallNextCustomer();
        if (success && SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.PlaySfxOneShot(CallSFX);

        if (showDebugLog)
        {
            if (success)
                Debug.Log("[CallDisplay] 민원인 호출 성공");
            else
                Debug.Log("[CallDisplay] 호출 실패 (대기자 없음 or 이미 처리중)");
        }
    }

}