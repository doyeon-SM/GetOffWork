using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UIServiceDesk : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("대기열 UI")]
    [SerializeField] private TMP_Text waitingCountText;

    [Header("민원인 이미지 후보")]
    [SerializeField] private List<Sprite> customerSpriteList = new List<Sprite>();

    [Header("민원인 UI")]
    [SerializeField] private Image customerImage;
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
        ShowCustomerImage(GetRandomCustomerSprite());
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
            waitingCountText.text = count.ToString();
    }

    private void ShowCustomerImage(Sprite sprite)
    {
        if (customerImage != null)
        {
            customerImage.sprite = sprite;
            customerImage.enabled = sprite != null;
        }

        if (customerImageRoot != null)
            customerImageRoot.SetActive(sprite != null);
    }

    private void HideCustomerImage()
    {
        if (customerImage != null)
        {
            customerImage.sprite = null;
            customerImage.enabled = false;
        }

        if (customerImageRoot != null)
            customerImageRoot.SetActive(false);
    }
    private Sprite GetRandomCustomerSprite()
    {
        if (customerSpriteList == null || customerSpriteList.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, customerSpriteList.Count);
        return customerSpriteList[index];
    }
}
