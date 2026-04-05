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

    [Header("출력 정보")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;

    private UserRecordData currentRecord;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        Hide();
    }

    public void Open()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.OpenMonitor);

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void OnClickSearch()
    {
        if (serviceDeskManager == null || idInputField == null)
            return;

        string inputId = idInputField.text;
        serviceDeskManager.ExecuteCommand(ManualCommandIds.SearchRecordByInput, inputId);

        if (serviceDeskManager.TryGetResidentRecord(inputId, out UserRecordData record))
        {
            currentRecord = record;
            RefreshView(record);
        }
    }

    public void OnClickCompare()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.CompareCardAndMonitor);
    }

    public void OnClickSelectPrint()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.SelectPrint);
    }

    public void OnClickSelectMobile()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.SelectMobile);
    }

    public void OnClickPrint()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.PrintDocument);
    }

    public void OnClickSendMobile()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.SendMobile);
    }

    public void OnClickRejectAddressMismatch()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.RejectAddressMismatch);
    }

    private void RefreshView(UserRecordData record)
    {
        if (record == null)
            return;

        if (portraitImage != null)
            portraitImage.sprite = record.portrait;

        if (idText != null)
            idText.text = record.recordId;

        if (nameText != null)
            nameText.text = record.fullName;

        if (addressText != null)
            addressText.text = record.address;
    }

}