using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunBattleMockupScreenController : MonoBehaviour
{
    [SerializeField] private string runId = "offline-run";
    [SerializeField] private int stageIndex = 2;
    [SerializeField] private int runCurrency = 120;
    [SerializeField] private RunBattleContextCardView[] battleCards = new RunBattleContextCardView[0];
    [SerializeField] private RunBattleStackRowView[] currentArmyRows = new RunBattleStackRowView[0];
    [SerializeField] private RunBattleStackRowView[] afterBattleRows = new RunBattleStackRowView[0];
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI launchPayloadText;
    [SerializeField] private TextMeshProUGUI completionPayloadText;
    [SerializeField] private TextMeshProUGUI nextScreenText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button launchButton;
    [SerializeField] private Button completeWinButton;
    [SerializeField] private Button completeLossButton;

    private readonly List<RunBattleEncounterDefinition> encounters = new List<RunBattleEncounterDefinition>();
    private OfflineRunBattleAdapter adapter;
    private DataMapper dataMapper;
    private RunBattleArmySnapshot currentArmy;
    private RunBattleLaunchViewData preparedBattle;
    private string selectedEncounterId = "enc-iron-border-clash";

    private void Awake()
    {
        adapter = new OfflineRunBattleAdapter(new InMemoryRunBattleStore());
        dataMapper = DataMapper.Instance;
        currentArmy = BuildMockArmy("mock-run-battle-before", 28, 10, 5, 22, 0, 0, 0, 0);
        BuildEncounters();
        WireButtons();
        Refresh();
    }

    public void ConfigureMockup(
        RunBattleContextCardView[] battleCards,
        RunBattleStackRowView[] currentArmyRows,
        RunBattleStackRowView[] afterBattleRows,
        TextMeshProUGUI headerText,
        TextMeshProUGUI launchPayloadText,
        TextMeshProUGUI completionPayloadText,
        TextMeshProUGUI nextScreenText,
        TextMeshProUGUI messageText,
        Button launchButton,
        Button completeWinButton,
        Button completeLossButton)
    {
        this.battleCards = battleCards ?? new RunBattleContextCardView[0];
        this.currentArmyRows = currentArmyRows ?? new RunBattleStackRowView[0];
        this.afterBattleRows = afterBattleRows ?? new RunBattleStackRowView[0];
        this.headerText = headerText;
        this.launchPayloadText = launchPayloadText;
        this.completionPayloadText = completionPayloadText;
        this.nextScreenText = nextScreenText;
        this.messageText = messageText;
        this.launchButton = launchButton;
        this.completeWinButton = completeWinButton;
        this.completeLossButton = completeLossButton;
    }

    public void SelectEncounter(string encounterId)
    {
        selectedEncounterId = encounterId;
        preparedBattle = null;
        Refresh();
    }

    public void PrepareSelectedBattle()
    {
        RunBattleEncounterDefinition encounter = SelectedEncounter();
        if (encounter == null)
        {
            SetText(messageText, "Select a battle encounter.");
            return;
        }

        preparedBattle = adapter.PrepareBattle(new RunBattlePrepareRequest(
            runId,
            encounter.RouteNodeId,
            encounter.EncounterId,
            stageIndex,
            runCurrency,
            currentArmy));

        SetText(messageText, preparedBattle.Message);
        Refresh();
    }

    public void CompleteAsWin()
    {
        CompleteBattle(RunBattleOutcome.Win);
    }

    public void CompleteAsLoss()
    {
        CompleteBattle(RunBattleOutcome.Loss);
    }

    public void Refresh()
    {
        BindBattleCards();
        BindArmyRows(currentArmyRows, currentArmy);

        RunBattleArmySnapshot afterArmy = preparedBattle == null
            ? BuildPreviewArmy(RunBattleOutcome.Win)
            : BuildPreviewArmy(RunBattleOutcome.Win);
        BindArmyRows(afterBattleRows, afterArmy);

        RunBattleEncounterDefinition encounter = SelectedEncounter();
        SetText(headerText, encounter == null ? "Run Battle Bridge" : "Run Battle Bridge - " + encounter.DisplayName);
        SetText(launchPayloadText, BuildLaunchText(encounter));
        SetText(completionPayloadText, preparedBattle == null ? "Prepare battle to create a runBattleId." : "Prepared: " + preparedBattle.RunBattleId);
        SetText(nextScreenText, preparedBattle == null ? "Next screen waits for completion." : "Win -> Reward or FinalSummary / Loss -> RunLoss");

        if (completeWinButton != null)
        {
            completeWinButton.interactable = preparedBattle != null;
        }

        if (completeLossButton != null)
        {
            completeLossButton.interactable = preparedBattle != null;
        }
    }

    private void CompleteBattle(RunBattleOutcome outcome)
    {
        if (preparedBattle == null)
        {
            SetText(messageText, "Prepare battle before completing it.");
            return;
        }

        RunBattleCompletionResult result = adapter.CompleteBattle(new RunBattleCompletionPayload(
            preparedBattle.RunBattleId,
            outcome,
            BuildPreviewArmy(outcome),
            outcome == RunBattleOutcome.Win && preparedBattle.Encounter.NodeType == RunBattleNodeType.Battle ? 45 : 0,
            "mock-completion-payload",
            "offline-local-run-battle-mockup"));

        if (result == null || !result.Success)
        {
            SetText(messageText, result == null ? "Completion returned no result." : result.Message);
            return;
        }

        BindArmyRows(afterBattleRows, result.CompletionRecord.ArmyAfterBattle);
        SetText(completionPayloadText, BuildCompletionText(result.CompletionRecord));
        SetText(nextScreenText, "Next screen: " + result.CompletionRecord.NextScreen);
        SetText(messageText, result.Message);
    }

    private void BindBattleCards()
    {
        for (int i = 0; i < battleCards.Length; i++)
        {
            RunBattleContextCardView card = battleCards[i];
            RunBattleEncounterDefinition encounter = i < encounters.Count ? encounters[i] : null;
            if (card == null)
            {
                continue;
            }

            bool selected = encounter != null && encounter.EncounterId == selectedEncounterId;
            card.Bind(encounter, selected);
            if (card.Button != null)
            {
                card.Button.onClick.RemoveAllListeners();
                if (encounter != null)
                {
                    string id = encounter.EncounterId;
                    card.Button.onClick.AddListener(delegate { SelectEncounter(id); });
                }
            }
        }
    }

    private void BindArmyRows(RunBattleStackRowView[] rows, RunBattleArmySnapshot army)
    {
        if (rows == null)
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            RunBattleStackRowView row = rows[i];
            RunBattleStackSnapshot stack = army != null && army.Stacks != null && i < army.Stacks.Count ? army.Stacks[i] : null;
            if (row != null)
            {
                row.Bind(stack, dataMapper);
            }
        }
    }

    private string BuildLaunchText(RunBattleEncounterDefinition encounter)
    {
        if (encounter == null)
        {
            return "No encounter selected.";
        }

        return
            "runId: " + runId +
            "\nrouteNodeId: " + encounter.RouteNodeId +
            "\nencounterId: " + encounter.EncounterId +
            "\ncurrentArmySnapshotId: " + currentArmy.SnapshotId +
            "\nenemyArmySourceId: " + encounter.EnemyArmySourceId +
            "\nenemyGoal: " + encounter.EnemyGoal;
    }

    private static string BuildCompletionText(RunBattleCompletionRecord record)
    {
        return
            "outcome: " + record.Outcome +
            "\ntotalLosses: " + record.TotalLosses +
            "\nrunGoldGained: " + record.RunGoldGained +
            "\nresultSource: " + record.ResultSource;
    }

    private RunBattleEncounterDefinition SelectedEncounter()
    {
        for (int i = 0; i < encounters.Count; i++)
        {
            if (encounters[i].EncounterId == selectedEncounterId)
            {
                return encounters[i];
            }
        }

        return encounters.Count == 0 ? null : encounters[0];
    }

    private void BuildEncounters()
    {
        encounters.Clear();
        encounters.Add(new RunBattleEncounterDefinition("enc-iron-border-clash", "n1", RunBattleNodeType.Battle, "Border Clash", "Low", 1450, "enemy-build-iron-border-clash", RunBattleEnemyGoal.TryToWin));
        encounters.Add(new RunBattleEncounterDefinition("enc-iron-hill-ambush", "n4", RunBattleNodeType.Battle, "Hill Ambush", "Medium", 2050, "enemy-build-iron-hill-ambush", RunBattleEnemyGoal.DealMaximumLosses));
        encounters.Add(new RunBattleEncounterDefinition("enc-final-proof", "n5", RunBattleNodeType.Final, "Final Proof", "High", 2650, "enemy-build-final-proof", RunBattleEnemyGoal.TryToWin));
    }

    private RunBattleArmySnapshot BuildPreviewArmy(RunBattleOutcome outcome)
    {
        if (outcome == RunBattleOutcome.Loss)
        {
            return BuildMockArmy("mock-run-battle-after-loss", 0, 0, 0, 0, 28, 10, 5, 22);
        }

        return BuildMockArmy("mock-run-battle-after-win", 21, 10, 2, 22, 7, 0, 3, 0);
    }

    private RunBattleArmySnapshot BuildMockArmy(
        string snapshotId,
        int rushers,
        int throwers,
        int healers,
        int wisps,
        int rusherLost,
        int throwerLost,
        int healerLost,
        int wispLost)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", rushers, rusherLost, "Chope", "Rush"),
            Stack("stack-thrower", "Thrower", throwers, throwerLost, "Range_Stance_Barb", "Double_Throw"),
            Stack("stack-healer", "Healer", healers, healerLost, "Tough_Skin", "Defence_Ritual"),
            Stack("stack-wisp", "Wisp", wisps, wispLost, "Blind_by_light", "Unstoppable_Light")
        };

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RunBattleArmySnapshot(snapshotId, total, stacks);
    }

    private RunBattleStackSnapshot Stack(string stackId, string unitId, int amount, int lost, params string[] skillIds)
    {
        DataMapper.UnitDefinition unit = dataMapper == null ? null : dataMapper.FindUnit(unitId);
        int cost = unit == null ? 0 : unit.Cost;
        List<RunBattleSkillState> skills = new List<RunBattleSkillState>();
        for (int i = 0; i < skillIds.Length; i++)
        {
            skills.Add(new RunBattleSkillState(skillIds[i], i == 0));
        }

        return new RunBattleStackSnapshot(
            stackId,
            unitId,
            unit == null ? unitId : unit.Name,
            "I",
            1,
            amount,
            lost,
            amount * cost,
            skills);
    }

    private void WireButtons()
    {
        if (launchButton != null)
        {
            WireRuntimeButton(launchButton, PrepareSelectedBattle);
        }

        if (completeWinButton != null)
        {
            WireRuntimeButton(completeWinButton, CompleteAsWin);
        }

        if (completeLossButton != null)
        {
            WireRuntimeButton(completeLossButton, CompleteAsLoss);
        }
    }

    private static void WireRuntimeButton(Button button, UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
