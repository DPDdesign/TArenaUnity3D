using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_035_RandomStartRoutesMockupBuilder
{
    private const string BuildSessionKey = "TArena.PRD19_035.RandomStartRoutesMockupBuilt.v1";
    private const string BaseFolder = "Assets/UI/PRD_19/PRD_19_35";
    private const string PrefabFolder = BaseFolder + "/Prefabs";
    private const string PolishedFolder = BaseFolder + "/Polished_1";
    private const string PolishedPrefabFolder = PolishedFolder + "/Prefabs";
    private const string ScreenPath = BaseFolder + "/PRD_19_35_RandomStartRoutes.prefab";
    private const string OfferCardPath = PrefabFolder + "/PRD_19_35_GeneratedArmyOffer.prefab";
    private const string RouteNodePath = PrefabFolder + "/PRD_19_35_MissionRouteNode.prefab";
    private const string CommandButtonPath = PrefabFolder + "/PRD_19_35_CommandButton.prefab";
    private const string PolishedScreenPath = PolishedFolder + "/PRD_19_35_RandomStartRoutes_Polished.prefab";
    private const string PolishedOfferCardPath = PolishedPrefabFolder + "/PRD_19_35_GeneratedArmyOffer_Polished.prefab";
    private const string PolishedRouteNodePath = PolishedPrefabFolder + "/PRD_19_35_MissionRouteNode_Polished.prefab";
    private const string PolishedCommandButtonPath = PolishedPrefabFolder + "/PRD_19_35_CommandButton_Polished.prefab";

    private static readonly Color Surface = new Color(0.045f, 0.038f, 0.030f, 1f);
    private static readonly Color Panel = new Color(0.10f, 0.070f, 0.045f, 0.97f);
    private static readonly Color PanelDark = new Color(0.055f, 0.045f, 0.038f, 0.98f);
    private static readonly Color Parchment = new Color(0.62f, 0.48f, 0.30f, 1f);
    private static readonly Color Gold = new Color(0.94f, 0.74f, 0.36f, 1f);
    private static readonly Color Text = new Color(0.94f, 0.82f, 0.64f, 1f);
    private static readonly Color Ink = new Color(0.16f, 0.10f, 0.055f, 1f);

    static PRD19_035_RandomStartRoutesMockupBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 035 Random Start Routes Prefabs")]
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
        EnsureFolder(PrefabFolder);
        EnsureFolder(PolishedPrefabFolder);

        SavePrefab(BuildOfferCard(false), OfferCardPath);
        SavePrefab(BuildRouteNode(false), RouteNodePath);
        SavePrefab(BuildCommandButton(false), CommandButtonPath);
        SavePrefab(BuildOfferCard(true), PolishedOfferCardPath);
        SavePrefab(BuildRouteNode(true), PolishedRouteNodePath);
        SavePrefab(BuildCommandButton(true), PolishedCommandButtonPath);

        AssetDatabase.ImportAsset(OfferCardPath);
        AssetDatabase.ImportAsset(RouteNodePath);
        AssetDatabase.ImportAsset(CommandButtonPath);
        AssetDatabase.ImportAsset(PolishedOfferCardPath);
        AssetDatabase.ImportAsset(PolishedRouteNodePath);
        AssetDatabase.ImportAsset(PolishedCommandButtonPath);

        SavePrefab(BuildScreen(false), ScreenPath);
        SavePrefab(BuildScreen(true), PolishedScreenPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD 19 035 random start routes mockup prefabs under Assets/UI/PRD_19/PRD_19_35.");
    }

    private static GameObject BuildScreen(bool polished)
    {
        string offerCardPath = polished ? PolishedOfferCardPath : OfferCardPath;
        string routeNodePath = polished ? PolishedRouteNodePath : RouteNodePath;
        string commandButtonPath = polished ? PolishedCommandButtonPath : CommandButtonPath;
        Color surface = polished ? new Color(0.035f, 0.033f, 0.030f, 1f) : Surface;
        Color centerColor = polished ? new Color(0.66f, 0.52f, 0.33f, 1f) : Parchment;

        GameObject root = CreatePanel(polished ? "PRD_19_35_RandomStartRoutes_Polished" : "PRD_19_35_RandomStartRoutes", null, new Vector2(1600f, 900f), surface, null, false);
        PRD19_035_RandomStartRoutesMockupController controller = CreateScriptOwner(root.transform);

        AddHeader(root.transform, polished ? "PRD 035 GENERATED RUN - POLISHED" : "PRD 035 GENERATED RUN", "Random starting armies plus fixed mission route map.");

        GameObject left = CreatePanel("Section_GeneratedArmyOffers", root.transform, new Vector2(430f, 690f), Panel, LoadSprite("Assets/Classic_RPG_GUI/Parts/info_window.png"), false);
        Place(left, new Vector2(-555f, -10f));
        AddText("Text_ArmyOffersTitle", left.transform, "STARTING ARMY OFFERS", 24, TextAlignmentOptions.Center, new Vector2(360f, 34f), new Vector2(0f, 300f), Gold);
        AddText("Text_SelectedOffer", left.transform, "Selected generated offer 1", 16, TextAlignmentOptions.Center, new Vector2(350f, 28f), new Vector2(0f, 264f), Text);

        GameObject offerList = CreateVerticalList("List_GeneratedArmyOfferCards", left.transform, new Vector2(370f, 410f), new Vector2(0f, 28f), 10f);
        GameObject[] offerInstances = new GameObject[3];
        for (int i = 0; i < offerInstances.Length; i++)
        {
            offerInstances[i] = InstantiateNested(offerCardPath, "OfferCard_0" + (i + 1).ToString(), offerList.transform);
            AddLayout(offerInstances[i], 350f, 124f);
        }

        AddText("Text_StartingAssets", left.transform, "150 RUN GOLD / 1 REROLL TOKEN / 0 BATTLE SKIP TOKENS", 15, TextAlignmentOptions.Center, new Vector2(350f, 42f), new Vector2(0f, -242f), Gold);
        GameObject reroll = InstantiateNested(commandButtonPath, "Button_RerollPreview", left.transform);
        Place(reroll, new Vector2(-92f, -310f));
        SetButtonLabel(reroll, "REROLL");
        GameObject begin = InstantiateNested(commandButtonPath, "Button_BeginPreview", left.transform);
        Place(begin, new Vector2(92f, -310f));
        SetButtonLabel(begin, "BEGIN");

        GameObject center = CreatePanel("Section_GeneratedMissionRouteMap", root.transform, new Vector2(690f, 690f), centerColor, LoadSprite("Assets/Old_Paper_Gui/paper/old_paper_vintage0.png"), false);
        Place(center, new Vector2(20f, -10f));
        AddText("Text_RouteTitle", center.transform, "FOREST MISSION 1", 26, TextAlignmentOptions.Center, new Vector2(470f, 38f), new Vector2(0f, 300f), Ink);
        AddText("Text_RouteSummary", center.transform, "Fixed mission route: battle, event, battle, safe/risk branch, shop, final battle.", 15, TextAlignmentOptions.Center, new Vector2(590f, 32f), new Vector2(0f, -304f), Ink);

        GameObject nodeLayer = CreateRect("List_MissionRouteNodes", center.transform, new Vector2(650f, 540f));
        Place(nodeLayer, new Vector2(0f, -8f));
        string[] nodeTitles = { "1 Battle", "2 Event", "3 Battle", "4A Safe", "4B Risk", "9 Shop", "10 Final" };
        string[] nodeTypes = { "Battle", "Random Event", "Battle", "Battle", "Hard Battle", "Shop", "Final" };
        string[] nodeRisks = { "Low", "None", "Medium", "Medium", "High", "None", "Final" };
        Vector2[] nodePositions =
        {
            new Vector2(-250f, 150f),
            new Vector2(-120f, 150f),
            new Vector2(10f, 150f),
            new Vector2(165f, 230f),
            new Vector2(165f, 70f),
            new Vector2(320f, 150f),
            new Vector2(320f, -40f)
        };
        GameObject[] routeInstances = new GameObject[nodeTitles.Length];
        for (int i = 0; i < routeInstances.Length; i++)
        {
            routeInstances[i] = InstantiateNested(routeNodePath, "RouteNode_" + nodeTitles[i].Replace(" ", "_"), nodeLayer.transform);
            Place(routeInstances[i], nodePositions[i]);
            SetNodeLabels(routeInstances[i], nodeTitles[i], nodeTypes[i], nodeRisks[i]);
        }

        AddRouteLine(nodeLayer.transform, "Edge_1_2", new Vector2(-186f, 150f), new Vector2(-184f, 8f));
        AddRouteLine(nodeLayer.transform, "Edge_2_3", new Vector2(-56f, 150f), new Vector2(-184f, 8f));
        AddRouteLine(nodeLayer.transform, "Edge_3_4A", new Vector2(78f, 188f), new Vector2(-116f, 8f));
        AddRouteLine(nodeLayer.transform, "Edge_3_4B", new Vector2(78f, 112f), new Vector2(-116f, 8f));
        AddRouteLine(nodeLayer.transform, "Edge_Branch_Shop", new Vector2(242f, 150f), new Vector2(-108f, 8f));
        AddRouteLine(nodeLayer.transform, "Edge_Shop_Final", new Vector2(320f, 55f), new Vector2(8f, 110f));

        GameObject right = CreatePanel("Section_DeterminismAndRules", root.transform, new Vector2(380f, 690f), PanelDark, LoadSprite("Assets/Classic_RPG_GUI/Parts/info_window.png"), false);
        Place(right, new Vector2(590f, -10f));
        AddText("Text_RulesTitle", right.transform, "GENERATOR RULES", 25, TextAlignmentOptions.Center, new Vector2(310f, 36f), new Vector2(0f, 300f), Gold);
        AddRule(right.transform, "Rule_Seed", new Vector2(0f, 220f), "Seeded", "Same seed returns same offers and route.");
        AddRule(right.transform, "Rule_Budget", new Vector2(0f, 126f), "Budget", "1450-1750 hard band, target 1650.");
        AddRule(right.transform, "Rule_Races", new Vector2(0f, 32f), "Races", "Generated army stays within two races.");
        AddRule(right.transform, "Rule_Skills", new Vector2(0f, -62f), "Skills", "One unlocked legal skill per stack.");
        AddRule(right.transform, "Rule_DB", new Vector2(0f, -156f), "Offline DB", "Selected army and route nodes persist.");
        AddText("Text_SelectedRouteNode", right.transform, "Focused route point 1", 16, TextAlignmentOptions.Center, new Vector2(310f, 32f), new Vector2(0f, -252f), Gold);
        AddText("Text_RuntimeMessage", right.transform, "Preview ready.", 15, TextAlignmentOptions.Center, new Vector2(310f, 50f), new Vector2(0f, -308f), Text);

        WireController(controller, root, offerInstances, routeInstances, reroll, begin);
        return root;
    }

    private static GameObject BuildOfferCard(bool polished)
    {
        GameObject root = CreatePanel(polished ? "PRD_19_35_GeneratedArmyOffer_Polished" : "PRD_19_35_GeneratedArmyOffer", null, new Vector2(350f, 124f), polished ? new Color(0.18f, 0.11f, 0.06f, 1f) : Panel, LoadSprite("Assets/Classic_RPG_GUI/Parts/inventory_frame_little_long.png"), true);
        AddText("Text_Name", root.transform, "STONE SPARK", 20, TextAlignmentOptions.MidlineLeft, new Vector2(170f, 28f), new Vector2(-48f, 36f), Gold);
        AddText("Text_Value", root.transform, "VALUE 1640", 16, TextAlignmentOptions.MidlineRight, new Vector2(110f, 24f), new Vector2(105f, 36f), Text);
        AddText("Text_Stacks", root.transform, "4 stacks / 1 unlocked skill each", 14, TextAlignmentOptions.MidlineLeft, new Vector2(250f, 24f), new Vector2(-8f, 2f), Text);
        AddText("Text_Status", root.transform, "READY", 14, TextAlignmentOptions.MidlineRight, new Vector2(100f, 24f), new Vector2(102f, -34f), Gold);
        GameObject selected = CreatePanel("State_Selected", root.transform, new Vector2(340f, 114f), new Color(1f, 0.76f, 0.28f, 0.18f), null, false);
        Place(selected, Vector2.zero);
        return root;
    }

    private static GameObject BuildRouteNode(bool polished)
    {
        GameObject root = CreatePanel(polished ? "PRD_19_35_MissionRouteNode_Polished" : "PRD_19_35_MissionRouteNode", null, new Vector2(118f, 84f), polished ? new Color(0.30f, 0.17f, 0.07f, 1f) : new Color(0.24f, 0.14f, 0.07f, 1f), LoadSprite("Assets/Old_Paper_Gui/button/rect/button_rect0.png"), true);
        AddText("Text_Title", root.transform, "1 Battle", 14, TextAlignmentOptions.Center, new Vector2(100f, 22f), new Vector2(0f, 24f), Gold);
        AddText("Text_Type", root.transform, "Battle", 12, TextAlignmentOptions.Center, new Vector2(98f, 20f), new Vector2(0f, 2f), Text);
        AddText("Text_Risk", root.transform, "Low", 11, TextAlignmentOptions.Center, new Vector2(90f, 18f), new Vector2(0f, -22f), Text);
        GameObject selected = CreatePanel("State_Selected", root.transform, new Vector2(112f, 78f), new Color(1f, 0.78f, 0.24f, 0.22f), null, false);
        Place(selected, Vector2.zero);
        return root;
    }

    private static GameObject BuildCommandButton(bool polished)
    {
        GameObject root = CreatePanel(polished ? "PRD_19_35_CommandButton_Polished" : "PRD_19_35_CommandButton", null, new Vector2(154f, 52f), polished ? new Color(0.34f, 0.16f, 0.065f, 1f) : new Color(0.22f, 0.11f, 0.055f, 1f), LoadSprite("Assets/Classic_RPG_GUI/Parts/long_button.png"), true);
        AddText("Text_Label", root.transform, "COMMAND", 16, TextAlignmentOptions.Center, new Vector2(132f, 36f), Vector2.zero, Gold);
        return root;
    }

    private static PRD19_035_RandomStartRoutesMockupController CreateScriptOwner(Transform root)
    {
        GameObject scriptOwner = CreateRect("Script_PRD19_035_RandomStartRoutesMockupController", root, new Vector2(100f, 100f));
        Place(scriptOwner, new Vector2(-760f, 390f));
        return scriptOwner.AddComponent<PRD19_035_RandomStartRoutesMockupController>();
    }

    private static void WireController(PRD19_035_RandomStartRoutesMockupController controller, GameObject root, GameObject[] offers, GameObject[] nodes, GameObject reroll, GameObject begin)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty offerArray = serialized.FindProperty("offerBindings");
        offerArray.arraySize = offers.Length;
        for (int i = 0; i < offers.Length; i++)
        {
            SerializedProperty element = offerArray.GetArrayElementAtIndex(i);
            SetObject(element, "button", offers[i].GetComponent<Button>());
            SetObject(element, "background", offers[i].GetComponent<Image>());
            SetObject(element, "selectedFrame", ChildImage(offers[i], "State_Selected"));
            SetObject(element, "nameText", ChildText(offers[i], "Text_Name"));
            SetObject(element, "valueText", ChildText(offers[i], "Text_Value"));
            SetObject(element, "stackText", ChildText(offers[i], "Text_Stacks"));
            SetObject(element, "statusText", ChildText(offers[i], "Text_Status"));
            UnityEventTools.AddIntPersistentListener(offers[i].GetComponent<Button>().onClick, controller.SelectOffer, i);
        }

        SerializedProperty nodeArray = serialized.FindProperty("routeNodeBindings");
        nodeArray.arraySize = nodes.Length;
        for (int i = 0; i < nodes.Length; i++)
        {
            SerializedProperty element = nodeArray.GetArrayElementAtIndex(i);
            SetObject(element, "button", nodes[i].GetComponent<Button>());
            SetObject(element, "background", nodes[i].GetComponent<Image>());
            SetObject(element, "selectedFrame", ChildImage(nodes[i], "State_Selected"));
            SetObject(element, "titleText", ChildText(nodes[i], "Text_Title"));
            SetObject(element, "typeText", ChildText(nodes[i], "Text_Type"));
            SetObject(element, "riskText", ChildText(nodes[i], "Text_Risk"));
            UnityEventTools.AddIntPersistentListener(nodes[i].GetComponent<Button>().onClick, controller.SelectRouteNode, i);
        }

        SetObject(serialized, "selectedOfferText", FindText(root, "Text_SelectedOffer"));
        SetObject(serialized, "startingAssetsText", FindText(root, "Text_StartingAssets"));
        SetObject(serialized, "selectedRouteNodeText", FindText(root, "Text_SelectedRouteNode"));
        SetObject(serialized, "routeSummaryText", FindText(root, "Text_RouteSummary"));
        SetObject(serialized, "rerollPreviewButton", reroll.GetComponent<Button>());
        SetObject(serialized, "beginPreviewButton", begin.GetComponent<Button>());
        SetObject(serialized, "runtimeMessageText", FindText(root, "Text_RuntimeMessage"));
        UnityEventTools.AddPersistentListener(reroll.GetComponent<Button>().onClick, controller.HandleRerollPreviewClicked);
        UnityEventTools.AddPersistentListener(begin.GetComponent<Button>().onClick, controller.HandleBeginPreviewClicked);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddHeader(Transform root, string title, string subtitle)
    {
        GameObject header = CreatePanel("Section_Header", root, new Vector2(1500f, 82f), PanelDark, LoadSprite("Assets/Classic_RPG_GUI/Parts/big_bar.png"), false);
        Place(header, new Vector2(0f, 390f));
        AddText("Text_Title", header.transform, title, 34, TextAlignmentOptions.MidlineLeft, new Vector2(760f, 48f), new Vector2(-340f, 4f), Gold);
        AddText("Text_Subtitle", header.transform, subtitle, 17, TextAlignmentOptions.MidlineRight, new Vector2(650f, 40f), new Vector2(380f, 0f), Text);
    }

    private static void AddRule(Transform parent, string name, Vector2 position, string title, string body)
    {
        GameObject row = CreatePanel(name, parent, new Vector2(310f, 78f), new Color(0.10f, 0.075f, 0.055f, 0.92f), null, false);
        Place(row, position);
        AddText("Text_Title", row.transform, title, 17, TextAlignmentOptions.MidlineLeft, new Vector2(270f, 24f), new Vector2(0f, 18f), Gold);
        AddText("Text_Body", row.transform, body, 13, TextAlignmentOptions.TopLeft, new Vector2(270f, 36f), new Vector2(0f, -14f), Text);
    }

    private static void AddRouteLine(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject line = CreatePanel(name, parent, size, new Color(0.17f, 0.10f, 0.04f, 0.88f), null, false);
        Place(line, position);
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color, Sprite sprite, bool button)
    {
        GameObject root = CreateRect(name, parent, size);
        Image image = root.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
        image.raycastTarget = button;
        if (button)
        {
            root.AddComponent<Button>();
        }

        return root;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }

        return root;
    }

    private static TextMeshProUGUI AddText(string name, Transform parent, string value, int size, TextAlignmentOptions alignment, Vector2 rectSize, Vector2 position, Color color)
    {
        GameObject textObject = CreateRect(name, parent, rectSize);
        Place(textObject, position);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private static GameObject CreateVerticalList(string name, Transform parent, Vector2 size, Vector2 position, float spacing)
    {
        GameObject list = CreateRect(name, parent, size);
        Place(list, position);
        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return list;
    }

    private static GameObject InstantiateNested(string assetPath, string instanceName, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        GameObject instance = prefab == null ? null : PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            instance = CreatePanel(instanceName, parent, new Vector2(120f, 60f), Panel, null, true);
        }
        else
        {
            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
        }

        return instance;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string folder)
    {
        string normalized = folder.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void Place(GameObject obj, Vector2 position)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void AddLayout(GameObject obj, float width, float height)
    {
        LayoutElement layout = obj.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = obj.AddComponent<LayoutElement>();
        }

        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TextMeshProUGUI ChildText(GameObject root, string childName)
    {
        Transform child = root.transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private static Image ChildImage(GameObject root, string childName)
    {
        Transform child = root.transform.Find(childName);
        return child == null ? null : child.GetComponent<Image>();
    }

    private static TMP_Text FindText(GameObject root, string name)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == name)
            {
                return texts[i];
            }
        }

        return null;
    }

    private static void SetButtonLabel(GameObject button, string label)
    {
        TMP_Text text = FindText(button, "Text_Label");
        if (text != null)
        {
            text.text = label;
        }
    }

    private static void SetNodeLabels(GameObject node, string title, string type, string risk)
    {
        TMP_Text titleText = FindText(node, "Text_Title");
        TMP_Text typeText = FindText(node, "Text_Type");
        TMP_Text riskText = FindText(node, "Text_Risk");
        if (titleText != null) titleText.text = title;
        if (typeText != null) typeText.text = type;
        if (riskText != null) riskText.text = risk;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObject(SerializedProperty parent, string propertyName, Object value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
