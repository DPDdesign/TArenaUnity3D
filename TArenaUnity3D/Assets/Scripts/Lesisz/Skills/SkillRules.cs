using System;
using System.Collections.Generic;

public enum SkillValidationStatus
{
    Valid,
    Invalid,
    UnsupportedLegacySkill,
    MissingSkillDefinition,
    BlockedByTurnState
}

public enum SkillRejectReason
{
    None,
    MissingContext,
    MissingSnapshot,
    MissingActor,
    MissingSkillDefinition,
    ActorIsNotActive,
    ActorIsDead,
    ActorDoesNotOwnSkill,
    SkillIsPassive,
    SkillOnCooldown,
    ActorAlreadyWaited,
    SkillAlreadyUsedThisTurn,
    CannotUseAfterMove,
    TargetCountMismatch,
    DuplicateTarget,
    TargetHexMissing,
    TargetHexNotLegal,
    TargetOccupied,
    TargetEmpty,
    TargetWrongTeam,
    TargetHasTrap,
    NoSpawnHex,
    TurnStateBlocked
}

public class SkillValidationResult
{
    public SkillValidationStatus Status = SkillValidationStatus.Invalid;
    public SkillRejectReason RejectReason = SkillRejectReason.None;
    public string Message = string.Empty;
    public SkillCast Cast;

    public bool IsValid
    {
        get { return Status == SkillValidationStatus.Valid && RejectReason == SkillRejectReason.None; }
    }

    public static SkillValidationResult Valid(SkillCast cast = null)
    {
        return new SkillValidationResult
        {
            Status = SkillValidationStatus.Valid,
            RejectReason = SkillRejectReason.None,
            Cast = cast
        };
    }

    public static SkillValidationResult Invalid(SkillRejectReason reason, string message = "", SkillValidationStatus status = SkillValidationStatus.Invalid)
    {
        return new SkillValidationResult
        {
            Status = status,
            RejectReason = reason,
            Message = message ?? string.Empty
        };
    }
}

public static class SkillRules
{
    public static SkillValidationResult CanUse(SkillContext context)
    {
        SkillSnapshotIndex index;
        BattleUnitSnapshot actor;
        SkillValidationResult setup = ValidateContext(context, out index, out actor);
        if (setup.IsValid == false)
        {
            return setup;
        }

        ActivationRuleData activation = GetActivation(context);
        if (activation.activationKind == SkillActivationKind.Passive)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.SkillIsPassive);
        }

        if (context.Snapshot.TurnState != null &&
            (context.Snapshot.TurnState.IsActionBlocking || context.Snapshot.TurnState.IsResolvingNewTurnSequence))
        {
            return SkillValidationResult.Invalid(SkillRejectReason.TurnStateBlocked, string.Empty, SkillValidationStatus.BlockedByTurnState);
        }

        if (string.IsNullOrEmpty(context.Snapshot.ActiveUnitId) == false &&
            string.Equals(context.Snapshot.ActiveUnitId, actor.RuntimeUnitId, StringComparison.Ordinal) == false)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.ActorIsNotActive);
        }

        if (actor.IsAlive == false)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.ActorIsDead);
        }

        int skillSlot = FindSkillSlot(actor, context.SkillId, context.SkillSlot);
        if (skillSlot < 0)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.ActorDoesNotOwnSkill);
        }

        if (skillSlot < actor.CooldownsBySlot.Count && actor.CooldownsBySlot[skillSlot] > 0)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.SkillOnCooldown);
        }

        if (actor.Waited)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.ActorAlreadyWaited);
        }

        if (activation.repeatableInTurn == false &&
            actor.UsedSkillIdsThisTurn != null &&
            actor.UsedSkillIdsThisTurn.Contains(context.SkillId))
        {
            return SkillValidationResult.Invalid(SkillRejectReason.SkillAlreadyUsedThisTurn);
        }

        if (actor.MovedThisTurn && activation.canUseAfterMove == false && activation.repeatableInTurn == false)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.CannotUseAfterMove);
        }

        return SkillValidationResult.Valid();
    }

    public static List<SkillTarget> GetTargets(SkillContext context, IReadOnlyList<HexCoord> selectedTargets)
    {
        List<SkillTarget> targets = new List<SkillTarget>();
        SkillSnapshotIndex index;
        BattleUnitSnapshot actor;
        if (ValidateContext(context, out index, out actor).IsValid == false)
        {
            return targets;
        }

        TargetingRuleData targeting = GetTargeting(context);
        int selectedCount = selectedTargets == null ? 0 : selectedTargets.Count;
        if (selectedCount >= targeting.targetCount)
        {
            return targets;
        }

        SkillTargetRole role = ResolveRole(targeting, selectedCount);
        switch (role)
        {
            case SkillTargetRole.ActorSelf:
                targets.Add(new SkillTarget(actor.C, actor.R, role, actor.RuntimeUnitId));
                break;
            case SkillTargetRole.EnemyUnitHex:
                AddUnitTargets(targets, index, actor, role, false, true, GetUnitTargetRange(targeting, role));
                break;
            case SkillTargetRole.AllyUnitHex:
                AddUnitTargets(targets, index, actor, role, true, false, GetUnitTargetRange(targeting, role));
                break;
            case SkillTargetRole.AllyOrSelfUnitHex:
                AddUnitTargets(targets, index, actor, role, true, true, GetUnitTargetRange(targeting, role));
                break;
            case SkillTargetRole.AreaCenterHex:
                AddWalkableHexTargets(targets, index, role);
                break;
            case SkillTargetRole.EmptyPlacementHex:
                AddEmptyHexTargets(targets, index, role, true, true);
                break;
            case SkillTargetRole.RushLineHex:
                AddRushTargets(targets, index, actor, role);
                break;
            case SkillTargetRole.MovementDestinationHex:
                AddMovementTargets(targets, index, actor, role);
                break;
            case SkillTargetRole.DirectionalImpactHex:
                AddDirectionalImpactTargets(targets, index, selectedTargets, role);
                break;
            case SkillTargetRole.EmptyDestinationHex:
                AddEmptyDestinationsAroundActor(targets, index, actor, role, Math.Max(1, targeting.radius));
                break;
            case SkillTargetRole.AdjacentEmptyDestinationHex:
                AddAdjacentEmptyDestinations(targets, index, actor, role);
                break;
        }

        return targets;
    }

    public static SkillValidationResult Validate(SkillUse use, SkillContext context)
    {
        SkillValidationResult canUse = CanUse(context);
        if (canUse.IsValid == false)
        {
            return canUse;
        }

        SkillSnapshotIndex index = SkillSnapshotIndex.Build(context.Snapshot);
        BattleUnitSnapshot actor = index.GetUnitOrNull(context.ActorUnitId);
        TargetingRuleData targeting = GetTargeting(context);
        List<HexCoord> selected = CopyHexes(use != null ? use.SelectedHexes : null);

        if (selected.Count != targeting.targetCount)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.TargetCountMismatch);
        }

        if (targeting.allowDuplicateTargets == false && HasDuplicateHex(selected))
        {
            return SkillValidationResult.Invalid(SkillRejectReason.DuplicateTarget);
        }

        List<HexCoord> accepted = new List<HexCoord>();
        for (int i = 0; i < selected.Count; i++)
        {
            List<SkillTarget> legalTargets = GetTargets(context, accepted);
            if (ContainsTarget(legalTargets, selected[i]) == false)
            {
                return SkillValidationResult.Invalid(SkillRejectReason.TargetHexNotLegal);
            }

            accepted.Add(new HexCoord(selected[i].C, selected[i].R));
        }

        SkillValidationResult resolved = ResolveCast(context, index, actor, selected);
        if (resolved.IsValid == false)
        {
            return resolved;
        }

        return resolved;
    }

    public static SkillResult Preview(SkillCast cast, SkillContext context)
    {
        SkillResult result = new SkillResult
        {
            ActorUnitId = cast == null ? string.Empty : cast.ActorUnitId,
            SkillId = cast == null ? string.Empty : cast.SkillId,
            ActionIndex = context != null && context.Snapshot != null ? context.Snapshot.NextActionIndex : 0
        };

        if (cast == null)
        {
            return result;
        }

        SkillEffect[] effects = cast.Effects ?? new SkillEffect[0];
        for (int i = 0; i < effects.Length; i++)
        {
            AddPreviewEvent(result, cast, effects[i]);
        }

        if (cast.CooldownTurns > 0)
        {
            result.Add(new SkillResultEvent(SkillResultEventType.CooldownApplied, cast) { Amount = cast.CooldownTurns });
        }

        if (cast.ConsumesTurn)
        {
            result.Add(new SkillResultEvent(SkillResultEventType.TurnCostApplied, cast));
        }

        return result;
    }

    public static SkillResult Apply(SkillCast cast, SkillContext context, ISkillRuntime runtime)
    {
        if (runtime != null)
        {
            return runtime.Apply(cast, context);
        }

        return Preview(cast, context);
    }

    static SkillValidationResult ResolveCast(
        SkillContext context,
        SkillSnapshotIndex index,
        BattleUnitSnapshot actor,
        List<HexCoord> selected)
    {
        ActivationRuleData activation = GetActivation(context);
        ResolutionRuleData resolution = GetResolution(context);
        SkillCast cast = new SkillCast
        {
            ActorUnitId = actor.RuntimeUnitId,
            SkillId = context.SkillId,
            SelectedHexes = CopyHexes(selected),
            CooldownTurns = activation.cooldownTurns,
            ConsumesTurn = activation.consumesTurn,
            CanMoveAfterUse = activation.canMoveAfterUse,
            RepeatableInTurn = activation.repeatableInTurn,
            Effects = GetEffects(context)
        };

        switch (resolution.resolutionFamily)
        {
            case SkillResolutionFamily.EmptyHexPlacement:
                cast.AffectedHexes.AddRange(CopyHexes(selected));
                break;
            case SkillResolutionFamily.DirectUnit:
                ResolveDirectUnit(cast, index, actor, selected);
                break;
            case SkillResolutionFamily.MultiDirectUnit:
                ResolveMultiDirect(cast, context, index, actor, selected);
                break;
            case SkillResolutionFamily.AreaAroundTarget:
                ResolveAreaAroundTarget(cast, index, selected, Math.Max(1, resolution.radius));
                break;
            case SkillResolutionFamily.AreaAroundCaster:
                ResolveAreaAroundCaster(cast, index, actor, Math.Max(1, resolution.radius));
                break;
            case SkillResolutionFamily.LineScan:
                ResolveLineScan(cast, index, actor, selected);
                break;
            case SkillResolutionFamily.MoveThenDirectionalAreaAttack:
                ResolveMoveThenDirectional(cast, index, selected, Math.Max(1, resolution.radius));
                break;
            case SkillResolutionFamily.TeleportTargetToDestination:
                ResolveTeleportTarget(cast, index, selected);
                break;
            case SkillResolutionFamily.AroundPostMoveCaster:
                ResolveAroundPostMoveCaster(cast, index, selected, Math.Max(1, resolution.radius));
                break;
            case SkillResolutionFamily.SpawnNearTarget:
                if (ResolveSpawnNearTarget(cast, index, actor, selected) == false)
                {
                    return SkillValidationResult.Invalid(SkillRejectReason.NoSpawnHex);
                }
                break;
        }

        return SkillValidationResult.Valid(cast);
    }

    static SkillValidationResult ValidateContext(
        SkillContext context,
        out SkillSnapshotIndex index,
        out BattleUnitSnapshot actor)
    {
        index = null;
        actor = null;
        if (context == null)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.MissingContext);
        }

        if (context.Snapshot == null)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.MissingSnapshot);
        }

        if (HasSkillDefinition(context) == false || string.IsNullOrEmpty(context.SkillId))
        {
            return SkillValidationResult.Invalid(SkillRejectReason.MissingSkillDefinition, string.Empty, SkillValidationStatus.MissingSkillDefinition);
        }

        index = SkillSnapshotIndex.Build(context.Snapshot);
        actor = index.GetUnitOrNull(context.ActorUnitId);
        if (actor == null)
        {
            return SkillValidationResult.Invalid(SkillRejectReason.MissingActor);
        }

        return SkillValidationResult.Valid();
    }

    static bool HasSkillDefinition(SkillContext context)
    {
        return context != null && (context.SkillSpec != null || context.SkillDefinition != null);
    }

    static ActivationRuleData GetActivation(SkillContext context)
    {
        if (context != null && context.SkillSpec != null && context.SkillSpec.ActivationRule != null)
        {
            return context.SkillSpec.ActivationRule;
        }

        return context != null && context.SkillDefinition != null
            ? context.SkillDefinition.ActivationRule
            : new ActivationRuleData();
    }

    static TargetingRuleData GetTargeting(SkillContext context)
    {
        if (context != null && context.SkillSpec != null && context.SkillSpec.TargetingRule != null)
        {
            return context.SkillSpec.TargetingRule;
        }

        return context != null && context.SkillDefinition != null
            ? context.SkillDefinition.TargetingRule
            : new TargetingRuleData();
    }

    static ResolutionRuleData GetResolution(SkillContext context)
    {
        if (context != null && context.SkillSpec != null && context.SkillSpec.ResolutionRule != null)
        {
            return context.SkillSpec.ResolutionRule;
        }

        return context != null && context.SkillDefinition != null
            ? context.SkillDefinition.ResolutionRule
            : new ResolutionRuleData();
    }

    static SkillEffect[] GetEffects(SkillContext context)
    {
        if (context != null && context.SkillSpec != null)
        {
            return SkillEffect.CloneArray(context.SkillSpec.Effects);
        }

        return context != null && context.SkillDefinition != null
            ? context.SkillDefinition.Effects
            : new SkillEffect[0];
    }

    static int FindSkillSlot(BattleUnitSnapshot actor, string skillId, int preferredSlot)
    {
        if (actor == null || actor.SkillIdsBySlot == null)
        {
            return -1;
        }

        if (preferredSlot >= 0 &&
            preferredSlot < actor.SkillIdsBySlot.Count &&
            string.Equals(actor.SkillIdsBySlot[preferredSlot], skillId, StringComparison.Ordinal))
        {
            return preferredSlot;
        }

        for (int i = 0; i < actor.SkillIdsBySlot.Count; i++)
        {
            if (string.Equals(actor.SkillIdsBySlot[i], skillId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    static SkillTargetRole ResolveRole(TargetingRuleData targeting, int selectedCount)
    {
        if (targeting == null || targeting.targetRoles == null || targeting.targetRoles.Length == 0)
        {
            return SkillTargetRole.None;
        }

        if (selectedCount >= 0 && selectedCount < targeting.targetRoles.Length)
        {
            return targeting.targetRoles[selectedCount];
        }

        return targeting.targetRoles[targeting.targetRoles.Length - 1];
    }

    static int GetUnitTargetRange(TargetingRuleData targeting, SkillTargetRole role)
    {
        if (targeting == null || targeting.targetFamily != SkillTargetFamily.Movement)
        {
            return 0;
        }

        if (role != SkillTargetRole.EnemyUnitHex)
        {
            return 0;
        }

        return Math.Max(0, targeting.radius);
    }

    static void AddUnitTargets(
        List<SkillTarget> targets,
        SkillSnapshotIndex index,
        BattleUnitSnapshot actor,
        SkillTargetRole role,
        bool ally,
        bool includeSelf,
        int maxRange)
    {
        for (int i = 0; i < index.Units.Count; i++)
        {
            BattleUnitSnapshot unit = index.Units[i];
            if (unit == null || unit.IsAlive == false)
            {
                continue;
            }

            bool isSelf = string.Equals(unit.RuntimeUnitId, actor.RuntimeUnitId, StringComparison.Ordinal);
            if (isSelf && includeSelf == false)
            {
                continue;
            }

            bool sameTeam = unit.TeamIndex == actor.TeamIndex;
            if (ally && sameTeam == false)
            {
                continue;
            }

            if (ally == false && sameTeam)
            {
                continue;
            }

            if (maxRange > 0 && HexDistance(actor.C, actor.R, unit.C, unit.R) > maxRange)
            {
                continue;
            }

            targets.Add(new SkillTarget(unit.C, unit.R, role, unit.RuntimeUnitId));
        }
    }

    static void AddWalkableHexTargets(List<SkillTarget> targets, SkillSnapshotIndex index, SkillTargetRole role)
    {
        for (int i = 0; i < index.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = index.Hexes[i];
            if (hex != null && hex.IsWalkable)
            {
                targets.Add(new SkillTarget(hex.C, hex.R, role, hex.OccupyingUnitId));
            }
        }
    }

    static void AddEmptyHexTargets(
        List<SkillTarget> targets,
        SkillSnapshotIndex index,
        SkillTargetRole role,
        bool requireWalkable,
        bool rejectExistingTrap)
    {
        for (int i = 0; i < index.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = index.Hexes[i];
            if (hex == null)
            {
                continue;
            }

            if (requireWalkable && hex.IsWalkable == false)
            {
                continue;
            }

            if (string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
            {
                continue;
            }

            if (rejectExistingTrap && string.IsNullOrEmpty(hex.TrapName) == false)
            {
                continue;
            }

            targets.Add(new SkillTarget(hex.C, hex.R, role));
        }
    }

    static void AddRushTargets(List<SkillTarget> targets, SkillSnapshotIndex index, BattleUnitSnapshot actor, SkillTargetRole role)
    {
        int dc = actor.TeamIndex == 0 ? 1 : -1;
        int dr = 0;
        BattleHexSnapshot lastEmpty = null;

        for (int step = 1; step <= Math.Max(index.MapWidth, index.MapHeight) + 1; step++)
        {
            BattleHexSnapshot hex = index.GetHex(actor.C + dc * step, actor.R + dr * step);
            if (hex == null || hex.IsWalkable == false)
            {
                break;
            }

            if (string.IsNullOrEmpty(hex.OccupyingUnitId))
            {
                lastEmpty = hex;
                continue;
            }

            BattleUnitSnapshot unit = index.GetUnitOrNull(hex.OccupyingUnitId);
            if (unit != null && unit.TeamIndex != actor.TeamIndex)
            {
                targets.Add(new SkillTarget(hex.C, hex.R, role, unit.RuntimeUnitId));
            }
            else if (lastEmpty != null)
            {
                targets.Add(new SkillTarget(lastEmpty.C, lastEmpty.R, role));
            }

            lastEmpty = null;
            break;
        }

        if (lastEmpty != null)
        {
            targets.Add(new SkillTarget(lastEmpty.C, lastEmpty.R, role));
        }
    }

    static void AddMovementTargets(List<SkillTarget> targets, SkillSnapshotIndex index, BattleUnitSnapshot actor, SkillTargetRole role)
    {
        Dictionary<string, int> reachable = FindReachableHexCosts(actor, index);
        foreach (KeyValuePair<string, int> pair in reachable)
        {
            BattleHexSnapshot hex = index.GetHexByKey(pair.Key);
            if (hex == null)
            {
                continue;
            }

            bool isActorHex = hex.C == actor.C && hex.R == actor.R;
            if (isActorHex == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
            {
                continue;
            }

            targets.Add(new SkillTarget(hex.C, hex.R, role, hex.OccupyingUnitId));
        }
    }

    static void AddDirectionalImpactTargets(
        List<SkillTarget> targets,
        SkillSnapshotIndex index,
        IReadOnlyList<HexCoord> selectedTargets,
        SkillTargetRole role)
    {
        if (selectedTargets == null || selectedTargets.Count == 0 || selectedTargets[0] == null)
        {
            return;
        }

        HexCoord from = selectedTargets[0];
        List<HexCoord> neighbours = GetNeighbourCoordinates(from.C, from.R);
        for (int i = 0; i < neighbours.Count; i++)
        {
            BattleHexSnapshot hex = index.GetHex(neighbours[i].C, neighbours[i].R);
            if (hex != null && hex.IsWalkable)
            {
                targets.Add(new SkillTarget(hex.C, hex.R, role, hex.OccupyingUnitId));
            }
        }
    }

    static void AddEmptyDestinationsAroundActor(
        List<SkillTarget> targets,
        SkillSnapshotIndex index,
        BattleUnitSnapshot actor,
        SkillTargetRole role,
        int radius)
    {
        AddEmptyDestinationsWithinLegacyRadius(targets, index, actor.C, actor.R, role, radius, false);
    }

    static void AddAdjacentEmptyDestinations(List<SkillTarget> targets, SkillSnapshotIndex index, BattleUnitSnapshot actor, SkillTargetRole role)
    {
        AddEmptyDestinationsWithinLegacyRadius(targets, index, actor.C, actor.R, role, 1, false);
    }

    static void AddEmptyDestinationsWithinLegacyRadius(
        List<SkillTarget> targets,
        SkillSnapshotIndex index,
        int centerC,
        int centerR,
        SkillTargetRole role,
        int radius,
        bool includeCenter)
    {
        int safeRadius = Math.Max(0, radius);
        for (int dc = -safeRadius; dc <= safeRadius; dc++)
        {
            int minDr = Math.Max(-safeRadius, -dc - safeRadius);
            int maxDr = Math.Min(safeRadius, -dc + safeRadius);
            for (int dr = minDr; dr <= maxDr; dr++)
            {
                if (includeCenter == false && dc == 0 && dr == 0)
                {
                    continue;
                }

                BattleHexSnapshot hex = index.GetHex(centerC + dc, centerR + dr);
                if (hex != null && hex.IsWalkable && string.IsNullOrEmpty(hex.OccupyingUnitId))
                {
                    targets.Add(new SkillTarget(hex.C, hex.R, role));
                }
            }
        }
    }

    static void ResolveDirectUnit(SkillCast cast, SkillSnapshotIndex index, BattleUnitSnapshot actor, List<HexCoord> selected)
    {
        BattleUnitSnapshot target = selected.Count > 0 ? index.GetUnitAt(selected[0].C, selected[0].R) : actor;
        if (target == null)
        {
            target = actor;
        }

        cast.PrimaryTargetUnitId = target.RuntimeUnitId;
        cast.TargetUnitIds.Add(target.RuntimeUnitId);
        cast.AffectedUnitIds.Add(target.RuntimeUnitId);
    }

    static void ResolveMultiDirect(SkillCast cast, SkillContext context, SkillSnapshotIndex index, BattleUnitSnapshot actor, List<HexCoord> selected)
    {
        if (selected.Count == 0)
        {
            SkillTargetRole role = ResolveRole(GetTargeting(context), 0);
            if (role == SkillTargetRole.AutoAllEnemies || role == SkillTargetRole.AutoAllAllies)
            {
                for (int i = 0; i < index.Units.Count; i++)
                {
                    BattleUnitSnapshot unit = index.Units[i];
                    if (unit == null || unit.IsAlive == false)
                    {
                        continue;
                    }

                    bool include = role == SkillTargetRole.AutoAllEnemies
                        ? unit.TeamIndex != actor.TeamIndex
                        : unit.TeamIndex == actor.TeamIndex;
                    if (include)
                    {
                        cast.TargetUnitIds.Add(unit.RuntimeUnitId);
                        cast.AffectedUnitIds.Add(unit.RuntimeUnitId);
                    }
                }
            }

            return;
        }

        for (int i = 0; i < selected.Count; i++)
        {
            BattleUnitSnapshot unit = index.GetUnitAt(selected[i].C, selected[i].R);
            if (unit == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(cast.PrimaryTargetUnitId))
            {
                cast.PrimaryTargetUnitId = unit.RuntimeUnitId;
            }

            cast.TargetUnitIds.Add(unit.RuntimeUnitId);
            cast.AffectedUnitIds.Add(unit.RuntimeUnitId);
        }
    }

    static void ResolveAreaAroundTarget(SkillCast cast, SkillSnapshotIndex index, List<HexCoord> selected, int radius)
    {
        if (selected.Count == 0)
        {
            return;
        }

        AddAffectedArea(cast, index, selected[0].C, selected[0].R, radius, string.Empty);
    }

    static void ResolveAreaAroundCaster(SkillCast cast, SkillSnapshotIndex index, BattleUnitSnapshot actor, int radius)
    {
        AddAffectedArea(cast, index, actor.C, actor.R, radius, actor.RuntimeUnitId);
    }

    static void ResolveLineScan(SkillCast cast, SkillSnapshotIndex index, BattleUnitSnapshot actor, List<HexCoord> selected)
    {
        if (selected.Count == 0)
        {
            return;
        }

        HexCoord target = selected[0];
        BattleUnitSnapshot unit = index.GetUnitAt(target.C, target.R);
        if (unit != null && unit.TeamIndex != actor.TeamIndex)
        {
            cast.PrimaryTargetUnitId = unit.RuntimeUnitId;
            cast.TargetUnitIds.Add(unit.RuntimeUnitId);
            cast.AffectedUnitIds.Add(unit.RuntimeUnitId);
            cast.ImpactHex = new HexCoord(target.C, target.R);

            int dc;
            int dr;
            if (TryResolveLineDirection(actor, target, out dc, out dr) == false)
            {
                cast.DestinationHex = new HexCoord(actor.C, actor.R);
                return;
            }

            BattleHexSnapshot previous = index.GetHex(target.C - dc, target.R - dr);
            cast.DestinationHex = previous == null ? new HexCoord(actor.C, actor.R) : new HexCoord(previous.C, previous.R);
        }
        else
        {
            cast.DestinationHex = new HexCoord(target.C, target.R);
        }
    }

    static bool TryResolveLineDirection(BattleUnitSnapshot actor, HexCoord target, out int dc, out int dr)
    {
        dc = 0;
        dr = 0;
        if (actor == null || target == null)
        {
            return false;
        }

        List<HexCoord> directions = GetNeighbourCoordinates(actor.C, actor.R);
        int maxSteps = HexDistance(actor.C, actor.R, target.C, target.R) + 1;
        for (int i = 0; i < directions.Count; i++)
        {
            HexCoord direction = directions[i];
            int candidateDc = direction.C - actor.C;
            int candidateDr = direction.R - actor.R;
            int c = actor.C;
            int r = actor.R;

            for (int step = 1; step <= maxSteps; step++)
            {
                c += candidateDc;
                r += candidateDr;
                if (c == target.C && r == target.R)
                {
                    dc = candidateDc;
                    dr = candidateDr;
                    return true;
                }
            }
        }

        return false;
    }

    static void ResolveMoveThenDirectional(SkillCast cast, SkillSnapshotIndex index, List<HexCoord> selected, int radius)
    {
        if (selected.Count < 2)
        {
            return;
        }

        cast.DestinationHex = new HexCoord(selected[0].C, selected[0].R);
        cast.ImpactHex = new HexCoord(selected[1].C, selected[1].R);
        AddAffectedArea(cast, index, selected[1].C, selected[1].R, radius, cast.ActorUnitId);
    }

    static void ResolveTeleportTarget(SkillCast cast, SkillSnapshotIndex index, List<HexCoord> selected)
    {
        if (selected.Count < 2)
        {
            return;
        }

        BattleUnitSnapshot target = index.GetUnitAt(selected[0].C, selected[0].R);
        if (target != null)
        {
            cast.PrimaryTargetUnitId = target.RuntimeUnitId;
            cast.TargetUnitIds.Add(target.RuntimeUnitId);
        }

        cast.DestinationHex = new HexCoord(selected[1].C, selected[1].R);
    }

    static void ResolveAroundPostMoveCaster(SkillCast cast, SkillSnapshotIndex index, List<HexCoord> selected, int radius)
    {
        if (selected.Count == 0)
        {
            return;
        }

        cast.DestinationHex = new HexCoord(selected[0].C, selected[0].R);
        AddAffectedArea(cast, index, selected[0].C, selected[0].R, radius, cast.ActorUnitId);
    }

    static bool ResolveSpawnNearTarget(SkillCast cast, SkillSnapshotIndex index, BattleUnitSnapshot actor, List<HexCoord> selected)
    {
        if (selected.Count == 0)
        {
            return false;
        }

        BattleUnitSnapshot target = index.GetUnitAt(selected[0].C, selected[0].R);
        if (target == null || target.TeamIndex == actor.TeamIndex)
        {
            return false;
        }

        cast.PrimaryTargetUnitId = target.RuntimeUnitId;
        cast.TargetUnitIds.Add(target.RuntimeUnitId);
        cast.AffectedUnitIds.Add(target.RuntimeUnitId);

        List<HexCoord> neighbours = GetNeighbourCoordinates(target.C, target.R);
        neighbours.Sort(delegate(HexCoord left, HexCoord right)
        {
            int leftDistance = HexDistance(actor.C, actor.R, left.C, left.R);
            int rightDistance = HexDistance(actor.C, actor.R, right.C, right.R);
            int distanceCompare = leftDistance.CompareTo(rightDistance);
            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            int cCompare = left.C.CompareTo(right.C);
            return cCompare != 0 ? cCompare : left.R.CompareTo(right.R);
        });

        for (int i = 0; i < neighbours.Count; i++)
        {
            BattleHexSnapshot hex = index.GetHex(neighbours[i].C, neighbours[i].R);
            if (hex != null && hex.IsWalkable && string.IsNullOrEmpty(hex.OccupyingUnitId))
            {
                cast.DestinationHex = new HexCoord(hex.C, hex.R);
                return true;
            }
        }

        return false;
    }

    static void AddAffectedArea(SkillCast cast, SkillSnapshotIndex index, int c, int r, int radius, string excludedUnitId)
    {
        for (int i = 0; i < index.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = index.Hexes[i];
            if (hex == null || HexDistance(c, r, hex.C, hex.R) > radius)
            {
                continue;
            }

            cast.AffectedHexes.Add(new HexCoord(hex.C, hex.R));
            if (string.IsNullOrEmpty(hex.OccupyingUnitId) == false &&
                string.Equals(hex.OccupyingUnitId, excludedUnitId ?? string.Empty, StringComparison.Ordinal) == false)
            {
                cast.AffectedUnitIds.Add(hex.OccupyingUnitId);
            }
        }
    }

    static void AddPreviewEvent(SkillResult result, SkillCast cast, SkillEffect effect)
    {
        if (effect == null || effect.effectType == SkillEffectType.None)
        {
            return;
        }

        switch (effect.effectType)
        {
            case SkillEffectType.PlaceTrap:
                result.Add(new SkillResultEvent(SkillResultEventType.TrapPlaced, cast)
                {
                    TrapId = string.IsNullOrEmpty(effect.trapId) ? cast.SkillId : effect.trapId,
                    Hex = FirstHex(cast.SelectedHexes)
                });
                break;
            case SkillEffectType.Damage:
                AddTargetEvents(result, cast, effect, SkillResultEventType.DamageApplied);
                break;
            case SkillEffectType.ApplyStatus:
                AddTargetEvents(result, cast, effect, SkillResultEventType.StatusApplied);
                break;
            case SkillEffectType.MoveUnit:
                result.Add(new SkillResultEvent(SkillResultEventType.UnitMoved, cast)
                {
                    TargetUnitId = ResolveFirstTarget(cast, effect),
                    Hex = cast.DestinationHex
                });
                break;
            case SkillEffectType.ApplyHpCostOrSelfDamage:
                result.Add(new SkillResultEvent(SkillResultEventType.HpCostApplied, cast) { Amount = effect.hpCost });
                break;
            case SkillEffectType.ModifyStackAmount:
                result.Add(new SkillResultEvent(SkillResultEventType.StackAmountChanged, cast) { Amount = effect.stackAmountDelta });
                break;
            case SkillEffectType.SpawnUnit:
                result.Add(new SkillResultEvent(SkillResultEventType.UnitSpawned, cast) { Hex = cast.DestinationHex });
                break;
            case SkillEffectType.SetStanceMode:
            case SkillEffectType.ToggleStance:
                result.Add(new SkillResultEvent(SkillResultEventType.StanceChanged, cast));
                break;
        }
    }

    static void AddTargetEvents(SkillResult result, SkillCast cast, SkillEffect effect, SkillResultEventType eventType)
    {
        List<string> targetIds = ResolveTargetIds(cast, effect.targetSource);
        if (targetIds.Count == 0 && effect.skipIfNoTarget)
        {
            return;
        }

        if (targetIds.Count == 0)
        {
            targetIds.Add(string.Empty);
        }

        for (int i = 0; i < targetIds.Count; i++)
        {
            result.Add(new SkillResultEvent(eventType, cast)
            {
                TargetUnitId = targetIds[i],
                StatusId = effect.statusId,
                Amount = effect.fixedDamageValue
            });
        }
    }

    static List<string> ResolveTargetIds(SkillCast cast, SkillEffectTargetSource source)
    {
        List<string> result = new List<string>();
        if (cast == null)
        {
            return result;
        }

        switch (source)
        {
            case SkillEffectTargetSource.Actor:
                result.Add(cast.ActorUnitId);
                break;
            case SkillEffectTargetSource.PrimaryUnit:
                if (string.IsNullOrEmpty(cast.PrimaryTargetUnitId) == false)
                {
                    result.Add(cast.PrimaryTargetUnitId);
                }
                break;
            case SkillEffectTargetSource.SelectedUnits:
                result.AddRange(cast.TargetUnitIds ?? new List<string>());
                break;
            case SkillEffectTargetSource.AffectedUnits:
                result.AddRange(cast.AffectedUnitIds ?? new List<string>());
                break;
        }

        return result;
    }

    static string ResolveFirstTarget(SkillCast cast, SkillEffect effect)
    {
        List<string> targets = ResolveTargetIds(cast, effect.targetSource);
        return targets.Count > 0 ? targets[0] : cast.ActorUnitId;
    }

    static HexCoord FirstHex(List<HexCoord> hexes)
    {
        return hexes != null && hexes.Count > 0 ? hexes[0] : null;
    }

    static bool ContainsTarget(List<SkillTarget> targets, HexCoord selected)
    {
        if (selected == null)
        {
            return false;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null && targets[i].Hex != null && targets[i].Hex.C == selected.C && targets[i].Hex.R == selected.R)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDuplicateHex(List<HexCoord> selected)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            for (int j = i + 1; j < selected.Count; j++)
            {
                if (selected[i] != null && selected[j] != null && selected[i].C == selected[j].C && selected[i].R == selected[j].R)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static List<HexCoord> CopyHexes(IEnumerable<HexCoord> source)
    {
        List<HexCoord> result = new List<HexCoord>();
        if (source == null)
        {
            return result;
        }

        foreach (HexCoord hex in source)
        {
            if (hex != null)
            {
                result.Add(new HexCoord(hex.C, hex.R));
            }
        }

        return result;
    }

    static Dictionary<string, int> FindReachableHexCosts(BattleUnitSnapshot actor, SkillSnapshotIndex index)
    {
        Dictionary<string, int> reachable = new Dictionary<string, int>(StringComparer.Ordinal);
        Queue<ReachableNode> frontier = new Queue<ReachableNode>();
        frontier.Enqueue(new ReachableNode(actor.C, actor.R, 0));
        reachable[SkillSnapshotIndex.GetHexKey(actor.C, actor.R)] = 0;

        while (frontier.Count > 0)
        {
            ReachableNode current = frontier.Dequeue();
            if (current.Cost >= actor.MovementSpeed)
            {
                continue;
            }

            List<HexCoord> neighbours = GetNeighbourCoordinates(current.C, current.R);
            for (int i = 0; i < neighbours.Count; i++)
            {
                HexCoord neighbour = neighbours[i];
                BattleHexSnapshot hex = index.GetHex(neighbour.C, neighbour.R);
                if (hex == null || hex.IsWalkable == false)
                {
                    continue;
                }

                bool isActorSource = neighbour.C == actor.C && neighbour.R == actor.R;
                if (isActorSource == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
                {
                    continue;
                }

                int nextCost = current.Cost + 1;
                string key = SkillSnapshotIndex.GetHexKey(neighbour.C, neighbour.R);
                int knownCost;
                if (reachable.TryGetValue(key, out knownCost) && knownCost <= nextCost)
                {
                    continue;
                }

                reachable[key] = nextCost;
                frontier.Enqueue(new ReachableNode(neighbour.C, neighbour.R, nextCost));
            }
        }

        return reachable;
    }

    static int HexDistance(int c1, int r1, int c2, int r2)
    {
        int s1 = -(c1 + r1);
        int s2 = -(c2 + r2);
        return Math.Max(Math.Abs(c1 - c2), Math.Max(Math.Abs(r1 - r2), Math.Abs(s1 - s2)));
    }

    static List<HexCoord> GetNeighbourCoordinates(int c, int r)
    {
        return new List<HexCoord>
        {
            new HexCoord(c, r - 1),
            new HexCoord(c, r + 1),
            new HexCoord(c + 1, r - 1),
            new HexCoord(c - 1, r + 1),
            new HexCoord(c - 1, r),
            new HexCoord(c + 1, r)
        };
    }

    struct ReachableNode
    {
        public readonly int C;
        public readonly int R;
        public readonly int Cost;

        public ReachableNode(int c, int r, int cost)
        {
            C = c;
            R = r;
            Cost = cost;
        }
    }

    sealed class SkillSnapshotIndex
    {
        readonly Dictionary<string, BattleUnitSnapshot> unitsById;
        readonly Dictionary<string, BattleHexSnapshot> hexesByKey;
        readonly Dictionary<string, BattleUnitSnapshot> unitsByHex;

        SkillSnapshotIndex(BattleSnapshot snapshot)
        {
            MapWidth = snapshot == null ? 0 : snapshot.MapWidth;
            MapHeight = snapshot == null ? 0 : snapshot.MapHeight;
            Units = snapshot == null || snapshot.Units == null ? new List<BattleUnitSnapshot>() : snapshot.Units;
            Hexes = snapshot == null || snapshot.Hexes == null ? new List<BattleHexSnapshot>() : snapshot.Hexes;
            unitsById = new Dictionary<string, BattleUnitSnapshot>(StringComparer.Ordinal);
            hexesByKey = new Dictionary<string, BattleHexSnapshot>(StringComparer.Ordinal);
            unitsByHex = new Dictionary<string, BattleUnitSnapshot>(StringComparer.Ordinal);

            for (int i = 0; i < Units.Count; i++)
            {
                BattleUnitSnapshot unit = Units[i];
                if (unit == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(unit.RuntimeUnitId) == false && unitsById.ContainsKey(unit.RuntimeUnitId) == false)
                {
                    unitsById.Add(unit.RuntimeUnitId, unit);
                }

                string hexKey = GetHexKey(unit.C, unit.R);
                if (unitsByHex.ContainsKey(hexKey) == false)
                {
                    unitsByHex.Add(hexKey, unit);
                }
            }

            for (int i = 0; i < Hexes.Count; i++)
            {
                BattleHexSnapshot hex = Hexes[i];
                if (hex == null)
                {
                    continue;
                }

                string key = GetHexKey(hex.C, hex.R);
                if (hexesByKey.ContainsKey(key) == false)
                {
                    hexesByKey.Add(key, hex);
                }
            }
        }

        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        public List<BattleUnitSnapshot> Units { get; private set; }
        public List<BattleHexSnapshot> Hexes { get; private set; }

        public static SkillSnapshotIndex Build(BattleSnapshot snapshot)
        {
            return new SkillSnapshotIndex(snapshot);
        }

        public static string GetHexKey(int c, int r)
        {
            return c + "|" + r;
        }

        public BattleUnitSnapshot GetUnitOrNull(string runtimeUnitId)
        {
            BattleUnitSnapshot unit;
            unitsById.TryGetValue(runtimeUnitId ?? string.Empty, out unit);
            return unit;
        }

        public BattleUnitSnapshot GetUnitAt(int c, int r)
        {
            BattleUnitSnapshot unit;
            unitsByHex.TryGetValue(GetHexKey(c, r), out unit);
            return unit;
        }

        public BattleHexSnapshot GetHex(int c, int r)
        {
            return GetHexByKey(GetHexKey(c, r));
        }

        public BattleHexSnapshot GetHexByKey(string key)
        {
            BattleHexSnapshot hex;
            hexesByKey.TryGetValue(key ?? string.Empty, out hex);
            return hex;
        }
    }
}
