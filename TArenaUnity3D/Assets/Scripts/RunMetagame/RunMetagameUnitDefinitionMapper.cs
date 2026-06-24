using System.Collections.Generic;

public static class RunMetagameUnitDefinitionMapper
{
    public static StartRunUnitDefinition ToStartRunUnitDefinition(DataMapper.UnitDefinition unit)
    {
        if (unit == null)
        {
            return null;
        }

        return new StartRunUnitDefinition(
            unit.Name,
            unit.Name,
            unit.Tier,
            unit.Cost,
            unit.FactionId,
            unit.UnitRoleCategory,
            CopySkillIds(unit.SkillNames));
    }

    public static RunShopUnitDefinition ToRunShopUnitDefinition(DataMapper.UnitDefinition unit)
    {
        if (unit == null)
        {
            return null;
        }

        return new RunShopUnitDefinition(
            unit.Name,
            unit.Name,
            unit.Tier,
            unit.Cost,
            unit.FactionId,
            CopySkillIds(unit.SkillNames));
    }

    private static List<string> CopySkillIds(List<string> skillIds)
    {
        return skillIds == null ? new List<string>() : new List<string>(skillIds);
    }
}
