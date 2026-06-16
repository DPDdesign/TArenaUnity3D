using System;
using System.Collections.Generic;
using System.Data;

public sealed class OfflineArmySnapshotDbRepository
{
    public int SaveSnapshot(IDbConnection connection, IDbTransaction transaction, OfflineArmySnapshotRecord snapshot)
    {
        if (connection == null || snapshot == null)
        {
            return 0;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO army_snapshots (
    account_id,
    run_id,
    saved_army_id,
    node_id,
    created_at_utc,
    is_active
) VALUES (
    @accountId,
    @runId,
    @savedArmyId,
    @nodeId,
    @createdAtUtc,
    @isActive
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", snapshot.AccountId),
            new OfflineDatabaseSqlParameter("@runId", snapshot.RunId > 0 ? (object)snapshot.RunId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@savedArmyId", snapshot.SavedArmyId > 0 ? (object)snapshot.SavedArmyId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@nodeId", snapshot.NodeId > 0 ? (object)snapshot.NodeId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@createdAtUtc", string.IsNullOrEmpty(snapshot.CreatedAtUtc) ? OfflineDatabaseSql.UtcNowText() : snapshot.CreatedAtUtc),
            new OfflineDatabaseSqlParameter("@isActive", snapshot.IsActive ? 1 : 0));

        int snapshotId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
        if (snapshot.Stacks == null)
        {
            return snapshotId;
        }

        for (int stackIndex = 0; stackIndex < snapshot.Stacks.Count; stackIndex++)
        {
            OfflineArmySnapshotStackRecord stack = snapshot.Stacks[stackIndex];
            if (stack == null)
            {
                continue;
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO army_snapshot_stacks (
    snapshot_id,
    unit_id,
    amount,
    formation_slot,
    is_active
) VALUES (
    @snapshotId,
    @unitId,
    @amount,
    @formationSlot,
    @isActive
);",
                transaction,
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@unitId", stack.UnitId),
                new OfflineDatabaseSqlParameter("@amount", stack.Amount),
                new OfflineDatabaseSqlParameter("@formationSlot", stack.FormationSlot),
                new OfflineDatabaseSqlParameter("@isActive", stack.IsActive ? 1 : 0));

            int snapshotStackId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
            SaveSkills(connection, transaction, snapshotStackId, stack.Skills);
        }

        return snapshotId;
    }

    public OfflineArmySnapshotRecord LoadSnapshot(IDbConnection connection, int snapshotId, IDbTransaction transaction = null)
    {
        if (connection == null || snapshotId <= 0)
        {
            return null;
        }

        List<OfflineArmySnapshotRecord> snapshots = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT snapshot_id, account_id, run_id, saved_army_id, node_id, created_at_utc, is_active
FROM army_snapshots
WHERE snapshot_id = @snapshotId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new OfflineArmySnapshotRecord(
                    OfflineDatabaseSql.ReadInt(row["snapshot_id"]),
                    OfflineDatabaseSql.ReadInt(row["account_id"]),
                    OfflineDatabaseSql.ReadInt(row["run_id"]),
                    OfflineDatabaseSql.ReadInt(row["saved_army_id"]),
                    OfflineDatabaseSql.ReadInt(row["node_id"]),
                    OfflineDatabaseSql.ReadText(row["created_at_utc"]),
                    OfflineDatabaseSql.ReadBool(row["is_active"], true),
                    new List<OfflineArmySnapshotStackRecord>());
            },
            transaction,
            new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));

        if (snapshots.Count == 0)
        {
            return null;
        }

        OfflineArmySnapshotRecord snapshot = snapshots[0];
        Dictionary<int, List<OfflineArmySnapshotStackSkillRecord>> skillsByStackId = LoadSkillsByStackId(connection, snapshotId, transaction);
        snapshot.Stacks = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT snapshot_stack_id, unit_id, amount, formation_slot, is_active
FROM army_snapshot_stacks
WHERE snapshot_id = @snapshotId
ORDER BY formation_slot, snapshot_stack_id;",
            delegate(IDataRecord row)
            {
                int snapshotStackId = OfflineDatabaseSql.ReadInt(row["snapshot_stack_id"]);
                List<OfflineArmySnapshotStackSkillRecord> skills;
                if (!skillsByStackId.TryGetValue(snapshotStackId, out skills))
                {
                    skills = new List<OfflineArmySnapshotStackSkillRecord>();
                }

                return new OfflineArmySnapshotStackRecord(
                    snapshotStackId,
                    OfflineDatabaseSql.ReadText(row["unit_id"]),
                    OfflineDatabaseSql.ReadInt(row["amount"]),
                    OfflineDatabaseSql.ReadInt(row["formation_slot"]),
                    OfflineDatabaseSql.ReadBool(row["is_active"], true),
                    skills);
            },
            transaction,
            new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));

        return snapshot;
    }

    public Dictionary<int, int> LoadSnapshotStackIdsByFormationSlot(IDbConnection connection, int snapshotId, IDbTransaction transaction = null)
    {
        Dictionary<int, int> result = new Dictionary<int, int>();
        if (connection == null || snapshotId <= 0)
        {
            return result;
        }

        List<object[]> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT snapshot_stack_id, formation_slot
FROM army_snapshot_stacks
WHERE snapshot_id = @snapshotId
ORDER BY formation_slot, snapshot_stack_id;",
            delegate(IDataRecord row)
            {
                return new object[]
                {
                    row["snapshot_stack_id"],
                    row["formation_slot"]
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));

        for (int i = 0; i < rows.Count; i++)
        {
            int snapshotStackId = OfflineDatabaseSql.ReadInt(rows[i][0]);
            int formationSlot = OfflineDatabaseSql.ReadInt(rows[i][1]);
            if (!result.ContainsKey(formationSlot))
            {
                result.Add(formationSlot, snapshotStackId);
            }
        }

        return result;
    }

    private static void SaveSkills(
        IDbConnection connection,
        IDbTransaction transaction,
        int snapshotStackId,
        List<OfflineArmySnapshotStackSkillRecord> skills)
    {
        if (skills == null)
        {
            return;
        }

        for (int skillIndex = 0; skillIndex < skills.Count; skillIndex++)
        {
            OfflineArmySnapshotStackSkillRecord skill = skills[skillIndex];
            if (skill == null)
            {
                continue;
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO army_snapshot_stack_skills (
    snapshot_stack_id,
    skill_id,
    acquired_at_run_node_id,
    is_active
) VALUES (
    @snapshotStackId,
    @skillId,
    @acquiredAtRunNodeId,
    @isActive
);",
                transaction,
                new OfflineDatabaseSqlParameter("@snapshotStackId", snapshotStackId),
                new OfflineDatabaseSqlParameter("@skillId", skill.SkillId),
                new OfflineDatabaseSqlParameter("@acquiredAtRunNodeId", skill.AcquiredAtRunNodeId > 0 ? (object)skill.AcquiredAtRunNodeId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@isActive", skill.IsActive ? 1 : 0));
        }
    }

    private static Dictionary<int, List<OfflineArmySnapshotStackSkillRecord>> LoadSkillsByStackId(
        IDbConnection connection,
        int snapshotId,
        IDbTransaction transaction)
    {
        Dictionary<int, List<OfflineArmySnapshotStackSkillRecord>> result = new Dictionary<int, List<OfflineArmySnapshotStackSkillRecord>>();
        List<object[]> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT skills.snapshot_stack_skill_id, skills.snapshot_stack_id, skills.skill_id, skills.acquired_at_run_node_id, skills.is_active
FROM army_snapshot_stack_skills skills
INNER JOIN army_snapshot_stacks stacks ON stacks.snapshot_stack_id = skills.snapshot_stack_id
WHERE stacks.snapshot_id = @snapshotId
ORDER BY skills.snapshot_stack_skill_id;",
            delegate(IDataRecord row)
            {
                return new object[]
                {
                    row["snapshot_stack_skill_id"],
                    row["snapshot_stack_id"],
                    row["skill_id"],
                    row["acquired_at_run_node_id"],
                    row["is_active"]
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));

        for (int i = 0; i < rows.Count; i++)
        {
            int snapshotStackId = OfflineDatabaseSql.ReadInt(rows[i][1]);
            List<OfflineArmySnapshotStackSkillRecord> list;
            if (!result.TryGetValue(snapshotStackId, out list))
            {
                list = new List<OfflineArmySnapshotStackSkillRecord>();
                result.Add(snapshotStackId, list);
            }

            list.Add(
                new OfflineArmySnapshotStackSkillRecord(
                    OfflineDatabaseSql.ReadInt(rows[i][0]),
                    OfflineDatabaseSql.ReadText(rows[i][2]),
                    OfflineDatabaseSql.ReadInt(rows[i][3]),
                    OfflineDatabaseSql.ReadBool(rows[i][4], true)));
        }

        return result;
    }
}
