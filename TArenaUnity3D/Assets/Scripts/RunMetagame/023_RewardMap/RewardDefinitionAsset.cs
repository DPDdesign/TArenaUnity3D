using UnityEngine;

[CreateAssetMenu(fileName = "RewardDefinition", menuName = "TArena/Run Metagame/Reward Definition")]
public class RewardDefinitionAsset : ScriptableObject
{
    [SerializeField] private string rewardCatalogId;
    [SerializeField] private RewardMapFamily family;
    [SerializeField] private RewardMapIntention intention;
    [SerializeField] private RewardMapRarity rarity;
    [SerializeField] private string verb;
    [SerializeField] private string title;
    [SerializeField] private string detail;
    [SerializeField] private RewardMapOperationType operationType;
    [SerializeField] private string stackId;
    [SerializeField] private string unitId;
    [SerializeField] private string toUnitId;
    [SerializeField] private string skillId;
    [SerializeField] private string newStackId;
    [SerializeField] private int amount;
    [SerializeField] private int currencyDelta;

    public string RewardCatalogId { get { return rewardCatalogId; } }
    public RewardMapFamily Family { get { return family; } }
    public RewardMapIntention Intention { get { return intention; } }
    public RewardMapRarity Rarity { get { return rarity; } }
    public string Verb { get { return verb; } }
    public string Title { get { return title; } }
    public string Detail { get { return detail; } }
    public RewardMapOperationType OperationType { get { return operationType; } }
    public string StackId { get { return stackId; } }
    public string UnitId { get { return unitId; } }
    public string ToUnitId { get { return toUnitId; } }
    public string SkillId { get { return skillId; } }
    public string NewStackId { get { return newStackId; } }
    public int Amount { get { return Mathf.Max(0, amount); } }
    public int CurrencyDelta { get { return currencyDelta; } }

    public RewardMapTemplate ToTemplate()
    {
        return new RewardMapTemplate(
            rewardCatalogId,
            family,
            intention,
            rarity,
            verb,
            title,
            detail,
            new RewardMapOperation(
                operationType,
                stackId,
                unitId,
                toUnitId,
                skillId,
                newStackId,
                amount,
                currencyDelta));
    }

#if UNITY_EDITOR
    public void Configure(RewardMapTemplate template)
    {
        if (template == null)
        {
            return;
        }

        rewardCatalogId = template.TemplateId;
        family = template.Family;
        intention = template.Intention;
        rarity = template.Rarity;
        verb = template.Verb;
        title = template.Title;
        detail = template.Detail;

        RewardMapOperation operation = template.Operation;
        operationType = operation == null ? RewardMapOperationType.AddUnits : operation.Type;
        stackId = operation == null ? string.Empty : operation.StackId;
        unitId = operation == null ? string.Empty : operation.UnitId;
        toUnitId = operation == null ? string.Empty : operation.ToUnitId;
        skillId = operation == null ? string.Empty : operation.SkillId;
        newStackId = operation == null ? string.Empty : operation.NewStackId;
        amount = operation == null ? 0 : operation.Amount;
        currencyDelta = operation == null ? 0 : operation.CurrencyDelta;
    }
#endif
}
