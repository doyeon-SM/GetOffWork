using UnityEngine;

public class TitleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private SoundSettingsManager soundSettingsManagerPrefab;

    private void Awake()
    {
        if (SoundSettingsManager.Instance == null)
        {
            Instantiate(soundSettingsManagerPrefab);
        }
    }
}