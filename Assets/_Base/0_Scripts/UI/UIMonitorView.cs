using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMonitorView : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("루트")]
    [SerializeField] private GameObject root;

    [Header("검색")]
    [SerializeField] private TMP_InputField idInputField;

    [Header("조회 결과")]
    [SerializeField] private Image    portraitImage;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
        Hide();
    }

    private void OnEnable()
    {
        if (serviceDeskManager == null) return;
        // 민원인이 변경(퇴장)될 때 조회 결과 초기화
        serviceDeskManager.OnCustomerCleared += HandleCustomerCleared;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnCustomerCleared -= HandleCustomerCleared;
    }

    private void HandleCustomerCleared()
    {
        // 조회 결과와 입력 필드를 초기화하고 모니터를 닫는다
        ClearView();
        Hide();
    }

    // ── 열기/닫기 ─────────────────────────────────────────────────────────
    // OpenMonitor 절차가 제거됐으므로 ExecuteCommand 호출 없이 순수 UI 토글만 한다.
    public void Open()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────────────────
    public void OnClickSearch()
    {
        if (serviceDeskManager == null || idInputField == null) return;

        string inputId = idInputField.text;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SearchRecordByInput, inputId);

        if (serviceDeskManager.TryGetResidentRecord(inputId, out UserRecordData record))
            RefreshView(record);
        else
            ClearView();
    }

    // CompareCardAndMonitor 버튼 제거 — 비교는 플레이어가 눈으로 판단

    public void OnClickSelectPrint()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SelectPrint);
    }

    public void OnClickSelectMobile()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SelectMobile);
    }

    public void OnClickPrint()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.PrintDocument);
    }

    public void OnClickSendMobile()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SendMobile);
    }

    public void OnClickRejectAddressMismatch()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.RejectAddressMismatch);
    }

    // ── 뷰 갱신 ──────────────────────────────────────────────────────────
    private void RefreshView(UserRecordData record)
    {
        if (record == null) { ClearView(); return; }

        if (portraitImage != null) portraitImage.sprite = record.portrait;
        if (idText        != null) idText.text          = record.recordId;
        if (nameText      != null) nameText.text         = record.fullName;
        if (addressText   != null) addressText.text      = record.address;
    }

    private void ClearView()
    {
        if (idInputField  != null) idInputField.text    = string.Empty;
        if (portraitImage != null) portraitImage.sprite = null;
        if (idText        != null) idText.text          = string.Empty;
        if (nameText      != null) nameText.text        = string.Empty;
        if (addressText   != null) addressText.text     = string.Empty;
    }
}
