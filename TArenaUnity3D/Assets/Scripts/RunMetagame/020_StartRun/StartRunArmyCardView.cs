using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StartRunArmyCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject locked;
    [SerializeField] private TMP_Text lockedReasonText;
    [SerializeField] private Transform stackRowsParent;

    private readonly List<StackRepresentation> stackRows = new List<StackRepresentation>();

    public string TemplateId { get; private set; }

    public Button Button
    {
        get { return button; }
    }

    public void Bind(StartingArmyOptionViewData option, bool isSelected, DataMapper dataMapper)
    {
        bool hasData = option != null;
        TemplateId = hasData ? option.TemplateId : string.Empty;

        gameObject.SetActive(hasData);
        if (!hasData)
        {
            return;
        }

        if (nameText != null)
        {
            nameText.text = option.DisplayName.ToUpperInvariant();
        }

        if (valueText != null)
        {
            valueText.text = option.TotalArmyValue.ToString();
        }

        if (statusText != null)
        {
            statusText.text = option.IsLocked ? "LOCKED" : (isSelected ? "SELECTED" : "READY");
            statusText.color = option.IsLocked
                ? new Color(0.74f, 0.72f, 0.68f, 1f)
                : (isSelected ? new Color(0.62f, 1f, 0.42f, 1f) : new Color(0.9f, 0.78f, 0.42f, 1f));
        }

        if (background != null)
        {
            background.color = isSelected
                ? new Color(0.72f, 0.34f, 0.13f, 1f)
                : new Color(0.33f, 0.18f, 0.09f, 1f);
        }

        if (locked != null)
        {
            locked.SetActive(option.IsLocked);
        }

        if (lockedReasonText != null)
        {
            lockedReasonText.text = option.IsLocked ? option.LockedReason : string.Empty;
        }

        if (button != null)
        {
            button.interactable = option.CanStartRun;
            button.enabled = option.CanStartRun;
        }

        BindStackRepresentations(option, dataMapper);
    }

    private void BindStackRepresentations(
        StartingArmyOptionViewData option,
        DataMapper dataMapper)
    {
        EnsureStackRows();

        for (int i = 0; i < stackRows.Count; i++)
        {
            StackRepresentation row = stackRows[i];
            if (row == null)
            {
                continue;
            }

            StartRunStackViewData stack = option.Stacks != null && i < option.Stacks.Count
                ? option.Stacks[i]
                : null;
            row.DisplayStackInfo(RunMetagameDisplayInfoFactory.FromStartRun(stack, dataMapper));
        }
    }

    private void EnsureStackRows()
    {
        if (stackRows.Count > 0 || stackRowsParent == null)
        {
            return;
        }

        StackRepresentation[] rows = stackRowsParent.GetComponentsInChildren<StackRepresentation>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
            {
                stackRows.Add(rows[i]);
            }
        }
    }
}
