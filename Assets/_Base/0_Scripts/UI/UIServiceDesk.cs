using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIServiceDesk : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("대기열 UI")]
    [SerializeField] private TMP_Text waitingCountText;

    [Header("민원인 UI")]
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
            waitingCountText.text = "대기자 : " + count.ToString();
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

        if (serviceDeskManager.TryGetResidentRecord(complaint.applicantRecordId, out UserRecordData record))
        {
            return record != null ? record.portrait : null;
        }

        return null;
    }
}