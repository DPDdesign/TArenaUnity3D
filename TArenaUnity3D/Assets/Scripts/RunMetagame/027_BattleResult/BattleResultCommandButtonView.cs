using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultCommandButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;

    public Button Button
    {
        get { return button; }
    }

    public void SetLabel(string label)
    {
        if (labelText != null)
        {
            labelText.text = label ?? string.Empty;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
