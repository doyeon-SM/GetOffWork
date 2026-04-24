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

        if (!serviceDeskManager.TryGetResidentRecord(recordId, out UserRecordData record) || cardView == null)
            return;

        // Spawn 시점과 동일하게 표시값을 계산해서 Show() 호출
        bool isProxy       = complaint.applicantType == ComplaintContext.ApplicantType.Proxy;
        bool useIdFake     = isProxy
            ? (complaint.isIdMismatch      && record.HasIdMismatch)
            : complaint.isIdMismatch;
        bool useAddrFake   = isProxy
            ? (complaint.isAddressMismatch  && record.HasAddressMismatch)
            : complaint.isAddressMismatch;
        bool usePortFake   = isProxy
            ? (complaint.isPortraitMismatch && record.HasPortraitMismatch)
            : complaint.isPortraitMismatch;

        cardView.Show(
            record.ResolveDisplayId(useIdFake),
            record.ResolveDisplayAddress(useAddrFake),
            record.fullName,
            record.ResolveDisplayPortrait(usePortFake));
    }
}