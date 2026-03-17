using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyBestResolution();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void ApplyBestResolution()
    {
        Resolution[] resolutions = Screen.resolutions;
        Resolution best = resolutions[resolutions.Length - 1];

        Screen.SetResolution(
            best.width,
            best.height,
            FullScreenMode.FullScreenWindow
        );

        Debug.Log($"해상도 설정: {best.width}x{best.height}");
    }
}