using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_027_BattleResultPolishedPrefabBuilder
{
    private const string PolishedFolder = "Assets/Resources/UI/PRD_19/027_BattleResult/Polished_1";
    private const string MainPrefabPath = PolishedFolder + "/PRD_19_027_BattleResult_Polished.prefab";
    private const string TemplateFolder = PolishedFolder + "/Prefabs";
    private const string ArmySummaryCardPath = TemplateFolder + "/PRD19_027_ArmySummaryCard_Polished.prefab";
    private const string RankDeltaPanelPath = TemplateFolder + "/PRD19_027_RankDeltaPanel_Polished.prefab";
    private const string XpProgressPanelPath = TemplateFolder + "/PRD19_027_XpProgressPanel_Polished.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD19_027_CommandButton_Polished.prefab";
    private const string BuildSessionKey = "TArena.PRD19_027.BattleResultPolishedPrefabBuilder.NestedPrefabsBuilt";

    private const string BigFramePath = "Assets/GUI_Parts/Gui_parts/Frame_big.png";
    private const string MidFramePath = "Assets/GUI_Parts/Gui_parts/Frame_mid.png";
    private const string ButtonFramePath = "Assets/GUI_Parts/Gui_parts/button_frame.png";
    private const string ScreenBackgroundPath = "Assets/Old_Paper_Gui/background/big/big_dark_background0.png";
    private const string PanelBackgroundPath = "Assets/Old_Paper_Gui/background/mid/middle_dark_background0.png";
    private const string ButtonPrimaryPath = "Assets/GUI_Parts/Gui_parts/button_ready_on.png";
    private const string IconFramePath = "Assets/Classic_RPG_GUI/frame_backgrounds/skill_background.png";
    private const string BodyFontPath = "Assets/Fonts/Noto_Sans/static/NotoSans_SemiCondensed-SemiBold SDF.asset";
    private const string NumberFontPath = "Assets/Fonts/Noto_Sans/static/NotoSans-Bold SDF.asset";

    static PRD19_027_BattleResultPolishedPrefabBuilder()
    {
        EditorApplication.delayCall += QueueBuildAfterSharedBuilder;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 027 Battle Result Polished Prefabs")]
    public static void RebuildFromMenu()
    {
        BuildAll();
    }

    private static void QueueBuildAfterSharedBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
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

        SavePrefab(BuildArmySummaryCardTemplate(), ArmySummaryCardPath);
        SavePrefab(BuildRankDeltaPanelTemplate(), RankDeltaPanelPath);
        SavePrefab(BuildXpProgressPanelTemplate(), XpProgressPanelPath);
        SavePrefab(BuildCommandButtonTemplate(), CommandButtonPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ArmySummaryCardPath);
        AssetDatabase.ImportAsset(RankDeltaPanelPath);
        AssetDatabase.ImportAsset(XpProgressPanelPath);
        AssetDatabase.ImportAsset(CommandButtonPath);

        SavePrefab(BuildMainPrefab(), MainPrefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD_19_027 Battle Result polished UI prefabs in Polished_1.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_027_BattleResult_Polished",
            null,
            new Vector2(1600f, 900f),
            ScreenBackgroundPath,
            ScreenColor(),
            BigFramePath,
            TrimColor(0.92f),
            false);
        DisableRaycast(root);
        CreateOverlay("Decor_ScreenVignette", root.transform, new Color(0.02f, 0.02f, 0.02f, 0.34f));

        GameObject scriptOwner = CreateRect("Script_BattleResultScreenController", root.transform, new Vector2(100f, 100f));
        BattleResultScreenController controller = scriptOwner.AddComponent<BattleResultScreenController>();

        GameObject header = CreateDecoratedPanel(
            "Section_ResultHeader",
            root.transform,
            new Vector2(1504f, 104f),
            PanelBackgroundPath,
            PanelColor(0.98f),
            BigFramePath,
            TrimColor(0.96f),
            false);
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f));

        TextMeshProUGUI titleText = AddText(
            "Text_Title",
            header.transform,
            "BATTLE RESULT",
            46,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(428f, 56f),
            new Vector2(-462f, 10f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.22f));

        AddText(
            "Text_HeaderHint",
            header.transform,
            "Lock in outcome, compare saved armies, then continue the run flow.",
            17,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(640f, 30f),
            new Vector2(-228f, -24f),
            SecondaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.18f));

        AddText(
            "Text_HeaderMode",
            header.transform,
            "OFFLINE AUTHORITY",
            18,
            TextAlignmentOptions.Midline,
            new Vector2(260f, 38f),
            new Vector2(518f, 22f),
            ImportantValueColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.28f));

        TextMeshProUGUI summaryText = AddText(
            "Text_ResultSummary",
            header.transform,
            "Battle Result / Offline / LocalOfflineAdapter / persisted DB result",
            17,
            TextAlignmentOptions.Midline,
            new Vector2(604f, 34f),
            new Vector2(336f, -24f),
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.20f));

        GameObject summaryPanel = CreateDecoratedPanel(
            "Section_BattleSummary",
            root.transform,
            new Vector2(360f, 648f),
            PanelBackgroundPath,
            RaisedPanelColor(0.98f),
            BigFramePath,
            TrimColor(0.88f),
            false);
        SetAnchored(summaryPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, -2f));

        AddText(
            "Text_SummaryHeader",
            summaryPanel.transform,
            "BATTLE SUMMARY",
            24,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(248f, 34f),
            new Vector2(-18f, 286f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.22f));

        AddText(
            "Text_SummaryHint",
            summaryPanel.transform,
            "Outcome, authority, and focused saved-army detail.",
            14,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(262f, 24f),
            new Vector2(-10f, 254f),
            MutedTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.16f));

        AddText(
            "Text_ResultWord",
            summaryPanel.transform,
            "VICTORY",
            38,
            TextAlignmentOptions.Midline,
            new Vector2(260f, 52f),
            new Vector2(0f, 194f),
            SuccessTextColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.30f));

        GameObject battleTypeInset = CreateInset("Inset_BattleType", summaryPanel.transform, new Vector2(304f, 68f), new Vector2(0f, 116f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        AddText(
            "Text_BattleType",
            battleTypeInset.transform,
            "BATTLE TYPE\nOFFENCE",
            17,
            TextAlignmentOptions.Midline,
            new Vector2(252f, 44f),
            Vector2.zero,
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        GameObject opponentInset = CreateInset("Inset_OpponentStrength", summaryPanel.transform, new Vector2(304f, 72f), new Vector2(0f, 32f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        AddText(
            "Text_OpponentStrength",
            opponentInset.transform,
            "OPPONENT\nStrong / Rank 1518",
            16,
            TextAlignmentOptions.Midline,
            new Vector2(252f, 46f),
            Vector2.zero,
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        TextMeshProUGUI detailHeaderText = AddText(
            "Text_DetailHeader",
            summaryPanel.transform,
            "Attacker Army",
            22,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(252f, 32f),
            new Vector2(-16f, -62f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.22f));

        GameObject detailInset = CreateInset("Inset_ArmyDetails", summaryPanel.transform, new Vector2(304f, 228f), new Vector2(0f, -186f), new Color(0.05f, 0.04f, 0.03f, 0.82f));
        TextMeshProUGUI detailBodyText = AddText(
            "Text_DetailBody",
            detailInset.transform,
            "Army details render from BattleResultViewData.",
            15,
            TextAlignmentOptions.TopLeft,
            new Vector2(268f, 190f),
            Vector2.zero,
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.12f));

        GameObject rosterPreviewPanel = CreateDecoratedPanel(
            "Section_SavedArmiesRosterPreview",
            summaryPanel.transform,
            new Vector2(304f, 228f),
            PanelBackgroundPath,
            new Color(0.08f, 0.07f, 0.05f, 0.95f),
            MidFramePath,
            TrimActiveColor(0.94f),
            false);
        SetAnchored(rosterPreviewPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -186f));
        TextMeshProUGUI rosterPreviewText = AddText(
            "Text_SavedArmiesRosterPreview",
            rosterPreviewPanel.transform,
            "Saved Armies Roster",
            15,
            TextAlignmentOptions.TopLeft,
            new Vector2(268f, 190f),
            Vector2.zero,
            new Color(0.90f, 0.92f, 0.82f, 1f),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.12f));
        rosterPreviewPanel.SetActive(false);

        GameObject comparisonPanel = CreateDecoratedPanel(
            "Section_AttackerVsDefender",
            root.transform,
            new Vector2(640f, 648f),
            PanelBackgroundPath,
            PanelColor(0.98f),
            BigFramePath,
            TrimColor(0.90f),
            false);
        SetAnchored(comparisonPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f));

        AddText(
            "Text_ComparisonHeader",
            comparisonPanel.transform,
            "ATTACKER VS DEFENDER",
            26,
            TextAlignmentOptions.Midline,
            new Vector2(360f, 36f),
            new Vector2(0f, 286f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.24f));

        AddText(
            "Text_ComparisonHint",
            comparisonPanel.transform,
            "Read stack pressure first, then army preservation.",
            14,
            TextAlignmentOptions.Midline,
            new Vector2(362f, 24f),
            new Vector2(0f, 254f),
            MutedTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.16f));

        AddText(
            "Text_VS",
            comparisonPanel.transform,
            "VS",
            28,
            TextAlignmentOptions.Midline,
            new Vector2(64f, 44f),
            new Vector2(0f, 40f),
            ImportantValueColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.32f));

        GameObject attackerCardObject = InstantiateNestedPrefab(ArmySummaryCardPath, "ArmyCard_Attacker", comparisonPanel.transform);
        SetAnchored(attackerCardObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-164f, 22f));
        GameObject defenderCardObject = InstantiateNestedPrefab(ArmySummaryCardPath, "ArmyCard_Defender", comparisonPanel.transform);
        SetAnchored(defenderCardObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(164f, 22f));
        BattleResultArmySummaryCardView attackerCard = attackerCardObject.GetComponent<BattleResultArmySummaryCardView>();
        BattleResultArmySummaryCardView defenderCard = defenderCardObject.GetComponent<BattleResultArmySummaryCardView>();

        GameObject preservationInset = CreateInset("Inset_Preservation", comparisonPanel.transform, new Vector2(560f, 92f), new Vector2(0f, -266f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        TextMeshProUGUI noArmyLostText = AddText(
            "Text_NoArmyLost",
            preservationInset.transform,
            "NO ARMY LOST",
            18,
            TextAlignmentOptions.Midline,
            new Vector2(520f, 54f),
            Vector2.zero,
            SuccessTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.14f));

        GameObject progressPanel = CreateDecoratedPanel(
            "Section_RankAndAccountProgress",
            root.transform,
            new Vector2(360f, 648f),
            PanelBackgroundPath,
            RaisedPanelColor(0.98f),
            BigFramePath,
            TrimColor(0.88f),
            false);
        SetAnchored(progressPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220f, -2f));

        AddText(
            "Text_ProgressHeader",
            progressPanel.transform,
            "ACCOUNT PROGRESSION",
            24,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(266f, 34f),
            new Vector2(-10f, 286f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.22f));

        AddText(
            "Text_ProgressHint",
            progressPanel.transform,
            "Rank swing, account XP, and next unlock checkpoint.",
            14,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(278f, 24f),
            new Vector2(-4f, 254f),
            MutedTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.16f));

        GameObject rankPanelObject = InstantiateNestedPrefab(RankDeltaPanelPath, "Panel_RankDelta", progressPanel.transform);
        SetAnchored(rankPanelObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 124f));
        BattleResultRankDeltaPanelView rankPanel = rankPanelObject.GetComponent<BattleResultRankDeltaPanelView>();

        GameObject xpPanelObject = InstantiateNestedPrefab(XpProgressPanelPath, "Panel_XpProgress", progressPanel.transform);
        SetAnchored(xpPanelObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f));
        BattleResultXpProgressPanelView xpPanel = xpPanelObject.GetComponent<BattleResultXpProgressPanelView>();

        GameObject statusInset = CreateInset("Inset_Status", root.transform, new Vector2(840f, 48f), new Vector2(0f, -306f), new Color(0.05f, 0.04f, 0.03f, 0.72f));
        TextMeshProUGUI flowStatusText = AddText(
            "Text_FlowStatus",
            statusInset.transform,
            "Rendered from BattleResultViewData.",
            17,
            TextAlignmentOptions.Midline,
            new Vector2(796f, 28f),
            Vector2.zero,
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        GameObject commands = CreateDecoratedPanel(
            "Section_Commands",
            root.transform,
            new Vector2(656f, 84f),
            PanelBackgroundPath,
            new Color(0.10f, 0.08f, 0.06f, 0.96f),
            BigFramePath,
            TrimColor(0.84f),
            false);
        SetAnchored(commands, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f));

        GameObject viewArmiesObject = InstantiateNestedPrefab(CommandButtonPath, "Button_ViewArmies", commands.transform);
        SetAnchored(viewArmiesObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-154f, 0f));
        SetSize(viewArmiesObject, new Vector2(220f, 60f));
        BattleResultCommandButtonView viewArmiesCommand = viewArmiesObject.GetComponent<BattleResultCommandButtonView>();

        GameObject continueObject = InstantiateNestedPrefab(CommandButtonPath, "Button_Continue", commands.transform);
        SetAnchored(continueObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(126f, 0f));
        SetSize(continueObject, new Vector2(260f, 68f));
        BattleResultCommandButtonView continueCommand = continueObject.GetComponent<BattleResultCommandButtonView>();
        if (continueCommand != null)
        {
            continueCommand.SetLabel("CONTINUE");
        }

        if (viewArmiesCommand != null)
        {
            viewArmiesCommand.SetLabel("VIEW ARMIES");
        }

        GameObject backendInset = CreateInset("Inset_BackendGap", root.transform, new Vector2(900f, 40f), new Vector2(0f, -122f), new Color(0.05f, 0.04f, 0.03f, 0.64f));
        TextMeshProUGUI backendGapText = AddText(
            "Text_BackendGap",
            backendInset.transform,
            "Online authority/result storage: tutaj powinno byc z bazy danych",
            13,
            TextAlignmentOptions.Midline,
            new Vector2(860f, 22f),
            Vector2.zero,
            MutedTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.08f));

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "attackerArmyCard", attackerCard);
        SetObject(serialized, "defenderArmyCard", defenderCard);
        SetObject(serialized, "rankDeltaPanel", rankPanel);
        SetObject(serialized, "xpProgressPanel", xpPanel);
        SetObject(serialized, "continueCommand", continueCommand);
        SetObject(serialized, "viewArmiesCommand", viewArmiesCommand);
        SetObject(serialized, "titleText", titleText);
        SetObject(serialized, "summaryText", summaryText);
        SetObject(serialized, "noArmyLostText", noArmyLostText);
        SetObject(serialized, "detailHeaderText", detailHeaderText);
        SetObject(serialized, "detailBodyText", detailBodyText);
        SetObject(serialized, "flowStatusText", flowStatusText);
        SetObject(serialized, "backendGapText", backendGapText);
        SetObject(serialized, "rosterPreviewText", rosterPreviewText);
        SetObject(serialized, "rosterPreviewPanel", rosterPreviewPanel);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (attackerCard != null && attackerCard.FocusButton != null)
        {
            UnityEventTools.AddPersistentListener(attackerCard.FocusButton.onClick, controller.OnAttackerArmyClicked);
        }

        if (defenderCard != null && defenderCard.FocusButton != null)
        {
            UnityEventTools.AddPersistentListener(defenderCard.FocusButton.onClick, controller.OnDefenderArmyClicked);
        }

        if (continueCommand != null && continueCommand.Button != null)
        {
            UnityEventTools.AddPersistentListener(continueCommand.Button.onClick, controller.OnContinueClicked);
        }

        if (viewArmiesCommand != null && viewArmiesCommand.Button != null)
        {
            UnityEventTools.AddPersistentListener(viewArmiesCommand.Button.onClick, controller.OnViewArmiesClicked);
        }

        controller.LoadAndRenderLatestResult();
        return root;
    }

    private static GameObject BuildArmySummaryCardTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_027_ArmySummaryCard_Polished",
            null,
            new Vector2(272f, 456f),
            PanelBackgroundPath,
            CardColor(0.98f),
            MidFramePath,
            TrimColor(0.88f),
            true);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.74f, 1f);
        colors.pressedColor = new Color(0.86f, 0.72f, 0.50f, 1f);
        colors.selectedColor = new Color(1f, 0.86f, 0.62f, 1f);
        colors.disabledColor = new Color(0.44f, 0.38f, 0.32f, 1f);
        button.colors = colors;

        BattleResultArmySummaryCardView view = root.AddComponent<BattleResultArmySummaryCardView>();

        TextMeshProUGUI roleText = AddText(
            "Text_Role",
            root.transform,
            "ATTACKER",
            17,
            TextAlignmentOptions.Midline,
            new Vector2(208f, 28f),
            new Vector2(0f, 194f),
            ImportantValueColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.28f));

        TextMeshProUGUI armyNameText = AddText(
            "Text_ArmyName",
            root.transform,
            "Saved Army 3",
            24,
            TextAlignmentOptions.Midline,
            new Vector2(220f, 36f),
            new Vector2(0f, 154f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.20f));

        TextMeshProUGUI ownerText = AddText(
            "Text_Owner",
            root.transform,
            "Owner",
            15,
            TextAlignmentOptions.Midline,
            new Vector2(200f, 24f),
            new Vector2(0f, 122f),
            SecondaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.14f));

        TextMeshProUGUI powerText = AddText(
            "Text_Power",
            root.transform,
            "18,500 power",
            21,
            TextAlignmentOptions.Midline,
            new Vector2(206f, 34f),
            new Vector2(0f, 82f),
            ImportantValueColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.24f));

        GameObject iconStrip = CreateInset("Inset_StackIcons", root.transform, new Vector2(220f, 72f), new Vector2(0f, 20f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        GameObject iconList = CreateRect("List_StackIcons", iconStrip.transform, new Vector2(196f, 48f));
        SetAnchored(iconList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        HorizontalLayoutGroup iconLayout = iconList.AddComponent<HorizontalLayoutGroup>();
        iconLayout.padding = new RectOffset(10, 10, 12, 12);
        iconLayout.spacing = 6f;
        iconLayout.childAlignment = TextAnchor.MiddleCenter;
        iconLayout.childControlWidth = false;
        iconLayout.childControlHeight = false;
        iconLayout.childForceExpandWidth = false;
        iconLayout.childForceExpandHeight = false;

        Image[] stackIcons = new Image[5];
        for (int i = 0; i < stackIcons.Length; i++)
        {
            stackIcons[i] = CreateIconSlot("Icon_Stack_" + (i + 1).ToString("00"), iconList.transform);
        }

        GameObject summaryInset = CreateInset("Inset_StackSummary", root.transform, new Vector2(220f, 118f), new Vector2(0f, -98f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        TextMeshProUGUI stackSummaryText = AddText(
            "Text_StackSummary",
            summaryInset.transform,
            "Stack summary",
            15,
            TextAlignmentOptions.TopLeft,
            new Vector2(188f, 88f),
            Vector2.zero,
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.12f));

        TextMeshProUGUI preservedText = AddText(
            "Text_Preserved",
            root.transform,
            "Saved army preserved",
            14,
            TextAlignmentOptions.Midline,
            new Vector2(212f, 26f),
            new Vector2(0f, -196f),
            SuccessTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.18f));

        GameObject selectedState = CreateStateOverlay("State_Selected", root.transform, new Color(0.94f, 0.77f, 0.56f, 0.12f), MidFramePath, TrimActiveColor(0.98f));
        selectedState.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "focusButton", button);
        SetObject(serialized, "roleText", roleText);
        SetObject(serialized, "armyNameText", armyNameText);
        SetObject(serialized, "ownerText", ownerText);
        SetObject(serialized, "powerText", powerText);
        SetObject(serialized, "stackSummaryText", stackSummaryText);
        SetObject(serialized, "preservedText", preservedText);
        SetObjectArray(serialized, "stackIcons", stackIcons);
        SetObject(serialized, "selectedState", selectedState);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildRankDeltaPanelTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_027_RankDeltaPanel_Polished",
            null,
            new Vector2(320f, 232f),
            PanelBackgroundPath,
            CardColor(0.98f),
            MidFramePath,
            TrimColor(0.84f),
            false);
        BattleResultRankDeltaPanelView view = root.AddComponent<BattleResultRankDeltaPanelView>();

        TextMeshProUGUI resultText = AddText(
            "Text_Result",
            root.transform,
            "OFFENCE VICTORY",
            18,
            TextAlignmentOptions.Midline,
            new Vector2(220f, 28f),
            new Vector2(0f, 86f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.22f));

        TextMeshProUGUI deltaText = AddText(
            "Text_RankDelta",
            root.transform,
            "+32",
            42,
            TextAlignmentOptions.Midline,
            new Vector2(140f, 54f),
            new Vector2(0f, 28f),
            SuccessTextColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.28f));

        GameObject beforeInset = CreateInset("Inset_RankBefore", root.transform, new Vector2(128f, 48f), new Vector2(-72f, -32f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        GameObject afterInset = CreateInset("Inset_RankAfter", root.transform, new Vector2(128f, 48f), new Vector2(72f, -32f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        TextMeshProUGUI beforeText = AddText(
            "Text_RankBefore",
            beforeInset.transform,
            "Current 1520",
            15,
            TextAlignmentOptions.Midline,
            new Vector2(112f, 24f),
            Vector2.zero,
            SecondaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));
        TextMeshProUGUI afterText = AddText(
            "Text_RankAfter",
            afterInset.transform,
            "New 1552",
            15,
            TextAlignmentOptions.Midline,
            new Vector2(112f, 24f),
            Vector2.zero,
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        TextMeshProUGUI opponentText = AddText(
            "Text_OpponentRank",
            root.transform,
            "Player_Jorvik rank 1518",
            14,
            TextAlignmentOptions.Midline,
            new Vector2(242f, 22f),
            new Vector2(0f, -82f),
            SecondaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.14f));

        TextMeshProUGUI sourceText = AddText(
            "Text_Source",
            root.transform,
            "Offline / LocalOfflineAdapter",
            12,
            TextAlignmentOptions.Midline,
            new Vector2(242f, 18f),
            new Vector2(0f, -104f),
            MutedTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "resultText", resultText);
        SetObject(serialized, "deltaText", deltaText);
        SetObject(serialized, "beforeText", beforeText);
        SetObject(serialized, "afterText", afterText);
        SetObject(serialized, "opponentText", opponentText);
        SetObject(serialized, "sourceText", sourceText);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildXpProgressPanelTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_027_XpProgressPanel_Polished",
            null,
            new Vector2(320f, 324f),
            PanelBackgroundPath,
            CardColor(0.98f),
            MidFramePath,
            TrimColor(0.84f),
            false);
        BattleResultXpProgressPanelView view = root.AddComponent<BattleResultXpProgressPanelView>();

        TextMeshProUGUI levelText = AddText(
            "Text_Level",
            root.transform,
            "Level 9",
            24,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(120f, 34f),
            new Vector2(-66f, 124f),
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.22f));

        TextMeshProUGUI gainedText = AddText(
            "Text_XpGained",
            root.transform,
            "+95 XP",
            24,
            TextAlignmentOptions.MidlineRight,
            new Vector2(128f, 34f),
            new Vector2(70f, 124f),
            FocusBlueColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.20f));

        Slider progressSlider = CreateSlider("Slider_XpProgress", root.transform, new Vector2(256f, 30f), new Vector2(0f, 74f));

        TextMeshProUGUI progressText = AddText(
            "Text_Progress",
            root.transform,
            "15 / 250",
            15,
            TextAlignmentOptions.Midline,
            new Vector2(180f, 22f),
            new Vector2(0f, 34f),
            PrimaryTextColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.12f));

        TextMeshProUGUI totalText = AddText(
            "Text_TotalXp",
            root.transform,
            "2,265 total XP",
            16,
            TextAlignmentOptions.Midline,
            new Vector2(196f, 24f),
            new Vector2(0f, 6f),
            SecondaryTextColor(),
            LoadNumberFont(),
            FontStyles.Normal,
            FrameColor(0.12f));

        GameObject unlockInset = CreateInset("Inset_UnlockPreview", root.transform, new Vector2(268f, 112f), new Vector2(0f, -94f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        AddText(
            "Text_UnlockHeader",
            unlockInset.transform,
            "NEXT UNLOCK",
            13,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(108f, 18f),
            new Vector2(-62f, 34f),
            SecondaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        TextMeshProUGUI unlockText = AddText(
            "Text_UnlockPreview",
            unlockInset.transform,
            "Next unlock: future unit pool progress",
            15,
            TextAlignmentOptions.TopLeft,
            new Vector2(224f, 56f),
            new Vector2(0f, -10f),
            PrimaryTextColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "levelText", levelText);
        SetObject(serialized, "gainedText", gainedText);
        SetObject(serialized, "totalText", totalText);
        SetObject(serialized, "progressText", progressText);
        SetObject(serialized, "unlockText", unlockText);
        SetObject(serialized, "progressSlider", progressSlider);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD19_027_CommandButton_Polished",
            null,
            new Vector2(236f, 60f),
            ButtonPrimaryPath,
            new Color(0.58f, 0.38f, 0.19f, 1f),
            ButtonFramePath,
            new Color(0.62f, 0.46f, 0.24f, 0.96f),
            true);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.76f, 1f);
        colors.pressedColor = new Color(0.86f, 0.68f, 0.38f, 1f);
        colors.selectedColor = new Color(1f, 0.86f, 0.62f, 1f);
        colors.disabledColor = new Color(0.38f, 0.34f, 0.28f, 1f);
        button.colors = colors;

        BattleResultCommandButtonView view = root.AddComponent<BattleResultCommandButtonView>();
        TextMeshProUGUI labelText = AddText(
            "Text_Label",
            root.transform,
            "COMMAND",
            20,
            TextAlignmentOptions.Midline,
            new Vector2(176f, 28f),
            Vector2.zero,
            TitleColor(),
            LoadBodyFont(),
            FontStyles.Normal,
            FrameColor(0.10f));

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "labelText", labelText);
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

    private static Slider CreateSlider(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject root = CreateRect(name, parent, size);
        SetAnchored(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);

        Slider slider = root.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;

        GameObject background = CreateRect("Background", root.transform, Vector2.zero);
        SetStretch(background);
        Image backgroundImage = background.AddComponent<Image>();
        ApplySprite(backgroundImage, PanelBackgroundPath, new Color(0.05f, 0.04f, 0.03f, 0.92f), false);

        GameObject fillArea = CreateRect("Fill Area", root.transform, Vector2.zero);
        StretchWithPadding(fillArea, 6f, 6f, 6f, 6f);
        GameObject fill = CreateRect("Fill", fillArea.transform, Vector2.zero);
        SetStretch(fill);
        Image fillImage = fill.AddComponent<Image>();
        ApplySprite(fillImage, ButtonPrimaryPath, FocusBlueColor(), false);

        AddStretchImage("Decor_Frame", root.transform, MidFramePath, TrimColor(0.78f), false);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        return slider;
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
        GameObject go = CreateDecoratedPanel(name, parent, size, PanelBackgroundPath, color, MidFramePath, new Color(0.24f, 0.18f, 0.12f, 0.72f), false);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        return go;
    }

    private static Image CreateIconSlot(string name, Transform parent)
    {
        GameObject holder = CreateRect(name, parent, new Vector2(36f, 36f));
        LayoutElement layoutElement = holder.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 36f;
        layoutElement.preferredHeight = 36f;

        Image frame = holder.AddComponent<Image>();
        ApplySprite(frame, IconFramePath, new Color(0.30f, 0.22f, 0.16f, 0.92f), false);

        GameObject iconObject = CreateRect("Icon", holder.transform, new Vector2(28f, 28f));
        SetAnchored(iconObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image icon = iconObject.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        return icon;
    }

    private static TextMeshProUGUI AddText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        TextAlignmentOptions alignment,
        Vector2 size,
        Vector2 anchoredPosition,
        Color color,
        TMP_FontAsset font,
        FontStyles fontStyle,
        Color frameColor)
    {
        GameObject holder = CreateRect(name, parent, size);
        SetAnchored(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);

        GameObject frame = CreateRect("Frame", holder.transform, Vector2.zero);
        SetStretch(frame);
        Image image = frame.AddComponent<Image>();
        ApplySprite(image, PanelBackgroundPath, frameColor, false);

        GameObject labelObject = CreateRect("Label", frame.transform, Vector2.zero);
        StretchWithPadding(labelObject, 8f, 8f, 4f, 4f);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontSizeMin = Mathf.Max(10, fontSize - 4);
        label.fontSizeMax = fontSize;
        label.enableAutoSizing = true;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.fontStyle = fontStyle;
        if (font != null)
        {
            label.font = font;
        }

        return label;
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

    private static GameObject CreateStateOverlay(string name, Transform parent, Color fillColor, string frameSpritePath, Color frameColor)
    {
        GameObject overlay = CreateOverlay(name, parent, fillColor);
        AddStretchImage("Decor_Frame", overlay.transform, frameSpritePath, frameColor, false);
        return overlay;
    }

    private static Image AddStretchImage(string name, Transform parent, string spritePath, Color tint, bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent, Vector2.zero);
        SetStretch(go);
        Image image = go.AddComponent<Image>();
        ApplySprite(image, spritePath, tint, raycastTarget);
        return image;
    }

    private static void DisableRaycast(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    private static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
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

    private static void StretchWithPadding(GameObject go, float left, float right, float top, float bottom)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
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
        return new Color(0.05f, 0.04f, 0.03f, 1f);
    }

    private static Color PanelColor(float alpha)
    {
        return new Color(0.12f, 0.08f, 0.05f, alpha);
    }

    private static Color RaisedPanelColor(float alpha)
    {
        return new Color(0.15f, 0.10f, 0.06f, alpha);
    }

    private static Color CardColor(float alpha)
    {
        return new Color(0.16f, 0.10f, 0.06f, alpha);
    }

    private static Color FrameColor(float alpha)
    {
        return new Color(0.05f, 0.04f, 0.03f, alpha);
    }

    private static Color TrimColor(float alpha)
    {
        return new Color(0.44f, 0.31f, 0.20f, alpha);
    }

    private static Color TrimActiveColor(float alpha)
    {
        return new Color(0.94f, 0.77f, 0.56f, alpha);
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

    private static Color FocusBlueColor()
    {
        return new Color(0.44f, 0.66f, 1f, 1f);
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
