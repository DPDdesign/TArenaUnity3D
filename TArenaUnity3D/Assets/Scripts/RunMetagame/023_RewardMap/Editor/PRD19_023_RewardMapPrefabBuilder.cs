using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_023_RewardMapPrefabBuilder
{
    private const string MainPrefabPath = "Assets/Resources/UI/PRD_19/023_RewardMap/PRD_19_023_RewardMap.prefab";
    private const string TemplateFolder = "Assets/Resources/UI/PRD_19/023_RewardMap/Prefabs";
    private const string RewardCardPath = TemplateFolder + "/PRD_19_023_RewardMap_Card.prefab";
    private const string ArmyPreviewUnitPath = TemplateFolder + "/PRD_19_023_RewardMap_ArmyPreviewUnit.prefab";
    private const string ResultGainedPanelPath = TemplateFolder + "/PRD_19_023_RewardMap_ResultGainedPanel.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD_19_023_RewardMap_CommandButton.prefab";
    private const string BuildSessionKey = "TArena.PRD19_023.RewardMapPrefabBuilder.NestedPrefabsBuilt";

    static PRD19_023_RewardMapPrefabBuilder()
    {
        EditorApplication.delayCall += QueueBuildAfterSharedBuilder;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 023 Reward Map Prefabs")]
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
        Debug.Log("Rebuilt PRD_19_023 Reward Map UI prefabs with nested task-specific prefabs and reward service wiring.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreateRect("PRD_19_023_RewardMap", null, new Vector2(1600f, 900f));
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.09f, 0.07f, 0.05f, 1f);
        background.raycastTarget = false;

        GameObject scriptOwner = CreateRect("Script_RewardMapScreenController", root.transform, new Vector2(100f, 100f));
        RewardMapScreenController controller = scriptOwner.AddComponent<RewardMapScreenController>();

        GameObject header = CreatePanel("Section_Header", root.transform, new Vector2(1540f, 70f), new Color(0.18f, 0.12f, 0.07f, 0.96f));
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f));
        AddText("Text_Title", header.transform, "CHOOSE REWARD", 38, TextAlignmentOptions.Midline, new Vector2(680f, 54f), Vector2.zero, new Color(0.96f, 0.84f, 0.56f, 1f));
        AddText("Text_Subtitle", header.transform, "Choose 1 of 3 after the road battle", 18, TextAlignmentOptions.MidlineRight, new Vector2(430f, 40f), new Vector2(500f, 0f), new Color(0.82f, 0.75f, 0.63f, 1f));

        GameObject left = CreatePanel("Section_BattleResultGained", root.transform, new Vector2(320f, 590f), new Color(0.13f, 0.11f, 0.08f, 0.96f));
        SetAnchored(left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 70f));
        GameObject resultPanelObject = InstantiateNestedPrefab(ResultGainedPanelPath, "Panel_BattleResultGained", left.transform);
        RewardMapResultGainedPanelView resultPanel = resultPanelObject.GetComponent<RewardMapResultGainedPanelView>();

        GameObject rewardSection = CreatePanel("Section_RewardChoice", root.transform, new Vector2(960f, 590f), new Color(0.17f, 0.13f, 0.09f, 0.94f));
        SetAnchored(rewardSection, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, 70f));
        AddText("Text_ChoiceHeader", rewardSection.transform, "Reward Cards", 24, TextAlignmentOptions.MidlineLeft, new Vector2(380f, 36f), new Vector2(-280f, 260f), Color.white);
        AddText("Text_ChoiceHint", rewardSection.transform, "Choose 1 of 3", 15, TextAlignmentOptions.MidlineRight, new Vector2(520f, 36f), new Vector2(220f, 260f), new Color(0.80f, 0.73f, 0.61f, 1f));

        GameObject cardList = CreateRect("List_RewardCards", rewardSection.transform, new Vector2(920f, 520f));
        SetAnchored(cardList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f));
        HorizontalLayoutGroup cardLayout = cardList.AddComponent<HorizontalLayoutGroup>();
        cardLayout.spacing = 22f;
        cardLayout.padding = new RectOffset(12, 12, 0, 0);
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
            layoutElement.preferredWidth = 280f;
            layoutElement.preferredHeight = 510f;
            rewardCardViews.Add(card.GetComponent<RewardMapRewardCardView>());
        }

        GameObject armySection = CreatePanel("Section_ArmyPreviewAfterReward", root.transform, new Vector2(1180f, 160f), new Color(0.13f, 0.11f, 0.08f, 0.96f));
        SetAnchored(armySection, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 90f));
        AddText("Text_ArmyPreviewHeader", armySection.transform, "Your Army Preview After Reward", 22, TextAlignmentOptions.Midline, new Vector2(520f, 30f), new Vector2(0f, 62f), new Color(0.96f, 0.84f, 0.56f, 1f));
        GameObject armyList = CreateRect("List_ArmyPreviewUnits", armySection.transform, new Vector2(1120f, 118f));
        SetAnchored(armyList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f));
        HorizontalLayoutGroup armyLayout = armyList.AddComponent<HorizontalLayoutGroup>();
        armyLayout.spacing = 10f;
        armyLayout.padding = new RectOffset(10, 10, 0, 0);
        armyLayout.childAlignment = TextAnchor.MiddleCenter;
        armyLayout.childControlWidth = false;
        armyLayout.childControlHeight = false;
        armyLayout.childForceExpandWidth = false;
        armyLayout.childForceExpandHeight = false;

        GameObject right = CreatePanel("Section_WalletInventorySummary", root.transform, new Vector2(280f, 650f), new Color(0.13f, 0.10f, 0.07f, 0.96f));
        SetAnchored(right, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, -45f));
        AddText("Text_RightHeader", right.transform, "Run Summary", 23, TextAlignmentOptions.Midline, new Vector2(240f, 40f), new Vector2(0f, 285f), new Color(0.96f, 0.84f, 0.56f, 1f));
        TextMeshProUGUI walletText = AddText("Text_Wallet", right.transform, "360 RUN GOLD", 26, TextAlignmentOptions.Midline, new Vector2(240f, 42f), new Vector2(0f, 235f), new Color(1f, 0.82f, 0.30f, 1f));
        TextMeshProUGUI inventoryText = AddText("Text_InventorySummary", right.transform, "Inventory: 2 reward supplies", 15, TextAlignmentOptions.TopLeft, new Vector2(238f, 60f), new Vector2(0f, 178f), new Color(0.84f, 0.78f, 0.66f, 1f));
        TextMeshProUGUI focusedTitle = AddText("Text_FocusedRewardTitle", right.transform, "Focused reward", 20, TextAlignmentOptions.MidlineLeft, new Vector2(238f, 34f), new Vector2(0f, 105f), Color.white);
        TextMeshProUGUI focusedPreview = AddText("Text_FocusedRewardPreview", right.transform, "Before -> After", 15, TextAlignmentOptions.TopLeft, new Vector2(238f, 155f), new Vector2(0f, 12f), new Color(0.82f, 0.76f, 0.64f, 1f));

        RewardMapCommandButtonView selectButton = InstantiateNestedPrefab(CommandButtonPath, "Button_SelectFocusedReward", right.transform).GetComponent<RewardMapCommandButtonView>();
        SetAnchored(selectButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f));
        selectButton.Bind("Select", true, true);
        RewardMapCommandButtonView continueButton = InstantiateNestedPrefab(CommandButtonPath, "Button_Continue", right.transform).GetComponent<RewardMapCommandButtonView>();
        SetAnchored(continueButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f));
        continueButton.Bind("Continue", true, false);
        if (selectButton.Button != null)
        {
            UnityEventTools.AddPersistentListener(selectButton.Button.onClick, controller.SelectFocusedReward);
        }

        if (continueButton.Button != null)
        {
            UnityEventTools.AddPersistentListener(continueButton.Button.onClick, controller.ContinueAfterReward);
        }

        TextMeshProUGUI statusText = AddText("Text_Status", root.transform, "Previewing selected reward.", 17, TextAlignmentOptions.Midline, new Vector2(760f, 32f), new Vector2(-80f, -252f), new Color(0.86f, 0.80f, 0.68f, 1f));

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "resultGainedPanel", resultPanel);
        SetObjectArray(serialized, "rewardCards", rewardCardViews.ToArray());
        SetObject(serialized, "changedStackPreviewRowsParent", armyList.transform);
        SetObject(serialized, "changedStackPreviewRowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ArmyPreviewUnitPath));
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
        GameObject root = CreatePanel("PRD_19_023_RewardMap_Card", null, new Vector2(280f, 510f), new Color(0.28f, 0.21f, 0.13f, 0.98f));
        Image rootImage = root.GetComponent<Image>();
        rootImage.raycastTarget = true;
        Button button = root.AddComponent<Button>();
        RewardMapRewardCardView view = root.AddComponent<RewardMapRewardCardView>();

        Image accent = CreateImage("Accent_IntentionColor", root.transform, new Vector2(260f, 44f), new Vector2(0f, 220f), new Color(0.18f, 0.45f, 0.20f, 1f));
        TextMeshProUGUI intention = AddText("Text_Intention", root.transform, "STABILIZE", 22, TextAlignmentOptions.Midline, new Vector2(250f, 38f), new Vector2(0f, 220f), new Color(0.96f, 0.84f, 0.56f, 1f));
        TextMeshProUGUI family = AddText("Text_Family", root.transform, "Recovery / Common", 15, TextAlignmentOptions.Midline, new Vector2(250f, 28f), new Vector2(0f, 178f), new Color(0.84f, 0.78f, 0.66f, 1f));
        TextMeshProUGUI verb = AddText("Text_Verb", root.transform, "Revive", 30, TextAlignmentOptions.Midline, new Vector2(250f, 44f), new Vector2(0f, 138f), Color.white);
        TextMeshProUGUI title = AddText("Text_Title", root.transform, "Recover Losses", 22, TextAlignmentOptions.Midline, new Vector2(250f, 34f), new Vector2(0f, 98f), new Color(0.98f, 0.90f, 0.70f, 1f));
        TextMeshProUGUI detail = AddText("Text_Detail", root.transform, "Restore part of the most damaged stack.", 15, TextAlignmentOptions.TopLeft, new Vector2(242f, 78f), new Vector2(0f, 42f), new Color(0.86f, 0.80f, 0.68f, 1f));
        TextMeshProUGUI legal = AddText("Text_LegalState", root.transform, "Click to preview", 15, TextAlignmentOptions.Midline, new Vector2(238f, 34f), new Vector2(0f, -220f), new Color(0.96f, 0.84f, 0.56f, 1f));
        GameObject selectedState = CreateOverlay("State_Selected", root.transform, new Color(1f, 0.82f, 0.26f, 0.18f));
        GameObject disabledState = CreateOverlay("State_Disabled", root.transform, new Color(0f, 0f, 0f, 0.46f));
        selectedState.SetActive(false);
        disabledState.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "accentImage", accent);
        SetObject(serialized, "intentionText", intention);
        SetObject(serialized, "familyText", family);
        SetObject(serialized, "verbText", verb);
        SetObject(serialized, "titleText", title);
        SetObject(serialized, "detailText", detail);
        SetObject(serialized, "legalText", legal);
        SetObject(serialized, "selectedState", selectedState);
        SetObject(serialized, "disabledState", disabledState);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildArmyPreviewUnitTemplate()
    {
        GameObject root = CreatePanel("PRD_19_023_RewardMap_ArmyPreviewUnit", null, new Vector2(148f, 112f), new Color(0.17f, 0.13f, 0.09f, 0.96f));

        Image icon = CreateImage("Icon_Unit", root.transform, new Vector2(54f, 54f), new Vector2(-38f, 20f), new Color(0.30f, 0.24f, 0.16f, 1f));
        TextMeshProUGUI name = AddText("Text_Name", root.transform, "Rusher", 15, TextAlignmentOptions.MidlineLeft, new Vector2(78f, 22f), new Vector2(34f, 36f), Color.white);
        TextMeshProUGUI tier = AddText("Text_Tier", root.transform, "Tier I / Lv 1", 10, TextAlignmentOptions.MidlineLeft, new Vector2(78f, 18f), new Vector2(34f, 18f), new Color(0.76f, 0.70f, 0.60f, 1f));
        TextMeshProUGUI amount = AddText("Text_Amount", root.transform, "x40 / lost 12", 11, TextAlignmentOptions.MidlineLeft, new Vector2(78f, 18f), new Vector2(34f, 1f), new Color(0.96f, 0.84f, 0.56f, 1f));
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "1258 value", 10, TextAlignmentOptions.MidlineRight, new Vector2(130f, 18f), new Vector2(0f, -28f), new Color(1f, 0.82f, 0.30f, 1f));
        TextMeshProUGUI skills = AddText("Text_Skills", root.transform, "Chope / [Rush]", 9, TextAlignmentOptions.Midline, new Vector2(132f, 18f), new Vector2(0f, -46f), new Color(0.70f, 0.86f, 1f, 1f));
        GameObject affectedState = CreateOverlay("State_AffectedByFocusedReward", root.transform, new Color(0.62f, 0.94f, 0.40f, 0.18f));
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

        return root;
    }

    private static GameObject BuildResultGainedPanelTemplate()
    {
        GameObject root = CreatePanel("PRD_19_023_RewardMap_ResultGainedPanel", null, new Vector2(300f, 570f), new Color(0.12f, 0.10f, 0.07f, 0.98f));
        RewardMapResultGainedPanelView view = root.AddComponent<RewardMapResultGainedPanelView>();

        AddText("Text_Header", root.transform, "Battle Result", 24, TextAlignmentOptions.Midline, new Vector2(250f, 44f), new Vector2(0f, 238f), new Color(0.96f, 0.84f, 0.56f, 1f));
        TextMeshProUGUI result = AddText("Text_Result", root.transform, "Victory", 30, TextAlignmentOptions.Midline, new Vector2(250f, 52f), new Vector2(0f, 176f), new Color(0.62f, 1f, 0.44f, 1f));
        TextMeshProUGUI battle = AddText("Text_Battle", root.transform, "Stage 2 - Road Battle", 16, TextAlignmentOptions.Midline, new Vector2(250f, 32f), new Vector2(0f, 132f), new Color(0.84f, 0.78f, 0.66f, 1f));
        TextMeshProUGUI losses = AddText("Text_Losses", root.transform, "Losses: 12", 18, TextAlignmentOptions.MidlineLeft, new Vector2(238f, 36f), new Vector2(0f, 50f), new Color(1f, 0.46f, 0.34f, 1f));
        TextMeshProUGUI gained = AddText("Text_Gained", root.transform, "Gained: 120 RUN GOLD", 18, TextAlignmentOptions.TopLeft, new Vector2(238f, 110f), new Vector2(0f, -48f), new Color(1f, 0.82f, 0.30f, 1f));
        AddText("Text_Mode", root.transform, "Offline reward authority:\nLocal adapter preview/apply", 13, TextAlignmentOptions.BottomLeft, new Vector2(238f, 86f), new Vector2(0f, -216f), new Color(0.70f, 0.82f, 0.94f, 1f));

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
        GameObject root = CreatePanel("PRD_19_023_RewardMap_CommandButton", null, new Vector2(210f, 54f), new Color(0.58f, 0.34f, 0.12f, 1f));
        Image background = root.GetComponent<Image>();
        background.raycastTarget = true;
        Button button = root.AddComponent<Button>();
        RewardMapCommandButtonView view = root.AddComponent<RewardMapCommandButtonView>();
        TextMeshProUGUI label = AddText("Text_Label", root.transform, "Command", 18, TextAlignmentOptions.Midline, new Vector2(190f, 42f), Vector2.zero, Color.white);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "labelText", label);
        SetObject(serialized, "backgroundImage", background);
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

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject go = CreateRect(name, parent, size);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreatePanel(name, parent, size, color);
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
        label.fontSizeMin = Mathf.Max(8, fontSize - 5);
        label.fontSizeMax = fontSize;
        label.enableAutoSizing = true;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private static GameObject CreateOverlay(string name, Transform parent, Color color)
    {
        GameObject go = CreatePanel(name, parent, Vector2.zero, color);
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
