#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class CombatDamageServiceTests
{
    [Test]
    public void Calculator_BasicAttackIncludesStackAmount()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 3, amount: 5));

        Assert.That(result.CommittedDamage, Is.EqualTo(15));
        Assert.That(result.Forecast.MinDamage, Is.EqualTo(15));
        Assert.That(result.Forecast.MaxDamage, Is.EqualTo(15));
    }

    [Test]
    public void Calculator_InclusiveMaxDamageRollCanCommitMax()
    {
        bool sawMax = false;
        for (int actionIndex = 0; actionIndex < 128; actionIndex++)
        {
            CombatDamageInput input = Input(minDamage: 1, maxDamage: 2, amount: 1, actionIndex: actionIndex);
            if (Calculator().Calculate(input).CommittedDamage == 2)
            {
                sawMax = true;
                break;
            }
        }

        Assert.That(sawMax, Is.True);
    }

    [Test]
    public void Calculator_CommittedDamageIsStableForSameInputs()
    {
        CombatDamageInput input = Input(minDamage: 1, maxDamage: 10, amount: 3, actionIndex: 4, actionSeed: 99);

        int first = Calculator().Calculate(input).CommittedDamage;
        int second = Calculator().Calculate(input).CommittedDamage;

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Calculator_DifferentRollPurposesSeparateAttackAndRetaliationRolls()
    {
        bool foundSeparatedRoll = false;
        for (int actionIndex = 0; actionIndex < 128; actionIndex++)
        {
            CombatDamageInput attack = Input(minDamage: 1, maxDamage: 20, amount: 1, actionIndex: actionIndex);
            attack.RollPurpose = CombatDamageRollPurpose.BasicAttack;
            CombatDamageInput retaliation = Input(minDamage: 1, maxDamage: 20, amount: 1, actionIndex: actionIndex);
            retaliation.RollPurpose = CombatDamageRollPurpose.Retaliation;

            if (Calculator().Calculate(attack).CommittedDamage != Calculator().Calculate(retaliation).CommittedDamage)
            {
                foundSeparatedRoll = true;
                break;
            }
        }

        Assert.That(foundSeparatedRoll, Is.True);
    }

    [Test]
    public void Calculator_AttackVsDefenseScalingAffectsDamage()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 10, amount: 1, attack: 15, defense: 10));

        Assert.That(result.CommittedDamage, Is.EqualTo(12));
    }

    [Test]
    public void Calculator_DefensePenetrationReducesDefenderDefense()
    {
        CombatDamageResult noPenetration = Calculator().Calculate(Input(baseRoll: 10, amount: 1, attack: 10, defense: 20));
        CombatDamageResult penetration = Calculator().Calculate(Input(baseRoll: 10, amount: 1, attack: 10, defense: 20, defensePenetration: 0.5));

        Assert.That(penetration.CommittedDamage, Is.GreaterThan(noPenetration.CommittedDamage));
    }

    [Test]
    public void Calculator_OutgoingDamageReductionReducesDamage()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 100, amount: 1, outgoingReduction: 20));

        Assert.That(result.CommittedDamage, Is.EqualTo(80));
    }

    [Test]
    public void Calculator_IncomingDamageReductionReducesDamage()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 100, amount: 1, incomingReduction: 20));

        Assert.That(result.CommittedDamage, Is.EqualTo(80));
    }

    [Test]
    public void Calculator_FlatDamageReductionCanReduceAtMostSeventyPercent()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 100, amount: 1, flatReduction: 100));

        Assert.That(result.CommittedDamage, Is.EqualTo(30));
    }

    [Test]
    public void Calculator_HatedTargetAddsFiftyPercentDamage()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 100, amount: 1, hatedTargetUnitId: "target"));

        Assert.That(result.CommittedDamage, Is.EqualTo(150));
    }

    [Test]
    public void Calculator_PureDamageContributesAndIsMarkedConsumed()
    {
        CombatDamageResult result = Calculator().Calculate(Input(baseRoll: 100, amount: 1, pureDamage: 7));

        Assert.That(result.CommittedDamage, Is.EqualTo(107));
        Assert.That(result.ConsumesActorPureDamage, Is.True);
    }

    [Test]
    public void Calculator_ForecastMinMaxAndCommittedUseSameFormula()
    {
        CombatDamageResult result = Calculator().Calculate(Input(minDamage: 2, maxDamage: 4, amount: 5));

        Assert.That(result.Forecast.MinDamage, Is.EqualTo(10));
        Assert.That(result.Forecast.MaxDamage, Is.EqualTo(20));
        Assert.That(result.Forecast.CommittedDamage, Is.EqualTo(result.CommittedDamage));
        Assert.That(result.CommittedDamage, Is.InRange(10, 20));
    }

    [Test]
    public void Service_MissingCatalogUnitRejectsDamage()
    {
        BattleSnapshot snapshot = Snapshot(Unit("actor", "Actor"), Unit("target", "Target"));
        TestCombatUnitCatalog catalog = new TestCombatUnitCatalog();
        catalog.Add("Actor", hp: 10, attack: 1, defense: 1, minDamage: 1, maxDamage: 1);
        CombatDamageService service = new CombatDamageService(catalog);

        CombatDamageServiceResult result = service.CalculateDamage(snapshot, Request("actor", "target"));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain("target catalog unit"));
    }

    [Test]
    public void BattleActionRules_MoveAndAttackIncludesRetaliationFromCombatDamageService()
    {
        BattleUnitSnapshot actor = Unit("actor", "Actor", minDamage: 2, maxDamage: 2, amount: 5);
        BattleUnitSnapshot target = Unit("target", "Target", c: 1, r: 0, minDamage: 4, maxDamage: 4, amount: 5);
        BattleSnapshot snapshot = Snapshot(actor, target);
        CombatDamageService service = new CombatDamageService(TestCombatUnitCatalog.FromUnits(snapshot.Units));

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "actor",
                ActionKind = BattleActionKind.MoveAndAttack,
                TargetUnitId = "target"
            },
            snapshot);

        BattleActionResult result = BattleActionRules.Apply(snapshot, validation.Action, service);
        List<BattleActionResultEvent> damageEvents = result.Events.FindAll(e => e.EventType == BattleActionResultEventType.DamageApplied);

        Assert.That(validation.IsValid, Is.True, validation.RejectReason);
        Assert.That(result.IsRejected, Is.False, result.RejectReason);
        Assert.That(damageEvents.Count, Is.EqualTo(2));
        Assert.That(damageEvents[0].ActorUnitId, Is.EqualTo("actor"));
        Assert.That(damageEvents[0].TargetUnitId, Is.EqualTo("target"));
        Assert.That(damageEvents[0].Amount, Is.EqualTo(10));
        Assert.That(damageEvents[1].ActorUnitId, Is.EqualTo("target"));
        Assert.That(damageEvents[1].TargetUnitId, Is.EqualTo("actor"));
        Assert.That(damageEvents[1].Amount, Is.EqualTo(20));
    }

    static CombatDamageCalculator Calculator()
    {
        return new CombatDamageCalculator();
    }

    static CombatDamageInput Input(
        int baseRoll = 1,
        int minDamage = -1,
        int maxDamage = -1,
        int amount = 1,
        int attack = 10,
        int defense = 10,
        double defensePenetration = 0,
        int outgoingReduction = 0,
        int incomingReduction = 0,
        int flatReduction = 0,
        int pureDamage = 0,
        string hatedTargetUnitId = "",
        int actionIndex = 1,
        int actionSeed = 0)
    {
        int resolvedMin = minDamage >= 0 ? minDamage : baseRoll;
        int resolvedMax = maxDamage >= 0 ? maxDamage : baseRoll;
        return new CombatDamageInput
        {
            ActorRuntimeUnitId = "actor",
            TargetRuntimeUnitId = "target",
            ActorAmount = amount,
            ActorAttack = attack,
            ActorMinDamage = resolvedMin,
            ActorMaxDamage = resolvedMax,
            ActorDefensePenetration = defensePenetration,
            ActorOutgoingDamageReductionPercent = outgoingReduction,
            ActorPureDamage = pureDamage,
            ActorHatedTargetUnitId = hatedTargetUnitId,
            DefenderDefense = defense,
            DefenderIncomingDamageReductionPercent = incomingReduction,
            DefenderFlatDamageReduction = flatReduction,
            GameSeed = 123,
            ActionIndex = actionIndex,
            ActionSeed = actionSeed,
            RollPurpose = CombatDamageRollPurpose.BasicAttack,
            DamageScale = 1.0,
            IsStackable = true,
            ConsumeActorPureDamage = true
        };
    }

    static CombatDamageRequest Request(string actorUnitId, string targetUnitId)
    {
        return new CombatDamageRequest
        {
            ActorUnitId = actorUnitId,
            TargetUnitId = targetUnitId,
            ActionIndex = 1,
            RollPurpose = CombatDamageRollPurpose.BasicAttack
        };
    }

    static BattleSnapshot Snapshot(BattleUnitSnapshot actor, BattleUnitSnapshot target)
    {
        List<BattleUnitSnapshot> units = new List<BattleUnitSnapshot> { actor, target };
        List<BattleHexSnapshot> hexes = new List<BattleHexSnapshot>
        {
            Hex(actor.C, actor.R, actor.RuntimeUnitId),
            Hex(target.C, target.R, target.RuntimeUnitId)
        };
        return BattleSnapshotBuilder.Build(3, 3, hexes, units, actor.RuntimeUnitId, new BattleTurnStateSnapshot(), 123, "test-battle", 1);
    }

    static BattleHexSnapshot Hex(int c, int r, string occupant)
    {
        return new BattleHexSnapshot
        {
            C = c,
            R = r,
            IsWalkable = true,
            OccupyingUnitId = occupant
        };
    }

    static BattleUnitSnapshot Unit(
        string runtimeUnitId,
        string catalogUnitId,
        int c = 0,
        int r = 0,
        int minDamage = 1,
        int maxDamage = 1,
        int amount = 1)
    {
        return new BattleUnitSnapshot
        {
            RuntimeUnitId = runtimeUnitId,
            CatalogUnitId = catalogUnitId,
            TeamIndex = runtimeUnitId == "actor" ? 0 : 1,
            RosterIndexWithinTeam = 0,
            UnitName = catalogUnitId,
            UnitType = catalogUnitId,
            C = c,
            R = r,
            Amount = amount,
            TempHP = 10,
            BaseHP = 10,
            Attack = 10,
            Defense = 10,
            MovementSpeed = 3,
            Initiative = 1,
            MinDamage = minDamage,
            MaxDamage = maxDamage,
            IsAlive = true,
            CounterAttackAvailable = true,
            CounterAttacks = 1,
            TempCounterAttacks = 1,
            SkillIdsBySlot = new List<string>(),
            CooldownsBySlot = new List<int>(),
            UsedSkillIdsThisTurn = new List<string>(),
            Statuses = new List<BattleStatusSnapshot>()
        };
    }

    sealed class TestCombatUnitCatalog : ICombatUnitCatalog
    {
        readonly Dictionary<string, CombatUnitCatalogEntry> units = new Dictionary<string, CombatUnitCatalogEntry>();

        public static TestCombatUnitCatalog FromUnits(IEnumerable<BattleUnitSnapshot> snapshots)
        {
            TestCombatUnitCatalog catalog = new TestCombatUnitCatalog();
            foreach (BattleUnitSnapshot unit in snapshots)
            {
                catalog.Add(unit.CatalogUnitId, unit.BaseHP, unit.Attack, unit.Defense, unit.MinDamage, unit.MaxDamage);
            }

            return catalog;
        }

        public void Add(string catalogUnitId, int hp, int attack, int defense, int minDamage, int maxDamage)
        {
            units[catalogUnitId] = new CombatUnitCatalogEntry(catalogUnitId, hp, attack, defense, minDamage, maxDamage);
        }

        public bool TryGetUnit(string catalogUnitId, out CombatUnitCatalogEntry unit)
        {
            return units.TryGetValue(catalogUnitId, out unit);
        }
    }
}
#endif
