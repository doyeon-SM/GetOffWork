using UnityEngine;

public class ManualObject : ClickableWorldObject
{
    [SerializeField] private UIManualView ManualView;

    protected override void Awake()
    {
        base.Awake();

        if (ManualView == null)
            ManualView = FindFirstObjectByType<UIManualView>();

        if (ManualView == null)
            Debug.LogWarning("[ManualObject] UIManualView를 찾을 수 없습니다. Inspector에서 직접 연결해 주세요.");
    }
    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[ManualObject] 메뉴얼 가이드 오브젝트 클릭");
        ManualView.Open();
    }
}
