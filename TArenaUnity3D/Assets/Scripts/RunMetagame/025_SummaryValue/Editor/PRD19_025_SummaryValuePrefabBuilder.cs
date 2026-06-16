using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_025_SummaryValuePrefabBuilder
{
    private const string MainPrefabPath = "Assets/Resources/UI/PRD_19/025_SummaryValue/PRD_19_025_SummaryValue.prefab";
    private const string TemplateFolder = "Assets/Resources/UI/PRD_19/025_SummaryValue/Prefabs";
    private const string TimelineEntryPath = TemplateFolder + "/PRD19_025_TimelineEntry.prefab";
    private const string StackRowPath = TemplateFolder + "/PRD19_025_SavedArmyStackRow.prefab";
    private const string SaveSlotPath = TemplateFolder + "/PRD19_025_SaveSlot.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD19_025_CommandButton.prefab";
    private const string LegacyCardPath = TemplateFolder + "/PRD_19_025_SummaryValue_Card.prefab";
    private const string BuildSessionKey = "TArena.PRD19_025.SummaryValuePrefabBuilder.NestedPrefabsBuilt";

    static PRD19_025_SummaryValuePrefabBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 025 Summary Value Prefabs")]
    public static void RebuildFromMenu()
    {
        BuildAll();
    }

    private static void BuildOnceAfterCompile()
    {
        if (SessionState.GetBool(BuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(BuildSessionKey, true);
        EditorApplication.delayCall += BuildAll;
    }

    private static void BuildAll()
    {
        EnsureFolder("Assets/Resources/UI");
        EnsureFolder(TemplateFolder);
        DeleteIfExists(LegacyCardPath);

        SavePrefab(BuildTimelineEntryTemplate(), TimelineEntryPath);
        SavePrefab(BuildStackRowTemplate(), StackRowPath);
        SavePrefab(BuildSaveSlotTemplate(), SaveSlotPath);
        SavePrefab(BuildCommandButtonTemplate(), CommandButtonPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(TimelineEntryPath);
        AssetDatabase.ImportAsset(StackRowPath);
        AssetDatabase.ImportAsset(SaveSlotPath);
        AssetDatabase.ImportAsset(CommandButtonPath);

        SavePrefab(BuildMainPrefab(), MainPrefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD_19_025 Summary Value UI prefabs with task-specific nested prefab wiring.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreatePanel("PRD_19_025_SummaryValue", null, new Vector2(1600f, 900f), new Color(0.08f, 0.09f, 0.10f, 1f), false);

        GameObject scriptOwner = CreateRect("Script_SummaryValueScreenController", root.transform, new Vector2(100f, 100f));
        SummaryValueScreenController controller = scriptOwner.AddComponent<SummaryValueScreenController>();

        GameObject header = CreatePanel("Section_FinalVictoryHeader", root.transform, new Vector2(760f, 106f), new Color(0.32f, 0.08f, 0.05f, 0.96f), false);
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-16f, -66f));
        TextMeshProUGUI resultHeader = AddText("Text_FinalWon", header.transform, "FINAL WON", 46, TextAlignmentOptions.Midline, new Vector2(720f, 58f), new Vector2(0f, 18f), new Color(1f, 0.82f, 0.32f, 1f));
        TextMeshProUGUI resultSubheader = AddText("Text_VictorySubtitle", header.transform, "Victory - pre-final saved army candidate", 18, TextAlignmentOptions.Midline, new Vector2(700f, 28f), new Vector2(0f, -32f), new Color(0.88f, 0.78f, 0.48f, 1f));

        GameObject timelinePanel = CreatePanel("Section_RunSummaryTimeline", root.transform, new Vector2(360f, 554f), new Color(0.20f, 0.16f, 0.11f, 0.95f), false);
        SetAnchored(timelinePanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(212f, 82f));
        AddText("Header", timelinePanel.transform, "Run Summary", 26, TextAlignmentOptions.MidlineLeft, new Vector2(310f, 40f), new Vector2(0f, 238f), new Color(0.98f, 0.86f, 0.56f, 1f));
        TextMeshProUGUI startValue = AddText("Text_StartValue", timelinePanel.transform, "Start Army: 420", 15, TextAlignmentOptions.MidlineLeft, new Vector2(150f, 24f), new Vector2(-80f, 207f), new Color(0.84f, 0.78f, 0.66f, 1f));
        TextMeshProUGUI finalValue = AddText("Text_FinalValue", timelinePanel.transform, "Pre-Final Candidate: 1,245", 15, TextAlignmentOptions.MidlineRight, new Vector2(170f, 24f), new Vector2(82f, 207f), new Color(0.84f, 0.78f, 0.66f, 1f));
        GameObject timelineList = CreateVerticalList("List_TimelineEntries", timelinePanel.transform, new Vector2(322f, 438f), new Vector2(0f, -26f), 7f);

        GameObject accountPanel = CreatePanel("Section_AccountProgress", root.transform, new Vector2(360f, 214f), new Color(0.18f, 0.14f, 0.10f, 0.96f), false);
        SetAnchored(accountPanel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(212f, 132f));
        AddText("Header", accountPanel.transform, "Account Progress", 24, TextAlignmentOptions.MidlineLeft, new Vector2(310f, 34f), new Vector2(0f, 74f), new Color(0.98f, 0.86f, 0.56f, 1f));
        TextMeshProUGUI accountText = AddText("Text_AccountXp", accountPanel.transform, "+100 XP | 3,520 / 5,500", 18, TextAlignmentOptions.MidlineLeft, new Vector2(300f, 28f), new Vector2(0f, 32f), Color.white);
        Slider accountSlider = CreateSlider("Slider_AccountProgress", accountPanel.transform, new Vector2(300f, 18f), new Vector2(0f, 2f));
        TextMeshProUGUI nextUnlock = AddText("Text_NextUnlock", accountPanel.transform, "Next unlock: saved army slot progress", 14, TextAlignmentOptions.TopLeft, new Vector2(300f, 46f), new Vector2(0f, -48f), new Color(0.76f, 0.88f, 0.62f, 1f));

        GameObject saveArmyPanel = CreatePanel("Section_SaveThisArmy", root.transform, new Vector2(760f, 660f), new Color(0.72f, 0.59f, 0.38f, 0.96f), false);
        SetAnchored(saveArmyPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-16f, -10f));
        TextMeshProUGUI candidateTitle = AddText("Text_SaveThisArmyTitle", saveArmyPanel.transform, "Save This Army", 34, TextAlignmentOptions.Midline, new Vector2(700f, 52f), new Vector2(0f, 274f), new Color(0.12f, 0.07f, 0.03f, 1f));
        TextMeshProUGUI candidateMeta = AddText("Text_CandidateMeta", saveArmyPanel.transform, "From pre-final snapshot / local offline adapter", 16, TextAlignmentOptions.Midline, new Vector2(690f, 32f), new Vector2(0f, 232f), new Color(0.18f, 0.10f, 0.04f, 1f));
        GameObject stackGrid = CreateRect("List_SavedArmyStackRows", saveArmyPanel.transform, new Vector2(696f, 318f));
        SetAnchored(stackGrid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f));
        GridLayoutGroup stackLayout = stackGrid.AddComponent<GridLayoutGroup>();
        stackLayout.cellSize = new Vector2(338f, 94f);
        stackLayout.spacing = new Vector2(16f, 16f);
        stackLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        stackLayout.constraintCount = 2;
        stackLayout.childAlignment = TextAnchor.UpperCenter;

        GameObject valueBand = CreatePanel("Section_ArmyValueBand", saveArmyPanel.transform, new Vector2(690f, 66f), new Color(0.14f, 0.10f, 0.06f, 0.94f), false);
        SetAnchored(valueBand, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f));
        TextMeshProUGUI armyValue = AddText("Text_ArmyValue", valueBand.transform, "Army Value 1,245", 26, TextAlignmentOptions.MidlineLeft, new Vector2(310f, 44f), new Vector2(-168f, 0f), new Color(1f, 0.82f, 0.32f, 1f));
        AddText("Text_CandidateRule", valueBand.transform, "Saved from pre-final snapshot", 16, TextAlignmentOptions.MidlineRight, new Vector2(300f, 34f), new Vector2(176f, 0f), new Color(0.82f, 0.76f, 0.64f, 1f));
        TextMeshProUGUI status = AddText("Text_Status", saveArmyPanel.transform, "Saved army candidate is based on the pre-final snapshot.", 17, TextAlignmentOptions.Midline, new Vector2(690f, 42f), new Vector2(0f, -258f), new Color(0.17f, 0.26f, 0.10f, 1f));

        GameObject slotsPanel = CreatePanel("Section_SaveSlots", root.transform, new Vector2(380f, 650f), new Color(0.13f, 0.10f, 0.08f, 0.96f), false);
        SetAnchored(slotsPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-214f, 38f));
        AddText("Header", slotsPanel.transform, "8 Save Slots", 30, TextAlignmentOptions.Midline, new Vector2(330f, 42f), new Vector2(0f, 286f), new Color(1f, 0.82f, 0.32f, 1f));
        GameObject slotsGrid = CreateRect("List_SaveSlots", slotsPanel.transform, new Vector2(344f, 536f));
        SetAnchored(slotsGrid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f));
        GridLayoutGroup slotLayout = slotsGrid.AddComponent<GridLayoutGroup>();
        slotLayout.cellSize = new Vector2(166f, 120f);
        slotLayout.spacing = new Vector2(12f, 12f);
        slotLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        slotLayout.constraintCount = 2;
        slotLayout.childAlignment = TextAnchor.UpperCenter;

        GameObject commands = CreatePanel("Section_Commands", root.transform, new Vector2(380f, 112f), new Color(0.10f, 0.08f, 0.06f, 0.96f), false);
        SetAnchored(commands, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-214f, 68f));

        List<SummaryValueTimelineEntryView> timelineViews = new List<SummaryValueTimelineEntryView>();
        for (int i = 0; i < 6; i++)
        {
            GameObject row = InstantiateNestedPrefab(TimelineEntryPath, "TimelineEntry_" + (i + 1).ToString("00"), timelineList.transform);
            timelineViews.Add(row.GetComponent<SummaryValueTimelineEntryView>());
        }

        List<SummaryValueStackRowView> stackViews = new List<SummaryValueStackRowView>();
        for (int i = 0; i < 6; i++)
        {
            GameObject row = InstantiateNestedPrefab(StackRowPath, "SavedArmy_StackRow_" + (i + 1).ToString("00"), stackGrid.transform);
            stackViews.Add(row.GetComponent<SummaryValueStackRowView>());
        }

        List<SummaryValueSaveSlotView> slotViews = new List<SummaryValueSaveSlotView>();
        for (int i = 0; i < 8; i++)
        {
            GameObject slot = InstantiateNestedPrefab(SaveSlotPath, "SaveSlot_" + (i + 1).ToString("00"), slotsGrid.transform);
            SummaryValueSaveSlotView slotView = slot.GetComponent<SummaryValueSaveSlotView>();
            SerializedObject slotSerialized = new SerializedObject(slotView);
            SetObject(slotSerialized, "screenController", controller);
            slotSerialized.ApplyModifiedPropertiesWithoutUndo();
            slotViews.Add(slotView);
        }

        GameObject primaryGo = InstantiateNestedPrefab(CommandButtonPath, "Button_SaveOrOverwrite", commands.transform);
        SetAnchored(primaryGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f));
        SummaryValueCommandButtonView primaryCommand = primaryGo.GetComponent<SummaryValueCommandButtonView>();
        SerializedObject primarySerialized = new SerializedObject(primaryCommand);
        SetObject(primarySerialized, "screenController", controller);
        SetEnum(primarySerialized, "commandKind", SummaryValueCommandButtonKind.PrimaryAction);
        primarySerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject returnGo = InstantiateNestedPrefab(CommandButtonPath, "Button_Return", commands.transform);
        SetAnchored(returnGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f));
        SummaryValueCommandButtonView returnCommand = returnGo.GetComponent<SummaryValueCommandButtonView>();
        SerializedObject returnSerialized = new SerializedObject(returnCommand);
        SetObject(returnSerialized, "screenController", controller);
        SetEnum(returnSerialized, "commandKind", SummaryValueCommandButtonKind.Return);
        returnSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serialized = new SerializedObject(controller);
        SetString(serialized, "runId", "offline-run-final-win");
        SetInt(serialized, "unlockedSlotCount", 2);
        SetString(serialized, "selectedSlotId", "slot-02");
        SetObject(serialized, "resultHeaderText", resultHeader);
        SetObject(serialized, "resultSubheaderText", resultSubheader);
        SetObject(serialized, "startValueText", startValue);
        SetObject(serialized, "finalValueText", finalValue);
        SetObject(serialized, "accountProgressText", accountText);
        SetObject(serialized, "nextUnlockText", nextUnlock);
        SetObject(serialized, "accountProgressSlider", accountSlider);
        SetObject(serialized, "candidateTitleText", candidateTitle);
        SetObject(serialized, "candidateMetaText", candidateMeta);
        SetObject(serialized, "armyValueText", armyValue);
        SetObject(serialized, "statusText", status);
        SetObjectArray(serialized, "timelineRows", timelineViews.ToArray());
        SetObjectArray(serialized, "savedArmyRows", stackViews.ToArray());
        SetObjectArray(serialized, "saveSlotViews", slotViews.ToArray());
        SetObject(serialized, "primaryCommandButton", primaryCommand);
        SetObject(serialized, "returnCommandButton", returnCommand);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildTimelineEntryTemplate()
    {
        GameObject root = CreatePanel("PRD19_025_TimelineEntry", null, new Vector2(322f, 66f), new Color(0.11f, 0.09f, 0.07f, 0.90f), false);
        SummaryValueTimelineEntryView view = root.AddComponent<SummaryValueTimelineEntryView>();
        TextMeshProUGUI stage = AddText("Text_StageIndex", root.transform, "01", 16, TextAlignmentOptions.Midline, new Vector2(38f, 46f), new Vector2(-136f, 0f), new Color(1f, 0.82f, 0.32f, 1f));
        TextMeshProUGUI label = AddText("Text_Label", root.transform, "Battle 1", 16, TextAlignmentOptions.MidlineLeft, new Vector2(108f, 24f), new Vector2(-62f, 14f), Color.white);
        TextMeshProUGUI received = AddText("Text_Received", root.transform, "Victory reward: Mass", 12, TextAlignmentOptions.MidlineLeft, new Vector2(154f, 22f), new Vector2(-39f, -14f), new Color(0.84f, 0.78f, 0.66f, 1f));
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "Value 780", 12, TextAlignmentOptions.MidlineRight, new Vector2(76f, 22f), new Vector2(102f, 13f), new Color(0.98f, 0.74f, 0.28f, 1f));
        TextMeshProUGUI gold = AddText("Text_Gold", root.transform, "75 gold", 11, TextAlignmentOptions.MidlineRight, new Vector2(76f, 20f), new Vector2(102f, -14f), new Color(0.70f, 0.86f, 0.68f, 1f));

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "stageText", stage);
        SetObject(serialized, "labelText", label);
        SetObject(serialized, "receivedText", received);
        SetObject(serialized, "valueText", value);
        SetObject(serialized, "goldText", gold);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject BuildStackRowTemplate()
    {
        GameObject root = CreatePanel("PRD19_025_SavedArmyStackRow", null, new Vector2(338f, 94f), new Color(0.17f, 0.12f, 0.08f, 0.94f), false);
        SummaryValueStackRowView view = root.AddComponent<SummaryValueStackRowView>();
        Image icon = CreateImage("Icon_UnitSwatch", root.transform, new Vector2(58f, 58f), new Vector2(-128f, 4f), new Color(0.44f, 0.28f, 0.18f, 1f), false);
        TextMeshProUGUI name = AddText("Text_Name", root.transform, "Rusher", 18, TextAlignmentOptions.MidlineLeft, new Vector2(150f, 24f), new Vector2(-24f, 27f), Color.white);
        TextMeshProUGUI tier = AddText("Text_Tier", root.transform, "Tier I / Level 1", 12, TextAlignmentOptions.MidlineLeft, new Vector2(150f, 20f), new Vector2(-24f, 7f), new Color(0.80f, 0.74f, 0.62f, 1f));
        TextMeshProUGUI amount = AddText("Text_Amount", root.transform, "x70", 13, TextAlignmentOptions.MidlineLeft, new Vector2(82f, 20f), new Vector2(-58f, -16f), new Color(0.95f, 0.80f, 0.42f, 1f));
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "420 value", 13, TextAlignmentOptions.MidlineRight, new Vector2(94f, 22f), new Vector2(108f, 25f), new Color(1f, 0.82f, 0.32f, 1f));
        TextMeshProUGUI skills = AddText("Text_Skills", root.transform, "Chope / [Rush]", 11, TextAlignmentOptions.MidlineRight, new Vector2(120f, 30f), new Vector2(88f, -18f), new Color(0.58f, 0.82f, 1f, 1f));

        UnitRepresentation unitRepresentation = root.AddComponent<UnitRepresentation>();
        SerializedObject unitSerialized = new SerializedObject(unitRepresentation);
        SetObject(unitSerialized, "Icon", icon);
        SetObject(unitSerialized, "Name", name);
        SetObject(unitSerialized, "Tier", tier);
        SetObject(unitSerialized, "SkillsText", skills);
        unitSerialized.ApplyModifiedPropertiesWithoutUndo();

        StackRepresentation stackRepresentation = root.AddComponent<StackRepresentation>();
        SerializedObject stackSerialized = new SerializedObject(stackRepresentation);
        SetObject(stackSerialized, "Count", amount);
        SetObject(stackSerialized, "StackValue", value);
        stackSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "unitIcon", icon);
        SetObject(serialized, "nameText", name);
        SetObject(serialized, "tierText", tier);
        SetObject(serialized, "amountText", amount);
        SetObject(serialized, "valueText", value);
        SetObject(serialized, "skillsText", skills);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject BuildSaveSlotTemplate()
    {
        GameObject root = CreatePanel("PRD19_025_SaveSlot", null, new Vector2(166f, 120f), new Color(0.16f, 0.12f, 0.08f, 0.96f), true);
        Button button = root.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.55f, 1f);
        colors.pressedColor = new Color(0.82f, 0.58f, 0.24f, 1f);
        colors.disabledColor = new Color(0.34f, 0.30f, 0.26f, 1f);
        button.colors = colors;

        SummaryValueSaveSlotView view = root.AddComponent<SummaryValueSaveSlotView>();
        TextMeshProUGUI slotNumber = AddText("Text_SlotNumber", root.transform, "1", 28, TextAlignmentOptions.Midline, new Vector2(42f, 34f), new Vector2(-52f, 32f), new Color(1f, 0.82f, 0.32f, 1f));
        TextMeshProUGUI state = AddText("Text_State", root.transform, "Taken", 16, TextAlignmentOptions.MidlineLeft, new Vector2(82f, 24f), new Vector2(34f, 34f), Color.white);
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "Value 1,180", 13, TextAlignmentOptions.Midline, new Vector2(140f, 22f), new Vector2(0f, -4f), new Color(0.96f, 0.76f, 0.34f, 1f));
        TextMeshProUGUI armyId = AddText("Text_ArmyId", root.transform, "saved-army-slot-01", 9, TextAlignmentOptions.Midline, new Vector2(140f, 24f), new Vector2(0f, -34f), new Color(0.72f, 0.68f, 0.56f, 1f));
        GameObject selected = CreateOverlay("State_Selected", root.transform, new Color(0.95f, 0.82f, 0.20f, 0.18f));
        GameObject locked = CreateOverlay("State_LockedOverlay", root.transform, new Color(0f, 0f, 0f, 0.48f));
        GameObject taken = CreateImage("State_TakenMarker", root.transform, new Vector2(14f, 14f), new Vector2(62f, 44f), new Color(0.36f, 0.76f, 0.34f, 1f), false).gameObject;
        GameObject empty = CreateImage("State_EmptyMarker", root.transform, new Vector2(14f, 14f), new Vector2(62f, 44f), new Color(0.65f, 0.88f, 0.22f, 1f), false).gameObject;
        selected.SetActive(false);
        locked.SetActive(false);
        empty.SetActive(false);

        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "slotNumberText", slotNumber);
        SetObject(serialized, "stateText", state);
        SetObject(serialized, "valueText", value);
        SetObject(serialized, "armyIdText", armyId);
        SetObject(serialized, "selectedState", selected);
        SetObject(serialized, "lockedOverlay", locked);
        SetObject(serialized, "takenState", taken);
        SetObject(serialized, "emptyState", empty);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreatePanel("PRD19_025_CommandButton", null, new Vector2(326f, 48f), new Color(0.54f, 0.28f, 0.10f, 1f), true);
        Button button = root.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.52f, 1f);
        colors.pressedColor = new Color(0.78f, 0.48f, 0.18f, 1f);
        colors.disabledColor = new Color(0.26f, 0.22f, 0.18f, 1f);
        button.colors = colors;

        SummaryValueCommandButtonView view = root.AddComponent<SummaryValueCommandButtonView>();
        TextMeshProUGUI label = AddText("Text_Label", root.transform, "Save Army", 21, TextAlignmentOptions.Midline, new Vector2(300f, 40f), Vector2.zero, Color.white);
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "labelText", label);
        SetEnum(serialized, "commandKind", SummaryValueCommandButtonKind.PrimaryAction);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject InstantiateNestedPrefab(string assetPath, string instanceName, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError("Missing nested prefab asset: " + assetPath);
            return CreateRect(instanceName, parent, new Vector2(100f, 100f));
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
        }

        instance.name = instanceName;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.localPosition = Vector3.zero;
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color, bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent, size);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return go;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color, bool raycastTarget)
    {
        GameObject go = CreatePanel(name, parent, size, color, raycastTarget);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        return go.GetComponent<Image>();
    }

    private static TextMeshProUGUI AddText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreateRect(name, parent, size);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject go = CreatePanel(name, parent, size, new Color(0.08f, 0.07f, 0.05f, 1f), false);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.64f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        GameObject fillArea = CreateRect("Fill Area", go.transform, Vector2.zero);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fill = CreatePanel("Fill", fillArea.transform, Vector2.zero, new Color(0.42f, 0.78f, 0.20f, 1f), false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;
        slider.targetGraphic = go.GetComponent<Image>();
        return slider;
    }

    private static GameObject CreateVerticalList(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, float spacing)
    {
        GameObject go = CreateRect(name, parent, size);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return go;
    }

    private static GameObject CreateOverlay(string name, Transform parent, Color color)
    {
        GameObject go = CreatePanel(name, parent, Vector2.zero, color, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    private static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetString(SerializedObject serialized, string propertyName, string value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetEnum(SerializedObject serialized, string propertyName, SummaryValueCommandButtonKind value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = (int)value;
        }
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
