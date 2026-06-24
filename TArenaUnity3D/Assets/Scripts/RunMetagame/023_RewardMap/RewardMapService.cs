using System;
using System.Collections.Generic;

public class RewardMapService
{
    private const string ResultSource = "offline-local-reward-resolver";

    private readonly IRewardMapTemplateCatalog templateCatalog;
    private readonly IRewardMapUnitDefinitionSource unitSource;
    private readonly IRewardMapChoiceStore choiceStore;

    public RewardMapService(IRewardMapTemplateCatalog templateCatalog, IRewardMapUnitDefinitionSource unitSource, IRewardMapChoiceStore choiceStore)
    {
        this.templateCatalog = templateCatalog;
        this.unitSource = unitSource;
        this.choiceStore = choiceStore;
    }

    public RewardMapChoiceViewData BuildChoice(RewardMapChoiceRequest request, string focusedRewardId)
    {
        if (request == null || request.ArmyAfterBattle == null)
        {
            return EmptyChoice(request, "Army after battle is missing.");
        }

        IMaterializedRewardMapChoiceStore materializedStore = choiceStore as IMaterializedRewardMapChoiceStore;
        if (materializedStore != null && OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(request.RunId) > 0)
        {
            RewardMapChoiceViewData materializedChoice = materializedStore.FindChoiceForRunNode(request.RunId);
            if (materializedChoice == null)
            {
                return EmptyChoice(request, "Materialized reward rows are missing for this run node.");
            }

            FocusChoice(materializedChoice, focusedRewardId);
            return materializedChoice;
        }

        RewardMapArmySnapshot army = CloneArmy(request.ArmyAfterBattle, request.ArmyAfterBattle.SnapshotId);
        List<RewardMapCardViewData> cards = BuildCards(army, request.StageIndex);
        RewardMapCardViewData focused = FindCard(cards, focusedRewardId);
        if (focused == null && cards.Count > 0)
        {
            focused = cards[0];
        }

        RewardMapPreviewData preview = focused == null
            ? BuildPreview(string.Empty, army, request.RunGoldBeforeReward, null, RewardMapError.MissingReward, "Select a reward.")
            : Preview(focused, army, request.RunGoldBeforeReward);

        RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
            "reward-choice-" + Guid.NewGuid().ToString("N"),
            request.RunId,
            RewardMapGameMode.Offline,
            RewardMapAuthoritySource.LocalOfflineAdapter,
            request.BattleResultSummary,
            BuildGainedSummary(request.BattleResultSummary),
            request.RunGoldBeforeReward,
            army,
            cards,
            focused,
            preview,
            preview.Message);

        return choiceStore == null ? choice : choiceStore.SaveChoice(choice);
    }

    private void FocusChoice(RewardMapChoiceViewData choice, string focusedRewardId)
    {
        if (choice == null)
        {
            return;
        }

        ResolveChoiceCards(choice);
        RewardMapCardViewData focused = FindCard(choice.Cards, focusedRewardId);
        if (focused == null && choice.Cards != null && choice.Cards.Count > 0)
        {
            focused = choice.Cards[0];
        }

        choice.FocusedCard = focused;
        choice.FocusedPreview = focused == null
            ? BuildPreview(string.Empty, choice.ArmyBeforeReward, choice.RunGoldBeforeReward, null, RewardMapError.MissingReward, "Select a reward.")
            : Preview(focused, choice.ArmyBeforeReward, choice.RunGoldBeforeReward);
        choice.Message = choice.FocusedPreview == null ? choice.Message : choice.FocusedPreview.Message;
    }

    private void ResolveChoiceCards(RewardMapChoiceViewData choice)
    {
        if (choice == null || choice.Cards == null)
        {
            return;
        }

        for (int i = 0; i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card == null)
            {
                continue;
            }

            if (!card.Legal && card.Error != RewardMapError.None)
            {
                card.AfterText = string.IsNullOrEmpty(card.AfterText) ? "No legal target" : card.AfterText;
                card.BeforeStackPreview = BuildBeforeStackPreview(choice.ArmyBeforeReward, card.Operation);
                card.AfterStackPreview = null;
                card.AffectedSlotIndex = ResolvePreviewSlotIndex(choice.ArmyBeforeReward, card.BeforeStackPreview, null);
                continue;
            }

            RewardMapOperation operation = ResolveTemplateOperation(card.Operation, choice.ArmyBeforeReward);
            RewardMapStackSnapshot beforeStackPreview = BuildBeforeStackPreview(choice.ArmyBeforeReward, operation);
            RewardMapArmySnapshot previewArmy = CloneArmy(choice.ArmyBeforeReward, "card-preview");
            int previewGold = 0;
            RewardMapStackSnapshot affected;
            RewardMapError error = ApplyOperation(previewArmy, operation, ref previewGold, out affected);
            card.Operation = operation;
            card.BeforeText = BuildBeforeText(choice.ArmyBeforeReward, operation);
            card.AfterText = error == RewardMapError.None ? BuildAfterText(previewArmy, operation, previewGold) : "No legal target";
            card.AffectedStackId = affected == null ? card.AffectedStackId : affected.StackId;
            card.Legal = error == RewardMapError.None;
            card.Error = error;
            card.BeforeStackPreview = beforeStackPreview;
            card.AfterStackPreview = error == RewardMapError.None ? affected : null;
            card.AffectedSlotIndex = ResolvePreviewSlotIndex(choice.ArmyBeforeReward, beforeStackPreview, affected);
        }
    }

    public RewardMapApplyResult Apply(RewardMapApplyCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.ChoiceId))
        {
            return Fail(RewardMapError.MissingChoice, "Reward choice is missing.", null, null, 0);
        }

        RewardMapChoiceViewData choice = choiceStore == null ? null : choiceStore.FindChoice(command.ChoiceId);
        if (choice == null)
        {
            return Fail(RewardMapError.MissingChoice, "Reward choice was not found.", null, command.ArmyBeforeReward, command.RunGoldBeforeReward);
        }

        if (!string.IsNullOrEmpty(choice.SelectedRewardId))
        {
            return Fail(RewardMapError.AlreadyApplied, "Reward was already applied.", null, command.ArmyBeforeReward, command.RunGoldBeforeReward);
        }

        RewardMapCardViewData card = FindCard(choice.Cards, command.RewardId);
        if (card == null)
        {
            return Fail(RewardMapError.MissingReward, "Reward card was not found.", null, command.ArmyBeforeReward, command.RunGoldBeforeReward);
        }

        if (command.ArmyBeforeReward == null)
        {
            return Fail(RewardMapError.MissingArmy, "Army before reward is missing.", card, null, command.RunGoldBeforeReward);
        }

        RewardMapPreviewData preview = Preview(card, command.ArmyBeforeReward, command.RunGoldBeforeReward);
        if (preview.Error != RewardMapError.None)
        {
            return Fail(preview.Error, preview.Message, card, command.ArmyBeforeReward, command.RunGoldBeforeReward);
        }

        RewardMapApplyResult result = new RewardMapApplyResult(true, RewardMapError.None, "Reward applied.", card, preview.ArmyAfterReward, preview.RunGoldAfterReward, ResultSource);
        return choiceStore == null ? result : choiceStore.SaveAppliedReward(command.ChoiceId, result);
    }

    private List<RewardMapCardViewData> BuildCards(RewardMapArmySnapshot army, int stageIndex)
    {
        List<RewardMapTemplate> templates = templateCatalog == null ? new List<RewardMapTemplate>() : templateCatalog.ListTemplates();
        List<RewardMapCardViewData> cards = new List<RewardMapCardViewData>();
        AddFirstLegal(cards, templates, army, RewardMapIntention.Stabilize, stageIndex);
        AddFirstLegal(cards, templates, army, RewardMapIntention.Strengthen, stageIndex);
        AddFirstLegal(cards, templates, army, RewardMapIntention.Pivot, stageIndex);
        return cards;
    }

    private void AddFirstLegal(List<RewardMapCardViewData> cards, List<RewardMapTemplate> templates, RewardMapArmySnapshot army, RewardMapIntention intention, int stageIndex)
    {
        for (int i = 0; i < templates.Count; i++)
        {
            RewardMapTemplate template = templates[i];
            if (template == null || template.Intention != intention)
            {
                continue;
            }

            RewardMapCardViewData card = BuildCard(template, army, stageIndex);
            if (card.Legal)
            {
                cards.Add(card);
                return;
            }
        }

        for (int i = 0; i < templates.Count; i++)
        {
            RewardMapTemplate template = templates[i];
            if (template != null && template.Intention == intention)
            {
                cards.Add(BuildCard(template, army, stageIndex));
                return;
            }
        }
    }

    private RewardMapCardViewData BuildCard(RewardMapTemplate template, RewardMapArmySnapshot army, int stageIndex)
    {
        RewardMapOperation operation = ResolveTemplateOperation(template.Operation, army);
        RewardMapStackSnapshot beforeStackPreview = BuildBeforeStackPreview(army, operation);
        RewardMapArmySnapshot previewArmy = CloneArmy(army, "card-preview");
        int previewGold = 0;
        RewardMapStackSnapshot affected;
        RewardMapError error = ApplyOperation(previewArmy, operation, ref previewGold, out affected);
        string beforeText = BuildBeforeText(army, operation);
        string afterText = error == RewardMapError.None ? BuildAfterText(previewArmy, operation, previewGold) : "No legal target";
        RewardMapCardViewData card = new RewardMapCardViewData(
            "reward-" + template.TemplateId,
            template.TemplateId,
            template.Family,
            template.Intention,
            template.Rarity,
            template.Verb,
            template.Title,
            template.Detail,
            beforeText,
            afterText,
            affected == null ? operation.StackId : affected.StackId,
            error == RewardMapError.None,
            error,
            operation);
        card.BeforeStackPreview = beforeStackPreview;
        card.AfterStackPreview = error == RewardMapError.None ? affected : null;
        card.AffectedSlotIndex = ResolvePreviewSlotIndex(army, beforeStackPreview, affected);
        return card;
    }

    private RewardMapOperation ResolveTemplateOperation(RewardMapOperation operation, RewardMapArmySnapshot army)
    {
        if (operation == null || string.IsNullOrEmpty(operation.UnitId))
        {
            return operation;
        }

        if (!OperationTargetsExistingStack(operation.Type))
        {
            return operation;
        }

        if (!string.IsNullOrEmpty(operation.StackId) && FindStack(army, operation.StackId) != null)
        {
            return operation;
        }

        RewardMapStackSnapshot stack = FindStackByUnit(army, operation.UnitId);
        if (stack == null || string.IsNullOrEmpty(stack.StackId))
        {
            return operation;
        }

        return new RewardMapOperation(
            operation.Type,
            stack.StackId,
            operation.UnitId,
            operation.ToUnitId,
            operation.SkillId,
            operation.NewStackId,
            operation.Amount,
            operation.CurrencyDelta);
    }

    private static bool OperationTargetsExistingStack(RewardMapOperationType type)
    {
        return type == RewardMapOperationType.AddUnits ||
            type == RewardMapOperationType.RecoverLosses ||
            type == RewardMapOperationType.TeachSkill ||
            type == RewardMapOperationType.PromoteStack ||
            type == RewardMapOperationType.DowngradeStack;
    }

    private RewardMapStackSnapshot BuildBeforeStackPreview(RewardMapArmySnapshot army, RewardMapOperation operation)
    {
        if (operation == null)
        {
            return null;
        }

        if (operation.Type == RewardMapOperationType.RecoverLosses && string.IsNullOrEmpty(operation.StackId))
        {
            return FindLargestLossStack(army);
        }

        if (!string.IsNullOrEmpty(operation.StackId))
        {
            return FindStack(army, operation.StackId);
        }

        return null;
    }

    private static int ResolvePreviewSlotIndex(RewardMapArmySnapshot army, RewardMapStackSnapshot before, RewardMapStackSnapshot after)
    {
        int beforeIndex = IndexOfStack(army, before == null ? string.Empty : before.StackId);
        if (beforeIndex >= 0)
        {
            return beforeIndex;
        }

        string afterStackId = after == null ? string.Empty : after.StackId;
        int afterIndex = IndexOfStack(army, afterStackId);
        if (afterIndex >= 0)
        {
            return afterIndex;
        }

        const string prefix = "slot-";
        if (!string.IsNullOrEmpty(afterStackId) && afterStackId.StartsWith(prefix))
        {
            int parsed;
            if (int.TryParse(afterStackId.Substring(prefix.Length), out parsed))
            {
                return parsed;
            }
        }

        return -1;
    }

    private static int IndexOfStack(RewardMapArmySnapshot army, string stackId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(stackId))
        {
            return -1;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.StackId == stackId)
            {
                return i;
            }
        }

        return -1;
    }

    private RewardMapPreviewData Preview(RewardMapCardViewData card, RewardMapArmySnapshot army, int runGold)
    {
        if (card == null)
        {
            return BuildPreview(string.Empty, army, runGold, null, RewardMapError.MissingReward, "Select a reward.");
        }

        if (!card.Legal && card.Error != RewardMapError.None)
        {
            return BuildPreview(
                card.RewardId,
                CloneArmy(army, "reward-preview-disabled"),
                runGold,
                null,
                card.Error,
                MessageFor(card.Error));
        }

        RewardMapOperation operation = ResolveTemplateOperation(card.Operation, army);
        card.Operation = operation;
        if (operation != null && !string.IsNullOrEmpty(operation.StackId))
        {
            card.AffectedStackId = operation.StackId;
        }

        RewardMapArmySnapshot previewArmy = CloneArmy(army, "reward-preview-" + Guid.NewGuid().ToString("N"));
        int currencyDelta = 0;
        RewardMapStackSnapshot affected;
        RewardMapError error = ApplyOperation(previewArmy, operation, ref currencyDelta, out affected);
        int goldAfter = Math.Max(0, runGold + currencyDelta);
        return BuildPreview(
            card.RewardId,
            previewArmy,
            goldAfter,
            affected,
            error,
            error == RewardMapError.None ? "Preview army after reward ready." : MessageFor(error));
    }

    private RewardMapError ApplyOperation(RewardMapArmySnapshot army, RewardMapOperation operation, ref int currencyDelta, out RewardMapStackSnapshot affected)
    {
        affected = null;
        if (army == null || operation == null)
        {
            return RewardMapError.InvalidTarget;
        }

        if (operation.Type == RewardMapOperationType.GainCurrency)
        {
            currencyDelta += operation.CurrencyDelta;
            return RewardMapError.None;
        }

        if (operation.Type == RewardMapOperationType.AddStack)
        {
            RunShopUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(operation.UnitId);
            if (unit == null)
            {
                return RewardMapError.InvalidTarget;
            }

            if (ArmyContainsUnit(army, unit.UnitId))
            {
                return RewardMapError.NoLegalTarget;
            }

            int formationSlot = RewardMapArmySlotRules.FindFirstFreeFormationSlot(army);
            if (formationSlot < 0)
            {
                return RewardMapError.NoLegalTarget;
            }

            affected = new RewardMapStackSnapshot(
                RewardMapArmySlotRules.ToFormationSlotStackId(formationSlot),
                unit.UnitId,
                unit.DisplayName,
                unit.Tier,
                1,
                operation.Amount,
                0,
                operation.Amount * unit.Cost,
                FirstSkillUnlocked(unit));
            army.Stacks.Add(affected);
            RecalculateArmyValue(army);
            return RewardMapError.None;
        }

        string targetStackId = operation.StackId;
        if (string.IsNullOrEmpty(targetStackId) && operation.Type == RewardMapOperationType.RecoverLosses)
        {
            RewardMapStackSnapshot lossTarget = FindLargestLossStack(army);
            targetStackId = lossTarget == null ? string.Empty : lossTarget.StackId;
        }

        affected = FindStack(army, targetStackId);
        if (affected == null)
        {
            return RewardMapError.NoLegalTarget;
        }

        if (!OperationMatchesAffectedUnit(operation, affected))
        {
            affected = null;
            return RewardMapError.NoLegalTarget;
        }

        if (operation.Type == RewardMapOperationType.AddUnits)
        {
            affected.Amount += operation.Amount;
            RecalculateStackValue(affected);
            RecalculateArmyValue(army);
            return RewardMapError.None;
        }

        if (operation.Type == RewardMapOperationType.RecoverLosses)
        {
            int recovered = Math.Min(Math.Max(0, affected.Lost), operation.Amount);
            if (recovered <= 0)
            {
                return RewardMapError.NoLegalTarget;
            }

            affected.Amount += recovered;
            affected.Lost -= recovered;
            RecalculateStackValue(affected);
            RecalculateArmyValue(army);
            return RewardMapError.None;
        }

        if (operation.Type == RewardMapOperationType.TeachSkill)
        {
            if (!SkillIsLegalForUnit(affected.UnitId, operation.SkillId))
            {
                return RewardMapError.InvalidTarget;
            }

            if (HasUnlockedSkill(affected, operation.SkillId))
            {
                return RewardMapError.NoLegalTarget;
            }

            affected.Skills.Add(new RewardMapSkillState(operation.SkillId, true));
            RecalculateStackValue(affected);
            RecalculateArmyValue(army);
            return RewardMapError.None;
        }

        if (operation.Type == RewardMapOperationType.PromoteStack || operation.Type == RewardMapOperationType.DowngradeStack)
        {
            RunShopUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(operation.ToUnitId);
            if (unit == null || operation.Amount <= 0)
            {
                return RewardMapError.InvalidTarget;
            }

            affected.UnitId = unit.UnitId;
            affected.DisplayName = unit.DisplayName;
            affected.Tier = unit.Tier;
            affected.Amount = operation.Amount;
            affected.Lost = 0;
            affected.Skills = FirstSkillUnlocked(unit);
            RecalculateStackValue(affected);
            RecalculateArmyValue(army);
            return RewardMapError.None;
        }

        return RewardMapError.InvalidTarget;
    }

    private static bool OperationMatchesAffectedUnit(RewardMapOperation operation, RewardMapStackSnapshot affected)
    {
        if (operation == null || affected == null || string.IsNullOrEmpty(operation.UnitId))
        {
            return true;
        }

        return affected.UnitId == operation.UnitId;
    }

    private RewardMapPreviewData BuildPreview(string rewardId, RewardMapArmySnapshot army, int runGold, RewardMapStackSnapshot affected, RewardMapError error, string message)
    {
        return new RewardMapPreviewData(rewardId, army, runGold, affected, error, message, ResultSource);
    }

    private RewardMapApplyResult Fail(RewardMapError error, string message, RewardMapCardViewData reward, RewardMapArmySnapshot army, int runGold)
    {
        return new RewardMapApplyResult(false, error, message, reward, army, runGold, ResultSource);
    }

    private RewardMapChoiceViewData EmptyChoice(RewardMapChoiceRequest request, string message)
    {
        return new RewardMapChoiceViewData(
            "reward-choice-" + Guid.NewGuid().ToString("N"),
            request == null ? string.Empty : request.RunId,
            RewardMapGameMode.Offline,
            RewardMapAuthoritySource.LocalOfflineAdapter,
            request == null ? null : request.BattleResultSummary,
            string.Empty,
            request == null ? 0 : request.RunGoldBeforeReward,
            request == null ? null : request.ArmyAfterBattle,
            new List<RewardMapCardViewData>(),
            null,
            null,
            message);
    }

    private static string BuildGainedSummary(RewardMapBattleResultSummary summary)
    {
        if (summary == null)
        {
            return "Gained: no battle summary.";
        }

        return "Gained: " + summary.RunGoldGained + " RUN GOLD, losses " + summary.Losses;
    }

    private string BuildBeforeText(RewardMapArmySnapshot army, RewardMapOperation operation)
    {
        if (operation == null)
        {
            return string.Empty;
        }

        if (operation.Type == RewardMapOperationType.GainCurrency)
        {
            return "RUN GOLD before reward";
        }

        if (operation.Type == RewardMapOperationType.AddStack)
        {
            return "New stack slot";
        }

        RewardMapStackSnapshot stack = FindStack(army, operation.StackId);
        if (stack == null && operation.Type == RewardMapOperationType.RecoverLosses)
        {
            stack = FindLargestLossStack(army);
        }

        return stack == null ? "No legal target" : stack.DisplayName + " x" + stack.Amount + " lost " + stack.Lost;
    }

    private string BuildAfterText(RewardMapArmySnapshot army, RewardMapOperation operation, int currencyDelta)
    {
        if (operation.Type == RewardMapOperationType.GainCurrency)
        {
            return "+" + currencyDelta + " RUN GOLD";
        }

        if (operation.Type == RewardMapOperationType.AddStack)
        {
            RewardMapStackSnapshot addedStack = FindStack(army, operation.NewStackId);
            if (addedStack == null)
            {
                addedStack = FindStackByUnit(army, operation.UnitId);
            }

            return addedStack == null ? "No legal target" : addedStack.DisplayName + " x" + addedStack.Amount + " joins";
        }

        RewardMapStackSnapshot stack = FindStack(army, operation.StackId);
        if (stack == null && operation.Type == RewardMapOperationType.RecoverLosses)
        {
            stack = FindLargestLossStack(army);
        }

        if (operation.Type == RewardMapOperationType.TeachSkill && stack != null)
        {
            return stack.DisplayName + " learns " + operation.SkillId;
        }

        return stack == null ? "No legal target" : stack.DisplayName + " x" + stack.Amount + " lost " + stack.Lost;
    }

    private RewardMapArmySnapshot CloneArmy(RewardMapArmySnapshot army, string snapshotId)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                RewardMapStackSnapshot stack = army.Stacks[i];
                if (stack != null)
                {
                    stacks.Add(new RewardMapStackSnapshot(stack.StackId, stack.UnitId, stack.DisplayName, stack.Tier, stack.Level, stack.Amount, stack.Lost, stack.CombatValue, CloneSkills(stack.Skills)));
                }
            }
        }

        return new RewardMapArmySnapshot(snapshotId, army == null ? 0 : army.TotalArmyValue, stacks);
    }

    private List<RewardMapSkillState> CloneSkills(List<RewardMapSkillState> skills)
    {
        List<RewardMapSkillState> result = new List<RewardMapSkillState>();
        if (skills == null)
        {
            return result;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new RewardMapSkillState(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    private List<RewardMapSkillState> FirstSkillUnlocked(RunShopUnitDefinition unit)
    {
        List<RewardMapSkillState> skills = new List<RewardMapSkillState>();
        if (unit == null || unit.SkillIds == null)
        {
            return skills;
        }

        for (int i = 0; i < unit.SkillIds.Count; i++)
        {
            skills.Add(new RewardMapSkillState(unit.SkillIds[i], i == 0));
        }

        return skills;
    }

    private RewardMapStackSnapshot FindStack(RewardMapArmySnapshot army, string stackId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(stackId))
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i] != null && army.Stacks[i].StackId == stackId)
            {
                return army.Stacks[i];
            }
        }

        return null;
    }

    private bool ArmyContainsUnit(RewardMapArmySnapshot army, string unitId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(unitId))
        {
            return false;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.UnitId == unitId)
            {
                return true;
            }
        }

        return false;
    }

    private RewardMapStackSnapshot FindStackByUnit(RewardMapArmySnapshot army, string unitId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.UnitId == unitId)
            {
                return stack;
            }
        }

        return null;
    }

    private RewardMapStackSnapshot FindLargestLossStack(RewardMapArmySnapshot army)
    {
        RewardMapStackSnapshot target = null;
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.Lost > 0 && (target == null || stack.Lost > target.Lost))
            {
                target = stack;
            }
        }

        return target;
    }

    private RewardMapCardViewData FindCard(List<RewardMapCardViewData> cards, string rewardId)
    {
        if (cards == null || string.IsNullOrEmpty(rewardId))
        {
            return null;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].RewardId == rewardId)
            {
                return cards[i];
            }
        }

        return null;
    }

    private bool SkillIsLegalForUnit(string unitId, string skillId)
    {
        RunShopUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(unitId);
        if (unit == null || unit.SkillIds == null)
        {
            return false;
        }

        for (int i = 0; i < unit.SkillIds.Count; i++)
        {
            if (unit.SkillIds[i] == skillId)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasUnlockedSkill(RewardMapStackSnapshot stack, string skillId)
    {
        if (stack == null || stack.Skills == null)
        {
            return false;
        }

        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i] != null && stack.Skills[i].SkillId == skillId && stack.Skills[i].Unlocked)
            {
                return true;
            }
        }

        return false;
    }

    private void RecalculateStackValue(RewardMapStackSnapshot stack)
    {
        RunShopUnitDefinition unit = unitSource == null || stack == null ? null : unitSource.FindUnit(stack.UnitId);
        int cost = unit == null ? 0 : unit.Cost;
        int skillValue = 0;
        if (stack != null && stack.Skills != null)
        {
            for (int i = 0; i < stack.Skills.Count; i++)
            {
                if (stack.Skills[i] != null && stack.Skills[i].Unlocked)
                {
                    skillValue += 18;
                }
            }
        }

        stack.CombatValue = Math.Max(0, stack.Amount * cost + skillValue);
    }

    private void RecalculateArmyValue(RewardMapArmySnapshot army)
    {
        int total = 0;
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i] != null)
            {
                total += army.Stacks[i].CombatValue;
            }
        }

        army.TotalArmyValue = total;
    }

    private static string MessageFor(RewardMapError error)
    {
        switch (error)
        {
            case RewardMapError.None:
                return "Reward can be applied.";
            case RewardMapError.NoLegalTarget:
                return "Reward has no legal target.";
            case RewardMapError.InvalidTarget:
                return "Reward target is invalid.";
            case RewardMapError.MissingArmy:
                return "Army is missing.";
            case RewardMapError.MissingChoice:
                return "Reward choice is missing.";
            case RewardMapError.MissingReward:
                return "Reward is missing.";
            case RewardMapError.AlreadyApplied:
                return "Reward was already applied.";
            default:
                return "Reward failed.";
        }
    }
}
