using System;

[Serializable]
public class HexCoord
{
    public int C;
    public int R;

    public HexCoord()
    {
    }

    public HexCoord(int c, int r)
    {
        C = c;
        R = r;
    }

    public bool SamePosition(HexCoord other)
    {
        return other != null && C == other.C && R == other.R;
    }
}

[Serializable]
public class SkillContext
{
    public BattleSnapshot Snapshot;
    public string ActorUnitId = string.Empty;
    public SkillDefinitionAsset SkillDefinition;
    public SkillDefinitionSpec SkillSpec;
    public int SkillSlot = -1;
    public int ActionSeed;

    public string SkillId
    {
        get
        {
            if (SkillSpec != null)
            {
                return SkillSpec.SkillName ?? string.Empty;
            }

            return SkillDefinition == null ? string.Empty : SkillDefinition.SkillName;
        }
    }

    public static SkillContext Create(
        BattleSnapshot snapshot,
        string actorUnitId,
        SkillDefinitionAsset skillDefinition,
        int skillSlot = -1,
        int actionSeed = 0)
    {
        return new SkillContext
        {
            Snapshot = snapshot,
            ActorUnitId = actorUnitId ?? string.Empty,
            SkillDefinition = skillDefinition,
            SkillSpec = SkillDefinitionSpec.FromAsset(skillDefinition),
            SkillSlot = skillSlot,
            ActionSeed = actionSeed
        };
    }

    public static SkillContext Create(
        BattleSnapshot snapshot,
        string actorUnitId,
        SkillDefinitionSpec skillSpec,
        int skillSlot = -1,
        int actionSeed = 0)
    {
        return new SkillContext
        {
            Snapshot = snapshot,
            ActorUnitId = actorUnitId ?? string.Empty,
            SkillDefinition = null,
            SkillSpec = skillSpec,
            SkillSlot = skillSlot,
            ActionSeed = actionSeed
        };
    }
}
