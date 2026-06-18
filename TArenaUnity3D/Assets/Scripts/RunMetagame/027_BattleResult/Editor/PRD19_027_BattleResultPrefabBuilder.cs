using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_027_BattleResultPrefabBuilder
{
    private const string MainPrefabPath = "Assets/Resources/UI/PRD_19/027_BattleResult/PRD_19_027_BattleResult.prefab";
    private const string TemplateFolder = "Assets/Resources/UI/PRD_19/027_BattleResult/Prefabs";
    private const string ArmySummaryCardPath = TemplateFolder + "/PRD19_027_ArmySummaryCard.prefab";
    private const string RankDeltaPanelPath = TemplateFolder + "/PRD19_027_RankDeltaPanel.prefab";
    private const string XpProgressPanelPath = TemplateFolder + "/PRD19_027_XpProgressPanel.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD19_027_CommandButton.prefab";
    private const string LegacyGenericCardPath = TemplateFolder + "/PRD_19_027_BattleResult_Card.prefab";
    private const string BuildSessionKey = "TArena.PRD19_027.BattleResultPrefabBuilder.NestedPrefabsBuilt";

    static PRD19_027_BattleResultPrefabBuilder()
    {
        EditorApplication.delayCall += QueueBuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 027 Battle Result Prefabs")]
    public static void RebuildFromMenu()
    {
        BuildAll();
    }

    private static void QueueBuildOnceAfterCompile()
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
        DeleteAssetIfPresent(LegacyGenericCardPath);

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
        Debug.Log("Rebuilt PRD_19_027 Battle Result UI prefabs with nested repeated prefabs and BattleResultService wiring.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreatePanel("PRD_19_027_BattleResult", null, new Vector2(1600f, 900f), new Color(0.08f, 0.09f, 0.10f, 1f), false);

        GameObject scriptOwner = CreateRect("Script_BattleResultScreenController", root.transform, new Vector2(100f, 100f));
        BattleResultScreenController controller = scriptOwner.AddComponent<BattleResultScreenController>();

        GameObject header = CreatePanel("Section_ResultHeader", root.transform, new Vector2(1520f, 92f), new Color(0.13f, 0.10f, 0.07f, 0.96f), false);
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f));
        TextMeshProUGUI titleText = AddText("Text_Title", header.transform, "Battle Result", 42, TextAlignmentOptions.MidlineLeft, new Vector2(620f, 66f), new Vector2(-420f, 0f), new Color(0.96f, 0.80f, 0.40f, 1f));
        TextMeshProUGUI summaryText = AddText("Text_ResultSummary", header.transform, "Victory / Offline / LocalOfflineAdapter", 20, TextAlignmentOptions.MidlineRight, new Vector2(760f, 50f), new Vector2(340f, 0f), new Color(0.84f, 0.78f, 0.66f, 1f));

        GameObject summaryPanel = CreatePanel("Section_BattleSummary", root.transform, new Vector2(390f, 620f), new Color(0.22f, 0.17f, 0.11f, 0.95f), false);
        SetAnchored(summaryPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(205f, -5f));
        AddText("Header", summaryPanel.transform, "BATTLE SUMMARY", 22, TextAlignmentOptions.Midline, new Vector2(330f, 38f), new Vector2(0f, 270f), new Color(0.96f, 0.80f, 0.40f, 1f));
        AddText("Text_ResultWord", summaryPanel.transform, "VICTORY", 42, TextAlignmentOptions.Midline, new Vector2(320f, 64f), new Vector2(0f, 210f), new Color(0.35f, 0.76f, 0.32f, 1f));
        AddText("Text_BattleType", summaryPanel.transform, "Battle Type\nOFFENCE", 20, TextAlignmentOptions.Midline, new Vector2(320f, 62f), new Vector2(0f, 138f), new Color(0.86f, 0.80f, 0.68f, 1f));
        AddText("Text_OpponentStrength", summaryPanel.transform, "Opponent Strength\nStrong / Rank 1518", 18, TextAlignmentOptions.Midline, new Vector2(320f, 62f), new Vector2(0f, 62f), new Color(0.86f, 0.80f, 0.68f, 1f));
        TextMeshProUGUI detailHeader = AddText("Text_DetailHeader", summaryPanel.transform, "Attacker Army", 22, TextAlignmentOptions.MidlineLeft, new Vector2(320f, 34f), new Vector2(0f, -22f), Color.white);
        TextMeshProUGUI detailBody = AddText("Text_DetailBody", summaryPanel.transform, "Army details render from BattleResultViewData.", 14, TextAlignmentOptions.TopLeft, new Vector2(320f, 240f), new Vector2(0f, -164f), new Color(0.88f, 0.82f, 0.70f, 1f));

        GameObject rosterPreview = CreatePanel("Section_SavedArmiesRosterPreview", summaryPanel.transform, new Vector2(336f, 218f), new Color(0.10f, 0.12f, 0.14f, 0.96f), false);
        SetAnchored(rosterPreview, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -157f));
        TextMeshProUGUI rosterText = AddText("Text_SavedArmiesRosterPreview", rosterPreview.transform, "Saved Armies Roster", 16, TextAlignmentOptions.TopLeft, new Vector2(294f, 170f), Vector2.zero, new Color(0.84f, 0.90f, 1f, 1f));
        rosterPreview.SetActive(false);

        GameObject comparisonPanel = CreatePanel("Section_AttackerVsDefender", root.transform, new Vector2(700f, 620f), new Color(0.18f, 0.14f, 0.10f, 0.94f), false);
        SetAnchored(comparisonPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-45f, -5f));
        AddText("Header", comparisonPanel.transform, "ATTACKER VS DEFENDER", 24, TextAlignmentOptions.Midline, new Vector2(620f, 40f), new Vector2(0f, 270f), new Color(0.96f, 0.80f, 0.40f, 1f));
        AddText("Text_VS", comparisonPanel.transform, "VS", 38, TextAlignmentOptions.Midline, new Vector2(58f, 58f), new Vector2(0f, 40f), new Color(0.96f, 0.86f, 0.62f, 1f));
        TextMeshProUGUI noArmyLostText = AddText("Text_NoArmyLost", comparisonPanel.transform, "NO ARMY LOST", 20, TextAlignmentOptions.Midline, new Vector2(620f, 94f), new Vector2(0f, -260f), new Color(0.85f, 0.96f, 0.68f, 1f));

        GameObject attackerCardObject = InstantiateNestedPrefab(ArmySummaryCardPath, "ArmyCard_Attacker", comparisonPanel.transform);
        SetAnchored(attackerCardObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-176f, 28f));
        GameObject defenderCardObject = InstantiateNestedPrefab(ArmySummaryCardPath, "ArmyCard_Defender", comparisonPanel.transform);
        SetAnchored(defenderCardObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(176f, 28f));
        BattleResultArmySummaryCardView attackerCard = attackerCardObject.GetComponent<BattleResultArmySummaryCardView>();
        BattleResultArmySummaryCardView defenderCard = defenderCardObject.GetComponent<BattleResultArmySummaryCardView>();

        GameObject progressPanel = CreatePanel("Section_RankAndAccountProgress", root.transform, new Vector2(390f, 620f), new Color(0.22f, 0.17f, 0.11f, 0.95f), false);
        SetAnchored(progressPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-205f, -5f));
        AddText("Header", progressPanel.transform, "ACCOUNT PROGRESSION", 22, TextAlignmentOptions.Midline, new Vector2(340f, 38f), new Vector2(0f, 270f), new Color(0.96f, 0.80f, 0.40f, 1f));

        GameObject rankPanelObject = InstantiateNestedPrefab(RankDeltaPanelPath, "Panel_RankDelta", progressPanel.transform);
        SetAnchored(rankPanelObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f));
        BattleResultRankDeltaPanelView rankPanel = rankPanelObject.GetComponent<BattleResultRankDeltaPanelView>();

        GameObject xpPanelObject = InstantiateNestedPrefab(XpProgressPanelPath, "Panel_XpProgress", progressPanel.transform);
        SetAnchored(xpPanelObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -135f));
        BattleResultXpProgressPanelView xpPanel = xpPanelObject.GetComponent<BattleResultXpProgressPanelView>();

        GameObject commands = CreatePanel("Section_Commands", root.transform, new Vector2(660f, 78f), new Color(0.12f, 0.09f, 0.06f, 0.92f), false);
        SetAnchored(commands, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f));
        GameObject continueObject = InstantiateNestedPrefab(CommandButtonPath, "Button_Continue", commands.transform);
        SetAnchored(continueObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, 0f));
        GameObject viewArmiesObject = InstantiateNestedPrefab(CommandButtonPath, "Button_ViewArmies", commands.transform);
        SetAnchored(viewArmiesObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 0f));
        BattleResultCommandButtonView continueCommand = continueObject.GetComponent<BattleResultCommandButtonView>();
        BattleResultCommandButtonView viewArmiesCommand = viewArmiesObject.GetComponent<BattleResultCommandButtonView>();
        continueCommand.SetLabel("Continue");
        viewArmiesCommand.SetLabel("View Armies");

        TextMeshProUGUI flowStatus = AddText("Text_FlowStatus", root.transform, "Rendered from BattleResultViewData.", 16, TextAlignmentOptions.Midline, new Vector2(960f, 26f), new Vector2(0f, 110f), new Color(0.84f, 0.80f, 0.70f, 1f));
        SetAnchored(flowStatus.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f));
        TextMeshProUGUI backendGap = AddText("Text_BackendGap", root.transform, "Offline result storage: persisted DB result.", 13, TextAlignmentOptions.Midline, new Vector2(960f, 24f), new Vector2(0f, -126f), new Color(0.64f, 0.60f, 0.52f, 1f));
        SetAnchored(backendGap.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f));

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
        SetObject(serialized, "detailHeaderText", detailHeader);
        SetObject(serialized, "detailBodyText", detailBody);
        SetObject(serialized, "flowStatusText", flowStatus);
        SetObject(serialized, "backendGapText", backendGap);
        SetObject(serialized, "rosterPreviewText", rosterText);
        SetObject(serialized, "rosterPreviewPanel", rosterPreview);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(attackerCard.FocusButton.onClick, controller.OnAttackerArmyClicked);
        UnityEventTools.AddPersistentListener(defenderCard.FocusButton.onClick, controller.OnDefenderArmyClicked);
        UnityEventTools.AddPersistentListener(continueCommand.Button.onClick, controller.OnContinueClicked);
        UnityEventTools.AddPersistentListener(viewArmiesCommand.Button.onClick, controller.OnViewArmiesClicked);

        controller.LoadAndRenderLatestResult();
        return root;
    }

    private static GameObject BuildArmySummaryCardTemplate()
    {
        GameObject root = CreatePanel("PRD19_027_ArmySummaryCard", null, new Vector2(300f, 480f), new Color(0.24f, 0.18f, 0.11f, 0.98f), true);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.64f, 1f);
        colors.pressedColor = new Color(0.82f, 0.62f, 0.32f, 1f);
        colors.selectedColor = new Color(1f, 0.82f, 0.42f, 1f);
        button.colors = colors;

        BattleResultArmySummaryCardView view = root.AddComponent<BattleResultArmySummaryCardView>();
        TextMeshProUGUI role = AddText("Text_Role", root.transform, "ATTACKER", 18, TextAlignmentOptions.Midline, new Vector2(250f, 34f), new Vector2(0f, 210f), new Color(0.96f, 0.80f, 0.40f, 1f));
        TextMeshProUGUI armyName = AddText("Text_ArmyName", root.transform, "Saved Army 3", 24, TextAlignmentOptions.Midline, new Vector2(250f, 42f), new Vector2(0f, 170f), Color.white);
        TextMeshProUGUI owner = AddText("Text_Owner", root.transform, "Owner", 15, TextAlignmentOptions.Midline, new Vector2(250f, 28f), new Vector2(0f, 138f), new Color(0.82f, 0.76f, 0.64f, 1f));
        TextMeshProUGUI power = AddText("Text_Power", root.transform, "18,500 power", 21, TextAlignmentOptions.Midline, new Vector2(250f, 38f), new Vector2(0f, 96f), new Color(0.96f, 0.86f, 0.62f, 1f));
        GameObject iconList = CreateRect("List_StackIcons", root.transform, new Vector2(248f, 52f));
        SetAnchored(iconList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f));
        HorizontalLayoutGroup iconLayout = iconList.AddComponent<HorizontalLayoutGroup>();
        iconLayout.spacing = 6f;
        iconLayout.childAlignment = TextAnchor.MiddleCenter;
        iconLayout.childControlWidth = false;
        iconLayout.childControlHeight = false;
        iconLayout.childForceExpandWidth = false;
        iconLayout.childForceExpandHeight = false;

        Image[] icons = new Image[5];
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i] = CreateImage("Icon_Stack_" + (i + 1).ToString("00"), iconList.transform, new Vector2(42f, 42f), Vector2.zero, new Color(0.36f, 0.27f, 0.16f, 1f), false);
        }

        TextMeshProUGUI stacks = AddText("Text_StackSummary", root.transform, "Stack summary", 14, TextAlignmentOptions.TopLeft, new Vector2(250f, 124f), new Vector2(0f, -58f), new Color(0.88f, 0.82f, 0.70f, 1f));
        TextMeshProUGUI preserved = AddText("Text_Preserved", root.transform, "Saved army preserved", 15, TextAlignmentOptions.Midline, new Vector2(250f, 32f), new Vector2(0f, -204f), new Color(0.70f, 0.92f, 0.62f, 1f));
        GameObject selected = CreateOverlay("State_Selected", root.transform, new Color(1f, 0.78f, 0.20f, 0.18f), false);
        selected.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "focusButton", button);
        SetObject(serialized, "roleText", role);
        SetObject(serialized, "armyNameText", armyName);
        SetObject(serialized, "ownerText", owner);
        SetObject(serialized, "powerText", power);
        SetObject(serialized, "stackSummaryText", stacks);
        SetObject(serialized, "preservedText", preserved);
        SetObjectArray(serialized, "stackIcons", icons);
        SetObject(serialized, "selectedState", selected);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildRankDeltaPanelTemplate()
    {
        GameObject root = CreatePanel("PRD19_027_RankDeltaPanel", null, new Vector2(350f, 220f), new Color(0.18f, 0.14f, 0.10f, 0.96f), false);
        BattleResultRankDeltaPanelView view = root.AddComponent<BattleResultRankDeltaPanelView>();
        TextMeshProUGUI result = AddText("Text_Result", root.transform, "OFFENCE VICTORY", 18, TextAlignmentOptions.Midline, new Vector2(300f, 32f), new Vector2(0f, 82f), new Color(0.96f, 0.80f, 0.40f, 1f));
        TextMeshProUGUI delta = AddText("Text_RankDelta", root.transform, "+32", 44, TextAlignmentOptions.Midline, new Vector2(150f, 58f), new Vector2(0f, 28f), new Color(0.42f, 0.92f, 0.42f, 1f));
        TextMeshProUGUI before = AddText("Text_RankBefore", root.transform, "Current 1520", 16, TextAlignmentOptions.MidlineLeft, new Vector2(130f, 30f), new Vector2(-84f, -34f), new Color(0.82f, 0.76f, 0.64f, 1f));
        TextMeshProUGUI after = AddText("Text_RankAfter", root.transform, "New 1552", 16, TextAlignmentOptions.MidlineRight, new Vector2(130f, 30f), new Vector2(84f, -34f), new Color(0.82f, 0.76f, 0.64f, 1f));
        TextMeshProUGUI opponent = AddText("Text_OpponentRank", root.transform, "Player_Jorvik rank 1518", 14, TextAlignmentOptions.Midline, new Vector2(300f, 26f), new Vector2(0f, -70f), new Color(0.86f, 0.80f, 0.68f, 1f));
        TextMeshProUGUI source = AddText("Text_Source", root.transform, "Offline / LocalOfflineAdapter", 12, TextAlignmentOptions.Midline, new Vector2(300f, 24f), new Vector2(0f, -96f), new Color(0.62f, 0.58f, 0.50f, 1f));

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "resultText", result);
        SetObject(serialized, "deltaText", delta);
        SetObject(serialized, "beforeText", before);
        SetObject(serialized, "afterText", after);
        SetObject(serialized, "opponentText", opponent);
        SetObject(serialized, "sourceText", source);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject BuildXpProgressPanelTemplate()
    {
        GameObject root = CreatePanel("PRD19_027_XpProgressPanel", null, new Vector2(350f, 310f), new Color(0.18f, 0.14f, 0.10f, 0.96f), false);
        BattleResultXpProgressPanelView view = root.AddComponent<BattleResultXpProgressPanelView>();
        TextMeshProUGUI level = AddText("Text_Level", root.transform, "Level 9", 24, TextAlignmentOptions.MidlineLeft, new Vector2(145f, 38f), new Vector2(-80f, 118f), new Color(0.96f, 0.86f, 0.62f, 1f));
        TextMeshProUGUI gained = AddText("Text_XpGained", root.transform, "+95 XP", 25, TextAlignmentOptions.MidlineRight, new Vector2(145f, 38f), new Vector2(80f, 118f), new Color(0.42f, 0.68f, 1f, 1f));
        Slider slider = CreateSlider("Slider_XpProgress", root.transform, new Vector2(284f, 28f), new Vector2(0f, 70f));
        TextMeshProUGUI progress = AddText("Text_Progress", root.transform, "15 / 250", 15, TextAlignmentOptions.Midline, new Vector2(290f, 26f), new Vector2(0f, 36f), new Color(0.86f, 0.80f, 0.68f, 1f));
        TextMeshProUGUI total = AddText("Text_TotalXp", root.transform, "2,265 total XP", 16, TextAlignmentOptions.Midline, new Vector2(290f, 28f), new Vector2(0f, 6f), new Color(0.86f, 0.80f, 0.68f, 1f));
        TextMeshProUGUI unlock = AddText("Text_UnlockPreview", root.transform, "Next unlock: future unit pool progress", 16, TextAlignmentOptions.TopLeft, new Vector2(290f, 100f), new Vector2(0f, -70f), Color.white);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "levelText", level);
        SetObject(serialized, "gainedText", gained);
        SetObject(serialized, "totalText", total);
        SetObject(serialized, "progressText", progress);
        SetObject(serialized, "unlockText", unlock);
        SetObject(serialized, "progressSlider", slider);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreatePanel("PRD19_027_CommandButton", null, new Vector2(260f, 56f), new Color(0.48f, 0.30f, 0.12f, 1f), true);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.62f, 1f);
        colors.pressedColor = new Color(0.82f, 0.58f, 0.24f, 1f);
        colors.disabledColor = new Color(0.34f, 0.30f, 0.26f, 1f);
        button.colors = colors;

        BattleResultCommandButtonView view = root.AddComponent<BattleResultCommandButtonView>();
        TextMeshProUGUI label = AddText("Text_Label", root.transform, "Command", 20, TextAlignmentOptions.Midline, new Vector2(230f, 42f), Vector2.zero, Color.white);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "labelText", label);
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

        GameObject background = CreatePanel("Background", root.transform, Vector2.zero, new Color(0.05f, 0.05f, 0.06f, 1f), false);
        Stretch(background);
        GameObject fillArea = CreateRect("Fill Area", root.transform, Vector2.zero);
        Stretch(fillArea);
        GameObject fill = CreatePanel("Fill", fillArea.transform, Vector2.zero, new Color(0.10f, 0.42f, 0.86f, 1f), false);
        Stretch(fill);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill.GetComponent<Image>();
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

    private static GameObject CreateOverlay(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject go = CreatePanel(name, parent, Vector2.zero, color, raycastTarget);
        Stretch(go);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    private static void DeleteAssetIfPresent(string path)
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
