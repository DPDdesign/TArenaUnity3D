#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class SkillPresentationCatalogIndicatorTests
{
    static readonly Dictionary<string, ExpectedIndicatorConfig> ExpectedConfigs = new Dictionary<string, ExpectedIndicatorConfig>
    {
        { "Axe_Rain", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.AoE, SkillIndicatorPlacement.UnderAffectedHexes, "artboard-02 1") },
        { "Blind_by_light", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderHover, "artboard-02 1") },
        { "Chope", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderAffectedHexes, "artboard-02 1") },
        { "Cold_Blood", ExpectedIndicatorConfig.None() },
        { "Defence_Ritual", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderAllAllies, "artboard-02 1") },
        { "Double_Throw", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Scatter, SkillIndicatorPlacement.UnderTargets, "arrowtechart-09 1") },
        { "Fire_Ball", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.AoE, SkillIndicatorPlacement.UnderAffectedHexes, "artboard-02 1") },
        { "Fire_Movement", ExpectedIndicatorConfig.None() },
        { "Fire_Skin", ExpectedIndicatorConfig.None() },
        { "Force_Pull", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderTargets, "artboard-02 1") },
        { "Hate", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderHover, "artboard-02 1") },
        { "Heavy_Fists", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Arc, SkillIndicatorPlacement.UnderAffectedHexes, "artboard-02 1") },
        { "Insult", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderAllEnemies, "artboard-02 1") },
        { "Long_Lick", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderTargets, "artboard-02 1") },
        { "Massochism", ExpectedIndicatorConfig.None() },
        { "Melee_Stance_Barb", ExpectedIndicatorConfig.None() },
        { "Melee_Stance_Lizard", ExpectedIndicatorConfig.None() },
        { "Rage", ExpectedIndicatorConfig.None() },
        { "Range_Stance_Barb", ExpectedIndicatorConfig.None() },
        { "Range_Stance_Lizard", ExpectedIndicatorConfig.None() },
        { "Rope_Trap", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderHover, "artboard-02 1") },
        { "Rotting", ExpectedIndicatorConfig.None() },
        { "Rush", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Line, SkillIndicatorPlacement.CasterToHover, "arrowtechart-09 1") },
        { "Shapeshift", ExpectedIndicatorConfig.None() },
        { "Slash", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Arc, SkillIndicatorPlacement.UnderAffectedHexes, "artboard-02 1") },
        { "Spike_Trap", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderHover, "artboard-02 1") },
        { "Stone_Skin", ExpectedIndicatorConfig.None() },
        { "Stone_Stance", ExpectedIndicatorConfig.None() },
        { "Stone_Throw", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderHover, "artboard-02 1") },
        { "Terrifying_Presence", ExpectedIndicatorConfig.None() },
        { "Tough_Skin", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderHover, "artboard-02 1") },
        { "Toxic_Fume", ExpectedIndicatorConfig.Enabled(SkillIndicatorType.Hex, SkillIndicatorPlacement.UnderAffectedHexes, "artboard-02 1") },
        { "Unstoppable_Light", ExpectedIndicatorConfig.None() }
    };

    [Test]
    public void SkillPresentationCatalog_Covers_All_CurrentSkillAssets()
    {
        SkillPresentationCatalog catalog = LoadCatalog();
        SkillDefinitionAsset[] skillAssets = Resources.LoadAll<SkillDefinitionAsset>("0_Data/Skills");

        HashSet<string> catalogSkillIds = new HashSet<string>(catalog.entries.Select(entry => entry.skillId));
        HashSet<string> currentSkillIds = new HashSet<string>(skillAssets.Select(asset => asset.SkillName));

        Assert.That(catalogSkillIds, Is.EquivalentTo(currentSkillIds));
    }

    [Test]
    public void SkillPresentationCatalog_Uses_Expected_Indicator_Mapping()
    {
        SkillPresentationCatalog catalog = LoadCatalog();

        foreach (KeyValuePair<string, ExpectedIndicatorConfig> pair in ExpectedConfigs)
        {
            SkillPresentationEntry entry = catalog.GetEntry(pair.Key);
            Assert.That(entry, Is.Not.Null, "Missing presentation entry for " + pair.Key);

            Assert.That(entry.indicatorType, Is.EqualTo(pair.Value.Type), pair.Key + " indicator type");
            Assert.That(entry.indicatorPlacement, Is.EqualTo(pair.Value.Placement), pair.Key + " indicator placement");

            if (pair.Value.Type == SkillIndicatorType.None)
            {
                Assert.That(entry.indicatorSprite, Is.Null, pair.Key + " should not use an indicator sprite");
                Assert.That(entry.indicatorMaterial, Is.Null, pair.Key + " should not use an indicator material");
                continue;
            }

            Assert.That(entry.indicatorSprite, Is.Not.Null, pair.Key + " indicator sprite");
            Assert.That(entry.indicatorMaterial, Is.Not.Null, pair.Key + " indicator material");
            Assert.That(entry.indicatorSprite.name, Is.EqualTo(pair.Value.SpriteName), pair.Key + " sprite name");
        }

        Assert.That(catalog.defaultBasicRangedAttackEntry, Is.Not.Null);
        Assert.That(catalog.defaultBasicRangedAttackEntry.indicatorType, Is.EqualTo(SkillIndicatorType.None));
        Assert.That(catalog.defaultBasicRangedAttackEntry.indicatorPlacement, Is.EqualTo(SkillIndicatorPlacement.None));
        Assert.That(catalog.defaultBasicRangedAttackEntry.indicatorSprite, Is.Null);
        Assert.That(catalog.defaultBasicRangedAttackEntry.indicatorMaterial, Is.Null);
    }

    static SkillPresentationCatalog LoadCatalog()
    {
        SkillPresentationCatalog catalog = Resources.Load<SkillPresentationCatalog>("0_Data/SkillPresentationCatalog");
        Assert.That(catalog, Is.Not.Null, "Expected Resources/0_Data/SkillPresentationCatalog asset.");
        return catalog;
    }

    struct ExpectedIndicatorConfig
    {
        public SkillIndicatorType Type;
        public SkillIndicatorPlacement Placement;
        public string SpriteName;

        public static ExpectedIndicatorConfig None()
        {
            return new ExpectedIndicatorConfig
            {
                Type = SkillIndicatorType.None,
                Placement = SkillIndicatorPlacement.None,
                SpriteName = null
            };
        }

        public static ExpectedIndicatorConfig Enabled(
            SkillIndicatorType type,
            SkillIndicatorPlacement placement,
            string spriteName)
        {
            return new ExpectedIndicatorConfig
            {
                Type = type,
                Placement = placement,
                SpriteName = spriteName
            };
        }
    }
}
#endif
