using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BattleResultScreenController : MonoBehaviour
{
    private enum BattleResultPrototypeFlowState
    {
        ResultShown,
        ArmyFocused,
        ContinueSelected,
        SavedArmiesRosterOpen
    }

    [Header("Sample Result Source")]
    [SerializeField] private string sampleResultId = "offline-async-result-027";
    [SerializeField] private int playerRankBefore = 1520;
    [SerializeField] private int accountXpBefore = 2170;

    [Header("Sections")]
    [SerializeField] private BattleResultArmySummaryCardView attackerArmyCard;
    [SerializeField] private BattleResultArmySummaryCardView defenderArmyCard;
    [SerializeField] private BattleResultRankDeltaPanelView rankDeltaPanel;
    [SerializeField] private BattleResultXpProgressPanelView xpProgressPanel;
    [SerializeField] private BattleResultCommandButtonView continueCommand;
    [SerializeField] private BattleResultCommandButtonView viewArmiesCommand;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private TextMeshProUGUI noArmyLostText;
    [SerializeField] private TextMeshProUGUI detailHeaderText;
    [SerializeField] private TextMeshProUGUI detailBodyText;
    [SerializeField] private TextMeshProUGUI flowStatusText;
    [SerializeField] private TextMeshProUGUI backendGapText;
    [SerializeField] private TextMeshProUGUI rosterPreviewText;
    [SerializeField] private GameObject rosterPreviewPanel;

    private BattleResultViewData currentResult;
    private string focusedArmyId;
    private BattleResultPrototypeFlowState flowState;
    private OfflineBattleResultAdapter adapter;

    private void Awake()
    {
        BuildAndRenderSampleResult();
        WireButtonsIfNeeded();
    }

    [ContextMenu("Rebuild Sample Battle Result")]
    public void BuildAndRenderSampleResult()
    {
        EnsureAdapter();
        currentResult = adapter.Find(sampleResultId);
        if (currentResult == null)
        {
            currentResult = adapter.Record(BuildSampleRequest());
        }

        Render(currentResult);
    }

    public void OnAttackerArmyClicked()
    {
        FocusArmy(currentResult == null ? null : currentResult.AttackerArmy, "Attacker Army");
    }

    public void OnDefenderArmyClicked()
    {
        FocusArmy(currentResult == null ? null : currentResult.DefenderArmy, "Defender Army");
    }

    public void OnContinueClicked()
    {
        if (!HasResult())
        {
            SetText(flowStatusText, "Continue blocked: no battle result payload.");
            return;
        }

        flowState = BattleResultPrototypeFlowState.ContinueSelected;
        BattleResultViewData storedResult = FindStoredCurrentResult();
        SetActive(rosterPreviewPanel, false);
        SetText(
            flowStatusText,
            "Flow state: " + flowState + " / result " + currentResult.AsyncBattleResultId
            + " keeps rank " + currentResult.RankAfter
            + ", account XP " + currentResult.AccountXpAfter
            + ", focused army " + focusedArmyId
            + ", store lookup " + (storedResult == null ? "missing" : "ok") + ".");
    }

    public void OnViewArmiesClicked()
    {
        if (!HasResult())
        {
            SetText(flowStatusText, "View Armies blocked: no battle result payload.");
            return;
        }

        flowState = BattleResultPrototypeFlowState.SavedArmiesRosterOpen;
        SetActive(rosterPreviewPanel, true);
        SetText(
            rosterPreviewText,
            BuildRosterPreview(currentResult));
        SetText(
            flowStatusText,
            "Flow state: " + flowState + " / roster opened for " + currentResult.AsyncBattleResultId + ".");
    }

    private void Render(BattleResultViewData result)
    {
        if (result == null || !result.Success)
        {
            SetText(titleText, "Battle Result");
            SetText(summaryText, result == null ? "No result payload." : result.Message);
            SetText(flowStatusText, "Battle result prototype could not build sample data.");
            return;
        }

        flowState = BattleResultPrototypeFlowState.ResultShown;
        string resultLabel = BuildResultLabel(result.ResultKind);
        SetText(titleText, "Battle Result");
        SetText(
            summaryText,
            resultLabel + " / " + result.GameMode
            + " / " + result.AuthoritySource
            + " / " + result.AsyncBattleResultId);
        SetText(noArmyLostText, BuildPreservationText(result));
        SetText(backendGapText, "Online authority/result storage: tutaj powinno byc z bazy danych");

        if (attackerArmyCard != null)
        {
            attackerArmyCard.Bind(result.AttackerArmy, "ATTACKER (YOU)", "Saved Army 3", true);
        }

        if (defenderArmyCard != null)
        {
            string defenderOwner = result.Opponent == null ? "Simulated defender" : result.Opponent.DisplayName;
            defenderArmyCard.Bind(result.DefenderArmy, "DEFENDER", defenderOwner, false);
        }

        if (rankDeltaPanel != null)
        {
            rankDeltaPanel.Bind(result);
        }

        if (xpProgressPanel != null)
        {
            xpProgressPanel.Bind(result);
        }

        if (continueCommand != null)
        {
            continueCommand.SetLabel("Continue");
            continueCommand.SetInteractable(true);
        }

        if (viewArmiesCommand != null)
        {
            viewArmiesCommand.SetLabel("View Armies");
            viewArmiesCommand.SetInteractable(true);
        }

        SetActive(rosterPreviewPanel, false);
        FocusArmy(result.AttackerArmy, "Attacker Army");
        SetText(flowStatusText, "Flow state: " + flowState + " / rendered from OfflineBattleResultAdapter -> BattleResultService -> BattleResultViewData.");
    }

    private BattleResultRecordRequest BuildSampleRequest()
    {
        BattleResultSavedArmySnapshot attacker = BuildAttackerArmy();
        BattleResultSavedArmySnapshot defender = BuildDefenderArmy();
        BattleResultOpponentMetadata opponent = new BattleResultOpponentMetadata(
            "offline-rival-jorvik",
            "Player_Jorvik",
            1518,
            defender.ArmyValue,
            true);

        return new BattleResultRecordRequest(
            sampleResultId,
            BattleResultKind.OffenceWin,
            attacker,
            defender,
            opponent,
            playerRankBefore,
            accountXpBefore);
    }

    private void EnsureAdapter()
    {
        if (adapter != null)
        {
            return;
        }

        adapter = OfflineModeDatabaseComposition.CreateBattleResultAdapter();
    }

    private BattleResultViewData FindStoredCurrentResult()
    {
        if (adapter == null || currentResult == null)
        {
            return null;
        }

        return adapter.Find(currentResult.AsyncBattleResultId);
    }

    private static BattleResultSavedArmySnapshot BuildAttackerArmy()
    {
        List<BattleResultStackSnapshot> stacks = new List<BattleResultStackSnapshot>
        {
            Stack("attacker-rusher", "Brb_Rusher", "Rusher Vanguard", 70, 4200, "Rush", "Chope"),
            Stack("attacker-thrower", "Brb_Thrower", "Axe Throwers", 60, 3600, "Double_Throw", "Axe_Rain"),
            Stack("attacker-golem", "Gol_FG", "Field Golems", 45, 3150, "Tough_Skin"),
            Stack("attacker-healer", "Liz_Healer", "Mist Healers", 30, 2100, "Defence_Ritual"),
            Stack("attacker-axeman", "Brb_Axeman", "Axemen", 25, 1450, "Fury")
        };

        return new BattleResultSavedArmySnapshot(
            "saved-army-3",
            "snapshot-battle-result-attacker",
            "Saved Army 3",
            SumPower(stacks),
            stacks);
    }

    private static BattleResultSavedArmySnapshot BuildDefenderArmy()
    {
        List<BattleResultStackSnapshot> stacks = new List<BattleResultStackSnapshot>
        {
            Stack("defender-tank", "Liz_Tank", "Swamp Shields", 65, 3900, "Guard"),
            Stack("defender-golem", "Gol_SG", "Stone Guard", 50, 3350, "Tough_Skin"),
            Stack("defender-wisp", "Gol_FE", "Fire Elementals", 40, 3050, "Burning_Aura"),
            Stack("defender-specialist", "Liz_Specialist", "Hex Callers", 30, 2500, "Nature_Grasp"),
            Stack("defender-trapper", "Liz_Trapper", "Mist Trappers", 20, 1750, "Trap")
        };

        return new BattleResultSavedArmySnapshot(
            "saved-army-defence-jorvik",
            "snapshot-battle-result-defender",
            "Jorvik Defence",
            SumPower(stacks),
            stacks);
    }

    private static BattleResultStackSnapshot Stack(string stackId, string unitId, string displayName, int amount, int combatValue, params string[] skillIds)
    {
        List<BattleResultSkillState> skills = new List<BattleResultSkillState>();
        for (int i = 0; i < skillIds.Length; i++)
        {
            skills.Add(new BattleResultSkillState(skillIds[i], i == 0));
        }

        return new BattleResultStackSnapshot(stackId, unitId, displayName, amount, combatValue, skills);
    }

    private static int SumPower(List<BattleResultStackSnapshot> stacks)
    {
        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] != null)
            {
                total += stacks[i].CombatValue;
            }
        }

        return total;
    }

    private void FocusArmy(BattleResultSavedArmySnapshot army, string header)
    {
        if (army == null)
        {
            return;
        }

        focusedArmyId = army.SavedArmyId;
        flowState = BattleResultPrototypeFlowState.ArmyFocused;
        if (attackerArmyCard != null)
        {
            attackerArmyCard.SetSelected(currentResult != null && currentResult.AttackerArmy == army);
        }

        if (defenderArmyCard != null)
        {
            defenderArmyCard.SetSelected(currentResult != null && currentResult.DefenderArmy == army);
        }

        SetText(detailHeaderText, header);
        SetText(detailBodyText, BuildArmyDetails(army));
        SetText(flowStatusText, "Flow state: " + flowState + " / focused " + army.DisplayName + " from result " + (currentResult == null ? sampleResultId : currentResult.AsyncBattleResultId) + ".");
    }

    private static string BuildArmyDetails(BattleResultSavedArmySnapshot army)
    {
        string result = army.DisplayName + "\n"
            + "Saved id: " + army.SavedArmyId + "\n"
            + "Snapshot: " + army.SnapshotId + "\n"
            + "Army power: " + army.ArmyValue.ToString("N0") + "\n\n";

        if (army.Stacks == null || army.Stacks.Count == 0)
        {
            return result + "No stack details.";
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            BattleResultStackSnapshot stack = army.Stacks[i];
            if (stack == null)
            {
                continue;
            }

            result += stack.DisplayName + " x" + stack.Amount
                + " / " + stack.CombatValue.ToString("N0")
                + " / " + BuildSkillSummary(stack) + "\n";
        }

        return result;
    }

    private static string BuildRosterPreview(BattleResultViewData result)
    {
        return "Saved Armies Roster\n"
            + BuildRosterLine("Attacker", result.AttackerArmy) + "\n"
            + BuildRosterLine("Defender", result.DefenderArmy) + "\n"
            + "Preservation: attacker and defender snapshots remain unchanged.";
    }

    private static string BuildRosterLine(string label, BattleResultSavedArmySnapshot army)
    {
        if (army == null)
        {
            return label + ": missing";
        }

        int stackCount = army.Stacks == null ? 0 : army.Stacks.Count;
        return label + ": " + army.DisplayName
            + " / " + army.SavedArmyId
            + " / " + army.ArmyValue.ToString("N0") + " power"
            + " / " + stackCount + " stacks";
    }

    private static string BuildSkillSummary(BattleResultStackSnapshot stack)
    {
        if (stack.Skills == null || stack.Skills.Count == 0)
        {
            return "No skills";
        }

        string result = string.Empty;
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            BattleResultSkillState skill = stack.Skills[i];
            if (skill == null)
            {
                continue;
            }

            if (result.Length > 0)
            {
                result += " / ";
            }

            result += skill.Unlocked ? skill.SkillId : "[" + skill.SkillId + "]";
        }

        return result;
    }

    private static string BuildPreservationText(BattleResultViewData result)
    {
        if (result.PreservationRecord == null)
        {
            return "NO ARMY LOST\nPreservation record missing.";
        }

        return "NO ARMY LOST\n"
            + result.PreservationRecord.Message + "\n"
            + "Attacker preserved: " + result.PreservationRecord.AttackerPreserved
            + " / Defender preserved: " + result.PreservationRecord.DefenderPreserved;
    }

    private static string BuildResultLabel(BattleResultKind resultKind)
    {
        switch (resultKind)
        {
            case BattleResultKind.OffenceWin:
                return "Victory";
            case BattleResultKind.OffenceLoss:
                return "Offence Loss";
            case BattleResultKind.DefenceWin:
                return "Defence Held";
            case BattleResultKind.DefenceLoss:
                return "Defence Lost";
            default:
                return "Result";
        }
    }

    private bool HasResult()
    {
        return currentResult != null && currentResult.Success;
    }

    private void WireButtonsIfNeeded()
    {
        WireButton(attackerArmyCard == null ? null : attackerArmyCard.FocusButton, OnAttackerArmyClicked);
        WireButton(defenderArmyCard == null ? null : defenderArmyCard.FocusButton, OnDefenderArmyClicked);
        WireButton(continueCommand == null ? null : continueCommand.Button, OnContinueClicked);
        WireButton(viewArmiesCommand == null ? null : viewArmiesCommand.Button, OnViewArmiesClicked);
    }

    private static void WireButton(Button button, UnityAction action)
    {
        if (button == null || action == null || button.onClick.GetPersistentEventCount() > 0)
        {
            return;
        }

        button.onClick.AddListener(action);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
