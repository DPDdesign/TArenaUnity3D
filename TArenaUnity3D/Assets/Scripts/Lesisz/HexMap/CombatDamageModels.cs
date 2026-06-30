using System;

public interface ICombatUnitCatalog
{
    bool TryGetUnit(string catalogUnitId, out CombatUnitCatalogEntry unit);
}

public sealed class CombatUnitCatalogEntry
{
    public string CatalogUnitId = string.Empty;
    public int BaseHP;
    public int Attack;
    public int Defense;
    public int MinDamage;
    public int MaxDamage;

    public CombatUnitCatalogEntry()
    {
    }

    public CombatUnitCatalogEntry(
        string catalogUnitId,
        int baseHP,
        int attack,
        int defense,
        int minDamage,
        int maxDamage)
    {
        CatalogUnitId = catalogUnitId ?? string.Empty;
        BaseHP = baseHP;
        Attack = attack;
        Defense = defense;
        MinDamage = minDamage;
        MaxDamage = maxDamage;
    }
}

public sealed class CombatDamageRequest
{
    public string ActorUnitId = string.Empty;
    public string TargetUnitId = string.Empty;
    public string RollPurpose = CombatDamageRollPurpose.BasicAttack;
    public int ActionIndex;
    public int ActionSeed;
    public double DamageScale = 1.0;
    public bool HasBaseDamageOverride;
    public int BaseDamageOverride;
    public bool IsStackable = true;
    public bool ConsumeActorPureDamage = true;
}

public static class CombatDamageRollPurpose
{
    public const string BasicAttack = "basic-attack";
    public const string Retaliation = "retaliation";
    public const string Skill = "skill";
}

public sealed class CombatDamageInput
{
    public string ActorRuntimeUnitId = string.Empty;
    public string TargetRuntimeUnitId = string.Empty;
    public int ActorAmount;
    public int ActorAttack;
    public int ActorMinDamage;
    public int ActorMaxDamage;
    public int ActorOutgoingDamageReductionPercent;
    public int ActorPureDamage;
    public double ActorDefensePenetration;
    public string ActorHatedTargetUnitId = string.Empty;
    public int DefenderDefense;
    public int DefenderIncomingDamageReductionPercent;
    public int DefenderFlatDamageReduction;
    public int GameSeed;
    public int ActionIndex;
    public int ActionSeed;
    public string RollPurpose = CombatDamageRollPurpose.BasicAttack;
    public double DamageScale = 1.0;
    public bool HasBaseDamageOverride;
    public int BaseDamageOverride;
    public bool IsStackable = true;
    public bool ConsumeActorPureDamage = true;
}

public sealed class CombatDamageForecast
{
    public int MinDamage;
    public int MaxDamage;
    public int CommittedDamage;
}

public sealed class CombatDamageResult
{
    public int CommittedDamage;
    public CombatDamageForecast Forecast = new CombatDamageForecast();
    public bool ConsumesActorPureDamage;
}

public sealed class CombatDamageServiceResult
{
    public bool IsValid;
    public string Error = string.Empty;
    public CombatDamageResult Damage;

    public static CombatDamageServiceResult Success(CombatDamageResult damage)
    {
        return new CombatDamageServiceResult
        {
            IsValid = true,
            Damage = damage
        };
    }

    public static CombatDamageServiceResult Failure(string error)
    {
        return new CombatDamageServiceResult
        {
            IsValid = false,
            Error = error ?? string.Empty
        };
    }
}

public sealed class CombatDamageCalculator
{
    public CombatDamageResult Calculate(CombatDamageInput input)
    {
        int minRoll = ResolveBaseRoll(input, true);
        int maxRoll = ResolveBaseRoll(input, false);
        if (minRoll > maxRoll)
        {
            int temp = minRoll;
            minRoll = maxRoll;
            maxRoll = temp;
        }

        int committedRoll = input.HasBaseDamageOverride
            ? input.BaseDamageOverride
            : RollInclusive(minRoll, maxRoll, input);

        int minDamage = CalculateDamageForRoll(input, minRoll);
        int maxDamage = CalculateDamageForRoll(input, maxRoll);
        if (minDamage > maxDamage)
        {
            int temp = minDamage;
            minDamage = maxDamage;
            maxDamage = temp;
        }

        int committedDamage = CalculateDamageForRoll(input, committedRoll);
        return new CombatDamageResult
        {
            CommittedDamage = committedDamage,
            ConsumesActorPureDamage = input.ConsumeActorPureDamage && input.ActorPureDamage > 0,
            Forecast = new CombatDamageForecast
            {
                MinDamage = minDamage,
                MaxDamage = maxDamage,
                CommittedDamage = committedDamage
            }
        };
    }

    static int ResolveBaseRoll(CombatDamageInput input, bool min)
    {
        if (input.HasBaseDamageOverride)
        {
            return Math.Max(0, input.BaseDamageOverride);
        }

        return min ? input.ActorMinDamage : input.ActorMaxDamage;
    }

    static int CalculateDamageForRoll(CombatDamageInput input, int baseRoll)
    {
        double effectiveDefense = input.DefenderDefense * (1.0 - input.ActorDefensePenetration);
        double attackDefenseDelta = input.ActorAttack - effectiveDefense;
        double attackDefenseMultiplier = ResolveAttackDefenseMultiplier(attackDefenseDelta);
        int stackAmount = input.IsStackable ? input.ActorAmount : 1;

        double damage = baseRoll;
        damage *= Math.Max(0, stackAmount);
        damage *= input.DamageScale;
        damage *= 1.0 + attackDefenseDelta * attackDefenseMultiplier;
        damage *= (100.0 - input.ActorOutgoingDamageReductionPercent) / 100.0;
        damage *= (100.0 - input.DefenderIncomingDamageReductionPercent) / 100.0;
        damage += input.ActorPureDamage;

        if (string.IsNullOrEmpty(input.ActorHatedTargetUnitId) == false &&
            string.Equals(input.ActorHatedTargetUnitId, input.TargetRuntimeUnitId, StringComparison.Ordinal))
        {
            damage *= 1.5;
        }

        damage = ApplyFlatReductionCap(damage, input.DefenderFlatDamageReduction, Math.Max(0, stackAmount));
        if (damage < 0)
        {
            damage = 0;
        }

        return (int)Math.Ceiling(damage);
    }

    static double ResolveAttackDefenseMultiplier(double attackDefenseDelta)
    {
        if (attackDefenseDelta > 0)
        {
            return 0.04;
        }

        if (attackDefenseDelta < 0)
        {
            return 0.014;
        }

        return 1.0;
    }

    static double ApplyFlatReductionCap(double damageBeforeFlat, int flatDamageReduction, int actorAmount)
    {
        if (damageBeforeFlat <= 0 || flatDamageReduction <= 0 || actorAmount <= 0)
        {
            return damageBeforeFlat;
        }

        double reduced = damageBeforeFlat - flatDamageReduction * actorAmount;
        double minimumAfterFlat = damageBeforeFlat * 0.3;
        return Math.Max(reduced, minimumAfterFlat);
    }

    static int RollInclusive(int minRoll, int maxRoll, CombatDamageInput input)
    {
        int spread = Math.Max(0, maxRoll - minRoll);
        if (spread == 0)
        {
            return Math.Max(0, minRoll);
        }

        unchecked
        {
            uint seed = 2166136261;
            seed = Hash(seed, input.GameSeed);
            seed = Hash(seed, input.ActionSeed);
            seed = Hash(seed, input.ActionIndex);
            seed = Hash(seed, input.ActorRuntimeUnitId);
            seed = Hash(seed, input.TargetRuntimeUnitId);
            seed = Hash(seed, input.RollPurpose);
            return minRoll + (int)(seed % (uint)(spread + 1));
        }
    }

    static uint Hash(uint seed, int value)
    {
        unchecked
        {
            seed ^= (uint)value;
            seed *= 16777619;
            return seed;
        }
    }

    static uint Hash(uint seed, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Hash(seed, 0);
        }

        unchecked
        {
            for (int i = 0; i < value.Length; i++)
            {
                seed ^= value[i];
                seed *= 16777619;
            }

            return seed;
        }
    }
}
