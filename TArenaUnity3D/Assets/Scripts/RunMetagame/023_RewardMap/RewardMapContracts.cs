using System.Collections.Generic;

public interface IRewardMapUnitDefinitionSource
{
    RunShopUnitDefinition FindUnit(string unitId);
}

public interface IRewardMapUnitPoolSource : IRewardMapUnitDefinitionSource
{
    List<RunShopUnitDefinition> ListUnits();
}

public interface IRewardMapTemplateCatalog
{
    List<RewardMapTemplate> ListTemplates();
}

public interface IRewardMapChoiceStore
{
    RewardMapChoiceViewData SaveChoice(RewardMapChoiceViewData choice);
    RewardMapChoiceViewData FindChoice(string choiceId);
    RewardMapApplyResult SaveAppliedReward(string choiceId, RewardMapApplyResult result);
}

public interface IMaterializedRewardMapChoiceStore : IRewardMapChoiceStore
{
    RewardMapChoiceViewData FindChoiceForRunNode(string runId);
}

public class InMemoryRewardMapChoiceStore : IRewardMapChoiceStore
{
    private readonly List<RewardMapChoiceViewData> choices = new List<RewardMapChoiceViewData>();

    public RewardMapChoiceViewData SaveChoice(RewardMapChoiceViewData choice)
    {
        if (choice == null)
        {
            return null;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].ChoiceId == choice.ChoiceId)
            {
                choices[i] = choice;
                return choice;
            }
        }

        choices.Add(choice);
        return choice;
    }

    public RewardMapChoiceViewData FindChoice(string choiceId)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].ChoiceId == choiceId)
            {
                return choices[i];
            }
        }

        return null;
    }

    public RewardMapApplyResult SaveAppliedReward(string choiceId, RewardMapApplyResult result)
    {
        if (result == null || string.IsNullOrEmpty(choiceId))
        {
            return result;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            RewardMapChoiceViewData choice = choices[i];
            if (choice == null || choice.ChoiceId != choiceId)
            {
                continue;
            }

            choice.SelectedRewardId = result.Reward == null ? string.Empty : result.Reward.RewardId;
            return result;
        }

        return result;
    }
}
