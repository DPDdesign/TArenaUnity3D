using System;
using System.Collections.Generic;

public static class BattleActionRules
{
    static readonly BattleActionKind[] GenerationOrder =
    {
        BattleActionKind.Skill,
        BattleActionKind.BasicRangedAttack,
        BattleActionKind.MoveAndAttack,
        BattleActionKind.Move,
        BattleActionKind.Wait,
        BattleActionKind.Defend
    };

    public static BattleActionValidationResult Validate(
        BattleActionUse use,
        BattleSnapshot snapshot,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        if (use == null)
        {
            return BattleActionValidationResult.Invalid("BattleActionUse was null.");
        }

        if (snapshot == null)
        {
            return BattleActionValidationResult.Invalid("Battle snapshot was unavailable.");
        }

        if (IsTurnStateBlockingAction(snapshot.TurnState))
        {
            return BattleActionValidationResult.Invalid("Battle turn state is currently blocking action execution.");
        }

        BattleUnitSnapshot actor = FindUnit(snapshot, use.ActorUnitId);
        if (actor == null)
        {
            return BattleActionValidationResult.Invalid("Actor does not exist in the snapshot.");
        }

        if (actor.IsAlive == false || actor.Amount <= 0)
        {
            return BattleActionValidationResult.Invalid("Actor is not alive/actionable.");
        }

        if (string.Equals(snapshot.ActiveUnitId, actor.RuntimeUnitId, StringComparison.Ordinal) == false)
        {
            return BattleActionValidationResult.Invalid("Actor is not the active unit.");
        }

        switch (use.ActionKind)
        {
            case BattleActionKind.Wait:
                return ValidateWait(use, snapshot, actor);
            case BattleActionKind.Defend:
                return ValidateDefend(use, snapshot, actor);
            case BattleActionKind.Move:
                return ValidateMove(use, snapshot, actor, skillMetadataProvider);
            case BattleActionKind.MoveAndAttack:
            case BattleActionKind.BasicMeleeAttack:
                return ValidateMoveAndAttack(use, snapshot, actor);
            case BattleActionKind.BasicRangedAttack:
                return ValidateBasicRangedAttack(use, snapshot, actor);
            case BattleActionKind.Skill:
            case BattleActionKind.Stance:
                return ValidateSkill(use, snapshot, actor, skillMetadataProvider);
            default:
                return BattleActionValidationResult.Invalid("Unsupported battle action kind: " + use.ActionKind);
        }
    }

    public static List<BattleAction> GenerateLegalActions(
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        List<BattleAction> actions = new List<BattleAction>();
        if (snapshot == null || string.IsNullOrEmpty(snapshot.ActiveUnitId))
        {
            return actions;
        }

        if (IsTurnStateBlockingAction(snapshot.TurnState))
        {
            return actions;
        }

        BattleUnitSnapshot actor = FindUnit(snapshot, snapshot.ActiveUnitId);
        if (actor == null || actor.IsAlive == false || actor.Amount <= 0)
        {
            return actions;
        }

        skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;
        TacticalAIResolvedProfile resolvedProfile = profile ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);

        Dictionary<BattleActionKind, List<BattleAction>> buckets = new Dictionary<BattleActionKind, List<BattleAction>>();
        buckets[BattleActionKind.Skill] = BuildSkillActions(snapshot, actor, resolvedProfile, skillMetadataProvider);
        buckets[BattleActionKind.BasicRangedAttack] = BuildBasicRangedAttackActions(snapshot, actor, resolvedProfile);
        buckets[BattleActionKind.MoveAndAttack] = BuildMoveAndAttackActions(snapshot, actor, resolvedProfile);
        buckets[BattleActionKind.Move] = BuildMoveActions(snapshot, actor, resolvedProfile, skillMetadataProvider);
        buckets[BattleActionKind.Wait] = BuildSingleAction(snapshot, actor, BattleActionKind.Wait, skillMetadataProvider);
        buckets[BattleActionKind.Defend] = BuildSingleAction(snapshot, actor, BattleActionKind.Defend, skillMetadataProvider);

        for (int i = 0; i < GenerationOrder.Length; i++)
        {
            List<BattleAction> bucket;
            if (buckets.TryGetValue(GenerationOrder[i], out bucket) && bucket != null)
            {
                bucket.Sort(CompareStableOrder);
                actions.AddRange(bucket);
            }
        }

        return actions;
    }

    public static BattleActionResult Apply(BattleSnapshot snapshot, BattleAction action)
    {
        BattleActionResult result = new BattleActionResult();
        if (action == null)
        {
            result.IsRejected = true;
            result.RejectReason = "BattleAction was null.";
            result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.ActionRejected, Message = result.RejectReason });
            return result;
        }

        result.ActorUnitId = action.ActorUnitId ?? string.Empty;
        result.ActionKind = action.ActionKind;
        result.ActionIndex = action.ActionIndex;

        switch (action.ActionKind)
        {
            case BattleActionKind.Move:
                result.Add(new BattleActionResultEvent
                {
                    EventType = BattleActionResultEventType.UnitMoved,
                    ActorUnitId = action.ActorUnitId,
                    Hex = BattleActionModelUtility.CopyHex(action.DestinationHex)
                });
                if (action.EndsTurn)
                {
                    result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.TurnCostApplied, ActorUnitId = action.ActorUnitId });
                }
                break;
            case BattleActionKind.MoveAndAttack:
            case BattleActionKind.BasicMeleeAttack:
                if (action.DestinationHex != null)
                {
                    result.Add(new BattleActionResultEvent
                    {
                        EventType = BattleActionResultEventType.UnitMoved,
                        ActorUnitId = action.ActorUnitId,
                        Hex = BattleActionModelUtility.CopyHex(action.DestinationHex)
                    });
                }
                AddBasicAttackDamageEvent(result, snapshot, action);
                AddCounterattackDamageEvent(result, snapshot, action);
                result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.TurnCostApplied, ActorUnitId = action.ActorUnitId });
                break;
            case BattleActionKind.BasicRangedAttack:
                AddBasicAttackDamageEvent(result, snapshot, action);
                result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.TurnCostApplied, ActorUnitId = action.ActorUnitId });
                break;
            case BattleActionKind.Wait:
                result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.WaitApplied, ActorUnitId = action.ActorUnitId });
                result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.TurnCostApplied, ActorUnitId = action.ActorUnitId });
                break;
            case BattleActionKind.Defend:
                result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.DefenseApplied, ActorUnitId = action.ActorUnitId, Amount = 5 });
                result.Add(new BattleActionResultEvent { EventType = BattleActionResultEventType.TurnCostApplied, ActorUnitId = action.ActorUnitId });
                break;
            case BattleActionKind.Skill:
            case BattleActionKind.Stance:
                AddSkillResultEvents(result, action);
                break;
        }

        return result;
    }

    static BattleActionValidationResult ValidateWait(BattleActionUse use, BattleSnapshot snapshot, BattleUnitSnapshot actor)
    {
        if (actor.MovedThisTurn || actor.UsedSkillThisTurn || actor.Waited)
        {
            return BattleActionValidationResult.Invalid("Wait is not legal after movement, skill use, or previous wait.");
        }

        return BattleActionValidationResult.Valid(CreateAction(use, actor, BattleActionKind.Wait, null, null, null));
    }

    static bool IsTurnStateBlockingAction(BattleTurnStateSnapshot turnState)
    {
        return turnState != null && (turnState.IsActionBlocking || turnState.IsResolvingNewTurnSequence);
    }

    static BattleActionValidationResult ValidateDefend(BattleActionUse use, BattleSnapshot snapshot, BattleUnitSnapshot actor)
    {
        if (actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return BattleActionValidationResult.Invalid("Defend is not legal after movement or skill use.");
        }

        return BattleActionValidationResult.Valid(CreateAction(use, actor, BattleActionKind.Defend, null, null, null));
    }

    static BattleActionValidationResult ValidateMove(
        BattleActionUse use,
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        if (CanStartMovement(actor) == false)
        {
            return BattleActionValidationResult.Invalid("Move is not legal for the actor state.");
        }

        HexCoord destination = FirstHex(use.SelectedHexes);
        BattleHexSnapshot destinationHex = destination != null ? FindHex(snapshot, destination.C, destination.R) : null;
        string reason;
        if (IsLegalDestination(snapshot, actor, destinationHex, out reason) == false)
        {
            return BattleActionValidationResult.Invalid(reason);
        }

        Dictionary<string, int> reachable = BattleHexGridUtility.FindReachableHexCosts(snapshot, actor);
        if (reachable.ContainsKey(BattleHexGridUtility.GetHexKey(destinationHex.C, destinationHex.R)) == false)
        {
            return BattleActionValidationResult.Invalid("Move destination is outside actor movement budget.");
        }

        BattleAction action = CreateAction(use, actor, BattleActionKind.Move, destinationHex, null, null);
        action.EndsTurn = actor.Waited || HasAvailableSkillAfterMove(actor, snapshot, skillMetadataProvider) == false;
        action.AllowsPostMoveFollowUp = action.EndsTurn == false;
        return BattleActionValidationResult.Valid(action);
    }

    static BattleActionValidationResult ValidateMoveAndAttack(BattleActionUse use, BattleSnapshot snapshot, BattleUnitSnapshot actor)
    {
        if (CanStartMovement(actor) == false)
        {
            return BattleActionValidationResult.Invalid("Move-and-attack is not legal for the actor state.");
        }

        if (actor.IsRange)
        {
            return BattleActionValidationResult.Invalid("Move-and-attack requires a melee actor.");
        }

        BattleUnitSnapshot target = ResolveTarget(snapshot, actor, use.TargetUnitId, use.SelectedHexes);
        if (target == null)
        {
            return BattleActionValidationResult.Invalid("Move-and-attack target is missing or invalid.");
        }

        HexCoord destination = FirstHex(use.SelectedHexes);
        BattleHexSnapshot destinationHex = destination != null ? FindHex(snapshot, destination.C, destination.R) : FindHex(snapshot, actor.C, actor.R);
        string reason;
        if (IsLegalDestination(snapshot, actor, destinationHex, out reason) == false)
        {
            return BattleActionValidationResult.Invalid(reason);
        }

        Dictionary<string, int> reachable = BattleHexGridUtility.FindReachableHexCosts(snapshot, actor);
        if (reachable.ContainsKey(BattleHexGridUtility.GetHexKey(destinationHex.C, destinationHex.R)) == false)
        {
            return BattleActionValidationResult.Invalid("Move-and-attack destination is outside actor movement budget.");
        }

        if (BattleHexGridUtility.AreAdjacent(snapshot, destinationHex.C, destinationHex.R, target.C, target.R) == false)
        {
            return BattleActionValidationResult.Invalid("Move-and-attack destination is not adjacent to target.");
        }

        BattleAction action = CreateAction(use, actor, BattleActionKind.MoveAndAttack, destinationHex, FindHex(snapshot, target.C, target.R), target);
        return BattleActionValidationResult.Valid(action);
    }

    static BattleActionValidationResult ValidateBasicRangedAttack(BattleActionUse use, BattleSnapshot snapshot, BattleUnitSnapshot actor)
    {
        if (actor.IsRange == false)
        {
            return BattleActionValidationResult.Invalid("Basic ranged attack requires a ranged actor.");
        }

        if (actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return BattleActionValidationResult.Invalid("Basic ranged attack is not legal after movement or skill use.");
        }

        BattleUnitSnapshot target = ResolveTarget(snapshot, actor, use.TargetUnitId, use.SelectedHexes);
        if (target == null)
        {
            return BattleActionValidationResult.Invalid("Basic ranged attack target is missing or invalid.");
        }

        return BattleActionValidationResult.Valid(CreateAction(use, actor, BattleActionKind.BasicRangedAttack, null, FindHex(snapshot, target.C, target.R), target));
    }

    static BattleActionValidationResult ValidateSkill(
        BattleActionUse use,
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        SkillDefinitionSpec spec = ResolveSkillSpec(use.SkillId, skillMetadataProvider);
        if (spec == null)
        {
            return BattleActionValidationResult.Invalid("Skill action has no skill definition spec.");
        }

        int skillSlot = ResolveSkillSlot(actor, use.SkillId, use.SkillSlot);
        if (skillSlot < 0)
        {
            return BattleActionValidationResult.Invalid("Skill action slot/id pair is invalid.");
        }

        if (actor.CooldownsBySlot == null || skillSlot >= actor.CooldownsBySlot.Count || actor.CooldownsBySlot[skillSlot] > 0)
        {
            return BattleActionValidationResult.Invalid("Skill action is on cooldown.");
        }

        if (spec.ActivationRule.activationKind == SkillActivationKind.Passive)
        {
            return BattleActionValidationResult.Invalid("Passive skills are not legal tactical actions.");
        }

        SkillContext context = SkillContext.Create(snapshot, actor.RuntimeUnitId, spec, skillSlot, use.ActionSeed);
        SkillValidationResult skillValidation = SkillRules.Validate(
            new SkillUse(actor.RuntimeUnitId, use.SkillId, use.SelectedHexes),
            context);
        if (skillValidation.IsValid == false || skillValidation.Cast == null)
        {
            return BattleActionValidationResult.Invalid(FormatSkillRejectReason(skillValidation));
        }

        SkillCast cast = skillValidation.Cast.Clone();
        BattleActionKind actionKind = spec.ActivationRule.activationKind == SkillActivationKind.Stance
            ? BattleActionKind.Stance
            : BattleActionKind.Skill;
        BattleAction action = CreateAction(use, actor, actionKind, null, null, null);
        action.SkillSlot = skillSlot;
        action.SkillId = cast.SkillId ?? string.Empty;
        action.SkillCast = cast;
        action.SelectedHexes = BattleActionModelUtility.CopyHexes(cast.SelectedHexes);
        action.DestinationHex = BattleActionModelUtility.CopyHex(cast.DestinationHex);
        action.ImpactHex = BattleActionModelUtility.CopyHex(cast.ImpactHex);
        action.PrimaryTargetUnitId = cast.PrimaryTargetUnitId ?? string.Empty;
        action.TargetUnitIds = new List<string>(cast.TargetUnitIds ?? new List<string>());
        action.AffectedUnitIds = new List<string>(cast.AffectedUnitIds ?? new List<string>());
        action.AffectedHexes = BattleActionModelUtility.CopyHexes(cast.AffectedHexes);
        action.EndsTurn = cast.ConsumesTurn;
        action.TurnCost = cast.ConsumesTurn ? 1 : 0;
        action.AllowsPostMoveFollowUp = cast.CanMoveAfterUse;
        return BattleActionValidationResult.Valid(action);
    }

    static string FormatSkillRejectReason(SkillValidationResult validation)
    {
        if (validation == null)
        {
            return "Skill validation failed.";
        }

        if (string.IsNullOrEmpty(validation.Message) == false)
        {
            return validation.Message;
        }

        return validation.RejectReason.ToString();
    }

    static List<BattleAction> BuildSingleAction(
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        BattleActionKind actionKind,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        BattleActionValidationResult validation = Validate(
            new BattleActionUse { ActorUnitId = actor.RuntimeUnitId, ActionKind = actionKind },
            snapshot,
            skillMetadataProvider);
        return validation.IsValid && validation.Action != null
            ? new List<BattleAction> { validation.Action }
            : new List<BattleAction>();
    }

    static List<BattleAction> BuildMoveActions(
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        List<BattleAction> actions = new List<BattleAction>();
        if (CanStartMovement(actor) == false)
        {
            return actions;
        }

        Dictionary<string, int> reachable = BattleHexGridUtility.FindReachableHexCosts(snapshot, actor);
        List<MoveScore> scored = new List<MoveScore>();
        foreach (KeyValuePair<string, int> pair in reachable)
        {
            BattleHexSnapshot destination = FindHexByKey(snapshot, pair.Key);
            if (destination == null || destination.C == actor.C && destination.R == actor.R)
            {
                continue;
            }

            scored.Add(new MoveScore
            {
                Hex = destination,
                Steps = pair.Value,
                NearestEnemyDistance = FindNearestEnemyDistance(snapshot, destination.C, destination.R, actor.TeamIndex)
            });
        }

        scored.Sort(CompareMoveScores);
        Trim(scored, profile != null ? profile.MaxMoveCandidates : 16);
        for (int i = 0; i < scored.Count; i++)
        {
            BattleActionValidationResult validation = Validate(
                new BattleActionUse
                {
                    ActorUnitId = actor.RuntimeUnitId,
                    ActionKind = BattleActionKind.Move,
                    SelectedHexes = new List<HexCoord> { new HexCoord(scored[i].Hex.C, scored[i].Hex.R) }
                },
                snapshot,
                skillMetadataProvider);
            if (validation.IsValid && validation.Action != null)
            {
                actions.Add(validation.Action);
            }
        }

        return actions;
    }

    static List<BattleAction> BuildMoveAndAttackActions(BattleSnapshot snapshot, BattleUnitSnapshot actor, TacticalAIResolvedProfile profile)
    {
        List<BattleAction> actions = new List<BattleAction>();
        if (CanStartMovement(actor) == false || actor.IsRange)
        {
            return actions;
        }

        Dictionary<string, int> reachable = BattleHexGridUtility.FindReachableHexCosts(snapshot, actor);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot enemy = snapshot.Units[i];
            if (enemy == null || enemy.IsAlive == false || enemy.TeamIndex == actor.TeamIndex)
            {
                continue;
            }

            List<HexCoord> positions = GetAttackPositions(snapshot, actor, enemy, reachable);
            for (int positionIndex = 0; positionIndex < positions.Count; positionIndex++)
            {
                HexCoord destination = positions[positionIndex];
                string key = enemy.RuntimeUnitId + "|" + destination.C + "|" + destination.R;
                if (seen.Add(key) == false)
                {
                    continue;
                }

                BattleActionValidationResult validation = Validate(
                    new BattleActionUse
                    {
                        ActorUnitId = actor.RuntimeUnitId,
                        ActionKind = BattleActionKind.MoveAndAttack,
                        TargetUnitId = enemy.RuntimeUnitId,
                        SelectedHexes = new List<HexCoord> { destination, new HexCoord(enemy.C, enemy.R) }
                    },
                    snapshot);
                if (validation.IsValid && validation.Action != null)
                {
                    actions.Add(validation.Action);
                }
            }
        }

        actions.Sort(CompareAttackActions);
        Trim(actions, profile != null ? profile.MaxAttackCandidates : 16);
        return actions;
    }

    static List<BattleAction> BuildBasicRangedAttackActions(BattleSnapshot snapshot, BattleUnitSnapshot actor, TacticalAIResolvedProfile profile)
    {
        List<BattleAction> actions = new List<BattleAction>();
        if (actor.IsRange == false || actor.MovedThisTurn || actor.UsedSkillThisTurn)
        {
            return actions;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot enemy = snapshot.Units[i];
            if (enemy == null || enemy.IsAlive == false || enemy.TeamIndex == actor.TeamIndex)
            {
                continue;
            }

            BattleActionValidationResult validation = Validate(
                new BattleActionUse
                {
                    ActorUnitId = actor.RuntimeUnitId,
                    ActionKind = BattleActionKind.BasicRangedAttack,
                    TargetUnitId = enemy.RuntimeUnitId,
                    SelectedHexes = new List<HexCoord> { new HexCoord(enemy.C, enemy.R) }
                },
                snapshot);
            if (validation.IsValid && validation.Action != null)
            {
                actions.Add(validation.Action);
            }
        }

        actions.Sort(CompareAttackActions);
        Trim(actions, profile != null ? profile.MaxAttackCandidates : 16);
        return actions;
    }

    static List<BattleAction> BuildSkillActions(
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        List<BattleAction> actions = new List<BattleAction>();
        if (actor.SkillIdsBySlot == null || actor.CooldownsBySlot == null || actor.Waited)
        {
            return actions;
        }

        int slotCount = Math.Min(actor.SkillIdsBySlot.Count, actor.CooldownsBySlot.Count);
        int targetLimit = Math.Max(1, profile != null ? profile.MaxSkillCandidates : 16);
        for (int slot = 0; slot < slotCount; slot++)
        {
            string skillId = actor.SkillIdsBySlot[slot];
            if (string.IsNullOrEmpty(skillId) || actor.CooldownsBySlot[slot] > 0)
            {
                continue;
            }

            SkillDefinitionSpec spec = ResolveSkillSpec(skillId, skillMetadataProvider);
            if (spec == null)
            {
                continue;
            }

            if (spec.ActivationRule.activationKind == SkillActivationKind.Passive)
            {
                continue;
            }

            SkillContext context = SkillContext.Create(snapshot, actor.RuntimeUnitId, spec, slot);
            if (SkillRules.CanUse(context).IsValid == false)
            {
                continue;
            }

            AddValidatedSkillActions(actions, snapshot, actor, context, skillId, slot, new List<HexCoord>(), targetLimit, skillMetadataProvider);
            if (actions.Count >= targetLimit)
            {
                break;
            }
        }

        actions.Sort(CompareStableOrder);
        Trim(actions, profile != null ? profile.MaxSkillCandidates : 16);
        return actions;
    }

    static void AddValidatedSkillActions(
        List<BattleAction> actions,
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        SkillContext context,
        string skillId,
        int skillSlot,
        List<HexCoord> selected,
        int limit,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        if (actions.Count >= limit)
        {
            return;
        }

        SkillValidationResult validation = SkillRules.Validate(new SkillUse(actor.RuntimeUnitId, skillId, selected), context);
        if (validation.IsValid && validation.Cast != null)
        {
            if (ShouldGenerateSkillAction(validation.Cast) == false)
            {
                return;
            }

            BattleActionValidationResult actionValidation = Validate(
                new BattleActionUse
                {
                    ActorUnitId = actor.RuntimeUnitId,
                    ActionKind = BattleActionKind.Skill,
                    SkillSlot = skillSlot,
                    SkillId = skillId,
                    SelectedHexes = selected
                },
                snapshot,
                skillMetadataProvider);
            if (actionValidation.IsValid && actionValidation.Action != null)
            {
                AddUniqueAction(actions, actionValidation.Action);
            }

            return;
        }

        List<SkillTarget> targets = SkillRules.GetTargets(context, selected);
        if (targets == null || targets.Count == 0)
        {
            return;
        }

        for (int i = 0; i < targets.Count && actions.Count < limit; i++)
        {
            SkillTarget target = targets[i];
            if (target == null || target.Hex == null)
            {
                continue;
            }

            List<HexCoord> next = BattleActionModelUtility.CopyHexes(selected);
            next.Add(new HexCoord(target.Hex.C, target.Hex.R));
            AddValidatedSkillActions(actions, snapshot, actor, context, skillId, skillSlot, next, limit, skillMetadataProvider);
        }
    }

    static bool ShouldGenerateSkillAction(SkillCast cast)
    {
        if (cast == null)
        {
            return false;
        }

        if (HasDamageEffect(cast) == false)
        {
            return true;
        }

        SkillResult preview = SkillRules.Preview(cast, null);
        if (preview == null || preview.Events == null)
        {
            return false;
        }

        for (int i = 0; i < preview.Events.Count; i++)
        {
            SkillResultEvent resultEvent = preview.Events[i];
            if (resultEvent != null &&
                resultEvent.EventType == SkillResultEventType.DamageApplied &&
                string.IsNullOrEmpty(resultEvent.TargetUnitId) == false)
            {
                return true;
            }
        }

        return false;
    }

    static void AddUniqueAction(List<BattleAction> actions, BattleAction action)
    {
        if (actions == null || action == null)
        {
            return;
        }

        string key = action.StableOrderKey ?? string.Empty;
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != null && string.Equals(actions[i].StableOrderKey ?? string.Empty, key, StringComparison.Ordinal))
            {
                return;
            }
        }

        actions.Add(action);
    }

    static bool HasDamageEffect(SkillCast cast)
    {
        if (cast == null || cast.Effects == null)
        {
            return false;
        }

        for (int i = 0; i < cast.Effects.Length; i++)
        {
            SkillEffect effect = cast.Effects[i];
            if (effect != null && effect.effectType == SkillEffectType.Damage)
            {
                return true;
            }
        }

        return false;
    }

    static BattleAction CreateAction(
        BattleActionUse use,
        BattleUnitSnapshot actor,
        BattleActionKind kind,
        BattleHexSnapshot destination,
        BattleHexSnapshot impact,
        BattleUnitSnapshot target)
    {
        BattleAction action = new BattleAction
        {
            ActorUnitId = actor.RuntimeUnitId,
            ActionKind = kind,
            SelectedHexes = BattleActionModelUtility.CopyHexes(use.SelectedHexes),
            DestinationHex = destination != null ? new HexCoord(destination.C, destination.R) : null,
            ImpactHex = impact != null ? new HexCoord(impact.C, impact.R) : null,
            PrimaryTargetUnitId = target != null ? target.RuntimeUnitId : string.Empty,
            ActionIndex = use.ActionIndex,
            ActionSeed = use.ActionSeed,
            StableOrderKey = BuildStableOrderKey(kind, actor.RuntimeUnitId, use.SkillSlot, use.SkillId, destination, impact, target)
        };

        if (target != null)
        {
            action.TargetUnitIds.Add(target.RuntimeUnitId);
            action.AffectedUnitIds.Add(target.RuntimeUnitId);
        }

        if (impact != null)
        {
            action.AffectedHexes.Add(new HexCoord(impact.C, impact.R));
        }

        return action;
    }

    static void AddSkillResultEvents(BattleActionResult result, BattleAction action)
    {
        if (action.SkillCast == null)
        {
            return;
        }

        SkillResult skillResult = SkillRules.Preview(action.SkillCast, null);
        if (skillResult == null || skillResult.Events == null)
        {
            return;
        }

        for (int i = 0; i < skillResult.Events.Count; i++)
        {
            SkillResultEvent skillEvent = skillResult.Events[i];
            if (skillEvent == null)
            {
                continue;
            }

            result.Add(new BattleActionResultEvent
            {
                EventType = ConvertSkillEventType(skillEvent.EventType),
                ActorUnitId = skillEvent.ActorUnitId,
                TargetUnitId = skillEvent.TargetUnitId,
                Hex = BattleActionModelUtility.CopyHex(skillEvent.Hex),
                StatusId = skillEvent.StatusId ?? string.Empty,
                TrapId = skillEvent.TrapId ?? string.Empty,
                Amount = skillEvent.Amount
            });
        }
    }

    static BattleActionResultEventType ConvertSkillEventType(SkillResultEventType eventType)
    {
        switch (eventType)
        {
            case SkillResultEventType.UnitMoved:
                return BattleActionResultEventType.UnitMoved;
            case SkillResultEventType.DamageApplied:
                return BattleActionResultEventType.DamageApplied;
            case SkillResultEventType.StatusApplied:
                return BattleActionResultEventType.StatusApplied;
            case SkillResultEventType.TrapPlaced:
                return BattleActionResultEventType.TrapPlaced;
            case SkillResultEventType.TrapTriggered:
                return BattleActionResultEventType.TrapTriggered;
            case SkillResultEventType.UnitSpawned:
                return BattleActionResultEventType.UnitSpawned;
            case SkillResultEventType.StackAmountChanged:
                return BattleActionResultEventType.StackAmountChanged;
            case SkillResultEventType.HpCostApplied:
                return BattleActionResultEventType.HpCostApplied;
            case SkillResultEventType.CooldownApplied:
                return BattleActionResultEventType.CooldownApplied;
            case SkillResultEventType.TurnCostApplied:
                return BattleActionResultEventType.TurnCostApplied;
            case SkillResultEventType.StanceChanged:
                return BattleActionResultEventType.StanceChanged;
            default:
                return BattleActionResultEventType.None;
        }
    }

    static SkillDefinitionSpec ResolveSkillSpec(string skillId, ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        ITacticalAISkillSpecProvider specProvider = skillMetadataProvider as ITacticalAISkillSpecProvider;
        SkillDefinitionSpec spec;
        if (specProvider != null && specProvider.TryGetSkillSpec(skillId, out spec))
        {
            return spec;
        }

        SkillDefinitionAsset asset = DataMapper.Instance != null ? DataMapper.Instance.FindSkillAsset(skillId) : null;
        return SkillDefinitionSpec.FromAsset(asset);
    }

    static int ResolveSkillSlot(BattleUnitSnapshot actor, string skillId, int requestedSlot)
    {
        if (actor == null || actor.SkillIdsBySlot == null)
        {
            return -1;
        }

        if (requestedSlot >= 0 &&
            requestedSlot < actor.SkillIdsBySlot.Count &&
            string.Equals(actor.SkillIdsBySlot[requestedSlot], skillId ?? string.Empty, StringComparison.Ordinal))
        {
            return requestedSlot;
        }

        for (int i = 0; i < actor.SkillIdsBySlot.Count; i++)
        {
            if (string.Equals(actor.SkillIdsBySlot[i], skillId ?? string.Empty, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    static bool CanStartMovement(BattleUnitSnapshot actor)
    {
        return actor != null &&
            actor.IsAlive &&
            actor.MovedThisTurn == false &&
            (actor.UsedSkillThisTurn == false || actor.CanMoveAfterSkillThisTurn);
    }

    static bool HasAvailableSkillAfterMove(
        BattleUnitSnapshot actor,
        BattleSnapshot snapshot,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        if (actor == null || actor.SkillIdsBySlot == null || actor.CooldownsBySlot == null)
        {
            return false;
        }

        int slotCount = Math.Min(actor.SkillIdsBySlot.Count, actor.CooldownsBySlot.Count);
        for (int i = 0; i < slotCount; i++)
        {
            if (actor.CooldownsBySlot[i] > 0)
            {
                continue;
            }

            string skillId = actor.SkillIdsBySlot[i];
            TacticalAISkillMetadata metadata;
            if (skillMetadataProvider != null &&
                skillMetadataProvider.TryGetSkillMetadata(skillId, out metadata) &&
                metadata != null &&
                (metadata.IsPassive || metadata.IsRepeatableToggle))
            {
                continue;
            }

            SkillDefinitionSpec spec = ResolveSkillSpec(skillId, skillMetadataProvider);
            if (spec == null)
            {
                spec = ResolveSkillSpec(skillId, TacticalAIDataMapperSkillMetadataProvider.Instance);
            }

            if (spec != null &&
                spec.ActivationRule.activationKind != SkillActivationKind.Passive &&
                spec.ActivationRule.repeatableInTurn == false &&
                spec.ActivationRule.canUseAfterMove)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsLegalDestination(BattleSnapshot snapshot, BattleUnitSnapshot actor, BattleHexSnapshot destination, out string reason)
    {
        reason = string.Empty;
        if (destination == null || destination.IsWalkable == false)
        {
            reason = "Destination does not exist or is not walkable.";
            return false;
        }

        bool isActorSource = destination.C == actor.C && destination.R == actor.R;
        if (isActorSource == false && string.IsNullOrEmpty(destination.OccupyingUnitId) == false)
        {
            reason = "Destination is occupied.";
            return false;
        }

        return true;
    }

    static BattleUnitSnapshot ResolveTarget(BattleSnapshot snapshot, BattleUnitSnapshot actor, string targetUnitId, List<HexCoord> selectedHexes)
    {
        BattleUnitSnapshot target = string.IsNullOrEmpty(targetUnitId) ? null : FindUnit(snapshot, targetUnitId);
        if (target == null)
        {
            HexCoord targetHex = selectedHexes != null && selectedHexes.Count > 1 ? selectedHexes[1] : FirstHex(selectedHexes);
            if (targetHex != null)
            {
                target = FindUnitAt(snapshot, targetHex.C, targetHex.R);
            }
        }

        if (target == null || target.IsAlive == false || target.Amount <= 0 || target.TeamIndex == actor.TeamIndex)
        {
            return null;
        }

        return target;
    }

    static List<HexCoord> GetAttackPositions(
        BattleSnapshot snapshot,
        BattleUnitSnapshot actor,
        BattleUnitSnapshot enemy,
        Dictionary<string, int> reachable)
    {
        List<HexCoord> positions = new List<HexCoord>();
        if (BattleHexGridUtility.AreAdjacent(snapshot, actor.C, actor.R, enemy.C, enemy.R))
        {
            positions.Add(new HexCoord(actor.C, actor.R));
        }

        List<HexCoord> neighbours = BattleHexGridUtility.GetNeighbourCoordinates(snapshot, enemy.C, enemy.R);
        for (int i = 0; i < neighbours.Count; i++)
        {
            HexCoord candidate = neighbours[i];
            BattleHexSnapshot hex = FindHex(snapshot, candidate.C, candidate.R);
            if (hex == null || hex.IsWalkable == false)
            {
                continue;
            }

            bool isActorSource = candidate.C == actor.C && candidate.R == actor.R;
            if (isActorSource == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false)
            {
                continue;
            }

            if (reachable.ContainsKey(BattleHexGridUtility.GetHexKey(candidate.C, candidate.R)))
            {
                positions.Add(candidate);
            }
        }

        positions.Sort(CompareHexes);
        return positions;
    }

    static void AddBasicAttackDamageEvent(BattleActionResult result, BattleSnapshot snapshot, BattleAction action)
    {
        result.Add(new BattleActionResultEvent
        {
            EventType = BattleActionResultEventType.DamageApplied,
            ActorUnitId = action.ActorUnitId,
            TargetUnitId = action.PrimaryTargetUnitId,
            Amount = ResolveBasicAttackDamage(snapshot, action.ActorUnitId, action.PrimaryTargetUnitId, action.ActionIndex)
        });
    }

    static void AddCounterattackDamageEvent(BattleActionResult result, BattleSnapshot snapshot, BattleAction action)
    {
        BattleUnitSnapshot defender = FindUnit(snapshot, action.PrimaryTargetUnitId);
        BattleUnitSnapshot attacker = FindUnit(snapshot, action.ActorUnitId);
        if (defender == null ||
            attacker == null ||
            defender.CounterAttackAvailable == false ||
            defender.TempCounterAttacks < 1)
        {
            return;
        }

        result.Add(new BattleActionResultEvent
        {
            EventType = BattleActionResultEventType.DamageApplied,
            ActorUnitId = defender.RuntimeUnitId,
            TargetUnitId = attacker.RuntimeUnitId,
            Amount = ResolveBasicAttackDamage(snapshot, defender.RuntimeUnitId, attacker.RuntimeUnitId, action.ActionIndex)
        });
    }

    static int ResolveBasicAttackDamage(BattleSnapshot snapshot, string actorUnitId, string targetUnitId, int actionIndex)
    {
        BattleUnitSnapshot actor = FindUnit(snapshot, actorUnitId);
        if (actor == null)
        {
            return 0;
        }

        int min = Math.Min(actor.MinDamage, actor.MaxDamage);
        int max = Math.Max(actor.MinDamage, actor.MaxDamage);
        int spread = Math.Max(0, max - min);
        if (spread == 0)
        {
            return Math.Max(0, min);
        }

        int seed = snapshot != null ? snapshot.GameSeed : 0;
        unchecked
        {
            seed = seed * 397 ^ actionIndex;
            seed = seed * 397 ^ StableStringHash(actorUnitId);
            seed = seed * 397 ^ StableStringHash(targetUnitId);
        }

        return Math.Max(0, min + Math.Abs(seed) % (spread + 1));
    }

    static int StableStringHash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
            {
                hash = hash * 31 + value[i];
            }

            return hash;
        }
    }

    static void Trim<T>(List<T> list, int maxCount)
    {
        if (list == null || maxCount < 0 || list.Count <= maxCount)
        {
            return;
        }

        if (maxCount == 0)
        {
            list.Clear();
            return;
        }

        list.RemoveRange(maxCount, list.Count - maxCount);
    }

    static int CompareMoveScores(MoveScore left, MoveScore right)
    {
        int enemyCompare = left.NearestEnemyDistance.CompareTo(right.NearestEnemyDistance);
        if (enemyCompare != 0)
        {
            return enemyCompare;
        }

        int stepCompare = left.Steps.CompareTo(right.Steps);
        if (stepCompare != 0)
        {
            return stepCompare;
        }

        return CompareHexes(new HexCoord(left.Hex.C, left.Hex.R), new HexCoord(right.Hex.C, right.Hex.R));
    }

    static int CompareAttackActions(BattleAction left, BattleAction right)
    {
        int targetCompare = string.CompareOrdinal(left != null ? left.PrimaryTargetUnitId : string.Empty, right != null ? right.PrimaryTargetUnitId : string.Empty);
        if (targetCompare != 0)
        {
            return targetCompare;
        }

        return CompareStableOrder(left, right);
    }

    static int CompareStableOrder(BattleAction left, BattleAction right)
    {
        return string.CompareOrdinal(left != null ? left.StableOrderKey : string.Empty, right != null ? right.StableOrderKey : string.Empty);
    }

    static int CompareHexes(HexCoord left, HexCoord right)
    {
        int cCompare = left.C.CompareTo(right.C);
        if (cCompare != 0)
        {
            return cCompare;
        }

        return left.R.CompareTo(right.R);
    }

    static int FindNearestEnemyDistance(BattleSnapshot snapshot, int c, int r, int actorTeamIndex)
    {
        int nearest = int.MaxValue;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null || unit.IsAlive == false || unit.TeamIndex == actorTeamIndex)
            {
                continue;
            }

            int distance = BattleHexGridUtility.HexDistance(snapshot, c, r, unit.C, unit.R);
            if (distance < nearest)
            {
                nearest = distance;
            }
        }

        return nearest == int.MaxValue ? int.MaxValue / 2 : nearest;
    }

    static BattleUnitSnapshot FindUnit(BattleSnapshot snapshot, string runtimeUnitId)
    {
        if (snapshot == null || snapshot.Units == null || string.IsNullOrEmpty(runtimeUnitId))
        {
            return null;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && string.Equals(unit.RuntimeUnitId, runtimeUnitId, StringComparison.Ordinal))
            {
                return unit;
            }
        }

        return null;
    }

    static BattleUnitSnapshot FindUnitAt(BattleSnapshot snapshot, int c, int r)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return null;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && unit.C == c && unit.R == r)
            {
                return unit;
            }
        }

        return null;
    }

    static BattleHexSnapshot FindHex(BattleSnapshot snapshot, int c, int r)
    {
        if (snapshot == null || snapshot.Hexes == null)
        {
            return null;
        }

        for (int i = 0; i < snapshot.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = snapshot.Hexes[i];
            if (hex != null && hex.C == c && hex.R == r)
            {
                return hex;
            }
        }

        return null;
    }

    static BattleHexSnapshot FindHexByKey(BattleSnapshot snapshot, string key)
    {
        if (snapshot == null || snapshot.Hexes == null)
        {
            return null;
        }

        for (int i = 0; i < snapshot.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = snapshot.Hexes[i];
            if (hex != null && string.Equals(BattleHexGridUtility.GetHexKey(hex.C, hex.R), key, StringComparison.Ordinal))
            {
                return hex;
            }
        }

        return null;
    }

    static HexCoord FirstHex(List<HexCoord> hexes)
    {
        return hexes != null && hexes.Count > 0 ? hexes[0] : null;
    }

    static string BuildStableOrderKey(
        BattleActionKind kind,
        string actorUnitId,
        int skillSlot,
        string skillId,
        BattleHexSnapshot destination,
        BattleHexSnapshot impact,
        BattleUnitSnapshot target)
    {
        return kind + "|" +
            (actorUnitId ?? string.Empty) + "|" +
            skillSlot + "|" +
            (skillId ?? string.Empty) + "|" +
            (target != null ? target.RuntimeUnitId : string.Empty) + "|" +
            (destination != null ? destination.C.ToString() : string.Empty) + "|" +
            (destination != null ? destination.R.ToString() : string.Empty) + "|" +
            (impact != null ? impact.C.ToString() : string.Empty) + "|" +
            (impact != null ? impact.R.ToString() : string.Empty);
    }

    struct MoveScore
    {
        public BattleHexSnapshot Hex;
        public int Steps;
        public int NearestEnemyDistance;
    }
}
