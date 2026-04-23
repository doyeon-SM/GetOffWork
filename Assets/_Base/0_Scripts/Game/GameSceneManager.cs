using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class GameSceneManager : MonoBehaviour
{   
    public static GameSceneManager Instance;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// -> MainScene(2)
    /// moning -> working
    /// </summary>
    public void GoToMainScene()
    {
        UnitySceneManager.LoadScene(2);
    }

    /// <summary>
    /// -> HomeScene(1)
    /// startbutton = scene move
    /// working ending -> moning
    /// </summary>
    public void GoToHomeScene()
    {
        UnitySceneManager.LoadScene(1);
    }

    /// <summary>
    /// -> TitleScene(0)
    /// </summary>
    /// <summary>
    /// -> TitleScene(0)
    /// </summary>
    public void GoTotileScene()
    {
        UnitySceneManager.LoadScene(0);
    }

    /// <summary>
    /// -> EndingScene(3) — 추후 추가될 엔딩씬
    /// 현재는 TitleScene(0)으로 폴백
    /// </summary>
    public void GoToEndingScene()
    {
        const int ENDING_SCENE_INDEX = 3;
        int sceneCount = UnitySceneManager.sceneCountInBuildSettings;
        if (sceneCount > ENDING_SCENE_INDEX)
        {
            UnitySceneManager.LoadScene(ENDING_SCENE_INDEX);
        }
        else
        {
            Debug.LogWarning($"[GameSceneManager] EndingScene(인덱스 {ENDING_SCENE_INDEX}) 미등록 — TitleScene으로 폴백 (등록된 씬: {sceneCount}개)");
            UnitySceneManager.LoadScene(0); // 0_TitleScene 
        }
    }
}
