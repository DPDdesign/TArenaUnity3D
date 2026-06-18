using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunMapNodeRepresentation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string DefaultNodeTypeIconCatalogPath = "0_Data/RunMapNodeTypeIconCatalog";

    [Header("Interaction")]
    [SerializeField] private Button button;

    [Header("Catalog")]
    [SerializeField] private RunMapNodeTypeIconCatalog nodeTypeIconCatalog;

    [Header("Visuals")]
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private Image lockedOverlay;

    [Header("State Colors")]
    [SerializeField] private Color hoverColor = new Color(0.78f, 0.55f, 0.18f, 1f);
    [SerializeField] private Color passedColor = new Color(0.26f, 0.42f, 0.24f, 1f);
    [SerializeField] private Color availableColor = new Color(0.52f, 0.20f, 0.12f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.18f, 0.16f, 0.14f, 1f);

    [Header("Debug Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text nodeIdText;

    public Action<string> HoverRequested;
    public Action<string> HoverEnded;
    public Action<string> ClickRequested;

    private string nodeId;
    private bool canTravel;
    private Sprite fallbackIconSprite;
    private bool fallbackIconCaptured;

    public string NodeId
    {
        get { return nodeId; }
    }

    public Button Button
    {
        get { return button; }
        set { button = value; }
    }

    public Image Background
    {
        get { return background; }
        set { background = value; }
    }

    public Image Icon
    {
        get { return icon; }
        set { icon = value; }
    }

    public Image SelectionFrame
    {
        get { return selectionFrame; }
        set { selectionFrame = value; }
    }

    public Image LockedOverlay
    {
        get { return lockedOverlay; }
        set { lockedOverlay = value; }
    }

    public TMP_Text TitleText
    {
        get { return titleText; }
        set { titleText = value; }
    }

    public TMP_Text TypeText
    {
        get { return typeText; }
        set { typeText = value; }
    }

    public TMP_Text StateText
    {
        get { return stateText; }
        set { stateText = value; }
    }

    public TMP_Text NodeIdText
    {
        get { return nodeIdText; }
        set { nodeIdText = value; }
    }

    public RunMapNodeTypeIconCatalog NodeTypeIconCatalog
    {
        get { return nodeTypeIconCatalog; }
        set { nodeTypeIconCatalog = value; }
    }

    public Color HoverColor
    {
        get { return hoverColor; }
        set { hoverColor = value; }
    }

    public Color PassedColor
    {
        get { return passedColor; }
        set { passedColor = value; }
    }

    public Color AvailableColor
    {
        get { return availableColor; }
        set { availableColor = value; }
    }

    public Color LockedColor
    {
        get { return lockedColor; }
        set { lockedColor = value; }
    }

    private void Awake()
    {
        CaptureFallbackIcon();
        BindButton();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void Bind(RunMapNodeViewData node, bool focused)
    {
        bool hasNode = node != null;
        gameObject.SetActive(hasNode);

        if (!hasNode)
        {
            nodeId = string.Empty;
            canTravel = false;
            SetButtonInteractable(false);
            return;
        }

        nodeId = node.NodeId;
        canTravel = node.CanTravel;

        SetText(titleText, node.DisplayName);
        SetText(typeText, FormatNodeType(node.NodeType));
        SetText(stateText, FormatNodeState(node.State));
        SetText(nodeIdText, node.NodeId);
        SetIconSprite(ResolveNodeTypeIcon(node.NodeType));
        Color nodeColor = NodeColor(node, focused);
        SetImageColor(background, nodeColor);
        SetImageColor(icon, nodeColor);
        SetActive(selectionFrame, focused);
        SetActive(lockedOverlay, node.State == RunMapNodeState.Locked);
        SetButtonInteractable(node.CanTravel);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(nodeId) && HoverRequested != null)
        {
            HoverRequested(nodeId);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(nodeId) && HoverEnded != null)
        {
            HoverEnded(nodeId);
        }
    }

    private void BindButton()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void HandleButtonClicked()
    {
        if (canTravel && !string.IsNullOrEmpty(nodeId) && ClickRequested != null)
        {
            ClickRequested(nodeId);
        }
    }

    private void SetButtonInteractable(bool interactable)
    {
        BindButton();
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private Sprite ResolveNodeTypeIcon(RunMapNodeType nodeType)
    {
        CaptureFallbackIcon();

        RunMapNodeTypeIconCatalog catalog = ResolveNodeTypeIconCatalog();
        Sprite iconSprite = catalog == null ? null : catalog.FindIcon(nodeType);
        return iconSprite == null ? fallbackIconSprite : iconSprite;
    }

    private RunMapNodeTypeIconCatalog ResolveNodeTypeIconCatalog()
    {
        if (nodeTypeIconCatalog == null)
        {
            nodeTypeIconCatalog = Resources.Load<RunMapNodeTypeIconCatalog>(DefaultNodeTypeIconCatalogPath);
        }

        return nodeTypeIconCatalog;
    }

    private void CaptureFallbackIcon()
    {
        if (fallbackIconCaptured && fallbackIconSprite != null)
        {
            return;
        }

        if (icon != null && icon.sprite != null)
        {
            fallbackIconSprite = icon.sprite;
        }

        fallbackIconCaptured = true;
    }

    private void SetIconSprite(Sprite sprite)
    {
        if (icon == null)
        {
            return;
        }

        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.preserveAspect = true;
    }

    private Color NodeColor(RunMapNodeViewData node, bool focused)
    {
        if (focused)
        {
            return hoverColor;
        }

        if (node.State == RunMapNodeState.Completed)
        {
            return passedColor;
        }

        if (node.CanTravel)
        {
            return availableColor;
        }

        return lockedColor;
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = value ?? string.Empty;
        }
    }

    private static void SetImageColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void SetActive(Graphic graphic, bool active)
    {
        if (graphic != null)
        {
            graphic.gameObject.SetActive(active);
        }
    }

    private static string FormatNodeType(RunMapNodeType type)
    {
        switch (type)
        {
            case RunMapNodeType.Start:
                return "Start";
            case RunMapNodeType.Battle:
                return "Battle";
            case RunMapNodeType.Shop:
                return "Shop";
            case RunMapNodeType.RecruitReward:
                return "Recruit / Reward";
            case RunMapNodeType.FinalBoss:
                return "Final Battle";
            case RunMapNodeType.RandomEvent:
                return "Random Event";
            case RunMapNodeType.Empty:
                return "Empty";
            default:
                return type.ToString();
        }
    }

    private static string FormatNodeState(RunMapNodeState state)
    {
        switch (state)
        {
            case RunMapNodeState.Locked:
                return "Locked";
            case RunMapNodeState.Available:
                return "Available";
            case RunMapNodeState.Completed:
                return "Completed";
            case RunMapNodeState.Selected:
                return "Selected";
            default:
                return state.ToString();
        }
    }
}
