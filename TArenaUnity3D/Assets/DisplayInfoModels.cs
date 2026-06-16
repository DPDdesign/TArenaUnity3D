using System;
using System.Collections.Generic;

[Serializable]
public class SkillInfoData
{
    public string SkillId;
    public bool Unlocked;

    public SkillInfoData(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class UnitStatsData
{
    public int Attack;
    public int Defence;
    public int Damage;
    public int DamageMin;
    public int DamageMax;
    public int Movement;
    public int Initiative;
    public int Health;
    public int CurrentHealth;
    public int MaxHealth;

    public UnitStatsData(
        int attack,
        int defence,
        int damageMin,
        int damageMax,
        int movement,
        int initiative,
        int currentHealth,
        int maxHealth)
    {
        Attack = Math.Max(0, attack);
        Defence = Math.Max(0, defence);
        DamageMin = Math.Max(0, damageMin);
        DamageMax = Math.Max(0, damageMax);
        Damage = (int)Math.Ceiling((DamageMin + DamageMax) / 2f);
        Movement = Math.Max(0, movement);
        Initiative = Math.Max(0, initiative);
        CurrentHealth = Math.Max(0, currentHealth);
        MaxHealth = Math.Max(0, maxHealth);
        Health = MaxHealth;
    }
}

[Serializable]
public class UnitInfoData
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Cost;
    public string SpriteReference;
    public UnitStatsData Stats;
    public List<SkillInfoData> Skills;

    public UnitInfoData(
        string unitId,
        string displayName,
        string tier,
        int cost,
        string spriteReference,
        UnitStatsData stats,
        List<SkillInfoData> skills)
    {
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "?" : tier;
        Cost = Math.Max(0, cost);
        SpriteReference = string.IsNullOrEmpty(spriteReference) ? unitId : spriteReference;
        Stats = stats;
        Skills = skills ?? new List<SkillInfoData>();
    }
}

[Serializable]
public class StackInfoData
{
    public string StackId;
    public int Count;
    public int StackCost;
    public int Level;
    public int Lost;
    public int StackValue;
    public UnitInfoData Unit;

    public StackInfoData(
        string stackId,
        int count,
        int level,
        int lost,
        int stackValue,
        UnitInfoData unit)
    {
        StackId = stackId;
        Count = Math.Max(0, count);
        Level = Math.Max(0, level);
        Lost = Math.Max(0, lost);
        StackValue = Math.Max(0, stackValue);
        Unit = unit;
        StackCost = Unit == null ? StackValue : Math.Max(0, Count * Unit.Cost);
    }
}
