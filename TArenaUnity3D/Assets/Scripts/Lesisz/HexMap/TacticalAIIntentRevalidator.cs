using System;

public sealed class TacticalAIRevalidatedIntent
{
    public TacticalAIActionType ActionType;
    public BattleUnitSnapshot Actor;
    public BattleUnitSnapshot Target;
    public TacticalAIHexCoordinate DestinationHex;
    public TacticalAIHexCoordinate TargetHex;
    public int SkillSlot = -1;
    public string SkillId = string.Empty;
}

public static class TacticalAIIntentRevalidator
{
    public static bool TryRevalidate(
        TacticalAIActionIntent intent,
        BattleSnapshot liveSnapshot,
        BattleSnapshot plannedSnapshot,
        out TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        revalidatedIntent = null;
        failureReason = string.Empty;

        if (intent == null)
        {
            failureReason = "Intent was null.";
            return false;
        }

        if (liveSnapshot == null)
        {
            failureReason = "Live snapshot was unavailable.";
            return false;
        }

        if (liveSnapshot.TurnState != null && liveSnapshot.TurnState.IsActionBlocking)
        {
            failureReason = "Battle action lifecycle is currently blocking.";
            return false;
        }

        if (string.IsNullOrEmpty(liveSnapshot.ActiveUnitId))
        {
            failureReason = "Live snapshot has no active unit.";
            return false;
        }

        if (string.Equals(liveSnapshot.ActiveUnitId, intent.ActorUnitId, StringComparison.Ordinal) == false)
        {
            failureReason = "Intent actor is no longer the live active unit.";
            return false;
        }

        BattleUnitSnapshot liveActor = FindUnit(liveSnapshot, intent.ActorUnitId);
        if (liveActor == null)
        {
            failureReason = "Intent actor no longer exists in the live snapshot.";
            return false;
        }

        if (liveActor.IsAlive == false || liveActor.Amount <= 0)
        {
            failureReason = "Intent actor is no longer alive/actionable.";
            return false;
        }

        if (intent.SourceHex == null)
        {
            failureReason = "Intent source hex is missing.";
            return false;
        }

        if (liveActor.C != intent.SourceHex.C || liveActor.R != intent.SourceHex.R)
        {
            failureReason = "Intent source hex no longer matches the live actor position.";
            return false;
        }

        if (plannedSnapshot != null && TryValidatePlannedActor(intent, plannedSnapshot, liveActor, out failureReason) == false)
        {
            return false;
        }

        skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;

        TacticalAIRevalidatedIntent request = new TacticalAIRevalidatedIntent
        {
            ActionType = intent.ActionType,
            Actor = liveActor,
            SkillSlot = intent.SkillSlot,
            SkillId = intent.SkillId ?? string.Empty
        };

        switch (intent.ActionType)
        {
            case TacticalAIActionType.Wait:
                if (liveActor.MovedThisTurn || liveActor.UsedSkillThisTurn || liveActor.Waited)
                {
                    failureReason = "Wait is no longer legal for the live actor state.";
                    return false;
                }
                break;
            case TacticalAIActionType.Defend:
                if (liveActor.MovedThisTurn || liveActor.UsedSkillThisTurn)
                {
                    failureReason = "Defend is no longer legal for the live actor state.";
                    return false;
                }
                break;
            case TacticalAIActionType.Move:
                if (CanStartMovement(liveActor) == false)
                {
                    failureReason = "Move is no longer legal for the live actor state.";
                    return false;
                }

                if (TryResolveDestination(intent, liveSnapshot, liveActor, out request.DestinationHex, out failureReason) == false)
                {
                    return false;
                }
                break;
            case TacticalAIActionType.MoveAndAttack:
                if (CanStartMovement(liveActor) == false)
                {
                    failureReason = "Move-and-attack is no longer legal for the live actor state.";
                    return false;
                }

                if (liveActor.IsRange)
                {
                    failureReason = "Move-and-attack requires a melee actor.";
                    return false;
                }

                if (TryResolveDestination(intent, liveSnapshot, liveActor, out request.DestinationHex, out failureReason) == false)
                {
                    return false;
                }

                if (TryResolveTarget(intent, liveSnapshot, liveActor, out request.Target, out request.TargetHex, out failureReason) == false)
                {
                    return false;
                }

                if (AreAdjacent(request.DestinationHex.C, request.DestinationHex.R, request.Target.C, request.Target.R) == false)
                {
                    failureReason = "Move-and-attack destination is no longer adjacent to the target.";
                    return false;
                }
                break;
            case TacticalAIActionType.BasicRangedAttack:
                if (liveActor.IsRange == false)
                {
                    failureReason = "Basic ranged attack requires a ranged actor.";
                    return false;
                }

                if (liveActor.MovedThisTurn || liveActor.UsedSkillThisTurn)
                {
                    failureReason = "Basic ranged attack is no longer legal for the live actor state.";
                    return false;
                }

                if (TryResolveTarget(intent, liveSnapshot, liveActor, out request.Target, out request.TargetHex, out failureReason) == false)
                {
                    return false;
                }
                break;
            case TacticalAIActionType.Skill:
                if (TryResolveSkill(intent, liveActor, skillMetadataProvider, out request.SkillSlot, out request.SkillId, out failureReason) == false)
                {
                    return false;
                }

                if (TryResolveOptionalSkillTarget(intent, liveSnapshot, out request.Target, out request.TargetHex, out request.DestinationHex, out failureReason) == false)
                {
                    return false;
                }
                break;
            default:
                failureReason = "Unsupported tactical AI action type: " + intent.ActionType;
                return false;
        }

        revalidatedIntent = request;
        return true;
    }

    static bool TryValidatePlannedActor(
        TacticalAIActionIntent intent,
        BattleSnapshot plannedSnapshot,
        BattleUnitSnapshot liveActor,
        out string failureReason)
    {
        failureReason = string.Empty;
        BattleUnitSnapshot plannedActor = FindUnit(plannedSnapshot, intent.ActorUnitId);
        if (plannedActor == null)
        {
            failureReason = "Planned snapshot no longer contains the selected actor.";
            return false;
        }

        if (plannedActor.TeamIndex != liveActor.TeamIndex ||
            plannedActor.RosterIndexWithinTeam != liveActor.RosterIndexWithinTeam)
        {
            failureReason = "Planned actor identity no longer matches the live actor.";
            return false;
        }

        if (string.Equals(plannedActor.UnitName, liveActor.UnitName, StringComparison.Ordinal) == false ||
            string.Equals(plannedActor.UnitType, liveActor.UnitType, StringComparison.Ordinal) == false)
        {
            failureReason = "Planned actor name/type no longer matches the live actor.";
            return false;
        }

        return true;
    }

    static bool TryResolveDestination(
        TacticalAIActionIntent intent,
        BattleSnapshot liveSnapshot,
        BattleUnitSnapshot liveActor,
        out TacticalAIHexCoordinate destinationHex,
        out string failureReason)
    {
        destinationHex = null;
        failureReason = string.Empty;

        if (intent.DestinationHex == null)
        {
            failureReason = "Intent destination hex is missing.";
            return false;
        }

        BattleHexSnapshot destination = FindHex(liveSnapshot, intent.DestinationHex.C, intent.DestinationHex.R);
        if (destination == null || destination.IsWalkable == false)
        {
            failureReason = "Intent destination hex no longer exists or is not walkable.";
            return false;
        }

        bool isActorSource = destination.C == liveActor.C && destination.R == liveActor.R;
        if (isActorSource == false && string.IsNullOrEmpty(destination.OccupyingUnitId) == false)
        {
            failureReason = "Intent destination hex is now occupied.";
            return false;
        }

        destinationHex = new TacticalAIHexCoordinate(destination.C, destination.R);
        return true;
    }

    static bool TryResolveTarget(
        TacticalAIActionIntent intent,
        BattleSnapshot liveSnapshot,
        BattleUnitSnapshot liveActor,
        out BattleUnitSnapshot target,
        out TacticalAIHexCoordinate targetHex,
        out string failureReason)
    {
        target = null;
        targetHex = null;
        failureReason = string.Empty;

        if (string.IsNullOrEmpty(intent.TargetUnitId))
        {
            failureReason = "Intent target unit id is missing.";
            return false;
        }

        if (intent.TargetHex == null)
        {
            failureReason = "Intent target hex is missing.";
            return false;
        }

        target = FindUnit(liveSnapshot, intent.TargetUnitId);
        if (target == null || target.IsAlive == false || target.Amount <= 0)
        {
            failureReason = "Intent target is no longer alive in the live snapshot.";
            return false;
        }

        if (target.TeamIndex == liveActor.TeamIndex)
        {
            failureReason = "Intent target is no longer an enemy unit.";
            return false;
        }

        if (target.C != intent.TargetHex.C || target.R != intent.TargetHex.R)
        {
            failureReason = "Intent target hex no longer matches the live target position.";
            return false;
        }

        targetHex = new TacticalAIHexCoordinate(target.C, target.R);
        return true;
    }

    static bool TryResolveOptionalSkillTarget(
        TacticalAIActionIntent intent,
        BattleSnapshot liveSnapshot,
        out BattleUnitSnapshot target,
        out TacticalAIHexCoordinate targetHex,
        out TacticalAIHexCoordinate destinationHex,
        out string failureReason)
    {
        target = null;
        targetHex = null;
        destinationHex = null;
        failureReason = string.Empty;

        if (intent.TargetHex != null)
        {
            BattleHexSnapshot liveTargetHex = FindHex(liveSnapshot, intent.TargetHex.C, intent.TargetHex.R);
            if (liveTargetHex == null || liveTargetHex.IsWalkable == false)
            {
                failureReason = "Skill target hex no longer exists or is not walkable.";
                return false;
            }

            targetHex = new TacticalAIHexCoordinate(liveTargetHex.C, liveTargetHex.R);
        }

        if (intent.DestinationHex != null)
        {
            BattleHexSnapshot liveDestinationHex = FindHex(liveSnapshot, intent.DestinationHex.C, intent.DestinationHex.R);
            if (liveDestinationHex == null || liveDestinationHex.IsWalkable == false)
            {
                failureReason = "Skill destination hex no longer exists or is not walkable.";
                return false;
            }

            destinationHex = new TacticalAIHexCoordinate(liveDestinationHex.C, liveDestinationHex.R);
        }

        if (string.IsNullOrEmpty(intent.TargetUnitId))
        {
            return true;
        }

        target = FindUnit(liveSnapshot, intent.TargetUnitId);
        if (target == null || target.IsAlive == false || target.Amount <= 0)
        {
            failureReason = "Skill target unit is no longer alive in the live snapshot.";
            return false;
        }

        if (targetHex == null)
        {
            targetHex = new TacticalAIHexCoordinate(target.C, target.R);
            return true;
        }

        if (target.C != targetHex.C || target.R != targetHex.R)
        {
            failureReason = "Skill target hex no longer matches the live target unit position.";
            return false;
        }

        return true;
    }

    static bool TryResolveSkill(
        TacticalAIActionIntent intent,
        BattleUnitSnapshot liveActor,
        ITacticalAISkillMetadataProvider skillMetadataProvider,
        out int skillSlot,
        out string skillId,
        out string failureReason)
    {
        skillSlot = -1;
        skillId = string.Empty;
        failureReason = string.Empty;

        if (liveActor == null || liveActor.SkillIdsBySlot == null || liveActor.CooldownsBySlot == null)
        {
            failureReason = "Live actor has no skill state available.";
            return false;
        }

        if (liveActor.Waited)
        {
            failureReason = "Skills are no longer legal after waiting.";
            return false;
        }

        if (intent.SkillSlot < 0 ||
            intent.SkillSlot >= liveActor.SkillIdsBySlot.Count ||
            intent.SkillSlot >= liveActor.CooldownsBySlot.Count)
        {
            failureReason = "Intent skill slot is no longer valid for the live actor.";
            return false;
        }

        string liveSkillId = liveActor.SkillIdsBySlot[intent.SkillSlot] ?? string.Empty;
        if (string.IsNullOrEmpty(liveSkillId))
        {
            failureReason = "Live actor no longer has a skill in the selected slot.";
            return false;
        }

        if (string.Equals(liveSkillId, intent.SkillId ?? string.Empty, StringComparison.Ordinal) == false)
        {
            failureReason = "Intent skill id no longer matches the live actor slot.";
            return false;
        }

        if (liveActor.CooldownsBySlot[intent.SkillSlot] > 0)
        {
            failureReason = "Intent skill is now on cooldown.";
            return false;
        }

        TacticalAISkillMetadata metadata = ResolveSkillMetadata(liveSkillId, skillMetadataProvider);
        if (metadata.IsPassive)
        {
            failureReason = "Passive skills are not executable AI action intents.";
            return false;
        }

        if (metadata.IsRepeatableToggle == false)
        {
            if (liveActor.MovedThisTurn && metadata.CanUseAfterMove == false)
            {
                failureReason = "Skill can no longer be used after the actor's movement state changed.";
                return false;
            }

            if (liveActor.UsedSkillIdsThisTurn != null && liveActor.UsedSkillIdsThisTurn.Contains(liveSkillId))
            {
                failureReason = "Skill was already used this turn.";
                return false;
            }
        }

        skillSlot = intent.SkillSlot;
        skillId = liveSkillId;
        return true;
    }

    static TacticalAISkillMetadata ResolveSkillMetadata(
        string skillId,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        TacticalAISkillMetadata metadata;
        if (skillMetadataProvider != null && skillMetadataProvider.TryGetSkillMetadata(skillId, out metadata) && metadata != null)
        {
            return metadata;
        }

        return new TacticalAISkillMetadata
        {
            SkillId = skillId ?? string.Empty,
            IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
        };
    }

    static bool CanStartMovement(BattleUnitSnapshot actor)
    {
        return actor != null &&
            actor.IsAlive &&
            actor.MovedThisTurn == false &&
            (actor.UsedSkillThisTurn == false || actor.CanMoveAfterSkillThisTurn);
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

    static bool AreAdjacent(int c1, int r1, int c2, int r2)
    {
        return (c1 == c2 && Math.Abs(r1 - r2) == 1) ||
            (r1 == r2 && Math.Abs(c1 - c2) == 1) ||
            (c1 + 1 == c2 && r1 - 1 == r2) ||
            (c1 - 1 == c2 && r1 + 1 == r2);
    }
}
