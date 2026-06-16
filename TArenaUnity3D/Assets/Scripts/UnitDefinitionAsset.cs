using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitDefinition", menuName = "TArena/Units/Unit Definition")]
public class UnitDefinitionAsset : ScriptableObject
{
    [SerializeField] private string unitName;
    [SerializeField] private string tier = "I";
    [SerializeField] private int factionId;
    [SerializeField] private UnitRoleCategory unitRoleCategory = UnitRoleCategory.Flexible;
    [SerializeField] private int hp;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int initiative;
    [SerializeField] private int speed;
    [SerializeField] private int damageMinimum;
    [SerializeField] private int damageMaximum;
    [SerializeField] private int cost;
    [SerializeField] private string spritePath;
    [SerializeField] private List<string> skillNames = new List<string>();

    public string UnitName { get { return unitName; } }
    public string Tier { get { return string.IsNullOrEmpty(tier) ? "I" : tier; } }
    public int FactionId { get { return factionId <= 0 ? UnitFactionResolver.ResolveFactionId(unitName) : factionId; } }
    public UnitRoleCategory UnitRoleCategory { get { return unitRoleCategory; } }
    public int HP { get { return hp; } }
    public int Attack { get { return attack; } }
    public int Defense { get { return defense; } }
    public int Initiative { get { return initiative; } }
    public int Speed { get { return speed; } }
    public int DamageMinimum { get { return damageMinimum; } }
    public int DamageMaximum { get { return damageMaximum; } }
    public int Cost { get { return cost; } }
    public string SpritePath { get { return spritePath; } }

    public List<string> SkillNames
    {
        get { return skillNames == null ? new List<string>() : new List<string>(skillNames); }
    }

    public DataMapper.UnitDefinition ToUnitDefinition()
    {
        return new DataMapper.UnitDefinition(
            unitName,
            Tier,
            FactionId,
            UnitRoleCategory,
            hp,
            attack,
            defense,
            initiative,
            speed,
            damageMinimum,
            damageMaximum,
            cost,
            spritePath,
            SkillNames);
    }

#if UNITY_EDITOR
    public void Configure(
        string newUnitName,
        string newTier,
        int newFactionId,
        UnitRoleCategory newUnitRoleCategory,
        int newHp,
        int newAttack,
        int newDefense,
        int newInitiative,
        int newSpeed,
        int newDamageMinimum,
        int newDamageMaximum,
        int newCost,
        string newSpritePath,
        List<string> newSkillNames)
    {
        unitName = newUnitName;
        tier = string.IsNullOrEmpty(newTier) ? "I" : newTier;
        factionId = newFactionId <= 0 ? UnitFactionResolver.ResolveFactionId(newUnitName) : newFactionId;
        unitRoleCategory = newUnitRoleCategory;
        hp = newHp;
        attack = newAttack;
        defense = newDefense;
        initiative = newInitiative;
        speed = newSpeed;
        damageMinimum = newDamageMinimum;
        damageMaximum = newDamageMaximum;
        cost = newCost;
        spritePath = newSpritePath;
        skillNames = newSkillNames == null ? new List<string>() : new List<string>(newSkillNames);
    }
#endif
}
