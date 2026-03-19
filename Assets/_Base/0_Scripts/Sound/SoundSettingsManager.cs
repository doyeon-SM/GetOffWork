using UnityEngine;
using UnityEngine.Audio;

public class SoundSettingsManager : MonoBehaviour
{
    public static SoundSettingsManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string bgmParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    private const string MASTER_KEY = "Sound_Master";
    private const string BGM_KEY = "Sound_BGM";
    private const string SFX_KEY = "Sound_SFX";

    // 0~1 범위로 관리
    public float MasterVolume { get; private set; } = 0.5f; // 초기값 50%
    public float BgmVolume { get; private set; } = 1.0f;
    public float SfxVolume { get; private set; } = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyAllVolumes();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyVolume(masterParam, MasterVolume);
        SaveSettings();
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        ApplyVolume(bgmParam, BgmVolume);
        SaveSettings();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        ApplyVolume(sfxParam, SfxVolume);
        SaveSettings();
    }

    public void ApplyAllVolumes()
    {
        ApplyVolume(masterParam, MasterVolume);
        ApplyVolume(bgmParam, BgmVolume);
        ApplyVolume(sfxParam, SfxVolume);
    }

    private void ApplyVolume(string exposedParamName, float normalizedValue)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[SoundSettingsManager] AudioMixer가 연결되지 않았습니다.");
            return;
        }

        // 0이면 log10 에러 방지용 최소값
        float clamped = Mathf.Clamp(normalizedValue, 0.0001f, 1f);

        // 0~1 Slider 값을 dB(-80 ~ 0 근사)로 변환
        float dB = Mathf.Log10(clamped) * 20f;

        bool result = audioMixer.SetFloat(exposedParamName, dB);
        if (!result)
        {
            Debug.LogWarning($"[SoundSettingsManager] '{exposedParamName}' 파라미터를 찾지 못했습니다. Mixer에서 Expose 여부를 확인하세요.");
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_KEY, MasterVolume);
        PlayerPrefs.SetFloat(BGM_KEY, BgmVolume);
        PlayerPrefs.SetFloat(SFX_KEY, SfxVolume);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MASTER_KEY, 0.5f); // 초기값 50%
        BgmVolume = PlayerPrefs.GetFloat(BGM_KEY, 1.0f);
        SfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1.0f);
    }

    public void ResetToDefault()
    {
        MasterVolume = 0.5f;
        BgmVolume = 1.0f;
        SfxVolume = 1.0f;

        ApplyAllVolumes();
        SaveSettings();
    }
}