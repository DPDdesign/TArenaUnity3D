using System.Collections.Generic;
using NUnit.Framework;

public class TacticalAIExecutionBridgeTests
{
    [Test]
    public void Revalidator_AcceptsLegalMoveIntentAgainstLiveSnapshot()
    {
        BattleSnapshot liveSnapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent moveIntent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Move,
            ActorUnitId = "team-0-slot-0",
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            DestinationHex = new TacticalAIHexCoordinate(1, 0),
            StableOrderKey = "move"
        };

        TacticalAIRevalidatedIntent revalidated;
        string failureReason;
        bool valid = TacticalAIIntentRevalidator.TryRevalidate(
            moveIntent,
            liveSnapshot,
            liveSnapshot,
            out revalidated,
            out failureReason,
            new TestSkillMetadataProvider());

        Assert.That(valid, Is.True, failureReason);
        Assert.That(revalidated, Is.Not.Null);
        Assert.That(revalidated.DestinationHex.C, Is.EqualTo(1));
        Assert.That(revalidated.DestinationHex.R, Is.EqualTo(0));
    }

    [Test]
    public void Revalidator_RejectsWhenIntentActorIsNotLiveActiveUnit()
    {
        BattleSnapshot liveSnapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent staleIntent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Wait,
            ActorUnitId = "team-1-slot-0",
            SourceHex = new TacticalAIHexCoordinate(3, 0),
            StableOrderKey = "stale-wait"
        };

        TacticalAIRevalidatedIntent revalidated;
        string failureReason;
        bool valid = TacticalAIIntentRevalidator.TryRevalidate(
            staleIntent,
            liveSnapshot,
            liveSnapshot,
            out revalidated,
            out failureReason,
            new TestSkillMetadataProvider());

        Assert.That(valid, Is.False);
        Assert.That(failureReason, Does.Contain("live active unit"));
        Assert.That(revalidated, Is.Null);
    }

    [Test]
    public void Revalidator_RejectsSkillWhenLiveCooldownChanged()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "BattleCry" };
        actor.CooldownsBySlot = new List<int> { 2 };

        BattleSnapshot liveSnapshot = CreateSnapshot(
            actor,
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent skillIntent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Skill,
            ActorUnitId = actor.RuntimeUnitId,
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            SkillSlot = 0,
            SkillId = "BattleCry",
            StableOrderKey = "skill"
        };

        TacticalAIRevalidatedIntent revalidated;
        string failureReason;
        bool valid = TacticalAIIntentRevalidator.TryRevalidate(
            skillIntent,
            liveSnapshot,
            liveSnapshot,
            out revalidated,
            out failureReason,
            new TestSkillMetadataProvider());

        Assert.That(valid, Is.False);
        Assert.That(failureReason, Does.Contain("cooldown"));
        Assert.That(revalidated, Is.Null);
    }

    [Test]
    public void Revalidator_AcceptsSkillAndPreservesSlotIdPair()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "FirstSkill", "BattleCry" };
        actor.CooldownsBySlot = new List<int> { 0, 0 };

        BattleSnapshot liveSnapshot = CreateSnapshot(
            actor,
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent skillIntent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Skill,
            ActorUnitId = actor.RuntimeUnitId,
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            TargetUnitId = "team-1-slot-0",
            TargetHex = new TacticalAIHexCoordinate(3, 0),
            SkillSlot = 1,
            SkillId = "BattleCry",
            StableOrderKey = "skill-slot-pair"
        };

        TacticalAIRevalidatedIntent revalidated;
        string failureReason;
        bool valid = TacticalAIIntentRevalidator.TryRevalidate(
            skillIntent,
            liveSnapshot,
            liveSnapshot,
            out revalidated,
            out failureReason,
            new TestSkillMetadataProvider());

        Assert.That(valid, Is.True, failureReason);
        Assert.That(revalidated.SkillSlot, Is.EqualTo(1));
        Assert.That(revalidated.SkillId, Is.EqualTo("BattleCry"));
        Assert.That(revalidated.TargetHex.C, Is.EqualTo(3));
        Assert.That(revalidated.TargetHex.R, Is.EqualTo(0));
    }

    [Test]
    public void Revalidator_RejectsUsedNonToggleSkill()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "BattleCry" };
        actor.CooldownsBySlot = new List<int> { 0 };
        actor.UsedSkillIdsThisTurn = new List<string> { "BattleCry" };

        BattleSnapshot liveSnapshot = CreateSnapshot(
            actor,
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent skillIntent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Skill,
            ActorUnitId = actor.RuntimeUnitId,
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            SkillSlot = 0,
            SkillId = "BattleCry",
            StableOrderKey = "used-skill"
        };

        TacticalAIRevalidatedIntent revalidated;
        string failureReason;
        bool valid = TacticalAIIntentRevalidator.TryRevalidate(
            skillIntent,
            liveSnapshot,
            liveSnapshot,
            out revalidated,
            out failureReason,
            new TestSkillMetadataProvider());

        Assert.That(valid, Is.False);
        Assert.That(failureReason, Does.Contain("already used"));
        Assert.That(revalidated, Is.Null);
    }

    [Test]
    public void Revalidator_AcceptsRepeatableToggleEvenWhenAlreadyUsed()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "Range_Stance_Barb" };
        actor.CooldownsBySlot = new List<int> { 0 };
        actor.UsedSkillIdsThisTurn = new List<string> { "Range_Stance_Barb" };

        BattleSnapshot liveSnapshot = CreateSnapshot(
            actor,
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent skillIntent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Skill,
            ActorUnitId = actor.RuntimeUnitId,
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            SkillSlot = 0,
            SkillId = "Range_Stance_Barb",
            StableOrderKey = "toggle-skill"
        };

        TacticalAIRevalidatedIntent revalidated;
        string failureReason;
        bool valid = TacticalAIIntentRevalidator.TryRevalidate(
            skillIntent,
            liveSnapshot,
            liveSnapshot,
            out revalidated,
            out failureReason,
            new TestSkillMetadataProvider());

        Assert.That(valid, Is.True, failureReason);
        Assert.That(revalidated.SkillSlot, Is.EqualTo(0));
        Assert.That(revalidated.SkillId, Is.EqualTo("Range_Stance_Barb"));
    }

    [Test]
    public void FallbackPlanner_UsesPlanThenFreshGreedyThenDefendThenWait()
    {
        BattleSnapshot liveSnapshot = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        TacticalAIActionIntent plannedMove = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Move,
            ActorUnitId = "team-0-slot-0",
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            DestinationHex = new TacticalAIHexCoordinate(1, 0),
            StableOrderKey = "planned-move"
        };

        List<TacticalAIActionIntent> queue = TacticalAIExecutionFallbackPlanner.BuildAttemptQueue(
            new[] { plannedMove },
            liveSnapshot,
            new TacticalAICandidateGenerationOptions
            {
                MaxCandidatesPerActionType = 16,
                MaxSkillCandidates = 0,
                MaxMoveCandidates = 16,
                MaxAttackCandidates = 16
            },
            new TestSkillMetadataProvider(),
            maxFallbackCandidates: 4);

        Assert.That(queue.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(queue[0].StableOrderKey, Is.EqualTo("planned-move"));
        Assert.That(queue[1].ActionType, Is.EqualTo(TacticalAIActionType.BasicRangedAttack));
        Assert.That(queue[queue.Count - 2].ActionType, Is.EqualTo(TacticalAIActionType.Defend));
        Assert.That(queue[queue.Count - 1].ActionType, Is.EqualTo(TacticalAIActionType.Wait));
    }

    [Test]
    public void FallbackPlanner_DeduplicatesMatchingPlanAndFreshCandidate()
    {
        BattleSnapshot liveSnapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0));

        TacticalAIActionIntent freshMove = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Move,
            ActorUnitId = "team-0-slot-0",
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            DestinationHex = new TacticalAIHexCoordinate(1, 0),
            StableOrderKey = "Move|team-0-slot-0|0|0|-1||1|0||"
        };

        List<TacticalAIActionIntent> queue = TacticalAIExecutionFallbackPlanner.BuildAttemptQueue(
            new[] { freshMove },
            liveSnapshot,
            new TacticalAICandidateGenerationOptions
            {
                MaxCandidatesPerActionType = 16,
                MaxSkillCandidates = 0,
                MaxMoveCandidates = 16,
                MaxAttackCandidates = 16
            },
            new TestSkillMetadataProvider(),
            maxFallbackCandidates: 4);

        int occurrences = CountStableKey(queue, freshMove.StableOrderKey);
        Assert.That(occurrences, Is.EqualTo(1));
    }

    static int CountStableKey(List<TacticalAIActionIntent> actions, string stableKey)
    {
        int count = 0;
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != null && actions[i].StableOrderKey == stableKey)
            {
                count++;
            }
        }

        return count;
    }

    static BattleSnapshot CreateSnapshot(BattleUnitSnapshot actor, params BattleUnitSnapshot[] others)
    {
        List<BattleUnitSnapshot> units = new List<BattleUnitSnapshot> { actor };
        if (others != null)
        {
            units.AddRange(others);
        }

        List<BattleHexSnapshot> hexes = new List<BattleHexSnapshot>();
        for (int c = 0; c < 5; c++)
        {
            for (int r = 0; r < 5; r++)
            {
                hexes.Add(new BattleHexSnapshot
                {
                    C = c,
                    R = r,
                    IsWalkable = true,
                    OccupyingUnitId = FindOccupant(units, c, r)
                });
            }
        }

        return BattleSnapshotBuilder.Build(
            5,
            5,
            hexes,
            units,
            actor.RuntimeUnitId,
            new BattleTurnStateSnapshot());
    }

    static string FindOccupant(List<BattleUnitSnapshot> units, int c, int r)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].C == c && units[i].R == r)
            {
                return units[i].RuntimeUnitId;
            }
        }

        return string.Empty;
    }

    static BattleUnitSnapshot ActorUnit(
        int c,
        int r,
        bool movedThisTurn = false,
        bool usedSkillThisTurn = false,
        bool waited = false,
        bool isRange = false)
    {
        return BaseUnit("team-0-slot-0", 0, 0, c, r, movedThisTurn, usedSkillThisTurn, waited, isRange);
    }

    static BattleUnitSnapshot EnemyUnit(string runtimeUnitId, int teamIndex, int rosterIndex, int c, int r)
    {
        return BaseUnit(runtimeUnitId, teamIndex, rosterIndex, c, r, false, false, false, false);
    }

    static BattleUnitSnapshot BaseUnit(
        string runtimeUnitId,
        int teamIndex,
        int rosterIndex,
        int c,
        int r,
        bool movedThisTurn,
        bool usedSkillThisTurn,
        bool waited,
        bool isRange)
    {
        return new BattleUnitSnapshot
        {
            RuntimeUnitId = runtimeUnitId,
            TeamIndex = teamIndex,
            RosterIndexWithinTeam = rosterIndex,
            UnitName = "Unit",
            UnitType = "Unit",
            C = c,
            R = r,
            Amount = 5,
            TempHP = 20,
            BaseHP = 20,
            Attack = 5,
            Defense = 4,
            MovementSpeed = 3,
            Initiative = 7,
            MinDamage = 2,
            MaxDamage = 4,
            IsAlive = true,
            IsRange = isRange,
            Waited = waited,
            Moved = false,
            MovedThisTurn = movedThisTurn,
            UsedSkillThisTurn = usedSkillThisTurn,
            UsedSkillIdsThisTurn = new List<string>(),
            CanMoveAfterSkillThisTurn = false,
            CooldownsBySlot = new List<int> { 0 },
            SkillIdsBySlot = new List<string> { "BattleCry" },
            Statuses = new List<BattleStatusSnapshot>()
        };
    }

    sealed class TestSkillMetadataProvider : ITacticalAISkillMetadataProvider
    {
        public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
        {
            metadata = new TacticalAISkillMetadata
            {
                SkillId = skillId ?? string.Empty,
                IsPassive = false,
                CanUseAfterMove = false,
                CanMoveAfterSkill = false,
                IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
            };
            return true;
        }
    }
}
