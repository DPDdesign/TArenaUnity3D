using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PRD19_020_StartRunCampaingSelectionPrefabBuilder
{
    private const string BuildSessionKey = "TArena.PRD19_020.StartAssetsCampaingSelectionBuilt.v2";
    private const string BaseFolder = "Assets/Resources/UI/PRD_19/020_StartRun";
    private const string PrefabFolder = BaseFolder + "/Prefabs";
    private const string StartAssetsPath = PrefabFolder + "/StartAssets.prefab";
    private const string CampaingSelectionPath = BaseFolder + "/CampaingSelection.prefab";
    private const string RouteOptionPath = PrefabFolder + "/PRD19_020_RouteOption_Polished.prefab";

    private static readonly Color Surface = new Color(0.04f, 0.03f, 0.02f, 1f);
    private static readonly Color Panel = new Color(0.08f, 0.055f, 0.035f, 0.96f);
    private static readonly Color Raised = new Color(0.12f, 0.075f, 0.045f, 0.98f);
    private static readonly Color Inset = new Color(0.02f, 0.016f, 0.012f, 0.9f);
    private static readonly Color Gold = new Color(0.94f, 0.77f, 0.50f, 1f);
    private static readonly Color Text = new Color(0.96f, 0.83f, 0.65f, 1f);
    private static readonly Color Muted = new Color(0.78f, 0.61f, 0.44f, 1f);

    static PRD19_020_StartRunCampaingSelectionPrefabBuilder()
    {
        EditorApplication.delayCall += BuildOnceAfterCompile;
    }

    [MenuItem("TArena/Mockups/Rebuild PRD 19 020 Start Assets And Campaign Selection Prefabs")]
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
        EnsureFolder(BaseFolder);
        EnsureFolder(PrefabFolder);

        SavePrefab(BuildStartAssetsPrefab(), StartAssetsPath);
        SavePrefab(BuildCampaingSelectionPrefab(), CampaingSelectionPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt PRD 19 020 StartAssets and CampaingSelection UI prefabs.");
    }

    private static GameObject BuildStartAssetsPrefab()
    {
        GameObject root = CreatePanel("StartAssets", null, new Vector2(430f, 650f), Panel);
        StartAssetsView view = CreateScriptOwner<StartAssetsView>("Script_StartAssetsView", root.transform);
        Place(view.gameObject, Vector2.zero);

        AddFramedText("Text_StartingAssetsTitle", root.transform, "STARTING ASSETS", 28, TextAlignmentOptions.Center, new Vector2(350f, 42f), new Vector2(0f, 285f), Gold);
        AddFramedText("Text_StartingAssetsHint", root.transform, "Assets committed when the route is started", 15, TextAlignmentOptions.Center, new Vector2(350f, 46f), new Vector2(0f, 238f), Muted);

        TMP_Text runGold = AddAssetRow(root.transform, "Asset_RunStartingGold", new Vector2(0f, 130f), "RUN STARTING GOLD", "0 RUN GOLD");
        TMP_Text rollTokens = AddAssetRow(root.transform, "Asset_RunRollTokens", new Vector2(0f, 14f), "RUN ROLL TOKENS", "0 ROLL TOKENS");
        TMP_Text battleSkip = AddAssetRow(root.transform, "Asset_BattleSkipTokens", new Vector2(0f, -102f), "BATTLE SKIP TOKENS", "0 BATTLE SKIP TOKENS");
        AddFramedText("Text_StartingAssetsNote", root.transform, "Roll and skip tokens are displayed as explicit run-start values. Current PRD data keeps them at 0.", 16, TextAlignmentOptions.TopLeft, new Vector2(350f, 92f), new Vector2(0f, -238f), Muted);

        SerializedObject serialized = new SerializedObject(view);
        SetObject(serialized, "runStartingGoldText", runGold);
        SetObject(serialized, "runRollTokensText", rollTokens);
        SetObject(serialized, "battleSkipTokensText", battleSkip);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject BuildCampaingSelectionPrefab()
    {
        GameObject root = CreatePanel("CampaingSelection", null, new Vector2(1600f, 900f), Surface);
        CampaingSelectionScreenController controller = CreateScriptOwner<CampaingSelectionScreenController>("Script_CampaingSelectionScreenController", root.transform);

        AddHeader(root.transform, "CAMPAIGN SELECTION", "Choose the route preview, then begin the offline run.");

        GameObject left = CreatePanel("Section_RouteChoices", root.transform, new Vector2(530f, 650f), Panel);
        Place(left, new Vector2(-470f, -15f));
        AddFramedText("Text_RouteChoicesTitle", left.transform, "ROUTE PREVIEW", 28, TextAlignmentOptions.Center, new Vector2(400f, 42f), new Vector2(0f, 285f), Gold);
        AddFramedText("Text_RouteChoicesHint", left.transform, "Pick one of three campaign routes", 16, TextAlignmentOptions.Center, new Vector2(390f, 30f), new Vector2(0f, 244f), Muted);

        GameObject routeList = CreateVerticalList("List_RouteOptions", left.transform, new Vector2(460f, 500f), new Vector2(0f, -34f), 14f);
        List<StartRunRouteOptionView> routeOptions = new List<StartRunRouteOptionView>();
        string[] routeNames = { "IronLine", "RelicTrail", "RiskRoad" };
        for (int i = 0; i < 3; i++)
        {
            GameObject route = InstantiateNestedPrefab(RouteOptionPath, "RouteOption_" + routeNames[i], routeList.transform);
            LayoutElement layout = route.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = route.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = 430f;
            layout.preferredHeight = 142f;
            routeOptions.Add(route.GetComponent<StartRunRouteOptionView>());
        }

        GameObject right = CreatePanel("Section_SelectedRouteDetails", root.transform, new Vector2(660f, 650f), Panel);
        Place(right, new Vector2(360f, -15f));
        TMP_Text selectedArmy = AddFramedText("Text_SelectedStartingArmy", right.transform, "STARTING ARMY: NONE", 21, TextAlignmentOptions.MidlineLeft, new Vector2(540f, 44f), new Vector2(0f, 270f), Gold);
        AddFramedText("Text_SelectedRouteTitle", right.transform, "SELECTED ROUTE", 28, TextAlignmentOptions.Center, new Vector2(450f, 42f), new Vector2(0f, 214f), Gold);
        TMP_Text routeSummary = AddFramedText("Text_RouteSummary", right.transform, "Select a route.", 23, TextAlignmentOptions.TopLeft, new Vector2(540f, 220f), new Vector2(0f, 58f), Text);

        GameObject flow = CreatePanel("Section_FlowPreview", right.transform, new Vector2(540f, 200f), Raised);
        Place(flow, new Vector2(0f, -190f));
        AddFramedText("Text_FlowTitle", flow.transform, "RUN FLOW", 22, TextAlignmentOptions.Center, new Vector2(440f, 34f), new Vector2(0f, 66f), Gold);
        AddFramedText("Text_FlowBody", flow.transform, "Begin Run creates the persisted offline run, starting army snapshot, route map, paths, and route nodes through PRD030 DB composition.", 18, TextAlignmentOptions.TopLeft, new Vector2(470f, 104f), new Vector2(0f, -12f), Muted);

        BottomRefs bottom = AddBottomBar(root.transform, "BEGIN RUN", controller.HandleBackButtonClicked, controller.HandleBeginButtonClicked);
        TMP_Text message = AddFramedText("Text_CampaingSelectionRuntimeMessage", root.transform, "Run can be started.", 17, TextAlignmentOptions.Center, new Vector2(760f, 34f), new Vector2(0f, -340f), Muted);

        SerializedObject serialized = new SerializedObject(controller);
        SetObjectArray(serialized, "routeOptions", routeOptions.ToArray());
        SetObject(serialized, "selectedArmyText", selectedArmy);
        SetObject(serialized, "routeSummaryText", routeSummary);
        SetObject(serialized, "runtimeMessageText", message);
        SetObject(serialized, "backButton", bottom.BackButton);
        SetObject(serialized, "beginButton", bottom.PrimaryButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static TMP_Text AddAssetRow(Transform parent, string name, Vector2 position, string label, string value)
    {
        GameObject row = CreatePanel(name, parent, new Vector2(350f, 86f), Raised);
        Place(row, position);
        AddFramedText("Text_Label", row.transform, label, 16, TextAlignmentOptions.MidlineLeft, new Vector2(300f, 26f), new Vector2(0f, 20f), Muted);
        return AddFramedText("Text_Value", row.transform, value, 25, TextAlignmentOptions.MidlineLeft, new Vector2(300f, 36f), new Vector2(0f, -16f), Gold);
    }

    private static void AddHeader(Transform root, string title, string subtitle)
    {
        GameObject header = CreatePanel("Section_Header", root, new Vector2(1500f, 82f), Raised);
        Place(header, new Vector2(0f, 390f));
        AddFramedText("Text_Title", header.transform, title, 40, TextAlignmentOptions.MidlineLeft, new Vector2(700f, 56f), new Vector2(-350f, 0f), Gold);
        AddFramedText("Text_Subtitle", header.transform, subtitle, 18, TextAlignmentOptions.MidlineRight, new Vector2(660f, 44f), new Vector2(380f, 0f), Muted);
    }

    private static BottomRefs AddBottomBar(Transform root, string primaryLabel, UnityEngine.Events.UnityAction backAction, UnityEngine.Events.UnityAction primaryAction)
    {
        GameObject bottom = CreatePanel("Section_BottomCommands", root, new Vector2(1500f, 86f), Raised);
        Place(bottom, new Vector2(0f, -390f));

        Button back = CreateButton("Button_Back", bottom.transform, "BACK", new Vector2(180f, 56f), new Vector2(-600f, 0f), false);
        Button primary = CreateButton("Button_Primary", bottom.transform, primaryLabel, new Vector2(260f, 60f), new Vector2(570f, 0f), true);
        UnityEventTools.AddPersistentListener(back.onClick, backAction);
        UnityEventTools.AddPersistentListener(primary.onClick, primaryAction);
        return new BottomRefs(back, primary);
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 position, bool primary)
    {
        GameObject root = CreatePanel(name, parent, size, primary ? new Color(0.22f, 0.12f, 0.055f, 1f) : new Color(0.10f, 0.07f, 0.045f, 1f));
        Place(root, position);
        Button button = root.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary ? new Color(1f, 0.88f, 0.62f, 1f) : new Color(0.86f, 0.72f, 0.52f, 1f);
        colors.pressedColor = new Color(0.65f, 0.38f, 0.18f, 1f);
        colors.disabledColor = new Color(0.25f, 0.21f, 0.18f, 1f);
        button.colors = colors;
        AddFramedText("Text_Label", root.transform, label, 22, TextAlignmentOptions.Center, new Vector2(size.x - 24f, size.y - 16f), Vector2.zero, primary ? Gold : Text);
        return button;
    }

    private static T CreateScriptOwner<T>(string name, Transform parent) where T : Component
    {
        GameObject scriptOwner = CreateRect(name, parent, new Vector2(100f, 100f));
        Place(scriptOwner, new Vector2(-760f, 390f));
        return scriptOwner.AddComponent<T>();
    }

    private static GameObject CreateVerticalList(string name, Transform parent, Vector2 size, Vector2 position, float spacing)
    {
        GameObject list = CreateRect(name, parent, size);
        Place(list, position);
        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return list;
    }

    private static GameObject InstantiateNestedPrefab(string assetPath, string instanceName, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError("Missing nested prefab asset: " + assetPath);
            return CreatePanel(instanceName, parent, new Vector2(220f, 80f), Raised);
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

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject root = CreateRect(name, parent, size);
        Image image = root.AddComponent<Image>();
        image.color = color;
        image.sprite = LoadSprite("Assets/Classic_RPG_GUI/Parts/info_window.png");
        image.type = image.sprite == null ? Image.Type.Simple : Image.Type.Sliced;
        image.raycastTarget = false;
        return root;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        GameObject root = CreateRect(name, parent, size);
        Place(root, position);
        Image image = root.AddComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text AddFramedText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment, Vector2 frameSize, Vector2 position, Color color)
    {
        GameObject frame = CreateRect("Frame", parent, frameSize);
        Place(frame, position);
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = Inset;
        frameImage.raycastTarget = false;

        GameObject textObject = CreateRect(name, frame.transform, new Vector2(frameSize.x - 16f, frameSize.y - 8f));
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        root.layer = 5;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        return root;
    }

    private static void Place(GameObject target, Vector2 position)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
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

        property.arraySize = values == null ? 0 : values.Length;
        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private sealed class BottomRefs
    {
        public readonly Button BackButton;
        public readonly Button PrimaryButton;

        public BottomRefs(Button backButton, Button primaryButton)
        {
            BackButton = backButton;
            PrimaryButton = primaryButton;
        }
    }
}
