using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_026_SavedArmiesPolishedPrefabBuilder
{
    private const string PolishedFolder = "Assets/Resources/UI/PRD_19/026_SavedArmies/Polished_1";
    private const string TemplateFolder = PolishedFolder + "/Prefabs";
    private const string MainPrefabPath = PolishedFolder + "/PRD_19_026_SavedArmies_Polished.prefab";
    private const string SlotPath = TemplateFolder + "/PRD19_026_SavedArmySlot_Polished.prefab";
    private const string StackRowPath = TemplateFolder + "/PRD19_026_StackRow_Polished.prefab";
    private const string ArenaOptionPath = TemplateFolder + "/PRD19_026_ArenaOption_Polished.prefab";
    private const string HistoryRowPath = TemplateFolder + "/PRD19_026_HistoryRow_Polished.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD19_026_CommandButton_Polished.prefab";
    private const string BuildSessionKey = "TArena.PRD19_026.SavedArmiesPolishedPrefabBuilder.BuiltThisSession";

    private const string BigFramePath = "Assets/GUI_Parts/Gui_parts/Frame_big.png";
    private const string MidFramePath = "Assets/GUI_Parts/Gui_parts/Frame_mid.png";
    private const string ButtonFramePath = "Assets/GUI_Parts/Gui_parts/button_frame.png";
    private const string ScreenBackgroundPath = "Assets/Old_Paper_Gui/background/big/big_dark_background0.png";
    private const string PanelBackgroundPath = "Assets/Old_Paper_Gui/background/mid/middle_dark_background0.png";
    private const string ButtonPrimaryPath = "Assets/GUI_Parts/Gui_parts/button_ready_on.png";
    private const string ButtonSecondaryPath = "Assets/GUI_Parts/Gui_parts/button_ready_off.png";
    private const string IconFramePath = "Assets/Classic_RPG_GUI/frame_backgrounds/skill_background.png";
    private const string PortraitFramePath = "Assets/Classic_RPG_GUI/Parts/Hero_icon_frame_bg.png";
    private const string LongRowFramePath = "Assets/Classic_RPG_GUI/Parts/inventory_frame_little_long.png";
    private const string BodyFontPath = "Assets/Fonts/Noto_Sans/static/NotoSans_SemiCondensed-SemiBold SDF.asset";
    private const string NumberFontPath = "Assets/Fonts/Noto_Sans/static/NotoSans-Bold SDF.asset";

    static PRD19_026_SavedArmiesPolishedPrefabBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 026 Saved Armies Polished Prefabs")]
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
        BuildAll();
    }

    private static void BuildAll()
    {
        EnsureFolder("Assets/Resources/UI");
        EnsureFolder(PolishedFolder);
        EnsureFolder(TemplateFolder);

        SavePrefab(BuildSlotTemplate(), SlotPath);
        SavePrefab(BuildStackRowTemplate(), StackRowPath);
        SavePrefab(BuildArenaOptionTemplate(), ArenaOptionPath);
        SavePrefab(BuildHistoryRowTemplate(), HistoryRowPath);
        SavePrefab(BuildCommandButtonTemplate(), CommandButtonPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(SlotPath);
        AssetDatabase.ImportAsset(StackRowPath);
        AssetDatabase.ImportAsset(ArenaOptionPath);
        AssetDatabase.ImportAsset(HistoryRowPath);
        AssetDatabase.ImportAsset(CommandButtonPath);

        SavePrefab(BuildMainPrefab(), MainPrefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD 19 026 Saved Armies polished UI prefabs in Polished_1.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_026_SavedArmies_Polished",
            null,
            new Vector2(1600f, 900f),
            ScreenBackgroundPath,
            ScreenColor(),
            BigFramePath,
            FrameColor(),
            false);
        CreateOverlay("Decor_Vignette", root.transform, new Color(0.01f, 0.01f, 0.01f, 0.34f));

        GameObject scriptOwner = CreateRect("Script_SavedArmiesScreenController", root.transform, new Vector2(32f, 32f));
        SavedArmiesScreenController controller = scriptOwner.AddComponent<SavedArmiesScreenController>();

        GameObject header = CreateDecoratedPanel(
            "Section_Header",
            root.transform,
            new Vector2(1528f, 98f),
            PanelBackgroundPath,
            PanelColor(),
            BigFramePath,
            FrameColor(),
            false);
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f));
        TextMeshProUGUI title = CreateFramedText(
            header.transform,
            "Text_Title",
            "SAVED ARMIES",
            40,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(446f, 50f),
            new Vector2(-454f, 10f),
            FrameInsetColor(),
            TitleColor(),
            false);
        CreateFramedText(
            header.transform,
            "Text_HeaderHint",
            "Offline roster, seed imports, and defence assignment.",
            15,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(612f, 28f),
            new Vector2(-278f, -24f),
            FrameHintColor(),
            SecondaryTextColor(),
            false);
        TextMeshProUGUI defenceState = CreateFramedText(
            header.transform,
            "Text_DefenceState",
            "Current Defence: none",
            18,
            TextAlignmentOptions.Midline,
            new Vector2(330f, 42f),
            new Vector2(520f, 0f),
            new Color(0.08f, 0.11f, 0.07f, 0.86f),
            SuccessTextColor(),
            true);

        GameObject roster = CreateDecoratedPanel(
            "Section_RosterSlots",
            root.transform,
            new Vector2(748f, 692f),
            PanelBackgroundPath,
            PanelColor(),
            BigFramePath,
            FrameColor(),
            false);
        SetAnchored(roster, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(404f, -6f));
        CreateFramedText(
            roster.transform,
            "Text_RosterHeader",
            "SLOT ROSTER",
            24,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(250f, 34f),
            new Vector2(-216f, 294f),
            FrameInsetColor(),
            TitleColor(),
            false);
        CreateFramedText(
            roster.transform,
            "Text_RosterHint",
            "Eight physical slots. Locked, empty, and taken states stay readable at a glance.",
            14,
            TextAlignmentOptions.MidlineRight,
            new Vector2(388f, 26f),
            new Vector2(122f, 294f),
            FrameHintColor(),
            MutedTextColor(),
            false);

        GameObject rosterGridFrame = CreateInset(
            "Section_RosterGridFrame",
            roster.transform,
            new Vector2(700f, 618f),
            new Vector2(0f, -18f),
            InsetColor());
        GameObject rosterGrid = CreateRect("Grid_RosterSlots", rosterGridFrame.transform, new Vector2(664f, 596f));
        SetAnchored(rosterGrid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        GridLayoutGroup grid = rosterGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(314f, 134f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        List<SavedArmiesSlotView> slotViews = new List<SavedArmiesSlotView>();
        for (int i = 0; i < 8; i++)
        {
            GameObject slot = InstantiateNestedPrefab(SlotPath, "SavedArmySlot_" + (i + 1).ToString("00"), rosterGrid.transform);
            LayoutElement layout = slot.AddComponent<LayoutElement>();
            layout.preferredWidth = 314f;
            layout.preferredHeight = 134f;
            SavedArmiesSlotView view = slot.GetComponent<SavedArmiesSlotView>();
            if (view != null)
            {
                slotViews.Add(view);
            }
        }

        GameObject detail = CreateDecoratedPanel(
            "Section_SelectedArmyDetails",
            root.transform,
            new Vector2(430f, 692f),
            PanelBackgroundPath,
            PanelColor(),
            BigFramePath,
            FrameColor(),
            false);
        SetAnchored(detail, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(108f, -6f));
        TextMeshProUGUI selectedTitle = CreateFramedText(
            detail.transform,
            "Text_SelectedArmyTitle",
            "No Saved Army Selected",
            24,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(348f, 36f),
            new Vector2(-8f, 294f),
            FrameInsetColor(),
            TitleColor(),
            false);
        TextMeshProUGUI selectedMeta = CreateFramedText(
            detail.transform,
            "Text_SelectedArmyMeta",
            "Select a taken slot or load a seed army.",
            14,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(348f, 28f),
            new Vector2(-8f, 262f),
            FrameHintColor(),
            MutedTextColor(),
            false);

        GameObject heroInset = CreateInset("Inset_SelectedArmySummary", detail.transform, new Vector2(382f, 126f), new Vector2(0f, 178f), InsetColor());
        CreateInset("Inset_SelectedArmyPortrait", heroInset.transform, new Vector2(90f, 90f), new Vector2(-130f, 0f), new Color(0.06f, 0.05f, 0.04f, 0.92f));
        AddStretchImage("Decor_SelectedArmyPortraitFrame", heroInset.transform, PortraitFramePath, new Color(0.46f, 0.34f, 0.22f, 0.92f), false, new Vector2(90f, 90f), new Vector2(-130f, 0f));
        CreateFramedText(
            heroInset.transform,
            "Text_SelectedArmyGlyph",
            "ARMY",
            16,
            TextAlignmentOptions.Center,
            new Vector2(64f, 28f),
            new Vector2(-130f, 0f),
            new Color(0f, 0f, 0f, 0f),
            ImportantValueColor(),
            true);
        TextMeshProUGUI selectedValue = CreateFramedText(
            heroInset.transform,
            "Text_SelectedArmyValue",
            "Current Value 0",
            28,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(214f, 40f),
            new Vector2(70f, 20f),
            new Color(0.07f, 0.05f, 0.03f, 0.88f),
            ImportantValueColor(),
            true);
        CreateFramedText(
            heroInset.transform,
            "Text_SelectedArmyValueHint",
            "Immutable snapshot. Current value recalculates from live unit definitions.",
            13,
            TextAlignmentOptions.TopLeft,
            new Vector2(214f, 44f),
            new Vector2(70f, -28f),
            FrameHintColor(),
            SecondaryTextColor(),
            false);

        GameObject stackInset = CreateInset("Inset_SelectedArmyStacks", detail.transform, new Vector2(382f, 306f), new Vector2(0f, -28f), InsetColor());
        CreateFramedText(
            stackInset.transform,
            "Text_StackHeader",
            "STACK PREVIEW",
            18,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(164f, 28f),
            new Vector2(-92f, 124f),
            FrameInsetColor(),
            TitleColor(),
            false);
        CreateFramedText(
            stackInset.transform,
            "Text_StackHint",
            "Unit count, tier, and current value stay readable before defence choice.",
            12,
            TextAlignmentOptions.MidlineRight,
            new Vector2(170f, 24f),
            new Vector2(88f, 124f),
            FrameHintColor(),
            MutedTextColor(),
            false);
        GameObject stackList = CreateRect("List_SelectedArmyStacks", stackInset.transform, new Vector2(350f, 240f));
        SetAnchored(stackList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f));
        VerticalLayoutGroup stackLayout = stackList.AddComponent<VerticalLayoutGroup>();
        stackLayout.spacing = 8f;
        stackLayout.padding = new RectOffset(0, 0, 0, 0);
        stackLayout.childAlignment = TextAnchor.UpperCenter;
        stackLayout.childControlWidth = false;
        stackLayout.childControlHeight = false;
        stackLayout.childForceExpandWidth = false;
        stackLayout.childForceExpandHeight = false;

        List<SavedArmiesStackRowView> stackRows = new List<SavedArmiesStackRowView>();
        for (int i = 0; i < 5; i++)
        {
            GameObject row = InstantiateNestedPrefab(StackRowPath, "StackRow_" + (i + 1).ToString("00"), stackList.transform);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredWidth = 350f;
            layout.preferredHeight = 40f;
            SavedArmiesStackRowView view = row.GetComponent<SavedArmiesStackRowView>();
            if (view != null)
            {
                stackRows.Add(view);
            }
        }

        GameObject setDefenceObject = InstantiateNestedPrefab(CommandButtonPath, "Button_SetDefence", detail.transform);
        SetAnchored(setDefenceObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f));
        SetSize(setDefenceObject, new Vector2(280f, 68f));
        TintDecoratedPanel(setDefenceObject, new Color(0.50f, 0.34f, 0.16f, 1f), new Color(0.68f, 0.50f, 0.24f, 0.96f));
        SavedArmiesCommandButtonView setDefence = setDefenceObject.GetComponent<SavedArmiesCommandButtonView>();
        ConfigureCommand(setDefence, SavedArmiesCommandButtonKind.SetDefence, "Set Defence");

        GameObject right = CreateDecoratedPanel(
            "Section_ImportAndHistory",
            root.transform,
            new Vector2(332f, 692f),
            PanelBackgroundPath,
            PanelColor(),
            BigFramePath,
            FrameColor(),
            false);
        SetAnchored(right, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-184f, -6f));
        CreateFramedText(
            right.transform,
            "Text_ImportHeader",
            "SEED ARMIES",
            22,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(156f, 32f),
            new Vector2(-72f, 294f),
            FrameInsetColor(),
            TitleColor(),
            false);
        CreateFramedText(
            right.transform,
            "Text_ImportHint",
            "Select a source army, then load it into the highlighted slot.",
            12,
            TextAlignmentOptions.MidlineRight,
            new Vector2(142f, 28f),
            new Vector2(76f, 294f),
            FrameHintColor(),
            MutedTextColor(),
            false);
        GameObject arenaInset = CreateInset("Inset_ArenaOptions", right.transform, new Vector2(284f, 180f), new Vector2(0f, 190f), InsetColor());
        GameObject arenaList = CreateRect("List_ArenaImportOptions", arenaInset.transform, new Vector2(248f, 150f));
        SetAnchored(arenaList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        VerticalLayoutGroup arenaLayout = arenaList.AddComponent<VerticalLayoutGroup>();
        arenaLayout.spacing = 8f;
        arenaLayout.childAlignment = TextAnchor.UpperCenter;
        arenaLayout.childControlWidth = false;
        arenaLayout.childControlHeight = false;
        arenaLayout.childForceExpandWidth = false;
        arenaLayout.childForceExpandHeight = false;

        List<SavedArmiesArenaOptionView> arenaViews = new List<SavedArmiesArenaOptionView>();
        for (int i = 0; i < 3; i++)
        {
            GameObject option = InstantiateNestedPrefab(ArenaOptionPath, "ArenaOption_" + (i + 1).ToString("00"), arenaList.transform);
            LayoutElement layout = option.AddComponent<LayoutElement>();
            layout.preferredWidth = 248f;
            layout.preferredHeight = 44f;
            SavedArmiesArenaOptionView view = option.GetComponent<SavedArmiesArenaOptionView>();
            if (view != null)
            {
                arenaViews.Add(view);
            }
        }

        GameObject importObject = InstantiateNestedPrefab(CommandButtonPath, "Button_ImportFromArena", right.transform);
        SetAnchored(importObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 72f));
        SetSize(importObject, new Vector2(260f, 62f));
        TintDecoratedPanel(importObject, new Color(0.45f, 0.28f, 0.13f, 1f), new Color(0.60f, 0.43f, 0.22f, 0.96f));
        SavedArmiesCommandButtonView import = importObject.GetComponent<SavedArmiesCommandButtonView>();
        ConfigureCommand(import, SavedArmiesCommandButtonKind.ImportFromArena, "Load Seed Army");

        CreateFramedText(
            right.transform,
            "Text_HistoryHeader",
            "ATTACK HISTORY",
            22,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(170f, 32f),
            new Vector2(-64f, -2f),
            FrameInsetColor(),
            TitleColor(),
            false);
        CreateFramedText(
            right.transform,
            "Text_HistoryHint",
            "Offence and defence results stay in one scan-friendly list.",
            12,
            TextAlignmentOptions.MidlineRight,
            new Vector2(164f, 28f),
            new Vector2(58f, -2f),
            FrameHintColor(),
            MutedTextColor(),
            false);
        GameObject historyInset = CreateInset("Inset_AttackHistory", right.transform, new Vector2(284f, 224f), new Vector2(0f, -156f), InsetColor());
        GameObject historyList = CreateRect("List_AttackHistory", historyInset.transform, new Vector2(248f, 188f));
        SetAnchored(historyList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        VerticalLayoutGroup historyLayout = historyList.AddComponent<VerticalLayoutGroup>();
        historyLayout.spacing = 8f;
        historyLayout.childAlignment = TextAnchor.UpperCenter;
        historyLayout.childControlWidth = false;
        historyLayout.childControlHeight = false;
        historyLayout.childForceExpandWidth = false;
        historyLayout.childForceExpandHeight = false;

        List<SavedArmiesHistoryEntryView> historyRows = new List<SavedArmiesHistoryEntryView>();
        for (int i = 0; i < 3; i++)
        {
            GameObject row = InstantiateNestedPrefab(HistoryRowPath, "HistoryRow_" + (i + 1).ToString("00"), historyList.transform);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredWidth = 248f;
            layout.preferredHeight = 52f;
            SavedArmiesHistoryEntryView view = row.GetComponent<SavedArmiesHistoryEntryView>();
            if (view != null)
            {
                historyRows.Add(view);
            }
        }

        CreateFramedText(
            right.transform,
            "Text_BackendGap",
            "Roster persistence: tutaj powinno byc z bazy danych",
            12,
            TextAlignmentOptions.TopLeft,
            new Vector2(252f, 40f),
            new Vector2(0f, -302f),
            FrameHintColor(),
            WarningTextColor(),
            false);

        GameObject statusInset = CreateInset("Inset_Status", root.transform, new Vector2(800f, 50f), new Vector2(134f, -404f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        TextMeshProUGUI status = CreateFramedText(
            statusInset.transform,
            "Text_Status",
            "Offline Saved Armies prototype.",
            16,
            TextAlignmentOptions.Midline,
            new Vector2(744f, 30f),
            Vector2.zero,
            new Color(0f, 0f, 0f, 0f),
            PrimaryTextColor(),
            false);

        GameObject backObject = InstantiateNestedPrefab(CommandButtonPath, "Button_Back", root.transform);
        SetAnchored(backObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(204f, 58f));
        SetSize(backObject, new Vector2(188f, 56f));
        TintDecoratedPanel(backObject, new Color(0.22f, 0.16f, 0.10f, 1f), new Color(0.38f, 0.28f, 0.18f, 0.92f));
        SavedArmiesCommandButtonView back = backObject.GetComponent<SavedArmiesCommandButtonView>();
        ConfigureCommand(back, SavedArmiesCommandButtonKind.Back, "Back");

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "titleText", title);
        SetObject(serialized, "defenceStateText", defenceState);
        SetObject(serialized, "statusText", status);
        SetObject(serialized, "selectedArmyTitleText", selectedTitle);
        SetObject(serialized, "selectedArmyMetaText", selectedMeta);
        SetObject(serialized, "selectedArmyValueText", selectedValue);
        SetObjectArray(serialized, "stackRows", stackRows.ToArray());
        SetObjectArray(serialized, "slotViews", slotViews.ToArray());
        SetObjectArray(serialized, "arenaOptions", arenaViews.ToArray());
        SetObjectArray(serialized, "historyRows", historyRows.ToArray());
        SetObject(serialized, "importButton", import);
        SetObject(serialized, "setDefenceButton", setDefence);
        SetObject(serialized, "backButton", back);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildSlotTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_026_SavedArmySlot_Polished",
            null,
            new Vector2(314f, 134f),
            PanelBackgroundPath,
            new Color(0.14f, 0.10f, 0.07f, 0.98f),
            LongRowFramePath,
            new Color(0.48f, 0.34f, 0.22f, 0.88f),
            true);
        Button button = root.AddComponent<Button>();
        SavedArmiesSlotView view = root.AddComponent<SavedArmiesSlotView>();
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);

        CreateInset("Inset_IconWell", root.transform, new Vector2(78f, 78f), new Vector2(-110f, 0f), new Color(0.05f, 0.04f, 0.03f, 0.84f));
        AddStretchImage("Decor_IconFrame", root.transform, IconFramePath, new Color(0.34f, 0.25f, 0.18f, 0.86f), false, new Vector2(78f, 78f), new Vector2(-110f, 0f));
        CreateFramedText(
            root.transform,
            "Text_SlotGlyph",
            "ARMY",
            14,
            TextAlignmentOptions.Center,
            new Vector2(56f, 24f),
            new Vector2(-110f, 0f),
            new Color(0f, 0f, 0f, 0f),
            ImportantValueColor(),
            true);

        TextMeshProUGUI slotNumber = CreateFramedText(
            root.transform,
            "Text_SlotNumber",
            "1",
            18,
            TextAlignmentOptions.Center,
            new Vector2(44f, 30f),
            new Vector2(-130f, 46f),
            FrameInsetColor(),
            TitleColor(),
            true);
        TextMeshProUGUI state = CreateFramedText(
            root.transform,
            "Text_State",
            "Taken",
            16,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(180f, 30f),
            new Vector2(44f, 42f),
            FrameInsetColor(),
            PrimaryTextColor(),
            false);
        TextMeshProUGUI value = CreateFramedText(
            root.transform,
            "Text_Value",
            "Value 2,450",
            18,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(180f, 32f),
            new Vector2(44f, 4f),
            new Color(0.08f, 0.06f, 0.04f, 0.88f),
            ImportantValueColor(),
            true);
        TextMeshProUGUI savedId = CreateFramedText(
            root.transform,
            "Text_SavedArmyId",
            "saved-army",
            12,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(180f, 26f),
            new Vector2(44f, -36f),
            FrameHintColor(),
            SecondaryTextColor(),
            false);

        GameObject selected = CreateOverlay("State_Selected", root.transform, new Color(0.94f, 0.77f, 0.56f, 0.14f));
        AddStretchImage("Decor_SelectedFrame", selected.transform, LongRowFramePath, new Color(0.94f, 0.77f, 0.56f, 0.94f), false);
        selected.SetActive(false);

        GameObject locked = CreateOverlay("State_Locked", root.transform, new Color(0.01f, 0.01f, 0.01f, 0.60f));
        CreateFramedText(
            locked.transform,
            "Text_LockedState",
            "LOCKED",
            18,
            TextAlignmentOptions.Center,
            new Vector2(128f, 34f),
            new Vector2(70f, 0f),
            new Color(0.05f, 0.04f, 0.03f, 0.90f),
            WarningTextColor(),
            true);
        locked.SetActive(false);

        GameObject defence = CreateInset("State_CurrentDefence", root.transform, new Vector2(150f, 26f), new Vector2(70f, -48f), new Color(0.09f, 0.18f, 0.08f, 0.88f));
        CreateFramedText(
            defence.transform,
            "Text_CurrentDefenceLabel",
            "CURRENT DEFENCE",
            11,
            TextAlignmentOptions.Center,
            new Vector2(126f, 20f),
            Vector2.zero,
            new Color(0f, 0f, 0f, 0f),
            SuccessTextColor(),
            true);
        defence.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "slotNumberText", slotNumber);
        SetObject(serialized, "stateText", state);
        SetObject(serialized, "valueText", value);
        SetObject(serialized, "savedArmyIdText", savedId);
        SetObject(serialized, "selectedState", selected);
        SetObject(serialized, "lockedOverlay", locked);
        SetObject(serialized, "defenceMarker", defence);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildStackRowTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_026_StackRow_Polished",
            null,
            new Vector2(350f, 40f),
            PanelBackgroundPath,
            new Color(0.12f, 0.10f, 0.07f, 0.98f),
            LongRowFramePath,
            new Color(0.36f, 0.27f, 0.18f, 0.82f),
            false);
        SavedArmiesStackRowView view = root.AddComponent<SavedArmiesStackRowView>();

        CreateInset("Inset_UnitIconWell", root.transform, new Vector2(34f, 34f), new Vector2(-142f, 0f), new Color(0.05f, 0.04f, 0.03f, 0.84f));
        AddStretchImage("Decor_UnitIconFrame", root.transform, PortraitFramePath, new Color(0.34f, 0.25f, 0.18f, 0.86f), false, new Vector2(34f, 34f), new Vector2(-142f, 0f));
        Image icon = CreatePanelImage("Image_UnitIcon", root.transform, new Vector2(20f, 20f), new Vector2(-142f, 0f), Color.white);
        TextMeshProUGUI name = CreateFramedText(
            root.transform,
            "Text_Name",
            "Stone Golem",
            15,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(118f, 22f),
            new Vector2(-38f, 10f),
            new Color(0f, 0f, 0f, 0f),
            PrimaryTextColor(),
            false);
        TextMeshProUGUI tier = CreateFramedText(
            root.transform,
            "Text_Tier",
            "Tier III / unit 20",
            11,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(118f, 18f),
            new Vector2(-38f, -10f),
            new Color(0f, 0f, 0f, 0f),
            SecondaryTextColor(),
            false);
        TextMeshProUGUI amount = CreateFramedText(
            root.transform,
            "Text_Amount",
            "x120",
            13,
            TextAlignmentOptions.MidlineRight,
            new Vector2(60f, 18f),
            new Vector2(92f, 10f),
            new Color(0f, 0f, 0f, 0f),
            PrimaryTextColor(),
            true);
        TextMeshProUGUI value = CreateFramedText(
            root.transform,
            "Text_Value",
            "2,400 value",
            11,
            TextAlignmentOptions.MidlineRight,
            new Vector2(84f, 18f),
            new Vector2(80f, -10f),
            new Color(0f, 0f, 0f, 0f),
            ImportantValueColor(),
            true);

        UnitRepresentation unitRepresentation = root.AddComponent<UnitRepresentation>();
        SerializedObject unitSerialized = new SerializedObject(unitRepresentation);
        SetObject(unitSerialized, "Icon", icon);
        SetObject(unitSerialized, "Name", name);
        SetObject(unitSerialized, "Tier", tier);
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
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildArenaOptionTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_026_ArenaOption_Polished",
            null,
            new Vector2(248f, 44f),
            PanelBackgroundPath,
            new Color(0.12f, 0.10f, 0.07f, 0.98f),
            LongRowFramePath,
            new Color(0.36f, 0.27f, 0.18f, 0.82f),
            true);
        Button button = root.AddComponent<Button>();
        SavedArmiesArenaOptionView view = root.AddComponent<SavedArmiesArenaOptionView>();
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);

        CreateInset("Inset_SeedIconWell", root.transform, new Vector2(40f, 40f), new Vector2(-92f, 0f), new Color(0.05f, 0.04f, 0.03f, 0.84f));
        AddStretchImage("Decor_SeedIconFrame", root.transform, IconFramePath, new Color(0.34f, 0.25f, 0.18f, 0.86f), false, new Vector2(40f, 40f), new Vector2(-92f, 0f));
        CreateFramedText(
            root.transform,
            "Text_SeedGlyph",
            "S",
            15,
            TextAlignmentOptions.Center,
            new Vector2(28f, 22f),
            new Vector2(-92f, 0f),
            new Color(0f, 0f, 0f, 0f),
            ImportantValueColor(),
            true);

        TextMeshProUGUI name = CreateFramedText(
            root.transform,
            "Text_Name",
            "Seed Army",
            14,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(112f, 18f),
            new Vector2(10f, 8f),
            new Color(0f, 0f, 0f, 0f),
            PrimaryTextColor(),
            false);
        TextMeshProUGUI value = CreateFramedText(
            root.transform,
            "Text_Value",
            "Value 1,200",
            11,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(112f, 16f),
            new Vector2(10f, -10f),
            new Color(0f, 0f, 0f, 0f),
            ImportantValueColor(),
            true);

        GameObject selected = CreateOverlay("State_Selected", root.transform, new Color(0.94f, 0.77f, 0.56f, 0.14f));
        AddStretchImage("Decor_SelectedFrame", selected.transform, LongRowFramePath, new Color(0.94f, 0.77f, 0.56f, 0.94f), false);
        selected.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "nameText", name);
        SetObject(serialized, "valueText", value);
        SetObject(serialized, "selectedState", selected);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildHistoryRowTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_026_HistoryRow_Polished",
            null,
            new Vector2(248f, 52f),
            PanelBackgroundPath,
            new Color(0.11f, 0.09f, 0.07f, 0.98f),
            LongRowFramePath,
            new Color(0.30f, 0.24f, 0.18f, 0.76f),
            false);
        SavedArmiesHistoryEntryView view = root.AddComponent<SavedArmiesHistoryEntryView>();

        CreatePanelImage("Decor_ResultAccent", root.transform, new Vector2(6f, 34f), new Vector2(-110f, 0f), new Color(0.40f, 0.72f, 0.38f, 1f));
        TextMeshProUGUI result = CreateFramedText(
            root.transform,
            "Text_Result",
            "Defence Win",
            12,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(98f, 18f),
            new Vector2(-52f, 10f),
            new Color(0f, 0f, 0f, 0f),
            PrimaryTextColor(),
            false);
        TextMeshProUGUI opponent = CreateFramedText(
            root.transform,
            "Text_Opponent",
            "Forest Bandits",
            11,
            TextAlignmentOptions.MidlineRight,
            new Vector2(102f, 18f),
            new Vector2(54f, 10f),
            new Color(0f, 0f, 0f, 0f),
            SecondaryTextColor(),
            false);
        TextMeshProUGUI values = CreateFramedText(
            root.transform,
            "Text_Values",
            "A 2,450 / D 2,110",
            11,
            TextAlignmentOptions.Center,
            new Vector2(200f, 18f),
            new Vector2(0f, -12f),
            new Color(0f, 0f, 0f, 0f),
            ImportantValueColor(),
            true);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "resultText", result);
        SetObject(serialized, "opponentText", opponent);
        SetObject(serialized, "valuesText", values);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_026_CommandButton_Polished",
            null,
            new Vector2(236f, 64f),
            ButtonPrimaryPath,
            new Color(0.52f, 0.34f, 0.17f, 1f),
            ButtonFramePath,
            new Color(0.64f, 0.46f, 0.24f, 0.96f),
            true);
        Button button = root.AddComponent<Button>();
        SavedArmiesCommandButtonView view = root.AddComponent<SavedArmiesCommandButtonView>();
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);

        TextMeshProUGUI label = CreateFramedText(
            root.transform,
            "Text_Label",
            "COMMAND",
            20,
            TextAlignmentOptions.Center,
            new Vector2(190f, 28f),
            Vector2.zero,
            new Color(0f, 0f, 0f, 0f),
            TitleColor(),
            false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "labelText", label);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static void ConfigureCommand(SavedArmiesCommandButtonView view, SavedArmiesCommandButtonKind kind, string label)
    {
        if (view == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(view);
        SerializedProperty property = serialized.FindProperty("commandKind");
        if (property != null)
        {
            property.enumValueIndex = (int)kind;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        view.Bind(label, true);
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

    private static GameObject CreateDecoratedPanel(
        string name,
        Transform parent,
        Vector2 size,
        string backgroundSpritePath,
        Color backgroundColor,
        string frameSpritePath,
        Color frameColor,
        bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent, size);
        Image image = go.AddComponent<Image>();
        ApplySprite(image, backgroundSpritePath, backgroundColor, raycastTarget);
        AddStretchImage("Decor_Frame", go.transform, frameSpritePath, frameColor, false);
        return go;
    }

    private static GameObject CreateInset(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreateRect(name, parent, size);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Image image = go.AddComponent<Image>();
        ApplySprite(image, PanelBackgroundPath, color, false);
        AddStretchImage("Decor_Frame", go.transform, MidFramePath, new Color(0.24f, 0.18f, 0.12f, 0.72f), false);
        return go;
    }

    private static TextMeshProUGUI CreateFramedText(
        Transform parent,
        string textName,
        string text,
        int fontSize,
        TextAlignmentOptions alignment,
        Vector2 frameSize,
        Vector2 anchoredPosition,
        Color frameColor,
        Color textColor,
        bool useNumberFont)
    {
        GameObject frame = CreateRect("Frame", parent, frameSize);
        SetAnchored(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Image image = frame.AddComponent<Image>();
        ApplySprite(image, PanelBackgroundPath, frameColor, false);

        GameObject textObject = CreateRect(textName, frame.transform, new Vector2(Mathf.Max(0f, frameSize.x - 16f), Mathf.Max(0f, frameSize.y - 8f)));
        SetAnchored(textObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontSizeMin = Mathf.Max(8, fontSize - 4);
        label.fontSizeMax = fontSize;
        label.enableAutoSizing = true;
        label.alignment = alignment;
        label.color = textColor;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.font = useNumberFont ? LoadNumberFont() : LoadBodyFont();
        return label;
    }

    private static Image CreatePanelImage(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreateRect(name, parent, size);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image AddStretchImage(string name, Transform parent, string spritePath, Color tint, bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent, Vector2.zero);
        SetStretch(go);
        Image image = go.AddComponent<Image>();
        ApplySprite(image, spritePath, tint, raycastTarget);
        return image;
    }

    private static Image AddStretchImage(string name, Transform parent, string spritePath, Color tint, bool raycastTarget, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject go = CreateRect(name, parent, size);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Image image = go.AddComponent<Image>();
        ApplySprite(image, spritePath, tint, raycastTarget);
        return image;
    }

    private static GameObject CreateOverlay(string name, Transform parent, Color color)
    {
        GameObject go = CreateRect(name, parent, Vector2.zero);
        SetStretch(go);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static GameObject InstantiateNestedPrefab(string assetPath, string instanceName, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError("Missing nested prefab asset: " + assetPath);
            return CreateRect(instanceName, parent, new Vector2(100f, 40f));
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

    private static void TintDecoratedPanel(GameObject panel, Color backgroundColor, Color frameColor)
    {
        if (panel == null)
        {
            return;
        }

        Image rootImage = panel.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = backgroundColor;
        }

        Transform frame = panel.transform.Find("Decor_Frame");
        if (frame != null)
        {
            Image frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.color = frameColor;
            }
        }
    }

    private static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
    }

    private static void SetStretch(GameObject go)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void SetSize(GameObject go, Vector2 size)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
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

    private static void ApplySprite(Image image, string assetPath, Color tint, bool raycastTarget)
    {
        image.color = tint;
        image.raycastTarget = raycastTarget;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = sprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static TMP_FontAsset LoadBodyFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
    }

    private static TMP_FontAsset LoadNumberFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NumberFontPath);
    }

    private static Color ScreenColor()
    {
        return new Color(0.08f, 0.06f, 0.04f, 1f);
    }

    private static Color PanelColor()
    {
        return new Color(0.14f, 0.10f, 0.07f, 0.98f);
    }

    private static Color InsetColor()
    {
        return new Color(0.06f, 0.05f, 0.04f, 0.86f);
    }

    private static Color FrameColor()
    {
        return new Color(0.42f, 0.31f, 0.21f, 0.90f);
    }

    private static Color FrameInsetColor()
    {
        return new Color(0.07f, 0.05f, 0.03f, 0.82f);
    }

    private static Color FrameHintColor()
    {
        return new Color(0.05f, 0.04f, 0.03f, 0.62f);
    }

    private static Color TitleColor()
    {
        return new Color(0.94f, 0.77f, 0.56f, 1f);
    }

    private static Color PrimaryTextColor()
    {
        return new Color(0.96f, 0.84f, 0.65f, 1f);
    }

    private static Color SecondaryTextColor()
    {
        return new Color(0.78f, 0.61f, 0.44f, 1f);
    }

    private static Color MutedTextColor()
    {
        return new Color(0.54f, 0.44f, 0.35f, 1f);
    }

    private static Color ImportantValueColor()
    {
        return new Color(0.94f, 0.77f, 0.38f, 1f);
    }

    private static Color SuccessTextColor()
    {
        return new Color(0.76f, 0.92f, 0.56f, 1f);
    }

    private static Color WarningTextColor()
    {
        return new Color(0.90f, 0.48f, 0.42f, 1f);
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
