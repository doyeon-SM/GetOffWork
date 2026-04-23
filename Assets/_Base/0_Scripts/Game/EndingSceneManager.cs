using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndingSceneManager : MonoBehaviour
{
    [Header("배경 이미지")]
    [SerializeField] private Image backgroundImage;
    [Header("로고 이미지")]
    [SerializeField] private Image logoImage;
    [Header("배경 스프라이트")]
    [SerializeField] private Sprite firedBackground;
    [SerializeField] private Sprite resignBackground;
    [SerializeField] private Sprite happyBackground;
    [Header("로고 스프라이트")]
    [SerializeField] private Sprite gameOverLogo;
    [SerializeField] private Sprite promotionLogo;
    [Header("게임오버 버튼 그룹")]
    [SerializeField] private GameObject gameOverButtonGroup;
    [SerializeField] private Button toTitleButton;
    [Header("해피엔딩 버튼 그룹")]
    [SerializeField] private GameObject happyButtonGroup;
    [SerializeField] private Button happyToTitleButton;
    [SerializeField] private Button continueButton;

    private const int TITLE_SCENE_INDEX = 0;
    private const int HOME_SCENE_INDEX  = 1;

    private void Start()
    {
        if (toTitleButton      != null) toTitleButton.onClick.AddListener(GoToTitle);
        if (happyToTitleButton != null) happyToTitleButton.onClick.AddListener(GoToTitle);
        if (continueButton     != null) continueButton.onClick.AddListener(GoToHome);
        SetupEnding();
    }

    private void SetupEnding()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[EndingSceneManager] GameFlowManager 없음 - 해고엔딩");
            DisplayFiredEnding();
            return;
        }
        var e = GameFlowManager.Instance.LastEnding;
        if      (e == PlayerBase.PlayerEnding.NormalEnding) DisplayHappyEnding();
        else if (e == PlayerBase.PlayerEnding.Stressfull)   DisplayResignEnding();
        else                                                 DisplayFiredEnding();
    }

    private void DisplayFiredEnding()
    {
        if (backgroundImage != null && firedBackground  != null) backgroundImage.sprite = firedBackground;
        if (logoImage       != null && gameOverLogo     != null) logoImage.sprite       = gameOverLogo;
        if (gameOverButtonGroup != null) gameOverButtonGroup.SetActive(true);
        if (happyButtonGroup    != null) happyButtonGroup.SetActive(false);
    }

    private void DisplayResignEnding()
    {
        if (backgroundImage != null && resignBackground != null) backgroundImage.sprite = resignBackground;
        if (logoImage       != null && gameOverLogo     != null) logoImage.sprite       = gameOverLogo;
        if (gameOverButtonGroup != null) gameOverButtonGroup.SetActive(true);
        if (happyButtonGroup    != null) happyButtonGroup.SetActive(false);
    }

    private void DisplayHappyEnding()
    {
        if (backgroundImage != null && happyBackground  != null) backgroundImage.sprite = happyBackground;
        if (logoImage       != null && promotionLogo    != null) logoImage.sprite       = promotionLogo;
        if (gameOverButtonGroup != null) gameOverButtonGroup.SetActive(false);
        if (happyButtonGroup    != null) happyButtonGroup.SetActive(true);
    }

    private void GoToTitle() { SceneManager.LoadScene(TITLE_SCENE_INDEX); }
    private void GoToHome()  { SceneManager.LoadScene(HOME_SCENE_INDEX); }
}