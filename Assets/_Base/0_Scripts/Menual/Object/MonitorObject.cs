using UnityEngine;

public class MonitorObject : ClickableWorldObject
{
    [SerializeField] private UIMonitorView monitorView;

    protected override void Awake()
    {
        base.Awake();
        if (monitorView == null)
            monitorView = FindFirstObjectByType<UIMonitorView>();
    }
    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[MonitorObject] 모니터 오브젝트 클릭");

        if (monitorView != null)
            monitorView.Open();
    }
}