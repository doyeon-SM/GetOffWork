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

    public void OnClickConvenienceStore()
    {
        if (GameFlowManager.Instance == null || playerBase == null)
            return;

        GameFlowManager.Instance.SelectMorningAction(GameFlowManager.MorningAction.ConvenienceStore);

        // 예시 효과
        playerBase.AddPay(-1000); // 실패할 수도 있음
        playerBase.SubtractStat(PlayerBase.PlayerStat.Stress, 1);

        GameFlowManager.Instance.SavePlayerState(playerBase);

        Debug.Log("편의점에 다녀왔습니다.");
        // 여기서 UI나 이벤트를 더 넣을 수 있음
    }

    public void OnClickGoToWork()
    {
        if (GameFlowManager.Instance == null || playerBase == null)
            return;

        GameFlowManager.Instance.SelectMorningAction(GameFlowManager.MorningAction.GoToWork);
        GameFlowManager.Instance.SavePlayerState(playerBase);
        GameFlowManager.Instance.StartWorkDay();
    }
}