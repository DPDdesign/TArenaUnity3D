# [TARENA] PRD050 Active Close Pass - Coding Agent Completion

Date: 2026-06-26
Task source: `_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIRevalidatedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`

## Deleted Files

- None.
- Attempted to delete `SkillQuery.cs`, but Windows returned access denied. The file was neutralized to a comment-only non-API source file instead.

## Migrated Action Kinds / Runtime Surfaces

- Active skill and stance live execution now runs through:
  `BattleActionUse -> BattleAction -> BattleActionResult -> BattleActionLiveApplier`.
- `BattleActionLiveApplier` no longer delegates skill execution to a separate `ITacticalAISkillActionExecutor`.
- Former `TacticalAISkillRulesExecutor` runtime authority was removed. The remaining helper in that file is `BattleActionSkillResultRuntime`, which consumes `BattleActionResult` events.
- Player selected-skill commit no longer falls back to `CastManager.startSpell`; the old `startSpell` RPC name was replaced with `SubmitSelectedSkillTarget`.
- Tactical AI planned/revalidated DTOs no longer carry duplicate `ValidatedSkillCast`; skill payload remains only inside `BattleAction.SkillCast`.
- `SkillRules.Apply(..., ISkillRuntime)` and `ISkillRuntime` were removed.
- `SkillQuery` no longer exposes a separate skill-only API.

## Source Audit

Passed text audit for removed runtime split symbols:

- `ValidatedSkillCast`
- `ITacticalAISkillActionExecutor`
- `TacticalAISkillRulesExecutor`
- `TacticalAISkillRuntime`
- `TryExecuteSkillAction`
- `ISkillRuntime`
- `SkillRules.Apply(`
- `SkillQuery.`

Passed text audit for disabled legacy blocks:

- no `#if false`
- no `#if 0`

Passed runtime call-site audit for `CastManager` authority:

- no `castManager.startSpell`
- no `castManager.getMode`
- no code call sites to `CastManager.startSpell`

Remaining legacy names found:

- `CastManager.cs` remains in the project. It is not referenced as skill execution authority by current code, but cannot be safely deleted without Unity scene/Inspector validation.
- `MouseControler.castManager` remains as a scene-wired holder/cleanup reference.
- `SkillUse`, `SkillCast`, `SkillResult`, and `SkillRules` remain as Battle Action skill-resolution internals and focused test surfaces.

## Remaining Blockers

PRD050 should not be marked fully closed yet.

Blocking reason:

- Passive/trap/automatic tactical mutations still have legacy authority hooks outside Battle Action result/apply:
  - `TosterHexUnit.SetHex(...)` directly triggers traps and applies trap statuses/removal.
  - `TosterHexUnit.ResolveNewTurnSpell(...)` directly calls `SpellOverTime.DoTurn()`.
  - `TosterHexUnit.QueueAutocastsForNextTurn(...)` directly queues autocast spells.
  - `SpellOverTime.DoTurn()` contains direct passive/over-time mutation logic.

These need a follow-up migration into automatic `BattleActionResult` application before the acceptance criterion "passive/trap/automatic actions no longer bypass Battle Action result/apply authority" is true.

## Tests Added / Updated

- Updated `TacticalAIExecutionBridgeTests` to assert `BattleActionLiveApplier` as the skill execution surface and `BattleAction.SkillCast` as the only skill payload.
- Updated `TacticalAISearchScoringTests` assertions from `ValidatedSkillCast` to `BattleAction.SkillCast`.

## Tests Not Run

Per project rules, no Unity build, `dotnet`, package restore, or Unity test command was run from the agent session.

## Manual Unity Checks Required

- Unity compile.
- EditMode tests:
  - `TacticalAIExecutionBridgeTests`
  - `TacticalAISearchScoringTests`
  - `BattleActionRulesTests`
  - `SkillRulesTests`
- Play Mode parity:
  - player active skill target and execute for Slash, Toxic_Fume, one direct-damage skill, one trap skill, and one stance skill;
  - AI active skill execution through the bridge;
  - verify CastManager is not called for player or AI active skills;
  - verify current trap/passive behavior before migrating the remaining blocker.

## Closure Verdict

PRD050 is not fully closed.

Code-side active skill execution is migrated to Battle Action result/apply, and CastManager is no longer an active skill runtime authority from current code call sites. Full PRD050 closure remains blocked by passive/trap/automatic legacy mutation paths and by required manual Unity compile / Play Mode validation.
