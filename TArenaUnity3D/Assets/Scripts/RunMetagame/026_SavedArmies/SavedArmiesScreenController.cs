using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SavedArmiesScreenController : MonoBehaviour
{
    [Header("Offline Roster State")]
    [SerializeField] private int unlockedSlotCount = 8;
    [SerializeField] private string selectedSlotId = "slot-01";
    [SerializeField] private string selectedArenaArmyId = "seed-army-01";

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI defenceStateText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Selected Army")]
    [SerializeField] private TextMeshProUGUI selectedArmyTitleText;
    [SerializeField] private TextMeshProUGUI selectedArmyMetaText;
    [SerializeField] private TextMeshProUGUI selectedArmyValueText;
    [SerializeField] private SavedArmiesStackRowView[] stackRows = new SavedArmiesStackRowView[0];

    [Header("Repeated Views")]
    [SerializeField] private SavedArmiesSlotView[] slotViews = new SavedArmiesSlotView[0];
    [SerializeField] private SavedArmiesArenaOptionView[] arenaOptions = new SavedArmiesArenaOptionView[0];
    [SerializeField] private SavedArmiesHistoryEntryView[] historyRows = new SavedArmiesHistoryEntryView[0];

    [Header("Commands")]
    [SerializeField] private SavedArmiesCommandButtonView importButton;
    [SerializeField] private SavedArmiesCommandButtonView setDefenceButton;
    [SerializeField] private SavedArmiesCommandButtonView backButton;

    private ISavedArmiesRosterStore rosterStore;
    private ISavedArmiesAttackHistoryStore historyStore;
    private OfflineSavedArmiesAdapter adapter;
    private SavedArmiesRosterViewData currentView;
    private bool initialized;
    private bool pendingOverwriteConfirmation;
    private string pendingOverwriteSlotId;
    private string pendingOverwriteArenaArmyId;
    private ISavedArmiesSeedSource seedSource;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        Refresh(null);
    }

    public void SelectSlot(string slotId)
    {
        InitializeIfNeeded();
        selectedSlotId = slotId;
        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;
        pendingOverwriteArenaArmyId = string.Empty;
        Refresh("Slot selected.");
    }

    public void SelectArenaArmy(string arenaArmyId)
    {
        InitializeIfNeeded();
        selectedArenaArmyId = arenaArmyId;
        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;
        pendingOverwriteArenaArmyId = string.Empty;
        Refresh("Seed army selected. Load will copy this army into the selected slot.");
    }

    public void OnImportFromArenaClicked()
    {
        // Kept for prefab/button compatibility; runtime behavior now loads seed snapshots.
        InitializeIfNeeded();
        EnsureView();

        if (currentView == null || currentView.SelectedSlot == null)
        {
            Render("Select a saved-army slot first.");
            return;
        }

        ArenaArmyOptionViewData arenaArmy = currentView.SelectedArenaArmy;
        if (arenaArmy == null)
        {
            Render("Choose a seed army to load.");
            return;
        }

        bool confirmOverwrite = pendingOverwriteConfirmation && pendingOverwriteSlotId == selectedSlotId && pendingOverwriteArenaArmyId == arenaArmy.ArenaArmyId;
        SavedArmyCommandResult result = adapter.LoadSeedArmy(new SavedArmyImportCommand(selectedSlotId, arenaArmy.ArenaArmyId, unlockedSlotCount, confirmOverwrite));

        if (!result.Success && result.Error == SavedArmiesError.MissingConfirmation)
        {
            pendingOverwriteConfirmation = true;
            pendingOverwriteSlotId = selectedSlotId;
            pendingOverwriteArenaArmyId = arenaArmy.ArenaArmyId;
            Render("Overwrite requires confirmation. Click Confirm Overwrite to replace this saved army.");
            return;
        }

        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;
        pendingOverwriteArenaArmyId = string.Empty;
        Refresh(result.Message);
    }

    public void OnSetDefenceClicked()
    {
        InitializeIfNeeded();
        EnsureView();
        if (currentView == null || currentView.SelectedArmy == null)
        {
            Render("Select a taken saved army before setting defence.");
            return;
        }

        SavedArmyCommandResult result = adapter.SetDefence(currentView.SelectedArmy.SavedArmyId);
        Refresh(result.Message);
    }

    public void OnBackClicked()
    {
        pendingOverwriteConfirmation = false;
        pendingOverwriteSlotId = string.Empty;
        pendingOverwriteArenaArmyId = string.Empty;
        Render("Back selected. Offline Saved Armies roster is ready to leave this screen.");
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        OfflineSavedArmiesDbStore dbStore = OfflineModeDatabaseComposition.CreateSavedArmiesStore();
        rosterStore = dbStore;
        historyStore = dbStore;
        seedSource = new SavedArmiesSeedSnapshotSource();
        adapter = OfflineModeDatabaseComposition.CreateSavedArmiesAdapter(rosterStore, seedSource, historyStore);
        WireNestedOwners();
    }

    private void EnsureView()
    {
        if (currentView == null)
        {
            Refresh(null);
        }
    }

    private void Refresh(string statusOverride)
    {
        currentView = adapter.BuildRoster(new SavedArmiesRosterRequest(unlockedSlotCount, selectedSlotId, selectedArenaArmyId));
        if (currentView != null && currentView.SelectedSlot != null)
        {
            selectedSlotId = currentView.SelectedSlot.SlotId;
        }

        if (currentView != null && currentView.SelectedArenaArmy != null)
        {
            selectedArenaArmyId = currentView.SelectedArenaArmy.ArenaArmyId;
        }

        Render(statusOverride);
    }

    private void Render(string statusOverride)
    {
        if (currentView == null)
        {
            return;
        }

        SetText(titleText, "Saved Armies");
        SetText(defenceStateText, string.IsNullOrEmpty(currentView.CurrentDefenceSavedArmyId) ? "Current Defence: none" : "Current Defence: " + ShortId(currentView.CurrentDefenceSavedArmyId));
        SetText(statusText, string.IsNullOrEmpty(statusOverride) ? currentView.Message : statusOverride);
        BindSlots();
        BindArenaOptions();
        BindSelectedArmy();
        BindHistory();
        BindCommands();
    }

    private void BindSlots()
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            SavedArmySlotViewData slot = currentView.Slots != null && i < currentView.Slots.Count ? currentView.Slots[i] : null;
            if (slotViews[i] != null)
            {
                slotViews[i].Bind(slot);
            }
        }
    }

    private void BindArenaOptions()
    {
        for (int i = 0; i < arenaOptions.Length; i++)
        {
            ArenaArmyOptionViewData option = currentView.ArenaArmies != null && i < currentView.ArenaArmies.Count ? currentView.ArenaArmies[i] : null;
            if (arenaOptions[i] != null)
            {
                arenaOptions[i].Bind(option);
            }
        }
    }

    private void BindSelectedArmy()
    {
        SavedArmyPreviewViewData army = currentView.SelectedArmy;
        SetText(selectedArmyTitleText, army == null ? "No Saved Army Selected" : "Saved Army " + ShortId(army.SavedArmyId));
        SetText(selectedArmyMetaText, army == null ? "Select a taken slot or load a seed army." : "Snapshot " + ShortId(army.SnapshotId) + " / valid " + army.IsValid);
        SetText(selectedArmyValueText, army == null ? "Current Value 0" : "Current Value " + army.CurrentArmyValue.ToString("N0"));

        for (int i = 0; i < stackRows.Length; i++)
        {
            SavedArmyStackViewData stack = army != null && army.Stacks != null && i < army.Stacks.Count ? army.Stacks[i] : null;
            if (stackRows[i] != null)
            {
                stackRows[i].Bind(stack);
            }
        }
    }

    private void BindHistory()
    {
        for (int i = 0; i < historyRows.Length; i++)
        {
            SavedArmyAttackHistoryEntry entry = currentView.AttackHistory != null && i < currentView.AttackHistory.Count ? currentView.AttackHistory[i] : null;
            if (historyRows[i] != null)
            {
                historyRows[i].Bind(entry);
            }
        }
    }

    private void BindCommands()
    {
        bool canImport = currentView.SelectedSlot != null && currentView.SelectedSlot.State != SavedArmySlotState.Locked && currentView.SelectedArenaArmy != null;
        bool confirmOverwrite = pendingOverwriteConfirmation && currentView.SelectedSlot != null && currentView.SelectedSlot.SlotId == pendingOverwriteSlotId;

        if (importButton != null)
        {
            importButton.Bind(confirmOverwrite ? "Confirm Overwrite" : "Load Seed Army", canImport);
        }

        if (setDefenceButton != null)
        {
            setDefenceButton.Bind("Set Defence", currentView.CanSetDefence);
        }

        if (backButton != null)
        {
            backButton.Bind("Back", true);
        }
    }

    private void WireNestedOwners()
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
            {
                slotViews[i].SetController(this);
            }
        }

        for (int i = 0; i < arenaOptions.Length; i++)
        {
            if (arenaOptions[i] != null)
            {
                arenaOptions[i].SetController(this);
            }
        }

        if (importButton != null)
        {
            importButton.SetController(this);
        }

        if (setDefenceButton != null)
        {
            setDefenceButton.SetController(this);
        }

        if (backButton != null)
        {
            backButton.SetController(this);
        }
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
