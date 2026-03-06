using UnityEngine;
using SM = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
   
    public static SceneManager Instance;

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
    /// TitleScene(0) -> MainScene(1) 이동
    /// </summary>
    public void GoToMainScene()
    {
        SM.LoadScene(1);
    }
}
