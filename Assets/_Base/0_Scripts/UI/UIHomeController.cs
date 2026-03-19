using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIHomeController : MonoBehaviour
{
    [Header("Home 기본 UI")]
    [SerializeField] private Button goToWorkButton;     // 출근하기 버튼
    [SerializeField] private Button convenienceButton;  // 편의점 버튼

    [Header("배경 이미지 (I_Background의 Image)")]
    [SerializeField] private Image backgroundImage;

    [Header("배경 스프라이트")]
    [SerializeField] private Sprite background_morning; // 아침
    [SerializeField] private Sprite background_store; // 편의점
    [SerializeField] private Sprite background_street; // 거리

    [Header("편의점 UI")]
    [SerializeField] private GameObject convenienceStoreUIPrefab;
    [SerializeField] private Transform uiSpawnRoot; // 보통 Home Canvas

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
        // Home씬 진입 시 기본 배경은 morning
        if (backgroundImage != null && background_morning != null)
        {
            backgroundImage.sprite = background_morning;
        }

        // 하루 시작 시 편의점은 다시 활성 상태로 시작
        // 추후 "진짜 하루 개념"과 연결하고 싶다면 외부 DayManager 값으로 바꾸면 됨.
        hasVisitedStoreToday = false;
        SetConvenienceButtonState(true);
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

    public void OnClickGoToWork()
    {
        if(GameFlowManager.Instance == null)
        {
            Debug.Log("[Error] GameFlowManager Instance NULL");
            return;
        }

        GameFlowManager.Instance.StartWorkDay();
    }

    public void OnClickConvenienceStore()
    {
        if (hasVisitedStoreToday)
            return;

        if (currentStoreUIInstance != null)
            return;

        // 배경 morning -> store
        if (backgroundImage != null && background_store != null)
        {
            backgroundImage.sprite = background_store;
        }

        if (convenienceStoreUIPrefab == null)
        {
            Debug.LogWarning("ConvenienceStoreUIPrefab이 연결되지 않았습니다.");
            return;
        }

        Transform parent = uiSpawnRoot != null ? uiSpawnRoot : transform;
        currentStoreUIInstance = Instantiate(convenienceStoreUIPrefab, parent);

        UIStore storeUI = currentStoreUIInstance.GetComponent<UIStore>();
        if (storeUI != null)
        {
            storeUI.Initialize(this);
        }
        else
        {
            Debug.LogWarning("편의점 UI 프리팹에 ConvenienceStoreUI 스크립트가 없습니다.");
        }
    }

    /// <summary>
    /// 편의점 UI가 닫힐 때 호출됨
    /// </summary>
    public void OnConvenienceStoreClosed()
    {
        hasVisitedStoreToday = true;

        if (currentStoreUIInstance != null)
        {
            Destroy(currentStoreUIInstance);
            currentStoreUIInstance = null;
        }

        // 배경을 street로 변경
        if (backgroundImage != null && background_street != null)
        {
            backgroundImage.sprite = background_street;
        }

        // 편의점 버튼 비활성화 + 반투명 처리
        SetConvenienceButtonState(false);
    }

    private void SetConvenienceButtonState(bool isActive)
    {
        if (convenienceButton == null)
            return;

        convenienceButton.interactable = isActive;

        Image buttonImage = convenienceButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color color = buttonImage.color;
            color.a = isActive ? 1f : disabledButtonAlpha;
            buttonImage.color = color;
        }
    }
}