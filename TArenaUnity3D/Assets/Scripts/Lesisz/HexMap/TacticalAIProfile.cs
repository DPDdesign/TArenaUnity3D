using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public enum TacticalAIDeterministicTieBreakMode
{
    StableSeededOrder = 0
}

public enum TacticalAICandidateBucket
{
    ActionType = 0,
    Skill = 1,
    Move = 2,
    Attack = 3,
    Fallback = 4
}

[Serializable]
public class TacticalAIScoringWeights
{
    public float EnemyValueRemoved = 1f;
    public float OwnValueLost = 1.2f;
    public float EnemyStackKillBonus = 25f;
    public float OwnStackLossPenalty = 25f;
    public float WinScore = 1000f;
    public float LossScore = -1000f;
    public float DamageEfficiency = 0.25f;
    public float PositionSafety = 0.2f;
    public float ThreatControl = 0.2f;
    public float ProgressTempo = 0.2f;

    public TacticalAIScoringWeights Clone()
    {
        return new TacticalAIScoringWeights
        {
            EnemyValueRemoved = EnemyValueRemoved,
            OwnValueLost = OwnValueLost,
            EnemyStackKillBonus = EnemyStackKillBonus,
            OwnStackLossPenalty = OwnStackLossPenalty,
            WinScore = WinScore,
            LossScore = LossScore,
            DamageEfficiency = DamageEfficiency,
            PositionSafety = PositionSafety,
            ThreatControl = ThreatControl,
            ProgressTempo = ProgressTempo
        };
    }
}

[Serializable]
public class TacticalAIActionTypeBiases
{
    public float Skill = 0.15f;
    public float Attack = 0.1f;
    public float MoveAndAttack = 0.05f;
    public float Move = 0f;
    public float Defend = -0.05f;
    public float Wait = -0.1f;

    public TacticalAIActionTypeBiases Clone()
    {
        return new TacticalAIActionTypeBiases
        {
            Skill = Skill,
            Attack = Attack,
            MoveAndAttack = MoveAndAttack,
            Move = Move,
            Defend = Defend,
            Wait = Wait
        };
    }
}

[Serializable]
public class TacticalAIResolvedProfile
{
    public string DifficultyName = string.Empty;
    public int SearchDepthPlies;
    public int DecisionWatchdogMs;
    public int OwnActionBeam;
    public int EnemyResponseBeam;
    public int MaxCandidatesPerActionType;
    public int MaxSkillCandidates;
    public int MaxMoveCandidates;
    public int MaxAttackCandidates;
    public int MaxFallbackCandidates;
    public bool RequireOpponentResponseWhenReachable;
    public TacticalAIDeterministicTieBreakMode DeterministicTieBreakMode;
    public int StableTieBreakSeed;
    public TacticalAIScoringWeights ScoringWeights = new TacticalAIScoringWeights();
    public TacticalAIActionTypeBiases ActionTypeBiases = new TacticalAIActionTypeBiases();
    public string ProfileHash = string.Empty;

    public TacticalAIResolvedProfile Clone()
    {
        return new TacticalAIResolvedProfile
        {
            DifficultyName = DifficultyName,
            SearchDepthPlies = SearchDepthPlies,
            DecisionWatchdogMs = DecisionWatchdogMs,
            OwnActionBeam = OwnActionBeam,
            EnemyResponseBeam = EnemyResponseBeam,
            MaxCandidatesPerActionType = MaxCandidatesPerActionType,
            MaxSkillCandidates = MaxSkillCandidates,
            MaxMoveCandidates = MaxMoveCandidates,
            MaxAttackCandidates = MaxAttackCandidates,
            MaxFallbackCandidates = MaxFallbackCandidates,
            RequireOpponentResponseWhenReachable = RequireOpponentResponseWhenReachable,
            DeterministicTieBreakMode = DeterministicTieBreakMode,
            StableTieBreakSeed = StableTieBreakSeed,
            ScoringWeights = ScoringWeights == null ? new TacticalAIScoringWeights() : ScoringWeights.Clone(),
            ActionTypeBiases = ActionTypeBiases == null ? new TacticalAIActionTypeBiases() : ActionTypeBiases.Clone(),
            ProfileHash = ProfileHash
        };
    }
}

[CreateAssetMenu(fileName = "TacticalAIProfile", menuName = "TArena/AI/Tactical AI Profile")]
public class TacticalAIProfile : ScriptableObject
{
    [SerializeField] private string difficultyName = "Normal";
    [SerializeField] private int searchDepthPlies = 3;
    [SerializeField] private int decisionWatchdogMs = 300;
    [SerializeField] private int ownActionBeam = 8;
    [SerializeField] private int enemyResponseBeam = 5;
    [SerializeField] private int maxCandidatesPerActionType = 8;
    [SerializeField] private int maxSkillCandidates = 8;
    [SerializeField] private int maxMoveCandidates = 8;
    [SerializeField] private int maxAttackCandidates = 8;
    [SerializeField] private int maxFallbackCandidates = 4;
    [SerializeField] private bool requireOpponentResponseWhenReachable = true;
    [SerializeField] private TacticalAIDeterministicTieBreakMode deterministicTieBreakMode = TacticalAIDeterministicTieBreakMode.StableSeededOrder;
    [SerializeField] private int stableTieBreakSeed = 46046;
    [SerializeField] private TacticalAIScoringWeights scoringWeights = new TacticalAIScoringWeights();
    [SerializeField] private TacticalAIActionTypeBiases actionTypeBiases = new TacticalAIActionTypeBiases();

    public string DifficultyName
    {
        get { return difficultyName; }
    }

    public int SearchDepthPlies
    {
        get { return searchDepthPlies; }
    }

    public int DecisionWatchdogMs
    {
        get { return decisionWatchdogMs; }
    }

    public int OwnActionBeam
    {
        get { return ownActionBeam; }
    }

    public int EnemyResponseBeam
    {
        get { return enemyResponseBeam; }
    }

    public int MaxCandidatesPerActionType
    {
        get { return maxCandidatesPerActionType; }
    }

    public int MaxSkillCandidates
    {
        get { return maxSkillCandidates; }
    }

    public int MaxMoveCandidates
    {
        get { return maxMoveCandidates; }
    }

    public int MaxAttackCandidates
    {
        get { return maxAttackCandidates; }
    }

    public int MaxFallbackCandidates
    {
        get { return maxFallbackCandidates; }
    }

    public bool RequireOpponentResponseWhenReachable
    {
        get { return requireOpponentResponseWhenReachable; }
    }

    public TacticalAIDeterministicTieBreakMode DeterministicTieBreakMode
    {
        get { return deterministicTieBreakMode; }
    }

    public int StableTieBreakSeed
    {
        get { return stableTieBreakSeed; }
    }

    public TacticalAIScoringWeights ScoringWeights
    {
        get { return scoringWeights == null ? new TacticalAIScoringWeights() : scoringWeights.Clone(); }
    }

    public TacticalAIActionTypeBiases ActionTypeBiases
    {
        get { return actionTypeBiases == null ? new TacticalAIActionTypeBiases() : actionTypeBiases.Clone(); }
    }

    private void OnEnable()
    {
        Sanitize();
    }

    private void OnValidate()
    {
        Sanitize();
    }

    public TacticalAIResolvedProfile ToResolvedProfile()
    {
        Sanitize();

        TacticalAIResolvedProfile profile = new TacticalAIResolvedProfile
        {
            DifficultyName = difficultyName,
            SearchDepthPlies = searchDepthPlies,
            DecisionWatchdogMs = decisionWatchdogMs,
            OwnActionBeam = ownActionBeam,
            EnemyResponseBeam = enemyResponseBeam,
            MaxCandidatesPerActionType = maxCandidatesPerActionType,
            MaxSkillCandidates = maxSkillCandidates,
            MaxMoveCandidates = maxMoveCandidates,
            MaxAttackCandidates = maxAttackCandidates,
            MaxFallbackCandidates = maxFallbackCandidates,
            RequireOpponentResponseWhenReachable = requireOpponentResponseWhenReachable,
            DeterministicTieBreakMode = deterministicTieBreakMode,
            StableTieBreakSeed = stableTieBreakSeed,
            ScoringWeights = scoringWeights == null ? new TacticalAIScoringWeights() : scoringWeights.Clone(),
            ActionTypeBiases = actionTypeBiases == null ? new TacticalAIActionTypeBiases() : actionTypeBiases.Clone()
        };

        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);
        return profile;
    }

    private void Sanitize()
    {
        difficultyName = string.IsNullOrEmpty(difficultyName) ? "Normal" : difficultyName.Trim();
        searchDepthPlies = Math.Max(1, searchDepthPlies);
        decisionWatchdogMs = Math.Max(1, decisionWatchdogMs);
        ownActionBeam = Math.Max(1, ownActionBeam);
        enemyResponseBeam = Math.Max(1, enemyResponseBeam);
        maxCandidatesPerActionType = Math.Max(1, maxCandidatesPerActionType);
        maxSkillCandidates = Math.Max(1, maxSkillCandidates);
        maxMoveCandidates = Math.Max(1, maxMoveCandidates);
        maxAttackCandidates = Math.Max(1, maxAttackCandidates);
        maxFallbackCandidates = Math.Max(1, maxFallbackCandidates);
        stableTieBreakSeed = Math.Max(0, stableTieBreakSeed);

        if (scoringWeights == null)
        {
            scoringWeights = new TacticalAIScoringWeights();
        }

        if (actionTypeBiases == null)
        {
            actionTypeBiases = new TacticalAIActionTypeBiases();
        }
    }
}

public static class TacticalAIProfileCatalog
{
    public const string NormalProfileResourcePath = "0_Data/TacticalAIProfile_Normal";

    public static TacticalAIProfile LoadNormalProfileAsset()
    {
        return Resources.Load<TacticalAIProfile>(NormalProfileResourcePath);
    }

    public static TacticalAIResolvedProfile ResolveAssignedOrRuntimeDefault(TacticalAIProfile assignedProfile)
    {
        if (assignedProfile != null)
        {
            return assignedProfile.ToResolvedProfile();
        }

        TacticalAIResolvedProfile profile = CreateRuntimeNormalDefault();
        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);
        return profile;
    }

    public static TacticalAIResolvedProfile CreateRuntimeNormalDefault()
    {
        return new TacticalAIResolvedProfile
        {
            DifficultyName = "Normal",
            SearchDepthPlies = 3,
            DecisionWatchdogMs = 300,
            OwnActionBeam = 8,
            EnemyResponseBeam = 5,
            MaxCandidatesPerActionType = 8,
            MaxSkillCandidates = 8,
            MaxMoveCandidates = 8,
            MaxAttackCandidates = 8,
            MaxFallbackCandidates = 4,
            RequireOpponentResponseWhenReachable = true,
            DeterministicTieBreakMode = TacticalAIDeterministicTieBreakMode.StableSeededOrder,
            StableTieBreakSeed = 46046,
            ScoringWeights = new TacticalAIScoringWeights(),
            ActionTypeBiases = new TacticalAIActionTypeBiases()
        };
    }
}

public static class TacticalAIProfileHasher
{
    public static string ComputeHash(TacticalAIResolvedProfile profile)
    {
        if (profile == null)
        {
            return string.Empty;
        }

        TacticalAIScoringWeights scoringWeights = profile.ScoringWeights == null
            ? new TacticalAIScoringWeights()
            : profile.ScoringWeights;
        TacticalAIActionTypeBiases actionTypeBiases = profile.ActionTypeBiases == null
            ? new TacticalAIActionTypeBiases()
            : profile.ActionTypeBiases;

        StringBuilder canonical = new StringBuilder(512);
        canonical.Append("difficulty|").Append(Normalize(profile.DifficultyName)).Append('\n');
        canonical.Append("budget|")
            .Append(profile.SearchDepthPlies).Append('|')
            .Append(profile.DecisionWatchdogMs).Append('|')
            .Append(profile.OwnActionBeam).Append('|')
            .Append(profile.EnemyResponseBeam).Append('|')
            .Append(profile.MaxCandidatesPerActionType).Append('|')
            .Append(profile.MaxSkillCandidates).Append('|')
            .Append(profile.MaxMoveCandidates).Append('|')
            .Append(profile.MaxAttackCandidates).Append('|')
            .Append(profile.MaxFallbackCandidates).Append('|')
            .Append(profile.RequireOpponentResponseWhenReachable ? 1 : 0).Append('|')
            .Append((int)profile.DeterministicTieBreakMode).Append('|')
            .Append(profile.StableTieBreakSeed).Append('\n');

        AppendFloat(canonical, "enemyValueRemoved", scoringWeights.EnemyValueRemoved);
        AppendFloat(canonical, "ownValueLost", scoringWeights.OwnValueLost);
        AppendFloat(canonical, "enemyStackKillBonus", scoringWeights.EnemyStackKillBonus);
        AppendFloat(canonical, "ownStackLossPenalty", scoringWeights.OwnStackLossPenalty);
        AppendFloat(canonical, "winScore", scoringWeights.WinScore);
        AppendFloat(canonical, "lossScore", scoringWeights.LossScore);
        AppendFloat(canonical, "damageEfficiency", scoringWeights.DamageEfficiency);
        AppendFloat(canonical, "positionSafety", scoringWeights.PositionSafety);
        AppendFloat(canonical, "threatControl", scoringWeights.ThreatControl);
        AppendFloat(canonical, "progressTempo", scoringWeights.ProgressTempo);

        AppendFloat(canonical, "skillBias", actionTypeBiases.Skill);
        AppendFloat(canonical, "attackBias", actionTypeBiases.Attack);
        AppendFloat(canonical, "moveAndAttackBias", actionTypeBiases.MoveAndAttack);
        AppendFloat(canonical, "moveBias", actionTypeBiases.Move);
        AppendFloat(canonical, "defendBias", actionTypeBiases.Defend);
        AppendFloat(canonical, "waitBias", actionTypeBiases.Wait);

        byte[] bytes = Encoding.UTF8.GetBytes(canonical.ToString());
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(bytes);
            StringBuilder hash = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hash.Append(hashBytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return hash.ToString();
        }
    }

    private static void AppendFloat(StringBuilder canonical, string label, float value)
    {
        canonical.Append(label)
            .Append('|')
            .Append(value.ToString("R", CultureInfo.InvariantCulture))
            .Append('\n');
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
    }
}

public sealed class TacticalAIFixedBudget
{
    private readonly TacticalAIResolvedProfile profile;

    public TacticalAIFixedBudget(TacticalAIResolvedProfile profile)
    {
        this.profile = profile ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
    }

    public bool IsWatchdogExpired(long elapsedMilliseconds)
    {
        return elapsedMilliseconds >= profile.DecisionWatchdogMs;
    }

    public bool CanSearchPly(int plyNumber, long elapsedMilliseconds)
    {
        if (plyNumber <= 0 || IsWatchdogExpired(elapsedMilliseconds))
        {
            return false;
        }

        return plyNumber <= profile.SearchDepthPlies;
    }

    public int GetBeamWidth(bool isOpponentResponsePly)
    {
        return isOpponentResponsePly ? profile.EnemyResponseBeam : profile.OwnActionBeam;
    }

    public int ClampBeamWidth(bool isOpponentResponsePly, int proposedBeamWidth)
    {
        return Math.Min(Math.Max(0, proposedBeamWidth), GetBeamWidth(isOpponentResponsePly));
    }

    public int ClampCandidateCount(TacticalAICandidateBucket bucket, int proposedCount)
    {
        int safeRequested = Math.Max(0, proposedCount);
        int bucketLimit = GetBucketLimit(bucket);
        int combinedLimit = Math.Min(profile.MaxCandidatesPerActionType, bucketLimit);
        return Math.Min(safeRequested, combinedLimit);
    }

    public TPlan ResolveWatchdogFallback<TPlan>(long elapsedMilliseconds, TPlan bestCompletedPlan, TPlan fallbackPlan)
        where TPlan : class
    {
        if (!IsWatchdogExpired(elapsedMilliseconds))
        {
            return bestCompletedPlan;
        }

        return bestCompletedPlan ?? fallbackPlan;
    }

    private int GetBucketLimit(TacticalAICandidateBucket bucket)
    {
        switch (bucket)
        {
            case TacticalAICandidateBucket.Skill:
                return profile.MaxSkillCandidates;
            case TacticalAICandidateBucket.Move:
                return profile.MaxMoveCandidates;
            case TacticalAICandidateBucket.Attack:
                return profile.MaxAttackCandidates;
            case TacticalAICandidateBucket.Fallback:
                return profile.MaxFallbackCandidates;
            default:
                return profile.MaxCandidatesPerActionType;
        }
    }
}

public sealed class TacticalAIPlanCacheKey : IEquatable<TacticalAIPlanCacheKey>
{
    public readonly string SnapshotHash;
    public readonly string ActiveUnitId;
    public readonly string ProfileHash;

    public TacticalAIPlanCacheKey(string snapshotHash, string activeUnitId, string profileHash)
    {
        SnapshotHash = Normalize(snapshotHash);
        ActiveUnitId = Normalize(activeUnitId);
        ProfileHash = Normalize(profileHash);
    }

    public static TacticalAIPlanCacheKey From(BattleSnapshot snapshot, TacticalAIResolvedProfile profile)
    {
        if (snapshot == null || profile == null)
        {
            return null;
        }

        return new TacticalAIPlanCacheKey(
            snapshot.SnapshotHash,
            snapshot.ActiveUnitId,
            profile.ProfileHash);
    }

    public bool Equals(TacticalAIPlanCacheKey other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(SnapshotHash, other.SnapshotHash, StringComparison.Ordinal)
            && string.Equals(ActiveUnitId, other.ActiveUnitId, StringComparison.Ordinal)
            && string.Equals(ProfileHash, other.ProfileHash, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as TacticalAIPlanCacheKey);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + SnapshotHash.GetHashCode();
            hash = (hash * 31) + ActiveUnitId.GetHashCode();
            hash = (hash * 31) + ProfileHash.GetHashCode();
            return hash;
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }
}

public sealed class TacticalAIPlanCacheValue<TIntent>
{
    private readonly List<TIntent> orderedActionIntents;

    public TacticalAIPlanCacheValue(IEnumerable<TIntent> orderedActionIntents, float bestScore, int completedDepth)
    {
        this.orderedActionIntents = orderedActionIntents == null
            ? new List<TIntent>()
            : new List<TIntent>(orderedActionIntents);
        BestScore = bestScore;
        CompletedDepth = Math.Max(0, completedDepth);
    }

    public List<TIntent> OrderedActionIntents
    {
        get { return new List<TIntent>(orderedActionIntents); }
    }

    public float BestScore { get; private set; }

    public int CompletedDepth { get; private set; }
}

public sealed class TacticalAIPlanCache<TIntent>
{
    private readonly Dictionary<TacticalAIPlanCacheKey, TacticalAIPlanCacheValue<TIntent>> entries =
        new Dictionary<TacticalAIPlanCacheKey, TacticalAIPlanCacheValue<TIntent>>();

    public int Count
    {
        get { return entries.Count; }
    }

    public void Clear()
    {
        entries.Clear();
    }

    public void Remove(TacticalAIPlanCacheKey key)
    {
        if (key == null)
        {
            return;
        }

        entries.Remove(key);
    }

    public void StoreAdvisoryPlan(
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile,
        IEnumerable<TIntent> orderedActionIntents,
        float bestScore,
        int completedDepth)
    {
        TacticalAIPlanCacheKey key = TacticalAIPlanCacheKey.From(snapshot, profile);
        if (key == null)
        {
            return;
        }

        entries[key] = new TacticalAIPlanCacheValue<TIntent>(orderedActionIntents, bestScore, completedDepth);
    }

    // Cache hits are advisory only. Execution must still revalidate live state.
    public bool TryGetAdvisoryPlan(
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile,
        out TacticalAIPlanCacheValue<TIntent> value)
    {
        TacticalAIPlanCacheKey key = TacticalAIPlanCacheKey.From(snapshot, profile);
        if (key == null)
        {
            value = null;
            return false;
        }

        return entries.TryGetValue(key, out value);
    }
}
