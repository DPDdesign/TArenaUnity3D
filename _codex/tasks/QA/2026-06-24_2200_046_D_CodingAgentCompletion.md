# [TARENA] Coding Agent Completion - PRD046-D

Task: `_codex/tasks/046_D_PRD_TacticalAI_SearchScoring.md`
Date: 2026-06-24
Agent: Coding Agent

## Implementation Summary

Implemented PRD046-D as a pure Tactical AI planning module in:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`

The module adds:

- `TacticalAISearchPlanner` and `TacticalAISearchEngine` for profile-driven 3-ply search.
- `TacticalAISearchPlan` for best intent, ordered fallback intents, score, completed depth, opponent-response coverage, watchdog state, snapshot hash, and profile hash.
- `BattleSnapshotTurnOrderEstimator` for pure turn-order approximation based on snapshot unit state, initiative, movement speed, wait state, moved state, team index, and roster order.
- `TacticalAISnapshotSimulator` for approximate snapshot simulation of move, move-and-attack, ranged attack, wait, defend, and skill intents.
- `TacticalAIDamagePredictor` for deterministic average-damage prediction without `Random.Range`, live runtime mutation, presentation, chat, cooldown, or status mutation.
- `TacticalAISnapshotScorer` for profile-weighted score from the AI team's perspective.
- `TacticalAISearchCandidateExpander` to expand generic skill candidates into target-aware skill intents for enemy targets while preserving repeatable stance/toggle skills as targetless intents.
- `TacticalAISnapshotQuery` helper methods for pure snapshot lookup, cloning, team value, durability, distance, and liveness checks.

## Scope Notes

- No Unity scene, prefab, material, controller, `.inputactions`, generated file, `.asmdef`, or `.asmref` edits were made.
- No public or serialized Inspector fields were added, removed, or renamed.
- No gameplay float balance values were changed.
- No live Unity object references are stored or used by the planning module.
- No calls were added to `MouseControler`, `CastManager`, `TosterHexUnit`, `HexClass`, `Random.Range`, animation, presentation, chat, or live skill execution paths.
- Skill planning remains approximate. Live skill legality and execution remain owned by the PRD046-F CastManager bridge and PRD046-C execution bridge.

## Architecture Fit

- `BattleSnapshot` remains the input authority for planning.
- `TacticalAICandidateGenerator` remains the first candidate source.
- `TacticalAIProfile` controls depth, beam width, candidate caps, scoring weights, action biases, opponent-response requirement, and watchdog.
- `TacticalAIPlanCache` from PRD046-E is used only as an advisory cache inside `TacticalAISearchPlanner`.
- Search returns ordered fallback intents for the existing `TacticalAIExecutionBridge.TryExecuteOrderedIntents(...)` path.
- The execution bridge still performs live revalidation and remains authoritative before any action starts.

## Acceptance Coverage

- Normal profile depth starts from `searchDepthPlies = 3`.
- A ply is modeled as the next estimated actionable unit opportunity.
- Turn-order estimation supports same-side consecutive turns and opponent-side consecutive turns.
- AI-side plies maximize score and opponent plies minimize score.
- Candidate pruning uses profile beam widths and candidate caps.
- Opponent-response coverage is tracked and preferred when the profile requires it.
- Average damage prediction uses `(minDamage + maxDamage) / 2` plus snapshot-visible attack/defense/status modifiers.
- Planning returns one best intent and an ordered fallback list.
- Planning does not mutate the input snapshot or live Unity objects.

## Verification Performed

- Read relevant task, coding agent, task tracker, Unity coding, testing, codebase map, AI/action-rule context, and PRD046 A/B/C/E/F files.
- Read updated PRD046-F implementation notes and code.
- Inspected related Tactical AI source files:
  - `BattleSnapshotModels.cs`
  - `BattleSnapshotBuilder.cs`
  - `BattleSnapshotLiveAdapter.cs`
  - `TacticalAIActionIntent.cs`
  - `TacticalAICandidateGenerator.cs`
  - `TacticalAIProfile.cs`
  - `TacticalAIIntentRevalidator.cs`
  - `TacticalAIExecutionBridge.cs`
  - `TacticalAICastManagerSkillIntentExecutor.cs`
  - `TurnManager.cs`
  - `TeamClass.cs`
- Searched the new implementation for forbidden live-planning dependencies such as `Random`, `MouseControler`, `CastManager`, `HexClass`, `TosterHexUnit`, `Debug.Log`, and `UnityEngine`; none are referenced by the new planning file.

## Tests

Per local `/implement` workflow, focused EditMode tests are intentionally deferred until after QA Architecture Review and any focused follow-up fixes.

Recommended post-QA test coverage:

- 3-ply search returns completed depth 3 when three opportunities are available.
- opponent-response coverage is true when an opponent opportunity is reachable.
- same-side consecutive turn estimation.
- opponent consecutive turn estimation.
- immediate kill preference.
- own stack loss avoidance through scoring weights.
- wait/defend losing to productive progress.
- deterministic average damage.
- profile weight changes alter selected intent without changing candidate legality.
- skill target expansion does not mutate input snapshots.

## Manual Unity Verification To Run After Tests Exist

- Open an existing tactical battle scene with `HexMap`, `MouseControler`, `TurnManager`, `CastManager`, and `BattleActionLifecycle`.
- Capture a snapshot through `TacticalAISnapshotProbe`.
- In a temporary local watch/debug call, run `TacticalAISearchEngine.Search(...)` or `new TacticalAISearchPlanner().BuildPlan(...)` on the captured snapshot.
- Confirm the plan returns ordered intents for the current active unit.
- Pass the returned `OrderedActionIntents` into `TacticalAIExecutionBridge.TryExecuteOrderedIntents(...)`.
- Confirm execution still revalidates live state and uses the existing bridge/fallback path.

## Known Limitations

- Skill prediction is intentionally approximate and mostly target/damage heuristic based.
- The estimator approximates the current `TurnManager` turn order and does not attempt full passive/end-round presentation simulation.
- The planner is not yet wired into a live enemy AI controller; it exposes a pure planning API for the next integration step.
- Unity Test Runner execution was not run automatically because project rules leave Unity compilation and tests to the user unless explicitly allowed.
