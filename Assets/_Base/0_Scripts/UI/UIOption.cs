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

    private bool isInitialized = false;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);

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
            Debug.LogWarning("[OptionUI] SoundSettingsManager.Instance가 없습니다.");
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

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void ResetSettings()
    {
        if (SoundSettingsManager.Instance == null)
            return;

        SoundSettingsManager.Instance.ResetToDefault();
        RefreshUIFromManager();
    }
}