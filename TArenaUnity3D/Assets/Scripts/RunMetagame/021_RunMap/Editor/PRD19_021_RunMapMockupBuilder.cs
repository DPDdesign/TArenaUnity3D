using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_021_RunMapMockupBuilder
{
    private const string BuildSessionKey = "TArena.PRD19.021.RunMapMockupBuilt.v4";
    private const string BaseFolder = "Assets/Resources/UI/PRD_19/021_RunMap";
    private const string PrefabFolder = BaseFolder + "/Prefabs";
    private const string ScreenPath = BaseFolder + "/PRD_19_021_RunMap.prefab";
    private const string RouteNodePath = PrefabFolder + "/PRD_19_021_RunMap_RouteNode.prefab";
    private const string RouteEdgePath = PrefabFolder + "/PRD_19_021_RunMap_RouteEdge.prefab";
    private const string ArmyRowPath = PrefabFolder + "/PRD_19_021_RunMap_ArmyRow.prefab";
    private const string CommandButtonPath = PrefabFolder + "/PRD_19_021_RunMap_CommandButton.prefab";

    private static readonly Color Ink = new Color(0.16f, 0.10f, 0.05f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.72f, 0.32f, 1f);
    private static readonly Color WarmText = new Color(0.88f, 0.78f, 0.56f, 1f);
    private static readonly Color Parchment = new Color(0.64f, 0.48f, 0.25f, 1f);
    private static readonly Color PanelDark = new Color(0.08f, 0.07f, 0.06f, 0.98f);

    static PRD19_021_RunMapMockupBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 021 Run Map Prefab")]
    public static void RebuildFromMenu()
    {
        BuildRunMap();
    }

    private static void BuildOnceAfterCompile()
    {
        if (SessionState.GetBool(BuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(BuildSessionKey, true);
        BuildRunMap();
    }

    private static void BuildRunMap()
    {
        EnsureFolder(PrefabFolder);

        SavePrefab(BuildRouteNodeTemplate(), RouteNodePath);
        SavePrefab(BuildRouteEdgeTemplate(), RouteEdgePath);
        SavePrefab(BuildArmyRowTemplate(), ArmyRowPath);
        SavePrefab(BuildCommandButtonTemplate(), CommandButtonPath);

        AssetDatabase.ImportAsset(RouteNodePath);
        AssetDatabase.ImportAsset(RouteEdgePath);
        AssetDatabase.ImportAsset(ArmyRowPath);
        AssetDatabase.ImportAsset(CommandButtonPath);

        GameObject root = CreatePanel("PRD_19_021_RunMap", null, new Vector2(1600f, 900f), new Color(0.03f, 0.025f, 0.02f, 1f), null, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        GameObject scriptOwner = CreateRect("Script_PRD_19_021_RunMapMockupController", root.transform, Vector2.zero);
        PRD19_021_RunMapMockupController controller = scriptOwner.AddComponent<PRD19_021_RunMapMockupController>();

        AddHeader(root.transform);
        Button viewArmyButton = BuildArmyPanel(root.transform, controller);
        RouteNodeReferences routeRefs = BuildRouteMapPanel(root.transform, controller);
        DetailReferences details = BuildDetailsPanel(root.transform);
        BottomReferences bottom = BuildBottomBar(root.transform, controller);

        WireController(controller, routeRefs, details, bottom, viewArmyButton);

        SavePrefab(root, ScreenPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AddHeader(Transform root)
    {
        GameObject titleBar = CreatePanel("Section_TitleBar", root, new Vector2(620f, 74f), new Color(0.09f, 0.075f, 0.055f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/big_bar.png"), false);
        Place(titleBar, new Vector2(0f, 394f));
        AddText("Text_Title", titleBar.transform, "RUN MAP", 42, TextAlignmentOptions.Center, new Vector2(560f, 58f), Vector2.zero, Gold);
        AddText("Text_Subtitle", titleBar.transform, "Route choice: Balanced Frontier", 16, TextAlignmentOptions.Center, new Vector2(360f, 24f), new Vector2(0f, -42f), WarmText);
    }

    private static Button BuildArmyPanel(Transform root, PRD19_021_RunMapMockupController controller)
    {
        GameObject panel = CreatePanel("Section_Left_YourArmy", root, new Vector2(300f, 760f), PanelDark, LoadSprite("Assets/Classic_RPG_GUI/Parts/info_window.png"), false);
        Place(panel, new Vector2(-634f, 46f));

        AddText("Text_YourArmyTitle", panel.transform, "YOUR ARMY", 27, TextAlignmentOptions.Center, new Vector2(260f, 38f), new Vector2(0f, 342f), Gold);
        AddText("Text_ArmySummary", panel.transform, "Army Value\n2,850", 22, TextAlignmentOptions.Center, new Vector2(230f, 66f), new Vector2(0f, 280f), WarmText);

        GameObject list = CreateRect("List_ArmyRows", panel.transform, new Vector2(260f, 460f));
        Place(list, new Vector2(0f, 12f));
        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        AddArmyRow(list.transform, "ArmyRow_01_Rusher", "Rusher", "70 units", "Value 910", "Assets/Resources/UI/Unit_Icons/Brb_Rusher_sprite.png");
        AddArmyRow(list.transform, "ArmyRow_02_Thrower", "Thrower", "50 units", "Value 700", "Assets/Resources/UI/Unit_Icons/Brb_Thrower_sprite.png");
        AddArmyRow(list.transform, "ArmyRow_03_Axeman", "Axeman", "30 units", "Value 540", "Assets/Resources/UI/Unit_Icons/Brb_Axeman_sprite.png");
        AddArmyRow(list.transform, "ArmyRow_04_Trapper", "Trapper", "25 units", "Value 410", "Assets/Resources/UI/Unit_Icons/Liz_Trapper_sprite.png");
        AddArmyRow(list.transform, "ArmyRow_05_Wisp", "Wisp", "15 units", "Value 290", "Assets/Resources/UI/Unit_Icons/Gol_W_sprite.png");

        GameObject armyButton = InstantiateNested(CommandButtonPath, "Button_ViewArmy", panel.transform);
        Place(armyButton, new Vector2(0f, -336f));
        SetLabel(armyButton, "VIEW ARMY");
        Button button = armyButton.GetComponent<Button>();
        if (button != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, controller.OnViewArmyClicked);
        }

        return button;
    }

    private static RouteNodeReferences BuildRouteMapPanel(Transform root, PRD19_021_RunMapMockupController controller)
    {
        GameObject panel = CreatePanel("Section_Center_ParchmentRouteMap", root, new Vector2(880f, 760f), Parchment, LoadSprite("Assets/Old_Paper_Gui/paper/old_paper_vintage0.png"), false);
        Place(panel, new Vector2(-34f, 46f));

        AddText("Text_MapCaption", panel.transform, "Three route choices from Start Run converge on the final battle.", 18, TextAlignmentOptions.Center, new Vector2(760f, 32f), new Vector2(0f, 320f), Ink);
        AddText("Text_MapNote", panel.transform, "Select a node to inspect possible rewards and uncertain risk.", 15, TextAlignmentOptions.Center, new Vector2(720f, 26f), new Vector2(0f, -326f), Ink);
        AddText("Text_PressurePathLabel", panel.transform, "Pressure Path", 18, TextAlignmentOptions.Left, new Vector2(180f, 24f), new Vector2(-330f, 258f), Ink);
        AddText("Text_RecoveryPathLabel", panel.transform, "Recovery Path", 18, TextAlignmentOptions.Left, new Vector2(180f, 24f), new Vector2(-330f, 48f), Ink);
        AddText("Text_PivotPathLabel", panel.transform, "Pivot Path", 18, TextAlignmentOptions.Left, new Vector2(180f, 24f), new Vector2(-330f, -162f), Ink);

        CreateMapDecoration(panel.transform);

        GameObject edgeLayer = CreateRect("List_RouteEdges_NestedPrefabs", panel.transform, new Vector2(840f, 620f));
        Place(edgeLayer, new Vector2(0f, -8f));
        GameObject nodeLayer = CreateRect("List_RouteNodes_NestedPrefabs", panel.transform, new Vector2(840f, 620f));
        Place(nodeLayer, new Vector2(0f, -8f));

        Vector2 start = new Vector2(-360f, 0f);
        RouteSlot[] slots =
        {
            new RouteSlot("node-pressure-1", "Border Clash", new Vector2(-140f, 210f)),
            new RouteSlot("node-pressure-2", "Hill Ambush", new Vector2(145f, 210f)),
            new RouteSlot("node-recovery-1", "Scavenger Guard", new Vector2(-140f, 0f)),
            new RouteSlot("node-recovery-2", "Run Shop", new Vector2(145f, 0f)),
            new RouteSlot("node-pivot-1", "Recruit Signal", new Vector2(-140f, -210f)),
            new RouteSlot("node-pivot-2", "Proving Fight", new Vector2(145f, -210f)),
            new RouteSlot("node-final", "Final Proof", new Vector2(360f, 0f))
        };

        AddStartMarker(nodeLayer.transform, start);

        AddEdge(edgeLayer.transform, "Edge_Start_To_Pressure", start, slots[0].Position);
        AddEdge(edgeLayer.transform, "Edge_Pressure_1_To_2", slots[0].Position, slots[1].Position);
        AddEdge(edgeLayer.transform, "Edge_Pressure_To_Final", slots[1].Position, slots[6].Position);
        AddEdge(edgeLayer.transform, "Edge_Start_To_Recovery", start, slots[2].Position);
        AddEdge(edgeLayer.transform, "Edge_Recovery_1_To_2", slots[2].Position, slots[3].Position);
        AddEdge(edgeLayer.transform, "Edge_Recovery_To_Final", slots[3].Position, slots[6].Position);
        AddEdge(edgeLayer.transform, "Edge_Start_To_Pivot", start, slots[4].Position);
        AddEdge(edgeLayer.transform, "Edge_Pivot_1_To_2", slots[4].Position, slots[5].Position);
        AddEdge(edgeLayer.transform, "Edge_Pivot_To_Final", slots[5].Position, slots[6].Position);

        RouteNodeReferences references = new RouteNodeReferences(slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject node = InstantiateNested(RouteNodePath, "RouteNode_" + slots[i].NodeId, nodeLayer.transform);
            Place(node, slots[i].Position);
            SetLabel(node, slots[i].Label);

            Button button = node.GetComponent<Button>();
            if (button != null)
            {
                UnityEventTools.AddStringPersistentListener(button.onClick, controller.OnRouteNodeClicked, slots[i].NodeId);
            }

            references.Items[i] = new RouteNodeReference(
                slots[i].NodeId,
                button,
                node.GetComponent<Image>(),
                ChildImage(node, "State_Selected"),
                ChildImage(node, "State_Locked"),
                ChildText(node, "Text_Title"),
                ChildText(node, "Text_Type"),
                ChildText(node, "Text_State"));
        }

        return references;
    }

    private static DetailReferences BuildDetailsPanel(Transform root)
    {
        GameObject panel = CreatePanel("Section_Right_SelectedBattleDetails", root, new Vector2(368f, 760f), PanelDark, LoadSprite("Assets/Classic_RPG_GUI/Parts/info_window.png"), false);
        Place(panel, new Vector2(600f, 46f));

        TextMeshProUGUI sectionTitle = AddText("Text_DetailSectionTitle", panel.transform, "BATTLE", 30, TextAlignmentOptions.Center, new Vector2(300f, 38f), new Vector2(0f, 344f), Gold);
        TextMeshProUGUI title = AddText("Text_SelectedNodeTitle", panel.transform, "Border Clash", 25, TextAlignmentOptions.Center, new Vector2(312f, 44f), new Vector2(0f, 296f), WarmText);

        GameObject portrait = CreatePanel("SelectedBattle_EncounterIcon", panel.transform, new Vector2(180f, 180f), new Color(0.18f, 0.16f, 0.12f, 1f), LoadSprite("Assets/Classic_RPG_GUI/frame_backgrounds/melee_background.png"), false);
        Place(portrait, new Vector2(0f, 190f));
        AddText("Text_EncounterGlyph", portrait.transform, "X", 58, TextAlignmentOptions.Center, new Vector2(150f, 110f), new Vector2(0f, 14f), Gold);

        TextMeshProUGUI type = AddText("Text_SelectedType", panel.transform, "Battle / Available", 18, TextAlignmentOptions.Center, new Vector2(300f, 30f), new Vector2(0f, 72f), WarmText);
        TextMeshProUGUI encounter = AddText("Text_SelectedEncounter", panel.transform, "Encounter: enc-iron-border-clash", 16, TextAlignmentOptions.Center, new Vector2(310f, 28f), new Vector2(0f, 38f), WarmText);

        AddText("Text_RewardsTitle", panel.transform, "POSSIBLE REWARDS", 20, TextAlignmentOptions.Center, new Vector2(300f, 30f), new Vector2(0f, -20f), Gold);
        TextMeshProUGUI rewards = AddText("Text_PossibleRewards", panel.transform, "Possible Rewards: Mass or Skill", 18, TextAlignmentOptions.Top, new Vector2(300f, 70f), new Vector2(0f, -72f), WarmText);

        AddRewardIcon(panel.transform, "RewardHint_01_UnitMass", new Vector2(-92f, -150f), "ARMY");
        AddRewardIcon(panel.transform, "RewardHint_02_Skill", new Vector2(0f, -150f), "SKILL");
        AddRewardIcon(panel.transform, "RewardHint_03_RunGold", new Vector2(92f, -150f), "GOLD");

        AddText("Text_RiskTitle", panel.transform, "EXPECTED RISK", 20, TextAlignmentOptions.Center, new Vector2(300f, 30f), new Vector2(0f, -226f), Gold);
        TextMeshProUGUI risk = AddText("Text_ExpectedRisk", panel.transform, "Expected Risk: Uncertain", 18, TextAlignmentOptions.Top, new Vector2(300f, 60f), new Vector2(0f, -260f), WarmText);
        TextMeshProUGUI current = AddText("Text_CurrentNode", panel.transform, "Current: Run Start", 17, TextAlignmentOptions.Center, new Vector2(300f, 28f), new Vector2(0f, -310f), WarmText);
        TextMeshProUGUI status = AddText("Text_Status", panel.transform, "Run Map ready.", 15, TextAlignmentOptions.Center, new Vector2(318f, 34f), new Vector2(0f, -346f), new Color(0.74f, 0.88f, 0.70f, 1f));

        return new DetailReferences(sectionTitle, title, type, encounter, rewards, risk, current, status);
    }

    private static BottomReferences BuildBottomBar(Transform root, PRD19_021_RunMapMockupController controller)
    {
        GameObject panel = CreatePanel("Section_Bottom_RunMapCommands", root, new Vector2(1568f, 96f), new Color(0.07f, 0.055f, 0.04f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/big_bar_bg.png"), false);
        Place(panel, new Vector2(0f, -386f));

        GameObject backButtonObject = InstantiateNested(CommandButtonPath, "Button_Back", panel.transform);
        Place(backButtonObject, new Vector2(-660f, 0f));
        SetLabel(backButtonObject, "BACK");
        Button backButton = backButtonObject.GetComponent<Button>();
        if (backButton != null)
        {
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.OnBackClicked);
        }

        TextMeshProUGUI runGold = AddBottomStat(panel.transform, "Stat_RunGold", new Vector2(-414f, 0f), "RUN GOLD\n360");
        TextMeshProUGUI stageText = AddBottomStat(panel.transform, "Stat_StageProgress", new Vector2(-42f, 18f), "Stage Progress 0 / 3");
        Slider stageSlider = AddStageSlider(panel.transform, new Vector2(-42f, -22f));
        TextMeshProUGUI armyValue = AddBottomStat(panel.transform, "Stat_ArmyValue", new Vector2(346f, 0f), "Army Value\n2,850");

        GameObject travelButtonObject = InstantiateNested(CommandButtonPath, "Button_Travel", panel.transform);
        Place(travelButtonObject, new Vector2(660f, 0f));
        SetLabel(travelButtonObject, "TRAVEL");
        Button travelButton = travelButtonObject.GetComponent<Button>();
        if (travelButton != null)
        {
            UnityEventTools.AddPersistentListener(travelButton.onClick, controller.OnTravelClicked);
        }

        return new BottomReferences(runGold, stageText, stageSlider, armyValue, travelButton, backButton);
    }

    private static GameObject BuildRouteNodeTemplate()
    {
        GameObject root = CreatePanel("PRD_19_021_RunMap_RouteNode", null, new Vector2(116f, 116f), new Color(0.52f, 0.20f, 0.12f, 1f), LoadSprite("Assets/Old_Paper_Gui/button/circle/button_circle0.png"), true);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();

        GameObject selected = CreatePanel("State_Selected", root.transform, new Vector2(126f, 126f), new Color(1f, 0.78f, 0.18f, 0.58f), LoadSprite("Assets/Old_Paper_Gui/button/circle/button_circle2.png"), false);
        Place(selected, Vector2.zero);
        GameObject locked = CreatePanel("State_Locked", root.transform, new Vector2(116f, 116f), new Color(0f, 0f, 0f, 0.54f), null, false);
        Place(locked, Vector2.zero);

        AddText("Text_Title", root.transform, "Route Node", 15, TextAlignmentOptions.Center, new Vector2(96f, 42f), new Vector2(0f, 18f), Color.white);
        AddText("Text_Type", root.transform, "Battle", 12, TextAlignmentOptions.Center, new Vector2(90f, 22f), new Vector2(0f, -16f), WarmText);
        AddText("Text_State", root.transform, "Available", 10, TextAlignmentOptions.Center, new Vector2(88f, 18f), new Vector2(0f, -39f), Gold);
        return root;
    }

    private static GameObject BuildRouteEdgeTemplate()
    {
        GameObject root = CreatePanel("PRD_19_021_RunMap_RouteEdge", null, new Vector2(120f, 10f), new Color(0.24f, 0.13f, 0.06f, 0.95f), LoadSprite("Assets/Old_Paper_Gui/background/other/line.png"), false);
        return root;
    }

    private static GameObject BuildArmyRowTemplate()
    {
        GameObject root = CreatePanel("PRD_19_021_RunMap_ArmyRow", null, new Vector2(260f, 80f), new Color(0.12f, 0.095f, 0.07f, 0.98f), LoadSprite("Assets/Classic_RPG_GUI/Parts/inventory_frame_little_long.png"), false);
        LayoutElement layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 260f;
        layoutElement.preferredHeight = 80f;

        GameObject portrait = CreatePanel("Image_Portrait", root.transform, new Vector2(58f, 58f), new Color(0.22f, 0.19f, 0.15f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/Hero_icon_frame_bg.png"), false);
        Place(portrait, new Vector2(-96f, 0f));
        AddText("Text_Name", root.transform, "Unit", 18, TextAlignmentOptions.MidlineLeft, new Vector2(120f, 26f), new Vector2(-20f, 18f), Gold);
        AddText("Text_Count", root.transform, "00 units", 14, TextAlignmentOptions.MidlineLeft, new Vector2(120f, 22f), new Vector2(-20f, -6f), WarmText);
        AddText("Text_Value", root.transform, "Value 000", 13, TextAlignmentOptions.MidlineRight, new Vector2(82f, 22f), new Vector2(74f, -22f), WarmText);
        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreatePanel("PRD_19_021_RunMap_CommandButton", null, new Vector2(210f, 62f), new Color(0.34f, 0.15f, 0.08f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/long_button.png"), true);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        AddText("Text_Label", root.transform, "COMMAND", 24, TextAlignmentOptions.Center, new Vector2(174f, 40f), Vector2.zero, Color.white);
        return root;
    }

    private static void CreateMapDecoration(Transform parent)
    {
        AddText("Decoration_MountainMarks", parent, "/\\   /\\        /\\          /\\   /\\", 24, TextAlignmentOptions.Center, new Vector2(620f, 40f), new Vector2(-20f, 236f), new Color(0.25f, 0.16f, 0.08f, 0.55f));
        AddText("Decoration_ForestMarks", parent, "^ ^ ^     ^ ^ ^ ^      ^ ^", 18, TextAlignmentOptions.Center, new Vector2(620f, 32f), new Vector2(16f, -260f), new Color(0.18f, 0.15f, 0.07f, 0.55f));
        AddText("Decoration_Compass", parent, "N\n+\nS", 18, TextAlignmentOptions.Center, new Vector2(70f, 90f), new Vector2(-374f, -252f), new Color(0.18f, 0.09f, 0.04f, 0.78f));
    }

    private static void AddStartMarker(Transform parent, Vector2 position)
    {
        GameObject start = CreatePanel("Marker_RunStart_CurrentArmy", parent, new Vector2(120f, 76f), new Color(0.24f, 0.20f, 0.12f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/mid_bar.png"), false);
        Place(start, position);
        AddText("Text_StartLabel", start.transform, "RUN\nSTART", 18, TextAlignmentOptions.Center, new Vector2(90f, 50f), Vector2.zero, Gold);
    }

    private static void AddEdge(Transform parent, string name, Vector2 from, Vector2 to)
    {
        GameObject edge = InstantiateNested(RouteEdgePath, name, parent);
        RectTransform rect = edge.GetComponent<RectTransform>();
        Vector2 delta = to - from;
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, 10f);
        rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static void AddArmyRow(Transform parent, string name, string unitName, string count, string value, string spritePath)
    {
        GameObject row = InstantiateNested(ArmyRowPath, name, parent);
        SetChildText(row, "Text_Name", unitName);
        SetChildText(row, "Text_Count", count);
        SetChildText(row, "Text_Value", value);

        Image portrait = ChildImage(row, "Image_Portrait");
        if (portrait != null)
        {
            portrait.sprite = LoadSprite(spritePath);
            portrait.preserveAspect = true;
        }
    }

    private static void AddRewardIcon(Transform parent, string name, Vector2 position, string label)
    {
        GameObject icon = CreatePanel(name, parent, new Vector2(68f, 68f), new Color(0.18f, 0.12f, 0.07f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/inventory_frame_little.png"), false);
        Place(icon, position);
        AddText("Text_Label", icon.transform, label, 12, TextAlignmentOptions.Center, new Vector2(56f, 30f), Vector2.zero, Gold);
    }

    private static TextMeshProUGUI AddBottomStat(Transform parent, string name, Vector2 position, string text)
    {
        GameObject stat = CreatePanel(name, parent, new Vector2(230f, 74f), new Color(0.10f, 0.085f, 0.065f, 0.95f), LoadSprite("Assets/Classic_RPG_GUI/Parts/mid_bar.png"), false);
        Place(stat, position);
        return AddText("Text_Value", stat.transform, text, 22, TextAlignmentOptions.Center, new Vector2(200f, 56f), Vector2.zero, WarmText);
    }

    private static Slider AddStageSlider(Transform parent, Vector2 position)
    {
        GameObject sliderRoot = CreateRect("Slider_StageProgress", parent, new Vector2(300f, 18f));
        Place(sliderRoot, position);
        Slider slider = sliderRoot.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 3f;
        slider.wholeNumbers = true;

        GameObject background = CreatePanel("Background", sliderRoot.transform, new Vector2(300f, 10f), new Color(0.09f, 0.06f, 0.03f, 1f), null, false);
        Place(background, Vector2.zero);
        GameObject fillArea = CreateRect("Fill Area", sliderRoot.transform, new Vector2(292f, 10f));
        Place(fillArea, Vector2.zero);
        GameObject fill = CreatePanel("Fill", fillArea.transform, new Vector2(292f, 10f), Gold, null, false);
        Place(fill, Vector2.zero);

        slider.targetGraphic = background.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        return slider;
    }

    private static void WireController(PRD19_021_RunMapMockupController controller, RouteNodeReferences routeRefs, DetailReferences details, BottomReferences bottom, Button viewArmyButton)
    {
        SerializedObject serialized = new SerializedObject(controller);

        SetObject(serialized, "selectedTitleText", details.SelectedTitle);
        SetObject(serialized, "selectedSectionTitleText", details.SectionTitle);
        SetObject(serialized, "selectedTypeText", details.SelectedType);
        SetObject(serialized, "selectedEncounterText", details.SelectedEncounter);
        SetObject(serialized, "possibleRewardsText", details.PossibleRewards);
        SetObject(serialized, "expectedRiskText", details.ExpectedRisk);
        SetObject(serialized, "currentNodeText", details.CurrentNode);
        SetObject(serialized, "statusText", details.Status);
        SetObject(serialized, "runGoldText", bottom.RunGold);
        SetObject(serialized, "stageProgressText", bottom.StageProgress);
        SetObject(serialized, "stageProgressSlider", bottom.StageProgressSlider);
        SetObject(serialized, "armyValueText", bottom.ArmyValue);
        SetObject(serialized, "travelButton", bottom.TravelButton);
        SetObject(serialized, "backButton", bottom.BackButton);
        SetObject(serialized, "viewArmyButton", viewArmyButton);

        SerializedProperty nodes = serialized.FindProperty("routeNodes");
        if (nodes != null)
        {
            nodes.arraySize = routeRefs.Items.Length;
            for (int i = 0; i < routeRefs.Items.Length; i++)
            {
                SerializedProperty item = nodes.GetArrayElementAtIndex(i);
                RouteNodeReference reference = routeRefs.Items[i];
                SetRelativeString(item, "nodeId", reference.NodeId);
                SetRelativeObject(item, "button", reference.Button);
                SetRelativeObject(item, "background", reference.Background);
                SetRelativeObject(item, "selectionFrame", reference.SelectionFrame);
                SetRelativeObject(item, "lockedOverlay", reference.LockedOverlay);
                SetRelativeObject(item, "titleText", reference.Title);
                SetRelativeObject(item, "typeText", reference.Type);
                SetRelativeObject(item, "stateText", reference.State);
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject InstantiateNested(string path, string name, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null && prefab != null)
        {
            instance = UnityEngine.Object.Instantiate(prefab);
        }

        if (instance == null)
        {
            throw new InvalidOperationException("Missing nested prefab asset: " + path);
        }

        instance.name = name;
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
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color, Sprite sprite, bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent, size);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = raycastTarget;
        if (sprite != null)
        {
            image.type = Image.Type.Sliced;
        }

        return go;
    }

    private static TextMeshProUGUI AddText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreateRect(name, parent, size);
        Place(go, anchoredPosition);
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

    private static void Place(GameObject go, Vector2 anchoredPosition)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
    }

    private static void SetLabel(GameObject root, string label)
    {
        SetChildText(root, "Text_Label", label);
    }

    private static void SetChildText(GameObject root, string childName, string text)
    {
        TextMeshProUGUI label = ChildText(root, childName);
        if (label != null)
        {
            label.text = text;
        }
    }

    private static TextMeshProUGUI ChildText(GameObject root, string childName)
    {
        Transform child = FindDirectChild(root, childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private static Image ChildImage(GameObject root, string childName)
    {
        Transform child = FindDirectChild(root, childName);
        return child == null ? null : child.GetComponent<Image>();
    }

    private static Transform FindDirectChild(GameObject root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform child = rootTransform.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
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

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetRelativeString(SerializedProperty parent, string propertyName, string value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetRelativeObject(SerializedProperty parent, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private struct RouteSlot
    {
        public readonly string NodeId;
        public readonly string Label;
        public readonly Vector2 Position;

        public RouteSlot(string nodeId, string label, Vector2 position)
        {
            NodeId = nodeId;
            Label = label;
            Position = position;
        }
    }

    private struct RouteNodeReference
    {
        public readonly string NodeId;
        public readonly Button Button;
        public readonly Image Background;
        public readonly Image SelectionFrame;
        public readonly Image LockedOverlay;
        public readonly TextMeshProUGUI Title;
        public readonly TextMeshProUGUI Type;
        public readonly TextMeshProUGUI State;

        public RouteNodeReference(string nodeId, Button button, Image background, Image selectionFrame, Image lockedOverlay, TextMeshProUGUI title, TextMeshProUGUI type, TextMeshProUGUI state)
        {
            NodeId = nodeId;
            Button = button;
            Background = background;
            SelectionFrame = selectionFrame;
            LockedOverlay = lockedOverlay;
            Title = title;
            Type = type;
            State = state;
        }
    }

    private class RouteNodeReferences
    {
        public readonly RouteNodeReference[] Items;

        public RouteNodeReferences(int count)
        {
            Items = new RouteNodeReference[count];
        }
    }

    private struct DetailReferences
    {
        public readonly TextMeshProUGUI SectionTitle;
        public readonly TextMeshProUGUI SelectedTitle;
        public readonly TextMeshProUGUI SelectedType;
        public readonly TextMeshProUGUI SelectedEncounter;
        public readonly TextMeshProUGUI PossibleRewards;
        public readonly TextMeshProUGUI ExpectedRisk;
        public readonly TextMeshProUGUI CurrentNode;
        public readonly TextMeshProUGUI Status;

        public DetailReferences(TextMeshProUGUI sectionTitle, TextMeshProUGUI selectedTitle, TextMeshProUGUI selectedType, TextMeshProUGUI selectedEncounter, TextMeshProUGUI possibleRewards, TextMeshProUGUI expectedRisk, TextMeshProUGUI currentNode, TextMeshProUGUI status)
        {
            SectionTitle = sectionTitle;
            SelectedTitle = selectedTitle;
            SelectedType = selectedType;
            SelectedEncounter = selectedEncounter;
            PossibleRewards = possibleRewards;
            ExpectedRisk = expectedRisk;
            CurrentNode = currentNode;
            Status = status;
        }
    }

    private struct BottomReferences
    {
        public readonly TextMeshProUGUI RunGold;
        public readonly TextMeshProUGUI StageProgress;
        public readonly Slider StageProgressSlider;
        public readonly TextMeshProUGUI ArmyValue;
        public readonly Button TravelButton;
        public readonly Button BackButton;

        public BottomReferences(TextMeshProUGUI runGold, TextMeshProUGUI stageProgress, Slider stageProgressSlider, TextMeshProUGUI armyValue, Button travelButton, Button backButton)
        {
            RunGold = runGold;
            StageProgress = stageProgress;
            StageProgressSlider = stageProgressSlider;
            ArmyValue = armyValue;
            TravelButton = travelButton;
            BackButton = backButton;
        }
    }
}
