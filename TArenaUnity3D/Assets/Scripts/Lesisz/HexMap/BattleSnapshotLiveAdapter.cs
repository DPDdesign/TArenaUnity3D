using System.Collections.Generic;
using UnityEngine;

public static class BattleSnapshotLiveAdapter
{
    struct RuntimeUnitReference
    {
        public int TeamIndex;
        public int RosterIndexWithinTeam;
        public string RuntimeUnitId;
    }

    public static BattleSnapshot BuildCurrentSceneSnapshot()
    {
        return BuildSnapshot(
            Object.FindObjectOfType<HexMap>(),
            Object.FindObjectOfType<MouseControler>(),
            Object.FindObjectOfType<TurnManager>(),
            BattleActionLifecycle.Instance);
    }

    public static BattleSnapshot BuildSnapshot(
        HexMap hexMap,
        MouseControler mouseControler,
        TurnManager turnManager,
        BattleActionLifecycle actionLifecycle)
    {
        if (hexMap == null || hexMap.IsBattleReadyForTacticalActions == false)
        {
            return null;
        }

        Dictionary<TosterHexUnit, RuntimeUnitReference> runtimeUnitReferences = BuildRuntimeUnitReferences(hexMap.Teams);
        List<BattleUnitSnapshot> units = BuildUnits(runtimeUnitReferences);
        List<BattleHexSnapshot> hexes = BuildHexes(hexMap, runtimeUnitReferences);

        string activeUnitId = ResolveUnitId(mouseControler != null ? mouseControler.SelectedToster : null, runtimeUnitReferences);
        if (activeUnitId.Length == 0)
        {
            activeUnitId = ResolveUnitId(actionLifecycle != null ? actionLifecycle.ActiveActor : null, runtimeUnitReferences);
        }

        BattleTurnStateSnapshot turnState = new BattleTurnStateSnapshot
        {
            RoundNumber = turnManager != null ? turnManager.Tura : 0,
            IsResolvingNewTurnSequence = turnManager != null && turnManager.IsResolvingNewTurnSequence,
            IsActionBlocking = BattleActionLifecycle.IsActionBlocking,
            ActiveActionKind = actionLifecycle != null && actionLifecycle.IsBusy ? actionLifecycle.ActiveKindName : string.Empty
        };

        return BattleSnapshotBuilder.Build(
            hexMap.CurrentLength,
            hexMap.CurrentWidth,
            hexes,
            units,
            activeUnitId,
            turnState);
    }

    static Dictionary<TosterHexUnit, RuntimeUnitReference> BuildRuntimeUnitReferences(List<TeamClass> teams)
    {
        Dictionary<TosterHexUnit, RuntimeUnitReference> references = new Dictionary<TosterHexUnit, RuntimeUnitReference>();
        if (teams == null)
        {
            return references;
        }

        for (int teamIndex = 0; teamIndex < teams.Count; teamIndex++)
        {
            TeamClass team = teams[teamIndex];
            if (team == null || team.Tosters == null)
            {
                continue;
            }

            for (int rosterIndex = 0; rosterIndex < team.Tosters.Count; rosterIndex++)
            {
                TosterHexUnit unit = team.Tosters[rosterIndex];
                if (unit == null || references.ContainsKey(unit))
                {
                    continue;
                }

                references.Add(unit, new RuntimeUnitReference
                {
                    TeamIndex = teamIndex,
                    RosterIndexWithinTeam = rosterIndex,
                    RuntimeUnitId = BattleSnapshotRuntimeIds.CreateUnitId(teamIndex, rosterIndex)
                });
            }
        }

        return references;
    }

    static List<BattleUnitSnapshot> BuildUnits(Dictionary<TosterHexUnit, RuntimeUnitReference> runtimeUnitReferences)
    {
        List<BattleUnitSnapshot> units = new List<BattleUnitSnapshot>();
        foreach (KeyValuePair<TosterHexUnit, RuntimeUnitReference> pair in runtimeUnitReferences)
        {
            TosterHexUnit unit = pair.Key;
            RuntimeUnitReference runtimeUnitReference = pair.Value;
            units.Add(new BattleUnitSnapshot
            {
                RuntimeUnitId = runtimeUnitReference.RuntimeUnitId,
                TeamIndex = runtimeUnitReference.TeamIndex,
                RosterIndexWithinTeam = runtimeUnitReference.RosterIndexWithinTeam,
                UnitName = unit.Name,
                UnitType = unit.Name,
                C = unit.Hex != null ? unit.Hex.C : unit.C,
                R = unit.Hex != null ? unit.Hex.R : unit.R,
                Amount = unit.Amount,
                TempHP = unit.TempHP,
                BaseHP = unit.HP,
                Attack = unit.Att,
                Defense = unit.Def,
                MovementSpeed = unit.MovmentSpeed,
                Initiative = unit.Initiative,
                MinDamage = unit.mindmg,
                MaxDamage = unit.maxdmg,
                IsAlive = unit.isDead == false && unit.Amount > 0,
                IsRange = unit.isRange,
                Waited = unit.Waited,
                Moved = unit.Moved,
                MovedThisTurn = unit.MovedThisTurn,
                UsedSkillThisTurn = unit.UsedSkillThisTurn,
                UsedSkillIdsThisTurn = new List<string>(unit.UsedSkillIdsThisTurn ?? new List<string>()),
                CanMoveAfterSkillThisTurn = unit.CanMoveAfterSkillThisTurn,
                CooldownsBySlot = new List<int>(unit.cooldowns ?? new List<int>()),
                SkillIdsBySlot = new List<string>(unit.skillstrings ?? new List<string>()),
                Statuses = BuildStatuses(unit.SpellsGoingOn, runtimeUnitReferences)
            });
        }

        return units;
    }

    static List<BattleStatusSnapshot> BuildStatuses(
        List<TimeSpells.SpellOverTime> spells,
        Dictionary<TosterHexUnit, RuntimeUnitReference> runtimeUnitReferences)
    {
        List<BattleStatusSnapshot> statuses = new List<BattleStatusSnapshot>();
        if (spells == null)
        {
            return statuses;
        }

        for (int i = 0; i < spells.Count; i++)
        {
            TimeSpells.SpellOverTime spell = spells[i];
            if (spell == null)
            {
                continue;
            }

            statuses.Add(new BattleStatusSnapshot
            {
                StatusId = spell.nameofspell,
                SourceSkillId = spell.nameofspell,
                SourceUnitId = ResolveUnitId(spell.SourceUnit, runtimeUnitReferences),
                RemainingDurationOrTurns = spell.Time,
                HpModifier = spell.HpModifier,
                AttackModifier = spell.AttackModifier,
                DefenseModifier = spell.DefenseModifier,
                MovementModifier = spell.MovementModifier,
                InitiativeModifier = spell.InitiativeModifier,
                MaxDamageModifier = spell.MaxDamageModifier,
                MinDamageModifier = spell.MinDamageModifier,
                DamageOverTime = spell.DamageOverTime,
                ResistanceModifier = spell.ResistanceModifier,
                CounterAttacksModifier = spell.CounterAttacksModifier,
                DamageModifier = spell.DamageModifier,
                IsStackable = spell.isStackable
            });
        }

        return statuses;
    }

    static List<BattleHexSnapshot> BuildHexes(
        HexMap hexMap,
        Dictionary<TosterHexUnit, RuntimeUnitReference> runtimeUnitReferences)
    {
        List<BattleHexSnapshot> hexes = new List<BattleHexSnapshot>();
        for (int c = 0; c < hexMap.CurrentLength; c++)
        {
            for (int r = 0; r < hexMap.CurrentWidth; r++)
            {
                HexClass hex = hexMap.GetHexAt(c, r);
                BattleHexSnapshot snapshot = new BattleHexSnapshot
                {
                    C = c,
                    R = r,
                    IsWalkable = hex != null && hex.BaseMovementCost() >= 0,
                    OccupyingUnitId = ResolveOccupyingUnitId(hex, runtimeUnitReferences)
                };

                if (hex != null && hex.isTraped && hex.trap != null)
                {
                    snapshot.TrapName = hex.trap.NameOfTraps;
                    snapshot.TrapRemainingDurationOrTurns = hex.trap.Time;
                    snapshot.TrapSourceUnitId = ResolveUnitId(hex.trap.TosterWhoSetupThisTrap, runtimeUnitReferences);
                }

                hexes.Add(snapshot);
            }
        }

        return hexes;
    }

    static string ResolveOccupyingUnitId(
        HexClass hex,
        Dictionary<TosterHexUnit, RuntimeUnitReference> runtimeUnitReferences)
    {
        if (hex == null || hex.Tosters == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < hex.Tosters.Count; i++)
        {
            string unitId = ResolveUnitId(hex.Tosters[i], runtimeUnitReferences);
            if (unitId.Length > 0)
            {
                return unitId;
            }
        }

        return string.Empty;
    }

    static string ResolveUnitId(
        TosterHexUnit unit,
        Dictionary<TosterHexUnit, RuntimeUnitReference> runtimeUnitReferences)
    {
        if (unit == null)
        {
            return string.Empty;
        }

        RuntimeUnitReference runtimeUnitReference;
        if (runtimeUnitReferences.TryGetValue(unit, out runtimeUnitReference))
        {
            return runtimeUnitReference.RuntimeUnitId;
        }

        return string.Empty;
    }
}
