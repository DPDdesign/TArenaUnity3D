# [TARENA] PRD046-C: Tactical AI Lifecycle Execution Bridge

- Status: implemented-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Action Lifecycle, Execution Adapter
- Label: needs-grill
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/archive/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Depends on: `_codex/tasks/archive/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- Related: `_codex/Documentation/ADR_014_TacticalAI_MultiplayerCompatibleValidation.md`
- Related: `_codex/tasks/018_PRD_BattleActionLifecycleFullMigration.md`

## Problem Statement

AI planning must not execute by mutating tactical scene objects directly. The
selected AI action must enter the same live action lifecycle as player actions
so turn completion, presentation blocking, action consumption, and cleanup stay
centralized.

Build a live execution bridge that maps a selected `TacticalAIActionIntent` to
the existing runtime action paths and revalidates the live state immediately
before execution.

## Scope

This PRD covers execution of selected intents through current live runtime
methods. It does not define search, scoring, profile tuning, or snapshot
building.

## Implementation Decisions

- AI execution must call existing live action paths, not direct model mutation.
- AI execution must never bypass `MouseControler`, `CastManager`,
  `BattleActionLifecycle`, or the current live validation path just because an
  intent came from AI search.
- Basic actions should route through existing `MouseControler` lifecycle entry
  points where available:
  - `TryStartMoveAction`,
  - `TryStartMoveAndAttackAction`,
  - `TryStartBasicRangedAttackAction`,
  - `TryStartWaitAction`,
  - `TryStartDefenseAction`.
- Skill actions route through the CastManager bridge detailed in PRD046-F.
- The bridge must re-resolve the live actor from the intent runtime id and
  sanity checks.
- If revalidation fails, the AI must use fallback rather than forcing the
  action.
- The bridge must not create a second full validation module. Existing legacy
  checks remain authoritative per ADR005.
- The bridge should be shaped like the future online/network revalidation seam:
  cached, AI-selected, player-selected, and future network intents must all be
  able to converge on the same validated-action boundary.
- ADR014 records that this multiplayer-compatible validation boundary must be
  formalized in future work.

## Live Revalidation

Before execution, check:

```text
Actor:
- runtime unit id maps to a current live unit
- team index matches
- unit name/type matches
- source hex matches
- unit is alive and actionable
- stack amount matches or is compatible with the latest fresh snapshot

Action:
- selected action is still possible in live state
- destination/target still exists
- target is alive when needed
- current action lifecycle is not busy
- skill slot/id/cooldown still match for skill intents
```

Cached plans must always be revalidated. A cache hit never bypasses this bridge.
Fallback intents must also pass the same bridge. Fallback is not a legality
bypass.

## Fallback Chain

When selected execution fails:

```text
1. next best intent from the completed plan, if still legal
2. fresh greedy/legal action from current snapshot
3. defend
4. wait
5. safe no-op with warning if no legal action exists
```

Fallback must not bypass live legality checks.

## Testing Decisions

Add deterministic tests where possible for mapping intents to bridge requests.
Use Play Mode/manual validation for scene-owned behavior:

- movement intent executes through lifecycle,
- move-and-attack intent executes through lifecycle,
- basic ranged attack intent executes through lifecycle,
- wait and defend execute through lifecycle,
- stale actor/target/destination rejects and falls back,
- lifecycle busy rejects overlapping AI execution,
- cached intent and fallback intent both pass through the same revalidation
  path.

## Acceptance Criteria

Done when:

- selected AI intents can be resolved against live tactical state,
- every executed basic AI action enters the existing lifecycle path,
- stale or invalid intents are rejected safely,
- fallback is implemented,
- no AI execution path directly mutates live tactical state outside the
  existing runtime action paths.

## Out Of Scope

- Full action validation rewrite.
- Search/scoring decisions.
- Perfect skill prediction.
- Editing scene or prefab wiring unless later explicitly scoped.

## Implementation - 2026-06-24

### What Changed

- Added `TacticalAIIntentRevalidator.cs` as a pure live-revalidation seam for `TacticalAIActionIntent`:
  - validates the live active actor against `ActorUnitId` and `SourceHex`;
  - checks stale destination/target/skill-slot state against a fresh `BattleSnapshot`;
  - keeps validation adapter-sized and does not replace legacy `MouseControler` legality.
- Added `TacticalAIExecutionBridge.cs` as the PRD046-C runtime bridge:
  - builds a fresh live snapshot through `BattleSnapshotLiveAdapter`;
  - applies fallback ordering: ordered plan intents, fresh greedy legal intents, `Defend`, `Wait`;
  - routes basic actions through existing `MouseControler` lifecycle entrypoints;
  - rejects overlapping execution while `BattleActionLifecycle` is busy;
  - leaves skill execution behind an explicit `ITacticalAISkillIntentExecutor` seam for PRD046-F.
- Added `TacticalAIExecutionBridgeTests.cs` for deterministic coverage of:
  - stale active-unit rejection;
  - legal move revalidation;
  - cooldown-based skill rejection;
  - fallback queue ordering and deduplication.
- Extended `TacticalAISnapshotProbe.cs` with a debug execution hook so a generated candidate set can be pushed through the new bridge during Play Mode inspection without scene or prefab edits.
- No Inspector field changes were required on gameplay components.

### Automatic Test

- Added `TacticalAIExecutionBridgeTests` under `Assets/Scripts/Tests/EditMode/`.
- These tests cover the pure seams only:
  - `Revalidator_AcceptsLegalMoveIntentAgainstLiveSnapshot`;
  - `Revalidator_RejectsWhenIntentActorIsNotLiveActiveUnit`;
  - `Revalidator_RejectsSkillWhenLiveCooldownChanged`;
  - `FallbackPlanner_UsesPlanThenFreshGreedyThenDefendThenWait`;
  - `FallbackPlanner_DeduplicatesMatchingPlanAndFreshCandidate`.
- The tests use handcrafted `BattleSnapshot` data and do not require a scene, prefab, or live Unity object graph.
- Tests were not run automatically. Run them manually in Unity Test Runner:
  - open `Window > General > Test Runner`;
  - switch to the `EditMode` tab;
  - run `TacticalAIExecutionBridgeTests`.
- Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No scene, prefab, material, controller, `.inputactions`, `.asmdef`, or `.asmref` edits are required.
- Open an existing tactical battle scene that already contains `HexMap`, `MouseControler`, `TurnManager`, and `BattleActionLifecycle`.
- If you want a manual execution hook without wiring a new AI brain yet, add `TacticalAISnapshotProbe` to a temporary scene object, let it capture/generate a candidate list, then use the context menu `Execute Tactical AI Candidates Through Bridge`.

#### Play Mode Test

- Enter Play Mode and wait for one tactical unit to become active.
- Capture a live snapshot and generate candidates through `TacticalAISnapshotProbe` or a temporary local debug hook.
- Execute the generated ordered intents through `TacticalAIExecutionBridge.TryExecuteOrderedIntents(...)`.
- Confirm that a legal basic action starts only when the selected actor is still the live active unit and the source hex still matches.
- Confirm that movement, move-and-attack, ranged attack, wait, and defend all enter the existing `MouseControler` lifecycle path rather than directly mutating tactical state.
- Confirm that if the top planned intent becomes stale, the bridge rejects it and tries the next plan/fresh fallback instead of forcing execution.
- Confirm that if `BattleActionLifecycle` is already busy, the bridge reports a busy result and does not start an overlapping action.
- Confirm that skill intents currently reject cleanly with a message about the pending PRD046-F executor seam instead of bypassing legacy skill flow.

### Notes

- This slice intentionally stops at basic-action execution bridging plus skill-seam reservation. It does not implement the CastManager skill execution adapter; that remains PRD046-F.
- The revalidation seam is intentionally conservative and snapshot-backed. Final live legality still belongs to existing runtime checks inside `MouseControler`, per ADR005.
- Fresh fallback generation reuses the pure PRD046-B candidate generator instead of building a second parallel legality catalog.

### Next Steps

- Run `TacticalAIExecutionBridgeTests` manually in Unity EditMode Test Runner.
- Do one Play Mode pass with `TacticalAISnapshotProbe.Execute Tactical AI Candidates Through Bridge`.
- Implement the PRD046-F `ITacticalAISkillIntentExecutor` bridge so `Skill` intents can enter the same live execution path as basic actions.
