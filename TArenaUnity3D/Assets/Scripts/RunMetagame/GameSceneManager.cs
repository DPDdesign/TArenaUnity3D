using UnityEngine;
using UnityEngine.SceneManagement;

public enum RunMetagameScreen
{
    None,
    StartRun,
    RunMap,
    RunShop,
    RewardMap,
    SummaryValue,
    SavedArmies,
    BattleResult,
    Battle
}

[DisallowMultipleComponent]
public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Lifetime")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool showDefaultScreenOnStart = true;
    [SerializeField] private RunMetagameScreen defaultScreen = RunMetagameScreen.StartRun;

    [Header("UI Shell")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private GameObject startRunUI;
    [SerializeField] private GameObject runMapUI;
    [SerializeField] private GameObject runShopUI;
    [SerializeField] private GameObject rewardMapUI;
    [SerializeField] private GameObject summaryValueUI;
    [SerializeField] private GameObject savedArmiesUI;
    [SerializeField] private GameObject battleResultUI;

    [Header("Battle Scene")]
    [SerializeField] private string battleSceneName = string.Empty;
    [SerializeField] private bool loadBattleSceneOnEnter;
    [SerializeField] private bool unloadBattleSceneOnReturn;

    private RunMetagameScreen currentScreen = RunMetagameScreen.None;
    private RunMetagameScreen screenBeforeBattle = RunMetagameScreen.None;
    private bool battleSceneLoadedByManager;

    public RunMetagameScreen CurrentScreen
    {
        get { return currentScreen; }
    }

    public RunMetagameScreen ScreenBeforeBattle
    {
        get { return screenBeforeBattle; }
    }

    private void Awake()
    {
        if (!RegisterSingleton())
        {
            return;
        }

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (showDefaultScreenOnStart && currentScreen == RunMetagameScreen.None)
        {
            Show(defaultScreen);
        }
    }

    public void ShowStartRun()
    {
        if (currentScreen != RunMetagameScreen.None && currentScreen != RunMetagameScreen.StartRun)
        {
            OfflineModeDatabaseComposition.EndRunGenerationSession();
        }

        Show(RunMetagameScreen.StartRun);
    }

    public void ShowRunMap()
    {
        Show(RunMetagameScreen.RunMap);
    }

    public void ShowRunShop()
    {
        Show(RunMetagameScreen.RunShop);
    }

    public void ShowShop()
    {
        ShowRunShop();
    }

    public void ShowRewardMap()
    {
        Show(RunMetagameScreen.RewardMap);
    }

    public void ShowReward()
    {
        ShowRewardMap();
    }

    public void ShowSummaryValue()
    {
        Show(RunMetagameScreen.SummaryValue);
    }

    public void ShowSummary()
    {
        ShowSummaryValue();
    }

    public void ShowSavedArmies()
    {
        Show(RunMetagameScreen.SavedArmies);
    }

    public void ShowBattleResult()
    {
        Show(RunMetagameScreen.BattleResult);
    }

    public void Show(RunMetagameScreen screen)
    {
        if (screen == RunMetagameScreen.Battle)
        {
            EnterBattle();
            return;
        }

        SetActive(uiRoot, true);
        SetAllScreensInactive();
        SetScreenActive(screen, true);
        currentScreen = screen;
        NotifyScreenShown(screen);
    }

    public void EnterBattle()
    {
        EnterBattleFrom(currentScreen);
    }

    public void EnterBattleFromRunMap()
    {
        EnterBattleFrom(RunMetagameScreen.RunMap);
    }

    private void EnterBattleFrom(RunMetagameScreen returnScreen)
    {
        RunBattleTacticalResultBridge.ResetRuntimeResultLatch();

        if (currentScreen != RunMetagameScreen.Battle)
        {
            screenBeforeBattle = returnScreen == RunMetagameScreen.None || returnScreen == RunMetagameScreen.Battle
                ? currentScreen
                : returnScreen;
        }

        currentScreen = RunMetagameScreen.Battle;
        SetActive(uiRoot, false);

        if (loadBattleSceneOnEnter)
        {
            LoadBattleScene();
        }
    }

    public void ReturnFromBattle()
    {
        if (unloadBattleSceneOnReturn)
        {
            UnloadBattleScene();
        }

        RunMetagameScreen target = screenBeforeBattle == RunMetagameScreen.None
            ? defaultScreen
            : screenBeforeBattle;

        screenBeforeBattle = RunMetagameScreen.None;
        Show(target);
    }

    public void ReturnFromBattleWon()
    {
        ReturnFromBattleTo(RunMetagameScreen.RunMap);
    }

    public void ReturnFromBattleLost()
    {
        ReturnFromBattleTo(defaultScreen);
    }

    private void ReturnFromBattleTo(RunMetagameScreen target)
    {
        if (unloadBattleSceneOnReturn)
        {
            UnloadBattleScene();
        }

        screenBeforeBattle = RunMetagameScreen.None;
        if (target == RunMetagameScreen.StartRun)
        {
            ShowStartRun();
            return;
        }

        Show(target);
    }

    private void LoadBattleScene()
    {
        if (string.IsNullOrEmpty(battleSceneName))
        {
            Debug.LogWarning("[GameSceneManager] Battle scene name is not assigned.");
            return;
        }

        Scene scene = SceneManager.GetSceneByName(battleSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            return;
        }

        SceneManager.LoadScene(battleSceneName, LoadSceneMode.Additive);
        battleSceneLoadedByManager = true;
    }

    private void UnloadBattleScene()
    {
        if (!battleSceneLoadedByManager || string.IsNullOrEmpty(battleSceneName))
        {
            return;
        }

        Scene scene = SceneManager.GetSceneByName(battleSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(battleSceneName);
        }

        battleSceneLoadedByManager = false;
    }

    private void SetAllScreensInactive()
    {
        SetActive(startRunUI, false);
        SetActive(runMapUI, false);
        SetActive(runShopUI, false);
        SetActive(rewardMapUI, false);
        SetActive(summaryValueUI, false);
        SetActive(savedArmiesUI, false);
        SetActive(battleResultUI, false);
    }

    private void SetScreenActive(RunMetagameScreen screen, bool active)
    {
        switch (screen)
        {
            case RunMetagameScreen.StartRun:
                SetActive(startRunUI, active);
                break;
            case RunMetagameScreen.RunMap:
                SetActive(runMapUI, active);
                break;
            case RunMetagameScreen.RunShop:
                SetActive(runShopUI, active);
                break;
            case RunMetagameScreen.RewardMap:
                SetActive(rewardMapUI, active);
                break;
            case RunMetagameScreen.SummaryValue:
                SetActive(summaryValueUI, active);
                break;
            case RunMetagameScreen.SavedArmies:
                SetActive(savedArmiesUI, active);
                break;
            case RunMetagameScreen.BattleResult:
                SetActive(battleResultUI, active);
                break;
        }
    }

    private void NotifyScreenShown(RunMetagameScreen screen)
    {
        if (screen != RunMetagameScreen.RunMap || runMapUI == null)
        {
            return;
        }

        RunMapController controller = runMapUI.GetComponentInChildren<RunMapController>(true);
        if (controller != null)
        {
            controller.RefreshFromPersistedRun();
            return;
        }

        Debug.LogWarning("[GameSceneManager] Run Map UI is assigned, but no RunMapController was found under it.");
    }

    private bool RegisterSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            return true;
        }

        if (Instance == this)
        {
            return true;
        }

        Destroy(gameObject);
        return false;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
