using System.Collections.Generic;
using System.Globalization;

public static class OfflineArmySnapshotMapper
{
    public static OfflineArmySnapshotRecord FromStartRun(RunArmySnapshot snapshot, int accountId = 0, int runId = 0, int nodeId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<RunArmyStackSnapshot> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                RunArmyStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(new OfflineArmySnapshotStackRecord(0, stack.UnitId, stack.Amount, i, true, ToSharedSkills(stack.Skills)));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, runId, 0, nodeId, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshot == null ? string.Empty : snapshot.SnapshotId);
        return shared;
    }

    public static OfflineArmySnapshotRecord FromRunBattle(RunBattleArmySnapshot snapshot, int accountId = 0, int runId = 0, int nodeId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<RunBattleStackSnapshot> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                RunBattleStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(
                    new OfflineArmySnapshotStackRecord(
                        0,
                        stack.UnitId,
                        stack.Amount,
                        ResolveFormationSlot(stack.StackId, i),
                        true,
                        ToSharedSkills(stack.Skills)));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, runId, 0, nodeId, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshot == null ? string.Empty : snapshot.SnapshotId);
        return shared;
    }

    public static OfflineArmySnapshotRecord FromRewardMap(RewardMapArmySnapshot snapshot, int accountId = 0, int runId = 0, int nodeId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<RewardMapStackSnapshot> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                RewardMapStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(
                    new OfflineArmySnapshotStackRecord(
                        0,
                        stack.UnitId,
                        stack.Amount,
                        ResolveFormationSlot(stack.StackId, i),
                        true,
                        ToSharedSkills(stack.Skills)));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, runId, 0, nodeId, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshot == null ? string.Empty : snapshot.SnapshotId);
        return shared;
    }

    public static OfflineArmySnapshotRecord FromRunShop(RunShopArmySnapshot snapshot, int accountId = 0, int runId = 0, int nodeId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<RunShopStackSnapshot> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                RunShopStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(
                    new OfflineArmySnapshotStackRecord(
                        0,
                        stack.UnitId,
                        stack.Amount,
                        ResolveFormationSlot(stack.StackId, i),
                        true,
                        ToSharedSkills(stack.Skills)));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, runId, 0, nodeId, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshot == null ? string.Empty : snapshot.SnapshotId);
        return shared;
    }

    public static OfflineArmySnapshotRecord FromSummaryValue(SummaryValueArmySnapshot snapshot, int accountId = 0, int runId = 0, int nodeId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<SummaryValueStackSnapshot> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SummaryValueStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(
                    new OfflineArmySnapshotStackRecord(
                        0,
                        stack.UnitId,
                        stack.Amount,
                        ResolveFormationSlot(stack.StackId, i),
                        true,
                        ToSharedSkills(stack.Skills)));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, runId, 0, nodeId, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshot == null ? string.Empty : snapshot.SnapshotId);
        return shared;
    }

    public static OfflineArmySnapshotRecord FromSavedArmy(SavedArmy army, int accountId = 0, int savedArmyId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<SavedArmyStackSnapshot> source = army == null ? null : army.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SavedArmyStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(new OfflineArmySnapshotStackRecord(0, stack.UnitId, stack.Amount, i, true, new List<OfflineArmySnapshotStackSkillRecord>()));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, 0, savedArmyId, 0, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(army == null ? string.Empty : army.SnapshotId);
        shared.SavedArmyId = savedArmyId > 0 ? savedArmyId : OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(army == null ? string.Empty : army.SavedArmyId);
        return shared;
    }

    public static OfflineArmySnapshotRecord FromBattleResult(BattleResultSavedArmySnapshot army, int accountId = 0, int savedArmyId = 0)
    {
        List<OfflineArmySnapshotStackRecord> stacks = new List<OfflineArmySnapshotStackRecord>();
        List<BattleResultStackSnapshot> source = army == null ? null : army.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                BattleResultStackSnapshot stack = source[i];
                if (stack == null)
                {
                    continue;
                }

                stacks.Add(
                    new OfflineArmySnapshotStackRecord(
                        0,
                        stack.UnitId,
                        stack.Amount,
                        ResolveFormationSlot(stack.StackId, i),
                        true,
                        ToSharedSkills(stack.Skills)));
            }
        }

        OfflineArmySnapshotRecord shared = OfflineArmySnapshotFactory.Create(accountId, 0, savedArmyId, 0, stacks);
        shared.SnapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(army == null ? string.Empty : army.SnapshotId);
        shared.SavedArmyId = savedArmyId > 0 ? savedArmyId : OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(army == null ? string.Empty : army.SavedArmyId);
        return shared;
    }

    public static RunArmySnapshot ToStartRun(OfflineArmySnapshotRecord snapshot, IOfflineArmySnapshotCatalogResolver resolver)
    {
        List<RunArmyStackSnapshot> stacks = new List<RunArmyStackSnapshot>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
                stacks.Add(
                    new RunArmyStackSnapshot(
                        stack.UnitId,
                        unit.Tier,
                        1,
                        stack.Amount,
                        unit.CombatValue,
                        BuildStartRunSkills(unit.SkillIds, stack.Skills)));
            }
        }

        return new RunArmySnapshot(ToSnapshotIdText(snapshot), CalculateArmyValue(snapshot, resolver), stacks);
    }

    public static RunBattleArmySnapshot ToRunBattle(OfflineArmySnapshotRecord snapshot, IOfflineArmySnapshotCatalogResolver resolver)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        List<string> usedStackIds = new List<string>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
                stacks.Add(
                    new RunBattleStackSnapshot(
                        BuildRuntimeStackId(stack.UnitId, stack.FormationSlot, usedStackIds),
                        stack.UnitId,
                        unit.DisplayName,
                        unit.Tier,
                        1,
                        stack.Amount,
                        0,
                        unit.CombatValue,
                        BuildRunBattleSkills(unit.SkillIds, stack.Skills)));
            }
        }

        return new RunBattleArmySnapshot(ToSnapshotIdText(snapshot), CalculateArmyValue(snapshot, resolver), stacks);
    }

    public static RewardMapArmySnapshot ToRewardMap(OfflineArmySnapshotRecord snapshot, IOfflineArmySnapshotCatalogResolver resolver)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        List<string> usedStackIds = new List<string>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
                stacks.Add(
                    new RewardMapStackSnapshot(
                        BuildRuntimeStackId(stack.UnitId, stack.FormationSlot, usedStackIds),
                        stack.UnitId,
                        unit.DisplayName,
                        unit.Tier,
                        1,
                        stack.Amount,
                        0,
                        unit.CombatValue,
                        BuildRewardMapSkills(unit.SkillIds, stack.Skills)));
            }
        }

        return new RewardMapArmySnapshot(ToSnapshotIdText(snapshot), CalculateArmyValue(snapshot, resolver), stacks);
    }

    public static RunShopArmySnapshot ToRunShop(OfflineArmySnapshotRecord snapshot, IOfflineArmySnapshotCatalogResolver resolver)
    {
        List<RunShopStackSnapshot> stacks = new List<RunShopStackSnapshot>();
        List<string> usedStackIds = new List<string>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
                stacks.Add(
                    new RunShopStackSnapshot(
                        BuildRuntimeStackId(stack.UnitId, stack.FormationSlot, usedStackIds),
                        stack.UnitId,
                        unit.DisplayName,
                        unit.Tier,
                        1,
                        stack.Amount,
                        0,
                        unit.CombatValue,
                        BuildRunShopSkills(unit.SkillIds, stack.Skills)));
            }
        }

        return new RunShopArmySnapshot(ToSnapshotIdText(snapshot), CalculateArmyValue(snapshot, resolver), stacks);
    }

    public static SummaryValueArmySnapshot ToSummaryValue(OfflineArmySnapshotRecord snapshot, IOfflineArmySnapshotCatalogResolver resolver)
    {
        List<SummaryValueStackSnapshot> stacks = new List<SummaryValueStackSnapshot>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
                stacks.Add(
                    new SummaryValueStackSnapshot(
                        ToStackIdText(stack.FormationSlot),
                        stack.UnitId,
                        unit.DisplayName,
                        unit.Tier,
                        1,
                        stack.Amount,
                        unit.CombatValue,
                        BuildSummaryValueSkills(unit.SkillIds, stack.Skills)));
            }
        }

        return new SummaryValueArmySnapshot(ToSnapshotIdText(snapshot), CalculateArmyValue(snapshot, resolver), stacks);
    }

    public static SavedArmy ToSavedArmy(OfflineArmySnapshotRecord snapshot, string savedArmyIdText)
    {
        List<SavedArmyStackSnapshot> stacks = new List<SavedArmyStackSnapshot>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                stacks.Add(new SavedArmyStackSnapshot(stack.UnitId, stack.Amount));
            }
        }

        string resolvedSavedArmyId = string.IsNullOrEmpty(savedArmyIdText)
            ? OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(snapshot == null ? 0 : snapshot.SavedArmyId)
            : savedArmyIdText;
        return new SavedArmy(resolvedSavedArmyId, ToSnapshotIdText(snapshot), true, stacks);
    }

    public static BattleResultSavedArmySnapshot ToBattleResult(
        OfflineArmySnapshotRecord snapshot,
        string savedArmyIdText,
        string displayName,
        IOfflineArmySnapshotCatalogResolver resolver)
    {
        List<BattleResultStackSnapshot> stacks = new List<BattleResultStackSnapshot>();
        List<OfflineArmySnapshotStackRecord> source = snapshot == null ? null : snapshot.Stacks;

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = source[i];
                OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
                stacks.Add(
                    new BattleResultStackSnapshot(
                        ToStackIdText(stack.FormationSlot),
                        stack.UnitId,
                        unit.DisplayName,
                        stack.Amount,
                        unit.CombatValue,
                        BuildBattleResultSkills(unit.SkillIds, stack.Skills)));
            }
        }

        return new BattleResultSavedArmySnapshot(
            savedArmyIdText,
            ToSnapshotIdText(snapshot),
            displayName,
            CalculateArmyValue(snapshot, resolver),
            stacks);
    }

    private static List<OfflineArmySnapshotStackSkillRecord> ToSharedSkills(List<StartRunSkillViewData> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> shared = new List<OfflineArmySnapshotStackSkillRecord>();
        if (skills == null)
        {
            return shared;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            StartRunSkillViewData skill = skills[i];
            if (skill != null && skill.Unlocked)
            {
                shared.Add(new OfflineArmySnapshotStackSkillRecord(0, skill.SkillId, 0, true));
            }
        }

        return shared;
    }

    private static List<OfflineArmySnapshotStackSkillRecord> ToSharedSkills(List<RunBattleSkillState> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> shared = new List<OfflineArmySnapshotStackSkillRecord>();
        if (skills == null)
        {
            return shared;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            RunBattleSkillState skill = skills[i];
            if (skill != null && skill.Unlocked)
            {
                shared.Add(new OfflineArmySnapshotStackSkillRecord(0, skill.SkillId, 0, true));
            }
        }

        return shared;
    }

    private static List<OfflineArmySnapshotStackSkillRecord> ToSharedSkills(List<RewardMapSkillState> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> shared = new List<OfflineArmySnapshotStackSkillRecord>();
        if (skills == null)
        {
            return shared;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            RewardMapSkillState skill = skills[i];
            if (skill != null && skill.Unlocked)
            {
                shared.Add(new OfflineArmySnapshotStackSkillRecord(0, skill.SkillId, 0, true));
            }
        }

        return shared;
    }

    private static List<OfflineArmySnapshotStackSkillRecord> ToSharedSkills(List<RunShopSkillState> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> shared = new List<OfflineArmySnapshotStackSkillRecord>();
        if (skills == null)
        {
            return shared;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            RunShopSkillState skill = skills[i];
            if (skill != null && skill.Unlocked)
            {
                shared.Add(new OfflineArmySnapshotStackSkillRecord(0, skill.SkillId, 0, true));
            }
        }

        return shared;
    }

    private static List<OfflineArmySnapshotStackSkillRecord> ToSharedSkills(List<SummaryValueSkillState> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> shared = new List<OfflineArmySnapshotStackSkillRecord>();
        if (skills == null)
        {
            return shared;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            SummaryValueSkillState skill = skills[i];
            if (skill != null && skill.Unlocked)
            {
                shared.Add(new OfflineArmySnapshotStackSkillRecord(0, skill.SkillId, 0, true));
            }
        }

        return shared;
    }

    private static List<OfflineArmySnapshotStackSkillRecord> ToSharedSkills(List<BattleResultSkillState> skills)
    {
        List<OfflineArmySnapshotStackSkillRecord> shared = new List<OfflineArmySnapshotStackSkillRecord>();
        if (skills == null)
        {
            return shared;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            BattleResultSkillState skill = skills[i];
            if (skill != null && skill.Unlocked)
            {
                shared.Add(new OfflineArmySnapshotStackSkillRecord(0, skill.SkillId, 0, true));
            }
        }

        return shared;
    }

    private static List<StartRunSkillViewData> BuildStartRunSkills(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<StartRunSkillViewData> result = new List<StartRunSkillViewData>();
        List<string> skillIds = BuildSkillIds(catalogSkillIds, assignedSkills);

        for (int i = 0; i < skillIds.Count; i++)
        {
            result.Add(new StartRunSkillViewData(skillIds[i], HasAssignedSkill(assignedSkills, skillIds[i])));
        }

        return result;
    }

    private static List<RunBattleSkillState> BuildRunBattleSkills(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<RunBattleSkillState> result = new List<RunBattleSkillState>();
        List<string> skillIds = BuildSkillIds(catalogSkillIds, assignedSkills);

        for (int i = 0; i < skillIds.Count; i++)
        {
            result.Add(new RunBattleSkillState(skillIds[i], HasAssignedSkill(assignedSkills, skillIds[i])));
        }

        return result;
    }

    private static List<RewardMapSkillState> BuildRewardMapSkills(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<RewardMapSkillState> result = new List<RewardMapSkillState>();
        List<string> skillIds = BuildSkillIds(catalogSkillIds, assignedSkills);

        for (int i = 0; i < skillIds.Count; i++)
        {
            result.Add(new RewardMapSkillState(skillIds[i], HasAssignedSkill(assignedSkills, skillIds[i])));
        }

        return result;
    }

    private static List<RunShopSkillState> BuildRunShopSkills(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<RunShopSkillState> result = new List<RunShopSkillState>();
        List<string> skillIds = BuildSkillIds(catalogSkillIds, assignedSkills);

        for (int i = 0; i < skillIds.Count; i++)
        {
            result.Add(new RunShopSkillState(skillIds[i], HasAssignedSkill(assignedSkills, skillIds[i])));
        }

        return result;
    }

    private static List<SummaryValueSkillState> BuildSummaryValueSkills(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<SummaryValueSkillState> result = new List<SummaryValueSkillState>();
        List<string> skillIds = BuildSkillIds(catalogSkillIds, assignedSkills);

        for (int i = 0; i < skillIds.Count; i++)
        {
            result.Add(new SummaryValueSkillState(skillIds[i], HasAssignedSkill(assignedSkills, skillIds[i])));
        }

        return result;
    }

    private static List<BattleResultSkillState> BuildBattleResultSkills(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<BattleResultSkillState> result = new List<BattleResultSkillState>();
        List<string> skillIds = BuildSkillIds(catalogSkillIds, assignedSkills);

        for (int i = 0; i < skillIds.Count; i++)
        {
            result.Add(new BattleResultSkillState(skillIds[i], HasAssignedSkill(assignedSkills, skillIds[i])));
        }

        return result;
    }

    private static List<string> BuildSkillIds(List<string> catalogSkillIds, List<OfflineArmySnapshotStackSkillRecord> assignedSkills)
    {
        List<string> skillIds = new List<string>();

        if (catalogSkillIds != null)
        {
            for (int i = 0; i < catalogSkillIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(catalogSkillIds[i]) && !ContainsText(skillIds, catalogSkillIds[i]))
                {
                    skillIds.Add(catalogSkillIds[i]);
                }
            }
        }

        if (assignedSkills != null)
        {
            for (int i = 0; i < assignedSkills.Count; i++)
            {
                if (assignedSkills[i] != null && !string.IsNullOrEmpty(assignedSkills[i].SkillId) && !ContainsText(skillIds, assignedSkills[i].SkillId))
                {
                    skillIds.Add(assignedSkills[i].SkillId);
                }
            }
        }

        return skillIds;
    }

    private static bool HasAssignedSkill(List<OfflineArmySnapshotStackSkillRecord> assignedSkills, string skillId)
    {
        if (assignedSkills == null || string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        for (int i = 0; i < assignedSkills.Count; i++)
        {
            if (assignedSkills[i] != null && assignedSkills[i].SkillId == skillId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsText(List<string> values, string value)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    private static int ResolveFormationSlot(string stackId, int fallbackIndex)
    {
        if (string.IsNullOrEmpty(stackId))
        {
            return fallbackIndex;
        }

        const string prefix = "slot-";
        if (stackId.StartsWith(prefix))
        {
            int parsedPrefixed;
            if (int.TryParse(stackId.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedPrefixed))
            {
                return parsedPrefixed;
            }
        }

        int parsed;
        if (int.TryParse(stackId, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return fallbackIndex;
    }

    private static string ToStackIdText(int formationSlot)
    {
        return OfflineDatabaseLegacyIdentity.ToLegacyFormationSlotId(formationSlot);
    }

    private static string BuildRuntimeStackId(string unitId, int formationSlot, List<string> usedStackIds)
    {
        string normalizedUnitId = NormalizeStackToken(unitId);
        string preferred = string.IsNullOrEmpty(normalizedUnitId)
            ? string.Empty
            : "stack-" + normalizedUnitId;
        if (!string.IsNullOrEmpty(preferred) && !ContainsText(usedStackIds, preferred))
        {
            usedStackIds.Add(preferred);
            return preferred;
        }

        string fallback = ToStackIdText(formationSlot);
        if (!ContainsText(usedStackIds, fallback))
        {
            usedStackIds.Add(fallback);
            return fallback;
        }

        string indexed = fallback + "-" + formationSlot.ToString(CultureInfo.InvariantCulture);
        usedStackIds.Add(indexed);
        return indexed;
    }

    private static string NormalizeStackToken(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        char[] buffer = new char[value.Length];
        int length = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (char.IsLetterOrDigit(character))
            {
                buffer[length++] = char.ToLowerInvariant(character);
            }
        }

        return length == 0 ? string.Empty : new string(buffer, 0, length);
    }

    private static string ToSnapshotIdText(OfflineArmySnapshotRecord snapshot)
    {
        return OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(snapshot == null ? 0 : snapshot.SnapshotId);
    }

    private static OfflineArmySnapshotUnitCatalogEntry ResolveUnit(OfflineArmySnapshotStackRecord stack, IOfflineArmySnapshotCatalogResolver resolver)
    {
        OfflineArmySnapshotUnitCatalogEntry unit = stack == null || resolver == null ? null : resolver.FindUnit(stack.UnitId);
        if (unit != null)
        {
            return unit;
        }

        return new OfflineArmySnapshotUnitCatalogEntry(
            stack == null ? string.Empty : stack.UnitId,
            stack == null || string.IsNullOrEmpty(stack.UnitId) ? "Unknown Unit" : stack.UnitId,
            "I",
            0,
            new List<string>());
    }

    private static int CalculateArmyValue(OfflineArmySnapshotRecord snapshot, IOfflineArmySnapshotCatalogResolver resolver)
    {
        if (snapshot == null || snapshot.Stacks == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < snapshot.Stacks.Count; i++)
        {
            OfflineArmySnapshotStackRecord stack = snapshot.Stacks[i];
            if (stack == null)
            {
                continue;
            }

            OfflineArmySnapshotUnitCatalogEntry unit = ResolveUnit(stack, resolver);
            total += stack.Amount * unit.CombatValue;
        }

        return total;
    }
}
