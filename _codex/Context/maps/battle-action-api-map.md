# TArenaUnity3D Battle Action API Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when a task touches tactical action validation, battle readiness,
turn lifecycle, battle snapshots, player/enemy action flow, move/attack/wait/
defend/stance actions, or the bridge between validated actions and live Unity
mutation.

This active map should describe current code, not history. Use
`_codex/Context/maps/combat-skill-ai-history-risks-map.md` only for regression
debugging, bug archaeology, or explicit historical questions.

## Sources

Read the smallest relevant subset:

- `_codex/Context/BattleActionRules.md`
- `_codex/agents/docs/codebase/battle-action-code-map.md`
- `_codex/agents/docs/codebase/menu-flow-code-map.md` when player input,
  selected unit state, or live action entry points matter
- `_codex/Context/maps/skill-api-map.md` when the action is a skill action

## Current Responsibility Boundary

Battle Action API owns tactical action legality and lifecycle coordination:

- represent submitted action requests,
- normalize validated actions,
- reject actions while battle state is blocking or not ready,
- validate move, move-and-attack, basic ranged attack, wait, defend, stance,
  and skill actions,
- expose preview/result shape for action consumers,
- apply validated actions through live adapters while legacy paths are still
  being migrated.

Battle Action API should not own AI scoring, route/metagame decisions, authored
skill data ownership, UI layout, persistence, or presentation timing.

## Current Code Surfaces

- `BattleActionModels.cs` - action request/action/result models.
- `BattleActionRules.cs` - validation and legality rules.
- `BattleActionLiveApplier.cs` - live application bridge.
- `BattleActionLifecycle.cs` - blocking/release lifecycle.
- `BattleSnapshotModels.cs` - copied battle state model.
- `BattleSnapshotLiveAdapter.cs` - live-to-snapshot adapter.
- `BattleSnapshotBuilder.cs` - snapshot construction.
- `MouseControler.cs` - player input and current live action adapter.
- `TurnManager.cs` - active unit/turn progression.
- `HexMap.cs` - battle scene readiness and map/unit ownership.

## Current Operating Rules

- Tactical actions should validate from snapshot/data before live mutation.
- AI and player action paths should converge through shared validation.
- Battle readiness gates action validation and AI startup.
- Non-skill live mutation may still delegate through existing
  `MouseControler.TryStart*` methods until a focused migration replaces each
  path.
- Skill actions delegate skill-specific legality and preview/result details to
  the Skill API.

## Boundary With Tactical AI

Battle Action API is the authority for action legality. Tactical AI can propose
an action, but the action must still be validated and applied through this
boundary.
