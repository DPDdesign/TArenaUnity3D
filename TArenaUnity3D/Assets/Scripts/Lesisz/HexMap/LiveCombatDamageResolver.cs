using System;
using UnityEngine;

public static class LiveCombatDamageResolver
{
    public static bool TryCalculateDamage(
        TosterHexUnit actor,
        TosterHexUnit target,
        string rollPurpose,
        double damageScale,
        bool hasBaseDamageOverride,
        int baseDamageOverride,
        bool isStackable,
        bool consumeActorPureDamage,
        out CombatDamageResult damage,
        out string error)
    {
        damage = null;
        error = string.Empty;

        BattleSnapshot snapshot = BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot();
        if (snapshot == null)
        {
            error = "Live combat damage snapshot was unavailable.";
            return false;
        }

        string actorUnitId;
        if (TryResolveRuntimeUnitId(actor, out actorUnitId, out error) == false)
        {
            return false;
        }

        string targetUnitId;
        if (TryResolveRuntimeUnitId(target, out targetUnitId, out error) == false)
        {
            return false;
        }

        CombatDamageServiceResult result = CombatDamageService.Default.CalculateDamage(
            snapshot,
            new CombatDamageRequest
            {
                ActorUnitId = actorUnitId,
                TargetUnitId = targetUnitId,
                RollPurpose = string.IsNullOrEmpty(rollPurpose) ? CombatDamageRollPurpose.BasicAttack : rollPurpose,
                ActionIndex = Math.Max(0, snapshot.NextActionIndex),
                DamageScale = damageScale,
                HasBaseDamageOverride = hasBaseDamageOverride,
                BaseDamageOverride = baseDamageOverride,
                IsStackable = isStackable,
                ConsumeActorPureDamage = consumeActorPureDamage
            });

        if (result == null || result.IsValid == false || result.Damage == null)
        {
            error = result != null ? result.Error : "Live combat damage calculation failed.";
            return false;
        }

        damage = result.Damage;
        return true;
    }

    public static bool TryCalculateCommittedDamage(
        TosterHexUnit actor,
        TosterHexUnit target,
        string rollPurpose,
        double damageScale,
        out int committedDamage,
        out string error)
    {
        CombatDamageResult damage;
        if (TryCalculateDamage(
            actor,
            target,
            rollPurpose,
            damageScale,
            false,
            0,
            true,
            true,
            out damage,
            out error))
        {
            committedDamage = damage.CommittedDamage;
            return true;
        }

        committedDamage = 0;
        return false;
    }

    static bool TryResolveRuntimeUnitId(TosterHexUnit unit, out string runtimeUnitId, out string error)
    {
        runtimeUnitId = string.Empty;
        error = string.Empty;

        if (unit == null)
        {
            error = "Live combat damage unit was null.";
            return false;
        }

        HexMap hexMap = HexMap.Instance != null ? HexMap.Instance : UnityEngine.Object.FindObjectOfType<HexMap>();
        if (hexMap == null || hexMap.Teams == null)
        {
            error = "Live combat damage could not resolve HexMap teams.";
            return false;
        }

        for (int teamIndex = 0; teamIndex < hexMap.Teams.Count; teamIndex++)
        {
            TeamClass team = hexMap.Teams[teamIndex];
            if (team == null || team.Tosters == null)
            {
                continue;
            }

            for (int rosterIndex = 0; rosterIndex < team.Tosters.Count; rosterIndex++)
            {
                if (ReferenceEquals(team.Tosters[rosterIndex], unit))
                {
                    runtimeUnitId = BattleSnapshotRuntimeIds.CreateUnitId(teamIndex, rosterIndex);
                    return true;
                }
            }
        }

        error = "Live combat damage runtime unit id was missing for: " + (unit.Name ?? "<unnamed>") + ".";
        return false;
    }
}
