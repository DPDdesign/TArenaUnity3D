using System;
using System.Collections.Generic;

[Serializable]
public class BattleTurnStateSnapshot
{
    public int RoundNumber;
    public bool IsResolvingNewTurnSequence;
    public bool IsActionBlocking;
    public string ActiveActionKind = string.Empty;
}

[Serializable]
public class BattleStatusSnapshot
{
    public string StatusId = string.Empty;
    public string SourceSkillId = string.Empty;
    public string SourceUnitId = string.Empty;
    public int RemainingDurationOrTurns;
    public int HpModifier;
    public int AttackModifier;
    public int DefenseModifier;
    public int MovementModifier;
    public int InitiativeModifier;
    public int MaxDamageModifier;
    public int MinDamageModifier;
    public int DamageOverTime;
    public int ResistanceModifier;
    public int CounterAttacksModifier;
    public int DamageModifier;
    public bool IsStackable;
}

[Serializable]
public class BattleHexSnapshot
{
    public int C;
    public int R;
    public bool IsWalkable;
    public string OccupyingUnitId = string.Empty;
    public string TrapName = string.Empty;
    public int TrapRemainingDurationOrTurns;
    public string TrapSourceUnitId = string.Empty;
}

[Serializable]
public class BattleUnitSnapshot
{
    public string RuntimeUnitId = string.Empty;
    public string CatalogUnitId = string.Empty;
    public int TeamIndex;
    public int RosterIndexWithinTeam;
    public string UnitName = string.Empty;
    public string UnitType = string.Empty;
    public int C;
    public int R;
    public int Amount;
    public int TempHP;
    public int BaseHP;
    public int Attack;
    public int Defense;
    public int MovementSpeed;
    public int Initiative;
    public int MinDamage;
    public int MaxDamage;
    public int AttackModifier;
    public int DefenseModifier;
    public int MinDamageModifier;
    public int MaxDamageModifier;
    public int OutgoingDamageReductionPercent;
    public int IncomingDamageReductionPercent;
    public int FlatDamageReduction;
    public int PureDamage;
    public double DefensePenetration;
    public string HatedTargetUnitId = string.Empty;
    public bool IsAlive;
    public bool IsRange;
    public bool Waited;
    public bool Moved;
    public bool MovedThisTurn;
    public bool UsedSkillThisTurn;
    public bool CounterAttackAvailable;
    public int CounterAttacks;
    public int TempCounterAttacks;
    public List<string> UsedSkillIdsThisTurn = new List<string>();
    public bool CanMoveAfterSkillThisTurn;
    public List<int> CooldownsBySlot = new List<int>();
    public List<string> SkillIdsBySlot = new List<string>();
    public List<BattleStatusSnapshot> Statuses = new List<BattleStatusSnapshot>();
}

[Serializable]
public class BattleSnapshot
{
    public int MapWidth;
    public int MapHeight;
    public bool UsesLegacyHexLayout;
    public int GameSeed;
    public string BattleId = string.Empty;
    public int NextActionIndex;
    public List<BattleHexSnapshot> Hexes = new List<BattleHexSnapshot>();
    public List<BattleUnitSnapshot> Units = new List<BattleUnitSnapshot>();
    public string ActiveUnitId = string.Empty;
    public BattleTurnStateSnapshot TurnState = new BattleTurnStateSnapshot();
    public string SnapshotHash = string.Empty;
}

public static class BattleSnapshotRuntimeIds
{
    public static string CreateUnitId(int teamIndex, int rosterIndexWithinTeam)
    {
        return "team-" + teamIndex + "-slot-" + rosterIndexWithinTeam;
    }
}
