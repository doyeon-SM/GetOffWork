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

        // Self: 방문객 본인 레코드 / Proxy: 대리 대상자 레코드
        string recordId = complaint.applicantType == ComplaintContext.ApplicantType.Proxy
            ? complaint.targetRecordId
            : complaint.applicantRecordId;

        if (serviceDeskManager.TryGetResidentRecord(recordId, out UserRecordData record))
        {
            if (cardView != null)
                cardView.Show(record);
        }
    }
}