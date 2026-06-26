# TArenaUnity3D Tactical AI Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when a task touches Tactical AI planning, AI action selection,
enemy turn flow, async decision work, AI scoring, AI fallback behavior, or the
bridge from planned intent to validated live action.

This active map should describe current code, not history. Use
`_codex/Context/maps/combat-skill-ai-history-risks-map.md` only for regression
debugging, bug archaeology, or explicit historical questions.

## Sources

Read the smallest relevant subset:

- `_codex/Context/AI_Context.md`
- `_codex/agents/docs/codebase/menu-flow-code-map.md`
- `_codex/agents/docs/codebase/battle-action-code-map.md`
- `_codex/Context/maps/battle-action-api-map.md`
- `_codex/Context/maps/skill-api-map.md` when AI considers skill actions

## Current Responsibility Boundary

Tactical AI chooses candidate actions. It should not be the source of truth for
action legality or mutate live battle state directly.

AI should:

- read copied battle state/snapshot data,
- generate candidate actions,
- score and order candidates,
- submit a selected action to the validation/execution bridge,
- accept rejection when the live state has drifted.

AI should not own run routes, rewards, shops, saved-army defence, matchmaking,
strategic metagame decisions, UI state, skill rule truth, or battle lifecycle
truth.

## Current Code Surfaces

- `MostStupidAIEver.cs` - current enemy-turn entry point and legacy fallback.
- `TacticalAIAsyncDecisionPipeline.cs` - async planning pipeline.
- `TacticalAILiveTurnIntegrator.cs` - main-thread integration of planned work.
- `TacticalAISearchScoring.cs` - scoring/search behavior.
- `TacticalAIExecutionBridge.cs` - live revalidation/execution bridge.
- `TacticalAIPlannedAction.cs` - planned action data.
- `TacticalAIProfile.cs` - AI profile data.
- Legacy intent/candidate concepts may appear in tests or historical tasks, but
  current routing should start from the live Tactical AI files above.

## Current Operating Rules

- Planning should use copied snapshot/profile/skill metadata, not live Unity
  object mutation.
- Worker-thread planning results must be consumed on the main thread.
- Planned action execution must be rejected when actor, snapshot, profile, or
  battle state has drifted.
- Live execution should remain authoritative through the Battle Action API,
  `TacticalAIExecutionBridge`, `MouseControler`, and action lifecycle.
