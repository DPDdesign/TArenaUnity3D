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

public enum EnemyEncounterResolutionMode
{
    Generated,
    Predefined
}

public enum EnemyEncounterRuleLookupError
{
    None,
    MissingEntry,
    DuplicateEntry,
    MissingArmyGeneratorRuleSet,
    MissingPredefinedEnemyId
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
                return "Generated enemy encounter rule requires an ArmyGeneratorRuleSet.";
            case EnemyEncounterRuleLookupError.MissingPredefinedEnemyId:
                return "Predefined enemy encounter rule requires a predefined enemy id.";
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
    public EnemyEncounterResolutionMode Mode;
    public ArmyGeneratorRuleSet ArmyGeneratorRuleSet;
    public string PredefinedEnemyId;

    public EnemyEncounterRule(
        EnemyEncounterDifficulty difficulty,
        EnemyEncounterResolutionMode mode,
        ArmyGeneratorRuleSet armyGeneratorRuleSet,
        string predefinedEnemyId)
    {
        Difficulty = difficulty;
        Mode = mode;
        ArmyGeneratorRuleSet = armyGeneratorRuleSet;
        PredefinedEnemyId = predefinedEnemyId;
    }

    public bool IsGenerated
    {
        get { return Mode == EnemyEncounterResolutionMode.Generated; }
    }

    public bool IsPredefined
    {
        get { return Mode == EnemyEncounterResolutionMode.Predefined; }
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

        if (IsPredefined && string.IsNullOrEmpty(TrimmedPredefinedEnemyId))
        {
            return EnemyEncounterRuleLookupError.MissingPredefinedEnemyId;
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
