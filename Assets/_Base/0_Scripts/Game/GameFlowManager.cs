using System.Collections.Generic;
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

    [Header("게임 진행 설정")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private MorningAction selectedMorningAction = MorningAction.None;

    [Header("플레이어 저장 데이터")]
    [SerializeField] private PlayerStat savedPlayerStats = new PlayerStat(0, 0.2f, 0.2f, 0.5f, 0);
    [SerializeField] private int PlayerLevel = 1;
    [SerializeField] private int DayGoalPerformance = 20;

    [Header("해금된 메뉴얼 목록")]
    [SerializeField] private List<ManualDataSO> unlockedManuals = new();

    // ── 프로퍼티 ──────────────────────────────────────────────────────────────
    public int CurrentDay => currentDay;
    public MorningAction SelectedMorningAction => selectedMorningAction;
    public PlayerStat SavedPlayerStats => savedPlayerStats;
    public int SavedPlayerLevel => PlayerLevel;
    public int SavedGoalPerformance => DayGoalPerformance;

    /// <summary>현재까지 해금된 메뉴얼 목록 (읽기 전용)</summary>
    public IReadOnlyList<ManualDataSO> UnlockedManuals => unlockedManuals;

    // ── 생명주기 ──────────────────────────────────────────────────────────────
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

    // ── 메뉴얼 해금 시스템 ────────────────────────────────────────────────────
    /// <summary>
    /// 메뉴얼을 해금한다. 이미 해금된 경우 무시한다.
    /// </summary>
    public bool UnlockManual(ManualDataSO manual)
    {
        if (manual == null) return false;
        if (unlockedManuals.Contains(manual)) return false;
        unlockedManuals.Add(manual);
        Debug.Log($"[GameFlowManager] 메뉴얼 해금: {manual.manualTitle}");
        return true;
    }

    /// <summary>
    /// 메뉴얼 해금 여부를 반환한다.
    /// </summary>
    public bool IsManualUnlocked(ManualDataSO manual)
    {
        if (manual == null) return false;
        return unlockedManuals.Contains(manual);
    }

    /// <summary>
    /// manualTitle string으로 해금 여부를 반환한다. (튜토리얼 등 외부 연동용)
    /// </summary>
    public bool IsManualUnlocked(string manualTitle)
    {
        foreach (var m in unlockedManuals)
            if (m != null && m.manualTitle == manualTitle) return true;
        return false;
    }

    /// <summary>
    /// 해금 목록을 초기화한다. NewGame 시 호출한다.
    /// </summary>
    public void ClearUnlockedManuals()
    {
        unlockedManuals.Clear();
        Debug.Log("[GameFlowManager] 메뉴얼 해금 목록 초기화");
    }

    // ── 게임 흐름 ─────────────────────────────────────────────────────────────
    public void StartNewGame()
    {
        currentDay = 1;
        selectedMorningAction = MorningAction.None;

        savedPlayerStats = new PlayerStat(
            performance: 0,
            kindness: 0.2f,
            stress: 0.2f,
            reliability: 0.5f,
            pay: 0
        );

        PlayerLevel = 1;
        DayGoalPerformance = 20; // 초기값, 이후 SetGoalPerformance(1)로 덮어씌워짐

        // PlayerBase의 승진 사이클 초기화 후 1일차 목표 계산
        if (PlayerBase.Instance != null)
        {
            PlayerBase.Instance.InitializeForNewGame(savedPlayerStats, PlayerLevel, DayGoalPerformance);
            PlayerBase.Instance.SetGoalPerformance(1);
            DayGoalPerformance = PlayerBase.Instance.GoalPerformance;
        }

        ClearUnlockedManuals();

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
        PlayerBase.Instance.SetGoalPerformance(currentDay);
        SavePlayerState(PlayerBase.Instance);
        
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

    /// <summary>
    /// 게임오버 처리. 엔딩 종류를 저장하고 엔딩씬(또는 타이틀)으로 이동한다.
    /// 추후 3_EndingScene 추가 시 GoToEndingScene()으로 교체하면 된다.
    /// </summary>
    public void TriggerGameOver(PlayerBase.PlayerEnding endingType)
    {
        Debug.Log($"[GameFlowManager] 게임오버 — {endingType}");
        // 추후 엔딩씬에서 참조할 수 있도록 저장
        LastEnding = endingType;
        if (GameSceneManager.Instance == null) return;
        // 현재: TitleScene / 추후: GoToEndingScene()으로 교체
        GameSceneManager.Instance.GoToEndingScene();
    }

    /// <summary>마지막 엔딩 종류. EndingScene에서 참조용.</summary>
    public PlayerBase.PlayerEnding LastEnding { get; private set; }

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
