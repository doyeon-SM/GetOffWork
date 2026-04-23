using UnityEngine;
using UnityEngine.Audio;

public class SoundSettingsManager : MonoBehaviour
{
    public static SoundSettingsManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string bgmParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    private const string MASTER_KEY = "Sound_Master";
    private const string BGM_KEY    = "Sound_BGM";
    private const string SFX_KEY    = "Sound_SFX";

    [Header("AudioSource (Optional — 미연결 시 자동 추가)")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    // 0~1 정규화 값
    public float MasterVolume { get; private set; } = 0.5f;
    public float BgmVolume    { get; private set; } = 1.0f;
    public float SfxVolume    { get; private set; } = 1.0f;

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
        EnsureAudioSources();   // AudioSource → MixerGroup 연결 먼저
        ApplyAllVolumes();       // 그 다음 Mixer 볼륨 적용
    }

    // ── 볼륨 설정 (슬라이더 → Mixer 실시간 반영) ─────────────────────────────

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
        ApplyVolume(bgmParam,    BgmVolume);
        ApplyVolume(sfxParam,    SfxVolume);
    }

    private void ApplyVolume(string exposedParamName, float normalizedValue)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[SoundSettingsManager] AudioMixer가 연결되지 않았습니다.");
            return;
        }

        // 0 입력 시 log10 -inf 방지
        float clamped = Mathf.Clamp(normalizedValue, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;

        bool result = audioMixer.SetFloat(exposedParamName, dB);
        if (!result)
            Debug.LogWarning($"[SoundSettingsManager] '{exposedParamName}' 파라미터를 찾지 못했습니다. Mixer에서 Expose 여부를 확인하세요.");
    }

    // ── 저장 / 불러오기 ───────────────────────────────────────────────────────

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_KEY, MasterVolume);
        PlayerPrefs.SetFloat(BGM_KEY,    BgmVolume);
        PlayerPrefs.SetFloat(SFX_KEY,    SfxVolume);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MASTER_KEY, 0.5f);
        BgmVolume    = PlayerPrefs.GetFloat(BGM_KEY,    1.0f);
        SfxVolume    = PlayerPrefs.GetFloat(SFX_KEY,    1.0f);
    }

    public void ResetToDefault()
    {
        MasterVolume = 0.5f;
        BgmVolume    = 1.0f;
        SfxVolume    = 1.0f;
        ApplyAllVolumes();
        SaveSettings();
    }

    // ── BGM ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// BGM을 재생합니다. 볼륨은 AudioMixer(BGMVolume)가 제어합니다.
    /// AudioSource.volume은 1로 고정 — 직접 건드리면 Mixer 값과 충돌합니다.
    /// </summary>
    public void PlayBgm(AudioClip bgmClip, bool loop = true)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[SoundSettingsManager] PlayBgm 실패: bgmClip이 null입니다.");
            return;
        }
        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundSettingsManager] PlayBgm 실패: bgmSource가 없습니다.");
            return;
        }

        bgmSource.clip   = bgmClip;
        bgmSource.loop   = loop;
        bgmSource.volume = 1f;   // 볼륨 제어는 Mixer에 위임
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundSettingsManager] StopBgm 실패: bgmSource가 없습니다.");
            return;
        }
        bgmSource.Stop();
    }

    // ── SFX ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// SFX를 재생합니다. 볼륨은 AudioMixer(SFXVolume)가 제어합니다.
    /// PlayOneShot의 volumeScale은 1로 고정 — 직접 건드리면 Mixer 값과 충돌합니다.
    /// </summary>
    public void PlaySfxOneShot(AudioClip sfxClip)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("[SoundSettingsManager] PlaySfxOneShot 실패: sfxClip이 null입니다.");
            return;
        }
        if (sfxSource == null)
        {
            Debug.LogWarning("[SoundSettingsManager] PlaySfxOneShot 실패: sfxSource가 없습니다.");
            return;
        }

        sfxSource.PlayOneShot(sfxClip, 1f);  // 볼륨 제어는 Mixer에 위임
    }

    // ── 초기화 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// AudioSource가 Inspector에 연결되지 않았을 때 자동 추가하고,
    /// 반드시 outputAudioMixerGroup을 Mixer 그룹에 연결합니다.
    /// (연결하지 않으면 SetFloat()로 볼륨을 바꿔도 AudioSource에 반영되지 않습니다.)
    /// </summary>
    private void EnsureAudioSources()
    {
        // ── BGM Source ────────────────────────────────
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop        = true;
        bgmSource.volume      = 1f;

        // Mixer Group 연결 (Inspector에서 bgmMixerGroup을 연결했거나, 자동 탐색)
        if (bgmMixerGroup == null && audioMixer != null)
        {
            var found = audioMixer.FindMatchingGroups("BGM");
            if (found != null && found.Length > 0) bgmMixerGroup = found[0];
        }
        if (bgmMixerGroup != null)
            bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        else
            Debug.LogWarning("[SoundSettingsManager] BGM MixerGroup을 찾지 못했습니다. Inspector에서 bgmMixerGroup을 연결하세요.");

        // ── SFX Source ────────────────────────────────
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop        = false;
        sfxSource.volume      = 1f;

        if (sfxMixerGroup == null && audioMixer != null)
        {
            var found = audioMixer.FindMatchingGroups("SFX");
            if (found != null && found.Length > 0) sfxMixerGroup = found[0];
        }
        if (sfxMixerGroup != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        else
            Debug.LogWarning("[SoundSettingsManager] SFX MixerGroup을 찾지 못했습니다. Inspector에서 sfxMixerGroup을 연결하세요.");
    }
}