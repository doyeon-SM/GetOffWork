using UnityEngine;

public class TitleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private SoundSettingsManager soundSettingsManagerPrefab;
    [SerializeField] private PauseManager         pauseManagerPrefab;

    private void Awake()
    {
        if (SoundSettingsManager.Instance == null && soundSettingsManagerPrefab != null)
            Instantiate(soundSettingsManagerPrefab);

        if (PauseManager.Instance == null && pauseManagerPrefab != null)
            Instantiate(pauseManagerPrefab);
    }
}