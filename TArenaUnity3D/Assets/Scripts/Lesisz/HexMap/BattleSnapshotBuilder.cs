using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public static class BattleSnapshotBuilder
{
    public static BattleSnapshot Build(
        int mapWidth,
        int mapHeight,
        IEnumerable<BattleHexSnapshot> hexes,
        IEnumerable<BattleUnitSnapshot> units,
        string activeUnitId,
        BattleTurnStateSnapshot turnState,
        int gameSeed = 0,
        string battleId = "",
        int nextActionIndex = 0,
        bool usesLegacyHexLayout = false)
    {
        BattleSnapshot snapshot = new BattleSnapshot();
        snapshot.MapWidth = Math.Max(0, mapWidth);
        snapshot.MapHeight = Math.Max(0, mapHeight);
        snapshot.UsesLegacyHexLayout = usesLegacyHexLayout;
        snapshot.GameSeed = gameSeed;
        snapshot.BattleId = NormalizeString(battleId);
        snapshot.NextActionIndex = Math.Max(0, nextActionIndex);
        snapshot.Hexes = CloneAndSortHexes(hexes);
        snapshot.Units = CloneAndSortUnits(units);
        snapshot.ActiveUnitId = NormalizeString(activeUnitId);
        snapshot.TurnState = CloneTurnState(turnState);
        snapshot.SnapshotHash = ComputeHash(snapshot);
        return snapshot;
    }

    static List<BattleHexSnapshot> CloneAndSortHexes(IEnumerable<BattleHexSnapshot> hexes)
    {
        List<BattleHexSnapshot> result = new List<BattleHexSnapshot>();
        if (hexes != null)
        {
            foreach (BattleHexSnapshot hex in hexes)
            {
                if (hex == null)
                {
                    continue;
                }

                result.Add(CloneHex(hex));
            }
        }

        result.Sort(CompareHexes);
        return result;
    }

    static List<BattleUnitSnapshot> CloneAndSortUnits(IEnumerable<BattleUnitSnapshot> units)
    {
        List<BattleUnitSnapshot> result = new List<BattleUnitSnapshot>();
        if (units != null)
        {
            foreach (BattleUnitSnapshot unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                result.Add(CloneUnit(unit));
            }
        }

        result.Sort(CompareUnits);
        return result;
    }

    static BattleTurnStateSnapshot CloneTurnState(BattleTurnStateSnapshot turnState)
    {
        BattleTurnStateSnapshot clone = new BattleTurnStateSnapshot();
        if (turnState == null)
        {
            return clone;
        }

        clone.RoundNumber = turnState.RoundNumber;
        clone.IsResolvingNewTurnSequence = turnState.IsResolvingNewTurnSequence;
        clone.IsActionBlocking = turnState.IsActionBlocking;
        clone.ActiveActionKind = NormalizeString(turnState.ActiveActionKind);
        return clone;
    }

    static BattleHexSnapshot CloneHex(BattleHexSnapshot hex)
    {
        return new BattleHexSnapshot
        {
            C = hex.C,
            R = hex.R,
            IsWalkable = hex.IsWalkable,
            OccupyingUnitId = NormalizeString(hex.OccupyingUnitId),
            TrapName = NormalizeString(hex.TrapName),
            TrapRemainingDurationOrTurns = hex.TrapRemainingDurationOrTurns,
            TrapSourceUnitId = NormalizeString(hex.TrapSourceUnitId)
        };
    }

    static BattleUnitSnapshot CloneUnit(BattleUnitSnapshot unit)
    {
        BattleUnitSnapshot clone = new BattleUnitSnapshot
        {
            RuntimeUnitId = NormalizeString(unit.RuntimeUnitId),
            TeamIndex = unit.TeamIndex,
            RosterIndexWithinTeam = unit.RosterIndexWithinTeam,
            UnitName = NormalizeString(unit.UnitName),
            UnitType = NormalizeString(unit.UnitType),
            C = unit.C,
            R = unit.R,
            Amount = unit.Amount,
            TempHP = unit.TempHP,
            BaseHP = unit.BaseHP,
            Attack = unit.Attack,
            Defense = unit.Defense,
            MovementSpeed = unit.MovementSpeed,
            Initiative = unit.Initiative,
            MinDamage = unit.MinDamage,
            MaxDamage = unit.MaxDamage,
            IsAlive = unit.IsAlive,
            IsRange = unit.IsRange,
            Waited = unit.Waited,
            Moved = unit.Moved,
            MovedThisTurn = unit.MovedThisTurn,
            UsedSkillThisTurn = unit.UsedSkillThisTurn,
            CounterAttackAvailable = unit.CounterAttackAvailable,
            CounterAttacks = unit.CounterAttacks,
            TempCounterAttacks = unit.TempCounterAttacks,
            CanMoveAfterSkillThisTurn = unit.CanMoveAfterSkillThisTurn,
            SkillIdsBySlot = CopyStringsPreservingOrder(unit.SkillIdsBySlot),
            CooldownsBySlot = CopyIntsPreservingOrder(unit.CooldownsBySlot),
            UsedSkillIdsThisTurn = CopySortedDistinctStrings(unit.UsedSkillIdsThisTurn),
            Statuses = CloneAndSortStatuses(unit.Statuses)
        };

        return clone;
    }

    static List<BattleStatusSnapshot> CloneAndSortStatuses(IEnumerable<BattleStatusSnapshot> statuses)
    {
        List<BattleStatusSnapshot> result = new List<BattleStatusSnapshot>();
        if (statuses != null)
        {
            foreach (BattleStatusSnapshot status in statuses)
            {
                if (status == null)
                {
                    continue;
                }

                result.Add(CloneStatus(status));
            }
        }

        result.Sort(CompareStatuses);
        return result;
    }

    static BattleStatusSnapshot CloneStatus(BattleStatusSnapshot status)
    {
        return new BattleStatusSnapshot
        {
            StatusId = NormalizeString(status.StatusId),
            SourceSkillId = NormalizeString(status.SourceSkillId),
            SourceUnitId = NormalizeString(status.SourceUnitId),
            RemainingDurationOrTurns = status.RemainingDurationOrTurns,
            HpModifier = status.HpModifier,
            AttackModifier = status.AttackModifier,
            DefenseModifier = status.DefenseModifier,
            MovementModifier = status.MovementModifier,
            InitiativeModifier = status.InitiativeModifier,
            MaxDamageModifier = status.MaxDamageModifier,
            MinDamageModifier = status.MinDamageModifier,
            DamageOverTime = status.DamageOverTime,
            ResistanceModifier = status.ResistanceModifier,
            CounterAttacksModifier = status.CounterAttacksModifier,
            DamageModifier = status.DamageModifier,
            IsStackable = status.IsStackable
        };
    }

    static List<string> CopyStringsPreservingOrder(IEnumerable<string> values)
    {
        List<string> result = new List<string>();
        if (values == null)
        {
            return result;
        }

        foreach (string value in values)
        {
            result.Add(NormalizeString(value));
        }

        return result;
    }

    static List<int> CopyIntsPreservingOrder(IEnumerable<int> values)
    {
        List<int> result = new List<int>();
        if (values == null)
        {
            return result;
        }

        foreach (int value in values)
        {
            result.Add(value);
        }

        return result;
    }

    static List<string> CopySortedDistinctStrings(IEnumerable<string> values)
    {
        List<string> result = new List<string>();
        if (values == null)
        {
            return result;
        }

        foreach (string value in values)
        {
            string normalized = NormalizeString(value);
            if (normalized.Length == 0)
            {
                continue;
            }

            if (result.Contains(normalized))
            {
                continue;
            }

            result.Add(normalized);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    static int CompareHexes(BattleHexSnapshot left, BattleHexSnapshot right)
    {
        int cCompare = left.C.CompareTo(right.C);
        if (cCompare != 0)
        {
            return cCompare;
        }

        return left.R.CompareTo(right.R);
    }

    static int CompareUnits(BattleUnitSnapshot left, BattleUnitSnapshot right)
    {
        int idCompare = string.CompareOrdinal(left.RuntimeUnitId, right.RuntimeUnitId);
        if (idCompare != 0)
        {
            return idCompare;
        }

        int teamCompare = left.TeamIndex.CompareTo(right.TeamIndex);
        if (teamCompare != 0)
        {
            return teamCompare;
        }

        return left.RosterIndexWithinTeam.CompareTo(right.RosterIndexWithinTeam);
    }

    static int CompareStatuses(BattleStatusSnapshot left, BattleStatusSnapshot right)
    {
        int statusCompare = string.CompareOrdinal(left.StatusId, right.StatusId);
        if (statusCompare != 0)
        {
            return statusCompare;
        }

        int sourceUnitCompare = string.CompareOrdinal(left.SourceUnitId, right.SourceUnitId);
        if (sourceUnitCompare != 0)
        {
            return sourceUnitCompare;
        }

        int remainingCompare = left.RemainingDurationOrTurns.CompareTo(right.RemainingDurationOrTurns);
        if (remainingCompare != 0)
        {
            return remainingCompare;
        }

        int sourceSkillCompare = string.CompareOrdinal(left.SourceSkillId, right.SourceSkillId);
        if (sourceSkillCompare != 0)
        {
            return sourceSkillCompare;
        }

        int hpCompare = left.HpModifier.CompareTo(right.HpModifier);
        if (hpCompare != 0)
        {
            return hpCompare;
        }

        int attackCompare = left.AttackModifier.CompareTo(right.AttackModifier);
        if (attackCompare != 0)
        {
            return attackCompare;
        }

        int defenseCompare = left.DefenseModifier.CompareTo(right.DefenseModifier);
        if (defenseCompare != 0)
        {
            return defenseCompare;
        }

        int movementCompare = left.MovementModifier.CompareTo(right.MovementModifier);
        if (movementCompare != 0)
        {
            return movementCompare;
        }

        int initiativeCompare = left.InitiativeModifier.CompareTo(right.InitiativeModifier);
        if (initiativeCompare != 0)
        {
            return initiativeCompare;
        }

        int maxDamageCompare = left.MaxDamageModifier.CompareTo(right.MaxDamageModifier);
        if (maxDamageCompare != 0)
        {
            return maxDamageCompare;
        }

        int minDamageCompare = left.MinDamageModifier.CompareTo(right.MinDamageModifier);
        if (minDamageCompare != 0)
        {
            return minDamageCompare;
        }

        int damageOverTimeCompare = left.DamageOverTime.CompareTo(right.DamageOverTime);
        if (damageOverTimeCompare != 0)
        {
            return damageOverTimeCompare;
        }

        int resistanceCompare = left.ResistanceModifier.CompareTo(right.ResistanceModifier);
        if (resistanceCompare != 0)
        {
            return resistanceCompare;
        }

        int counterAttackCompare = left.CounterAttacksModifier.CompareTo(right.CounterAttacksModifier);
        if (counterAttackCompare != 0)
        {
            return counterAttackCompare;
        }

        int damageCompare = left.DamageModifier.CompareTo(right.DamageModifier);
        if (damageCompare != 0)
        {
            return damageCompare;
        }

        return left.IsStackable.CompareTo(right.IsStackable);
    }

    static string ComputeHash(BattleSnapshot snapshot)
    {
        StringBuilder canonical = new StringBuilder(1024);
        canonical.Append("map|")
            .Append(snapshot.MapWidth).Append('|')
            .Append(snapshot.MapHeight).Append('|')
            .Append(B(snapshot.UsesLegacyHexLayout)).Append('\n');
        canonical.Append("seed|")
            .Append(snapshot.GameSeed).Append('|')
            .Append(Escape(snapshot.BattleId)).Append('|')
            .Append(snapshot.NextActionIndex).Append('\n');
        canonical.Append("active|").Append(Escape(snapshot.ActiveUnitId)).Append('\n');
        canonical.Append("turn|")
            .Append(snapshot.TurnState.RoundNumber).Append('|')
            .Append(B(snapshot.TurnState.IsResolvingNewTurnSequence)).Append('|')
            .Append(B(snapshot.TurnState.IsActionBlocking)).Append('|')
            .Append(Escape(snapshot.TurnState.ActiveActionKind)).Append('\n');

        for (int i = 0; i < snapshot.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = snapshot.Hexes[i];
            canonical.Append("hex|")
                .Append(hex.C).Append('|')
                .Append(hex.R).Append('|')
                .Append(B(hex.IsWalkable)).Append('|')
                .Append(Escape(hex.OccupyingUnitId)).Append('|')
                .Append(Escape(hex.TrapName)).Append('|')
                .Append(hex.TrapRemainingDurationOrTurns).Append('|')
                .Append(Escape(hex.TrapSourceUnitId)).Append('\n');
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            canonical.Append("unit|")
                .Append(Escape(unit.RuntimeUnitId)).Append('|')
                .Append(unit.TeamIndex).Append('|')
                .Append(unit.RosterIndexWithinTeam).Append('|')
                .Append(Escape(unit.UnitName)).Append('|')
                .Append(Escape(unit.UnitType)).Append('|')
                .Append(unit.C).Append('|')
                .Append(unit.R).Append('|')
                .Append(unit.Amount).Append('|')
                .Append(unit.TempHP).Append('|')
                .Append(unit.BaseHP).Append('|')
                .Append(unit.Attack).Append('|')
                .Append(unit.Defense).Append('|')
                .Append(unit.MovementSpeed).Append('|')
                .Append(unit.Initiative).Append('|')
                .Append(unit.MinDamage).Append('|')
                .Append(unit.MaxDamage).Append('|')
                .Append(B(unit.IsAlive)).Append('|')
                .Append(B(unit.IsRange)).Append('|')
                .Append(B(unit.Waited)).Append('|')
                .Append(B(unit.Moved)).Append('|')
                .Append(B(unit.MovedThisTurn)).Append('|')
                .Append(B(unit.UsedSkillThisTurn)).Append('|')
                .Append(B(unit.CounterAttackAvailable)).Append('|')
                .Append(unit.CounterAttacks).Append('|')
                .Append(unit.TempCounterAttacks).Append('|')
                .Append(B(unit.CanMoveAfterSkillThisTurn)).Append('\n');

            AppendSkillSlots(canonical, unit);
            AppendUsedSkills(canonical, unit);
            AppendStatuses(canonical, unit);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(canonical.ToString());
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(bytes);
            StringBuilder hash = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hash.Append(hashBytes[i].ToString("x2"));
            }

            return hash.ToString();
        }
    }

    static void AppendSkillSlots(StringBuilder canonical, BattleUnitSnapshot unit)
    {
        int maxSlots = Math.Max(unit.SkillIdsBySlot.Count, unit.CooldownsBySlot.Count);
        for (int i = 0; i < maxSlots; i++)
        {
            string skillId = i < unit.SkillIdsBySlot.Count ? unit.SkillIdsBySlot[i] : string.Empty;
            int cooldown = i < unit.CooldownsBySlot.Count ? unit.CooldownsBySlot[i] : 0;
            canonical.Append("slot|")
                .Append(Escape(unit.RuntimeUnitId)).Append('|')
                .Append(i).Append('|')
                .Append(Escape(skillId)).Append('|')
                .Append(cooldown).Append('\n');
        }
    }

    static void AppendUsedSkills(StringBuilder canonical, BattleUnitSnapshot unit)
    {
        for (int i = 0; i < unit.UsedSkillIdsThisTurn.Count; i++)
        {
            canonical.Append("used|")
                .Append(Escape(unit.RuntimeUnitId)).Append('|')
                .Append(Escape(unit.UsedSkillIdsThisTurn[i])).Append('\n');
        }
    }

    static void AppendStatuses(StringBuilder canonical, BattleUnitSnapshot unit)
    {
        for (int i = 0; i < unit.Statuses.Count; i++)
        {
            BattleStatusSnapshot status = unit.Statuses[i];
            canonical.Append("status|")
                .Append(Escape(unit.RuntimeUnitId)).Append('|')
                .Append(Escape(status.StatusId)).Append('|')
                .Append(Escape(status.SourceSkillId)).Append('|')
                .Append(Escape(status.SourceUnitId)).Append('|')
                .Append(status.RemainingDurationOrTurns).Append('|')
                .Append(status.HpModifier).Append('|')
                .Append(status.AttackModifier).Append('|')
                .Append(status.DefenseModifier).Append('|')
                .Append(status.MovementModifier).Append('|')
                .Append(status.InitiativeModifier).Append('|')
                .Append(status.MaxDamageModifier).Append('|')
                .Append(status.MinDamageModifier).Append('|')
                .Append(status.DamageOverTime).Append('|')
                .Append(status.ResistanceModifier).Append('|')
                .Append(status.CounterAttacksModifier).Append('|')
                .Append(status.DamageModifier).Append('|')
                .Append(B(status.IsStackable)).Append('\n');
        }
    }

    static string NormalizeString(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    static string Escape(string value)
    {
        return NormalizeString(value)
            .Replace("\\", "\\\\")
            .Replace("|", "\\|")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    static int B(bool value)
    {
        return value ? 1 : 0;
    }
}
