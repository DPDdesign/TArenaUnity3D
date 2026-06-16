using System;
using System.Collections.Generic;

public class RunShopService
{
    private const string ResultSource = "offline-local-run-shop-resolver";

    private readonly IRunShopUnitDefinitionSource unitSource;
    private readonly IRunShopVisitStore visitStore;

    public RunShopService(IRunShopUnitDefinitionSource unitSource, IRunShopVisitStore visitStore)
    {
        this.unitSource = unitSource;
        this.visitStore = visitStore;
    }

    public RunShopVisitViewData BuildVisit(RunShopVisitRequest request, string focusedOfferId)
    {
        if (request == null)
        {
            return EmptyVisit(null);
        }

        RunShopVisitViewData storedVisit = FindStoredVisit(request);
        if (storedVisit != null)
        {
            return RestoreVisit(storedVisit, request, focusedOfferId);
        }

        if (request.CurrentArmy == null)
        {
            return EmptyVisit(request);
        }

        RunShopArmySnapshot currentArmy = CloneArmy(request.CurrentArmy, request.CurrentArmy.SnapshotId);
        string visitId = string.IsNullOrEmpty(request.VisitId)
            ? "shop-visit-" + Guid.NewGuid().ToString("N")
            : request.VisitId;
        List<RunShopOfferViewData> offers = BuildOffers(currentArmy, request.RunCurrency);
        ApplyPurchasedState(visitId, offers);
        RunShopOfferViewData focusedOffer = FindOffer(offers, focusedOfferId);
        if (focusedOffer == null && offers.Count > 0)
        {
            focusedOffer = offers[0];
        }

        RunShopPreviewData preview = focusedOffer == null
            ? BuildPreview(string.Empty, currentArmy, request.RunCurrency, RunShopPurchaseError.MissingOffer, "Select a shop offer.")
            : PreviewOffer(focusedOffer, currentArmy, request.RunCurrency);

        RunShopVisitViewData visit = new RunShopVisitViewData(
            visitId,
            string.IsNullOrEmpty(request.RunId) ? "offline-run" : request.RunId,
            string.IsNullOrEmpty(request.RouteNodeId) ? "offline-shop-node" : request.RouteNodeId,
            RunShopGameMode.Offline,
            RunShopAuthoritySource.LocalOfflineAdapter,
            request.RunCurrency,
            currentArmy,
            offers,
            focusedOffer,
            preview,
            focusedOffer != null && focusedOffer.Available && focusedOffer.Affordable && !focusedOffer.Purchased,
            preview.Message);

        if (visitStore != null)
        {
            visitStore.SaveVisit(visit);

            RunShopVisitViewData persistedVisit = FindStoredVisit(new RunShopVisitRequest(
                visit.VisitId,
                visit.RunId,
                visit.RouteNodeId,
                visit.RunCurrency,
                visit.CurrentArmy));
            if (persistedVisit != null)
            {
                visit.VisitId = persistedVisit.VisitId;
                visit.RunId = persistedVisit.RunId;
                visit.RouteNodeId = persistedVisit.RouteNodeId;
            }
        }

        return visit;
    }

    public RunShopPurchaseResult Purchase(RunShopPurchaseCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.VisitId))
        {
            return Fail(RunShopPurchaseError.MissingVisit, "Missing run shop visit.", null, null, 0);
        }

        RunShopVisitViewData visit = visitStore == null ? null : visitStore.FindVisit(command.VisitId);
        if (visit == null)
        {
            return Fail(RunShopPurchaseError.MissingVisit, "Run shop visit was not found.", null, null, command.RunCurrency);
        }

        if (visitStore != null && visitStore.HasPurchasedOffer(command.VisitId, command.OfferId))
        {
            return Fail(RunShopPurchaseError.AlreadyPurchased, "This offer was already purchased.", null, command.CurrentArmy, command.RunCurrency);
        }

        RunShopOfferViewData offer = FindOffer(visit.Offers, command.OfferId);
        if (offer == null)
        {
            return Fail(RunShopPurchaseError.MissingOffer, "Select a shop offer.", null, command.CurrentArmy, command.RunCurrency);
        }

        RunShopArmySnapshot army = command.CurrentArmy == null ? visit.CurrentArmy : command.CurrentArmy;
        if (army == null)
        {
            return Fail(RunShopPurchaseError.InvalidTarget, "Current run army is missing.", offer, null, command.RunCurrency);
        }

        if (command.RunCurrency < offer.Cost)
        {
            return Fail(RunShopPurchaseError.InsufficientCurrency, "Not enough RUN GOLD.", offer, army, command.RunCurrency);
        }

        RunShopPreviewData preview = PreviewOffer(offer, army, command.RunCurrency);
        if (preview.Error != RunShopPurchaseError.None)
        {
            return Fail(preview.Error, preview.Message, offer, army, command.RunCurrency);
        }

        RunShopPurchaseRecord record = new RunShopPurchaseRecord(
            "shop-purchase-" + Guid.NewGuid().ToString("N"),
            command.VisitId,
            offer.OfferId,
            offer.Category,
            offer.Cost,
            command.RunCurrency,
            preview.CurrencyAfterPurchase,
            ResultSource);

        RunShopVisitViewData updatedVisit = BuildPurchasedVisitState(visit, offer.OfferId, preview.ArmyAfterPurchase, preview.CurrencyAfterPurchase);
        if (visitStore != null)
        {
            visitStore.SavePurchase(record, updatedVisit);
        }

        return new RunShopPurchaseResult(
            true,
            RunShopPurchaseError.None,
            "Purchase applied.",
            offer,
            preview.ArmyAfterPurchase,
            preview.CurrencyAfterPurchase,
            record);
    }

    public RunShopLeaveResult LeaveVisit(RunShopLeaveCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.VisitId))
        {
            return FailLeave("Missing run shop visit.");
        }

        RunShopVisitViewData visit = visitStore == null ? null : visitStore.FindVisit(command.VisitId);
        if (visit == null)
        {
            return FailLeave("Run shop visit was not found.");
        }

        if (visitStore == null)
        {
            return new RunShopLeaveResult(
                true,
                visit.VisitId,
                visit.RunId,
                visit.RouteNodeId,
                visit.RunCurrency,
                visit.CurrentArmy,
                "RunMap",
                "Leave shop accepted.");
        }

        RunShopLeaveResult result = visitStore.LeaveVisit(new RunShopLeaveCommand(
            visit.VisitId,
            string.IsNullOrEmpty(command.FocusedOfferId) && visit.FocusedOffer != null ? visit.FocusedOffer.OfferId : command.FocusedOfferId));
        return result ?? FailLeave("Leave shop failed.");
    }

    private List<RunShopOfferViewData> BuildOffers(RunShopArmySnapshot army, int runCurrency)
    {
        List<RunShopOfferViewData> offers = new List<RunShopOfferViewData>();

        RunShopStackSnapshot lossTarget = FindLargestLossStack(army);
        if (lossTarget != null)
        {
            int recoverAmount = Math.Max(1, CeilScaled(lossTarget.Lost, 3, 5));
            offers.Add(MakeOffer(
                "shop-recover-losses",
                RunShopOfferCategory.Resurrection,
                "Field Resurrection",
                "Recover part of one damaged stack.",
                55,
                lossTarget.StackId,
                lossTarget.DisplayName + ": " + lossTarget.Amount + " alive, " + lossTarget.Lost + " lost",
                lossTarget.DisplayName + ": " + (lossTarget.Amount + recoverAmount) + " alive, " + Math.Max(0, lossTarget.Lost - recoverAmount) + " lost",
                new RunShopOperation(RunShopOperationType.RecoverLosses, lossTarget.StackId, lossTarget.UnitId, string.Empty, string.Empty, string.Empty, recoverAmount, 0),
                runCurrency));
        }
        else
        {
            RunShopStackSnapshot growthTarget = FirstLiveStack(army);
            if (growthTarget != null)
            {
                offers.Add(MakeOffer(
                    "shop-reinforce-frontline",
                    RunShopOfferCategory.Recovery,
                    "Reserve Tonic",
                    "No losses to revive. Buy a small reinforcement instead.",
                    45,
                    growthTarget.StackId,
                    growthTarget.DisplayName + " x" + growthTarget.Amount,
                    growthTarget.DisplayName + " x" + (growthTarget.Amount + 6),
                    new RunShopOperation(RunShopOperationType.RecoverLosses, growthTarget.StackId, growthTarget.UnitId, string.Empty, string.Empty, string.Empty, 6, 0),
                    runCurrency));
            }
        }

        RunShopStackSnapshot skillTarget = FindLegalSkillTarget(army);
        if (skillTarget != null)
        {
            string skillId = FirstLockedSkill(skillTarget);
            offers.Add(MakeOffer(
                "shop-teach-skill",
                RunShopOfferCategory.Skill,
                "Teach " + skillId,
                "Adds one legal skill to one stack.",
                85,
                skillTarget.StackId,
                skillTarget.DisplayName + " skills: " + CountUnlockedSkills(skillTarget),
                skillTarget.DisplayName + " gains " + skillId,
                new RunShopOperation(RunShopOperationType.TeachSkill, skillTarget.StackId, skillTarget.UnitId, string.Empty, skillId, string.Empty, 0, 0),
                runCurrency));
        }

        offers.Add(MakeOffer(
            "shop-hire-trappers",
            RunShopOfferCategory.Stack,
            "Hire Trappers",
            "Add a limited tactical role for the final route.",
            75,
            string.Empty,
            "No new Trapper stack",
            "Add Trapper x10",
            new RunShopOperation(RunShopOperationType.AddStack, string.Empty, "Trapper", string.Empty, string.Empty, "shop-stack-trapper", 10, 0),
            runCurrency));

        RunShopStackSnapshot upgradeTarget = FindUpgradeTarget(army);
        if (upgradeTarget != null)
        {
            string toUnitId = upgradeTarget.UnitId == "Wisp" ? "StoneGolem" : "Axeman";
            int afterAmount = upgradeTarget.UnitId == "Wisp"
                ? Math.Max(1, FloorScaled(upgradeTarget.Amount, 16, 100))
                : Math.Max(1, FloorScaled(upgradeTarget.Amount, 28, 100));
            offers.Add(MakeOffer(
                "shop-controlled-promotion",
                RunShopOfferCategory.UpgradeExchange,
                "Controlled Promotion",
                "Trade quantity for a stronger class.",
                95,
                upgradeTarget.StackId,
                upgradeTarget.DisplayName + " x" + upgradeTarget.Amount,
                toUnitId + " x" + afterAmount,
                new RunShopOperation(RunShopOperationType.UpgradeStack, upgradeTarget.StackId, upgradeTarget.UnitId, toUnitId, string.Empty, string.Empty, afterAmount, 0),
                runCurrency));
        }

        return offers;
    }

    private RunShopOfferViewData MakeOffer(
        string offerId,
        RunShopOfferCategory category,
        string title,
        string detail,
        int cost,
        string affectedStackId,
        string beforeText,
        string afterText,
        RunShopOperation operation,
        int runCurrency)
    {
        bool available = operation != null;
        return new RunShopOfferViewData(
            offerId,
            category,
            title,
            detail,
            cost,
            available,
            false,
            runCurrency >= cost,
            beforeText,
            afterText,
            affectedStackId,
            operation);
    }

    private RunShopPreviewData PreviewOffer(RunShopOfferViewData offer, RunShopArmySnapshot army, int runCurrency)
    {
        if (offer == null)
        {
            return BuildPreview(string.Empty, army, runCurrency, RunShopPurchaseError.MissingOffer, "Select a shop offer.");
        }

        if (runCurrency < offer.Cost)
        {
            return BuildPreview(offer.OfferId, army, runCurrency, RunShopPurchaseError.InsufficientCurrency, "Not enough RUN GOLD.");
        }

        RunShopArmySnapshot previewArmy = CloneArmy(army, "preview-" + Guid.NewGuid().ToString("N"));
        int currency = runCurrency - offer.Cost;
        RunShopPurchaseError error = ApplyOperation(previewArmy, offer.Operation, ref currency);
        RunShopStackSnapshot affected = string.IsNullOrEmpty(offer.AffectedStackId)
            ? null
            : FindStack(previewArmy, offer.AffectedStackId);

        return BuildPreview(
            offer.OfferId,
            previewArmy,
            currency,
            error,
            error == RunShopPurchaseError.None ? "Preview ready." : MessageFor(error),
            affected);
    }

    private RunShopPurchaseError ApplyOperation(RunShopArmySnapshot army, RunShopOperation operation, ref int currency)
    {
        if (army == null || operation == null)
        {
            return RunShopPurchaseError.InvalidTarget;
        }

        if (operation.Type == RunShopOperationType.GainCurrency)
        {
            currency = Math.Max(0, currency + operation.CurrencyDelta);
            return RunShopPurchaseError.None;
        }

        if (operation.Type == RunShopOperationType.AddStack)
        {
            RunShopUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(operation.UnitId);
            if (unit == null)
            {
                return RunShopPurchaseError.InvalidTarget;
            }

            army.Stacks.Add(new RunShopStackSnapshot(
                string.IsNullOrEmpty(operation.NewStackId) ? "shop-stack-" + Guid.NewGuid().ToString("N") : operation.NewStackId,
                unit.UnitId,
                unit.DisplayName,
                unit.Tier,
                1,
                operation.Amount,
                0,
                operation.Amount * unit.Cost,
                FirstSkillUnlocked(unit)));
            RecalculateArmyValue(army);
            return RunShopPurchaseError.None;
        }

        RunShopStackSnapshot stack = FindStack(army, operation.StackId);
        if (stack == null)
        {
            return RunShopPurchaseError.InvalidTarget;
        }

        if (operation.Type == RunShopOperationType.RecoverLosses)
        {
            int recovered = Math.Min(operation.Amount, Math.Max(0, stack.Lost));
            if (recovered <= 0)
            {
                recovered = operation.Amount;
            }

            stack.Amount += recovered;
            stack.Lost = Math.Max(0, stack.Lost - recovered);
            RecalculateStackValue(stack);
            RecalculateArmyValue(army);
            return RunShopPurchaseError.None;
        }

        if (operation.Type == RunShopOperationType.TeachSkill)
        {
            if (!SkillIsLegalForUnit(stack.UnitId, operation.SkillId))
            {
                return RunShopPurchaseError.InvalidTarget;
            }

            SetSkillUnlocked(stack, operation.SkillId);
            RecalculateStackValue(stack);
            RecalculateArmyValue(army);
            return RunShopPurchaseError.None;
        }

        if (operation.Type == RunShopOperationType.UpgradeStack)
        {
            RunShopUnitDefinition targetUnit = unitSource == null ? null : unitSource.FindUnit(operation.ToUnitId);
            if (targetUnit == null || operation.Amount <= 0)
            {
                return RunShopPurchaseError.InvalidTarget;
            }

            stack.UnitId = targetUnit.UnitId;
            stack.DisplayName = targetUnit.DisplayName;
            stack.Tier = targetUnit.Tier;
            stack.Amount = operation.Amount;
            stack.Lost = 0;
            stack.Skills = FirstSkillUnlocked(targetUnit);
            RecalculateStackValue(stack);
            RecalculateArmyValue(army);
            return RunShopPurchaseError.None;
        }

        return RunShopPurchaseError.UnavailableOffer;
    }

    private RunShopVisitViewData EmptyVisit(RunShopVisitRequest request)
    {
        return new RunShopVisitViewData(
            "shop-visit-" + Guid.NewGuid().ToString("N"),
            request == null ? string.Empty : request.RunId,
            request == null ? string.Empty : request.RouteNodeId,
            RunShopGameMode.Offline,
            RunShopAuthoritySource.LocalOfflineAdapter,
            request == null ? 0 : request.RunCurrency,
            request == null ? null : request.CurrentArmy,
            new List<RunShopOfferViewData>(),
            null,
            null,
            false,
            "Current run army is missing.");
    }

    private RunShopVisitViewData FindStoredVisit(RunShopVisitRequest request)
    {
        if (visitStore == null || request == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.VisitId))
        {
            RunShopVisitViewData byVisitId = visitStore.FindVisit(request.VisitId);
            if (byVisitId != null)
            {
                return byVisitId;
            }
        }

        if (!string.IsNullOrEmpty(request.RunId) && !string.IsNullOrEmpty(request.RouteNodeId))
        {
            return visitStore.FindVisit(request.RunId, request.RouteNodeId);
        }

        return null;
    }

    private RunShopVisitViewData RestoreVisit(RunShopVisitViewData storedVisit, RunShopVisitRequest request, string focusedOfferId)
    {
        RunShopArmySnapshot currentArmy = CloneArmy(
            storedVisit.CurrentArmy == null ? request.CurrentArmy : storedVisit.CurrentArmy,
            storedVisit.CurrentArmy == null ? string.Empty : storedVisit.CurrentArmy.SnapshotId);
        List<RunShopOfferViewData> offers = CloneOffers(storedVisit.Offers);
        RefreshOfferCurrencyState(offers, storedVisit.RunCurrency);

        string resolvedFocusedOfferId = !string.IsNullOrEmpty(focusedOfferId)
            ? focusedOfferId
            : storedVisit.FocusedOffer == null
                ? string.Empty
                : storedVisit.FocusedOffer.OfferId;
        RunShopOfferViewData focusedOffer = FindOffer(offers, resolvedFocusedOfferId);
        if (focusedOffer == null && offers.Count > 0)
        {
            focusedOffer = offers[0];
        }

        RunShopPreviewData preview = focusedOffer == null
            ? BuildPreview(string.Empty, currentArmy, storedVisit.RunCurrency, RunShopPurchaseError.MissingOffer, "Select a shop offer.")
            : PreviewOffer(focusedOffer, currentArmy, storedVisit.RunCurrency);

        RunShopVisitViewData visit = new RunShopVisitViewData(
            storedVisit.VisitId,
            string.IsNullOrEmpty(storedVisit.RunId) ? request.RunId : storedVisit.RunId,
            string.IsNullOrEmpty(storedVisit.RouteNodeId) ? request.RouteNodeId : storedVisit.RouteNodeId,
            storedVisit.GameMode,
            storedVisit.AuthoritySource,
            storedVisit.RunCurrency,
            currentArmy,
            offers,
            focusedOffer,
            preview,
            focusedOffer != null &&
            focusedOffer.Available &&
            focusedOffer.Affordable &&
            !focusedOffer.Purchased &&
            preview.Error == RunShopPurchaseError.None,
            preview.Message);

        visitStore.SaveVisit(visit);
        return visit;
    }

    private RunShopPreviewData BuildPreview(
        string offerId,
        RunShopArmySnapshot army,
        int currency,
        RunShopPurchaseError error,
        string message,
        RunShopStackSnapshot affectedStack = null)
    {
        return new RunShopPreviewData(
            offerId,
            army,
            affectedStack,
            Math.Max(0, currency),
            error,
            message,
            ResultSource);
    }

    private void ApplyPurchasedState(string visitId, List<RunShopOfferViewData> offers)
    {
        if (visitStore == null || offers == null)
        {
            return;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            RunShopOfferViewData offer = offers[i];
            if (offer != null && visitStore.HasPurchasedOffer(visitId, offer.OfferId))
            {
                offer.Purchased = true;
                offer.Available = false;
            }
        }
    }

    private RunShopVisitViewData BuildPurchasedVisitState(
        RunShopVisitViewData existingVisit,
        string purchasedOfferId,
        RunShopArmySnapshot armyAfterPurchase,
        int currencyAfterPurchase)
    {
        List<RunShopOfferViewData> offers = CloneOffers(existingVisit == null ? null : existingVisit.Offers);
        for (int i = 0; i < offers.Count; i++)
        {
            RunShopOfferViewData offer = offers[i];
            if (offer == null)
            {
                continue;
            }

            offer.Purchased = offer.OfferId == purchasedOfferId || offer.Purchased;
            if (offer.Purchased)
            {
                offer.Available = false;
            }

            offer.Affordable = currencyAfterPurchase >= offer.Cost;
        }

        RunShopOfferViewData focusedOffer = FindOffer(offers, purchasedOfferId);
        return new RunShopVisitViewData(
            existingVisit == null ? string.Empty : existingVisit.VisitId,
            existingVisit == null ? string.Empty : existingVisit.RunId,
            existingVisit == null ? string.Empty : existingVisit.RouteNodeId,
            existingVisit == null ? RunShopGameMode.Offline : existingVisit.GameMode,
            existingVisit == null ? RunShopAuthoritySource.LocalOfflineAdapter : existingVisit.AuthoritySource,
            currencyAfterPurchase,
            CloneArmy(armyAfterPurchase, armyAfterPurchase == null ? string.Empty : armyAfterPurchase.SnapshotId),
            offers,
            focusedOffer,
            null,
            false,
            "Purchase applied.");
    }

    private void RefreshOfferCurrencyState(List<RunShopOfferViewData> offers, int runCurrency)
    {
        if (offers == null)
        {
            return;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            RunShopOfferViewData offer = offers[i];
            if (offer == null)
            {
                continue;
            }

            offer.Affordable = runCurrency >= offer.Cost;
            if (offer.Purchased)
            {
                offer.Available = false;
            }
        }
    }

    private RunShopPurchaseResult Fail(
        RunShopPurchaseError error,
        string message,
        RunShopOfferViewData offer,
        RunShopArmySnapshot army,
        int currency)
    {
        return new RunShopPurchaseResult(false, error, message, offer, army, currency, null);
    }

    private RunShopLeaveResult FailLeave(string message)
    {
        return new RunShopLeaveResult(false, string.Empty, string.Empty, string.Empty, 0, null, "RunMap", message);
    }

    private RunShopStackSnapshot FindLargestLossStack(RunShopArmySnapshot army)
    {
        RunShopStackSnapshot result = null;
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RunShopStackSnapshot stack = army.Stacks[i];
            if (stack == null || stack.Lost <= 0)
            {
                continue;
            }

            if (result == null || stack.Lost > result.Lost)
            {
                result = stack;
            }
        }

        return result;
    }

    private RunShopStackSnapshot FirstLiveStack(RunShopArmySnapshot army)
    {
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i] != null && army.Stacks[i].Amount > 0)
            {
                return army.Stacks[i];
            }
        }

        return null;
    }

    private RunShopStackSnapshot FindLegalSkillTarget(RunShopArmySnapshot army)
    {
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RunShopStackSnapshot stack = army.Stacks[i];
            if (stack != null && !string.IsNullOrEmpty(FirstLockedSkill(stack)))
            {
                return stack;
            }
        }

        return null;
    }

    private RunShopStackSnapshot FindUpgradeTarget(RunShopArmySnapshot army)
    {
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RunShopStackSnapshot stack = army.Stacks[i];
            if (stack == null)
            {
                continue;
            }

            if (stack.UnitId == "Rusher" && stack.Amount >= 8)
            {
                return stack;
            }

            if (stack.UnitId == "Wisp" && stack.Amount >= 10)
            {
                return stack;
            }
        }

        return null;
    }

    private RunShopStackSnapshot FindStack(RunShopArmySnapshot army, string stackId)
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

    private RunShopOfferViewData FindOffer(List<RunShopOfferViewData> offers, string offerId)
    {
        if (offers == null || string.IsNullOrEmpty(offerId))
        {
            return null;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null && offers[i].OfferId == offerId)
            {
                return offers[i];
            }
        }

        return null;
    }

    private string FirstLockedSkill(RunShopStackSnapshot stack)
    {
        RunShopUnitDefinition unit = unitSource == null || stack == null ? null : unitSource.FindUnit(stack.UnitId);
        if (unit == null || unit.SkillIds == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < unit.SkillIds.Count; i++)
        {
            if (!HasUnlockedSkill(stack, unit.SkillIds[i]))
            {
                return unit.SkillIds[i];
            }
        }

        return string.Empty;
    }

    private bool SkillIsLegalForUnit(string unitId, string skillId)
    {
        RunShopUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(unitId);
        if (unit == null || unit.SkillIds == null || string.IsNullOrEmpty(skillId))
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

    private bool HasUnlockedSkill(RunShopStackSnapshot stack, string skillId)
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

    private int CountUnlockedSkills(RunShopStackSnapshot stack)
    {
        int count = 0;
        if (stack == null || stack.Skills == null)
        {
            return count;
        }

        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i] != null && stack.Skills[i].Unlocked)
            {
                count++;
            }
        }

        return count;
    }

    private void SetSkillUnlocked(RunShopStackSnapshot stack, string skillId)
    {
        if (stack.Skills == null)
        {
            stack.Skills = new List<RunShopSkillState>();
        }

        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i] != null && stack.Skills[i].SkillId == skillId)
            {
                stack.Skills[i].Unlocked = true;
                return;
            }
        }

        stack.Skills.Add(new RunShopSkillState(skillId, true));
    }

    private List<RunShopSkillState> FirstSkillUnlocked(RunShopUnitDefinition unit)
    {
        List<RunShopSkillState> skills = new List<RunShopSkillState>();
        if (unit == null || unit.SkillIds == null)
        {
            return skills;
        }

        for (int i = 0; i < unit.SkillIds.Count; i++)
        {
            skills.Add(new RunShopSkillState(unit.SkillIds[i], i == 0));
        }

        return skills;
    }

    private RunShopArmySnapshot CloneArmy(RunShopArmySnapshot army, string snapshotId)
    {
        List<RunShopStackSnapshot> stacks = new List<RunShopStackSnapshot>();
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                RunShopStackSnapshot stack = army.Stacks[i];
                if (stack != null)
                {
                    stacks.Add(new RunShopStackSnapshot(
                        stack.StackId,
                        stack.UnitId,
                        stack.DisplayName,
                        stack.Tier,
                        stack.Level,
                        stack.Amount,
                        stack.Lost,
                        stack.CombatValue,
                        CloneSkills(stack.Skills)));
                }
            }
        }

        return new RunShopArmySnapshot(snapshotId, army == null ? 0 : army.TotalArmyValue, stacks);
    }

    private List<RunShopSkillState> CloneSkills(List<RunShopSkillState> skills)
    {
        List<RunShopSkillState> copy = new List<RunShopSkillState>();
        if (skills == null)
        {
            return copy;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                copy.Add(new RunShopSkillState(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return copy;
    }

    private List<RunShopOfferViewData> CloneOffers(List<RunShopOfferViewData> offers)
    {
        List<RunShopOfferViewData> copy = new List<RunShopOfferViewData>();
        if (offers == null)
        {
            return copy;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            RunShopOfferViewData offer = offers[i];
            if (offer == null)
            {
                continue;
            }

            copy.Add(new RunShopOfferViewData(
                offer.OfferId,
                offer.Category,
                offer.Title,
                offer.Detail,
                offer.Cost,
                offer.Available,
                offer.Purchased,
                offer.Affordable,
                offer.BeforeText,
                offer.AfterText,
                offer.AffectedStackId,
                CloneOperation(offer.Operation)));
        }

        return copy;
    }

    private static RunShopOperation CloneOperation(RunShopOperation operation)
    {
        if (operation == null)
        {
            return null;
        }

        return new RunShopOperation(
            operation.Type,
            operation.StackId,
            operation.UnitId,
            operation.ToUnitId,
            operation.SkillId,
            operation.NewStackId,
            operation.Amount,
            operation.CurrencyDelta);
    }

    private void RecalculateStackValue(RunShopStackSnapshot stack)
    {
        RunShopUnitDefinition unit = unitSource == null || stack == null ? null : unitSource.FindUnit(stack.UnitId);
        int cost = unit == null ? 0 : unit.Cost;
        int skillValue = CountUnlockedSkills(stack) * 18;
        stack.CombatValue = Math.Max(0, stack.Amount * cost + skillValue);
    }

    private void RecalculateArmyValue(RunShopArmySnapshot army)
    {
        if (army == null || army.Stacks == null)
        {
            return;
        }

        int total = 0;
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i] != null)
            {
                total += Math.Max(0, army.Stacks[i].CombatValue);
            }
        }

        army.TotalArmyValue = total;
    }

    private static string MessageFor(RunShopPurchaseError error)
    {
        switch (error)
        {
            case RunShopPurchaseError.None:
                return "Purchase can be applied.";
            case RunShopPurchaseError.MissingVisit:
                return "Run shop visit is missing.";
            case RunShopPurchaseError.MissingOffer:
                return "Select a shop offer.";
            case RunShopPurchaseError.AlreadyPurchased:
                return "This offer was already purchased.";
            case RunShopPurchaseError.InsufficientCurrency:
                return "Not enough RUN GOLD.";
            case RunShopPurchaseError.InvalidTarget:
                return "Shop offer target is invalid.";
            case RunShopPurchaseError.UnavailableOffer:
                return "Shop offer is unavailable.";
            default:
                return "Shop purchase failed.";
        }
    }

    private static int CeilScaled(int value, int numerator, int denominator)
    {
        if (value <= 0 || numerator <= 0 || denominator <= 0)
        {
            return 0;
        }

        return ((value * numerator) + denominator - 1) / denominator;
    }

    private static int FloorScaled(int value, int numerator, int denominator)
    {
        if (value <= 0 || numerator <= 0 || denominator <= 0)
        {
            return 0;
        }

        return (value * numerator) / denominator;
    }
}
