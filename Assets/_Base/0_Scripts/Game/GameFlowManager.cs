using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum MorningAction
    {
        None,
        ConvenienceStore,
        GoToWork
    }

    [Header("게임 진행 정보")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private MorningAction selectedMorningAction = MorningAction.None;

    [Header("플레이어 저장 데이터")]
    [SerializeField] private PlayerStat savedPlayerStats = new PlayerStat(0, 0.5f, 0.2f, 0.2f, 0);
    [SerializeField] private int PlayerLevel = 1;
    [SerializeField] private int DayGoalPerformance = 10;

    public int CurrentDay => currentDay;
    public MorningAction SelectedMorningAction => selectedMorningAction;
    public PlayerStat SavedPlayerStats => savedPlayerStats;
    public int SavedPlayerLevel => PlayerLevel;
    public int SavedGoalPerformance => DayGoalPerformance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewGame()
    {
        currentDay = 1;
        selectedMorningAction = MorningAction.None;

        savedPlayerStats = new PlayerStat(
            performance: 0,
            kindness: 0.5f,
            stress: 0.2f,
            reliability: 0.2f,
            pay: 0
        );

        PlayerLevel = 1;
        DayGoalPerformance = 10;

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("SceneManager Instance가 없습니다.");
            return;
        }

        GameSceneManager.Instance.GoToHomeScene();
    }

    public void SelectMorningAction(MorningAction action)
    {
        selectedMorningAction = action;
    }

    public void StartWorkDay()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("SceneManager Instance가 없습니다.");
            return;
        }

        GameSceneManager.Instance.GoToMainScene();
    }

    public void FinishDayAndGoNext()
    {
        currentDay++;
        selectedMorningAction = MorningAction.None;

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("SceneManager Instance가 없습니다.");
            return;
        }

        GameSceneManager.Instance.GoToHomeScene();
    }

    public void ReturnToTitle()
    {
        currentDay = 1;
        selectedMorningAction = MorningAction.None;

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("SceneManager Instance가 없습니다.");
            return;
        }

        GameSceneManager.Instance.GoTotileScene();
    }

    public void SavePlayerState(PlayerBase playerBase)
    {
        if (playerBase == null)
            return;

        savedPlayerStats = playerBase.CurrentStats;
        PlayerLevel = playerBase.PlayerLevel;
        DayGoalPerformance = playerBase.GoalPerformance;
    }

    public void ApplySavedStateToPlayer(PlayerBase playerBase)
    {
        if (playerBase == null)
            return;

        playerBase.InitializeForNewGame(savedPlayerStats, PlayerLevel, DayGoalPerformance);
    }
}