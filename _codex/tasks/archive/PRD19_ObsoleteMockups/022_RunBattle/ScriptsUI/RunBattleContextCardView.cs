using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunBattleContextCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI riskText;
    [SerializeField] private TextMeshProUGUI encounterText;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private GameObject selectedState;

    public Button Button
    {
        get { return button; }
    }

    public void Configure(
        Button button,
        TextMeshProUGUI titleText,
        TextMeshProUGUI typeText,
        TextMeshProUGUI riskText,
        TextMeshProUGUI encounterText,
        TextMeshProUGUI goalText,
        GameObject selectedState)
    {
        this.button = button;
        this.titleText = titleText;
        this.typeText = typeText;
        this.riskText = riskText;
        this.encounterText = encounterText;
        this.goalText = goalText;
        this.selectedState = selectedState;
    }

    public void Bind(RunBattleEncounterDefinition encounter, bool selected)
    {
        bool hasEncounter = encounter != null;
        gameObject.SetActive(hasEncounter);
        if (!hasEncounter)
        {
            return;
        }

        SetText(titleText, encounter.DisplayName);
        SetText(typeText, encounter.NodeType.ToString());
        SetText(riskText, "Risk: " + encounter.ExpectedRisk);
        SetText(encounterText, "Encounter: " + encounter.EncounterId);
        SetText(goalText, "Goal: " + encounter.EnemyGoal);
        SetActive(selectedState, selected);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
