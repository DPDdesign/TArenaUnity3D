# [TARENA] Coding Completion: PRD 018 Battle Action Lifecycle Full Migration

- Task: `_codex/tasks/018_PRD_BattleActionLifecycleFullMigration.md`
- Agent: Coding Agent
- Date: 2026-06-13

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TurnManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`

## What Changed

### `BattleActionLifecycle`

- Added a runtime-created battle action lifecycle component.
- Added action categories for movement, move-and-attack, ranged attack, skill, wait, defense, and automatic actions.
- Added one busy gate for action commit, action body execution, blocking presentation wait, completion cleanup, and timeout release.
- Added `IsActionBlocking`, which also treats tracked skill presentation as a turn-blocking condition.

### `MouseControler`

- Ensures a lifecycle instance exists at battle input startup.
- Blocks turn detection, waiting-for-turn handling, movement selection, and skill casting while lifecycle/presentation is blocking.
- Added lifecycle wrapper methods for:
  - normal movement,
  - move-and-attack,
  - basic ranged attack,
  - wait,
  - defense,
  - skill completion,
  - non-consuming legacy movement RPC.
- Moved live `Moved = true`, `Waited = true`, defense stance application, and skill action consumption into lifecycle commit callbacks.
- Converted live player/RPC entrypoints for movement, move-and-attack, ranged attack, wait, defense, and skill completion to use lifecycle wrappers.
- Kept `CancelUpdateFunc()` as input/UI mode cleanup only, called after lifecycle completion.

### `TurnManager`

- Added a guard so `AskWhosTurn()` returns no actionable unit while lifecycle or tracked presentation is blocking.
- The existing new-round recursive turn selection re-enters this guard after `NewTurn()`/`DoTurn()` can trigger passive presentation.

### `HexMap`

- Fixed `DoUnitMoves()` so it waits for the final visual movement step even when `TosterHexUnit.DoMove()` returns `false` after the last logical move.

### `SkillPresentationManager`

- Added tracked blocking presentation coroutines.
- Added `HasBlockingPresentation` and `WaitForBlockingPresentation(...)`.
- Converted public sequenced presentation entrypoints to tracked blocking coroutines.
- Changed projectile, impact, and reveal sequences so target reaction/death animations are awaited instead of fire-and-forget when a manager/catalog entry exists.
- Multi-projectile child routines are tracked so the lifecycle remains blocked until all child projectile/reveal routines finish.

### `CastManager`

- Added non-serialized `ActionInputBlockedByCommittedSkill`.
- Committed async skill coroutines such as `Rush`, `Slash`, `Toxic_Fume`, and `Heavy_Fists` block further skill input until their existing `SetFalse()` cleanup runs.
- `SetFalse()` remains skill-mode cleanup and no longer acts as the next-turn release point; next-turn release is owned by lifecycle.

### `MostStupidAIEver`

- AI move and move-and-attack selections now execute through the same `MouseControler` lifecycle wrappers as player/RPC actions.

## Static Verification

- Searched for remaining live `StartCoroutine(DoMoves...)`, `StartCoroutine(DoMoveAndAttack...)`, and `MC.StartCoroutine(...)` action bypasses in `MouseControler` and `MostStupidAIEver`.
- Result: remaining matches are comments only.
- Searched for lifecycle gates and tracked presentation calls.
- Result: action entrypoints route through `TryStart...` wrappers; presentation public entrypoints route through `StartBlockingCoroutine(...)`.
- Checked changed-file brace counts as a text sanity check. Counts match on changed files except `MostStupidAIEver.cs`, where the mismatch comes from pre-existing braces inside a block comment.

## Automatic Tests

- No EditMode tests added in this pass.
- Reason: the changed behavior is coroutine-driven Unity runtime sequencing across `MonoBehaviour`, `TosterView`, scene-owned managers, and legacy input/RPC adapters. There is no existing isolated test seam that can validate it without scene/prefab setup or new `.asmdef` infrastructure, which this PRD does not authorize.

## Unity Validation Needed

- Unity compile in the Editor.
- Play Mode validation against the PRD checklist, especially:
  - normal move waits for final visual hex,
  - move-and-attack waits for movement and hit/death reveal,
  - basic ranged attack waits for projectile, impact, and reaction,
  - `Fire_Ball`, `Rush`, and `Heavy_Fists` wait for their blocking presentation,
  - wait and defense consume through lifecycle,
  - AI actions do not overlap themselves,
  - missing presentation/catalog cases release safely.

## Known Risks / Follow-Up Watchpoints

- End-of-round passive/deferred effects still use the existing `TeamClass.NewTurn()` / `TosterHexUnit.CheckSpells()` model. Presentation triggered by that path is now tracked and blocks turn exposure, but the deeper unit-package queue model described in PRD 018 should be watched closely in QA.
- Some legacy model-state assignments remain outside `MouseControler`, such as spawned unit `Moved = true` in `Stone_Throw` and death cleanup in `TosterHexUnit.Died(...)`; these are not next-turn release calls.
- `SetFalse()` still exists in `CastManager` as skill-mode cleanup and the signal that a skill action has finished its legacy body. It no longer directly releases the next actionable unit.
