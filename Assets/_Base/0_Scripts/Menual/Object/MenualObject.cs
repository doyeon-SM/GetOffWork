using UnityEngine;

public class MenualObject : ClickableWorldObject
{
    [SerializeField] private UIMenualView menualView;

    protected override void Awake()
    {
        base.Awake();

        if (menualView == null)
            menualView = FindFirstObjectByType<UIMenualView>();

        if (menualView == null)
            Debug.LogWarning("[MenualObject] UIMenualView를 찾을 수 없습니다. Inspector에서 직접 연결해 주세요.");
    }
    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[MenualObject] 메뉴얼 가이드 오브젝트 클릭");
        menualView.Open();
    }
}
