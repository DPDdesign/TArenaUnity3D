# [TARENA] PRD046-D: Tactical AI 3-Ply Search And Scoring

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, Search, Scoring, Turn Order
- Label: needs-grill
- Parent: `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Depends on: `_codex/tasks/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Depends on: `_codex/tasks/046_E_PRD_TacticalAI_ProfileCache.md`

## Problem Statement

The first Tactical AI must not stop at a shallow greedy heuristic. It must use
3-ply search from the start, evaluate counterplay, and choose win-focused
actions while staying deterministic and bounded by `TacticalAIProfile`.

Build a pure search and scoring module that operates on `BattleSnapshot`,
candidate intents, approximate simulation, and profile-defined fixed budgets.

## Scope

This PRD covers:

- 3-ply search,
- turn-order estimation between plies,
- minimax-style scoring,
- approximate simulation for basic actions and skills,
- average damage prediction,
- deterministic candidate ordering and pruning.

It does not cover live execution or ScriptableObject implementation details.

## Implementation Decisions

- Normal starts at `searchDepthPlies = 3`.
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

## Testing Decisions

Add deterministic tests for:

- 3-ply search actually searches three opportunities when available,
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

