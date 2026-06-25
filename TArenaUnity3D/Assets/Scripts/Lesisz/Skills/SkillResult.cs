using System;
using System.Collections.Generic;

public enum SkillResultEventType
{
    None,
    UnitMoved,
    DamageApplied,
    StatusApplied,
    TrapPlaced,
    TrapTriggered,
    UnitSpawned,
    StackAmountChanged,
    HpCostApplied,
    CooldownApplied,
    TurnCostApplied,
    StanceChanged
}

[Serializable]
public class SkillResultEvent
{
    public SkillResultEventType EventType = SkillResultEventType.None;
    public string SkillId = string.Empty;
    public string ActorUnitId = string.Empty;
    public string TargetUnitId = string.Empty;
    public HexCoord Hex;
    public string StatusId = string.Empty;
    public string TrapId = string.Empty;
    public int Amount;

    public SkillResultEvent()
    {
    }

    public SkillResultEvent(SkillResultEventType eventType, SkillCast cast)
    {
        EventType = eventType;
        SkillId = cast == null ? string.Empty : cast.SkillId;
        ActorUnitId = cast == null ? string.Empty : cast.ActorUnitId;
    }
}

[Serializable]
public class SkillResult
{
    public string ActorUnitId = string.Empty;
    public string SkillId = string.Empty;
    public int ActionIndex;
    public List<SkillResultEvent> Events = new List<SkillResultEvent>();

    public void Add(SkillResultEvent resultEvent)
    {
        if (resultEvent != null)
        {
            Events.Add(resultEvent);
        }
    }
}

public interface ISkillRuntime
{
    SkillResult Apply(SkillCast cast, SkillContext context);
}
