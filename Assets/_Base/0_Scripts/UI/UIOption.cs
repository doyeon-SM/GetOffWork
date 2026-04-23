using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOption : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Texts")]
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [Tooltip("타이틀이 아닌 씬에서만 활성화되는 게임 포기 버튼")]
    [SerializeField] private Button giveUpButton;

    // PauseManager 참조 (일시정지 모드에서만 주입됨)
    private PauseManager _pauseManager;

    private bool isInitialized = false;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseAndResume);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);

        if (giveUpButton != null)
            giveUpButton.onClick.AddListener(OnGiveUpClicked);

        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(OnMasterChanged);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    private void OnEnable()
    {
        RefreshUIFromManager();
    }

    public void RefreshUIFromManager()
    {
        if (SoundSettingsManager.Instance == null)
        {
            Debug.LogWarning("[OptionUI] SoundSettingsManager.Instance�� �����ϴ�.");
            return;
        }

        isInitialized = false;

        masterSlider.minValue = 0f;
        masterSlider.maxValue = 1f;
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        masterSlider.value = SoundSettingsManager.Instance.MasterVolume;
        bgmSlider.value = SoundSettingsManager.Instance.BgmVolume;
        sfxSlider.value = SoundSettingsManager.Instance.SfxVolume;

        UpdateValueText(masterValueText, masterSlider.value);
        UpdateValueText(bgmValueText, bgmSlider.value);
        UpdateValueText(sfxValueText, sfxSlider.value);

        isInitialized = true;
    }

    private void OnMasterChanged(float value)
    {
        UpdateValueText(masterValueText, value);

        if (!isInitialized) return;
        SoundSettingsManager.Instance?.SetMasterVolume(value);
    }

    private void OnBgmChanged(float value)
    {
        UpdateValueText(bgmValueText, value);

        if (!isInitialized) return;
        SoundSettingsManager.Instance?.SetBgmVolume(value);
    }

    private void OnSfxChanged(float value)
    {
        UpdateValueText(sfxValueText, value);

        if (!isInitialized) return;
        SoundSettingsManager.Instance?.SetSfxVolume(value);
    }

    private void UpdateValueText(TMP_Text textUI, float value)
    {
        if (textUI != null)
        {
            int percent = Mathf.RoundToInt(value * 100f);
            textUI.text = percent + "%";
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshUIFromManager();
    }

    /// <summary>
    /// 패널을 숨긴다. PauseManager의 상태(timeScale 등)는 건드리지 않는다.
    /// 외부(PauseManager)에서 호출 시에도 재귀 루프가 발생하지 않는다.
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// CloseButton에서 호출된다.
    /// 패널을 숨기고 PauseManager에 재개를 요청한다.
    /// PauseManager.DoResume() 안에서 Close()를 부르는 경로와 분리되어 있으므로
    /// 재귀 루프가 발생하지 않는다.
    /// </summary>
    public void CloseAndResume()
    {
        gameObject.SetActive(false);   // 패널 숨김
        _pauseManager?.DoResume();     // PauseManager에 재개 요청 (이미 _isPaused=false이면 가드에서 차단)
    }

    public void ResetSettings()
    {
        if (SoundSettingsManager.Instance == null)
            return;

        SoundSettingsManager.Instance.ResetToDefault();
        RefreshUIFromManager();
    }

    // ── 일시정지 전용 API ─────────────────────────────────────────────

    /// <summary>PauseManager가 인스턴스화 후 한 번 호출한다.</summary>
    public void SetPauseManager(PauseManager pm)
    {
        _pauseManager = pm;
    }

    /// <summary>
    /// 씬에 따라 GiveUpButton 활성 여부를 설정한다.
    /// isTitleScene=true 이면 버튼 숨김, false 이면 표시.
    /// </summary>
    public void SetupForScene(bool isTitleScene)
    {
        if (giveUpButton != null)
            giveUpButton.gameObject.SetActive(!isTitleScene);
    }

    private void OnGiveUpClicked()
    {
        if (_pauseManager != null)
            _pauseManager.GiveUp();
        else
            SceneManager.LoadScene(1); // 폴백: 타이틀(빌드 인덱스 1)
    }
}