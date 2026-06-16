using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class UnitDefinitionAssetMetadataTests
{
    [Test]
    public void ToUnitDefinition_CarriesFactionAndRoleCategoryToStartRunDefinition()
    {
        UnitDefinitionAsset asset = ScriptableObject.CreateInstance<UnitDefinitionAsset>();
        asset.Configure(
            "Healer",
            "2",
            UnitFactionResolver.LizardFactionId,
            UnitRoleCategory.Support,
            8,
            2,
            3,
            5,
            3,
            1,
            2,
            60,
            "Sprites/Healer",
            new List<string> { "Tough_Skin", "Defence_Ritual" });

        DataMapper.UnitDefinition unit = asset.ToUnitDefinition();
        StartRunUnitDefinition startRunUnit = RunMetagameUnitDefinitionMapper.ToStartRunUnitDefinition(unit);

        Assert.That(unit.FactionId, Is.EqualTo(UnitFactionResolver.LizardFactionId));
        Assert.That(unit.UnitRoleCategory, Is.EqualTo(UnitRoleCategory.Support));
        Assert.That(startRunUnit.FactionId, Is.EqualTo(UnitFactionResolver.LizardFactionId));
        Assert.That(startRunUnit.UnitRoleCategory, Is.EqualTo(UnitRoleCategory.Support));
    }
}
