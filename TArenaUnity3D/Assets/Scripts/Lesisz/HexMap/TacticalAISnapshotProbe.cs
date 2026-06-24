using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TacticalAISnapshotProbe : MonoBehaviour
{
    [Header("Optional Scene References")]
    [SerializeField] private HexMap hexMap;
    [SerializeField] private MouseControler mouseControler;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BattleActionLifecycle battleActionLifecycle;
    [SerializeField] private TacticalAIProfile tacticalAIProfile;

    [Header("Debug Output")]
    [SerializeField] private bool autoFindMissingReferences = true;
    [SerializeField] private bool recaptureSnapshotBeforeGenerating = true;
    [SerializeField] private bool autoCaptureWhenBattleReady = true;
    [SerializeField] private bool autoGenerateWhenBattleReady = true;
    [SerializeField] private float autoProbeIntervalSeconds = 0.5f;
    [SerializeField] private string lastSnapshotHash = string.Empty;
    [SerializeField] private string lastActiveUnitId = string.Empty;
    [SerializeField] private int lastCandidateCount;
    [SerializeField] [TextArea(8, 20)] private string lastSnapshotSummary = string.Empty;
    [SerializeField] [TextArea(8, 30)] private string lastCandidateSummary = string.Empty;
    [SerializeField] [TextArea(5, 20)] private string lastExecutionSummary = string.Empty;

    [System.NonSerialized] private BattleSnapshot lastSnapshot;
    [System.NonSerialized] private List<TacticalAIActionIntent> lastCandidates = new List<TacticalAIActionIntent>();
    [System.NonSerialized] private float nextAutoProbeTime;
    [System.NonSerialized] private bool autoProbeCompletedForCurrentScene;
    [System.NonSerialized] private string lastAutoProbeSceneName = string.Empty;

    void Update()
    {
        if (Application.isPlaying == false)
        {
            return;
        }

        if (autoCaptureWhenBattleReady == false && autoGenerateWhenBattleReady == false)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(lastAutoProbeSceneName, sceneName) == false)
        {
            lastAutoProbeSceneName = sceneName;
            autoProbeCompletedForCurrentScene = false;
        }

        if (Time.unscaledTime < nextAutoProbeTime)
        {
            return;
        }

        nextAutoProbeTime = Time.unscaledTime + Mathf.Max(0.1f, autoProbeIntervalSeconds);
        ResolveReferencesIfNeeded();

        if (HasReadyBattleReferences() == false)
        {
            autoProbeCompletedForCurrentScene = false;
            SetWaitingForBattleSummary(sceneName);
            return;
        }

        if (autoProbeCompletedForCurrentScene)
        {
            return;
        }

        if (autoCaptureWhenBattleReady)
        {
            CaptureSnapshot();
        }

        if (autoGenerateWhenBattleReady)
        {
            GenerateCandidates();
        }

        autoProbeCompletedForCurrentScene = true;
    }

    [ContextMenu("Capture Tactical AI Snapshot")]
    public void CaptureSnapshot()
    {
        ResolveReferencesIfNeeded();

        lastSnapshot = BattleSnapshotLiveAdapter.BuildSnapshot(
            hexMap,
            mouseControler,
            turnManager,
            battleActionLifecycle != null ? battleActionLifecycle : BattleActionLifecycle.Instance);

        if (lastSnapshot == null)
        {
            lastSnapshotHash = string.Empty;
            lastActiveUnitId = string.Empty;
            lastSnapshotSummary = "Snapshot build failed. Missing live tactical scene references.";
            lastCandidateSummary = "No tactical battle runtime found yet. Enter the actual battle scene with HexMap, MouseControler, TurnManager, and BattleActionLifecycle.";
            Debug.LogWarning("[TacticalAISnapshotProbe] Snapshot build failed.");
            return;
        }

        lastSnapshotHash = lastSnapshot.SnapshotHash ?? string.Empty;
        lastActiveUnitId = lastSnapshot.ActiveUnitId ?? string.Empty;
        lastSnapshotSummary = BuildSnapshotSummary(lastSnapshot);
        Debug.Log("[TacticalAISnapshotProbe] Captured snapshot:\n" + lastSnapshotSummary);
    }

    [ContextMenu("Generate Tactical AI Candidates")]
    public void GenerateCandidates()
    {
        if (recaptureSnapshotBeforeGenerating || lastSnapshot == null)
        {
            CaptureSnapshot();
        }

        if (lastSnapshot == null)
        {
            lastCandidateCount = 0;
            lastCandidateSummary = "Candidate generation skipped because no snapshot is available.";
            return;
        }

        lastCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            lastSnapshot,
            ResolveCandidateOptions());

        lastCandidateCount = lastCandidates != null ? lastCandidates.Count : 0;
        lastCandidateSummary = BuildCandidateSummary(lastCandidates);
        Debug.Log("[TacticalAISnapshotProbe] Generated candidates:\n" + lastCandidateSummary);
    }

    [ContextMenu("Capture Snapshot And Generate Candidates")]
    public void CaptureAndGenerateCandidates()
    {
        CaptureSnapshot();
        GenerateCandidates();
    }

    [ContextMenu("Execute Tactical AI Candidates Through Bridge")]
    public void ExecuteCandidatesThroughBridge()
    {
        ResolveReferencesIfNeeded();

        if (lastSnapshot == null || lastCandidates == null || lastCandidates.Count == 0)
        {
            CaptureAndGenerateCandidates();
        }

        TacticalAIExecutionBridge bridge = new TacticalAIExecutionBridge(
            new TacticalAIExecutionRuntimeContext(
                hexMap,
                mouseControler,
                turnManager,
                battleActionLifecycle != null ? battleActionLifecycle : BattleActionLifecycle.Instance),
            tacticalAIProfile);

        TacticalAIExecutionResult result = bridge.TryExecuteOrderedIntents(lastCandidates, lastSnapshot);
        lastExecutionSummary = BuildExecutionSummary(result);
        Debug.Log("[TacticalAISnapshotProbe] Execution result:\n" + lastExecutionSummary);
    }

    public BattleSnapshot GetLastSnapshot()
    {
        return lastSnapshot;
    }

    public List<TacticalAIActionIntent> GetLastCandidates()
    {
        return lastCandidates == null
            ? new List<TacticalAIActionIntent>()
            : new List<TacticalAIActionIntent>(lastCandidates);
    }

    void ResolveReferencesIfNeeded()
    {
        if (autoFindMissingReferences == false)
        {
            return;
        }

        if (hexMap == null)
        {
            hexMap = FindObjectOfType<HexMap>();
        }

        if (mouseControler == null)
        {
            mouseControler = FindObjectOfType<MouseControler>();
        }

        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        if (battleActionLifecycle == null)
        {
            battleActionLifecycle = BattleActionLifecycle.Instance != null
                ? BattleActionLifecycle.Instance
                : FindObjectOfType<BattleActionLifecycle>();
        }
    }

    bool HasReadyBattleReferences()
    {
        return hexMap != null && mouseControler != null && turnManager != null;
    }

    void SetWaitingForBattleSummary(string sceneName)
    {
        if (string.IsNullOrEmpty(lastSnapshotHash) == false || string.IsNullOrEmpty(lastSnapshotSummary) == false)
        {
            return;
        }

        lastSnapshotSummary =
            "Waiting for tactical battle scene runtime.\n" +
            "Current scene: " + sceneName + "\n" +
            "Required live objects: HexMap, MouseControler, TurnManager.";
        lastCandidateSummary =
            "No candidates yet.\n" +
            "This probe starts working only after the actual tactical battle scene is loaded and battle runtime objects exist.";
    }

    TacticalAICandidateGenerationOptions ResolveCandidateOptions()
    {
        if (tacticalAIProfile == null)
        {
            return TacticalAICandidateGenerationOptions.Default;
        }

        TacticalAIResolvedProfile resolvedProfile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(tacticalAIProfile);
        return new TacticalAICandidateGenerationOptions
        {
            MaxCandidatesPerActionType = resolvedProfile.MaxCandidatesPerActionType,
            MaxSkillCandidates = resolvedProfile.MaxSkillCandidates,
            MaxMoveCandidates = resolvedProfile.MaxMoveCandidates,
            MaxAttackCandidates = resolvedProfile.MaxAttackCandidates
        };
    }

    static string BuildSnapshotSummary(BattleSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "No snapshot.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Hash: " + snapshot.SnapshotHash);
        builder.AppendLine("Active Unit: " + snapshot.ActiveUnitId);
        builder.AppendLine("Map: " + snapshot.MapWidth + "x" + snapshot.MapHeight);
        builder.AppendLine("Units: " + (snapshot.Units == null ? 0 : snapshot.Units.Count));
        builder.AppendLine("Action Blocking: " + (snapshot.TurnState != null && snapshot.TurnState.IsActionBlocking));

        if (snapshot.Units != null)
        {
            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                BattleUnitSnapshot unit = snapshot.Units[i];
                if (unit == null)
                {
                    continue;
                }

                builder.Append("- ")
                    .Append(unit.RuntimeUnitId)
                    .Append(" @ ")
                    .Append(unit.C)
                    .Append(",")
                    .Append(unit.R)
                    .Append(" amount=")
                    .Append(unit.Amount)
                    .Append(" movedThisTurn=")
                    .Append(unit.MovedThisTurn)
                    .Append(" usedSkillThisTurn=")
                    .Append(unit.UsedSkillThisTurn)
                    .AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }

    static string BuildCandidateSummary(List<TacticalAIActionIntent> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return "No candidates generated.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Candidates: " + candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            TacticalAIActionIntent candidate = candidates[i];
            builder.Append(i)
                .Append(". ")
                .Append(candidate.ActionType)
                .Append(" actor=")
                .Append(candidate.ActorUnitId);

            if (candidate.DestinationHex != null)
            {
                builder.Append(" dest=")
                    .Append(candidate.DestinationHex.C)
                    .Append(",")
                    .Append(candidate.DestinationHex.R);
            }

            if (string.IsNullOrEmpty(candidate.TargetUnitId) == false)
            {
                builder.Append(" target=").Append(candidate.TargetUnitId);
            }

            if (candidate.TargetHex != null)
            {
                builder.Append(" targetHex=")
                    .Append(candidate.TargetHex.C)
                    .Append(",")
                    .Append(candidate.TargetHex.R);
            }

            if (candidate.SkillSlot >= 0)
            {
                builder.Append(" skillSlot=").Append(candidate.SkillSlot)
                    .Append(" skillId=").Append(candidate.SkillId);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    static string BuildExecutionSummary(TacticalAIExecutionResult result)
    {
        if (result == null)
        {
            return "No execution result.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Status: " + result.Status);
        builder.AppendLine("Message: " + result.Message);
        if (result.ExecutedIntent != null)
        {
            builder.AppendLine("Executed: " + result.ExecutedIntent.ActionType + " actor=" + result.ExecutedIntent.ActorUnitId);
        }

        builder.AppendLine("Attempts: " + (result.Attempts == null ? 0 : result.Attempts.Count));
        if (result.Attempts != null)
        {
            for (int i = 0; i < result.Attempts.Count; i++)
            {
                TacticalAIExecutionAttempt attempt = result.Attempts[i];
                if (attempt == null || attempt.Intent == null)
                {
                    continue;
                }

                builder.Append(i)
                    .Append(". ")
                    .Append(attempt.Intent.ActionType)
                    .Append(" started=")
                    .Append(attempt.Started)
                    .Append(" reason=")
                    .AppendLine(attempt.FailureReason);
            }
        }

        return builder.ToString().TrimEnd();
    }
}
