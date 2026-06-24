# [TARENA] PRD046-D: Tactical AI 3-Ply Search And Scoring

- Status: implemented-qa-pass-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Search, Scoring, Turn Order
- Label: needs-grill
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/archive/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Depends on: `_codex/tasks/archive/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Depends on: `_codex/tasks/archive/046_E_PRD_TacticalAI_ProfileCache.md`

## Problem Statement

The first Tactical AI must not stop at a shallow greedy heuristic. It must use
3-ply search from the start, evaluate counterplay, and choose win-focused
actions while staying deterministic and bounded by `TacticalAIProfile`.

Build a pure search and scoring module that operates on `BattleSnapshot`,
candidate intents, approximate simulation, and profile-defined fixed budgets.

## Scope

This PRD covers:

- 3-ply search,
- opponent-response coverage for Normal 3-ply search,
- turn-order estimation between plies,
- minimax-style scoring,
- approximate simulation for basic actions and skills,
- average damage prediction,
- deterministic candidate ordering and pruning.

It does not cover live execution or ScriptableObject implementation details.

## Implementation Decisions

- Normal starts at `searchDepthPlies = 3`.
- Normal 3-ply search must include at least one opponent action opportunity
  when an opponent action is reachable in the simulated turn order. A line that
  only evaluates same-side AI opportunities is not sufficient counterplay
  coverage unless no opponent unit can act.
- A ply means the next actionable unit opportunity, not alternating sides.
- Search follows estimated turn order from the simulated snapshot.
- Same-side consecutive turns must be supported.
- Enemy-side consecutive turns must be supported.
- If a ply belongs to the AI team, search maximizes AI score.
- If a ply belongs to the player/opponent team, search minimizes AI score.
- Use average damage prediction for planning.
- Do not call live damage methods that mutate runtime state during planning.
- Skill prediction is approximate in V1. Live execution remains authoritative.
- Candidate pruning must be deterministic and profile-limited.

## Turn Order Rule

The search sequence may be:

```text
AI -> AI -> AI
AI -> AI -> Enemy
AI -> Enemy -> AI
AI -> Enemy -> Enemy
AI -> Enemy -> Enemy of same side again
```

depending on initiative, wait state, moved state, deaths, and current turn
state.

V1 should implement a pure `BattleSnapshotTurnOrderEstimator` that approximates
current `TurnManager` behavior closely enough for planning. It does not need to
perfectly model all automatic passive/end-round presentation sequencing in the
first version. Live `TurnManager` remains authoritative.

The opponent-response rule does not make AI strength CPU-scaled. It is a fixed
profile requirement: the search follows deterministic turn order and profile
beam/candidate caps until it has covered the configured depth and, when
reachable, at least one opponent response.

## Scoring Direction

Score should be from the AI team's perspective:

```text
positive = better for AI
negative = better for opponent
```

Initial scoring weights come from `TacticalAIProfile`:

- enemy value removed,
- own value lost,
- enemy stack kill bonus,
- own stack loss penalty,
- win/loss terminal score,
- damage efficiency,
- position/threat safety,
- progress/tempo,
- action type bias.

Own value lost should usually be weighted slightly stronger than enemy value
removed so the AI avoids needless losing trades.

The starting direction for Normal is roughly `ownValueLost = 1.2 *
enemyValueRemoved`, with final numbers owned by `TacticalAIProfile`.

Wait and defend remain legal fallback actions, but their action-type bias should
be lower than productive attack, move-and-attack, or valuable skill lines.

## Average Damage Prediction

Planning uses average damage:

```text
averageBaseDamage = (minDamage + maxDamage) / 2
```

Then apply existing visible modifiers that can be read safely from snapshot
state. The prediction helper must not:

- call `Random.Range`,
- mutate `SpecialPUREDMG`,
- mutate status state,
- mutate cooldowns,
- send chat messages,
- play animations or presentation.

## Approximate Simulation

The search module should simulate enough state changes to rank actions:

- movement changes position,
- damage changes amount/temp HP,
- death removes actionability,
- wait updates wait/action state,
- defend applies approximate defensive state,
- skills apply approximate predicted effects where possible,
- unknown/complex skill effects use conservative heuristic scoring.

If prediction for a skill is uncertain, the search may still choose it based on
heuristic value, but execution must revalidate and run through the live skill
bridge.

Uncertain skill prediction should produce a bounded diagnostic log path during
development so the team can identify skills that need future extraction or
better prediction.

## Testing Decisions

Add deterministic tests for:

- 3-ply search actually searches three opportunities when available,
- Normal search covers at least one opponent response when reachable,
- same-side consecutive turns,
- opponent consecutive turns,
- immediate kill preference,
- avoiding own stack loss,
- winning trade preference,
- wait/defend not selected forever when progress is available,
- average damage prediction is deterministic,
- profile weights change selected actions without changing legality,
- skill prediction uncertainty does not mutate live objects.

## Acceptance Criteria

Done when:

- search depth 3 is implemented for Normal profile,
- Normal search includes at least one opponent response when an opponent action
  opportunity is reachable,
- turn-order estimation is not hardcoded to alternate teams,
- AI/player plies maximize/minimize correctly,
- scoring is profile-driven,
- average damage prediction is pure and deterministic,
- search returns one best intent plus ordered fallback intents,
- no planning path mutates live Unity objects.

## Out Of Scope

- Full perfect skill simulation.
- Full action validation rewrite.
- Perfect `TurnManager` end-round passive simulation.
- Changing balance values or gameplay formulas used by live execution.

## Implementation - 2026-06-24

### What Changed

- Added `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`.
- `TacticalAISearchPlanner` and `TacticalAISearchEngine` build a profile-driven 3-ply plan from `BattleSnapshot`, return one best intent, ordered fallback intents, score, completed depth, opponent-response coverage, watchdog state, snapshot hash, and profile hash.
- `BattleSnapshotTurnOrderEstimator` approximates current `TurnManager` / `TeamClass` turn choice from pure snapshot state, including same-side consecutive turns, opponent consecutive turns, wait ordering, moved-state skipping, and new-round reset.
- `TacticalAISnapshotSimulator`, `TacticalAIDamagePredictor`, and `TacticalAISnapshotScorer` add approximate simulation, deterministic average damage, and profile-weighted board scoring from the AI team's perspective.
- `TacticalAISearchCandidateExpander` expands generic skill candidates into target-aware enemy skill intents and keeps repeatable stance/toggle skills targetless, then reapplies profile candidate caps.
- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs` with focused pure EditMode coverage.
- No Inspector fields changed. No scene, prefab, material, controller, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files were edited.
- No new tuning fields were added. `046_D` uses existing `TacticalAIProfile` values: lower depth/beam/caps reduce search breadth, higher values broaden planning cost, and scoring/bias weights remain the tuning surface.

### Automatic Test

- Added `TacticalAISearchScoringTests`.
- The tests cover completed 3-ply search, opponent-response coverage, bounded skill expansion after target generation, same-side consecutive turn order, opponent consecutive turn order, deterministic average damage without mutation, immediate kill target preference, and profile bias changing selected action.
- These tests use handcrafted `BattleSnapshot` data and do not require scene, prefab, `MouseControler`, `CastManager`, `HexMap`, or live Unity objects.
- Tests were not run automatically. Run them manually in Unity: open `Window > General > Test Runner`, switch to `EditMode`, then run `TacticalAISearchScoringTests`. Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No new scene or Inspector setup is required for the pure planner.
- Open an existing tactical battle scene that already contains `HexMap`, `MouseControler`, `TurnManager`, `CastManager`, and `BattleActionLifecycle`.
- If manual probing is needed, use an existing temporary `TacticalAISnapshotProbe` object or add it to a temporary scene object for inspection only.

#### Play Mode Test

- Enter Play Mode and wait until one tactical unit is active.
- Capture a live snapshot through `TacticalAISnapshotProbe`.
- In a debugger/watch or temporary local call, run `TacticalAISearchEngine.Search(...)` or `new TacticalAISearchPlanner().BuildPlan(...)` on the captured snapshot.
- Confirm the returned plan has a best intent, ordered fallback intents, a planned snapshot hash, and profile hash.
- Pass `plan.OrderedActionIntents` into `TacticalAIExecutionBridge.TryExecuteOrderedIntents(...)`.
- Confirm live execution still revalidates through the PRD046-C/F bridge and falls back safely if the selected intent is stale or rejected.

### QA Verdict

- Final QA verdict: Pass.
- Initial QA report: `_codex/tasks/QA/2026-06-24_2201_046_D_QA_ArchitectureReview.md`.
- Final QA report: `_codex/tasks/QA/2026-06-24_2202_046_D_QA_ArchitectureReview.md`.
- Initial actionable finding: skill target expansion and root search could exceed fixed profile budget.
- Follow-up fix applied: expanded candidates are capped after skill target expansion, and root candidates are scored/pruned through the own-side beam before recursive search.
- Final actionable findings: none.
- Non-blocking observation: skill prediction remains approximate and Play Mode validation should watch how often selected skill intents reject through the CastManager bridge.

### Notes

- Planning remains pure and does not reference `UnityEngine`, `Random`, `MouseControler`, `CastManager`, `TosterHexUnit`, `HexClass`, presentation, chat, or live execution APIs.
- Skill prediction is intentionally approximate. Live skill legality and execution remain authoritative in `TacticalAIExecutionBridge` and `TacticalAICastManagerSkillIntentExecutor`.
- The turn-order estimator approximates current turn behavior but does not model every passive/end-round presentation detail.
- The planner is not yet wired into a live enemy AI controller; it exposes the pure API needed for that integration.

### Next Steps

- Run `TacticalAISearchScoringTests` manually in Unity EditMode Test Runner.
- Do one Play Mode probe with `TacticalAISnapshotProbe`, then execute the returned ordered intents through `TacticalAIExecutionBridge`.
- During Play Mode, watch Unity Console for rejected skill intent diagnostics, especially for skills that need richer future prediction.
