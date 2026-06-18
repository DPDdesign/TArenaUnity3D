using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_023_RewardMapPolishedPrefabBuilder
{
    private const string PolishedFolder = "Assets/Resources/UI/PRD_19/023_RewardMap/Polished_1";
    private const string MainPrefabPath = PolishedFolder + "/PRD_19_023_RewardMap_Polished.prefab";
    private const string TemplateFolder = PolishedFolder + "/Prefabs";
    private const string RewardCardPath = TemplateFolder + "/PRD_19_023_RewardMap_Card_Polished.prefab";
    private const string ArmyPreviewUnitPath = TemplateFolder + "/PRD_19_023_RewardMap_ArmyPreviewUnit_Polished.prefab";
    private const string ResultGainedPanelPath = TemplateFolder + "/PRD_19_023_RewardMap_ResultGainedPanel_Polished.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD_19_023_RewardMap_CommandButton_Polished.prefab";
    private const string BuildSessionKey = "TArena.PRD19_023.RewardMapPolishedPrefabBuilder.NestedPrefabsBuilt";

    private const string BigFramePath = "Assets/GUI_Parts/Gui_parts/Frame_big.png";
    private const string MidFramePath = "Assets/GUI_Parts/Gui_parts/Frame_mid.png";
    private const string ButtonFramePath = "Assets/GUI_Parts/Gui_parts/button_frame.png";
    private const string ScreenBackgroundPath = "Assets/Old_Paper_Gui/background/big/big_dark_background0.png";
    private const string PanelBackgroundPath = "Assets/Old_Paper_Gui/background/mid/middle_dark_background0.png";
    private const string ButtonPrimaryPath = "Assets/GUI_Parts/Gui_parts/button_ready_on.png";
    private const string IconFramePath = "Assets/Classic_RPG_GUI/frame_backgrounds/skill_background.png";
    private const string BodyFontPath = "Assets/Fonts/Noto_Sans/static/NotoSans_SemiCondensed-SemiBold SDF.asset";
    private const string NumberFontPath = "Assets/Fonts/Noto_Sans/static/NotoSans-Bold SDF.asset";

    static PRD19_023_RewardMapPolishedPrefabBuilder()
    {
        EditorApplication.delayCall += QueueBuildAfterSharedBuilder;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 023 Reward Map Polished Prefabs")]
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

        SavePrefab(BuildRewardCardTemplate(), RewardCardPath);
        SavePrefab(BuildArmyPreviewUnitTemplate(), ArmyPreviewUnitPath);
        SavePrefab(BuildResultGainedPanelTemplate(), ResultGainedPanelPath);
        SavePrefab(BuildCommandButtonTemplate(), CommandButtonPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(RewardCardPath);
        AssetDatabase.ImportAsset(ArmyPreviewUnitPath);
        AssetDatabase.ImportAsset(ResultGainedPanelPath);
        AssetDatabase.ImportAsset(CommandButtonPath);

        SavePrefab(BuildMainPrefab(), MainPrefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD_19_023 Reward Map polished UI prefabs in Polished_1.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_023_RewardMap_Polished",
            null,
            new Vector2(1600f, 900f),
            ScreenBackgroundPath,
            new Color(0.14f, 0.10f, 0.07f, 1f),
            BigFramePath,
            new Color(0.42f, 0.33f, 0.28f, 0.90f),
            false);

        Image background = root.GetComponent<Image>();
        if (background != null)
        {
            background.raycastTarget = false;
        }

        GameObject vignette = CreateOverlay("Decor_ScreenVignette", root.transform, new Color(0.02f, 0.02f, 0.02f, 0.34f));
        vignette.SetActive(true);

        GameObject scriptOwner = CreateRect("Script_RewardMapScreenController", root.transform, new Vector2(100f, 100f));
        RewardMapScreenController controller = scriptOwner.AddComponent<RewardMapScreenController>();

        GameObject header = CreateDecoratedPanel(
            "Section_Header",
            root.transform,
            new Vector2(1528f, 92f),
            PanelBackgroundPath,
            new Color(0.21f, 0.14f, 0.09f, 0.98f),
            BigFramePath,
            new Color(0.50f, 0.37f, 0.24f, 0.95f),
            false);
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f));
        CreateInset("Inset_HeaderMode", header.transform, new Vector2(360f, 42f), new Vector2(530f, 0f), new Color(0.05f, 0.04f, 0.03f, 0.72f));
        AddText("Text_Title", header.transform, "REWARD MAP", 42, TextAlignmentOptions.MidlineLeft, new Vector2(520f, 48f), new Vector2(-482f, 0f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        AddText("Text_Subtitle", header.transform, "Win the road battle, preview the army outcome, then commit one reward.", 18, TextAlignmentOptions.MidlineLeft, new Vector2(620f, 28f), new Vector2(-160f, -24f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        AddText("Text_Mode", header.transform, "OFFLINE MODE", 18, TextAlignmentOptions.Midline, new Vector2(250f, 36f), new Vector2(530f, 0f), ImportantValueColor(), LoadNumberFont(), FontStyles.Normal);

        GameObject left = CreateDecoratedPanel(
            "Section_BattleResultGained",
            root.transform,
            new Vector2(328f, 628f),
            PanelBackgroundPath,
            new Color(0.13f, 0.10f, 0.07f, 0.98f),
            BigFramePath,
            new Color(0.38f, 0.28f, 0.19f, 0.90f),
            false);
        SetAnchored(left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(186f, 36f));
        AddText("Text_LeftHeader", left.transform, "BATTLE RESULT", 24, TextAlignmentOptions.MidlineLeft, new Vector2(260f, 32f), new Vector2(-8f, 278f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        AddText("Text_LeftHint", left.transform, "Immediate outcome and gained rewards", 14, TextAlignmentOptions.MidlineLeft, new Vector2(260f, 22f), new Vector2(-8f, 248f), MutedTextColor(), LoadBodyFont(), FontStyles.Normal);
        GameObject resultPanelObject = InstantiateNestedPrefab(ResultGainedPanelPath, "Panel_BattleResultGained", left.transform);
        SetAnchored(resultPanelObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f));
        RewardMapResultGainedPanelView resultPanel = resultPanelObject.GetComponent<RewardMapResultGainedPanelView>();

        GameObject rewardSection = CreateDecoratedPanel(
            "Section_RewardChoice",
            root.transform,
            new Vector2(912f, 628f),
            PanelBackgroundPath,
            new Color(0.14f, 0.10f, 0.07f, 0.98f),
            BigFramePath,
            new Color(0.41f, 0.31f, 0.21f, 0.90f),
            false);
        SetAnchored(rewardSection, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 36f));
        AddText("Text_ChoiceHeader", rewardSection.transform, "CHOOSE 1 OF 3", 26, TextAlignmentOptions.MidlineLeft, new Vector2(320f, 34f), new Vector2(-282f, 278f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        AddText("Text_ChoiceHint", rewardSection.transform, "Stabilize, Strengthen, Pivot", 16, TextAlignmentOptions.MidlineRight, new Vector2(360f, 26f), new Vector2(226f, 278f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject cardList = CreateRect("List_RewardCards", rewardSection.transform, new Vector2(860f, 544f));
        SetAnchored(cardList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f));
        HorizontalLayoutGroup cardLayout = cardList.AddComponent<HorizontalLayoutGroup>();
        cardLayout.spacing = 24f;
        cardLayout.padding = new RectOffset(8, 8, 0, 0);
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childControlWidth = false;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandWidth = false;
        cardLayout.childForceExpandHeight = false;

        List<RewardMapRewardCardView> rewardCardViews = new List<RewardMapRewardCardView>();
        string[] cardNames = { "Stabilize", "Strengthen", "Pivot" };
        for (int i = 0; i < 3; i++)
        {
            GameObject card = InstantiateNestedPrefab(RewardCardPath, "RewardCard_" + (i + 1).ToString("00") + "_" + cardNames[i], cardList.transform);
            LayoutElement layoutElement = card.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 268f;
            layoutElement.preferredHeight = 520f;
            rewardCardViews.Add(card.GetComponent<RewardMapRewardCardView>());
        }

        GameObject armySection = CreateDecoratedPanel(
            "Section_ArmyPreviewAfterReward",
            root.transform,
            new Vector2(1210f, 164f),
            PanelBackgroundPath,
            new Color(0.12f, 0.10f, 0.07f, 0.98f),
            BigFramePath,
            new Color(0.42f, 0.31f, 0.21f, 0.90f),
            false);
        SetAnchored(armySection, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-104f, 106f));
        AddText("Text_ArmyPreviewHeader", armySection.transform, "ARMY PREVIEW AFTER REWARD", 22, TextAlignmentOptions.MidlineLeft, new Vector2(420f, 30f), new Vector2(-360f, 56f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        AddText("Text_ArmyPreviewHint", armySection.transform, "Focused card updates this strip before confirmation.", 14, TextAlignmentOptions.MidlineRight, new Vector2(430f, 24f), new Vector2(338f, 56f), MutedTextColor(), LoadBodyFont(), FontStyles.Normal);
        GameObject armyList = CreateRect("List_ArmyPreviewUnits", armySection.transform, new Vector2(1140f, 108f));
        SetAnchored(armyList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f));
        HorizontalLayoutGroup armyLayout = armyList.AddComponent<HorizontalLayoutGroup>();
        armyLayout.spacing = 10f;
        armyLayout.padding = new RectOffset(0, 0, 0, 0);
        armyLayout.childAlignment = TextAnchor.MiddleCenter;
        armyLayout.childControlWidth = false;
        armyLayout.childControlHeight = false;
        armyLayout.childForceExpandWidth = false;
        armyLayout.childForceExpandHeight = false;

        List<RewardMapArmyPreviewUnitView> armyUnitViews = new List<RewardMapArmyPreviewUnitView>();
        for (int i = 0; i < 7; i++)
        {
            GameObject unit = InstantiateNestedPrefab(ArmyPreviewUnitPath, "ArmyPreviewUnit_" + (i + 1).ToString("00"), armyList.transform);
            LayoutElement layoutElement = unit.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 156f;
            layoutElement.preferredHeight = 116f;
            armyUnitViews.Add(unit.GetComponent<RewardMapArmyPreviewUnitView>());
        }

        GameObject right = CreateDecoratedPanel(
            "Section_RunSummary",
            root.transform,
            new Vector2(314f, 700f),
            PanelBackgroundPath,
            new Color(0.12f, 0.09f, 0.06f, 0.98f),
            BigFramePath,
            new Color(0.42f, 0.31f, 0.21f, 0.90f),
            false);
        SetAnchored(right, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-170f, -6f));
        AddText("Text_RightHeader", right.transform, "RUN SUMMARY", 24, TextAlignmentOptions.MidlineLeft, new Vector2(220f, 34f), new Vector2(-18f, 312f), TitleColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject walletInset = CreateInset("Inset_Wallet", right.transform, new Vector2(268f, 74f), new Vector2(0f, 238f), new Color(0.05f, 0.04f, 0.03f, 0.78f));
        AddText("Text_WalletLabel", walletInset.transform, "RUN GOLD", 14, TextAlignmentOptions.MidlineLeft, new Vector2(90f, 20f), new Vector2(-78f, 18f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI walletText = AddText("Text_Wallet", walletInset.transform, "360 RUN GOLD", 29, TextAlignmentOptions.MidlineLeft, new Vector2(220f, 34f), new Vector2(-14f, -8f), ImportantValueColor(), LoadNumberFont(), FontStyles.Normal);

        GameObject inventoryInset = CreateInset("Inset_Inventory", right.transform, new Vector2(268f, 80f), new Vector2(0f, 156f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        AddText("Text_InventoryLabel", inventoryInset.transform, "INVENTORY", 14, TextAlignmentOptions.MidlineLeft, new Vector2(100f, 20f), new Vector2(-72f, 20f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI inventoryText = AddText("Text_InventorySummary", inventoryInset.transform, "Inventory: 2 reward supplies", 15, TextAlignmentOptions.TopLeft, new Vector2(232f, 34f), new Vector2(0f, -12f), PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject focusedInset = CreateInset("Inset_FocusedReward", right.transform, new Vector2(268f, 246f), new Vector2(0f, 10f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        AddText("Text_FocusedHeader", focusedInset.transform, "FOCUSED REWARD", 16, TextAlignmentOptions.MidlineLeft, new Vector2(140f, 24f), new Vector2(-50f, 92f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI focusedTitle = AddText("Text_FocusedRewardTitle", focusedInset.transform, "Focused reward", 22, TextAlignmentOptions.MidlineLeft, new Vector2(220f, 30f), new Vector2(0f, 58f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI focusedPreview = AddText("Text_FocusedRewardPreview", focusedInset.transform, "Before -> After", 16, TextAlignmentOptions.TopLeft, new Vector2(228f, 98f), new Vector2(0f, -4f), PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        AddText("Text_FocusedHint", focusedInset.transform, "Preview first, then select.", 14, TextAlignmentOptions.BottomLeft, new Vector2(228f, 22f), new Vector2(0f, -94f), MutedTextColor(), LoadBodyFont(), FontStyles.Normal);

        RewardMapCommandButtonView selectButton = InstantiateNestedPrefab(CommandButtonPath, "Button_SelectFocusedReward", right.transform).GetComponent<RewardMapCommandButtonView>();
        SetAnchored(selectButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 122f));
        SetSize(selectButton.gameObject, new Vector2(244f, 68f));
        selectButton.Bind("Select Reward", true, true);

        RewardMapCommandButtonView continueButton = InstantiateNestedPrefab(CommandButtonPath, "Button_Continue", right.transform).GetComponent<RewardMapCommandButtonView>();
        SetAnchored(continueButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f));
        SetSize(continueButton.gameObject, new Vector2(228f, 60f));
        continueButton.Bind("Continue", true, false);

        if (selectButton.Button != null)
        {
            UnityEventTools.AddPersistentListener(selectButton.Button.onClick, controller.SelectFocusedReward);
        }

        if (continueButton.Button != null)
        {
            UnityEventTools.AddPersistentListener(continueButton.Button.onClick, controller.ContinueAfterReward);
        }

        GameObject statusInset = CreateInset("Inset_Status", root.transform, new Vector2(728f, 44f), new Vector2(-110f, -286f), new Color(0.05f, 0.04f, 0.03f, 0.70f));
        TextMeshProUGUI statusText = AddText("Text_Status", statusInset.transform, "Previewing selected reward.", 17, TextAlignmentOptions.Midline, new Vector2(688f, 28f), Vector2.zero, PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "resultGainedPanel", resultPanel);
        SetObjectArray(serialized, "rewardCards", rewardCardViews.ToArray());
        SetObjectArray(serialized, "armyPreviewUnits", armyUnitViews.ToArray());
        SetObject(serialized, "walletText", walletText);
        SetObject(serialized, "inventoryText", inventoryText);
        SetObject(serialized, "focusedRewardTitleText", focusedTitle);
        SetObject(serialized, "focusedRewardPreviewText", focusedPreview);
        SetObject(serialized, "statusText", statusText);
        SetObject(serialized, "selectCommandButton", selectButton);
        SetObject(serialized, "continueCommandButton", continueButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildRewardCardTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_023_RewardMap_Card_Polished",
            null,
            new Vector2(268f, 520f),
            PanelBackgroundPath,
            new Color(0.13f, 0.10f, 0.07f, 0.98f),
            MidFramePath,
            new Color(0.42f, 0.31f, 0.21f, 0.88f),
            true);
        Image rootImage = root.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.raycastTarget = true;
        }

        Button button = root.AddComponent<Button>();
        RewardMapRewardCardView view = root.AddComponent<RewardMapRewardCardView>();

        Image accent = CreatePanelImage("Accent_IntentionColor", root.transform, new Vector2(224f, 26f), new Vector2(0f, 220f), new Color(0.20f, 0.38f, 0.18f, 1f));
        GameObject familyInset = CreateInset("Inset_Family", root.transform, new Vector2(224f, 44f), new Vector2(0f, 176f), new Color(0.05f, 0.04f, 0.03f, 0.66f));
        TextMeshProUGUI intention = AddText("Text_Intention", root.transform, "STABILIZE", 22, TextAlignmentOptions.Midline, new Vector2(224f, 24f), new Vector2(0f, 220f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI family = AddText("Text_Family", familyInset.transform, "Recovery / Common", 14, TextAlignmentOptions.Midline, new Vector2(204f, 24f), Vector2.zero, SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI verb = AddText("Text_Verb", root.transform, "Revive", 32, TextAlignmentOptions.Midline, new Vector2(220f, 36f), new Vector2(0f, 132f), PrimaryTextColor(), LoadNumberFont(), FontStyles.Normal);
        TextMeshProUGUI title = AddText("Text_Title", root.transform, "Recover Losses", 23, TextAlignmentOptions.Midline, new Vector2(220f, 32f), new Vector2(0f, 96f), TitleColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject detailInset = CreateInset("Inset_Detail", root.transform, new Vector2(224f, 82f), new Vector2(0f, 40f), new Color(0.05f, 0.04f, 0.03f, 0.72f));
        TextMeshProUGUI detail = AddText("Text_Detail", detailInset.transform, "Restore part of the most damaged stack.", 16, TextAlignmentOptions.TopLeft, new Vector2(196f, 54f), new Vector2(0f, -4f), PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject beforeInset = CreateInset("Inset_Before", root.transform, new Vector2(224f, 72f), new Vector2(0f, -72f), new Color(0.05f, 0.04f, 0.03f, 0.70f));
        AddText("Text_BeforeLabel", beforeInset.transform, "BEFORE", 13, TextAlignmentOptions.MidlineLeft, new Vector2(70f, 18f), new Vector2(-64f, 18f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI before = AddText("Text_Before", beforeInset.transform, "Rusher x40 lost 12", 17, TextAlignmentOptions.TopLeft, new Vector2(190f, 28f), new Vector2(0f, -10f), PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);

        AddText("Text_Arrow", root.transform, "=>", 18, TextAlignmentOptions.Midline, new Vector2(120f, 18f), new Vector2(0f, -126f), ImportantValueColor(), LoadNumberFont(), FontStyles.Normal);

        GameObject afterInset = CreateInset("Inset_After", root.transform, new Vector2(224f, 72f), new Vector2(0f, -186f), new Color(0.05f, 0.04f, 0.03f, 0.74f));
        AddText("Text_AfterLabel", afterInset.transform, "AFTER PREVIEW", 13, TextAlignmentOptions.MidlineLeft, new Vector2(104f, 18f), new Vector2(-48f, 18f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI after = AddText("Text_After", afterInset.transform, "Rusher x48 lost 4", 17, TextAlignmentOptions.TopLeft, new Vector2(190f, 28f), new Vector2(0f, -10f), SuccessTextColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject legalInset = CreateInset("Inset_LegalState", root.transform, new Vector2(224f, 42f), new Vector2(0f, -236f), new Color(0.07f, 0.05f, 0.03f, 0.82f));
        TextMeshProUGUI legal = AddText("Text_LegalState", legalInset.transform, "Click to preview", 15, TextAlignmentOptions.Midline, new Vector2(196f, 24f), Vector2.zero, TitleColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject selectedState = CreateOverlay("State_Selected", root.transform, new Color(0.94f, 0.77f, 0.56f, 0.10f));
        AddStretchImage("State_SelectedFrame", selectedState.transform, MidFramePath, new Color(0.94f, 0.77f, 0.56f, 0.95f), false);
        selectedState.SetActive(false);

        GameObject disabledState = CreateOverlay("State_Disabled", root.transform, new Color(0.02f, 0.02f, 0.02f, 0.56f));
        AddStretchImage("State_DisabledFrame", disabledState.transform, MidFramePath, new Color(0.34f, 0.24f, 0.18f, 0.92f), false);
        disabledState.SetActive(false);

        Image frameImage = FindChildImage(root.transform, "Decor_Frame");

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "accentImage", accent);
        SetObject(serialized, "frameImage", frameImage);
        SetObject(serialized, "intentionText", intention);
        SetObject(serialized, "familyText", family);
        SetObject(serialized, "verbText", verb);
        SetObject(serialized, "titleText", title);
        SetObject(serialized, "detailText", detail);
        SetObject(serialized, "beforeText", before);
        SetObject(serialized, "afterText", after);
        SetObject(serialized, "legalText", legal);
        SetObject(serialized, "selectedState", selectedState);
        SetObject(serialized, "disabledState", disabledState);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildArmyPreviewUnitTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_023_RewardMap_ArmyPreviewUnit_Polished",
            null,
            new Vector2(156f, 116f),
            PanelBackgroundPath,
            new Color(0.13f, 0.10f, 0.07f, 0.98f),
            MidFramePath,
            new Color(0.38f, 0.28f, 0.19f, 0.82f),
            false);
        RewardMapArmyPreviewUnitView view = root.AddComponent<RewardMapArmyPreviewUnitView>();

        CreateInset("Inset_IconWell", root.transform, new Vector2(58f, 58f), new Vector2(-42f, 16f), new Color(0.05f, 0.04f, 0.03f, 0.82f));
        AddStretchImage("Decor_IconFrame", root.transform, IconFramePath, new Color(0.32f, 0.24f, 0.18f, 0.88f), false, new Vector2(58f, 58f), new Vector2(-42f, 16f));
        Image icon = CreatePanelImage("Icon_Unit", root.transform, new Vector2(44f, 44f), new Vector2(-42f, 16f), Color.white);
        TextMeshProUGUI name = AddText("Text_Name", root.transform, "Rusher", 15, TextAlignmentOptions.MidlineLeft, new Vector2(86f, 20f), new Vector2(28f, 32f), PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI tier = AddText("Text_Tier", root.transform, "Tier I / Lv 1", 11, TextAlignmentOptions.MidlineLeft, new Vector2(86f, 18f), new Vector2(28f, 14f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI amount = AddText("Text_Amount", root.transform, "x40 / lost 12", 13, TextAlignmentOptions.MidlineLeft, new Vector2(86f, 18f), new Vector2(28f, -6f), ImportantValueColor(), LoadNumberFont(), FontStyles.Normal);
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "1258 value", 11, TextAlignmentOptions.MidlineRight, new Vector2(134f, 18f), new Vector2(0f, -32f), TitleColor(), LoadNumberFont(), FontStyles.Normal);
        TextMeshProUGUI skills = AddText("Text_Skills", root.transform, "Chope / [Rush]", 10, TextAlignmentOptions.Midline, new Vector2(132f, 18f), new Vector2(0f, -48f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        GameObject affectedState = CreateOverlay("State_AffectedByFocusedReward", root.transform, new Color(0.94f, 0.77f, 0.56f, 0.12f));
        AddStretchImage("State_AffectedFrame", affectedState.transform, MidFramePath, new Color(0.94f, 0.77f, 0.56f, 0.94f), false);
        affectedState.SetActive(false);

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
        SetObject(serialized, "affectedState", affectedState);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildResultGainedPanelTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_023_RewardMap_ResultGainedPanel_Polished",
            null,
            new Vector2(288f, 552f),
            PanelBackgroundPath,
            new Color(0.10f, 0.08f, 0.06f, 0.98f),
            MidFramePath,
            new Color(0.38f, 0.28f, 0.19f, 0.82f),
            false);
        RewardMapResultGainedPanelView view = root.AddComponent<RewardMapResultGainedPanelView>();

        GameObject resultInset = CreateInset("Inset_Result", root.transform, new Vector2(232f, 104f), new Vector2(0f, 178f), new Color(0.05f, 0.04f, 0.03f, 0.80f));
        AddText("Text_Header", root.transform, "BATTLE RESULT", 23, TextAlignmentOptions.MidlineLeft, new Vector2(220f, 30f), new Vector2(-6f, 240f), TitleColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI result = AddText("Text_Result", resultInset.transform, "Victory", 32, TextAlignmentOptions.MidlineLeft, new Vector2(180f, 36f), new Vector2(-24f, 16f), SuccessTextColor(), LoadNumberFont(), FontStyles.Normal);
        TextMeshProUGUI battle = AddText("Text_Battle", resultInset.transform, "Stage 2 - Road Battle", 16, TextAlignmentOptions.MidlineLeft, new Vector2(196f, 22f), new Vector2(-16f, -18f), PrimaryTextColor(), LoadBodyFont(), FontStyles.Normal);

        GameObject lossesInset = CreateInset("Inset_Losses", root.transform, new Vector2(232f, 64f), new Vector2(0f, 72f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        AddText("Text_LossesLabel", lossesInset.transform, "LOSSES", 13, TextAlignmentOptions.MidlineLeft, new Vector2(80f, 18f), new Vector2(-62f, 16f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI losses = AddText("Text_Losses", lossesInset.transform, "Losses: 12", 18, TextAlignmentOptions.MidlineLeft, new Vector2(160f, 24f), new Vector2(-20f, -8f), DangerTextColor(), LoadNumberFont(), FontStyles.Normal);

        GameObject gainedInset = CreateInset("Inset_Gained", root.transform, new Vector2(232f, 114f), new Vector2(0f, -30f), new Color(0.05f, 0.04f, 0.03f, 0.76f));
        AddText("Text_GainedLabel", gainedInset.transform, "GAINED", 13, TextAlignmentOptions.MidlineLeft, new Vector2(78f, 18f), new Vector2(-64f, 36f), SecondaryTextColor(), LoadBodyFont(), FontStyles.Normal);
        TextMeshProUGUI gained = AddText("Text_Gained", gainedInset.transform, "Gained: 120 RUN GOLD", 18, TextAlignmentOptions.TopLeft, new Vector2(192f, 44f), new Vector2(0f, 6f), ImportantValueColor(), LoadNumberFont(), FontStyles.Normal);

        AddText("Text_Mode", root.transform, "Offline reward authority\nLocal adapter preview/apply", 13, TextAlignmentOptions.BottomLeft, new Vector2(220f, 38f), new Vector2(0f, -216f), MutedTextColor(), LoadBodyFont(), FontStyles.Normal);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "resultText", result);
        SetObject(serialized, "battleText", battle);
        SetObject(serialized, "lossesText", losses);
        SetObject(serialized, "gainedText", gained);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreateDecoratedPanel(
            "PRD_19_023_RewardMap_CommandButton_Polished",
            null,
            new Vector2(236f, 64f),
            ButtonPrimaryPath,
            new Color(0.58f, 0.38f, 0.19f, 1f),
            ButtonFramePath,
            new Color(0.62f, 0.46f, 0.24f, 0.96f),
            true);
        Image background = root.GetComponent<Image>();
        if (background != null)
        {
            background.raycastTarget = true;
        }

        Button button = root.AddComponent<Button>();
        RewardMapCommandButtonView view = root.AddComponent<RewardMapCommandButtonView>();
        TextMeshProUGUI label = AddText("Text_Label", root.transform, "COMMAND", 20, TextAlignmentOptions.Midline, new Vector2(196f, 30f), Vector2.zero, TitleColor(), LoadBodyFont(), FontStyles.Normal);
        Image frame = FindChildImage(root.transform, "Decor_Frame");

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "labelText", label);
        SetObject(serialized, "backgroundImage", background);
        SetObject(serialized, "frameImage", frame);
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
        AddStretchImage("Decor_InsetFrame", go.transform, MidFramePath, new Color(0.24f, 0.18f, 0.12f, 0.72f), false);
        return go;
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
        FontStyles fontStyle)
    {
        GameObject go = CreateRect(name, parent, size);
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontSizeMin = Mathf.Max(8, fontSize - 4);
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

    private static Image FindChildImage(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child == null ? null : child.GetComponent<Image>();
    }

    private static TMP_FontAsset LoadBodyFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
    }

    private static TMP_FontAsset LoadNumberFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NumberFontPath);
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

    private static Color DangerTextColor()
    {
        return new Color(0.90f, 0.48f, 0.42f, 1f);
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
