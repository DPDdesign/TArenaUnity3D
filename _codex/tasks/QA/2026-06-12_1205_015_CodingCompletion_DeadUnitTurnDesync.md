# [TARENA] Coding Completion - PRD 015 Dead Unit Turn Desync

## Task

- `_codex/tasks/015_PRD_DeadUnitTurnDesync_Coding.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `_codex/tasks/015_PRD_DeadUnitTurnDesync_Coding.md`

## What Changed

- Added `CanReceiveTurn(TosterHexUnit t)` in `TeamClass`.
- Updated current turn selection to reject null, dead, and zero-amount units.
- Updated queue/simulator selection to use the same eligibility rule.
- Updated `NewTurn()` to re-check eligibility after `CheckSpells()` so DOT
  death cannot be followed by `Moved = false`.
- Updated team-dead evaluation to use the same eligibility rule.

## Checks Performed

- Inspected the changed `TeamClass.cs` region.
- Searched `TurnManager` call sites for `AskForUnit()` and
  `AskForUnitSimulator()`.
- Searched `MouseControler` call sites where active units are acquired through
  `TurnManager.AskWhosTurn()`.
- Confirmed no Unity assets, prefabs, scenes, controllers, `.asmdef`, or
  `.asmref` files were changed.

## Tests

- No Unity or `dotnet` build was run, per project rules.
- No automated gameplay test was added because the project currently has no
  local TArena gameplay test assembly; only vendor plugin tests were found.

## Manual Unity Validation

- Reproduce FireElemental fire trail / fire trap DOT killing HeavyHitter.
- Confirm HeavyHitter is not selected for a turn after death.
- Confirm queue preview excludes the dead zero-amount HeavyHitter.
- Confirm alive units still receive turns normally.
- Confirm wait, defense, and movement still advance to the next valid unit.

## Residual Risk

- `MouseControler` sets victory UI but does not return immediately before
  requesting the next selected unit. This is a separate game-end-control-flow
  issue and was not changed in this task.
- Broader backend/presentation sequencing for VFX/SFX remains out of scope.
