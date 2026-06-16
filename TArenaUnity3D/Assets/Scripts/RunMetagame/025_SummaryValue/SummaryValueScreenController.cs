using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SummaryValueScreenController : MonoBehaviour
{
    [Header("Sample Summary")]
    [SerializeField] private string runId = "offline-run-final-win";
    [SerializeField] private int unlockedSlotCount = 2;
    [SerializeField] private string selectedSlotId = "slot-02";

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI resultHeaderText;
    [SerializeField] private TextMeshProUGUI resultSubheaderText;

    [Header("Run Summary")]
    [SerializeField] private TextMeshProUGUI startValueText;
    [SerializeField] private TextMeshProUGUI finalValueText;
    [SerializeField] private TextMeshProUGUI accountProgressText;
    [SerializeField] private TextMeshProUGUI nextUnlockText;
    [SerializeField] private Slider accountProgressSlider;

    [Header("Saved Army Candidate")]
    [SerializeField] private TextMeshProUGUI candidateTitleText;
    [SerializeField] private TextMeshProUGUI candidateMetaText;
    [SerializeField] private TextMeshProUGUI armyValueText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Repeated Views")]
    [SerializeField] private SummaryValueTimelineEntryView[] timelineRows = new SummaryValueTimelineEntryView[0];
    [SerializeField] private SummaryValueStackRowView[] savedArmyRows = new SummaryValueStackRowView[0];
    [SerializeField] private SummaryValueSaveSlotView[] saveSlotViews = new SummaryValueSaveSlotView[0];
    [SerializeField] private SummaryValueCommandButtonView primaryCommandButton;
    [SerializeField] private SummaryValueCommandButtonView returnCommandButton;

    private ISummaryValueRosterStore rosterStore;
    private OfflineSummaryValueAdapter adapter;
    private SummaryValueScreenViewData currentScreen;
    private SummaryValueArmySnapshot startArmy;
    private SummaryValueArmySnapshot preFinalArmy;
    private SummaryValueArmySnapshot postFinalArmy;
    private List<SummaryValueTimelineEntry> timelineEntries;
    private bool initialized;
    private bool pendingOverwriteConfirmation;
    private string pendingOverwriteSlotId;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        RefreshSummary(null);
    }

    public void SelectSlot(string slotId)
    {
        InitializeIfNeeded();
        EnsureScreen();

        SummaryValueSaveSlotViewData slot = FindSlot(slotId);
        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;

        if (slot == null)
        {
            Render("Save slot was not found.");
            return;
        }

        if (!slot.Selectable || slot.State == SummaryValueSlotState.Locked)
        {
            Render("Slot " + slot.PhysicalIndex + " is locked. Unlock more saved-army slots through account progress.");
            return;
        }

        selectedSlotId = slot.SlotId;
        RefreshSummary(slot.State == SummaryValueSlotState.Taken
            ? "Slot " + slot.PhysicalIndex + " is taken. Primary action is Overwrite."
            : "Slot " + slot.PhysicalIndex + " is empty. Primary action is Save.");
    }

    public void OnPrimaryActionClicked()
    {
        InitializeIfNeeded();
        EnsureScreen();

        if (currentScreen == null || currentScreen.SelectedSlot == null)
        {
            Render("Select an unlocked save slot first.");
            return;
        }

        if (currentScreen.SavedArmyCandidate == null)
        {
            Render("Final victory is required before saving an army.");
            return;
        }

        bool confirmOverwrite = pendingOverwriteConfirmation
            && pendingOverwriteSlotId == currentScreen.SelectedSlot.SlotId;

        SummaryValueSaveResult result = adapter.Save(new SummaryValueSaveCommand(
            currentScreen.RunId,
            currentScreen.SavedArmyCandidate.CandidateId,
            currentScreen.SelectedSlot.SlotId,
            confirmOverwrite));

        if (!result.Success && result.Error == SummaryValueError.MissingConfirmation)
        {
            pendingOverwriteConfirmation = true;
            pendingOverwriteSlotId = currentScreen.SelectedSlot.SlotId;
            Render("Overwrite requires confirmation. Click Confirm Overwrite to replace slot " + currentScreen.SelectedSlot.PhysicalIndex + ".");
            return;
        }

        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;

        if (!result.Success)
        {
            Render(result.Message);
            return;
        }

        RefreshSummary(result.Message + " Slot " + currentScreen.SelectedSlot.PhysicalIndex + " now shows Taken.");
    }

    public void OnReturnClicked()
    {
        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;
        Render("Return selected. Summary flow status is ready to leave this screen.");
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        rosterStore = OfflineModeDatabaseComposition.CreateSummaryValueStore();
        adapter = OfflineModeDatabaseComposition.CreateSummaryValueAdapter(rosterStore);
        startArmy = BuildStartArmy();
        preFinalArmy = BuildPreFinalArmy();
        postFinalArmy = BuildPostFinalArmy();
        timelineEntries = BuildTimelineEntries();
        SeedExistingSaveSlots();
        WireNestedOwners();
    }

    private void EnsureScreen()
    {
        if (currentScreen == null)
        {
            RefreshSummary(null);
        }
    }

    private void RefreshSummary(string statusOverride)
    {
        currentScreen = adapter.BuildSummary(new SummaryValueBuildRequest(
            runId,
            SummaryValueFinalResult.Won,
            startArmy,
            preFinalArmy,
            postFinalArmy,
            timelineEntries,
            unlockedSlotCount,
            selectedSlotId));

        Render(statusOverride);
    }

    private void Render(string statusOverride)
    {
        if (currentScreen == null)
        {
            return;
        }

        SetText(resultHeaderText, currentScreen.FinalResult == SummaryValueFinalResult.Won ? "FINAL WON" : "RUN ENDED");
        SetText(resultSubheaderText, "Victory - pre-final saved army candidate");
        SetText(startValueText, "Start Army: " + SafeArmyValue(currentScreen.StartArmySnapshot));
        SetText(finalValueText, "Pre-Final Candidate: " + SafeArmyValue(currentScreen.PreFinalArmySnapshot));

        int accountXp = currentScreen.AccountProgressReward == null ? 0 : currentScreen.AccountProgressReward.AccountXp;
        int totalXp = 3420 + accountXp;
        SetText(accountProgressText, "+" + accountXp.ToString("N0") + " XP  |  " + totalXp.ToString("N0") + " / 5,500");
        SetText(nextUnlockText, currentScreen.AccountProgressReward == null ? string.Empty : currentScreen.AccountProgressReward.NextUnlockPreview);
        if (accountProgressSlider != null)
        {
            accountProgressSlider.value = Mathf.Clamp01(totalXp / 5500f);
        }

        SummaryValueSavedArmyCandidate candidate = currentScreen.SavedArmyCandidate;
        SetText(candidateTitleText, "Save This Army");
        SetText(candidateMetaText, candidate == null ? "No candidate" : "Candidate " + ShortId(candidate.CandidateId) + " / From " + candidate.PreFinalSnapshotId + " / local offline adapter");
        SetText(armyValueText, "Army Value " + (candidate == null ? "0" : candidate.ArmyValue.ToString("N0")));
        SetText(statusText, string.IsNullOrEmpty(statusOverride) ? currentScreen.Message : statusOverride);

        BindTimeline();
        BindSavedArmyRows(candidate == null ? null : candidate.ImmutableArmySnapshot);
        BindSaveSlots();
        BindCommands();
    }

    private void BindTimeline()
    {
        for (int i = 0; i < timelineRows.Length; i++)
        {
            SummaryValueTimelineEntry entry = currentScreen.TimelineEntries != null && i < currentScreen.TimelineEntries.Count
                ? currentScreen.TimelineEntries[i]
                : null;

            if (timelineRows[i] != null)
            {
                timelineRows[i].Bind(entry);
            }
        }
    }

    private void BindSavedArmyRows(SummaryValueArmySnapshot army)
    {
        for (int i = 0; i < savedArmyRows.Length; i++)
        {
            SummaryValueStackSnapshot stack = army != null && army.Stacks != null && i < army.Stacks.Count
                ? army.Stacks[i]
                : null;

            if (savedArmyRows[i] != null)
            {
                savedArmyRows[i].Bind(stack);
            }
        }
    }

    private void BindSaveSlots()
    {
        for (int i = 0; i < saveSlotViews.Length; i++)
        {
            SummaryValueSaveSlotViewData slot = currentScreen.SaveSlots != null && i < currentScreen.SaveSlots.Count
                ? currentScreen.SaveSlots[i]
                : null;

            if (saveSlotViews[i] != null)
            {
                saveSlotViews[i].Bind(slot);
            }
        }
    }

    private void BindCommands()
    {
        bool canUsePrimary = currentScreen.CanSave && currentScreen.SelectedSlot != null;
        string primaryLabel = BuildPrimaryLabel();

        if (primaryCommandButton != null)
        {
            primaryCommandButton.Bind(primaryLabel, canUsePrimary);
        }

        if (returnCommandButton != null)
        {
            returnCommandButton.Bind("Return", true);
        }
    }

    private string BuildPrimaryLabel()
    {
        if (currentScreen.SelectedSlot != null
            && pendingOverwriteConfirmation
            && pendingOverwriteSlotId == currentScreen.SelectedSlot.SlotId)
        {
            return "Confirm Overwrite";
        }

        if (currentScreen.ActionMode == SummaryValueSaveActionMode.Overwrite)
        {
            return "Overwrite";
        }

        if (currentScreen.ActionMode == SummaryValueSaveActionMode.Save)
        {
            return "Save Army";
        }

        return "Unavailable";
    }

    private SummaryValueSaveSlotViewData FindSlot(string slotId)
    {
        if (currentScreen == null || currentScreen.SaveSlots == null || string.IsNullOrEmpty(slotId))
        {
            return null;
        }

        for (int i = 0; i < currentScreen.SaveSlots.Count; i++)
        {
            SummaryValueSaveSlotViewData slot = currentScreen.SaveSlots[i];
            if (slot != null && slot.SlotId == slotId)
            {
                return slot;
            }
        }

        return null;
    }

    private void WireNestedOwners()
    {
        for (int i = 0; i < saveSlotViews.Length; i++)
        {
            if (saveSlotViews[i] != null)
            {
                saveSlotViews[i].SetController(this);
            }
        }

        if (primaryCommandButton != null)
        {
            primaryCommandButton.SetController(this);
        }

        if (returnCommandButton != null)
        {
            returnCommandButton.SetController(this);
        }
    }

    private void SeedExistingSaveSlots()
    {
        List<SummaryValueSaveSlotViewData> slots = rosterStore.ListSlots(unlockedSlotCount, "slot-01");
        SummaryValueSaveSlotViewData slot = slots != null && slots.Count > 0 ? slots[0] : null;
        if (slot != null && slot.State == SummaryValueSlotState.Taken)
        {
            return;
        }

        rosterStore.SaveCandidate(
            "slot-01",
            new SummaryValueSavedArmyCandidate(
                "saved-army-slot-01",
                "past-run-17",
                "past-pre-final-17",
                BuildExistingSavedArmy(),
                1180));
    }

    private static SummaryValueArmySnapshot BuildStartArmy()
    {
        return new SummaryValueArmySnapshot(
            "start-army",
            420,
            new List<SummaryValueStackSnapshot>
            {
                Stack("start-rusher", "Rusher", "Rusher", "I", 1, 38, 260, "Chope"),
                Stack("start-thrower", "Thrower", "Thrower", "I", 1, 16, 160, "Axe_Throw")
            });
    }

    private static SummaryValueArmySnapshot BuildPreFinalArmy()
    {
        return new SummaryValueArmySnapshot(
            "pre-final-army",
            1245,
            new List<SummaryValueStackSnapshot>
            {
                Stack("stack-rusher", "Rusher", "Rusher", "I", 1, 70, 420, "Chope", "Rush"),
                Stack("stack-thrower", "Thrower", "Thrower", "II", 2, 45, 270, "Axe_Throw", "Double_Throw"),
                Stack("stack-axeman", "Axeman", "Axeman", "II", 2, 35, 210, "Cleave", "Guard_Break"),
                Stack("stack-tank", "Tank", "Tank", "III", 3, 22, 132, "Defence_Ritual", "Stone_Wall"),
                Stack("stack-wisp", "Wisp", "Wisp", "III", 3, 28, 168, "Blind_by_light", "Unstoppable_Light"),
                Stack("stack-fire", "Fire_Elemental", "Fire Elemental", "IV", 4, 12, 45, "Fireball", "Burning_Ground")
            });
    }

    private static SummaryValueArmySnapshot BuildPostFinalArmy()
    {
        return new SummaryValueArmySnapshot(
            "post-final-battle-result",
            1088,
            new List<SummaryValueStackSnapshot>
            {
                Stack("post-rusher", "Rusher", "Rusher", "I", 1, 61, 366, "Chope", "Rush"),
                Stack("post-thrower", "Thrower", "Thrower", "II", 2, 42, 252, "Axe_Throw", "Double_Throw"),
                Stack("post-axeman", "Axeman", "Axeman", "II", 2, 31, 186, "Cleave", "Guard_Break"),
                Stack("post-tank", "Tank", "Tank", "III", 3, 20, 120, "Defence_Ritual", "Stone_Wall"),
                Stack("post-wisp", "Wisp", "Wisp", "III", 3, 24, 144, "Blind_by_light", "Unstoppable_Light"),
                Stack("post-fire", "Fire_Elemental", "Fire Elemental", "IV", 4, 5, 20, "Fireball", "Burning_Ground")
            });
    }

    private static SummaryValueArmySnapshot BuildExistingSavedArmy()
    {
        return new SummaryValueArmySnapshot(
            "existing-slot-01-army",
            1180,
            new List<SummaryValueStackSnapshot>
            {
                Stack("existing-rusher", "Rusher", "Rusher", "I", 1, 64, 384, "Chope", "Rush"),
                Stack("existing-thrower", "Thrower", "Thrower", "II", 2, 42, 252, "Axe_Throw", "Double_Throw"),
                Stack("existing-tank", "Tank", "Tank", "III", 3, 26, 156, "Defence_Ritual", "Stone_Wall"),
                Stack("existing-wisp", "Wisp", "Wisp", "III", 3, 31, 186, "Blind_by_light", "Unstoppable_Light"),
                Stack("existing-fire", "Fire_Elemental", "Fire Elemental", "IV", 4, 18, 202, "Fireball", "Burning_Ground")
            });
    }

    private static List<SummaryValueTimelineEntry> BuildTimelineEntries()
    {
        return new List<SummaryValueTimelineEntry>
        {
            new SummaryValueTimelineEntry("stage-start", 0, "Start Army", "Initial saved route army", 420, 0),
            new SummaryValueTimelineEntry("stage-battle-1", 1, "Battle 1", "Victory reward: Mass", 780, 75),
            new SummaryValueTimelineEntry("stage-shop", 2, "Shop", "Healed 120 / spent 150", 850, 0),
            new SummaryValueTimelineEntry("stage-battle-2", 3, "Battle 2", "Victory reward: Skill", 1010, 35),
            new SummaryValueTimelineEntry("stage-battle-3", 4, "Battle 3", "Victory reward: Quality", 1130, 55),
            new SummaryValueTimelineEntry("stage-final", 5, "Final Encounter", "Victory proof", 1245, 0)
        };
    }

    private static SummaryValueStackSnapshot Stack(string stackId, string unitId, string displayName, string tier, int level, int amount, int combatValue, params string[] skillIds)
    {
        List<SummaryValueSkillState> skills = new List<SummaryValueSkillState>();
        for (int i = 0; i < skillIds.Length; i++)
        {
            skills.Add(new SummaryValueSkillState(skillIds[i], i == 0));
        }

        return new SummaryValueStackSnapshot(stackId, unitId, displayName, tier, level, amount, combatValue, skills);
    }

    private static string SafeArmyValue(SummaryValueArmySnapshot army)
    {
        return army == null ? "0" : army.TotalArmyValue.ToString("N0");
    }

    private static string ShortId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 18)
        {
            return value;
        }

        return value.Substring(0, 18);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
