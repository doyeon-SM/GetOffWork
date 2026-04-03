using UnityEngine;

public class UIIDCard : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;
    [SerializeField] private UIIDCardView cardView;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();
    }

    public void OnClickIDCard()
    {
        if (serviceDeskManager == null || serviceDeskManager.CurrentComplaint == null)
            return;

        var complaint = serviceDeskManager.CurrentComplaint;

        serviceDeskManager.ExecuteCommand(ManualCommandIds.OpenIdCardDetail);

        string recordId = complaint.applicantRecordId;
        if (complaint.applicantType == ComplaintContext.ApplicantType.Proxy)
            recordId = complaint.applicantRecordId;

        if (serviceDeskManager.TryGetResidentRecord(recordId, out UserRecordData record))
        {
            if (cardView != null)
                cardView.Show(record);
        }
    }
}