#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class RunBattleMockupPrefabBuilder
{
    private const string FinalPrefabPath = "Assets/Resources/UI/PRD_19/PRD_19_22_RunBattleMockup.prefab";
    private const string GeneratedFolder = "Assets/Prefabs/GeneratedUI/RunMetagame/RunBattle";
    private const string BattleCardPrefabPath = GeneratedFolder + "/PRD19_22_BattleNodeCard.prefab";
    private const string StackRowPrefabPath = GeneratedFolder + "/PRD19_22_ArmyStackRow.prefab";
    private const string CommandButtonPrefabPath = GeneratedFolder + "/PRD19_22_CommandButton.prefab";

    [InitializeOnLoadMethod]
    private static void BuildMissingRunBattleMockupAfterReload()
    {
        EditorApplication.delayCall += delegate
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FinalPrefabPath);
            if (existingPrefab != null && HasCommandButtonCallbacks(existingPrefab))
            {
                return;
            }

            BuildRunBattleMockup();
        };
    }

    [MenuItem("TArena/PRD019/Build Run Battle Mockup")]
    public static void BuildRunBattleMockup()
    {
        EnsureFolders();

        GameObject battleCardPrefab = SavePrefab(BattleCardPrefabPath, CreateBattleCardTemplate());
        GameObject stackRowPrefab = SavePrefab(StackRowPrefabPath, CreateStackRowTemplate());
        SavePrefab(CommandButtonPrefabPath, CreateCommandButtonTemplate("Command"));
        GameObject screen = CreateScreen(battleCardPrefab, stackRowPrefab);

        SavePrefab(FinalPrefabPath, screen);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built Run Battle mockup prefab: " + FinalPrefabPath);
    }

    private static GameObject CreateScreen(
        GameObject battleCardPrefab,
        GameObject stackRowPrefab)
    {
        GameObject root = UiObject("Mock_RunBattleBridge", null, new Vector2(1600f, 900f));
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.08f, 0.09f, 0.11f, 1f);
        background.raycastTarget = false;

        GameObject scriptOwner = UiObject("Script_RunBattleMockupScreenController", root.transform, Vector2.zero);
        RunBattleMockupScreenController controller = scriptOwner.AddComponent<RunBattleMockupScreenController>();

        GameObject header = Panel("Section_Header", root.transform, new Vector2(1560f, 90f), new Color(0.15f, 0.11f, 0.08f, 1f));
        SetAnchor(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f));
        TextMeshProUGUI headerText = Text("Label_Header", header.transform, "Run Battle Bridge - Border Clash", 34, TextAlignmentOptions.Left, new Vector2(1180f, 48f));
        SetAnchor(headerText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f));
        Text("Label_Mode", header.transform, "OFFLINE MODE / CURRENT BATTLE ADAPTER", 18, TextAlignmentOptions.Right, new Vector2(330f, 32f));

        GameObject body = UiObject("Section_Body", root.transform, new Vector2(1560f, 650f));
        SetAnchor(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f));
        HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 16f;
        bodyLayout.padding = new RectOffset(0, 0, 0, 0);
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        GameObject launchSection = Panel("Section_LaunchContext", body.transform, new Vector2(430f, 650f), new Color(0.13f, 0.13f, 0.16f, 1f));
        AddLayoutElement(launchSection, 430f, 650f);
        AddVerticalLayout(launchSection, 12, 12, 12);
        Text("Label_LaunchTitle", launchSection.transform, "Launch Context", 24, TextAlignmentOptions.Left, new Vector2(390f, 34f));
        GameObject battleList = UiObject("List_BattleNodes", launchSection.transform, new Vector2(390f, 380f));
        AddVerticalLayout(battleList, 8, 0, 0);
        RunBattleContextCardView[] battleCards = new RunBattleContextCardView[3];
        for (int i = 0; i < battleCards.Length; i++)
        {
            GameObject instance = InstantiatePrefab(battleCardPrefab, battleList.transform, "BattleNodeCard_0" + (i + 1));
            battleCards[i] = instance.GetComponent<RunBattleContextCardView>();
        }

        GameObject payloadSection = Panel("Section_Payload", body.transform, new Vector2(540f, 650f), new Color(0.1f, 0.12f, 0.15f, 1f));
        AddLayoutElement(payloadSection, 540f, 650f);
        AddVerticalLayout(payloadSection, 12, 12, 12);
        Text("Label_PayloadTitle", payloadSection.transform, "Payload And Transition", 24, TextAlignmentOptions.Left, new Vector2(500f, 34f));
        TextMeshProUGUI launchPayloadText = Text("Text_LaunchPayload", payloadSection.transform, "runId / routeNodeId / encounterId / currentArmySnapshotId", 18, TextAlignmentOptions.Left, new Vector2(500f, 170f));
        TextMeshProUGUI completionPayloadText = Text("Text_CompletionPayload", payloadSection.transform, "Prepare battle to create a runBattleId.", 18, TextAlignmentOptions.Left, new Vector2(500f, 150f));
        TextMeshProUGUI nextScreenText = Text("Text_NextScreen", payloadSection.transform, "Next screen waits for completion.", 22, TextAlignmentOptions.Left, new Vector2(500f, 64f));
        TextMeshProUGUI messageText = Text("Text_RuntimeMessage", payloadSection.transform, "Select an encounter and prepare battle.", 18, TextAlignmentOptions.Left, new Vector2(500f, 74f));

        GameObject armySection = Panel("Section_ArmySnapshots", body.transform, new Vector2(560f, 650f), new Color(0.13f, 0.11f, 0.1f, 1f));
        AddLayoutElement(armySection, 560f, 650f);
        AddVerticalLayout(armySection, 10, 12, 12);
        Text("Label_ArmyTitle", armySection.transform, "Army Snapshots", 24, TextAlignmentOptions.Left, new Vector2(520f, 34f));
        Text("Label_CurrentArmy", armySection.transform, "Current Run Army", 18, TextAlignmentOptions.Left, new Vector2(520f, 26f));
        GameObject currentList = UiObject("List_CurrentArmyRows", armySection.transform, new Vector2(520f, 230f));
        AddVerticalLayout(currentList, 6, 0, 0);
        RunBattleStackRowView[] currentRows = CreateStackRows(stackRowPrefab, currentList.transform, "CurrentArmy_Row_");
        Text("Label_AfterBattleArmy", armySection.transform, "After Battle Preview", 18, TextAlignmentOptions.Left, new Vector2(520f, 26f));
        GameObject afterList = UiObject("List_AfterBattleRows", armySection.transform, new Vector2(520f, 230f));
        AddVerticalLayout(afterList, 6, 0, 0);
        RunBattleStackRowView[] afterRows = CreateStackRows(stackRowPrefab, afterList.transform, "AfterBattle_Row_");

        GameObject commands = Panel("Section_Commands", root.transform, new Vector2(1560f, 110f), new Color(0.12f, 0.12f, 0.13f, 1f));
        SetAnchor(commands, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f));
        HorizontalLayoutGroup commandLayout = commands.AddComponent<HorizontalLayoutGroup>();
        commandLayout.spacing = 16f;
        commandLayout.padding = new RectOffset(18, 18, 18, 18);
        commandLayout.childForceExpandWidth = false;
        Button launchButton = CreateLocalCommandButton(commands.transform, "Button_PrepareBattle", "Prepare Battle");
        Button completeWinButton = CreateLocalCommandButton(commands.transform, "Button_CompleteWin", "Complete Win");
        Button completeLossButton = CreateLocalCommandButton(commands.transform, "Button_CompleteLoss", "Complete Loss");

        controller.ConfigureMockup(
            battleCards,
            currentRows,
            afterRows,
            headerText,
            launchPayloadText,
            completionPayloadText,
            nextScreenText,
            messageText,
            launchButton,
            completeWinButton,
            completeLossButton);

        WireCommandButtonCallbacks(controller, launchButton, completeWinButton, completeLossButton);

        return root;
    }

    private static bool HasCommandButtonCallbacks(GameObject prefab)
    {
        bool hasPrepare = false;
        bool hasWin = false;
        bool hasLoss = false;
        Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.onClick.GetPersistentEventCount() == 0)
            {
                continue;
            }

            if (button.name == "Button_PrepareBattle")
            {
                hasPrepare = true;
            }
            else if (button.name == "Button_CompleteWin")
            {
                hasWin = true;
            }
            else if (button.name == "Button_CompleteLoss")
            {
                hasLoss = true;
            }
        }

        return hasPrepare && hasWin && hasLoss;
    }

    private static void WireCommandButtonCallbacks(
        RunBattleMockupScreenController controller,
        Button launchButton,
        Button completeWinButton,
        Button completeLossButton)
    {
        UnityEventTools.AddPersistentListener(launchButton.onClick, controller.PrepareSelectedBattle);
        UnityEventTools.AddPersistentListener(completeWinButton.onClick, controller.CompleteAsWin);
        UnityEventTools.AddPersistentListener(completeLossButton.onClick, controller.CompleteAsLoss);
    }

    private static GameObject CreateBattleCardTemplate()
    {
        GameObject root = Panel("PRD19_22_BattleNodeCard", null, new Vector2(390f, 112f), new Color(0.18f, 0.17f, 0.14f, 1f));
        Button button = root.AddComponent<Button>();
        Image image = root.GetComponent<Image>();
        image.raycastTarget = true;
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.17f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.26f, 0.22f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        AddVerticalLayout(root, 4, 10, 8);

        GameObject selectedState = Panel("State_Selected", root.transform, new Vector2(360f, 8f), new Color(0.85f, 0.62f, 0.24f, 1f));
        TextMeshProUGUI title = Text("Text_Title", root.transform, "Border Clash", 20, TextAlignmentOptions.Left, new Vector2(350f, 24f));
        TextMeshProUGUI type = Text("Text_Type", root.transform, "Battle", 15, TextAlignmentOptions.Left, new Vector2(350f, 18f));
        TextMeshProUGUI risk = Text("Text_Risk", root.transform, "Risk: Low", 15, TextAlignmentOptions.Left, new Vector2(350f, 18f));
        TextMeshProUGUI encounter = Text("Text_Encounter", root.transform, "Encounter: enc-iron-border-clash", 14, TextAlignmentOptions.Left, new Vector2(350f, 18f));
        TextMeshProUGUI goal = Text("Text_Goal", root.transform, "Goal: TryToWin", 14, TextAlignmentOptions.Left, new Vector2(350f, 18f));

        RunBattleContextCardView view = root.AddComponent<RunBattleContextCardView>();
        view.Configure(button, title, type, risk, encounter, goal, selectedState);
        return root;
    }

    private static GameObject CreateStackRowTemplate()
    {
        GameObject root = Panel("PRD19_22_ArmyStackRow", null, new Vector2(520f, 50f), new Color(0.16f, 0.15f, 0.13f, 1f));
        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject iconObject = Panel("Icon_Unit", root.transform, new Vector2(40f, 40f), new Color(0.35f, 0.31f, 0.24f, 1f));
        AddLayoutElement(iconObject, 40f, 40f);
        Image icon = iconObject.GetComponent<Image>();
        TextMeshProUGUI name = Text("Text_Name", root.transform, "Rusher", 16, TextAlignmentOptions.Left, new Vector2(120f, 34f));
        TextMeshProUGUI amount = Text("Text_Amount", root.transform, "x28 / Tier I / Level 1", 14, TextAlignmentOptions.Left, new Vector2(140f, 34f));
        TextMeshProUGUI loss = Text("Text_Loss", root.transform, "Lost 0", 14, TextAlignmentOptions.Left, new Vector2(70f, 34f));
        TextMeshProUGUI value = Text("Text_Value", root.transform, "868 value", 14, TextAlignmentOptions.Left, new Vector2(80f, 34f));
        TextMeshProUGUI skills = Text("Text_Skills", root.transform, "Chope / [Rush]", 13, TextAlignmentOptions.Left, new Vector2(140f, 34f));

        RunBattleStackRowView view = root.AddComponent<RunBattleStackRowView>();
        view.Configure(icon, name, amount, loss, value, skills);
        return root;
    }

    private static GameObject CreateCommandButtonTemplate(string label)
    {
        GameObject root = Panel("PRD19_22_CommandButton", null, new Vector2(240f, 64f), new Color(0.22f, 0.18f, 0.12f, 1f));
        Button button = root.AddComponent<Button>();
        Image image = root.GetComponent<Image>();
        image.raycastTarget = true;
        button.targetGraphic = image;
        TextMeshProUGUI text = Text("Text_Label", root.transform, label, 20, TextAlignmentOptions.Center, new Vector2(220f, 44f));
        SetAnchor(text.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        AddLayoutElement(root, 240f, 64f);
        return root;
    }

    private static RunBattleStackRowView[] CreateStackRows(GameObject prefab, Transform parent, string prefix)
    {
        RunBattleStackRowView[] rows = new RunBattleStackRowView[4];
        for (int i = 0; i < rows.Length; i++)
        {
            GameObject instance = InstantiatePrefab(prefab, parent, prefix + "0" + (i + 1));
            rows[i] = instance.GetComponent<RunBattleStackRowView>();
        }

        return rows;
    }

    private static Button CreateLocalCommandButton(Transform parent, string name, string label)
    {
        GameObject instance = CreateCommandButtonTemplate(label);
        instance.transform.SetParent(parent, false);
        instance.name = name;
        return instance.GetComponent<Button>();
    }

    private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab, parent);
        }

        instance.name = name;
        return instance;
    }

    private static GameObject SavePrefab(string path, GameObject root)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject Panel(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject root = UiObject(name, parent, size);
        Image image = root.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return root;
    }

    private static TextMeshProUGUI Text(
        string name,
        Transform parent,
        string value,
        int size,
        TextAlignmentOptions alignment,
        Vector2 rectSize)
    {
        GameObject root = UiObject(name, parent, rectSize);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.9f, 0.82f, 1f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        AddLayoutElement(root, rectSize.x, rectSize.y);
        return text;
    }

    private static GameObject UiObject(string name, Transform parent, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        return root;
    }

    private static void SetAnchor(GameObject target, Vector2 min, Vector2 max, Vector2 anchoredPosition)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = anchoredPosition;
    }

    private static void AddVerticalLayout(GameObject target, int spacing, int paddingX, int paddingY)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void AddLayoutElement(GameObject target, float preferredWidth, float preferredHeight)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = preferredHeight;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Resources/UI/PRD_19");
        Directory.CreateDirectory(GeneratedFolder);
        AssetDatabase.Refresh();
    }
}
#endif
