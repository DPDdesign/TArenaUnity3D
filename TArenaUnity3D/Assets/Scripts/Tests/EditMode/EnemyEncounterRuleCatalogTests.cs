#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EnemyEncounterRuleCatalogTests
{
    [Test]
    public void Resolve_GeneratedRules_ReturnsAssignedRuleSetsForBattleDifficulties()
    {
        EnemyEncounterRuleCatalog catalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        ArmyGeneratorRuleSet lowRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        ArmyGeneratorRuleSet mediumRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        ArmyGeneratorRuleSet highRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        RewardGeneratorRuleSet lowRewardRules = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        RewardGeneratorRuleSet mediumRewardRules = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        RewardGeneratorRuleSet highRewardRules = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();

        catalog.Entries = new List<EnemyEncounterRule>
        {
            Generated(EnemyEncounterDifficulty.Low, lowRules, lowRewardRules),
            Generated(EnemyEncounterDifficulty.Medium, mediumRules, mediumRewardRules),
            Generated(EnemyEncounterDifficulty.High, highRules, highRewardRules)
        };

        AssertResolvedGenerated(catalog, EnemyEncounterDifficulty.Low, lowRules, lowRewardRules);
        AssertResolvedGenerated(catalog, EnemyEncounterDifficulty.Medium, mediumRules, mediumRewardRules);
        AssertResolvedGenerated(catalog, EnemyEncounterDifficulty.High, highRules, highRewardRules);

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(lowRules);
        Object.DestroyImmediate(mediumRules);
        Object.DestroyImmediate(highRules);
        Object.DestroyImmediate(lowRewardRules);
        Object.DestroyImmediate(mediumRewardRules);
        Object.DestroyImmediate(highRewardRules);
    }

    [Test]
    public void Resolve_NonEmptyPredefinedId_AllowsNullRuleSetAndReturnsPredefinedId()
    {
        EnemyEncounterRuleCatalog catalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        catalog.Entries = new List<EnemyEncounterRule>
        {
            Predefined(EnemyEncounterDifficulty.Boss, null, "boss-army-forest-01")
        };

        EnemyEncounterRuleLookupResult result = catalog.Resolve(EnemyEncounterDifficulty.Boss);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(EnemyEncounterRuleLookupError.None));
        Assert.That(result.Rule.ResolvedArmyGeneratorRuleSet, Is.Null);
        Assert.That(result.Rule.ResolvedPredefinedEnemyId, Is.EqualTo("boss-army-forest-01"));

        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void Resolve_NonEmptyPredefinedId_IgnoresAssignedRuleSet()
    {
        EnemyEncounterRuleCatalog catalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        ArmyGeneratorRuleSet assignedRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        catalog.Entries = new List<EnemyEncounterRule>
        {
            Predefined(EnemyEncounterDifficulty.Boss, assignedRules, "boss-army-castle-01")
        };

        EnemyEncounterRuleLookupResult result = catalog.Resolve(EnemyEncounterDifficulty.Boss);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Rule.ArmyGeneratorRuleSet, Is.EqualTo(assignedRules));
        Assert.That(result.Rule.ResolvedArmyGeneratorRuleSet, Is.Null);
        Assert.That(result.Rule.ResolvedPredefinedEnemyId, Is.EqualTo("boss-army-castle-01"));

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(assignedRules);
    }

    [Test]
    public void Resolve_EmptyPredefinedId_UsesAssignedRuleSet()
    {
        EnemyEncounterRuleCatalog catalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        ArmyGeneratorRuleSet assignedRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        catalog.Entries = new List<EnemyEncounterRule>
        {
            new EnemyEncounterRule(EnemyEncounterDifficulty.Boss, assignedRules, "   ")
        };

        EnemyEncounterRuleLookupResult result = catalog.Resolve(EnemyEncounterDifficulty.Boss);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(EnemyEncounterRuleLookupError.None));
        Assert.That(result.Rule.IsGenerated, Is.True);
        Assert.That(result.Rule.ResolvedArmyGeneratorRuleSet, Is.EqualTo(assignedRules));
        Assert.That(result.Rule.ResolvedPredefinedEnemyId, Is.EqualTo(string.Empty));

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(assignedRules);
    }

    [Test]
    public void Resolve_NoPredefinedIdAndNoRuleSet_FailsClearly()
    {
        EnemyEncounterRuleCatalog catalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        catalog.Entries = new List<EnemyEncounterRule>
        {
            new EnemyEncounterRule(EnemyEncounterDifficulty.Low, null, string.Empty)
        };

        EnemyEncounterRuleLookupResult result = catalog.Resolve(EnemyEncounterDifficulty.Low);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Rule, Is.Null);
        Assert.That(result.Error, Is.EqualTo(EnemyEncounterRuleLookupError.MissingArmyGeneratorRuleSet));

        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void Resolve_MissingAndDuplicateEntries_FailClearly()
    {
        EnemyEncounterRuleCatalog missingCatalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        missingCatalog.Entries = new List<EnemyEncounterRule>();

        EnemyEncounterRuleLookupResult missing = missingCatalog.Resolve(EnemyEncounterDifficulty.Low);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error, Is.EqualTo(EnemyEncounterRuleLookupError.MissingEntry));

        EnemyEncounterRuleCatalog duplicateCatalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        ArmyGeneratorRuleSet lowRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        RewardGeneratorRuleSet lowRewardRules = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        duplicateCatalog.Entries = new List<EnemyEncounterRule>
        {
            Generated(EnemyEncounterDifficulty.Low, lowRules, lowRewardRules),
            Generated(EnemyEncounterDifficulty.Low, lowRules, lowRewardRules)
        };

        EnemyEncounterRuleLookupResult duplicate = duplicateCatalog.Resolve(EnemyEncounterDifficulty.Low);

        Assert.That(duplicate.Success, Is.False);
        Assert.That(duplicate.Error, Is.EqualTo(EnemyEncounterRuleLookupError.DuplicateEntry));

        Object.DestroyImmediate(missingCatalog);
        Object.DestroyImmediate(duplicateCatalog);
        Object.DestroyImmediate(lowRules);
        Object.DestroyImmediate(lowRewardRules);
    }

    private static EnemyEncounterRule Generated(
        EnemyEncounterDifficulty difficulty,
        ArmyGeneratorRuleSet rules,
        RewardGeneratorRuleSet rewardRules)
    {
        return new EnemyEncounterRule(
            difficulty,
            rules,
            rewardRules,
            string.Empty);
    }

    private static EnemyEncounterRule Predefined(
        EnemyEncounterDifficulty difficulty,
        ArmyGeneratorRuleSet rules,
        string predefinedEnemyId)
    {
        return new EnemyEncounterRule(
            difficulty,
            rules,
            predefinedEnemyId);
    }

    private static void AssertResolvedGenerated(
        EnemyEncounterRuleCatalog catalog,
        EnemyEncounterDifficulty difficulty,
        ArmyGeneratorRuleSet expectedRules,
        RewardGeneratorRuleSet expectedRewardRules)
    {
        EnemyEncounterRuleLookupResult result = catalog.Resolve(difficulty);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(EnemyEncounterRuleLookupError.None));
        Assert.That(result.Rule.ResolvedArmyGeneratorRuleSet, Is.EqualTo(expectedRules));
        Assert.That(result.Rule.ResolvedRewardGeneratorRuleSet, Is.EqualTo(expectedRewardRules));
        Assert.That(result.Rule.ResolvedPredefinedEnemyId, Is.EqualTo(string.Empty));
    }
}
#endif
