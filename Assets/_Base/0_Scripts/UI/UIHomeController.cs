using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIHomeController : MonoBehaviour
{
    [Header("Home 기본 UI")]
    [SerializeField] private Button goToWorkButton;
    [SerializeField] private Button convenienceButton;

    [Header("배경 이미지 (I_Background의 Image)")]
    [SerializeField] private Image backgroundImage;

    [Header("배경 스프라이트")]
    [SerializeField] private Sprite background_morning;
    [SerializeField] private Sprite background_store;
    [SerializeField] private Sprite background_street;

    [Header("편의점 UI")]
    [SerializeField] private PlayerBase playerBase;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject convenienceStoreUIPrefab;
    [SerializeField] private Transform uiSpawnRoot;

    [Header("버튼 비활성화 시각 효과")]
    [SerializeField, Range(0f, 1f)] private float disabledButtonAlpha = 0.5f;

    private GameObject currentStoreUIInstance;
    private bool hasVisitedStoreToday = false;

    private void Awake()
    {
        InitializeHomeUI();
        BindButtons();
    }

    private void InitializeHomeUI()
    {
        // 신문+대화가 끝나기 전까지 morning 배경, 버튼 비활성화
        if (backgroundImage != null && background_morning != null)
            backgroundImage.sprite = background_morning;

        hasVisitedStoreToday = false;

        // 신문+대화 완료 전까지 양쪽 버튼 모두 잠금
        SetButtonActive(goToWorkButton, false);
        SetConvenienceButtonState(false);
    }

    private void BindButtons()
    {
        if (goToWorkButton != null)
        {
            goToWorkButton.onClick.RemoveListener(OnClickGoToWork);
            goToWorkButton.onClick.AddListener(OnClickGoToWork);
        }

        if (convenienceButton != null)
        {
            convenienceButton.onClick.RemoveListener(OnClickConvenienceStore);
            convenienceButton.onClick.AddListener(OnClickConvenienceStore);
        }
    }

    /// <summary>
    /// MorningHomeController에서 신문+대화가 모두 끝난 뒤 호출.
    /// 배경을 street로 바꾸고 버튼을 활성화한다.
    /// </summary>
    public void OnMorningSequenceCompleted()
    {
        // 배경 street로 전환
        if (backgroundImage != null && background_street != null)
            backgroundImage.sprite = background_street;

        // 버튼 활성화
        SetButtonActive(goToWorkButton, true);
        SetConvenienceButtonState(true);

        Debug.Log("[UIHomeController] 신문+대화 완료 → street 배경, 버튼 활성화");
    }

    public void OnClickGoToWork()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.Log("[Error] GameFlowManager Instance NULL");
            return;
        }
        GameFlowManager.Instance.StartWorkDay();
    }

    public void OnClickConvenienceStore()
    {
        if (hasVisitedStoreToday) return;
        if (currentStoreUIInstance != null) return;

        // 배경 morning → store
        if (backgroundImage != null && background_store != null)
            backgroundImage.sprite = background_store;

        if (convenienceStoreUIPrefab == null)
        {
            Debug.LogWarning("ConvenienceStoreUIPrefab이 연결되지 않았습니다.");
            return;
        }

        Transform parent = uiSpawnRoot != null ? uiSpawnRoot : transform;
        currentStoreUIInstance = Instantiate(convenienceStoreUIPrefab, parent);

        UIStore storeUI = currentStoreUIInstance.GetComponent<UIStore>();
        playerBase = FindFirstObjectByType<PlayerBase>().GetComponent<PlayerBase>();
        playerInventory = FindFirstObjectByType<PlayerInventory>().GetComponent<PlayerInventory>();

        if (storeUI != null)
            storeUI.Initialize(this, playerInventory, playerBase);
        else
            Debug.LogWarning("편의점 UI 프리팹에 ConvenienceStoreUI 스크립트가 없습니다.");

        if (playerBase == null)    Debug.Log("Player Base Null");
        if (playerInventory == null) Debug.Log("Player Inventory Null");
    }

    /// <summary>편의점 UI가 닫힐 때 호출</summary>
    public void OnConvenienceStoreClosed()
    {
        hasVisitedStoreToday = true;

        if (currentStoreUIInstance != null)
        {
            Destroy(currentStoreUIInstance);
            currentStoreUIInstance = null;
        }

        // 배경 store → street
        if (backgroundImage != null && background_street != null)
            backgroundImage.sprite = background_street;

        SetConvenienceButtonState(false);
    }

    private void SetButtonActive(Button button, bool isActive)
    {
        if (button == null) return;

        button.interactable = isActive;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color color = buttonImage.color;
            color.a = isActive ? 1f : disabledButtonAlpha;
            buttonImage.color = color;
        }
    }

    private void SetConvenienceButtonState(bool isActive)
    {
        SetButtonActive(convenienceButton, isActive);
    }
}