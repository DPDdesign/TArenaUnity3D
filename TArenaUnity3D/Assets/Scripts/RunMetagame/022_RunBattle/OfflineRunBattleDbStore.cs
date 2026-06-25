using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class OfflineRunBattleDbStore : IRunBattleStore
{
    private readonly string databasePath;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly IRunBattleEncounterSource encounterSource;
    private readonly IRewardMapUnitDefinitionSource rewardUnitSource;
    private readonly EnemyEncounterRuleCatalog enemyEncounterRuleCatalog;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();
    private readonly OfflineRunContextDbWriter runContextWriter = new OfflineRunContextDbWriter();

    public OfflineRunBattleDbStore(
        string databasePath,
        IOfflineArmySnapshotCatalogResolver resolver,
        IRunBattleEncounterSource encounterSource)
        : this(databasePath, resolver, encounterSource, null, null)
    {
    }

    public OfflineRunBattleDbStore(
        string databasePath,
        IOfflineArmySnapshotCatalogResolver resolver,
        IRunBattleEncounterSource encounterSource,
        IRewardMapUnitDefinitionSource rewardUnitSource)
        : this(databasePath, resolver, encounterSource, rewardUnitSource, null)
    {
    }

    public OfflineRunBattleDbStore(
        string databasePath,
        IOfflineArmySnapshotCatalogResolver resolver,
        IRunBattleEncounterSource encounterSource,
        IRewardMapUnitDefinitionSource rewardUnitSource,
        EnemyEncounterRuleCatalog enemyEncounterRuleCatalog)
    {
        this.databasePath = databasePath;
        this.resolver = resolver;
        this.encounterSource = encounterSource ?? new DefaultRunBattleEncounterCatalog();
        this.rewardUnitSource = rewardUnitSource;
        this.enemyEncounterRuleCatalog = enemyEncounterRuleCatalog;
    }

    public RunBattleLaunchViewData SavePreparedBattle(RunBattleLaunchViewData preparedBattle)
    {
        if (preparedBattle == null)
        {
            return null;
        }

        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(preparedBattle.RunId);
        if (runId <= 0)
        {
            throw new InvalidOperationException("Run battle DB store requires a persisted run id.");
        }

        int persistedRunBattleId;

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            RunRow run = LoadRun(connection, transaction, runId);
            int nodeId = ResolveNodeId(connection, transaction, runId, preparedBattle.RouteNodeId, preparedBattle.Encounter == null ? string.Empty : preparedBattle.Encounter.EncounterId, run.CurrentNodeId);
            if (nodeId <= 0)
            {
                throw new InvalidOperationException("Could not resolve route node id for run battle persistence.");
            }

            string now = OfflineDatabaseSql.UtcNowText();
            persistedRunBattleId = ReadNextId(connection, "run_battles", "run_battle_id", transaction);
            RunBattleLaunchViewData persistedView = ClonePreparedBattle(preparedBattle, persistedRunBattleId);
            int snapshotId = snapshotRepository.SaveSnapshot(
                connection,
                transaction,
                OfflineArmySnapshotMapper.FromRunBattle(persistedView.CurrentArmy, run.AccountId, runId, nodeId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO run_events (
    run_id,
    account_id,
    node_id,
    event_type_id,
    before_snapshot_id,
    after_snapshot_id,
    run_gold_before,
    run_gold_after,
    result,
    created_at_utc,
    is_active
) VALUES (
    @runId,
    @accountId,
    @nodeId,
    @eventTypeId,
    @beforeSnapshotId,
    NULL,
    @runGoldBefore,
    NULL,
    @result,
    @createdAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@accountId", run.AccountId),
                new OfflineDatabaseSqlParameter("@nodeId", nodeId),
                new OfflineDatabaseSqlParameter("@eventTypeId", (int)DBEventTypeId.Battle),
                new OfflineDatabaseSqlParameter("@beforeSnapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@runGoldBefore", preparedBattle.RunCurrency),
                new OfflineDatabaseSqlParameter("@result", preparedBattle.Message),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
            int eventId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO run_battles (
    run_battle_id,
    event_id,
    run_id,
    node_id,
    encounter_id,
    enemy_goal,
    battle_status_id,
    pre_battle_snapshot_id,
    post_battle_snapshot_id,
    launch_payload_json,
    launch_adapter_surface,
    battle_outcome_id,
    result_source,
    next_screen,
    prepared_at_utc,
    completed_at_utc,
    is_active
) VALUES (
    @runBattleId,
    @eventId,
    @runId,
    @nodeId,
    @encounterId,
    @enemyGoal,
    @battleStatusId,
    @preBattleSnapshotId,
    NULL,
    @launchPayloadJson,
    @launchAdapterSurface,
    NULL,
    @resultSource,
    @nextScreen,
    @preparedAtUtc,
    NULL,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runBattleId", persistedRunBattleId),
                new OfflineDatabaseSqlParameter("@eventId", eventId),
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@nodeId", nodeId),
                new OfflineDatabaseSqlParameter("@encounterId", persistedView.Encounter == null ? string.Empty : persistedView.Encounter.EncounterId),
                new OfflineDatabaseSqlParameter("@enemyGoal", persistedView.Encounter == null ? string.Empty : persistedView.Encounter.EnemyGoal.ToString()),
                new OfflineDatabaseSqlParameter("@battleStatusId", (int)DBBattleStatusId.Prepared),
                new OfflineDatabaseSqlParameter("@preBattleSnapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@launchPayloadJson", JsonUtility.ToJson(persistedView.LaunchPayload)),
                new OfflineDatabaseSqlParameter("@launchAdapterSurface", persistedView.LaunchRecord == null ? string.Empty : persistedView.LaunchRecord.AdapterSurface),
                new OfflineDatabaseSqlParameter("@resultSource", persistedView.LaunchPayload == null ? string.Empty : persistedView.LaunchPayload.ResultSource),
                new OfflineDatabaseSqlParameter("@nextScreen", RunBattleNextScreen.Battle.ToString()),
                new OfflineDatabaseSqlParameter("@preparedAtUtc", now));

            runContextWriter.UpdateNodeArmyGoldScreen(
                connection,
                transaction,
                runId,
                nodeId,
                snapshotId,
                preparedBattle.RunCurrency,
                (int)DBRunStatusId.AwaitingBattle,
                RunBattleNextScreen.Battle.ToString());

            transaction.Commit();
        }

        return FindPreparedBattle(OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(persistedRunBattleId));
    }

    public RunBattleLaunchViewData FindPreparedBattle(string runBattleId)
    {
        int parsedRunBattleId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runBattleId);
        if (parsedRunBattleId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<BattleRow> rows = OfflineDatabaseSql.Query(
                connection,
                @"
SELECT rb.run_battle_id, rb.run_id, rb.node_id, rb.encounter_id, rb.enemy_goal, rb.pre_battle_snapshot_id,
       rb.launch_payload_json, rb.launch_adapter_surface, rb.result_source, rb.next_screen,
       ev.run_gold_before
FROM run_battles rb
INNER JOIN run_events ev ON ev.event_id = rb.event_id
WHERE rb.run_battle_id = @runBattleId AND rb.is_active = 1
LIMIT 1;",
                delegate(IDataRecord row)
                {
                    return new BattleRow
                    {
                        RunBattleId = OfflineDatabaseSql.ReadInt(row["run_battle_id"]),
                        RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                        NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                        EncounterId = OfflineDatabaseSql.ReadText(row["encounter_id"]),
                        EnemyGoal = OfflineDatabaseSql.ReadText(row["enemy_goal"]),
                        SnapshotId = OfflineDatabaseSql.ReadInt(row["pre_battle_snapshot_id"]),
                        LaunchPayloadJson = OfflineDatabaseSql.ReadText(row["launch_payload_json"]),
                        LaunchAdapterSurface = OfflineDatabaseSql.ReadText(row["launch_adapter_surface"]),
                        ResultSource = OfflineDatabaseSql.ReadText(row["result_source"]),
                        NextScreen = OfflineDatabaseSql.ReadText(row["next_screen"]),
                        RunGoldBefore = OfflineDatabaseSql.ReadInt(row["run_gold_before"])
                    };
                },
                null,
                new OfflineDatabaseSqlParameter("@runBattleId", parsedRunBattleId));

            if (rows.Count == 0)
            {
                return null;
            }

            BattleRow row = rows[0];
            OfflineArmySnapshotRecord snapshot = snapshotRepository.LoadSnapshot(connection, row.SnapshotId);
            RunBattleArmySnapshot army = OfflineArmySnapshotMapper.ToRunBattle(snapshot, resolver);
            RunBattleEncounterDefinition encounter = encounterSource == null
                ? null
                : encounterSource.FindEncounter(OfflineDatabaseLegacyIdentity.ToLegacyFormationSlotId(row.NodeId), row.EncounterId);
            if (encounter == null)
            {
                encounter = new RunBattleEncounterDefinition(
                    row.EncounterId,
                    OfflineDatabaseLegacyIdentity.ToLegacyFormationSlotId(row.NodeId),
                    row.NextScreen == RunBattleNextScreen.FinalSummary.ToString() ? RunBattleNodeType.Final : RunBattleNodeType.Battle,
                    row.EncounterId,
                    string.Empty,
                    0,
                    string.Empty,
                    ParseEnemyGoal(row.EnemyGoal));
            }

            RunBattleLaunchPayload payload = string.IsNullOrEmpty(row.LaunchPayloadJson)
                ? null
                : JsonUtility.FromJson<RunBattleLaunchPayload>(row.LaunchPayloadJson);
            if (payload == null)
            {
                payload = new RunBattleLaunchPayload(
                    OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(row.RunBattleId),
                    OfflineDatabaseLegacyIdentity.ToLegacyRunId(row.RunId),
                    encounter.RouteNodeId,
                    row.EncounterId,
                    OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(row.SnapshotId),
                    encounter.EnemyArmySourceId,
                    encounter.EnemyGoal,
                    row.ResultSource);
            }

            payload.RunBattleId = OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(row.RunBattleId);
            payload.RunId = OfflineDatabaseLegacyIdentity.ToLegacyRunId(row.RunId);
            payload.CurrentArmySnapshotId = OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(row.SnapshotId);
            string routeNodeId = string.IsNullOrEmpty(payload.RouteNodeId) ? encounter.RouteNodeId : payload.RouteNodeId;

            RunBattleLaunchRecord launchRecord = new RunBattleLaunchRecord(
                "launch-record-" + row.RunBattleId,
                payload.RunBattleId,
                string.Empty,
                string.Empty,
                row.LaunchAdapterSurface,
                row.ResultSource);

            return new RunBattleLaunchViewData(
                payload.RunBattleId,
                payload.RunId,
                routeNodeId,
                0,
                row.RunGoldBefore,
                RunBattleGameMode.Offline,
                RunBattleAuthoritySource.LocalOfflineAdapter,
                encounter,
                army,
                payload,
                launchRecord,
                true,
                RunBattleError.None,
                "Run battle can launch through the offline adapter.");
        }
    }

    public RunBattleCompletionRecord SaveCompletion(RunBattleCompletionRecord completionRecord)
    {
        if (completionRecord == null)
        {
            return null;
        }

        int parsedRunBattleId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(completionRecord.RunBattleId);
        if (parsedRunBattleId <= 0)
        {
            throw new InvalidOperationException("Run battle completion requires a persisted run battle id.");
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            PersistedBattle persistedBattle = LoadPersistedBattle(connection, transaction, parsedRunBattleId);
            if (persistedBattle == null)
            {
                throw new InvalidOperationException("Prepared run battle was not found in the database.");
            }

            int runGoldAfter = Math.Max(0, persistedBattle.RunGoldBefore + completionRecord.RunGoldGained);
            int postBattleSnapshotId = snapshotRepository.SaveSnapshot(
                connection,
                transaction,
                OfflineArmySnapshotMapper.FromRunBattle(completionRecord.ArmyAfterBattle, persistedBattle.AccountId, persistedBattle.RunId, persistedBattle.NodeId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE run_events
SET after_snapshot_id = @afterSnapshotId,
    run_gold_after = @runGoldAfter,
    result = @result
WHERE event_id = @eventId;",
                transaction,
                new OfflineDatabaseSqlParameter("@afterSnapshotId", postBattleSnapshotId),
                new OfflineDatabaseSqlParameter("@runGoldAfter", runGoldAfter),
                new OfflineDatabaseSqlParameter("@result", completionRecord.Outcome.ToString()),
                new OfflineDatabaseSqlParameter("@eventId", persistedBattle.EventId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE run_battles
SET battle_status_id = @battleStatusId,
    post_battle_snapshot_id = @postBattleSnapshotId,
    battle_outcome_id = @battleOutcomeId,
    result_source = @resultSource,
    next_screen = @nextScreen,
    completed_at_utc = @completedAtUtc
WHERE run_battle_id = @runBattleId;",
                transaction,
                new OfflineDatabaseSqlParameter("@battleStatusId", (int)DBBattleStatusId.Completed),
                new OfflineDatabaseSqlParameter("@postBattleSnapshotId", postBattleSnapshotId),
                new OfflineDatabaseSqlParameter("@battleOutcomeId", ToBattleOutcomeId(completionRecord.Outcome)),
                new OfflineDatabaseSqlParameter("@resultSource", completionRecord.ResultSource),
                new OfflineDatabaseSqlParameter("@nextScreen", completionRecord.NextScreen.ToString()),
                new OfflineDatabaseSqlParameter("@completedAtUtc", OfflineDatabaseSql.UtcNowText()),
                new OfflineDatabaseSqlParameter("@runBattleId", parsedRunBattleId));

            Dictionary<int, int> snapshotStackIdsByFormationSlot = snapshotRepository.LoadSnapshotStackIdsByFormationSlot(connection, persistedBattle.PreBattleSnapshotId, transaction);
            InsertLosses(connection, transaction, parsedRunBattleId, completionRecord.Losses, snapshotStackIdsByFormationSlot);

            runContextWriter.UpdateArmyGoldScreen(
                connection,
                transaction,
                persistedBattle.RunId,
                postBattleSnapshotId,
                runGoldAfter,
                ToRunStatusId(completionRecord.NextScreen),
                completionRecord.NextScreen.ToString());

            int runSeed = persistedBattle.RunSeed;
            int runSeedVersion = persistedBattle.RunSeedVersion;
            int nodeId = persistedBattle.NodeId;
            int runId = persistedBattle.RunId;
            transaction.Commit();

            RunBattleArmySnapshot persistedBefore = OfflineArmySnapshotMapper.ToRunBattle(snapshotRepository.LoadSnapshot(connection, persistedBattle.PreBattleSnapshotId), resolver);
            RunBattleArmySnapshot persistedAfter = OfflineArmySnapshotMapper.ToRunBattle(snapshotRepository.LoadSnapshot(connection, postBattleSnapshotId), resolver);
            RunBattleCompletionRecord result = new RunBattleCompletionRecord(
                completionRecord.CompletionRecordId,
                OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(parsedRunBattleId),
                OfflineDatabaseLegacyIdentity.ToLegacyRunId(persistedBattle.RunId),
                completionRecord.RouteNodeId,
                completionRecord.EncounterId,
                completionRecord.Outcome,
                completionRecord.NextScreen,
                persistedBefore,
                persistedAfter,
                completionRecord.Losses,
                completionRecord.TotalLosses,
                completionRecord.RunGoldGained,
                completionRecord.CompletionPayloadId,
                completionRecord.ResultSource);

            MaterializeRewardsIfNeeded(
                parsedRunBattleId,
                runId,
                nodeId,
                runSeed,
                runSeedVersion,
                runGoldAfter,
                result);

            return result;
        }
    }

    private void MaterializeRewardsIfNeeded(
        int runBattleId,
        int runId,
        int nodeId,
        int runSeed,
        int runSeedVersion,
        int runGoldAfterBattle,
        RunBattleCompletionRecord completionRecord)
    {
        if (completionRecord == null || completionRecord.NextScreen != RunBattleNextScreen.Reward)
        {
            return;
        }

        OfflineRewardMapDbStore store = new OfflineRewardMapDbStore(databasePath, resolver);
        if (store.FindChoiceForRunNode(OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId)) != null)
        {
            return;
        }

        IRewardMapUnitDefinitionSource source = rewardUnitSource ?? new RewardMapResolverUnitSource(resolver);
        RewardMapArmySnapshot rewardArmy = ToRewardMapArmy(completionRecord.ArmyAfterBattle);
        List<RewardMapOperationType> plannedOperationTypes = LoadPlannedRewardOperationTypes(runId, nodeId, runSeedVersion);
        RewardGeneratorRuleSet rewardRuleSet = ResolveRewardRuleSet(nodeId);
        RewardGeneratorValueContext valueContext = rewardRuleSet == null
            ? null
            : new RewardGeneratorValueContext(
                CalculateArmyValue(completionRecord.ArmyBeforeBattle) - CalculateArmyValue(completionRecord.ArmyAfterBattle),
                LoadEnemyArmyValue(nodeId),
                CalculateArmyValue(completionRecord.ArmyBeforeBattle));

        RewardMapChoiceViewData choice = new RewardMapMaterializedGenerator(source, rewardRuleSet, valueContext).BuildChoice(
            OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId),
            nodeId,
            runSeed,
            runSeedVersion,
            runGoldAfterBattle,
            rewardArmy,
            new RewardMapBattleResultSummary(
                OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(runBattleId),
                completionRecord.Outcome.ToString(),
                completionRecord.TotalLosses,
                completionRecord.RunGoldGained),
            plannedOperationTypes.Count == 0 ? null : plannedOperationTypes);

        store.SaveChoice(choice);
    }

    private RewardGeneratorRuleSet ResolveRewardRuleSet(int nodeId)
    {
        if (enemyEncounterRuleCatalog == null)
        {
            return null;
        }

        EnemyEncounterDifficulty difficulty = LoadEnemyEncounterDifficulty(nodeId);
        EnemyEncounterRuleLookupResult lookup = enemyEncounterRuleCatalog.Resolve(difficulty);
        if (lookup == null || !lookup.Success || lookup.Rule == null)
        {
            throw new InvalidOperationException(lookup == null ? "Enemy encounter reward rule lookup failed." : lookup.Message);
        }

        RewardGeneratorRuleSet ruleSet = lookup.Rule.ResolvedRewardGeneratorRuleSet;
        if (ruleSet == null)
        {
            throw new InvalidOperationException("Generated reward-producing battle is missing a RewardGeneratorRuleSet.");
        }

        return ruleSet;
    }

    private EnemyEncounterDifficulty LoadEnemyEncounterDifficulty(int nodeId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object result = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT enemy_rule_id
FROM map_node_enemies
WHERE node_id = @nodeId AND is_active = 1
LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@nodeId", nodeId));
            string value = OfflineDatabaseSql.ReadText(result);
            EnemyEncounterDifficulty parsed;
            if (Enum.TryParse(value, true, out parsed))
            {
                return parsed;
            }
        }

        throw new InvalidOperationException("Reward materialization requires a materialized enemy rule id for the completed battle node.");
    }

    private int LoadEnemyArmyValue(int nodeId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object result = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT army_snapshot_id
FROM map_node_enemies
WHERE node_id = @nodeId AND is_active = 1
LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@nodeId", nodeId));
            int snapshotId = OfflineDatabaseSql.ReadInt(result);
            OfflineArmySnapshotRecord snapshot = snapshotRepository.LoadSnapshot(connection, snapshotId);
            int value = CalculateArmyValue(snapshot);
            if (value > 0)
            {
                return value;
            }
        }

        throw new InvalidOperationException("Reward materialization requires a materialized enemy army snapshot with a positive value.");
    }

    private List<RewardMapOperationType> LoadPlannedRewardOperationTypes(int runId, int nodeId, int runSeedVersion)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return OfflineRewardOpportunityDbStore.LoadPlannedOperationTypes(
                connection,
                null,
                runId,
                nodeId,
                runSeedVersion);
        }
    }

    private static RewardMapArmySnapshot ToRewardMapArmy(RunBattleArmySnapshot army)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = army.Stacks[i];
            if (stack == null)
            {
                continue;
            }

            List<RewardMapSkillState> skills = new List<RewardMapSkillState>();
            for (int skillIndex = 0; stack.Skills != null && skillIndex < stack.Skills.Count; skillIndex++)
            {
                RunBattleSkillState skill = stack.Skills[skillIndex];
                if (skill != null)
                {
                    skills.Add(new RewardMapSkillState(skill.SkillId, skill.Unlocked));
                }
            }

            stacks.Add(new RewardMapStackSnapshot(
                stack.StackId,
                stack.UnitId,
                stack.DisplayName,
                stack.Tier,
                stack.Level,
                stack.Amount,
                stack.Lost,
                stack.CombatValue,
                skills));
        }

        return new RewardMapArmySnapshot(army == null ? string.Empty : army.SnapshotId, army == null ? 0 : army.TotalArmyValue, stacks);
    }

    private int CalculateArmyValue(RunBattleArmySnapshot army)
    {
        if (army == null)
        {
            return 0;
        }

        int stackTotal = 0;
        for (int i = 0; army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = army.Stacks[i];
            stackTotal += stack == null ? 0 : Math.Max(0, stack.CombatValue);
        }

        return Math.Max(Math.Max(0, army.TotalArmyValue), stackTotal);
    }

    private int CalculateArmyValue(OfflineArmySnapshotRecord snapshot)
    {
        int total = 0;
        for (int i = 0; snapshot != null && snapshot.Stacks != null && i < snapshot.Stacks.Count; i++)
        {
            OfflineArmySnapshotStackRecord stack = snapshot.Stacks[i];
            if (stack == null || !stack.IsActive || stack.Amount <= 0)
            {
                continue;
            }

            OfflineArmySnapshotUnitCatalogEntry unit = resolver == null ? null : resolver.FindUnit(stack.UnitId);
            int unitValue = unit == null ? 0 : unit.CombatValue;
            total += stack.Amount * Math.Max(0, unitValue);
        }

        return Math.Max(0, total);
    }

    private static RunBattleLaunchViewData ClonePreparedBattle(RunBattleLaunchViewData source, int runBattleId)
    {
        string persistedRunBattleId = OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(runBattleId);
        RunBattleLaunchPayload payload = source.LaunchPayload == null
            ? null
            : new RunBattleLaunchPayload(
                persistedRunBattleId,
                source.RunId,
                source.RouteNodeId,
                source.LaunchPayload.EncounterId,
                source.LaunchPayload.CurrentArmySnapshotId,
                source.LaunchPayload.EnemyArmySourceId,
                source.LaunchPayload.EnemyGoal,
                source.LaunchPayload.ResultSource);
        RunBattleLaunchRecord launchRecord = source.LaunchRecord == null
            ? null
            : new RunBattleLaunchRecord(
                source.LaunchRecord.BattleLaunchRecordId,
                persistedRunBattleId,
                source.LaunchRecord.PlayerArmyInputId,
                source.LaunchRecord.EnemyArmyInputId,
                source.LaunchRecord.AdapterSurface,
                source.LaunchRecord.ResultSource);

        return new RunBattleLaunchViewData(
            persistedRunBattleId,
            source.RunId,
            source.RouteNodeId,
            source.StageIndex,
            source.RunCurrency,
            source.GameMode,
            source.AuthoritySource,
            source.Encounter,
            source.CurrentArmy,
            payload,
            launchRecord,
            source.CanLaunch,
            source.Error,
            source.Message);
    }

    private static void InsertLosses(
        IDbConnection connection,
        IDbTransaction transaction,
        int runBattleId,
        List<RunBattleStackLossRecord> losses,
        Dictionary<int, int> snapshotStackIdsByFormationSlot)
    {
        if (losses == null)
        {
            return;
        }

        for (int i = 0; i < losses.Count; i++)
        {
            RunBattleStackLossRecord loss = losses[i];
            if (loss == null)
            {
                continue;
            }

            int formationSlot = OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault(loss.StackId, i);
            int snapshotStackId;
            if (!snapshotStackIdsByFormationSlot.TryGetValue(formationSlot, out snapshotStackId))
            {
                continue;
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO run_battle_losses (
    run_battle_id,
    snapshot_stack_id,
    unit_id,
    amount_before,
    amount_after,
    lost_amount,
    is_active
) VALUES (
    @runBattleId,
    @snapshotStackId,
    @unitId,
    @amountBefore,
    @amountAfter,
    @lostAmount,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runBattleId", runBattleId),
                new OfflineDatabaseSqlParameter("@snapshotStackId", snapshotStackId),
                new OfflineDatabaseSqlParameter("@unitId", loss.UnitId),
                new OfflineDatabaseSqlParameter("@amountBefore", loss.AmountBefore),
                new OfflineDatabaseSqlParameter("@amountAfter", loss.AmountAfter),
                new OfflineDatabaseSqlParameter("@lostAmount", loss.LostAmount));
        }
    }

    private static int ReadNextId(IDbConnection connection, string tableName, string keyColumn, IDbTransaction transaction)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT COALESCE(MAX(" + keyColumn + "), 0) + 1 FROM " + tableName + ";",
            transaction);
        return OfflineDatabaseSql.ReadInt(result, 1);
    }

    private static int ToBattleOutcomeId(RunBattleOutcome outcome)
    {
        switch (outcome)
        {
            case RunBattleOutcome.Win:
                return (int)DBBattleOutcomeId.Win;
            case RunBattleOutcome.Loss:
                return (int)DBBattleOutcomeId.Loss;
            default:
                return (int)DBBattleOutcomeId.Cancelled;
        }
    }

    private static int ToRunStatusId(RunBattleNextScreen nextScreen)
    {
        switch (nextScreen)
        {
            case RunBattleNextScreen.RunMap:
                return (int)DBRunStatusId.InProgress;
            case RunBattleNextScreen.Reward:
                return (int)DBRunStatusId.AwaitingReward;
            case RunBattleNextScreen.FinalSummary:
                return (int)DBRunStatusId.Won;
            case RunBattleNextScreen.RunLoss:
                return (int)DBRunStatusId.Lost;
            default:
                return (int)DBRunStatusId.AwaitingBattle;
        }
    }

    private static RunBattleEnemyGoal ParseEnemyGoal(string value)
    {
        RunBattleEnemyGoal parsed;
        return Enum.TryParse(value, true, out parsed) ? parsed : RunBattleEnemyGoal.TryToWin;
    }

    private int ResolveNodeId(IDbConnection connection, IDbTransaction transaction, int runId, string routeNodeId, string encounterId, int currentNodeId)
    {
        int parsedRouteNodeId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(routeNodeId);
        if (parsedRouteNodeId > 0)
        {
            return parsedRouteNodeId;
        }

        if (currentNodeId > 0)
        {
            return currentNodeId;
        }

        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT nodes.node_id
FROM map_nodes nodes
WHERE nodes.run_id = @runId
  AND nodes.encounter_id = @encounterId
  AND nodes.is_active = 1
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@encounterId", encounterId));
        return OfflineDatabaseSql.ReadInt(result);
    }

    private static RunRow LoadRun(IDbConnection connection, IDbTransaction transaction, int runId)
    {
        List<RunRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT account_id, current_node_id
FROM offline_runs
WHERE run_id = @runId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new RunRow
                {
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"]),
                    CurrentNodeId = OfflineDatabaseSql.ReadInt(row["current_node_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Offline run was not found for battle persistence.");
        }

        return rows[0];
    }

    private static PersistedBattle LoadPersistedBattle(IDbConnection connection, IDbTransaction transaction, int runBattleId)
    {
        List<PersistedBattle> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT rb.event_id, rb.run_id, rb.node_id, rb.pre_battle_snapshot_id, ev.account_id, ev.run_gold_before,
       runs.run_seed, runs.run_seed_version
FROM run_battles rb
INNER JOIN run_events ev ON ev.event_id = rb.event_id
INNER JOIN offline_runs runs ON runs.run_id = rb.run_id
WHERE rb.run_battle_id = @runBattleId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new PersistedBattle
                {
                    EventId = OfflineDatabaseSql.ReadInt(row["event_id"]),
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    PreBattleSnapshotId = OfflineDatabaseSql.ReadInt(row["pre_battle_snapshot_id"]),
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"]),
                    RunGoldBefore = OfflineDatabaseSql.ReadInt(row["run_gold_before"]),
                    RunSeed = OfflineDatabaseSql.ReadInt(row["run_seed"], 35035),
                    RunSeedVersion = OfflineDatabaseSql.ReadInt(row["run_seed_version"], 1)
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runBattleId", runBattleId));

        return rows.Count == 0 ? null : rows[0];
    }

    private sealed class RunRow
    {
        public int AccountId;
        public int CurrentNodeId;
    }

    private sealed class PersistedBattle
    {
        public int EventId;
        public int RunId;
        public int NodeId;
        public int PreBattleSnapshotId;
        public int AccountId;
        public int RunGoldBefore;
        public int RunSeed;
        public int RunSeedVersion;
    }

    private sealed class RewardMapResolverUnitSource : IRewardMapUnitDefinitionSource
    {
        private readonly IOfflineArmySnapshotCatalogResolver resolver;

        public RewardMapResolverUnitSource(IOfflineArmySnapshotCatalogResolver resolver)
        {
            this.resolver = resolver;
        }

        public RunShopUnitDefinition FindUnit(string unitId)
        {
            OfflineArmySnapshotUnitCatalogEntry unit = resolver == null ? null : resolver.FindUnit(unitId);
            return unit == null
                ? null
                : new RunShopUnitDefinition(unit.UnitId, unit.DisplayName, unit.Tier, unit.CombatValue, new List<string>(unit.SkillIds));
        }
    }

    private sealed class BattleRow
    {
        public int RunBattleId;
        public int RunId;
        public int NodeId;
        public string EncounterId;
        public string EnemyGoal;
        public int SnapshotId;
        public string LaunchPayloadJson;
        public string LaunchAdapterSurface;
        public string ResultSource;
        public string NextScreen;
        public int RunGoldBefore;
    }
}
