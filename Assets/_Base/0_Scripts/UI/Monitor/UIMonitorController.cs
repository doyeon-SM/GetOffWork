using UnityEngine;

/// <summary>
/// 모니터 UI 컨트롤러.
/// 각 패널은 프리팹으로 관리되며, 전환 시 현재 패널을 Destroy하고
/// 새 패널을 Instantiate하여 root에 붙인다.
///
/// [이벤트 구독 방식]
/// OnEnable/OnDisable 대신 Start/OnDestroy에서만 구독/해제한다.
/// SetActive(false)로 비활성화되어도 OnCustomerCleared 수신을 유지해
/// 어떤 경로(응대종료, 인내심 한계, 반려 등)로 민원이 끝나도
/// 반드시 모니터가 초기화되도록 보장한다.
/// </summary>
public class UIMonitorController : MonoBehaviour
{
    [Header("패널 프리팹 (각 화면을 프리팹으로 등록)")]
    [SerializeField] private UIMonitorMainPanel   mainPanelPrefab;
    [SerializeField] private UIMonitorPrintPanel  printPanelPrefab;
    [SerializeField] private UIMonitorMobilePanel  mobilePanelPrefab;
    [SerializeField] private UIMonitorAddressPanel addressPanelPrefab;
    [SerializeField] private UIMonitorIdPanel    idPanelPrefab;
    [SerializeField] private UIMonitorNewIdPanel newIdPanelPrefab;


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

    private void Start()
    {
        // Start에서 한 번만 구독
        // OnEnable/OnDisable 타이밍 문제(SetActive로 인한 구독 해제)를 방지한다.
        if (serviceDeskManager != null)
            serviceDeskManager.OnCustomerCleared += HandleCustomerCleared;
        else
            Debug.LogWarning("[UIMonitorController] ServiceDeskManager를 찾을 수 없어 OnCustomerCleared 구독 실패.");
    }

    private void OnDestroy()
    {
        // 오브젝트가 실제로 파괴될 때만 구독 해제
        if (serviceDeskManager != null)
            serviceDeskManager.OnCustomerCleared -= HandleCustomerCleared;
    }

    // ── 민원 종료 핸들러 ──────────────────────────────────────────────────

    private void HandleCustomerCleared()
    {
        currentRecord = null;

        if (gameObject.activeSelf)
        {
            // 모니터가 열려있으면 닫는다 (패널 Destroy + SetActive(false))
            Close();
        }
        else
        {
            // 비활성 상태에서도 currentPanelInstance가 남아있을 수 있으므로 정리
            ClearCurrentPanel();
        }

        Debug.Log("[UIMonitorController] 민원 종료 — 모니터 초기화 완료");
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
            currentPanelInstance?.GetComponent<UIMonitorMainPanel>()?.RefreshView(null);
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


/// <summary>
    /// AddressPanel 확정+출력 버튼.
    /// 절차 6(주소 확정):구실행, 절차 7(새 ID카드 출력)을 연속으로 실행한다.
    /// </summary>
    public void OnSubmitAndPrintNewIdCard(string inputAddress)
    {
        if (serviceDeskManager == null || string.IsNullOrWhiteSpace(inputAddress)) return;

        // 절차 6: SubmitNewAddress — 유효하면 런타임 UserData 수정
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SubmitNewAddress, inputAddress);

        // 절차 7: PrintNewIdCard — SubmitNewAddress 성공 이후에만 주소가 context에 저장될 수 있음
        serviceDeskManager.ExecuteCommand(ManualCommandIds.PrintNewIdCard);
    }


public void GoToAddress()
    {
        if (addressPanelPrefab == null)
        {
            Debug.LogError("[UIMonitorController] addressPanelPrefab이 할당되지 않았습니다.");
            return;
        }
        ShowPanel(addressPanelPrefab.gameObject, inst =>
        {
            var panel = inst.GetComponent<UIMonitorAddressPanel>();
            panel.Init(this, currentRecord);
        });
    }

public void GoToIdTab()
    {
        if (idPanelPrefab == null) { Debug.LogError("[UIMonitorController] idPanelPrefab이 할당되지 않았습니다."); return; }
        ShowPanel(idPanelPrefab.gameObject, inst =>
        {
            inst.GetComponent<UIMonitorIdPanel>()?.Init(this);
        });
    }

/// <summary>
    /// UIMonitorIdPanel의 등록/수정 버튼에서 호출.
    /// serviceDeskManager를 통해 GoToNewIdTab 커맨드를 실행한다.
    /// M_NewID.HandleGoToNewIdTab이 context 세팅 + GoToNewIdTab() UI 전환을 수행한다.
    /// </summary>
    public void ExecuteGoToNewIdTab(string payload)
    {
        serviceDeskManager?.ExecuteCommand(ManualCommandIds.GoToNewIdTab, payload);
    }


public void GoToNewIdTab(bool isEditMode, string prefillName = "", string prefillAddress = "", Sprite prefillPortrait = null)
    {
        if (newIdPanelPrefab == null) { Debug.LogError("[UIMonitorController] newIdPanelPrefab이 할당되지 않았습니다."); return; }
        ShowPanel(newIdPanelPrefab.gameObject, inst =>
        {
            inst.GetComponent<UIMonitorNewIdPanel>()?.Init(this, isEditMode, prefillName, prefillAddress, prefillPortrait);
        });
    }

public void OnSearchNewId(string inputId)
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SearchNewId, inputId);
        currentRecord = null;
        currentPanelInstance?.GetComponent<UIMonitorIdPanel>()?.RefreshSearchResult(inputId);
    }

    public void OnRegisterNewUser(string inputName, string inputAddress)
    {
        if (serviceDeskManager == null) return;
        string payload = inputName + "|" + inputAddress;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.RegisterNewUser, payload);
    }

    public void OnRegisterPortrait()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.RegisterNewIdPortrait);
    }

/// <summary>M_NewID가 초상화 등록 성공 후 NewIdPanel의 SetPortrait를 호출한다.</summary>
    public void NotifyPortraitRegistered(Sprite portrait)
    {
        currentPanelInstance?.GetComponent<UIMonitorNewIdPanel>()?.SetPortrait(portrait);
    }


}
