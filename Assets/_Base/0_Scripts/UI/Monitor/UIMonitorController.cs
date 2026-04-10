using UnityEngine;

/// <summary>
/// 모니터 UI 컨트롤러.
/// 각 패널은 프리팹으로 관리되며, 전환 시 현재 패널을 Destroy하고
/// 새 패널을 Instantiate하여 root에 붙인다.
/// </summary>
public class UIMonitorController : MonoBehaviour
{
    [Header("패널 프리팹 (각 화면을 프리팹으로 등록)")]
    [SerializeField] private UIMonitorMainPanel   mainPanelPrefab;
    [SerializeField] private UIMonitorPrintPanel  printPanelPrefab;
    [SerializeField] private UIMonitorMobilePanel mobilePanelPrefab;

    [Header("패널이 생성될 루트 Transform")]
    [SerializeField] private RectTransform panelRoot;

    private ServiceDeskManager serviceDeskManager;
    private UserRecordData     currentRecord;
    private GameObject         currentPanelInstance;

    // ── 초기화 ────────────────────────────────────────────────────────────

    private void Awake()
    {
        serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        if (panelRoot == null)
            panelRoot = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (serviceDeskManager != null)
            serviceDeskManager.OnCustomerCleared += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager != null)
            serviceDeskManager.OnCustomerCleared -= HandleCustomerCleared;
    }

    private void HandleCustomerCleared()
    {
        currentRecord = null;
        Close();
    }

    // ── 열기/닫기 ─────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        GoToMain();
    }

    public void Close()
    {
        ClearCurrentPanel();
        gameObject.SetActive(false);
    }

    // ── 패널 전환 ─────────────────────────────────────────────────────────

    public void GoToMain()
    {
        if (mainPanelPrefab == null)
        {
            Debug.LogError("[UIMonitorController] mainPanelPrefab이 할당되지 않았습니다.");
            return;
        }
        ShowPanel(mainPanelPrefab.gameObject, inst =>
        {
            var panel = inst.GetComponent<UIMonitorMainPanel>();
            panel.Init(this);
            panel.RefreshView(currentRecord);
        });
    }

    public void GoToPrint()
    {
        if (printPanelPrefab == null)
        {
            Debug.LogError("[UIMonitorController] printPanelPrefab이 할당되지 않았습니다.");
            return;
        }
        ShowPanel(printPanelPrefab.gameObject, inst =>
        {
            var panel = inst.GetComponent<UIMonitorPrintPanel>();
            panel.Init(this);
            panel.RefreshView(currentRecord);
        });
    }

    public void GoToMobile()
    {
        if (mobilePanelPrefab == null)
        {
            Debug.LogError("[UIMonitorController] mobilePanelPrefab이 할당되지 않았습니다.");
            return;
        }
        ShowPanel(mobilePanelPrefab.gameObject, inst =>
        {
            var panel = inst.GetComponent<UIMonitorMobilePanel>();
            panel.Init(this);
            panel.RefreshView(currentRecord);
        });
    }

    private void ShowPanel(GameObject prefab, System.Action<GameObject> onCreated)
    {
        ClearCurrentPanel();
        currentPanelInstance = Instantiate(prefab, panelRoot);
        var rect = currentPanelInstance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        onCreated?.Invoke(currentPanelInstance);
    }

    private void ClearCurrentPanel()
    {
        if (currentPanelInstance != null)
        {
            Destroy(currentPanelInstance);
            currentPanelInstance = null;
        }
    }

    // ── 커맨드 ────────────────────────────────────────────────────────────

    public void OnSearch(string inputId)
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SearchRecordByInput, inputId);

        if (serviceDeskManager.TryGetResidentRecord(inputId, out UserRecordData record))
        {
            currentRecord = record;
            currentPanelInstance?.GetComponent<UIMonitorMainPanel>()?.RefreshView(record);
        }
        else
        {
            currentRecord = null;
            currentPanelInstance?.GetComponent<UIMonitorMainPanel>()?.ClearView();
        }
    }

    public void OnSelectPrint()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SelectPrint);
        GoToPrint();
    }

    public void OnSelectMobile()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SelectMobile);
        GoToMobile();
    }

    public void OnPrint()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.PrintDocument);
    }

    /// <summary>
    /// MobilePanel의 Send 버튼 → 입력된 전화번호를 payload로 MobileNumberByInput 커맨드 실행
    /// </summary>
    public void OnMobileNumberByInput(string inputPhone)
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.MobileNumberByInput, inputPhone);
    }

    public void OnRejectAddressMismatch()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.RejectAddressMismatch);
    }
}
