using System;
using System.Collections.Generic;

public static class RunMetagameDisplayInfoFactory
{
    public static StackInfoData FromStartRun(StartRunStackViewData stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            "",
            stack.UnitId,
            stack.DisplayName,
            stack.Tier,
            stack.Level,
            stack.Amount,
            0,
            stack.CombatValue,
            ToSkillInfo(stack.Skills),
            dataMapper);
    }

    public static StackInfoData FromRewardMap(RewardMapStackSnapshot stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            stack.StackId,
            stack.UnitId,
            stack.DisplayName,
            stack.Tier,
            stack.Level,
            stack.Amount,
            stack.Lost,
            stack.CombatValue,
            ToSkillInfo(stack.Skills),
            dataMapper);
    }

    public static StackInfoData FromRunShop(RunShopStackSnapshot stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            stack.StackId,
            stack.UnitId,
            stack.DisplayName,
            stack.Tier,
            stack.Level,
            stack.Amount,
            stack.Lost,
            stack.CombatValue,
            ToSkillInfo(stack.Skills),
            dataMapper);
    }

    public static StackInfoData FromSummaryValue(SummaryValueStackSnapshot stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            stack.StackId,
            stack.UnitId,
            stack.DisplayName,
            stack.Tier,
            stack.Level,
            stack.Amount,
            0,
            stack.CombatValue,
            ToSkillInfo(stack.Skills),
            dataMapper);
    }

    public static StackInfoData FromSavedArmies(SavedArmyStackViewData stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            "",
            stack.UnitId,
            stack.DisplayName,
            stack.Tier,
            0,
            stack.Amount,
            0,
            stack.StackValue,
            null,
            dataMapper);
    }

    public static StackInfoData FromBattleResult(BattleResultStackSnapshot stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            stack.StackId,
            stack.UnitId,
            stack.DisplayName,
            "",
            0,
            stack.Amount,
            0,
            stack.CombatValue,
            ToSkillInfo(stack.Skills),
            dataMapper);
    }

    public static StackInfoData FromOfflineSnapshotStack(OfflineArmySnapshotStackRecord stack, DataMapper dataMapper)
    {
        if (stack == null)
        {
            return null;
        }

        return BuildStackInfo(
            stack.SnapshotStackId.ToString(),
            stack.UnitId,
            "",
            "",
            0,
            stack.Amount,
            0,
            0,
            ToSkillInfo(stack.Skills),
            dataMapper);
    }

    public static UnitInfoData BuildUnitInfo(string unitId, DataMapper dataMapper)
    {
        return BuildUnitInfo(unitId, "", "", null, dataMapper);
    }

    static StackInfoData BuildStackInfo(
        string stackId,
        string unitId,
        string displayName,
        string tier,
        int level,
        int amount,
        int lost,
        int stackValue,
        List<SkillInfoData> skills,
        DataMapper dataMapper)
    {
        UnitInfoData unit = BuildUnitInfo(unitId, displayName, tier, skills, dataMapper);
        return new StackInfoData(stackId, amount, level, lost, stackValue, unit);
    }

    static UnitInfoData BuildUnitInfo(
        string unitId,
        string displayName,
        string tier,
        List<SkillInfoData> skills,
        DataMapper dataMapper)
    {
        DataMapper mapper = dataMapper == null ? DataMapper.Instance : dataMapper;
        DataMapper.UnitDefinition definition = mapper == null ? null : mapper.FindUnit(unitId);

        string resolvedName = !string.IsNullOrEmpty(displayName)
            ? displayName
            : definition == null ? unitId : definition.Name;
        string resolvedTier = !string.IsNullOrEmpty(tier)
            ? tier
            : definition == null ? "?" : definition.Tier;
        int cost = definition == null ? 0 : definition.Cost;
        string spriteReference = definition == null ? unitId : definition.SpritePath;
        UnitStatsData stats = BuildStats(definition);
        List<SkillInfoData> resolvedSkills = skills == null ? BuildCatalogSkillInfo(definition) : skills;

        return new UnitInfoData(unitId, resolvedName, resolvedTier, cost, spriteReference, stats, resolvedSkills);
    }

    static UnitStatsData BuildStats(DataMapper.UnitDefinition definition)
    {
        if (definition == null)
        {
            return new UnitStatsData(0, 0, 0, 0, 0, 0, 0, 0);
        }

        return new UnitStatsData(
            definition.Attack,
            definition.Defense,
            definition.DamageMinimum,
            definition.DamageMaximum,
            Math.Max(0, definition.Speed - 1),
            definition.Initiative,
            definition.HP,
            definition.HP);
    }

    static List<SkillInfoData> BuildCatalogSkillInfo(DataMapper.UnitDefinition definition)
    {
        List<SkillInfoData> result = new List<SkillInfoData>();
        if (definition == null || definition.SkillNames == null)
        {
            return result;
        }

        for (int i = 0; i < definition.SkillNames.Count; i++)
        {
            if (!string.IsNullOrEmpty(definition.SkillNames[i]))
            {
                result.Add(new SkillInfoData(definition.SkillNames[i], true));
            }
        }

        return result;
    }

    static List<SkillInfoData> ToSkillInfo(List<StartRunSkillViewData> skills)
    {
        List<SkillInfoData> result = new List<SkillInfoData>();
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SkillInfoData(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    static List<SkillInfoData> ToSkillInfo(List<RewardMapSkillState> skills)
    {
        List<SkillInfoData> result = new List<SkillInfoData>();
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SkillInfoData(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    static List<SkillInfoData> ToSkillInfo(List<RunShopSkillState> skills)
    {
        List<SkillInfoData> result = new List<SkillInfoData>();
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SkillInfoData(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    static List<SkillInfoData> ToSkillInfo(List<SummaryValueSkillState> skills)
    {
        List<SkillInfoData> result = new List<SkillInfoData>();
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SkillInfoData(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    static List<SkillInfoData> ToSkillInfo(List<BattleResultSkillState> skills)
    {
        List<SkillInfoData> result = new List<SkillInfoData>();
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SkillInfoData(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    static List<SkillInfoData> ToSkillInfo(List<OfflineArmySnapshotStackSkillRecord> skills)
    {
        if (skills == null)
        {
            return null;
        }

        List<SkillInfoData> result = new List<SkillInfoData>();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SkillInfoData(skills[i].SkillId, skills[i].IsActive));
            }
        }

        return result;
    }
}
