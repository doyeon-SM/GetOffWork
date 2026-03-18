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
            DontDestroyOnLoad(gameObject); // 씬 변경 시 유지
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
    /// 게임 종료 or 게임 오버
    /// </summary>
    public void GoTotileScene()
    {
        UnitySceneManager.LoadScene(0);
    }
}
