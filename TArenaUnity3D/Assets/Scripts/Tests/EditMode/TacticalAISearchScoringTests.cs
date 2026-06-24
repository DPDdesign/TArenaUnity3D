using System.Collections.Generic;
using NUnit.Framework;

public class TacticalAISearchScoringTests
{
    [Test]
    public void Search_CompletesThreePliesAndCoversOpponentResponse()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, initiative: 8, isRange: true, skillIds: EmptySkills()),
            Unit("team-1-slot-0", 1, 0, 4, 0, initiative: 7, amount: 8),
            Unit("team-0-slot-1", 0, 1, 0, 1, initiative: 6, amount: 5));

        TacticalAISearchPlan plan = TacticalAISearchEngine.Search(snapshot, profile, new TestSkillMetadataProvider());

        Assert.That(plan.BestIntent, Is.Not.Null);
        Assert.That(plan.CompletedDepth, Is.EqualTo(3));
        Assert.That(plan.OpponentResponseReachable, Is.True);
        Assert.That(plan.CoveredOpponentResponse, Is.True);
    }

    [Test]
    public void SearchCandidateExpansion_StaysBoundedAfterSkillTargetExpansion()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        profile.MaxCandidatesPerActionType = 2;
        profile.MaxSkillCandidates = 2;

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Bolt", "Blast", "Mark" }),
            Unit("team-1-slot-0", 1, 0, 2, 0),
            Unit("team-1-slot-1", 1, 1, 3, 0),
            Unit("team-1-slot-2", 1, 2, 4, 0),
            Unit("team-1-slot-3", 1, 3, 4, 1));

        List<TacticalAIActionIntent> candidates = TacticalAISearchCandidateExpander.BuildSearchCandidates(
            snapshot,
            profile,
            new TestSkillMetadataProvider());

        Assert.That(CountActions(candidates, TacticalAIActionType.Skill), Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void TurnOrderEstimator_SupportsSameSideConsecutiveTurns()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, initiative: 9, moved: true, skillIds: EmptySkills()),
            Unit("team-0-slot-1", 0, 1, 0, 1, initiative: 8),
            Unit("team-1-slot-0", 1, 0, 4, 0, initiative: 3));

        string nextUnitId = BattleSnapshotTurnOrderEstimator.EstimateNextActiveUnitId(snapshot);

        Assert.That(nextUnitId, Is.EqualTo("team-0-slot-1"));
    }

    [Test]
    public void TurnOrderEstimator_SupportsOpponentConsecutiveTurns()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, initiative: 4, moved: true, skillIds: EmptySkills()),
            Unit("team-1-slot-0", 1, 0, 4, 0, initiative: 9, moved: true),
            Unit("team-1-slot-1", 1, 1, 4, 1, initiative: 8),
            Unit("team-0-slot-1", 0, 1, 0, 1, initiative: 3));

        string nextUnitId = BattleSnapshotTurnOrderEstimator.EstimateNextActiveUnitId(snapshot);

        Assert.That(nextUnitId, Is.EqualTo("team-1-slot-1"));
    }

    [Test]
    public void AverageDamagePrediction_IsDeterministicAndDoesNotMutateUnits()
    {
        BattleUnitSnapshot attacker = ActorUnit("team-0-slot-0", 0, 0, 0, 0, amount: 5, skillIds: EmptySkills());
        BattleUnitSnapshot defender = Unit("team-1-slot-0", 1, 0, 2, 0, amount: 4);
        int attackerAmountBefore = attacker.Amount;
        int defenderTempHpBefore = defender.TempHP;

        int first = TacticalAIDamagePredictor.PredictAverageDamage(attacker, defender);
        int second = TacticalAIDamagePredictor.PredictAverageDamage(attacker, defender);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.GreaterThan(0));
        Assert.That(attacker.Amount, Is.EqualTo(attackerAmountBefore));
        Assert.That(defender.TempHP, Is.EqualTo(defenderTempHpBefore));
    }

    [Test]
    public void Search_PrefersImmediateKillTarget()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, isRange: true, minDamage: 5, maxDamage: 5, skillIds: EmptySkills()),
            Unit("team-1-slot-0", 1, 0, 2, 0, amount: 1, tempHp: 5),
            Unit("team-1-slot-1", 1, 1, 3, 0, amount: 8, tempHp: 20));

        TacticalAISearchPlan plan = TacticalAISearchEngine.Search(snapshot, profile, new TestSkillMetadataProvider());

        Assert.That(plan.BestIntent, Is.Not.Null);
        Assert.That(plan.BestIntent.TargetUnitId, Is.EqualTo("team-1-slot-0"));
    }

    [Test]
    public void ProfileBiases_ChangeSelectedActionWithoutChangingLegality()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, isRange: true, minDamage: 1, maxDamage: 1, skillIds: EmptySkills()),
            Unit("team-1-slot-0", 1, 0, 3, 0, amount: 8));

        TacticalAIResolvedProfile normal = TestProfile();
        TacticalAISearchPlan normalPlan = TacticalAISearchEngine.Search(snapshot, normal, new TestSkillMetadataProvider());

        TacticalAIResolvedProfile waitBiased = TestProfile();
        waitBiased.ScoringWeights.EnemyValueRemoved = 0f;
        waitBiased.ScoringWeights.EnemyStackKillBonus = 0f;
        waitBiased.ScoringWeights.DamageEfficiency = 0f;
        waitBiased.ScoringWeights.ProgressTempo = 0f;
        waitBiased.ScoringWeights.PositionSafety = 0f;
        waitBiased.ScoringWeights.ThreatControl = 0f;
        waitBiased.ActionTypeBiases.Attack = -1000f;
        waitBiased.ActionTypeBiases.Wait = 1000f;
        waitBiased.ProfileHash = TacticalAIProfileHasher.ComputeHash(waitBiased);

        TacticalAISearchPlan waitBiasedPlan = TacticalAISearchEngine.Search(snapshot, waitBiased, new TestSkillMetadataProvider());

        Assert.That(normalPlan.BestIntent, Is.Not.Null);
        Assert.That(waitBiasedPlan.BestIntent, Is.Not.Null);
        Assert.That(waitBiasedPlan.BestIntent.ActionType, Is.EqualTo(TacticalAIActionType.Wait));
    }

    static TacticalAIResolvedProfile TestProfile()
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        profile.OwnActionBeam = 8;
        profile.EnemyResponseBeam = 5;
        profile.MaxCandidatesPerActionType = 8;
        profile.MaxSkillCandidates = 8;
        profile.MaxMoveCandidates = 8;
        profile.MaxAttackCandidates = 8;
        profile.MaxFallbackCandidates = 4;
        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);
        return profile;
    }

    static List<string> EmptySkills()
    {
        return new List<string>();
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
            new BattleTurnStateSnapshot
            {
                RoundNumber = 1,
                IsActionBlocking = false,
                IsResolvingNewTurnSequence = false
            });
    }

    static string FindOccupant(List<BattleUnitSnapshot> units, int c, int r)
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnitSnapshot unit = units[i];
            if (unit.C == c && unit.R == r && unit.IsAlive && unit.Amount > 0)
            {
                return unit.RuntimeUnitId;
            }
        }

        return string.Empty;
    }

    static BattleUnitSnapshot ActorUnit(
        string runtimeUnitId,
        int teamIndex,
        int rosterIndex,
        int c,
        int r,
        int initiative = 7,
        int amount = 5,
        int tempHp = 20,
        bool moved = false,
        bool isRange = false,
        int minDamage = 2,
        int maxDamage = 4,
        List<string> skillIds = null)
    {
        return Unit(runtimeUnitId, teamIndex, rosterIndex, c, r, initiative, amount, tempHp, moved, isRange, minDamage, maxDamage, skillIds);
    }

    static BattleUnitSnapshot Unit(
        string runtimeUnitId,
        int teamIndex,
        int rosterIndex,
        int c,
        int r,
        int initiative = 5,
        int amount = 5,
        int tempHp = 20,
        bool moved = false,
        bool isRange = false,
        int minDamage = 2,
        int maxDamage = 4,
        List<string> skillIds = null)
    {
        List<string> safeSkillIds = skillIds ?? new List<string> { "BattleCry" };
        List<int> cooldowns = new List<int>();
        for (int i = 0; i < safeSkillIds.Count; i++)
        {
            cooldowns.Add(0);
        }

        return new BattleUnitSnapshot
        {
            RuntimeUnitId = runtimeUnitId,
            TeamIndex = teamIndex,
            RosterIndexWithinTeam = rosterIndex,
            UnitName = "Unit",
            UnitType = "Unit",
            C = c,
            R = r,
            Amount = amount,
            TempHP = tempHp,
            BaseHP = 20,
            Attack = 6,
            Defense = 3,
            MovementSpeed = 3,
            Initiative = initiative,
            MinDamage = minDamage,
            MaxDamage = maxDamage,
            IsAlive = amount > 0,
            IsRange = isRange,
            Waited = false,
            Moved = moved,
            MovedThisTurn = moved,
            UsedSkillThisTurn = false,
            UsedSkillIdsThisTurn = new List<string>(),
            CanMoveAfterSkillThisTurn = false,
            SkillIdsBySlot = safeSkillIds,
            CooldownsBySlot = cooldowns,
            Statuses = new List<BattleStatusSnapshot>()
        };
    }

    static int CountActions(List<TacticalAIActionIntent> actions, TacticalAIActionType actionType)
    {
        int count = 0;
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionType == actionType)
            {
                count++;
            }
        }

        return count;
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
