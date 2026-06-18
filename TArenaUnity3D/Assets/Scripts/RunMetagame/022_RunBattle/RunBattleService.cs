using System;
using System.Collections.Generic;

public class RunBattleService
{
    private const string LaunchResultSource = "offline-local-run-battle-launch";
    private const string CompletionResultSource = "offline-local-run-battle-completion";

    private readonly IRunBattleEncounterSource encounterSource;
    private readonly IRunBattleLaunchAdapter launchAdapter;
    private readonly IRunBattleStore store;

    public RunBattleService(
        IRunBattleEncounterSource encounterSource,
        IRunBattleLaunchAdapter launchAdapter,
        IRunBattleStore store)
    {
        this.encounterSource = encounterSource;
        this.launchAdapter = launchAdapter;
        this.store = store;
    }

    public RunBattleLaunchViewData PrepareBattle(RunBattlePrepareRequest request)
    {
        RunBattleError error = ValidatePrepareRequest(request);
        RunBattleEncounterDefinition encounter = error == RunBattleError.None
            ? encounterSource.FindEncounter(request.RouteNodeId, request.EncounterId)
            : null;

        if (error == RunBattleError.None && encounter == null)
        {
            error = RunBattleError.MissingEncounter;
        }

        if (error != RunBattleError.None)
        {
            return EmptyLaunchView(request, error);
        }

        string runBattleId = "run-battle-" + Guid.NewGuid().ToString("N");
        RunBattleArmySnapshot currentArmy = CloneArmy(request.CurrentArmy, request.CurrentArmy.SnapshotId);
        RunBattleLaunchPayload payload = new RunBattleLaunchPayload(
            runBattleId,
            request.RunId,
            request.RouteNodeId,
            encounter.EncounterId,
            currentArmy.SnapshotId,
            encounter.EnemyArmySourceId,
            encounter.EnemyGoal,
            LaunchResultSource);
        RunBattleLaunchRecord launchRecord = launchAdapter == null ? null : launchAdapter.CreateLaunchRecord(payload);

        RunBattleLaunchViewData viewData = new RunBattleLaunchViewData(
            runBattleId,
            request.RunId,
            request.RouteNodeId,
            request.StageIndex,
            request.RunCurrency,
            RunBattleGameMode.Offline,
            RunBattleAuthoritySource.LocalOfflineAdapter,
            encounter,
            currentArmy,
            payload,
            launchRecord,
            true,
            RunBattleError.None,
            "Run battle can launch through the offline adapter.");

        return store == null ? viewData : store.SavePreparedBattle(viewData);
    }

    public RunBattleCompletionResult CompleteBattle(RunBattleCompletionPayload payload)
    {
        if (payload == null || string.IsNullOrEmpty(payload.RunBattleId))
        {
            return Fail(RunBattleError.MissingCompletionPayload, "Battle completion payload is missing.", null);
        }

        RunBattleLaunchViewData prepared = store == null ? null : store.FindPreparedBattle(payload.RunBattleId);
        if (prepared == null)
        {
            return Fail(RunBattleError.MissingPreparedBattle, "Prepared run battle was not found.", null);
        }

        if (payload.PlayerArmyAfterBattle == null)
        {
            return Fail(RunBattleError.MissingCurrentArmy, "Battle completion army snapshot is missing.", null);
        }

        RunBattleArmySnapshot before = CloneArmy(prepared.CurrentArmy, prepared.CurrentArmy.SnapshotId);
        RunBattleArmySnapshot after = CloneArmy(payload.PlayerArmyAfterBattle, payload.PlayerArmyAfterBattle.SnapshotId);
        List<RunBattleStackLossRecord> losses = BuildLosses(before, after);
        int totalLosses = CountTotalLosses(losses);
        RunBattleCompletionRecord record = new RunBattleCompletionRecord(
            "battle-completion-" + Guid.NewGuid().ToString("N"),
            prepared.RunBattleId,
            prepared.RunId,
            prepared.RouteNodeId,
            prepared.Encounter.EncounterId,
            payload.Outcome,
            DetermineNextScreen(prepared.Encounter.NodeType, payload.Outcome),
            before,
            after,
            losses,
            totalLosses,
            payload.RunGoldGained,
            string.IsNullOrEmpty(payload.CompletionPayloadId)
                ? "completion-payload-" + Guid.NewGuid().ToString("N")
                : payload.CompletionPayloadId,
            string.IsNullOrEmpty(payload.ResultSource) ? CompletionResultSource : payload.ResultSource);

        RunBattleCompletionRecord persistedRecord = store == null ? record : store.SaveCompletion(record);
        return new RunBattleCompletionResult(true, RunBattleError.None, "Battle completion recorded.", persistedRecord ?? record);
    }

    private RunBattleLaunchViewData EmptyLaunchView(RunBattlePrepareRequest request, RunBattleError error)
    {
        return new RunBattleLaunchViewData(
            string.Empty,
            request == null ? string.Empty : request.RunId,
            request == null ? string.Empty : request.RouteNodeId,
            request == null ? 0 : request.StageIndex,
            request == null ? 0 : request.RunCurrency,
            RunBattleGameMode.Offline,
            RunBattleAuthoritySource.LocalOfflineAdapter,
            null,
            request == null ? null : request.CurrentArmy,
            null,
            null,
            false,
            error,
            MessageFor(error));
    }

    private RunBattleError ValidatePrepareRequest(RunBattlePrepareRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.RunId))
        {
            return RunBattleError.MissingRun;
        }

        if (string.IsNullOrEmpty(request.RouteNodeId))
        {
            return RunBattleError.MissingRouteNode;
        }

        if (string.IsNullOrEmpty(request.EncounterId))
        {
            return RunBattleError.MissingEncounter;
        }

        if (request.CurrentArmy == null || request.CurrentArmy.Stacks == null || request.CurrentArmy.Stacks.Count == 0)
        {
            return RunBattleError.MissingCurrentArmy;
        }

        if (encounterSource == null)
        {
            return RunBattleError.MissingEncounter;
        }

        return RunBattleError.None;
    }

    private static RunBattleNextScreen DetermineNextScreen(RunBattleNodeType nodeType, RunBattleOutcome outcome)
    {
        if (outcome != RunBattleOutcome.Win)
        {
            return RunBattleNextScreen.RunLoss;
        }

        return RunBattleNextScreen.RunMap;
    }

    private static List<RunBattleStackLossRecord> BuildLosses(RunBattleArmySnapshot before, RunBattleArmySnapshot after)
    {
        List<RunBattleStackLossRecord> losses = new List<RunBattleStackLossRecord>();
        if (before == null || before.Stacks == null)
        {
            return losses;
        }

        for (int i = 0; i < before.Stacks.Count; i++)
        {
            RunBattleStackSnapshot beforeStack = before.Stacks[i];
            if (beforeStack == null)
            {
                continue;
            }

            RunBattleStackSnapshot afterStack = FindStack(after, beforeStack.StackId);
            int afterAmount = afterStack == null ? 0 : afterStack.Amount;
            RunBattleStackLossRecord loss = new RunBattleStackLossRecord(
                beforeStack.StackId,
                beforeStack.UnitId,
                beforeStack.Amount,
                afterAmount);
            if (loss.LostAmount > 0)
            {
                losses.Add(loss);
            }
        }

        return losses;
    }

    private static int CountTotalLosses(List<RunBattleStackLossRecord> losses)
    {
        int total = 0;
        if (losses == null)
        {
            return total;
        }

        for (int i = 0; i < losses.Count; i++)
        {
            if (losses[i] != null)
            {
                total += losses[i].LostAmount;
            }
        }

        return total;
    }

    private static RunBattleStackSnapshot FindStack(RunBattleArmySnapshot army, string stackId)
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

    private static RunBattleArmySnapshot CloneArmy(RunBattleArmySnapshot army, string snapshotId)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                RunBattleStackSnapshot stack = army.Stacks[i];
                if (stack != null)
                {
                    stacks.Add(new RunBattleStackSnapshot(
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

        return new RunBattleArmySnapshot(snapshotId, army == null ? 0 : army.TotalArmyValue, stacks);
    }

    private static List<RunBattleSkillState> CloneSkills(List<RunBattleSkillState> skills)
    {
        List<RunBattleSkillState> copy = new List<RunBattleSkillState>();
        if (skills == null)
        {
            return copy;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                copy.Add(new RunBattleSkillState(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return copy;
    }

    private static RunBattleCompletionResult Fail(RunBattleError error, string message, RunBattleCompletionRecord record)
    {
        return new RunBattleCompletionResult(false, error, message, record);
    }

    private static string MessageFor(RunBattleError error)
    {
        switch (error)
        {
            case RunBattleError.None:
                return "Run battle can launch.";
            case RunBattleError.MissingRun:
                return "Run id is missing.";
            case RunBattleError.MissingRouteNode:
                return "Route node id is missing.";
            case RunBattleError.MissingEncounter:
                return "Battle encounter is missing.";
            case RunBattleError.MissingCurrentArmy:
                return "Current run army snapshot is missing.";
            case RunBattleError.MissingPreparedBattle:
                return "Prepared run battle was not found.";
            case RunBattleError.MissingCompletionPayload:
                return "Battle completion payload is missing.";
            default:
                return "Run battle failed.";
        }
    }
}
