using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TacticalAISkillRulesExecutor : ITacticalAISkillActionExecutor
{
    static TacticalAISkillRulesExecutor instance;

    public static TacticalAISkillRulesExecutor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TacticalAISkillRulesExecutor();
            }

            return instance;
        }
    }

    public bool TryExecuteSkillAction(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason)
    {
        failureReason = string.Empty;
        if (runtimeContext == null || runtimeContext.HasRequiredReferences == false)
        {
            failureReason = "Tactical AI skill rules executor is missing live battle references.";
            return false;
        }

        if (revalidatedIntent == null || revalidatedIntent.ValidatedSkillCast == null)
        {
            failureReason = "Tactical AI skill had no live validated SkillCast.";
            return false;
        }

        TosterHexUnit actor = ResolveLiveUnit(runtimeContext, revalidatedIntent.Actor);
        if (actor == null)
        {
            failureReason = "Could not resolve live actor for shared skill execution.";
            return false;
        }

        SkillDefinitionAsset definition = DataMapper.Instance != null
            ? DataMapper.Instance.FindSkillAsset(revalidatedIntent.ValidatedSkillCast.SkillId)
            : null;
        if (definition == null)
        {
            failureReason = "Could not resolve SkillDefinitionAsset for shared skill execution.";
            return false;
        }

        BattleSnapshot liveSnapshot = BattleSnapshotLiveAdapter.BuildSnapshot(
            runtimeContext.HexMap,
            runtimeContext.MouseControler,
            runtimeContext.TurnManager,
            runtimeContext.BattleActionLifecycle);
        SkillContext context = SkillContext.Create(
            liveSnapshot,
            revalidatedIntent.ValidatedSkillCast.ActorUnitId,
            definition,
            revalidatedIntent.SkillSlot);
        TacticalAISkillRuntime runtime = new TacticalAISkillRuntime(runtimeContext, revalidatedIntent.SkillSlot);
        SkillCast cast = revalidatedIntent.ValidatedSkillCast.Clone();

        runtimeContext.BattleActionLifecycle = runtimeContext.BattleActionLifecycle ?? BattleActionLifecycle.EnsureInstance();
        bool started = runtimeContext.BattleActionLifecycle.TryRunAction(
            actor,
            BattleActionLifecycleKind.Skill,
            cast.SkillId,
            null,
            () => runtime.ApplySequence(cast, context),
            null);

        if (started == false)
        {
            failureReason = "BattleActionLifecycle rejected shared skill execution.";
        }

        return started;
    }

    static TosterHexUnit ResolveLiveUnit(TacticalAIExecutionRuntimeContext runtimeContext, BattleUnitSnapshot snapshotUnit)
    {
        if (runtimeContext == null ||
            runtimeContext.HexMap == null ||
            runtimeContext.HexMap.Teams == null ||
            snapshotUnit == null ||
            snapshotUnit.TeamIndex < 0 ||
            snapshotUnit.TeamIndex >= runtimeContext.HexMap.Teams.Count)
        {
            return null;
        }

        TeamClass team = runtimeContext.HexMap.Teams[snapshotUnit.TeamIndex];
        if (team == null ||
            team.Tosters == null ||
            snapshotUnit.RosterIndexWithinTeam < 0 ||
            snapshotUnit.RosterIndexWithinTeam >= team.Tosters.Count)
        {
            return null;
        }

        return team.Tosters[snapshotUnit.RosterIndexWithinTeam];
    }
}

public sealed class TacticalAISkillRuntime : ISkillRuntime
{
    readonly TacticalAIExecutionRuntimeContext runtimeContext;
    readonly int skillSlot;

    public TacticalAISkillRuntime(TacticalAIExecutionRuntimeContext runtimeContext, int skillSlot)
    {
        this.runtimeContext = runtimeContext;
        this.skillSlot = skillSlot;
    }

    public SkillResult Apply(SkillCast cast, SkillContext context)
    {
        SkillResult result = SkillRules.Preview(cast, context);
        if (cast == null)
        {
            return result;
        }

        TosterHexUnit actor = ResolveLiveUnit(cast.ActorUnitId, context != null ? context.Snapshot : null);
        if (actor == null)
        {
            return result;
        }

        SendSkillChat(cast, actor);
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        ApplyResultEvents(result, cast, actor, reveals);
        PlaySkillPresentation(cast, actor, reveals);
        ApplyTurnAndCooldown(cast, actor);
        if (runtimeContext != null && runtimeContext.HexMap != null)
        {
            runtimeContext.HexMap.UpdateHexVisuals();
        }

        return result;
    }

    public IEnumerator ApplySequence(SkillCast cast, SkillContext context)
    {
        SkillResult result = SkillRules.Preview(cast, context);
        if (cast == null)
        {
            yield break;
        }

        TosterHexUnit actor = ResolveLiveUnit(cast.ActorUnitId, context != null ? context.Snapshot : null);
        if (actor == null)
        {
            yield break;
        }

        SendSkillChat(cast, actor);
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        yield return ApplyResultEventsSequence(result, cast, actor, reveals);
        PlaySkillPresentation(cast, actor, reveals);
        ApplyTurnAndCooldown(cast, actor);
        if (runtimeContext != null && runtimeContext.HexMap != null)
        {
            runtimeContext.HexMap.UpdateHexVisuals();
        }
    }

    void ApplyResultEvents(SkillResult result, SkillCast cast, TosterHexUnit actor, List<FrontendResultReveal> reveals)
    {
        if (result == null || result.Events == null)
        {
            return;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            SkillResultEvent resultEvent = result.Events[i];
            if (resultEvent == null)
            {
                continue;
            }

            switch (resultEvent.EventType)
            {
                case SkillResultEventType.DamageApplied:
                    AddReveal(reveals, ApplyDamage(cast, actor, resultEvent));
                    break;
                case SkillResultEventType.HpCostApplied:
                    actor.DealMePURE(Math.Max(0, resultEvent.Amount));
                    break;
                case SkillResultEventType.UnitMoved:
                    ApplyMove(actor, resultEvent);
                    break;
                case SkillResultEventType.TrapPlaced:
                    ApplyTrap(actor, resultEvent);
                    break;
                case SkillResultEventType.StackAmountChanged:
                    actor.SetAmount(Math.Max(0, actor.Amount + resultEvent.Amount));
                    break;
                case SkillResultEventType.StatusApplied:
                    AddReveal(reveals, ApplyStatus(cast, actor, resultEvent));
                    break;
                case SkillResultEventType.UnitSpawned:
                    ApplySpawn(cast, actor, resultEvent);
                    break;
                case SkillResultEventType.StanceChanged:
                    ApplyStance(cast, actor);
                    break;
            }
        }
    }

    IEnumerator ApplyResultEventsSequence(SkillResult result, SkillCast cast, TosterHexUnit actor, List<FrontendResultReveal> reveals)
    {
        if (result == null || result.Events == null)
        {
            yield break;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            SkillResultEvent resultEvent = result.Events[i];
            if (resultEvent == null)
            {
                continue;
            }

            switch (resultEvent.EventType)
            {
                case SkillResultEventType.DamageApplied:
                    AddReveal(reveals, ApplyDamage(cast, actor, resultEvent));
                    break;
                case SkillResultEventType.HpCostApplied:
                    actor.DealMePURE(Math.Max(0, resultEvent.Amount));
                    break;
                case SkillResultEventType.UnitMoved:
                    yield return ApplyMoveSequence(cast, actor, resultEvent);
                    break;
                case SkillResultEventType.TrapPlaced:
                    ApplyTrap(actor, resultEvent);
                    break;
                case SkillResultEventType.StackAmountChanged:
                    actor.SetAmount(Math.Max(0, actor.Amount + resultEvent.Amount));
                    break;
                case SkillResultEventType.StatusApplied:
                    AddReveal(reveals, ApplyStatus(cast, actor, resultEvent));
                    break;
                case SkillResultEventType.UnitSpawned:
                    ApplySpawn(cast, actor, resultEvent);
                    break;
                case SkillResultEventType.StanceChanged:
                    ApplyStance(cast, actor);
                    break;
            }
        }
    }

    FrontendResultReveal ApplyDamage(SkillCast cast, TosterHexUnit actor, SkillResultEvent resultEvent)
    {
        TosterHexUnit target = ResolveLiveUnit(resultEvent.TargetUnitId, null);
        if (target == null)
        {
            return null;
        }

        SkillEffect effect = FirstDamageEffect(cast);
        int finalDamage;
        if (resultEvent.Amount > 0 || effect == null || effect.damageMode == SkillDamageMode.FixedDamageThroughDefense || effect.damageMode == SkillDamageMode.PureDamage)
        {
            finalDamage = resultEvent.Amount > 0 ? resultEvent.Amount : Math.Max(0, effect.fixedDamageValue);
            return ApplyPureDamageForReveal(actor, target, finalDamage);
        }

        double scale = Math.Max(0f, effect.damageScale);
        int scaledDamage = Convert.ToInt32(actor.CalculateDamageBetweenTosters(actor, target, scale));
        finalDamage = Math.Max(0, scaledDamage);
        return ApplyPureDamageForReveal(actor, target, finalDamage);
    }

    FrontendResultReveal ApplyPureDamageForReveal(TosterHexUnit actor, TosterHexUnit target, int damage)
    {
        if (target == null)
        {
            return null;
        }

        TosterView targetView = target.tosterView;
        bool damageWasReduced = damage > 0 && target.FlatDMGReduce > 0;
        SendDamageChat(actor, damage, target);
        bool survived = target.DealMePURE(damage, false);
        return new FrontendResultReveal(
            FrontendResultRevealSource.Skill,
            actor,
            target,
            targetView,
            damage,
            survived,
            damageWasReduced);
    }

    void ApplyMove(TosterHexUnit actor, SkillResultEvent resultEvent)
    {
        TosterHexUnit target = ResolveLiveUnit(resultEvent.TargetUnitId, null) ?? actor;
        HexClass destination = ResolveLiveHex(resultEvent.Hex);
        if (target != null && destination != null)
        {
            target.TeleportToHex(destination);
        }
    }

    IEnumerator ApplyMoveSequence(SkillCast cast, TosterHexUnit actor, SkillResultEvent resultEvent)
    {
        TosterHexUnit target = ResolveLiveUnit(resultEvent.TargetUnitId, null) ?? actor;
        HexClass destination = ResolveLiveHex(resultEvent.Hex);
        if (target == null || destination == null)
        {
            yield break;
        }

        SkillEffect moveEffect = FirstMoveEffect(cast);
        SkillMovementMode movementMode = moveEffect != null ? moveEffect.movementMode : SkillMovementMode.None;

        if ((movementMode == SkillMovementMode.NormalPathMove || movementMode == SkillMovementMode.MoveThenArea) &&
            target == actor)
        {
            yield return MoveUnitAlongPath(target, destination, false);
            yield break;
        }

        if (movementMode == SkillMovementMode.LineRush && target == actor)
        {
            SkillPresentationManager.PlayCastSfxOnly(cast.SkillId);
            yield return MoveUnitAlongPath(target, destination, true);
            yield break;
        }

        if (movementMode == SkillMovementMode.TeleportTarget)
        {
            SkillPresentationManager.PlaySequencedHexCastUnitImpactThenReveals(
                cast.SkillId,
                actor,
                destination,
                target,
                new List<FrontendResultReveal>(),
                ResolveCasterAnimationState(),
                () => target.TeleportToHex(destination));
            yield return SkillPresentationManager.WaitForBlockingPresentation(15f);
            yield break;
        }

        target.TeleportToHex(destination);
    }

    IEnumerator MoveUnitAlongPath(TosterHexUnit unit, HexClass destination, bool useRunAnimation)
    {
        if (unit == null || destination == null)
        {
            yield break;
        }

        if (unit.Hex == destination)
        {
            yield break;
        }

        if (runtimeContext == null || runtimeContext.HexMap == null)
        {
            unit.TeleportToHex(destination);
            yield break;
        }

        bool previousMoveFlag = unit.move;
        unit.move = true;
        if (useRunAnimation)
        {
            unit.SetMovementAnimationOverride("run");
        }

        try
        {
            unit.Pathing_func(destination, false);
            yield return runtimeContext.HexMap.DoUnitMoves(unit);
        }
        finally
        {
            if (useRunAnimation)
            {
                unit.ClearMovementAnimationOverride();
            }

            unit.move = previousMoveFlag;
        }
    }

    void ApplyTrap(TosterHexUnit actor, SkillResultEvent resultEvent)
    {
        HexClass hex = ResolveLiveHex(resultEvent.Hex);
        if (hex == null)
        {
            return;
        }

        string trapId = string.IsNullOrEmpty(resultEvent.TrapId) ? resultEvent.SkillId : resultEvent.TrapId;
        hex.AddTrap(trapId, 999, actor, true, resultEvent.SkillId);
    }

    FrontendResultReveal ApplyStatus(SkillCast cast, TosterHexUnit actor, SkillResultEvent resultEvent)
    {
        TosterHexUnit target = ResolveLiveUnit(resultEvent.TargetUnitId, null) ?? actor;
        SkillEffect effect = FirstStatusEffect(cast, resultEvent.StatusId);
        if (target == null || effect == null)
        {
            return null;
        }

        target.AddNewTimeSpell(
            Math.Max(0, effect.durationTurns),
            actor,
            effect.hpModifier,
            effect.attackModifier,
            effect.defenseModifier,
            effect.movementModifier,
            effect.initiativeModifier,
            effect.maxDamageModifier,
            effect.minDamageModifier,
            effect.damageOverTime,
            effect.resistanceModifier,
            effect.counterAttacksModifier,
            effect.damageModifier,
            effect.specialResistanceModifier,
            string.IsNullOrEmpty(effect.statusId) ? cast.SkillId : effect.statusId,
            effect.isStackable);
        return target.BuildStatusFrontendReveal(actor, FrontendResultRevealSource.Skill);
    }

    void ApplySpawn(SkillCast cast, TosterHexUnit actor, SkillResultEvent resultEvent)
    {
        if (cast == null || actor == null || actor.Team == null || runtimeContext == null || runtimeContext.HexMap == null)
        {
            return;
        }

        SkillEffect effect = FirstSpawnEffect(cast);
        if (effect == null)
        {
            return;
        }

        HexClass destination = ResolveLiveHex(resultEvent.Hex ?? cast.DestinationHex);
        if (destination == null || destination.Tosters == null || destination.Tosters.Count > 0)
        {
            return;
        }

        string unitId = string.IsNullOrEmpty(effect.unitId) ? actor.Name : effect.unitId;
        int spawnedAmount = effect.stackAmountDelta > 0 ? effect.stackAmountDelta : Math.Max(1, actor.Amount / 2);
        TosterHexUnit newUnit = actor.Team.AddNewUnit(unitId, spawnedAmount);
        if (newUnit == null)
        {
            return;
        }

        newUnit.teamN = actor.teamN;
        newUnit.SetTosterPrefab(runtimeContext.HexMap);
        newUnit.SetTextAmount();
        runtimeContext.HexMap.GenerateToster(destination.C, destination.R, newUnit);
        newUnit.Moved = true;
    }

    void ApplyStance(SkillCast cast, TosterHexUnit actor)
    {
        if (cast == null || actor == null)
        {
            return;
        }

        if (cast.SkillId.StartsWith("Range_Stance", StringComparison.Ordinal))
        {
            actor.isRange = true;
            actor.SpecialDMGModificator = 20;
            actor.SpecialResistance = 20;
            ReplaceStanceSkillId(actor, cast.SkillId, "Melee_Stance");
        }
        else if (cast.SkillId.StartsWith("Melee_Stance", StringComparison.Ordinal))
        {
            actor.isRange = false;
            actor.SpecialDMGModificator = 0;
            actor.SpecialResistance = 0;
            ReplaceStanceSkillId(actor, cast.SkillId, "Range_Stance");
        }
    }

    void ReplaceStanceSkillId(TosterHexUnit actor, string currentSkillId, string replacementPrefix)
    {
        if (actor == null ||
            actor.skillstrings == null ||
            skillSlot < 0 ||
            skillSlot >= actor.skillstrings.Count ||
            string.IsNullOrEmpty(currentSkillId) ||
            string.IsNullOrEmpty(replacementPrefix))
        {
            return;
        }

        int separator = currentSkillId.IndexOf('_', "Range_Stance".Length);
        if (separator < 0)
        {
            separator = currentSkillId.IndexOf('_', "Melee_Stance".Length);
        }

        if (separator < 0 || separator >= currentSkillId.Length)
        {
            return;
        }

        actor.skillstrings[skillSlot] = replacementPrefix + currentSkillId.Substring(separator);
    }

    void PlaySkillPresentation(SkillCast cast, TosterHexUnit actor, List<FrontendResultReveal> reveals)
    {
        if (cast == null || actor == null)
        {
            return;
        }

        string animationState = ResolveCasterAnimationState();
        if (string.Equals(cast.SkillId, "Double_Throw", StringComparison.Ordinal) ||
            string.Equals(cast.SkillId, "Axe_Rain", StringComparison.Ordinal))
        {
            SkillPresentationManager.PlaySequencedProjectileHitsToUnits(cast.SkillId, actor, reveals, animationState);
            return;
        }

        if (string.Equals(cast.SkillId, "Rush", StringComparison.Ordinal))
        {
            SkillPresentationManager.PlaySequencedHexEffectWithReveals(
                cast.SkillId,
                actor,
                ResolvePresentationHex(cast, actor),
                reveals,
                animationState);
            return;
        }

        if (string.Equals(cast.SkillId, "Stone_Throw", StringComparison.Ordinal))
        {
            SkillPresentationManager.PlaySequencedProjectileHexImpactThenReveals(
                cast.SkillId,
                actor,
                ResolvePresentationHex(cast, actor),
                reveals,
                animationState,
                null);
            return;
        }

        if (reveals != null && reveals.Count > 0)
        {
            SkillPresentationManager.PlaySequencedInstantHits(cast.SkillId, actor, reveals, animationState);
            return;
        }

        if (IsCasterOnlyPresentation(cast.SkillId))
        {
            SkillPresentationManager.PlaySequencedCasterEffect(cast.SkillId, actor, animationState);
        }
    }

    HexClass ResolvePresentationHex(SkillCast cast, TosterHexUnit actor)
    {
        HexClass hex = ResolveLiveHex(cast != null ? cast.ImpactHex : null);
        if (hex != null)
        {
            return hex;
        }

        hex = ResolveLiveHex(FirstHex(cast != null ? cast.SelectedHexes : null));
        if (hex != null)
        {
            return hex;
        }

        hex = ResolveLiveHex(cast != null ? cast.DestinationHex : null);
        return hex != null ? hex : actor != null ? actor.Hex : null;
    }

    string ResolveCasterAnimationState()
    {
        return skillSlot >= 0 ? "skill" + (skillSlot + 1) : null;
    }

    static bool IsCasterOnlyPresentation(string skillId)
    {
        return string.Equals(skillId, "Range_Stance_Barb", StringComparison.Ordinal) ||
            string.Equals(skillId, "Melee_Stance_Barb", StringComparison.Ordinal) ||
            string.Equals(skillId, "Range_Stance_Lizard", StringComparison.Ordinal) ||
            string.Equals(skillId, "Melee_Stance_Lizard", StringComparison.Ordinal);
    }

    static HexCoord FirstHex(List<HexCoord> hexes)
    {
        if (hexes == null || hexes.Count == 0)
        {
            return null;
        }

        return hexes[0];
    }

    static void AddReveal(List<FrontendResultReveal> reveals, FrontendResultReveal reveal)
    {
        if (reveals != null && reveal != null)
        {
            reveals.Add(reveal);
        }
    }

    void SendSkillChat(SkillCast cast, TosterHexUnit actor)
    {
        if (cast == null || actor == null || Chat.chat == null)
        {
            return;
        }

        TosterHexUnit target = ResolveLiveUnit(cast.PrimaryTargetUnitId, null);
        if (target != null)
        {
            Chat.chat.SendTargetedSkillMessage(actor, cast.SkillId, target);
            return;
        }

        Chat.chat.SendSkillUseMessage(actor, cast.SkillId);
    }

    static void SendDamageChat(TosterHexUnit actor, int damage, TosterHexUnit target)
    {
        if (Chat.chat == null || target == null)
        {
            return;
        }

        Chat.chat.SendDamageMessage(actor, damage, target);
    }

    void ApplyTurnAndCooldown(SkillCast cast, TosterHexUnit actor)
    {
        if (actor.UsedSkillIdsThisTurn == null)
        {
            actor.UsedSkillIdsThisTurn = new List<string>();
        }

        bool repeatable = cast.RepeatableInTurn;
        if (repeatable == false)
        {
            actor.UsedSkillThisTurn = true;
            actor.AddUsedSkillIdThisTurn(cast.SkillId);
        }

        actor.CanMoveAfterSkillThisTurn = cast.CanMoveAfterUse;
        if (skillSlot >= 0 && actor.cooldowns != null && skillSlot < actor.cooldowns.Count && cast.CooldownTurns > 0)
        {
            actor.cooldowns[skillSlot] = Math.Max(1, cast.CooldownTurns);
        }

        if (repeatable == false && cast.ConsumesTurn && cast.CanMoveAfterUse == false)
        {
            actor.Moved = true;
        }
    }

    TosterHexUnit ResolveLiveUnit(string runtimeUnitId, BattleSnapshot snapshot)
    {
        if (runtimeContext == null || runtimeContext.HexMap == null || runtimeContext.HexMap.Teams == null)
        {
            return null;
        }

        BattleUnitSnapshot unitSnapshot = null;
        if (snapshot != null)
        {
            unitSnapshot = TacticalAISnapshotQuery.FindUnit(snapshot, runtimeUnitId);
        }

        if (unitSnapshot != null)
        {
            return ResolveLiveUnit(unitSnapshot);
        }

        for (int teamIndex = 0; teamIndex < runtimeContext.HexMap.Teams.Count; teamIndex++)
        {
            TeamClass team = runtimeContext.HexMap.Teams[teamIndex];
            if (team == null || team.Tosters == null)
            {
                continue;
            }

            for (int rosterIndex = 0; rosterIndex < team.Tosters.Count; rosterIndex++)
            {
                string candidateId = BattleSnapshotRuntimeIds.CreateUnitId(teamIndex, rosterIndex);
                if (string.Equals(candidateId, runtimeUnitId, StringComparison.Ordinal))
                {
                    return team.Tosters[rosterIndex];
                }
            }
        }

        return null;
    }

    TosterHexUnit ResolveLiveUnit(BattleUnitSnapshot snapshotUnit)
    {
        if (snapshotUnit == null ||
            runtimeContext == null ||
            runtimeContext.HexMap == null ||
            runtimeContext.HexMap.Teams == null ||
            snapshotUnit.TeamIndex < 0 ||
            snapshotUnit.TeamIndex >= runtimeContext.HexMap.Teams.Count)
        {
            return null;
        }

        TeamClass team = runtimeContext.HexMap.Teams[snapshotUnit.TeamIndex];
        if (team == null ||
            team.Tosters == null ||
            snapshotUnit.RosterIndexWithinTeam < 0 ||
            snapshotUnit.RosterIndexWithinTeam >= team.Tosters.Count)
        {
            return null;
        }

        return team.Tosters[snapshotUnit.RosterIndexWithinTeam];
    }

    HexClass ResolveLiveHex(HexCoord hex)
    {
        if (hex == null || runtimeContext == null || runtimeContext.HexMap == null)
        {
            return null;
        }

        return runtimeContext.HexMap.GetHexAt(hex.C, hex.R);
    }

    static SkillEffect FirstDamageEffect(SkillCast cast)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null && effects[i].effectType == SkillEffectType.Damage)
            {
                return effects[i];
            }
        }

        return null;
    }

    static SkillEffect FirstStatusEffect(SkillCast cast, string statusId)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] == null || effects[i].effectType != SkillEffectType.ApplyStatus)
            {
                continue;
            }

            if (string.IsNullOrEmpty(statusId) || string.Equals(effects[i].statusId, statusId, StringComparison.Ordinal))
            {
                return effects[i];
            }
        }

        return null;
    }

    static SkillEffect FirstMoveEffect(SkillCast cast)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null && effects[i].effectType == SkillEffectType.MoveUnit)
            {
                return effects[i];
            }
        }

        return null;
    }

    static SkillEffect FirstSpawnEffect(SkillCast cast)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null && effects[i].effectType == SkillEffectType.SpawnUnit)
            {
                return effects[i];
            }
        }

        return null;
    }

}
