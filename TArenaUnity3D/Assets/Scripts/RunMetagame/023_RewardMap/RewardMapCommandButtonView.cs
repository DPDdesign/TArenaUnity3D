using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardMapCommandButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image frameImage;

    public Button Button
    {
        get { return button; }
    }

    public void Bind(string label, bool interactable, bool primary)
    {
        if (labelText != null)
        {
            labelText.text = string.IsNullOrEmpty(label) ? string.Empty : label.ToUpperInvariant();
            labelText.color = interactable
                ? (primary ? new Color(0.94f, 0.77f, 0.56f, 1f) : new Color(0.88f, 0.80f, 0.70f, 1f))
                : new Color(0.54f, 0.44f, 0.35f, 1f);
        }

        if (button != null)
        {
            button.interactable = interactable;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = !interactable
                ? new Color(0.22f, 0.16f, 0.11f, 0.80f)
                : primary
                    ? new Color(0.58f, 0.38f, 0.19f, 1f)
                    : new Color(0.25f, 0.19f, 0.14f, 0.96f);
        }

        if (frameImage != null)
        {
            frameImage.color = !interactable
                ? new Color(0.31f, 0.24f, 0.18f, 0.62f)
                : primary
                    ? new Color(0.72f, 0.54f, 0.28f, 1f)
                    : new Color(0.50f, 0.38f, 0.26f, 0.92f);
        }
    }
}
