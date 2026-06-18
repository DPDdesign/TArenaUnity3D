using UnityEngine;
using UnityEngine.UI;

public class RunMapRouteEdgeRepresentation : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image background;
    [SerializeField] private Image fill;
    [SerializeField] private Image glow;

    [Header("State Colors")]
    [SerializeField] private Color inactiveColor = new Color(0.16f, 0.15f, 0.13f, 0.85f);
    [SerializeField] private Color passedColor = new Color(0.95f, 0.72f, 0.32f, 1f);
    [SerializeField] private Color availableColor = new Color(1f, 0.78f, 0.34f, 1f);
    [SerializeField] private Color availableGlowColor = new Color(1f, 0.78f, 0.34f, 0.35f);

    [Header("Fill")]
    [SerializeField] private float availableFillAmount = 0.5f;

    public Image Background
    {
        get { return background; }
        set { background = value; }
    }

    public Image Fill
    {
        get { return fill; }
        set { fill = value; }
    }

    public Image Glow
    {
        get { return glow; }
        set { glow = value; }
    }

    public Color InactiveColor
    {
        get { return inactiveColor; }
        set { inactiveColor = value; }
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

    public Color AvailableGlowColor
    {
        get { return availableGlowColor; }
        set { availableGlowColor = value; }
    }

    public float AvailableFillAmount
    {
        get { return availableFillAmount; }
        set { availableFillAmount = Mathf.Clamp01(value); }
    }

    public void Bind(RunMapNodeViewData sourceNode, RunMapNodeViewData targetNode)
    {
        ConfigureFillImage();
        SetImageColor(background, inactiveColor);

        if (!EdgeSourceAllowsProgress(sourceNode) || targetNode == null)
        {
            SetFill(0f, availableColor);
            SetGlow(false);
            return;
        }

        if (targetNode.State == RunMapNodeState.Completed)
        {
            SetFill(1f, passedColor);
            SetGlow(false);
            return;
        }

        if (targetNode.State == RunMapNodeState.Available || targetNode.CanTravel)
        {
            SetFill(Mathf.Clamp01(availableFillAmount), availableColor);
            SetGlow(true);
            return;
        }

        SetFill(0f, availableColor);
        SetGlow(false);
    }

    private static bool EdgeSourceAllowsProgress(RunMapNodeViewData sourceNode)
    {
        return sourceNode != null && sourceNode.State == RunMapNodeState.Completed;
    }

    private void Reset()
    {
        background = GetComponent<Image>();
        ConfigureFillImage();
    }

    private void OnValidate()
    {
        availableFillAmount = Mathf.Clamp01(availableFillAmount);
        ConfigureFillImage();
    }

    private void ConfigureFillImage()
    {
        if (fill == null)
        {
            return;
        }

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
    }

    private void SetFill(float fillAmount, Color color)
    {
        if (fill == null)
        {
            return;
        }

        fill.fillAmount = Mathf.Clamp01(fillAmount);
        fill.color = color;
        fill.gameObject.SetActive(fillAmount > 0f);
    }

    private void SetGlow(bool active)
    {
        if (glow == null)
        {
            return;
        }

        glow.gameObject.SetActive(active);
        if (active)
        {
            glow.color = availableGlowColor;
        }
    }

    private static void SetImageColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }
}
