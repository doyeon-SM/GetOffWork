using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIServiceDesk : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("대기자 UI")]
    [SerializeField] private TMP_Text waitingCountText;

    [Header("방문객 UI")]
    //[SerializeField] private Image customerImage;
    [SerializeField] private GameObject customerImageRoot;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        HideCustomerImage();
        SetWaitingCount(0);
    }

    private void OnEnable()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.OnWaitingQueueChanged += HandleWaitingQueueChanged;
        serviceDeskManager.OnCustomerCalled += HandleCustomerCalled;
        serviceDeskManager.OnCustomerCleared += HandleCustomerCleared;
        serviceDeskManager.OnWorkStateChanged += HandleWorkStateChanged;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.OnWaitingQueueChanged -= HandleWaitingQueueChanged;
        serviceDeskManager.OnCustomerCalled -= HandleCustomerCalled;
        serviceDeskManager.OnCustomerCleared -= HandleCustomerCleared;
        serviceDeskManager.OnWorkStateChanged -= HandleWorkStateChanged;
    }

    private void HandleWaitingQueueChanged(int waitingCount)
    {
        SetWaitingCount(waitingCount);
    }

    private void HandleCustomerCalled(ComplaintContext complaint)
    {
        ShowCustomerImage(GetCustomerPortrait(complaint));
    }

    private void HandleCustomerCleared()
    {
        HideCustomerImage();
    }

    private void HandleWorkStateChanged(bool isWorking)
    {
        if (!isWorking)
        {
            SetWaitingCount(0);
            HideCustomerImage();
        }
    }

    private void SetWaitingCount(int count)
    {
        if (waitingCountText != null)
            waitingCountText.text = "대기 : " + count.ToString()+"명";
    }

    private void ShowCustomerImage(Sprite sprite)
    {
        /*if (customerImage != null)
        {
            customerImage.sprite = sprite;
            customerImage.enabled = sprite != null;
        }*/

        if (customerImageRoot != null)
        {
            customerImageRoot.SetActive(sprite != null);
            SpriteRenderer sr = customerImageRoot.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.enabled = sprite != null;
        }
    }

    private void HideCustomerImage()
    {
        /*if (customerImage != null)
        {
            customerImage.sprite = null;
            customerImage.enabled = false;
        }*/

        if (customerImageRoot != null)
        {
            customerImageRoot.SetActive(false);
            SpriteRenderer sr = customerImageRoot.GetComponent<SpriteRenderer>();
            sr.sprite = null;
            sr.enabled = false;
        }
    }

private Sprite GetCustomerPortrait(ComplaintContext complaint)
    {
        if (serviceDeskManager == null || complaint == null)
            return null;

        // 기존 방문객: DB에서 portrait 조회
        if (serviceDeskManager.TryGetResidentRecord(complaint.applicantRecordId, out UserRecordData record))
            return record != null ? record.portrait : null;

        // NewID 방문객: DB 등록 전이므로 CurrentManual(M_NewID)에서 직접 가져오기
        if (serviceDeskManager.CurrentManual is M_NewID newIdManual)
            return newIdManual.GetVisitorPortrait();

        return null;
    }
}