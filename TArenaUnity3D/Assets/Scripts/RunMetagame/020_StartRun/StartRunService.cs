using System;
using System.Collections.Generic;

public class StartRunService
{
    private const string ActiveRunStatus = "active";

    private readonly IStartingArmyTemplateSource armySource;
    private readonly IRunRoutePreviewSource routeSource;
    private readonly IStartRunUnitDefinitionSource unitSource;
    private readonly IStartRunRecordStore recordStore;
    private readonly IStartRunSlotAvailabilitySource slotAvailabilitySource;

    public StartRunService(
        IStartingArmyTemplateSource armySource,
        IRunRoutePreviewSource routeSource,
        IStartRunUnitDefinitionSource unitSource,
        IStartRunRecordStore recordStore,
        IStartRunSlotAvailabilitySource slotAvailabilitySource = null)
    {
        this.armySource = armySource;
        this.routeSource = routeSource;
        this.unitSource = unitSource;
        this.recordStore = recordStore;
        this.slotAvailabilitySource = slotAvailabilitySource ?? new UnrestrictedStartRunSlotAvailabilitySource();
    }

    public StartRunScreenViewData BuildScreen(string selectedStartingArmyId, string selectedRouteId)
    {
        return BuildScreen(selectedStartingArmyId, selectedRouteId, 0, string.Empty);
    }

    public StartRunScreenViewData BuildScreen(
        string selectedStartingArmyId,
        string selectedRouteId,
        int requestedSlotCount,
        string accountPlayerId = "")
    {
        List<StartingArmyTemplate> armyTemplates = ListStartingArmies(requestedSlotCount);
        List<RoutePreviewTemplate> routeTemplates = routeSource == null
            ? new List<RoutePreviewTemplate>()
            : routeSource.ListRoutePreviews();
        StartRunSlotAvailabilityContext availabilityContext = LoadAvailabilityContext(accountPlayerId);

        StartingArmyTemplate selectedTemplate = FindArmy(armyTemplates, selectedStartingArmyId);
        if (selectedTemplate == null && armyTemplates.Count > 0)
        {
            selectedTemplate = armyTemplates[0];
        }

        RoutePreviewTemplate selectedRoute = FindRoute(routeTemplates, selectedRouteId);
        if (selectedRoute == null && routeTemplates.Count > 0)
        {
            selectedRoute = routeTemplates[0];
        }

        List<StartingArmyOptionViewData> armyOptions = new List<StartingArmyOptionViewData>();
        StartingArmyOptionViewData selectedArmyView = null;
        int optionCount = requestedSlotCount > 0 ? requestedSlotCount : armyTemplates.Count;
        for (int i = 0; i < optionCount; i++)
        {
            StartingArmyTemplate template = i < armyTemplates.Count ? armyTemplates[i] : null;
            StartingArmyOptionViewData option = BuildArmyOption(template, i, availabilityContext);
            armyOptions.Add(option);
            if (selectedTemplate != null && template != null && template.TemplateId == selectedTemplate.TemplateId)
            {
                selectedArmyView = option;
            }
        }

        int currentArmyValue = selectedArmyView == null ? 0 : selectedArmyView.TotalArmyValue;
        List<RoutePreviewViewData> routeOptions = new List<RoutePreviewViewData>();
        RoutePreviewViewData selectedRouteView = null;
        for (int i = 0; i < routeTemplates.Count; i++)
        {
            RoutePreviewViewData routeView = new RoutePreviewViewData(
                routeTemplates[i].RouteId,
                routeTemplates[i].DisplayName,
                routeTemplates[i].Description,
                routeTemplates[i].RecommendedArmyValue,
                currentArmyValue);
            routeOptions.Add(routeView);
            if (selectedRoute != null && routeTemplates[i].RouteId == selectedRoute.RouteId)
            {
                selectedRouteView = routeView;
            }
        }

        StartRunValidationError error = Validate(selectedTemplate, selectedRoute);
        string validationMessage = MessageFor(error);
        if (error == StartRunValidationError.None && selectedArmyView != null && selectedArmyView.IsLocked)
        {
            error = StartRunValidationError.BlockedRunStart;
            validationMessage = string.IsNullOrEmpty(selectedArmyView.LockedReason)
                ? MessageFor(error)
                : selectedArmyView.LockedReason;
        }

        return new StartRunScreenViewData(
            armyOptions,
            selectedArmyView,
            routeOptions,
            selectedRouteView,
            BuildStartingAssets(selectedArmyView),
            error == StartRunValidationError.None,
            error,
            validationMessage);
    }

    public StartRunResult BeginRun(StartRunCommand command)
    {
        if (command == null)
        {
            return Fail(StartRunValidationError.MissingStartingArmy);
        }

        List<StartingArmyTemplate> armyTemplates = ListStartingArmies(command.RequestedSlotCount);
        List<RoutePreviewTemplate> routeTemplates = routeSource == null
            ? new List<RoutePreviewTemplate>()
            : routeSource.ListRoutePreviews();
        StartRunSlotAvailabilityContext availabilityContext = LoadAvailabilityContext(command.AccountPlayerId);

        StartingArmyTemplate selectedTemplate = FindArmy(armyTemplates, command.SelectedStartingArmyId);
        if (selectedTemplate == null)
        {
            selectedTemplate = FindArmy(armyTemplates, command.StartingArmyTemplateId);
        }

        RoutePreviewTemplate selectedRoute = FindRoute(routeTemplates, command.RoutePreviewOptionId);
        StartRunValidationError error = Validate(selectedTemplate, selectedRoute);
        if (error != StartRunValidationError.None)
        {
            return Fail(error);
        }

        StartingArmyOptionViewData selectedArmyView = BuildArmyOption(
            selectedTemplate,
            FindArmyIndex(armyTemplates, selectedTemplate),
            availabilityContext);
        if (selectedArmyView.IsLocked)
        {
            return new StartRunResult(
                false,
                StartRunValidationError.BlockedRunStart,
                string.IsNullOrEmpty(selectedArmyView.LockedReason) ? MessageFor(StartRunValidationError.BlockedRunStart) : selectedArmyView.LockedReason,
                null);
        }

        RunArmySnapshot snapshot = CreateSnapshot(selectedArmyView);
        CreatedRunRecord record = new CreatedRunRecord(
            "run-" + Guid.NewGuid().ToString("N"),
            StartRunGameMode.Offline,
            StartRunAuthoritySource.LocalOfflineAdapter,
            string.IsNullOrEmpty(command.AccountPlayerId) ? "offline-player" : command.AccountPlayerId,
            selectedTemplate.TemplateId,
            selectedTemplate.VariantId,
            selectedTemplate.TemplateId,
            selectedRoute.RouteId,
            selectedTemplate.StartingCurrency,
            ActiveRunStatus,
            snapshot);

        if (recordStore != null)
        {
            CreatedRunRecord persisted = recordStore.SaveCreatedRun(record);
            if (persisted != null)
            {
                record = persisted;
            }
        }

        return new StartRunResult(true, StartRunValidationError.None, MessageFor(StartRunValidationError.None), record);
    }

    private List<StartingArmyTemplate> ListStartingArmies(int requestedSlotCount)
    {
        if (armySource == null)
        {
            return new List<StartingArmyTemplate>();
        }

        IRequestedStartingArmyTemplateSource requestedSource = armySource as IRequestedStartingArmyTemplateSource;
        if (requestedSource != null && requestedSlotCount > 0)
        {
            return requestedSource.ListStartingArmies(requestedSlotCount);
        }

        return armySource.ListStartingArmies();
    }

    private StartRunSlotAvailabilityContext LoadAvailabilityContext(string accountPlayerId)
    {
        return slotAvailabilitySource == null
            ? new StartRunSlotAvailabilityContext(99, true)
            : slotAvailabilitySource.LoadAvailabilityContext(accountPlayerId);
    }

    private StartingArmyOptionViewData BuildArmyOption(
        StartingArmyTemplate template,
        int visualSlotIndex,
        StartRunSlotAvailabilityContext availabilityContext)
    {
        List<StartRunStackViewData> stackViews = new List<StartRunStackViewData>();
        int totalValue = 0;
        StartRunValidationError validationError = ValidateArmy(template);
        StartRunSlotAvailability availability = StartRunSlotAvailabilityRules.Evaluate(visualSlotIndex, availabilityContext);

        if (template != null && template.Stacks != null)
        {
            for (int i = 0; i < template.Stacks.Count; i++)
            {
                StartRunStackViewData stackView = BuildStackView(template.Stacks[i]);
                stackViews.Add(stackView);
                totalValue += stackView.CombatValue;
            }
        }

        SortStackViews(stackViews);

        return new StartingArmyOptionViewData(
            template == null ? string.Empty : template.TemplateId,
            template == null ? string.Empty : template.VariantId,
            template == null ? "Slot " + (visualSlotIndex + 1).ToString() : template.DisplayName,
            template == null ? string.Empty : template.Description,
            template == null ? 0 : template.StartingCurrency,
            template == null ? 0 : template.StartingRerollTokens,
            template == null ? 0 : template.BattleSkipTokens,
            totalValue,
            validationError == StartRunValidationError.None,
            validationError,
            stackViews,
            visualSlotIndex,
            availability.IsLocked || template == null,
            template == null ? StartRunSlotAvailabilityRules.ComingSoonReason : availability.LockedReason);
    }

    private static StartRunStartingAssetsViewData BuildStartingAssets(StartingArmyOptionViewData selectedArmy)
    {
        return new StartRunStartingAssetsViewData(
            selectedArmy == null ? 0 : selectedArmy.StartingCurrency,
            selectedArmy == null ? 0 : selectedArmy.StartingRerollTokens,
            selectedArmy == null ? 0 : selectedArmy.BattleSkipTokens);
    }

    private StartRunStackViewData BuildStackView(StartRunStackTemplate stack)
    {
        StartRunUnitDefinition unit = unitSource == null || stack == null ? null : unitSource.FindUnit(stack.UnitId);
        string unitId = stack == null ? string.Empty : stack.UnitId;
        string displayName = unit == null ? unitId : unit.DisplayName;
        string tier = unit == null
            ? (stack == null || string.IsNullOrEmpty(stack.Tier) ? "I" : stack.Tier)
            : unit.Tier;
        int amount = stack == null ? 0 : Math.Max(0, stack.Amount);
        int level = stack == null ? 1 : Math.Max(1, stack.Level);
        int unitCost = unit == null ? 0 : unit.Cost;
        int combatValue = amount * unitCost;

        return new StartRunStackViewData(
            unitId,
            displayName,
            tier,
            level,
            amount,
            combatValue,
            BuildSkillViewData(stack, unit));
    }

    private void SortStackViews(List<StartRunStackViewData> stacks)
    {
        if (stacks == null)
        {
            return;
        }

        stacks.Sort(delegate(StartRunStackViewData left, StartRunStackViewData right)
        {
            int factionCompare = FactionFor(left).CompareTo(FactionFor(right));
            if (factionCompare != 0)
            {
                return factionCompare;
            }

            int tierCompare = TierNumber(left == null ? string.Empty : left.Tier).CompareTo(TierNumber(right == null ? string.Empty : right.Tier));
            if (tierCompare != 0)
            {
                return tierCompare;
            }

            string leftId = left == null ? string.Empty : left.UnitId;
            string rightId = right == null ? string.Empty : right.UnitId;
            return string.CompareOrdinal(leftId, rightId);
        });
    }

    private int FactionFor(StartRunStackViewData stack)
    {
        StartRunUnitDefinition unit = unitSource == null || stack == null ? null : unitSource.FindUnit(stack.UnitId);
        return unit == null ? UnitFactionResolver.ResolveFactionId(stack == null ? string.Empty : stack.UnitId) : unit.FactionId;
    }

    private static int TierNumber(string tier)
    {
        if (string.IsNullOrEmpty(tier))
        {
            return 1;
        }

        int parsed;
        if (int.TryParse(tier, out parsed))
        {
            return Math.Max(1, parsed);
        }

        switch (tier.Trim().ToUpperInvariant())
        {
            case "I":
                return 1;
            case "II":
                return 2;
            case "III":
                return 3;
            case "IV":
                return 4;
            case "V":
                return 5;
            default:
                return 1;
        }
    }

    private List<StartRunSkillViewData> BuildSkillViewData(StartRunStackTemplate stack, StartRunUnitDefinition unit)
    {
        List<StartRunSkillViewData> skills = new List<StartRunSkillViewData>();
        if (stack == null)
        {
            return skills;
        }

        List<StartRunSkillTemplate> stackSkills = stack.Skills ?? new List<StartRunSkillTemplate>();

        if (unit == null || unit.SkillIds == null || unit.SkillIds.Count == 0)
        {
            for (int i = 0; i < stackSkills.Count; i++)
            {
                StartRunSkillTemplate skill = stackSkills[i];
                if (skill != null)
                {
                    skills.Add(new StartRunSkillViewData(skill.SkillId, skill.Unlocked));
                }
            }

            return skills;
        }

        for (int i = 0; i < unit.SkillIds.Count; i++)
        {
            StartRunSkillTemplate skill = FindSkillTemplate(stackSkills, unit.SkillIds[i]);
            skills.Add(new StartRunSkillViewData(unit.SkillIds[i], skill != null && skill.Unlocked));
        }

        for (int i = 0; i < stackSkills.Count; i++)
        {
            StartRunSkillTemplate skill = stackSkills[i];
            if (skill != null && !ContainsSkill(skills, skill.SkillId))
            {
                skills.Add(new StartRunSkillViewData(skill.SkillId, skill.Unlocked));
            }
        }

        return skills;
    }

    private RunArmySnapshot CreateSnapshot(StartingArmyOptionViewData selectedArmy)
    {
        List<RunArmyStackSnapshot> stacks = new List<RunArmyStackSnapshot>();
        if (selectedArmy != null && selectedArmy.Stacks != null)
        {
            for (int i = 0; i < selectedArmy.Stacks.Count; i++)
            {
                StartRunStackViewData stack = selectedArmy.Stacks[i];
                stacks.Add(new RunArmyStackSnapshot(
                    stack.UnitId,
                    stack.Tier,
                    stack.Level,
                    stack.Amount,
                    stack.CombatValue,
                    CloneSkillViews(stack.Skills)));
            }
        }

        return new RunArmySnapshot("snapshot-" + Guid.NewGuid().ToString("N"), selectedArmy == null ? 0 : selectedArmy.TotalArmyValue, stacks);
    }

    private StartRunValidationError Validate(StartingArmyTemplate army, RoutePreviewTemplate route)
    {
        StartRunValidationError armyError = ValidateArmy(army);
        if (armyError != StartRunValidationError.None)
        {
            return armyError;
        }

        if (route == null || string.IsNullOrEmpty(route.RouteId))
        {
            return StartRunValidationError.MissingRoute;
        }

        return StartRunValidationError.None;
    }

    private StartRunValidationError ValidateArmy(StartingArmyTemplate army)
    {
        if (army == null || string.IsNullOrEmpty(army.TemplateId))
        {
            return StartRunValidationError.MissingStartingArmy;
        }

        if (army.Stacks == null || army.Stacks.Count == 0)
        {
            return StartRunValidationError.EmptyArmy;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            StartRunStackTemplate stack = army.Stacks[i];
            if (stack == null || string.IsNullOrEmpty(stack.UnitId) || stack.Amount <= 0 || stack.Level <= 0)
            {
                return StartRunValidationError.InvalidArmy;
            }

            StartRunUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(stack.UnitId);
            if (unit == null)
            {
                return StartRunValidationError.InvalidArmy;
            }

            if (!SkillsAreLegalForUnit(stack, unit))
            {
                return StartRunValidationError.InvalidArmy;
            }
        }

        return StartRunValidationError.None;
    }

    private bool SkillsAreLegalForUnit(StartRunStackTemplate stack, StartRunUnitDefinition unit)
    {
        if (stack.Skills == null || unit.SkillIds == null)
        {
            return true;
        }

        for (int i = 0; i < stack.Skills.Count; i++)
        {
            StartRunSkillTemplate skill = stack.Skills[i];
            if (skill == null || string.IsNullOrEmpty(skill.SkillId))
            {
                return false;
            }

            if (!ContainsSkill(unit.SkillIds, skill.SkillId))
            {
                return false;
            }
        }

        return true;
    }

    private static StartingArmyTemplate FindArmy(List<StartingArmyTemplate> armies, string id)
    {
        if (armies == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < armies.Count; i++)
        {
            if (armies[i] != null && (armies[i].TemplateId == id || armies[i].VariantId == id))
            {
                return armies[i];
            }
        }

        return null;
    }

    private static int FindArmyIndex(List<StartingArmyTemplate> armies, StartingArmyTemplate selectedTemplate)
    {
        if (armies == null || selectedTemplate == null)
        {
            return 0;
        }

        for (int i = 0; i < armies.Count; i++)
        {
            if (armies[i] != null && armies[i].TemplateId == selectedTemplate.TemplateId)
            {
                return i;
            }
        }

        return 0;
    }

    private static RoutePreviewTemplate FindRoute(List<RoutePreviewTemplate> routes, string id)
    {
        if (routes == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < routes.Count; i++)
        {
            if (routes[i] != null && routes[i].RouteId == id)
            {
                return routes[i];
            }
        }

        return null;
    }

    private static bool ContainsSkill(List<StartRunSkillViewData> skills, string skillId)
    {
        if (skills == null)
        {
            return false;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].SkillId == skillId)
            {
                return true;
            }
        }

        return false;
    }

    private static StartRunSkillTemplate FindSkillTemplate(List<StartRunSkillTemplate> skills, string skillId)
    {
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].SkillId == skillId)
            {
                return skills[i];
            }
        }

        return null;
    }

    private static bool ContainsSkill(List<string> skills, string skillId)
    {
        if (skills == null)
        {
            return false;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] == skillId)
            {
                return true;
            }
        }

        return false;
    }

    private static List<StartRunSkillViewData> CloneSkillViews(List<StartRunSkillViewData> skills)
    {
        List<StartRunSkillViewData> copy = new List<StartRunSkillViewData>();
        if (skills == null)
        {
            return copy;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                copy.Add(new StartRunSkillViewData(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return copy;
    }

    private static StartRunResult Fail(StartRunValidationError error)
    {
        return new StartRunResult(false, error, MessageFor(error), null);
    }

    private static string MessageFor(StartRunValidationError error)
    {
        switch (error)
        {
            case StartRunValidationError.None:
                return "Run can be started.";
            case StartRunValidationError.MissingStartingArmy:
                return "Select a starting army.";
            case StartRunValidationError.EmptyArmy:
                return "Selected starting army has no stacks.";
            case StartRunValidationError.InvalidArmy:
                return "Selected starting army contains invalid units, amounts, levels, or skills.";
            case StartRunValidationError.MissingRoute:
                return "Select a route preview.";
            case StartRunValidationError.BlockedRunStart:
                return "Run start is blocked.";
            default:
                return "Run start failed.";
        }
    }
}
