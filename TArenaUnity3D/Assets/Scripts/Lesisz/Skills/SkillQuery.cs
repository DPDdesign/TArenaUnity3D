using System.Collections.Generic;

public static class SkillQuery
{
    public static List<string> GetUsableSkillIds(SkillContext context)
    {
        List<string> usable = new List<string>();
        if (context == null || context.Snapshot == null || string.IsNullOrEmpty(context.ActorUnitId))
        {
            return usable;
        }

        BattleUnitSnapshot actor = FindUnit(context.Snapshot, context.ActorUnitId);
        if (actor == null || actor.SkillIdsBySlot == null)
        {
            return usable;
        }

        SkillCatalog catalog = DataMapper.Instance != null ? DataMapper.Instance.SkillCatalog : null;
        for (int i = 0; i < actor.SkillIdsBySlot.Count; i++)
        {
            string skillId = actor.SkillIdsBySlot[i];
            SkillDefinitionAsset definition = catalog == null ? null : catalog.FindSkillAsset(skillId);
            if (definition == null)
            {
                continue;
            }

            SkillContext skillContext = SkillContext.Create(context.Snapshot, actor.RuntimeUnitId, definition, i, context.ActionSeed);
            if (SkillRules.CanUse(skillContext).IsValid)
            {
                usable.Add(skillId);
            }
        }

        return usable;
    }

    public static List<SkillTarget> GetLegalTargets(SkillContext context, List<HexCoord> selectedTargets)
    {
        return SkillRules.GetTargets(context, selectedTargets);
    }

    public static SkillValidationResult Validate(SkillUse use, SkillContext context)
    {
        return SkillRules.Validate(use, context);
    }

    public static SkillResult Preview(SkillCast cast, SkillContext context)
    {
        return SkillRules.Preview(cast, context);
    }

    static BattleUnitSnapshot FindUnit(BattleSnapshot snapshot, string actorUnitId)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return null;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && unit.RuntimeUnitId == actorUnitId)
            {
                return unit;
            }
        }

        return null;
    }
}
