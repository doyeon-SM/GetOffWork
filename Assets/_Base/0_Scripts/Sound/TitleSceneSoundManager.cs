using UnityEngine;

public class TitleSceneSoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip TitleSceneBGM;
    [SerializeField] private AudioClip ButtonClickSFX;

    private void Awake()
    {
        
    }
    private void Start()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.PlayBgm(TitleSceneBGM);
    }

    public void OnClickButton()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.PlaySfxOneShot(ButtonClickSFX);
    }
}
