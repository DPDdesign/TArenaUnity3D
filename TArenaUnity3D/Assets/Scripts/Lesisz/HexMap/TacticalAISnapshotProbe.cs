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
    [System.NonSerialized] private TacticalAISearchPlan lastSearchPlan;
    [System.NonSerialized] private List<TacticalAIPlannedAction> lastPlannedActions = new List<TacticalAIPlannedAction>();
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

    [ContextMenu("Generate Tactical AI Planned Actions")]
    public void GenerateCandidates()
    {
        if (recaptureSnapshotBeforeGenerating || lastSnapshot == null)
        {
            CaptureSnapshot();
        }

        if (lastSnapshot == null)
        {
            lastCandidateCount = 0;
            lastCandidateSummary = "Planned action generation skipped because no snapshot is available.";
            return;
        }

        TacticalAISearchPlanner planner = new TacticalAISearchPlanner();
        lastSearchPlan = planner.BuildPlan(
            lastSnapshot,
            ResolveProfile(),
            TacticalAIDataMapperSkillMetadataProvider.Instance);

        lastPlannedActions = lastSearchPlan != null && lastSearchPlan.OrderedActions != null
            ? new List<TacticalAIPlannedAction>(lastSearchPlan.OrderedActions)
            : new List<TacticalAIPlannedAction>();

        lastCandidateCount = lastPlannedActions.Count;
        lastCandidateSummary = BuildPlannedActionSummary(lastSearchPlan, lastPlannedActions);
        Debug.Log("[TacticalAISnapshotProbe] Generated planned actions:\n" + lastCandidateSummary);
    }

    [ContextMenu("Capture Snapshot And Generate Planned Actions")]
    public void CaptureAndGenerateCandidates()
    {
        CaptureSnapshot();
        GenerateCandidates();
    }

    [ContextMenu("Execute Tactical AI Planned Actions Through Bridge")]
    public void ExecuteCandidatesThroughBridge()
    {
        ResolveReferencesIfNeeded();

        if (lastSnapshot == null || lastPlannedActions == null || lastPlannedActions.Count == 0)
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

        TacticalAIExecutionResult result = bridge.TryExecuteOrderedActions(lastPlannedActions, lastSnapshot);
        lastExecutionSummary = BuildExecutionSummary(result);
        Debug.Log("[TacticalAISnapshotProbe] Execution result:\n" + lastExecutionSummary);
    }

    public BattleSnapshot GetLastSnapshot()
    {
        return lastSnapshot;
    }

    public TacticalAISearchPlan GetLastSearchPlan()
    {
        return lastSearchPlan;
    }

    public List<TacticalAIPlannedAction> GetLastPlannedActions()
    {
        return lastPlannedActions == null
            ? new List<TacticalAIPlannedAction>()
            : new List<TacticalAIPlannedAction>(lastPlannedActions);
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
            "No planned actions yet.\n" +
            "This probe starts working only after the actual tactical battle scene is loaded and battle runtime objects exist.";
    }

    TacticalAIResolvedProfile ResolveProfile()
    {
        return TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(tacticalAIProfile);
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

    static string BuildPlannedActionSummary(TacticalAISearchPlan plan, List<TacticalAIPlannedAction> plannedActions)
    {
        if (plannedActions == null || plannedActions.Count == 0)
        {
            return "No planned actions generated.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Planned Actions: " + plannedActions.Count);
        if (plan != null)
        {
            builder.AppendLine("Best Score: " + plan.BestScore.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            builder.AppendLine("Depth: " + plan.CompletedDepth);
            builder.AppendLine("Snapshot: " + (plan.PlannedSnapshotHash ?? string.Empty));
        }

        for (int i = 0; i < plannedActions.Count; i++)
        {
            TacticalAIPlannedAction plannedAction = plannedActions[i];
            builder.Append(i)
                .Append(". ")
                .Append(BuildActionSummary(plannedAction))
                .AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    static string BuildActionSummary(TacticalAIPlannedAction plannedAction)
    {
        if (plannedAction == null)
        {
            return "null planned action";
        }

        BattleAction action = plannedAction.Action;
        BattleActionUse use = plannedAction.Use;
        BattleActionKind kind = action != null ? action.ActionKind : plannedAction.ActionKind;
        string actorUnitId = action != null
            ? action.ActorUnitId
            : use != null
                ? use.ActorUnitId
                : plannedAction.ActorUnitId;

        StringBuilder builder = new StringBuilder();
        builder.Append(kind)
            .Append(" actor=")
            .Append(actorUnitId ?? string.Empty);

        HexCoord destinationHex = action != null ? action.DestinationHex : FirstSelectedHex(use);
        if (destinationHex != null)
        {
            builder.Append(" dest=")
                .Append(destinationHex.C)
                .Append(",")
                .Append(destinationHex.R);
        }

        string targetUnitId = action != null
            ? action.PrimaryTargetUnitId
            : use != null
                ? use.TargetUnitId
                : string.Empty;
        if (string.IsNullOrEmpty(targetUnitId) == false)
        {
            builder.Append(" target=").Append(targetUnitId);
        }

        HexCoord impactHex = action != null ? action.ImpactHex : LastSelectedHex(use);
        if (impactHex != null)
        {
            builder.Append(" impact=")
                .Append(impactHex.C)
                .Append(",")
                .Append(impactHex.R);
        }

        int skillSlot = action != null ? action.SkillSlot : use != null ? use.SkillSlot : -1;
        string skillId = action != null ? action.SkillId : use != null ? use.SkillId : string.Empty;
        if (skillSlot >= 0 || string.IsNullOrEmpty(skillId) == false)
        {
            builder.Append(" skillSlot=").Append(skillSlot)
                .Append(" skillId=").Append(skillId ?? string.Empty);
        }

        if (action != null)
        {
            builder.Append(" endsTurn=").Append(action.EndsTurn)
                .Append(" key=").Append(action.StableOrderKey ?? string.Empty);
        }

        return builder.ToString().TrimEnd();
    }

    static HexCoord FirstSelectedHex(BattleActionUse use)
    {
        return use != null && use.SelectedHexes != null && use.SelectedHexes.Count > 0
            ? use.SelectedHexes[0]
            : null;
    }

    static HexCoord LastSelectedHex(BattleActionUse use)
    {
        return use != null && use.SelectedHexes != null && use.SelectedHexes.Count > 0
            ? use.SelectedHexes[use.SelectedHexes.Count - 1]
            : null;
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
        if (result.ExecutedAction != null)
        {
            builder.AppendLine("Executed: " + BuildActionSummary(result.ExecutedAction));
        }

        builder.AppendLine("Attempts: " + (result.Attempts == null ? 0 : result.Attempts.Count));
        if (result.Attempts != null)
        {
            for (int i = 0; i < result.Attempts.Count; i++)
            {
                TacticalAIExecutionAttempt attempt = result.Attempts[i];
                if (attempt == null || attempt.Action == null)
                {
                    continue;
                }

                builder.Append(i)
                    .Append(". ")
                    .Append(BuildActionSummary(attempt.Action))
                    .Append(" started=")
                    .Append(attempt.Started)
                    .Append(" reason=")
                    .AppendLine(attempt.FailureReason);
            }
        }

        return builder.ToString().TrimEnd();
    }
}
