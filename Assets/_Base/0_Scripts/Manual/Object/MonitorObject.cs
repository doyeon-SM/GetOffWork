using System;
using UnityEngine;

public class MonitorObject : ClickableWorldObject
{
    [SerializeField] private UIMonitorView monitorView;

    /// <summary>모니터가 클릭됐을 때 TutorialManager가 구독하는 이벤트</summary>
    public static event Action OnMonitorClicked;

    protected override void Awake()
    {
        base.Awake();
        if (monitorView == null)
            monitorView = FindFirstObjectByType<UIMonitorView>();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[MonitorObject] 모니터 클릭");

        OnMonitorClicked?.Invoke();

        if (monitorView != null)
            monitorView.Open();
    }
}
