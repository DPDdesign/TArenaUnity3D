#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class TacticalAICandidateGeneratorTests
{
    [Test]
    public void WaitAndDefend_AreAvailableOnlyBeforeMovementAndNonToggleSkillUse()
    {
        BattleSnapshot openingSnapshot = CreateSnapshot(
            ActorUnit(0, 0, movedThisTurn: false, usedSkillThisTurn: false, waited: false),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        List<TacticalAIActionIntent> openingCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            openingSnapshot,
            CreateOptions(),
            new TestSkillMetadataProvider());

        Assert.That(HasAction(openingCandidates, TacticalAIActionType.Wait), Is.True);
        Assert.That(HasAction(openingCandidates, TacticalAIActionType.Defend), Is.True);

        BattleSnapshot movedSnapshot = CreateSnapshot(
            ActorUnit(0, 0, movedThisTurn: true, usedSkillThisTurn: false, waited: false),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        List<TacticalAIActionIntent> movedCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            movedSnapshot,
            CreateOptions(),
            new TestSkillMetadataProvider());

        Assert.That(HasAction(movedCandidates, TacticalAIActionType.Wait), Is.False);
        Assert.That(HasAction(movedCandidates, TacticalAIActionType.Defend), Is.False);

        BattleSnapshot usedSkillSnapshot = CreateSnapshot(
            ActorUnit(0, 0, movedThisTurn: false, usedSkillThisTurn: true, waited: false),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        List<TacticalAIActionIntent> usedSkillCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            usedSkillSnapshot,
            CreateOptions(),
            new TestSkillMetadataProvider());

        Assert.That(HasAction(usedSkillCandidates, TacticalAIActionType.Wait), Is.False);
        Assert.That(HasAction(usedSkillCandidates, TacticalAIActionType.Defend), Is.False);
    }

    [Test]
    public void MoveCandidates_ExcludeOccupiedDestinations()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0),
            AllyUnit("team-0-slot-1", 0, 1, 1, 0));

        List<TacticalAIActionIntent> candidates = TacticalAICandidateGenerator.GenerateCandidates(
            snapshot,
            CreateOptions(),
            new TestSkillMetadataProvider());

        Assert.That(
            ContainsMoveDestination(candidates, 1, 0),
            Is.False,
            "Occupied allied hex should not be emitted as a move destination.");
    }

    [Test]
    public void MoveAndAttackCandidates_IncludeCurrentHexWhenAlreadyAdjacent()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 1, 0));

        List<TacticalAIActionIntent> candidates = TacticalAICandidateGenerator.GenerateCandidates(
            snapshot,
            CreateOptions(),
            new TestSkillMetadataProvider());

        TacticalAIActionIntent intent = FindAction(candidates, TacticalAIActionType.MoveAndAttack, "team-1-slot-0");
        Assert.That(intent, Is.Not.Null);
        Assert.That(intent.DestinationHex, Is.Not.Null);
        Assert.That(intent.DestinationHex.C, Is.EqualTo(0));
        Assert.That(intent.DestinationHex.R, Is.EqualTo(0));
    }

    [Test]
    public void BasicRangedAttackCandidates_TargetEnemyUnitsOnly()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0),
            AllyUnit("team-0-slot-1", 0, 1, 0, 1));

        List<TacticalAIActionIntent> candidates = TacticalAICandidateGenerator.GenerateCandidates(
            snapshot,
            CreateOptions(),
            new TestSkillMetadataProvider());

        List<TacticalAIActionIntent> rangedCandidates = FindActions(candidates, TacticalAIActionType.BasicRangedAttack);
        Assert.That(rangedCandidates.Count, Is.EqualTo(1));
        Assert.That(rangedCandidates[0].TargetUnitId, Is.EqualTo("team-1-slot-0"));
    }

    [Test]
    public void PassiveAndCooldownBlockedSkills_AreNotCandidates()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "BattleCry", "StoneSkinPassive", "Dash" };
        actor.CooldownsBySlot = new List<int> { 0, 0, 2 };

        TestSkillMetadataProvider metadataProvider = new TestSkillMetadataProvider();
        metadataProvider.Add("BattleCry", isPassive: false, canUseAfterMove: false, canMoveAfterSkill: false);
        metadataProvider.Add("StoneSkinPassive", isPassive: true, canUseAfterMove: false, canMoveAfterSkill: false);
        metadataProvider.Add("Dash", isPassive: false, canUseAfterMove: true, canMoveAfterSkill: true);

        List<TacticalAIActionIntent> candidates = TacticalAICandidateGenerator.GenerateCandidates(
            CreateSnapshot(actor, EnemyUnit("team-1-slot-0", 1, 0, 2, 0)),
            CreateOptions(),
            metadataProvider);

        List<TacticalAIActionIntent> skillCandidates = FindActions(candidates, TacticalAIActionType.Skill);
        Assert.That(skillCandidates.Count, Is.EqualTo(1));
        Assert.That(skillCandidates[0].SkillSlot, Is.EqualTo(0));
        Assert.That(skillCandidates[0].SkillId, Is.EqualTo("BattleCry"));
    }

    [Test]
    public void CandidateOrdering_IsStableAcrossEquivalentSnapshots()
    {
        BattleSnapshot first = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-1", 1, 1, 3, 1),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        BattleSnapshot second = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0),
            EnemyUnit("team-1-slot-1", 1, 1, 3, 1));

        List<TacticalAIActionIntent> firstCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            first,
            CreateOptions(),
            new TestSkillMetadataProvider());

        List<TacticalAIActionIntent> secondCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            second,
            CreateOptions(),
            new TestSkillMetadataProvider());

        Assert.That(GetStableKeys(firstCandidates), Is.EqualTo(GetStableKeys(secondCandidates)));
    }

    static TacticalAICandidateGenerationOptions CreateOptions()
    {
        return new TacticalAICandidateGenerationOptions
        {
            MaxCandidatesPerActionType = 16,
            MaxMoveCandidates = 16,
            MaxAttackCandidates = 16,
            MaxSkillCandidates = 16
        };
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

    static BattleUnitSnapshot AllyUnit(string runtimeUnitId, int teamIndex, int rosterIndex, int c, int r)
    {
        return BaseUnit(runtimeUnitId, teamIndex, rosterIndex, c, r, false, false, false, false);
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

    static bool HasAction(List<TacticalAIActionIntent> actions, TacticalAIActionType actionType)
    {
        return FindAction(actions, actionType, null) != null;
    }

    static TacticalAIActionIntent FindAction(
        List<TacticalAIActionIntent> actions,
        TacticalAIActionType actionType,
        string targetUnitId)
    {
        if (actions == null)
        {
            return null;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            TacticalAIActionIntent action = actions[i];
            if (action.ActionType != actionType)
            {
                continue;
            }

            if (targetUnitId == null || action.TargetUnitId == targetUnitId)
            {
                return action;
            }
        }

        return null;
    }

    static List<TacticalAIActionIntent> FindActions(List<TacticalAIActionIntent> actions, TacticalAIActionType actionType)
    {
        List<TacticalAIActionIntent> matches = new List<TacticalAIActionIntent>();
        if (actions == null)
        {
            return matches;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionType == actionType)
            {
                matches.Add(actions[i]);
            }
        }

        return matches;
    }

    static bool ContainsMoveDestination(List<TacticalAIActionIntent> actions, int c, int r)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            TacticalAIActionIntent action = actions[i];
            if (action.ActionType == TacticalAIActionType.Move &&
                action.DestinationHex != null &&
                action.DestinationHex.C == c &&
                action.DestinationHex.R == r)
            {
                return true;
            }
        }

        return false;
    }

    static List<string> GetStableKeys(List<TacticalAIActionIntent> actions)
    {
        List<string> keys = new List<string>();
        for (int i = 0; i < actions.Count; i++)
        {
            keys.Add(actions[i].StableOrderKey);
        }

        return keys;
    }

    sealed class TestSkillMetadataProvider : ITacticalAISkillMetadataProvider
    {
        readonly Dictionary<string, TacticalAISkillMetadata> metadataBySkillId =
            new Dictionary<string, TacticalAISkillMetadata>();

        public void Add(string skillId, bool isPassive, bool canUseAfterMove, bool canMoveAfterSkill)
        {
            metadataBySkillId[skillId] = new TacticalAISkillMetadata
            {
                SkillId = skillId,
                IsPassive = isPassive,
                CanUseAfterMove = canUseAfterMove,
                CanMoveAfterSkill = canMoveAfterSkill,
                IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
            };
        }

        public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
        {
            if (metadataBySkillId.TryGetValue(skillId, out metadata))
            {
                return true;
            }

            metadata = new TacticalAISkillMetadata
            {
                SkillId = skillId,
                IsPassive = false,
                CanUseAfterMove = false,
                CanMoveAfterSkill = false,
                IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
            };
            return true;
        }
    }
}
#endif
