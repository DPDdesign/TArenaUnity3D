using System;

[Serializable]
public class TacticalAIHexCoordinate
{
    public int C;
    public int R;

    public TacticalAIHexCoordinate()
    {
    }

    public TacticalAIHexCoordinate(int c, int r)
    {
        C = c;
        R = r;
    }
}

public sealed class TacticalAIRevalidatedIntent
{
    public TacticalAIActionType ActionType;
    public BattleActionUse Use;
    public BattleAction Action;
    public BattleActionResult Result;
    public BattleUnitSnapshot Actor;
    public BattleUnitSnapshot Target;
    public TacticalAIHexCoordinate DestinationHex;
    public TacticalAIHexCoordinate TargetHex;
    public int SkillSlot = -1;
    public string SkillId = string.Empty;
}
