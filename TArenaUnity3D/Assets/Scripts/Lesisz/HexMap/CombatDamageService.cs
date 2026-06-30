using System;

public sealed class CombatDamageService
{
    static readonly CombatDamageCalculator DefaultCalculator = new CombatDamageCalculator();
    static CombatDamageService defaultService;

    readonly ICombatUnitCatalog unitCatalog;
    readonly CombatDamageCalculator calculator;

    public CombatDamageService(ICombatUnitCatalog unitCatalog)
        : this(unitCatalog, DefaultCalculator)
    {
    }

    public CombatDamageService(ICombatUnitCatalog unitCatalog, CombatDamageCalculator calculator)
    {
        this.unitCatalog = unitCatalog;
        this.calculator = calculator ?? DefaultCalculator;
    }

    public static CombatDamageService Default
    {
        get
        {
            if (defaultService == null)
            {
                defaultService = new CombatDamageService(DataMapperCombatUnitCatalog.Instance);
            }

            return defaultService;
        }
    }

    public CombatDamageServiceResult CalculateDamage(BattleSnapshot snapshot, CombatDamageRequest request)
    {
        string error;
        CombatDamageInput input;
        if (TryBuildInput(snapshot, request, out input, out error) == false)
        {
            return CombatDamageServiceResult.Failure(error);
        }

        return CombatDamageServiceResult.Success(calculator.Calculate(input));
    }

    bool TryBuildInput(
        BattleSnapshot snapshot,
        CombatDamageRequest request,
        out CombatDamageInput input,
        out string error)
    {
        input = null;
        error = string.Empty;

        if (snapshot == null)
        {
            error = "Battle snapshot was unavailable for combat damage.";
            return false;
        }

        if (request == null)
        {
            error = "Combat damage request was null.";
            return false;
        }

        if (unitCatalog == null)
        {
            error = "Combat unit catalog was unavailable.";
            return false;
        }

        BattleUnitSnapshot actor = FindUnit(snapshot, request.ActorUnitId);
        if (actor == null)
        {
            error = "Combat damage actor snapshot was missing: " + Safe(request.ActorUnitId) + ".";
            return false;
        }

        BattleUnitSnapshot target = FindUnit(snapshot, request.TargetUnitId);
        if (target == null)
        {
            error = "Combat damage target snapshot was missing: " + Safe(request.TargetUnitId) + ".";
            return false;
        }

        if (actor.IsAlive == false || actor.Amount <= 0)
        {
            error = "Combat damage actor was not alive/actionable: " + Safe(actor.RuntimeUnitId) + ".";
            return false;
        }

        CombatUnitCatalogEntry actorCatalog;
        if (TryResolveCatalogUnit(actor, "actor", out actorCatalog, out error) == false)
        {
            return false;
        }

        CombatUnitCatalogEntry targetCatalog;
        if (TryResolveCatalogUnit(target, "target", out targetCatalog, out error) == false)
        {
            return false;
        }

        if (IsValidDamageData(actorCatalog) == false)
        {
            error = "Combat catalog damage data was invalid for actor catalog unit: " + Safe(actor.CatalogUnitId) + ".";
            return false;
        }

        if (IsValidDamageData(targetCatalog) == false)
        {
            error = "Combat catalog damage data was invalid for target catalog unit: " + Safe(target.CatalogUnitId) + ".";
            return false;
        }

        int actorMinDamage = actorCatalog.MinDamage + actor.MinDamageModifier;
        int actorMaxDamage = actorCatalog.MaxDamage + actor.MaxDamageModifier;
        if (actorMinDamage < 0 || actorMaxDamage < 0)
        {
            error = "Combat actor snapshot damage data was invalid: " + Safe(actor.RuntimeUnitId) + ".";
            return false;
        }

        if (actor.DefensePenetration < 0 || actor.DefensePenetration > 1)
        {
            error = "Combat actor defense penetration was outside 0..1: " + Safe(actor.RuntimeUnitId) + ".";
            return false;
        }

        if (string.IsNullOrEmpty(request.RollPurpose))
        {
            error = "Combat damage roll purpose was missing.";
            return false;
        }

        if (double.IsNaN(request.DamageScale) || double.IsInfinity(request.DamageScale))
        {
            error = "Combat damage scale was not deterministic.";
            return false;
        }

        input = new CombatDamageInput
        {
            ActorRuntimeUnitId = actor.RuntimeUnitId ?? string.Empty,
            TargetRuntimeUnitId = target.RuntimeUnitId ?? string.Empty,
            ActorAmount = actor.Amount,
            ActorAttack = actorCatalog.Attack + actor.AttackModifier,
            ActorMinDamage = Math.Min(actorMinDamage, actorMaxDamage),
            ActorMaxDamage = Math.Max(actorMinDamage, actorMaxDamage),
            ActorOutgoingDamageReductionPercent = actor.OutgoingDamageReductionPercent,
            ActorPureDamage = actor.PureDamage,
            ActorDefensePenetration = actor.DefensePenetration,
            ActorHatedTargetUnitId = actor.HatedTargetUnitId ?? string.Empty,
            DefenderDefense = targetCatalog.Defense + target.DefenseModifier,
            DefenderIncomingDamageReductionPercent = target.IncomingDamageReductionPercent,
            DefenderFlatDamageReduction = target.FlatDamageReduction,
            GameSeed = snapshot.GameSeed,
            ActionIndex = request.ActionIndex,
            ActionSeed = request.ActionSeed,
            RollPurpose = request.RollPurpose ?? string.Empty,
            DamageScale = request.DamageScale,
            HasBaseDamageOverride = request.HasBaseDamageOverride,
            BaseDamageOverride = request.BaseDamageOverride,
            IsStackable = request.IsStackable,
            ConsumeActorPureDamage = request.ConsumeActorPureDamage
        };

        return true;
    }

    bool TryResolveCatalogUnit(
        BattleUnitSnapshot snapshotUnit,
        string role,
        out CombatUnitCatalogEntry catalogUnit,
        out string error)
    {
        catalogUnit = null;
        error = string.Empty;

        if (snapshotUnit == null)
        {
            error = "Combat " + role + " snapshot was missing.";
            return false;
        }

        if (string.IsNullOrEmpty(snapshotUnit.CatalogUnitId))
        {
            error = "Combat " + role + " catalog unit id was missing for runtime unit: " + Safe(snapshotUnit.RuntimeUnitId) + ".";
            return false;
        }

        if (unitCatalog.TryGetUnit(snapshotUnit.CatalogUnitId, out catalogUnit) == false || catalogUnit == null)
        {
            error = "Combat " + role + " catalog unit was missing: " + Safe(snapshotUnit.CatalogUnitId) + ".";
            return false;
        }

        return true;
    }

    static bool IsValidDamageData(CombatUnitCatalogEntry unit)
    {
        return unit != null &&
            unit.MinDamage >= 0 &&
            unit.MaxDamage >= 0 &&
            unit.MaxDamage >= unit.MinDamage;
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

    static string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "<empty>" : value;
    }
}

public sealed class DataMapperCombatUnitCatalog : ICombatUnitCatalog
{
    static readonly DataMapperCombatUnitCatalog instance = new DataMapperCombatUnitCatalog();

    public static DataMapperCombatUnitCatalog Instance
    {
        get { return instance; }
    }

    public bool TryGetUnit(string catalogUnitId, out CombatUnitCatalogEntry unit)
    {
        unit = null;
        if (string.IsNullOrEmpty(catalogUnitId) || DataMapper.Instance == null)
        {
            return false;
        }

        DataMapper.UnitDefinition definition = DataMapper.Instance.FindUnit(catalogUnitId);
        if (definition == null)
        {
            return false;
        }

        unit = new CombatUnitCatalogEntry(
            definition.Name,
            definition.HP,
            definition.Attack,
            definition.Defense,
            definition.DamageMinimum,
            definition.DamageMaximum);
        return true;
    }
}
