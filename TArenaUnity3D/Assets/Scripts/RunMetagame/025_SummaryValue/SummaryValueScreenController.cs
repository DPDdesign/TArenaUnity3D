using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SummaryValueScreenController : MonoBehaviour
{
    [Header("Selection")]
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
    private OfflineRunContextDbReader runContextReader;
    private OfflineRunContext runContext;
    private SummaryValueScreenViewData currentScreen;
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
        runContextReader = OfflineModeDatabaseComposition.CreateRunContextReader();
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
        runContext = runContextReader == null ? null : runContextReader.LoadLatestRunWithSummary();
        if (runContext == null)
        {
            currentScreen = null;
            RenderUnavailable("Summary Value requires a persisted run summary.");
            return;
        }

        currentScreen = adapter.BuildSummary(new SummaryValueBuildRequest(
            runContext.RunIdText,
            SummaryValueFinalResult.Pending,
            null,
            null,
            null,
            new List<SummaryValueTimelineEntry>(),
            runContext.UnlockedSavedArmySlots,
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
        int totalXp = (runContext == null ? 0 : runContext.AccountXp) + accountXp;
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

    private void RenderUnavailable(string message)
    {
        SetText(resultHeaderText, "RUN SUMMARY");
        SetText(resultSubheaderText, "No persisted run summary");
        SetText(startValueText, "Start Army: 0");
        SetText(finalValueText, "Pre-Final Candidate: 0");
        SetText(accountProgressText, "+0 XP  |  0 / 5,500");
        SetText(nextUnlockText, string.Empty);
        SetText(candidateTitleText, "Save This Army");
        SetText(candidateMetaText, "No candidate");
        SetText(armyValueText, "Army Value 0");
        SetText(statusText, message);
        if (primaryCommandButton != null)
        {
            primaryCommandButton.Bind("Unavailable", false);
        }

        if (returnCommandButton != null)
        {
            returnCommandButton.Bind("Return", true);
        }
    }
}
