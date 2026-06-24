using System.Collections.Generic;
using NUnit.Framework;

public class TacticalAIProfileTests
{
    [Test]
    public void RuntimeNormalDefault_UsesRecommendedProfileValues()
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);

        Assert.That(profile.DifficultyName, Is.EqualTo("Normal"));
        Assert.That(profile.SearchDepthPlies, Is.EqualTo(3));
        Assert.That(profile.DecisionWatchdogMs, Is.EqualTo(300));
        Assert.That(profile.OwnActionBeam, Is.EqualTo(8));
        Assert.That(profile.EnemyResponseBeam, Is.EqualTo(5));
        Assert.That(profile.RequireOpponentResponseWhenReachable, Is.True);
        Assert.That(profile.ProfileHash, Is.Not.Empty);
    }

    [Test]
    public void NormalProfileAsset_ExistsWithRecommendedDefaults()
    {
        TacticalAIProfile asset = TacticalAIProfileCatalog.LoadNormalProfileAsset();

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.DifficultyName, Is.EqualTo("Normal"));
        Assert.That(asset.SearchDepthPlies, Is.EqualTo(3));
        Assert.That(asset.DecisionWatchdogMs, Is.EqualTo(300));
        Assert.That(asset.OwnActionBeam, Is.EqualTo(8));
        Assert.That(asset.EnemyResponseBeam, Is.EqualTo(5));
        Assert.That(asset.RequireOpponentResponseWhenReachable, Is.True);
    }

    [Test]
    public void ProfileHash_ChangesWhenBudgetChanges()
    {
        TacticalAIResolvedProfile baseline = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIResolvedProfile changed = baseline.Clone();
        changed.MaxMoveCandidates += 1;
        changed.ProfileHash = TacticalAIProfileHasher.ComputeHash(changed);

        Assert.That(changed.ProfileHash, Is.Not.EqualTo(baseline.ProfileHash));
    }

    [Test]
    public void ProfileHash_ChangesWhenWeightsChange()
    {
        TacticalAIResolvedProfile baseline = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIResolvedProfile changed = baseline.Clone();
        changed.ScoringWeights.ProgressTempo += 0.5f;
        changed.ProfileHash = TacticalAIProfileHasher.ComputeHash(changed);

        Assert.That(changed.ProfileHash, Is.Not.EqualTo(baseline.ProfileHash));
    }

    [Test]
    public void FixedBudget_RemainsProfileDrivenEvenWhenExecutionIsFast()
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIFixedBudget budget = new TacticalAIFixedBudget(profile);

        Assert.That(budget.CanSearchPly(3, 0), Is.True);
        Assert.That(budget.CanSearchPly(4, 0), Is.False);
        Assert.That(budget.ClampBeamWidth(false, 99), Is.EqualTo(8));
        Assert.That(budget.ClampBeamWidth(true, 99), Is.EqualTo(5));
        Assert.That(budget.ClampCandidateCount(TacticalAICandidateBucket.Skill, 99), Is.EqualTo(8));
        Assert.That(budget.ClampCandidateCount(TacticalAICandidateBucket.Move, 99), Is.EqualTo(8));
    }

    [Test]
    public void FixedBudget_WatchdogReturnsBestCompletedPlanOrFallback()
    {
        TacticalAIFixedBudget budget = new TacticalAIFixedBudget(TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null));

        TacticalAIPlanCacheValue<string> completedPlan = new TacticalAIPlanCacheValue<string>(new[] { "skill" }, 10f, 2);
        TacticalAIPlanCacheValue<string> fallbackPlan = new TacticalAIPlanCacheValue<string>(new[] { "wait" }, 1f, 0);

        Assert.That(
            budget.ResolveWatchdogFallback(300, completedPlan, fallbackPlan),
            Is.SameAs(completedPlan));
        Assert.That(
            budget.ResolveWatchdogFallback(300, null, fallbackPlan),
            Is.SameAs(fallbackPlan));
    }

    [Test]
    public void PlanCache_HitsWhenSnapshotAndProfileMatch()
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIPlanCache<string> cache = new TacticalAIPlanCache<string>();
        BattleSnapshot snapshot = BuildSnapshot("team-0-slot-0", 10, 0, 0);

        cache.StoreAdvisoryPlan(snapshot, profile, new[] { "attack", "wait" }, 15f, 3);

        TacticalAIPlanCacheValue<string> cachedPlan;
        bool found = cache.TryGetAdvisoryPlan(snapshot, profile, out cachedPlan);

        Assert.That(found, Is.True);
        Assert.That(cachedPlan, Is.Not.Null);
        Assert.That(cachedPlan.CompletedDepth, Is.EqualTo(3));
        Assert.That(cachedPlan.OrderedActionIntents, Is.EqualTo(new List<string> { "attack", "wait" }));
    }

    [Test]
    public void PlanCache_RejectsSnapshotHashMismatch()
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIPlanCache<string> cache = new TacticalAIPlanCache<string>();
        BattleSnapshot original = BuildSnapshot("team-0-slot-0", 10, 0, 0);
        BattleSnapshot changed = BuildSnapshot("team-0-slot-0", 9, 0, 0);

        cache.StoreAdvisoryPlan(original, profile, new[] { "attack" }, 12f, 3);

        TacticalAIPlanCacheValue<string> cachedPlan;
        bool found = cache.TryGetAdvisoryPlan(changed, profile, out cachedPlan);

        Assert.That(found, Is.False);
        Assert.That(cachedPlan, Is.Null);
    }

    [Test]
    public void PlanCache_RejectsProfileHashMismatch()
    {
        TacticalAIResolvedProfile originalProfile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIResolvedProfile changedProfile = originalProfile.Clone();
        changedProfile.ActionTypeBiases.Skill += 0.25f;
        changedProfile.ProfileHash = TacticalAIProfileHasher.ComputeHash(changedProfile);

        TacticalAIPlanCache<string> cache = new TacticalAIPlanCache<string>();
        BattleSnapshot snapshot = BuildSnapshot("team-0-slot-0", 10, 0, 0);

        cache.StoreAdvisoryPlan(snapshot, originalProfile, new[] { "attack" }, 12f, 3);

        TacticalAIPlanCacheValue<string> cachedPlan;
        bool found = cache.TryGetAdvisoryPlan(snapshot, changedProfile, out cachedPlan);

        Assert.That(found, Is.False);
        Assert.That(cachedPlan, Is.Null);
    }

    private static BattleSnapshot BuildSnapshot(string activeUnitId, int amount, int c, int r)
    {
        return BattleSnapshotBuilder.Build(
            2,
            2,
            new[]
            {
                new BattleHexSnapshot
                {
                    C = 0,
                    R = 0,
                    IsWalkable = true,
                    OccupyingUnitId = c == 0 && r == 0 ? "team-0-slot-0" : string.Empty
                },
                new BattleHexSnapshot
                {
                    C = 1,
                    R = 0,
                    IsWalkable = true,
                    OccupyingUnitId = c == 1 && r == 0 ? "team-0-slot-0" : string.Empty
                }
            },
            new[]
            {
                new BattleUnitSnapshot
                {
                    RuntimeUnitId = "team-0-slot-0",
                    TeamIndex = 0,
                    RosterIndexWithinTeam = 0,
                    UnitName = "Rusher",
                    UnitType = "Rusher",
                    C = c,
                    R = r,
                    Amount = amount,
                    TempHP = 30,
                    BaseHP = 30,
                    Attack = 5,
                    Defense = 3,
                    MovementSpeed = 6,
                    Initiative = 7,
                    MinDamage = 2,
                    MaxDamage = 4,
                    IsAlive = true,
                    CooldownsBySlot = new List<int> { 0, 0 },
                    SkillIdsBySlot = new List<string> { "Slash", "Guard" },
                    UsedSkillIdsThisTurn = new List<string>()
                }
            },
            activeUnitId,
            new BattleTurnStateSnapshot
            {
                RoundNumber = 1,
                IsResolvingNewTurnSequence = false,
                IsActionBlocking = false,
                ActiveActionKind = string.Empty
            });
    }
}
