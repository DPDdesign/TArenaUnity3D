#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunMapNodeRepresentationTests
{
    private GameObject root;
    private RunMapNodeRepresentation representation;
    private Button button;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI typeText;
    private TextMeshProUGUI stateText;
    private TextMeshProUGUI nodeIdText;
    private Image background;
    private Image icon;
    private Image selectionFrame;
    private Image lockedOverlay;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("RunMapNode", typeof(RectTransform), typeof(Image), typeof(Button));
        background = root.GetComponent<Image>();
        button = root.GetComponent<Button>();
        representation = root.AddComponent<RunMapNodeRepresentation>();
        titleText = CreateText("Title");
        typeText = CreateText("Type");
        stateText = CreateText("State");
        nodeIdText = CreateText("NodeId");
        icon = CreateImage("Icon_NodeType");
        selectionFrame = CreateImage("Selection");
        lockedOverlay = CreateImage("Locked");

        representation.Button = button;
        representation.Background = background;
        representation.Icon = icon;
        representation.TitleText = titleText;
        representation.TypeText = typeText;
        representation.StateText = stateText;
        representation.NodeIdText = nodeIdText;
        representation.SelectionFrame = selectionFrame;
        representation.LockedOverlay = lockedOverlay;
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Bind_WithAvailableNode_StoresRuntimeNodeIdAndUpdatesOptionalVisuals()
    {
        RunMapNodeViewData node = Node("node-recovery-1", RunMapNodeType.Shop, RunMapNodeState.Available, true);

        representation.Bind(node, true);

        Assert.That(root.activeSelf, Is.True);
        Assert.That(representation.NodeId, Is.EqualTo("node-recovery-1"));
        Assert.That(titleText.text, Is.EqualTo("Recovery Shop"));
        Assert.That(typeText.text, Is.EqualTo("Shop"));
        Assert.That(stateText.text, Is.EqualTo("Available"));
        Assert.That(nodeIdText.text, Is.EqualTo("node-recovery-1"));
        Assert.That(selectionFrame.gameObject.activeSelf, Is.True);
        Assert.That(lockedOverlay.gameObject.activeSelf, Is.False);
        Assert.That(button.interactable, Is.True);
    }

    [Test]
    public void Bind_WithNullNode_HidesSlotAndClearsRuntimeNodeId()
    {
        representation.Bind(Node("node-pressure-1", RunMapNodeType.Battle, RunMapNodeState.Available, true), false);

        representation.Bind(null, false);

        Assert.That(root.activeSelf, Is.False);
        Assert.That(representation.NodeId, Is.EqualTo(string.Empty));
        Assert.That(button.interactable, Is.False);
    }

    [Test]
    public void Bind_WhenCurrentNode_DoesNotShowStickyActiveState()
    {
        RunMapNodeViewData node = Node("node-pressure-1", RunMapNodeType.Battle, RunMapNodeState.Completed, false);

        representation.Bind(node, false);

        Assert.That(stateText.text, Is.EqualTo("Completed"));
        Assert.That(selectionFrame.gameObject.activeSelf, Is.False);
        Assert.That(button.interactable, Is.False);
    }

    [Test]
    public void Bind_UsesConfiguredStateColors()
    {
        Color hover = new Color(0.9f, 0.7f, 0.1f, 1f);
        Color passed = new Color(0.1f, 0.8f, 0.2f, 1f);
        Color available = new Color(0.8f, 0.1f, 0.1f, 1f);
        Color locked = new Color(0.1f, 0.1f, 0.1f, 1f);

        representation.HoverColor = hover;
        representation.PassedColor = passed;
        representation.AvailableColor = available;
        representation.LockedColor = locked;

        representation.Bind(Node("node-hover", RunMapNodeType.Battle, RunMapNodeState.Available, true), true);
        Assert.That(background.color, Is.EqualTo(hover));

        representation.Bind(Node("node-passed", RunMapNodeType.Battle, RunMapNodeState.Completed, false), false);
        Assert.That(background.color, Is.EqualTo(passed));

        representation.Bind(Node("node-available", RunMapNodeType.Battle, RunMapNodeState.Available, true), false);
        Assert.That(background.color, Is.EqualTo(available));

        representation.Bind(Node("node-locked", RunMapNodeType.Battle, RunMapNodeState.Locked, false), false);
        Assert.That(background.color, Is.EqualTo(locked));
    }

    [Test]
    public void Bind_WithUnassignedVisuals_DoesNotThrow()
    {
        GameObject minimalRoot = new GameObject("MinimalRunMapNode", typeof(RectTransform));
        RunMapNodeRepresentation minimal = minimalRoot.AddComponent<RunMapNodeRepresentation>();

        Assert.DoesNotThrow(() => minimal.Bind(Node("node-final", RunMapNodeType.FinalBoss, RunMapNodeState.Locked, false), false));

        Object.DestroyImmediate(minimalRoot);
    }

    [Test]
    public void Bind_UsesNodeTypeIconCatalogAndFallsBackToAssignedIconSprite()
    {
        Sprite fallbackSprite = CreateTestSprite("fallback");
        Sprite battleSprite = CreateTestSprite("battle");
        icon.sprite = fallbackSprite;

        RunMapNodeTypeIconCatalog catalog = ScriptableObject.CreateInstance<RunMapNodeTypeIconCatalog>();
        catalog.Entries = new System.Collections.Generic.List<RunMapNodeTypeIconEntry>
        {
            new RunMapNodeTypeIconEntry(RunMapNodeType.Battle, battleSprite)
        };
        representation.NodeTypeIconCatalog = catalog;

        representation.Bind(Node("node-pressure-1", RunMapNodeType.Battle, RunMapNodeState.Available, true), false);
        Assert.That(icon.sprite, Is.EqualTo(battleSprite));

        representation.Bind(Node("node-recovery-2", RunMapNodeType.Shop, RunMapNodeState.Available, true), false);
        Assert.That(icon.sprite, Is.EqualTo(fallbackSprite));

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(battleSprite);
        Object.DestroyImmediate(fallbackSprite);
    }

    [Test]
    public void Hover_EmitsBoundRuntimeNodeId()
    {
        string hoveredNodeId = string.Empty;
        representation.HoverRequested = nodeId => hoveredNodeId = nodeId;
        representation.Bind(Node("node-pivot-1", RunMapNodeType.RecruitReward, RunMapNodeState.Available, true), false);

        representation.OnPointerEnter(null);

        Assert.That(hoveredNodeId, Is.EqualTo("node-pivot-1"));
    }

    [Test]
    public void HoverExit_EmitsBoundRuntimeNodeId()
    {
        string endedNodeId = string.Empty;
        representation.HoverEnded = nodeId => endedNodeId = nodeId;
        representation.Bind(Node("node-pivot-1", RunMapNodeType.RecruitReward, RunMapNodeState.Available, true), false);

        representation.OnPointerExit(null);

        Assert.That(endedNodeId, Is.EqualTo("node-pivot-1"));
    }

    [Test]
    public void Click_WhenAvailable_EmitsBoundRuntimeNodeId()
    {
        string clickedNodeId = string.Empty;
        representation.ClickRequested = nodeId => clickedNodeId = nodeId;
        representation.Bind(Node("node-pressure-1", RunMapNodeType.Battle, RunMapNodeState.Available, true), false);

        button.onClick.Invoke();

        Assert.That(clickedNodeId, Is.EqualTo("node-pressure-1"));
    }

    [Test]
    public void Click_WhenLocked_DoesNotEmitCallback()
    {
        int clickCount = 0;
        representation.ClickRequested = nodeId => clickCount++;
        representation.Bind(Node("node-final", RunMapNodeType.FinalBoss, RunMapNodeState.Locked, false), false);

        button.onClick.Invoke();

        Assert.That(button.interactable, Is.False);
        Assert.That(clickCount, Is.EqualTo(0));
    }

    private TextMeshProUGUI CreateText(string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(root.transform, false);
        return child.AddComponent<TextMeshProUGUI>();
    }

    private Image CreateImage(string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(root.transform, false);
        return child.AddComponent<Image>();
    }

    private static Sprite CreateTestSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = name + "_texture";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }

    private static RunMapNodeViewData Node(string nodeId, RunMapNodeType type, RunMapNodeState state, bool canTravel)
    {
        return new RunMapNodeViewData(
            nodeId,
            "path-test",
            type,
            state,
            1,
            "Recovery Shop",
            "Reward hint",
            "Risk hint",
            "enc-test",
            canTravel);
    }
}
#endif
