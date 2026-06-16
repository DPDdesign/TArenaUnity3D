using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class PRD19_026_SavedArmiesPrefabBuilder
{
    private const string RootFolder = "Assets/Resources/UI/PRD_19/026_SavedArmies";
    private const string TemplateFolder = RootFolder + "/Prefabs";
    private const string ScreenPath = RootFolder + "/PRD_19_026_SavedArmies.prefab";
    private const string SlotPath = TemplateFolder + "/PRD19_026_SavedArmySlot.prefab";
    private const string StackRowPath = TemplateFolder + "/PRD19_026_StackRow.prefab";
    private const string ArenaOptionPath = TemplateFolder + "/PRD19_026_ArenaOption.prefab";
    private const string HistoryRowPath = TemplateFolder + "/PRD19_026_HistoryRow.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD19_026_CommandButton.prefab";
    private const string AutoBuildSessionKey = "PRD19_026_SavedArmiesPrefabBuilder.AutoBuildAttempted";

    [InitializeOnLoadMethod]
    private static void RebuildMissingPrefabAfterImport()
    {
        if (SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath) != null)
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath) != null)
            {
                return;
            }

            Rebuild();
        };
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 026 Saved Armies Prefabs")]
    public static void Rebuild()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(TemplateFolder);

        SavePrefab(BuildSlotTemplate(), SlotPath);
        SavePrefab(BuildStackRowTemplate(), StackRowPath);
        SavePrefab(BuildArenaOptionTemplate(), ArenaOptionPath);
        SavePrefab(BuildHistoryRowTemplate(), HistoryRowPath);
        SavePrefab(BuildCommandButtonTemplate(), CommandButtonPath);

        AssetDatabase.ImportAsset(SlotPath);
        AssetDatabase.ImportAsset(StackRowPath);
        AssetDatabase.ImportAsset(ArenaOptionPath);
        AssetDatabase.ImportAsset(HistoryRowPath);
        AssetDatabase.ImportAsset(CommandButtonPath);

        SavePrefab(BuildScreen(), ScreenPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject BuildScreen()
    {
        GameObject root = CreatePanel("PRD_19_026_SavedArmies", null, new Vector2(1600f, 900f), new Color(0.045f, 0.042f, 0.035f, 1f), false);
        GameObject script = CreateRect("Script_SavedArmiesScreenController", root.transform, Vector2.zero);
        SavedArmiesScreenController controller = script.AddComponent<SavedArmiesScreenController>();

        TextMeshProUGUI title = AddText("Text_Title", root.transform, "SAVED ARMIES", 44, TextAlignmentOptions.Center, new Vector2(520f, 64f), new Vector2(0f, 414f), Gold());
        TextMeshProUGUI defence = AddText("Text_DefenceState", root.transform, "Current Defence: none", 18, TextAlignmentOptions.MidlineRight, new Vector2(420f, 34f), new Vector2(520f, 412f), Green());

        GameObject roster = CreatePanel("Section_RosterSlots", root.transform, new Vector2(810f, 710f), new Color(0.08f, 0.07f, 0.055f, 0.96f), false);
        Place(roster, new Vector2(-380f, 22f));
        GridLayoutGroup grid = roster.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(188f, 318f);
        grid.spacing = new Vector2(10f, 14f);
        grid.padding = new RectOffset(14, 14, 18, 18);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        List<SavedArmiesSlotView> slotViews = new List<SavedArmiesSlotView>();
        for (int i = 0; i < 8; i++)
        {
            GameObject slot = InstantiateNestedPrefab(SlotPath, "SavedArmySlot_" + (i + 1).ToString("00"), roster.transform);
            SavedArmiesSlotView view = slot.GetComponent<SavedArmiesSlotView>();
            if (view != null)
            {
                slotViews.Add(view);
            }
        }

        GameObject detail = CreatePanel("Section_SelectedArmyDetails", root.transform, new Vector2(360f, 710f), new Color(0.62f, 0.50f, 0.32f, 1f), false);
        Place(detail, new Vector2(224f, 22f));
        TextMeshProUGUI selectedTitle = AddText("Text_SelectedArmyTitle", detail.transform, "Saved Army", 26, TextAlignmentOptions.MidlineLeft, new Vector2(320f, 44f), new Vector2(0f, 314f), DarkInk());
        TextMeshProUGUI selectedValue = AddText("Text_SelectedArmyValue", detail.transform, "Current Value 0", 22, TextAlignmentOptions.MidlineLeft, new Vector2(320f, 38f), new Vector2(0f, 270f), DarkInk());
        TextMeshProUGUI selectedMeta = AddText("Text_SelectedArmyMeta", detail.transform, "Immutable snapshot. Cannot be edited.", 14, TextAlignmentOptions.TopLeft, new Vector2(320f, 62f), new Vector2(0f, -292f), DarkInk());

        GameObject stackList = CreateRect("List_SelectedArmyStacks", detail.transform, new Vector2(320f, 380f));
        Place(stackList, new Vector2(0f, 34f));
        VerticalLayoutGroup stackLayout = stackList.AddComponent<VerticalLayoutGroup>();
        stackLayout.spacing = 8f;
        stackLayout.childControlHeight = false;
        stackLayout.childForceExpandHeight = false;

        List<SavedArmiesStackRowView> stackRows = new List<SavedArmiesStackRowView>();
        for (int i = 0; i < 5; i++)
        {
            GameObject row = InstantiateNestedPrefab(StackRowPath, "StackRow_" + (i + 1).ToString("00"), stackList.transform);
            SavedArmiesStackRowView view = row.GetComponent<SavedArmiesStackRowView>();
            if (view != null)
            {
                stackRows.Add(view);
            }
        }

        GameObject right = CreatePanel("Section_CommandsImportHistory", root.transform, new Vector2(300f, 710f), new Color(0.08f, 0.07f, 0.055f, 0.96f), false);
        Place(right, new Vector2(596f, 22f));

        GameObject setDefenceObject = InstantiateNestedPrefab(CommandButtonPath, "Button_SetDefence", right.transform);
        Place(setDefenceObject, new Vector2(0f, 300f));
        SavedArmiesCommandButtonView setDefence = setDefenceObject.GetComponent<SavedArmiesCommandButtonView>();
        ConfigureCommand(setDefence, SavedArmiesCommandButtonKind.SetDefence, "Set Defence");

        AddText("Text_ImportHeader", right.transform, "SEED ARMIES", 18, TextAlignmentOptions.Center, new Vector2(260f, 30f), new Vector2(0f, 238f), Gold());
        GameObject arenaList = CreateRect("List_ArenaImportOptions", right.transform, new Vector2(260f, 172f));
        Place(arenaList, new Vector2(0f, 132f));
        VerticalLayoutGroup arenaLayout = arenaList.AddComponent<VerticalLayoutGroup>();
        arenaLayout.spacing = 8f;
        arenaLayout.childControlHeight = false;
        arenaLayout.childForceExpandHeight = false;

        List<SavedArmiesArenaOptionView> arenaViews = new List<SavedArmiesArenaOptionView>();
        for (int i = 0; i < 3; i++)
        {
            GameObject option = InstantiateNestedPrefab(ArenaOptionPath, "ArenaOption_" + (i + 1).ToString("00"), arenaList.transform);
            SavedArmiesArenaOptionView view = option.GetComponent<SavedArmiesArenaOptionView>();
            if (view != null)
            {
                arenaViews.Add(view);
            }
        }

        GameObject importObject = InstantiateNestedPrefab(CommandButtonPath, "Button_ImportFromArena", right.transform);
        Place(importObject, new Vector2(0f, 18f));
        SavedArmiesCommandButtonView import = importObject.GetComponent<SavedArmiesCommandButtonView>();
        ConfigureCommand(import, SavedArmiesCommandButtonKind.ImportFromArena, "Load Seed Army");

        AddText("Text_HistoryHeader", right.transform, "ATTACK HISTORY", 18, TextAlignmentOptions.Center, new Vector2(260f, 30f), new Vector2(0f, -54f), Gold());
        GameObject historyList = CreateRect("List_AttackHistory", right.transform, new Vector2(260f, 178f));
        Place(historyList, new Vector2(0f, -158f));
        VerticalLayoutGroup historyLayout = historyList.AddComponent<VerticalLayoutGroup>();
        historyLayout.spacing = 8f;
        historyLayout.childControlHeight = false;
        historyLayout.childForceExpandHeight = false;

        List<SavedArmiesHistoryEntryView> historyRows = new List<SavedArmiesHistoryEntryView>();
        for (int i = 0; i < 3; i++)
        {
            GameObject row = InstantiateNestedPrefab(HistoryRowPath, "HistoryRow_" + (i + 1).ToString("00"), historyList.transform);
            SavedArmiesHistoryEntryView view = row.GetComponent<SavedArmiesHistoryEntryView>();
            if (view != null)
            {
                historyRows.Add(view);
            }
        }

        AddText("Text_BackendGap", right.transform, "Roster persistence: tutaj powinno byc z bazy danych", 12, TextAlignmentOptions.TopLeft, new Vector2(260f, 48f), new Vector2(0f, -322f), Warning());

        GameObject backObject = InstantiateNestedPrefab(CommandButtonPath, "Button_Back", root.transform);
        Place(backObject, new Vector2(0f, -412f));
        SavedArmiesCommandButtonView back = backObject.GetComponent<SavedArmiesCommandButtonView>();
        ConfigureCommand(back, SavedArmiesCommandButtonKind.Back, "Back");

        TextMeshProUGUI status = AddText("Text_Status", root.transform, "Offline Saved Armies prototype.", 15, TextAlignmentOptions.Center, new Vector2(900f, 30f), new Vector2(0f, -360f), new Color(0.85f, 0.76f, 0.56f, 1f));

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "titleText", title);
        SetObject(serialized, "defenceStateText", defence);
        SetObject(serialized, "statusText", status);
        SetObject(serialized, "selectedArmyTitleText", selectedTitle);
        SetObject(serialized, "selectedArmyMetaText", selectedMeta);
        SetObject(serialized, "selectedArmyValueText", selectedValue);
        SetArray(serialized, "stackRows", stackRows.ToArray());
        SetArray(serialized, "slotViews", slotViews.ToArray());
        SetArray(serialized, "arenaOptions", arenaViews.ToArray());
        SetArray(serialized, "historyRows", historyRows.ToArray());
        SetObject(serialized, "importButton", import);
        SetObject(serialized, "setDefenceButton", setDefence);
        SetObject(serialized, "backButton", back);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildSlotTemplate()
    {
        GameObject root = CreatePanel("PRD19_026_SavedArmySlot", null, new Vector2(188f, 318f), new Color(0.16f, 0.11f, 0.07f, 0.96f), true);
        Button button = root.AddComponent<Button>();
        SavedArmiesSlotView view = root.AddComponent<SavedArmiesSlotView>();
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);

        GameObject selected = CreateImageObject("State_Selected", root.transform, new Vector2(180f, 310f), Vector2.zero, new Color(1f, 0.72f, 0.16f, 0.23f));
        GameObject locked = CreateImageObject("State_Locked", root.transform, new Vector2(180f, 310f), Vector2.zero, new Color(0f, 0f, 0f, 0.48f));
        GameObject defence = CreateImageObject("State_CurrentDefence", root.transform, new Vector2(152f, 28f), new Vector2(0f, 114f), new Color(0.18f, 0.43f, 0.12f, 0.95f));
        TextMeshProUGUI number = AddText("Text_SlotNumber", root.transform, "1", 22, TextAlignmentOptions.Center, new Vector2(34f, 34f), new Vector2(-70f, 130f), Color.white);
        TextMeshProUGUI state = AddText("Text_State", root.transform, "Taken", 15, TextAlignmentOptions.Center, new Vector2(150f, 28f), new Vector2(0f, 82f), Gold());
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "Value 2,450", 18, TextAlignmentOptions.Center, new Vector2(150f, 32f), new Vector2(0f, -74f), Gold());
        TextMeshProUGUI savedId = AddText("Text_SavedArmyId", root.transform, "saved-army", 13, TextAlignmentOptions.Center, new Vector2(150f, 40f), new Vector2(0f, -118f), Color.white);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "slotNumberText", number);
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
        GameObject root = CreatePanel("PRD19_026_StackRow", null, new Vector2(318f, 62f), new Color(0.28f, 0.19f, 0.10f, 0.94f), false);
        SavedArmiesStackRowView view = root.AddComponent<SavedArmiesStackRowView>();
        Image icon = CreateImage("Image_UnitIcon", root.transform, new Vector2(48f, 48f), new Vector2(-128f, 0f), new Color(0.18f, 0.30f, 0.35f, 1f));
        TextMeshProUGUI name = AddText("Text_Name", root.transform, "Stone Golem", 15, TextAlignmentOptions.MidlineLeft, new Vector2(140f, 20f), new Vector2(-42f, 15f), Color.white);
        TextMeshProUGUI tier = AddText("Text_Tier", root.transform, "Tier III / unit 20", 12, TextAlignmentOptions.MidlineLeft, new Vector2(140f, 18f), new Vector2(-42f, -12f), new Color(0.82f, 0.72f, 0.54f, 1f));
        TextMeshProUGUI amount = AddText("Text_Amount", root.transform, "x120", 14, TextAlignmentOptions.MidlineRight, new Vector2(60f, 20f), new Vector2(94f, 15f), Color.white);
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "2,400 value", 12, TextAlignmentOptions.MidlineRight, new Vector2(88f, 18f), new Vector2(82f, -12f), Gold());

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
        GameObject root = CreatePanel("PRD19_026_ArenaOption", null, new Vector2(260f, 50f), new Color(0.20f, 0.13f, 0.08f, 0.96f), true);
        Button button = root.AddComponent<Button>();
        SavedArmiesArenaOptionView view = root.AddComponent<SavedArmiesArenaOptionView>();
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);
        GameObject selected = CreateImageObject("State_Selected", root.transform, new Vector2(252f, 44f), Vector2.zero, new Color(1f, 0.72f, 0.16f, 0.23f));
        TextMeshProUGUI name = AddText("Text_Name", root.transform, "Seed Army", 15, TextAlignmentOptions.MidlineLeft, new Vector2(150f, 20f), new Vector2(-40f, 10f), Color.white);
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "Value 1,200", 12, TextAlignmentOptions.MidlineLeft, new Vector2(150f, 18f), new Vector2(-40f, -12f), Gold());

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
        GameObject root = CreatePanel("PRD19_026_HistoryRow", null, new Vector2(260f, 50f), new Color(0.13f, 0.10f, 0.08f, 0.96f), false);
        SavedArmiesHistoryEntryView view = root.AddComponent<SavedArmiesHistoryEntryView>();
        TextMeshProUGUI result = AddText("Text_Result", root.transform, "Defence Win", 13, TextAlignmentOptions.MidlineLeft, new Vector2(112f, 18f), new Vector2(-62f, 10f), Green());
        TextMeshProUGUI opponent = AddText("Text_Opponent", root.transform, "Forest Bandits", 12, TextAlignmentOptions.MidlineRight, new Vector2(118f, 18f), new Vector2(62f, 10f), Color.white);
        TextMeshProUGUI values = AddText("Text_Values", root.transform, "A 2,450 / D 2,110", 12, TextAlignmentOptions.Center, new Vector2(230f, 18f), new Vector2(0f, -12f), Gold());

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "resultText", result);
        SetObject(serialized, "opponentText", opponent);
        SetObject(serialized, "valuesText", values);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject BuildCommandButtonTemplate()
    {
        GameObject root = CreatePanel("PRD19_026_CommandButton", null, new Vector2(220f, 46f), new Color(0.25f, 0.38f, 0.13f, 1f), true);
        Button button = root.AddComponent<Button>();
        SavedArmiesCommandButtonView view = root.AddComponent<SavedArmiesCommandButtonView>();
        UnityEventTools.AddPersistentListener(button.onClick, view.OnClicked);
        TextMeshProUGUI label = AddText("Text_Label", root.transform, "Command", 18, TextAlignmentOptions.Center, new Vector2(200f, 34f), Vector2.zero, new Color(0.98f, 0.86f, 0.55f, 1f));

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
        SetEnum(serialized, "commandKind", kind);
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
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
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

    private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreateImageObject(name, parent, size, anchoredPosition, color);
        return go.GetComponent<Image>();
    }

    private static GameObject CreateImageObject(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject go = CreateRect(name, parent, size);
        Place(go, anchoredPosition);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
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

    private static GameObject InstantiateNestedPrefab(string path, string name, Transform parent)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = asset == null
            ? CreatePanel(name, parent, new Vector2(100f, 40f), new Color(0.2f, 0.1f, 0.05f, 1f), false)
            : (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.name = name;
        return instance;
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

    private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = values == null ? 0 : values.Length;
        for (int i = 0; values != null && i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetEnum(SerializedObject serialized, string propertyName, SavedArmiesCommandButtonKind value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = (int)value;
        }
    }

    private static Color Gold()
    {
        return new Color(0.96f, 0.78f, 0.42f, 1f);
    }

    private static Color Green()
    {
        return new Color(0.68f, 0.92f, 0.38f, 1f);
    }

    private static Color Warning()
    {
        return new Color(0.95f, 0.64f, 0.42f, 1f);
    }

    private static Color DarkInk()
    {
        return new Color(0.14f, 0.08f, 0.035f, 1f);
    }
}
