using UnityEngine;

public class MorningSceneController : MonoBehaviour
{
    public void SelectConvenienceStore()
    {
        if (GameFlowManager.Instance == null) return;

        GameFlowManager.Instance.SelectMorningAction(GameFlowManager.MorningAction.ConvenienceStore);

        // 필요하면 여기서 스탯 변화나 이벤트 추가 가능
        GameFlowManager.Instance.StartWorkDay();
    }

    public void SelectGoToWork()
    {
        if (GameFlowManager.Instance == null) return;

        GameFlowManager.Instance.SelectMorningAction(GameFlowManager.MorningAction.GoToWork);
        GameFlowManager.Instance.StartWorkDay();
    }
}