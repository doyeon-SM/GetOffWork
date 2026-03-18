using UnityEngine;

public class UITitleController : MonoBehaviour
{
    public void OnClickStartButton()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("GameFlowManager Instance가 없습니다.");
            return;
        }

        GameFlowManager.Instance.StartNewGame();
    }

    public void OnClickQuitButton()
    {
        Application.Quit();
    }
}