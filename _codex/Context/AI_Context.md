# AI Context

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-24

This file captures stable TArenaUnity3D AI context for future planning and
implementation work.

## Purpose

Route Tactical Battle AI questions here after reading the specific task or PRD.
This context is about battle-level enemy action choice, not run-route AI,
reward choice AI, matchmaking, or saved-army defence strategy.

## What Belongs Here

- Project-specific decisions for TArenaUnity3D.
- Current facts verified in this repository.
- Small notes that help future agents avoid re-discovering stable context.

## Current Tactical Battle AI State

PRD046 Tactical Battle AI V1 has been implemented as a C# architecture slice
with QA pass on the reviewed child PRDs and user acceptance for the current
state.

Implemented PRD046/047 surfaces:

- `BattleSnapshot` captures tactical map/unit/action state for pure planning
  without Unity object references.
- `TacticalAIActionIntent` and `TacticalAICandidateGenerator` represent legal
  snapshot-level move, attack, wait, defend, and skill candidates.
- `TacticalAIExecutionBridge` revalidates ordered intents against live battle
  state and executes through existing `MouseControler`, `CastManager`, and
  `BattleActionLifecycle` paths.
- `TacticalAIProfile` and `TacticalAIPlanCache` hold fixed-budget AI strength,
  deterministic profile values, and advisory cache state.
- `TacticalAISearchScoring` provides the 3-ply search/scoring planner with
  deterministic pruning and ordered fallback intents.
- `TacticalAICastManagerSkillIntentExecutor` bridges AI skill intents back into
  the legacy `CastManager` skill execution path.
- `TacticalAILiveTurnIntegrator` connects Tactical AI to the current
  `MostStupidAIEver` enemy-turn entry point, with legacy AI still available as
  fallback.
- `TacticalAIAsyncTurnIntegrator` runs planning on a worker task from copied
  immutable input, then consumes results on the main thread through the same
  live execution bridge.

## Current AI Contract

- Tactical AI V1 is battle-only. It does not choose run paths, rewards, shops,
  saved army defence, or matchmaking.
- The AI uses full battle information and emits one ordered intent list for the
  currently active unit.
- Planning must stay pure: snapshot/profile/skill metadata copies are allowed;
  Unity objects, `Resources`, `DataMapper`, scene lookups, and live mutation are
  not allowed inside worker-thread search.
- Live execution remains authoritative. Every planned action must revalidate
  through `TacticalAIExecutionBridge` before it mutates the battle.
- Difficulty is profile-budgeted. Do not change unit stats, cooldowns,
  movement, damage, skill targeting, or gameplay floats to make AI stronger.
- Cached and async plans are advisory only and must be rejected when snapshot
  hash, actor id, or profile hash no longer match.
- The legacy `MostStupidAIEver` path remains as a temporary safety fallback
  when Tactical AI cannot produce or execute a valid action.

## Current Follow-Ups

- Unity compilation, EditMode test execution, and Play Mode validation remain
  user-side unless a later task explicitly allows running Unity commands.
- PRD047 used the lower-risk integration point at enemy-turn entry. A future
  focused slice may start async planning earlier after logical battle-state
  commit so thinking time is hidden behind presentation.
- Watch the PRD047 async completion diagnostic formatting; QA noted a
  non-blocking duplicated tactical-AI tag in one log line.

## What Must Not Be Copied Here

- Do not import design decisions, current state, tasks, milestones, enemy lists,
  skills, map designs, or gameplay truth from another project unless the user
  explicitly asks for a comparison or migration note.
- Do not reference another project's local files as default context.

## Related Sources

- `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- `_codex/tasks/archive/047_PRD_TacticalAI_AsyncDecisionPipeline.md`
- `_codex/Documentation/ADR_013_TacticalAI_CastManagerSkillBridge.md`
- `_codex/Documentation/ADR_014_TacticalAI_MultiplayerCompatibleValidation.md`
- `_codex/Context/BattleActionRules.md`
