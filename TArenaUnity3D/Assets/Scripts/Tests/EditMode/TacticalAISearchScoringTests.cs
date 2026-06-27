#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

        Assert.That(plan.BestAction, Is.Not.Null);
        Assert.That(plan.BestAction.Action, Is.Not.Null);
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

        List<BattleAction> candidates = BattleActionRules.GenerateLegalActions(
            snapshot,
            profile,
            new TestSkillMetadataProvider());

        Assert.That(CountActions(candidates, BattleActionKind.Skill), Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void SearchCandidateExpansion_EmitsBattleActionSkillPayloadAndPreview()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        profile.MaxCandidatesPerActionType = 8;
        profile.MaxSkillCandidates = 8;

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Bolt" }),
            Unit("team-1-slot-0", 1, 0, 2, 0));

        List<BattleAction> candidates = BattleActionRules.GenerateLegalActions(
            snapshot,
            profile,
            new TestSkillMetadataProvider());

        BattleAction skillCandidate = FindAction(candidates, BattleActionKind.Skill);
        Assert.That(skillCandidate, Is.Not.Null);
        Assert.That(skillCandidate.SkillCast, Is.Not.Null);
        Assert.That(skillCandidate.SkillCast.SkillId, Is.EqualTo("Bolt"));
        Assert.That(skillCandidate.SkillCast.PrimaryTargetUnitId, Is.EqualTo("team-1-slot-0"));

        BattleActionResult preview = BattleActionRules.Apply(snapshot, skillCandidate);
        Assert.That(preview, Is.Not.Null);
        Assert.That(preview.Events.Exists(e => e.EventType == BattleActionResultEventType.DamageApplied), Is.True);
    }

    [Test]
    public void SearchCandidateExpansion_DropsAreaDamageSkillWhenNoUnitWouldBeHit()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        profile.MaxCandidatesPerActionType = 8;
        profile.MaxSkillCandidates = 8;

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Chope" }),
            Unit("team-1-slot-0", 1, 0, 4, 4));

        List<BattleAction> candidates = BattleActionRules.GenerateLegalActions(
            snapshot,
            profile,
            new TestSkillMetadataProvider());

        Assert.That(CountActions(candidates, BattleActionKind.Skill), Is.EqualTo(0));
    }

    [Test]
    public void SearchCandidateExpansion_KeepsAreaDamageSkillWhenUnitWouldBeHit()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        profile.MaxCandidatesPerActionType = 8;
        profile.MaxSkillCandidates = 8;

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Chope" }),
            Unit("team-1-slot-0", 1, 0, 0, 1));

        List<BattleAction> candidates = BattleActionRules.GenerateLegalActions(
            snapshot,
            profile,
            new TestSkillMetadataProvider());

        Assert.That(CountActions(candidates, BattleActionKind.Skill), Is.EqualTo(1));
    }

    [Test]
    public void SnapshotSimulator_RageStatus_UsesCurrentDefenseForAttackAndDefenseModifiers()
    {
        BattleUnitSnapshot actor = ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Rage" });
        actor.Defense = 10;
        BattleSnapshot snapshot = CreateSnapshot(actor);
        BattleAction action = new BattleAction
        {
            ActorUnitId = actor.RuntimeUnitId,
            ActionKind = BattleActionKind.Skill,
            SkillId = "Rage",
            SkillSlot = 0,
            TurnCost = 1,
            EndsTurn = true,
            SkillCast = new SkillCast
            {
                ActorUnitId = actor.RuntimeUnitId,
                SkillId = "Rage",
                CooldownTurns = 0,
                ConsumesTurn = true,
                Effects = new[]
                {
                    new SkillEffect
                    {
                        effectType = SkillEffectType.ApplyStatus,
                        targetSource = SkillEffectTargetSource.Actor,
                        statusId = "Rage",
                        durationTurns = 2
                    }
                }
            }
        };

        BattleSnapshot simulated = TacticalAISnapshotSimulator.ApplyAction(snapshot, action);
        BattleUnitSnapshot simulatedActor = FindUnit(simulated, actor.RuntimeUnitId);

        Assert.That(simulatedActor.Statuses.Count, Is.EqualTo(1));
        Assert.That(simulatedActor.Statuses[0].AttackModifier, Is.EqualTo(5));
        Assert.That(simulatedActor.Statuses[0].DefenseModifier, Is.EqualTo(-10));
    }

    [Test]
    public void SnapshotSimulator_Shapeshift_SwapsMovementSpeedAndInitiative()
    {
        BattleUnitSnapshot actor = ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Shapeshift" });
        actor.MovementSpeed = 4;
        actor.Initiative = 9;
        BattleSnapshot snapshot = CreateSnapshot(actor);
        BattleAction action = new BattleAction
        {
            ActorUnitId = actor.RuntimeUnitId,
            ActionKind = BattleActionKind.Skill,
            SkillId = "Shapeshift",
            SkillSlot = 0,
            TurnCost = 0,
            EndsTurn = false,
            SkillCast = new SkillCast
            {
                ActorUnitId = actor.RuntimeUnitId,
                SkillId = "Shapeshift",
                CooldownTurns = 0,
                ConsumesTurn = false,
                CanMoveAfterUse = true,
                Effects = new[]
                {
                    new SkillEffect
                    {
                        effectType = SkillEffectType.ToggleStance,
                        targetSource = SkillEffectTargetSource.Actor
                    }
                }
            }
        };

        BattleSnapshot simulated = TacticalAISnapshotSimulator.ApplyAction(snapshot, action);
        BattleUnitSnapshot simulatedActor = FindUnit(simulated, actor.RuntimeUnitId);

        Assert.That(simulatedActor.MovementSpeed, Is.EqualTo(9));
        Assert.That(simulatedActor.Initiative, Is.EqualTo(4));
    }

    [Test]
    public void SearchPlan_SkillActionCarriesBattleActionUsePayload()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        profile.ActionTypeBiases.Skill = 1000f;

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Bolt" }),
            Unit("team-1-slot-0", 1, 0, 2, 0));

        TacticalAISearchPlan plan = TacticalAISearchEngine.Search(snapshot, profile, new TestSkillMetadataProvider());

        Assert.That(plan.BestAction, Is.Not.Null);
        Assert.That(plan.BestAction.ActionType, Is.EqualTo(TacticalAIActionType.Skill));
        Assert.That(plan.BestAction.Action, Is.Not.Null);
        Assert.That(plan.BestAction.Action.SkillCast, Is.Not.Null);
        Assert.That(plan.BestAction.Use, Is.Not.Null);
        Assert.That(plan.BestAction.Use.ActionKind, Is.EqualTo(BattleActionKind.Skill));
    }

    [Test]
    public void Search_DoesNotDropLegalSkillWhenWatchdogExpiresBeforeDeepSearch()
    {
        TacticalAIResolvedProfile profile = TestProfile();
        profile.DecisionWatchdogMs = 0;
        profile.OwnActionBeam = 1;
        profile.RequireOpponentResponseWhenReachable = false;
        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, skillIds: new List<string> { "Bolt" }),
            Unit("team-1-slot-0", 1, 0, 2, 0));

        TacticalAISearchPlan plan = TacticalAISearchEngine.Search(snapshot, profile, new TestSkillMetadataProvider());

        Assert.That(plan.WatchdogExpired, Is.True);
        Assert.That(plan.BestAction, Is.Not.Null);
        Assert.That(plan.BestAction.ActionType, Is.EqualTo(TacticalAIActionType.Skill));
        Assert.That(plan.BestAction.Action, Is.Not.Null);
        Assert.That(plan.BestAction.Action.SkillCast, Is.Not.Null);
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
        profile.SearchDepthPlies = 1;
        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);

        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit("team-0-slot-0", 0, 0, 0, 0, isRange: true, minDamage: 5, maxDamage: 5, skillIds: EmptySkills()),
            Unit("team-1-slot-0", 1, 0, 2, 0, amount: 1, tempHp: 5),
            Unit("team-1-slot-1", 1, 1, 3, 0, amount: 8, tempHp: 20));

        TacticalAISearchPlan plan = TacticalAISearchEngine.Search(snapshot, profile, new TestSkillMetadataProvider());

        Assert.That(plan.BestAction, Is.Not.Null);
        Assert.That(plan.BestAction.Action, Is.Not.Null);
        Assert.That(plan.BestAction.Action.PrimaryTargetUnitId, Is.EqualTo("team-1-slot-0"));
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

        Assert.That(normalPlan.BestAction, Is.Not.Null);
        Assert.That(waitBiasedPlan.BestAction, Is.Not.Null);
        Assert.That(waitBiasedPlan.BestAction.ActionKind, Is.EqualTo(BattleActionKind.Wait));
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

    static int CountActions(List<BattleAction> actions, BattleActionKind actionKind)
    {
        int count = 0;
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionKind == actionKind)
            {
                count++;
            }
        }

        return count;
    }

    static BattleAction FindAction(List<BattleAction> actions, BattleActionKind actionKind)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionKind == actionKind)
            {
                return actions[i];
            }
        }

        return null;
    }

    static BattleUnitSnapshot FindUnit(BattleSnapshot snapshot, string runtimeUnitId)
    {
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.Units, Is.Not.Null);
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && unit.RuntimeUnitId == runtimeUnitId)
            {
                return unit;
            }
        }

        Assert.Fail("Missing unit " + runtimeUnitId);
        return null;
    }

    sealed class TestSkillMetadataProvider : ITacticalAISkillMetadataProvider, ITacticalAISkillDefinitionProvider, ITacticalAISkillSpecProvider
    {
        readonly Dictionary<string, SkillDefinitionAsset> definitions =
            new Dictionary<string, SkillDefinitionAsset>();

        public TestSkillMetadataProvider()
        {
            Add("BattleCry");
            Add("Bolt");
            Add("Blast");
            Add("Mark");
            AddAreaAroundCasterDamage("Chope");
        }

        void Add(string skillId)
        {
            SkillDefinitionAsset skill = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            skill.Configure(skillId, "Active", string.Empty, string.Empty);
            skill.ConfigureRules(
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Active,
                    consumesTurn = true
                },
                new TargetingRuleData
                {
                    targetFamily = SkillTargetFamily.UnitTarget,
                    targetRoles = new[] { SkillTargetRole.EnemyUnitHex },
                    targetCount = 1,
                    requiresWalkable = true
                },
                new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.DirectUnit },
                new[]
                {
                    new SkillEffect
                    {
                        effectType = SkillEffectType.Damage,
                        targetSource = SkillEffectTargetSource.PrimaryUnit,
                        damageMode = SkillDamageMode.BasicAttackDamage,
                        damageScale = 1f
                    }
                });
            definitions[skillId] = skill;
        }

        void AddAreaAroundCasterDamage(string skillId)
        {
            SkillDefinitionAsset skill = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            skill.Configure(skillId, "Active", string.Empty, string.Empty);
            skill.ConfigureRules(
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Active,
                    consumesTurn = true
                },
                new TargetingRuleData
                {
                    targetFamily = SkillTargetFamily.Self,
                    targetRoles = new[] { SkillTargetRole.ActorSelf },
                    targetCount = 0,
                    requiresWalkable = true
                },
                new ResolutionRuleData
                {
                    resolutionFamily = SkillResolutionFamily.AreaAroundCaster,
                    radius = 1
                },
                new[]
                {
                    new SkillEffect
                    {
                        effectType = SkillEffectType.Damage,
                        targetSource = SkillEffectTargetSource.AffectedUnits,
                        damageMode = SkillDamageMode.BasicAttackDamage,
                        damageScale = 1f
                    }
                });
            definitions[skillId] = skill;
        }

        public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
        {
            metadata = new TacticalAISkillMetadata
            {
                SkillId = skillId ?? string.Empty,
                IsPassive = false,
                CanUseAfterMove = false,
                CanMoveAfterSkill = false,
                IsRepeatableToggle = BattleActionSkillUtility.IsRepeatableToggleSkillId(skillId)
            };
            return true;
        }

        public bool TryGetSkillDefinition(string skillId, out SkillDefinitionAsset definition)
        {
            return definitions.TryGetValue(skillId ?? string.Empty, out definition) && definition != null;
        }

        public bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec)
        {
            SkillDefinitionAsset definition;
            if (TryGetSkillDefinition(skillId, out definition) == false)
            {
                spec = null;
                return false;
            }

            spec = SkillDefinitionSpec.FromAsset(definition);
            return spec != null;
        }
    }
}
#endif
