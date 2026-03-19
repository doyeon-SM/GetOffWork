using UnityEngine;

public class MorningHomeController : MonoBehaviour
{
    [SerializeField] private PlayerBase playerBase;

    private void Start()
    {
        ResolvePlayerBase();

        if (GameFlowManager.Instance != null && playerBase != null)
        {
            GameFlowManager.Instance.ApplySavedStateToPlayer(playerBase);
        }
    }

    private void ResolvePlayerBase()
    {
        if (playerBase != null)
            return;

        playerBase = PlayerBase.Instance;

        if (playerBase == null)
        {
            Debug.LogError("PlayerBase Instance가 없습니다!");
        }
    }
}