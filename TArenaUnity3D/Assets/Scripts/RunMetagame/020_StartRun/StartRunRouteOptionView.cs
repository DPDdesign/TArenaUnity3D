using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StartRunRouteOptionView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text labelText;

    public string RouteId { get; private set; }

    public Button Button
    {
        get { return button; }
    }

    public void Bind(RoutePreviewViewData route, bool isSelected)
    {
        bool hasData = route != null;
        RouteId = hasData ? route.RouteId : string.Empty;

        gameObject.SetActive(hasData);
        if (!hasData)
        {
            return;
        }

        if (labelText != null)
        {
            labelText.text = route.DisplayName.ToUpperInvariant();
            labelText.color = isSelected
                ? new Color(0.35f, 0.1f, 0.02f, 1f)
                : new Color(0.17f, 0.06f, 0.015f, 1f);
        }

        if (background != null)
        {
            background.color = isSelected
                ? new Color(0.78f, 0.56f, 0.28f, 0.95f)
                : new Color(0.52f, 0.32f, 0.15f, 0.7f);
        }
    }
}
