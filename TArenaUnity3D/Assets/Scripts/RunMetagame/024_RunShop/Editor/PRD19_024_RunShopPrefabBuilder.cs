using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_024_RunShopPrefabBuilder
{
    private const string MainPrefabPath = "Assets/Resources/UI/PRD_19/024_RunShop/PRD_19_024_RunShop.prefab";
    private const string TemplateFolder = "Assets/Resources/UI/PRD_19/024_RunShop/Prefabs";
    private const string OfferCardPath = TemplateFolder + "/PRD19_024_OfferCard.prefab";
    private const string StackRowPath = TemplateFolder + "/PRD19_024_ArmyPreview_StackRow.prefab";
    private const string CommandButtonPath = TemplateFolder + "/PRD19_024_CommandButton.prefab";
    private const string WalletSummaryPath = TemplateFolder + "/PRD19_024_WalletSummary.prefab";
    private const string BuildSessionKey = "TArena.PRD19_024.RunShopPrefabBuilder.NestedPrefabsBuilt";

    static PRD19_024_RunShopPrefabBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 024 Run Shop Prefabs")]
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
        EnsureFolder(TemplateFolder);

        GameObject offerCard = BuildOfferCardTemplate();
        SavePrefab(offerCard, OfferCardPath);

        GameObject stackRow = BuildStackRowTemplate();
        SavePrefab(stackRow, StackRowPath);

        GameObject commandButton = BuildCommandButtonTemplate("PRD19_024_CommandButton", "Command");
        SavePrefab(commandButton, CommandButtonPath);

        GameObject walletSummary = BuildWalletSummaryTemplate();
        SavePrefab(walletSummary, WalletSummaryPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(OfferCardPath);
        AssetDatabase.ImportAsset(StackRowPath);
        AssetDatabase.ImportAsset(CommandButtonPath);
        AssetDatabase.ImportAsset(WalletSummaryPath);

        GameObject main = BuildMainPrefab();
        SavePrefab(main, MainPrefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD_19_024 Run Shop UI prefabs with components and serialized wiring.");
    }

    private static GameObject BuildMainPrefab()
    {
        GameObject root = CreateRect("PRD_19_024_RunShop", null, new Vector2(1600f, 900f));
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.10f, 0.13f, 0.16f, 1f);
        background.raycastTarget = false;

        GameObject scriptOwner = CreateRect("Script_RunShopScreenController", root.transform, new Vector2(100f, 100f));
        RunShopScreenController controller = scriptOwner.AddComponent<RunShopScreenController>();

        GameObject header = CreatePanel("Section_WalletAndHeader", root.transform, new Vector2(1520f, 92f), new Color(0.16f, 0.12f, 0.09f, 0.95f));
        SetAnchored(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f));
        AddText("Title", header.transform, "Run Shop", 34, TextAlignmentOptions.MidlineLeft, new Vector2(760f, 60f), new Vector2(-350f, 0f), new Color(0.95f, 0.86f, 0.62f, 1f));
        TextMeshProUGUI walletText = AddText("Text_Wallet", header.transform, "120 RUN GOLD", 26, TextAlignmentOptions.MidlineRight, new Vector2(360f, 54f), new Vector2(530f, 0f), new Color(1f, 0.82f, 0.26f, 1f));

        GameObject currentArmyPanel = CreatePanel("Section_YourArmy", root.transform, new Vector2(430f, 650f), new Color(0.18f, 0.15f, 0.11f, 0.92f));
        SetAnchored(currentArmyPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(260f, -15f));
        AddText("Header", currentArmyPanel.transform, "Your Army", 24, TextAlignmentOptions.MidlineLeft, new Vector2(360f, 38f), new Vector2(0f, 292f), Color.white);
        GameObject currentList = CreateVerticalList("List_CurrentArmyRows", currentArmyPanel.transform, new Vector2(386f, 560f), new Vector2(0f, -18f), 8f);

        GameObject offersPanel = CreatePanel("Section_GroupedOffers", root.transform, new Vector2(560f, 650f), new Color(0.20f, 0.16f, 0.11f, 0.94f));
        SetAnchored(offersPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -15f));
        AddText("Header", offersPanel.transform, "Limited Offers", 24, TextAlignmentOptions.MidlineLeft, new Vector2(500f, 38f), new Vector2(0f, 292f), Color.white);
        GameObject offerList = CreateRect("List_OfferCards", offersPanel.transform, new Vector2(510f, 560f));
        offerList.AddComponent<GridLayoutGroup>().cellSize = new Vector2(245f, 166f);
        GridLayoutGroup offerGrid = offerList.GetComponent<GridLayoutGroup>();
        offerGrid.spacing = new Vector2(12f, 12f);
        offerGrid.padding = new RectOffset(0, 0, 0, 0);
        SetAnchored(offerList, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f));

        GameObject selectedPanel = CreatePanel("Section_SelectedOfferPreview", root.transform, new Vector2(420f, 230f), new Color(0.16f, 0.13f, 0.10f, 0.94f));
        SetAnchored(selectedPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250f, -218f));
        TextMeshProUGUI selectedTitle = AddText("Text_SelectedOfferTitle", selectedPanel.transform, "Selected Offer", 24, TextAlignmentOptions.MidlineLeft, new Vector2(360f, 44f), new Vector2(0f, 74f), Color.white);
        TextMeshProUGUI selectedPreview = AddText("Text_SelectedOfferPreview", selectedPanel.transform, "Before -> After", 18, TextAlignmentOptions.TopLeft, new Vector2(360f, 110f), new Vector2(0f, -8f), new Color(0.88f, 0.82f, 0.70f, 1f));

        GameObject previewPanel = CreatePanel("Section_ArmyAfterPurchase", root.transform, new Vector2(420f, 380f), new Color(0.18f, 0.15f, 0.11f, 0.92f));
        SetAnchored(previewPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-250f, 255f));
        AddText("Header", previewPanel.transform, "Army After Purchase", 24, TextAlignmentOptions.MidlineLeft, new Vector2(360f, 38f), new Vector2(0f, 164f), Color.white);
        GameObject previewList = CreateVerticalList("List_PreviewArmyRows", previewPanel.transform, new Vector2(378f, 304f), new Vector2(0f, -16f), 8f);

        GameObject commands = CreatePanel("Section_Commands", root.transform, new Vector2(560f, 86f), new Color(0.14f, 0.11f, 0.08f, 0.88f));
        SetAnchored(commands, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f));
        Button buyButton = CreateButton("Button_BuyFocusedOffer", commands.transform, "Buy Focused Offer", new Vector2(236f, 52f), new Vector2(-132f, 0f), true);
        Button leaveButton = CreateButton("Button_LeaveShop", commands.transform, "Leave Shop", new Vector2(180f, 52f), new Vector2(108f, 0f), false);
        UnityEventTools.AddPersistentListener(buyButton.onClick, controller.BuyFocusedOffer);
        UnityEventTools.AddPersistentListener(leaveButton.onClick, controller.LeaveShop);

        TextMeshProUGUI messageText = AddText("Text_ResultMessage", root.transform, "Preview ready.", 18, TextAlignmentOptions.Midline, new Vector2(600f, 34f), new Vector2(0f, 14f), new Color(0.85f, 0.80f, 0.68f, 1f));
        SetAnchored(messageText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f));

        List<RunShopOfferCardView> offerCards = new List<RunShopOfferCardView>();
        for (int i = 0; i < 6; i++)
        {
            GameObject card = InstantiateNestedPrefab(
                OfferCardPath,
                "OfferCard_" + (i + 1).ToString("00"),
                offerList.transform);
            offerCards.Add(card.GetComponent<RunShopOfferCardView>());
        }

        List<RunShopStackRowView> currentRows = new List<RunShopStackRowView>();
        List<RunShopStackRowView> previewRows = new List<RunShopStackRowView>();
        for (int i = 0; i < 4; i++)
        {
            GameObject row = InstantiateNestedPrefab(
                StackRowPath,
                "CurrentArmy_Row_" + (i + 1).ToString("00"),
                currentList.transform);
            currentRows.Add(row.GetComponent<RunShopStackRowView>());

            GameObject previewRow = InstantiateNestedPrefab(
                StackRowPath,
                "PreviewArmy_Row_" + (i + 1).ToString("00"),
                previewList.transform);
            previewRows.Add(previewRow.GetComponent<RunShopStackRowView>());
        }

        SerializedObject serialized = new SerializedObject(controller);
        SetObjectArray(serialized, "offerCards", offerCards.ToArray());
        SetObjectArray(serialized, "currentArmyRows", currentRows.ToArray());
        SetObjectArray(serialized, "previewArmyRows", previewRows.ToArray());
        SetObject(serialized, "walletText", walletText);
        SetObject(serialized, "selectedOfferTitleText", selectedTitle);
        SetObject(serialized, "selectedOfferPreviewText", selectedPreview);
        SetObject(serialized, "resultMessageText", messageText);
        SetObject(serialized, "buyButton", buyButton);
        SetObject(serialized, "leaveButton", leaveButton);
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

    private static GameObject BuildOfferCardTemplate()
    {
        GameObject root = CreatePanel("PRD19_024_OfferCard", null, new Vector2(245f, 166f), new Color(0.24f, 0.18f, 0.11f, 0.96f));
        Button button = root.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, button.Select);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.60f, 1f);
        colors.pressedColor = new Color(0.85f, 0.62f, 0.28f, 1f);
        colors.disabledColor = new Color(0.34f, 0.31f, 0.28f, 1f);
        button.colors = colors;

        RunShopOfferCardView view = root.AddComponent<RunShopOfferCardView>();
        TextMeshProUGUI title = AddText("Text_Title", root.transform, "Field Resurrection", 18, TextAlignmentOptions.TopLeft, new Vector2(210f, 32f), new Vector2(0f, 54f), Color.white);
        TextMeshProUGUI category = AddText("Text_Category", root.transform, "Resurrection", 14, TextAlignmentOptions.MidlineLeft, new Vector2(118f, 24f), new Vector2(-46f, 23f), new Color(1f, 0.78f, 0.30f, 1f));
        TextMeshProUGUI cost = AddText("Text_Cost", root.transform, "55", 16, TextAlignmentOptions.MidlineRight, new Vector2(70f, 24f), new Vector2(66f, 23f), new Color(1f, 0.82f, 0.30f, 1f));
        TextMeshProUGUI detail = AddText("Text_Detail", root.transform, "Recover part of one damaged stack.", 13, TextAlignmentOptions.TopLeft, new Vector2(210f, 46f), new Vector2(0f, -14f), new Color(0.88f, 0.82f, 0.72f, 1f));
        TextMeshProUGUI preview = AddText("Text_BeforeAfter", root.transform, "Rusher: 28 alive, 5 lost -> 31 alive, 2 lost", 12, TextAlignmentOptions.TopLeft, new Vector2(210f, 42f), new Vector2(0f, -57f), new Color(0.72f, 0.91f, 0.76f, 1f));
        GameObject selectedState = CreateOverlay("State_Selected", root.transform, new Color(1f, 0.78f, 0.22f, 0.18f));
        GameObject disabledState = CreateOverlay("State_Disabled", root.transform, new Color(0f, 0f, 0f, 0.40f));
        GameObject purchasedState = AddText("State_Purchased", root.transform, "PURCHASED", 16, TextAlignmentOptions.Midline, new Vector2(180f, 34f), Vector2.zero, new Color(0.55f, 1f, 0.65f, 1f)).gameObject;
        selectedState.SetActive(false);
        disabledState.SetActive(false);
        purchasedState.SetActive(false);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "button", button);
        SetObject(serialized, "titleText", title);
        SetObject(serialized, "categoryText", category);
        SetObject(serialized, "costText", cost);
        SetObject(serialized, "detailText", detail);
        SetObject(serialized, "previewText", preview);
        SetObject(serialized, "selectedState", selectedState);
        SetObject(serialized, "disabledState", disabledState);
        SetObject(serialized, "purchasedState", purchasedState);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildStackRowTemplate()
    {
        GameObject root = CreatePanel("PRD19_024_ArmyPreview_StackRow", null, new Vector2(360f, 76f), new Color(0.12f, 0.11f, 0.10f, 0.86f));
        RunShopStackRowView view = root.AddComponent<RunShopStackRowView>();

        Image icon = CreateImage("Icon_Unit", root.transform, new Vector2(54f, 54f), new Vector2(-146f, 0f), new Color(0.34f, 0.27f, 0.18f, 1f));
        TextMeshProUGUI name = AddText("Text_Name", root.transform, "Rusher", 17, TextAlignmentOptions.MidlineLeft, new Vector2(176f, 22f), new Vector2(-32f, 20f), Color.white);
        TextMeshProUGUI tier = AddText("Text_Tier", root.transform, "Tier I / Level 1", 12, TextAlignmentOptions.MidlineLeft, new Vector2(176f, 18f), new Vector2(-32f, 1f), new Color(0.78f, 0.72f, 0.62f, 1f));
        TextMeshProUGUI amount = AddText("Text_Amount", root.transform, "x28 / lost 5", 12, TextAlignmentOptions.MidlineLeft, new Vector2(176f, 18f), new Vector2(-32f, -18f), new Color(0.88f, 0.78f, 0.48f, 1f));
        TextMeshProUGUI value = AddText("Text_Value", root.transform, "868 value", 13, TextAlignmentOptions.MidlineRight, new Vector2(82f, 24f), new Vector2(130f, 16f), new Color(1f, 0.82f, 0.30f, 1f));
        TextMeshProUGUI skills = AddText("Text_Skills", root.transform, "Chope / [Rush]", 11, TextAlignmentOptions.MidlineRight, new Vector2(100f, 30f), new Vector2(122f, -18f), new Color(0.68f, 0.86f, 1f, 1f));

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

    private static GameObject BuildCommandButtonTemplate(string name, string label)
    {
        GameObject root = CreatePanel(name, null, new Vector2(190f, 52f), new Color(0.54f, 0.34f, 0.14f, 1f));
        Button button = root.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, button.Select);
        AddText("Text_Label", root.transform, label, 18, TextAlignmentOptions.Midline, new Vector2(174f, 42f), Vector2.zero, Color.white);
        return root;
    }

    private static GameObject BuildWalletSummaryTemplate()
    {
        GameObject root = CreatePanel("PRD19_024_WalletSummary", null, new Vector2(280f, 82f), new Color(0.15f, 0.12f, 0.09f, 0.95f));
        AddText("Text_RunGold", root.transform, "RUN GOLD 120", 22, TextAlignmentOptions.Midline, new Vector2(250f, 38f), new Vector2(0f, 15f), new Color(1f, 0.82f, 0.30f, 1f));
        AddText("Text_InventorySummary", root.transform, "Inventory summary: tutaj powinno byc z bazy danych", 11, TextAlignmentOptions.Midline, new Vector2(250f, 28f), new Vector2(0f, -20f), new Color(0.80f, 0.74f, 0.62f, 1f));
        return root;
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
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 size, Vector2 anchoredPosition, bool primary)
    {
        GameObject go = CreatePanel(name, parent, size, primary ? new Color(0.64f, 0.38f, 0.12f, 1f) : new Color(0.25f, 0.22f, 0.18f, 1f));
        SetAnchored(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Button button = go.AddComponent<Button>();
        AddText("Text_Label", go.transform, text, 17, TextAlignmentOptions.Midline, new Vector2(size.x - 16f, size.y - 10f), Vector2.zero, Color.white);
        return button;
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
