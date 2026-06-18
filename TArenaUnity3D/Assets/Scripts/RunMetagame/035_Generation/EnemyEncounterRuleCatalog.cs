using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyEncounterDifficulty
{
    Low,
    Medium,
    High,
    Boss
}

public enum EnemyEncounterRuleLookupError
{
    None,
    MissingEntry,
    DuplicateEntry,
    MissingArmyGeneratorRuleSet
}

[CreateAssetMenu(fileName = "EnemyEncounterRuleCatalog", menuName = "TArena/Run Metagame/Enemy Encounter Rule Catalog")]
public class EnemyEncounterRuleCatalog : ScriptableObject
{
    [SerializeField] private List<EnemyEncounterRule> entries = new List<EnemyEncounterRule>();

    public List<EnemyEncounterRule> Entries
    {
        get { return entries; }
        set { entries = value ?? new List<EnemyEncounterRule>(); }
    }

    public EnemyEncounterRule FindRule(EnemyEncounterDifficulty difficulty)
    {
        EnemyEncounterRuleLookupResult result = Resolve(difficulty);
        return result.Success ? result.Rule : null;
    }

    public EnemyEncounterRuleLookupResult Resolve(EnemyEncounterDifficulty difficulty)
    {
        EnemyEncounterRule match = null;
        int matchCount = 0;

        for (int i = 0; entries != null && i < entries.Count; i++)
        {
            EnemyEncounterRule entry = entries[i];
            if (entry == null || entry.Difficulty != difficulty)
            {
                continue;
            }

            match = entry;
            matchCount++;
        }

        if (matchCount == 0)
        {
            return EnemyEncounterRuleLookupResult.Fail(
                difficulty,
                EnemyEncounterRuleLookupError.MissingEntry,
                "Enemy encounter rule is missing.");
        }

        if (matchCount > 1)
        {
            return EnemyEncounterRuleLookupResult.Fail(
                difficulty,
                EnemyEncounterRuleLookupError.DuplicateEntry,
                "Enemy encounter rule has duplicate entries.");
        }

        EnemyEncounterRuleLookupError validationError = match.GetValidationError();
        if (validationError != EnemyEncounterRuleLookupError.None)
        {
            return EnemyEncounterRuleLookupResult.Fail(
                difficulty,
                validationError,
                MessageFor(validationError));
        }

        return EnemyEncounterRuleLookupResult.SuccessResult(difficulty, match);
    }

    private static string MessageFor(EnemyEncounterRuleLookupError error)
    {
        switch (error)
        {
            case EnemyEncounterRuleLookupError.MissingArmyGeneratorRuleSet:
                return "Enemy encounter rule requires an ArmyGeneratorRuleSet when no predefined enemy id is assigned.";
            case EnemyEncounterRuleLookupError.DuplicateEntry:
                return "Enemy encounter rule has duplicate entries.";
            case EnemyEncounterRuleLookupError.MissingEntry:
                return "Enemy encounter rule is missing.";
            default:
                return "Enemy encounter rule is valid.";
        }
    }
}

[Serializable]
public class EnemyEncounterRule
{
    public EnemyEncounterDifficulty Difficulty;
    public ArmyGeneratorRuleSet ArmyGeneratorRuleSet;
    public string PredefinedEnemyId;

    public EnemyEncounterRule(
        EnemyEncounterDifficulty difficulty,
        ArmyGeneratorRuleSet armyGeneratorRuleSet,
        string predefinedEnemyId)
    {
        Difficulty = difficulty;
        ArmyGeneratorRuleSet = armyGeneratorRuleSet;
        PredefinedEnemyId = predefinedEnemyId;
    }

    public bool IsGenerated
    {
        get { return !IsPredefined; }
    }

    public bool IsPredefined
    {
        get { return !string.IsNullOrEmpty(TrimmedPredefinedEnemyId); }
    }

    public ArmyGeneratorRuleSet ResolvedArmyGeneratorRuleSet
    {
        get { return IsGenerated ? ArmyGeneratorRuleSet : null; }
    }

    public string ResolvedPredefinedEnemyId
    {
        get { return IsPredefined ? TrimmedPredefinedEnemyId : string.Empty; }
    }

    public EnemyEncounterRuleLookupError GetValidationError()
    {
        if (IsGenerated && ArmyGeneratorRuleSet == null)
        {
            return EnemyEncounterRuleLookupError.MissingArmyGeneratorRuleSet;
        }

        return EnemyEncounterRuleLookupError.None;
    }

    private string TrimmedPredefinedEnemyId
    {
        get { return string.IsNullOrEmpty(PredefinedEnemyId) ? string.Empty : PredefinedEnemyId.Trim(); }
    }
}

public class EnemyEncounterRuleLookupResult
{
    public readonly bool Success;
    public readonly EnemyEncounterDifficulty Difficulty;
    public readonly EnemyEncounterRule Rule;
    public readonly EnemyEncounterRuleLookupError Error;
    public readonly string Message;

    private EnemyEncounterRuleLookupResult(
        bool success,
        EnemyEncounterDifficulty difficulty,
        EnemyEncounterRule rule,
        EnemyEncounterRuleLookupError error,
        string message)
    {
        Success = success;
        Difficulty = difficulty;
        Rule = rule;
        Error = error;
        Message = message;
    }

    public static EnemyEncounterRuleLookupResult SuccessResult(
        EnemyEncounterDifficulty difficulty,
        EnemyEncounterRule rule)
    {
        return new EnemyEncounterRuleLookupResult(
            true,
            difficulty,
            rule,
            EnemyEncounterRuleLookupError.None,
            "Enemy encounter rule resolved.");
    }

    public static EnemyEncounterRuleLookupResult Fail(
        EnemyEncounterDifficulty difficulty,
        EnemyEncounterRuleLookupError error,
        string message)
    {
        return new EnemyEncounterRuleLookupResult(
            false,
            difficulty,
            null,
            error,
            message);
    }
}
