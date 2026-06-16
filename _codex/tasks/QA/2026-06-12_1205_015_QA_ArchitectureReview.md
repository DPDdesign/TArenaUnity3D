# [TARENA] QA Architecture Review - PRD 015

## Reviewed Protocol

- `_codex/tasks/QA/2026-06-12_1205_015_CodingCompletion_DeadUnitTurnDesync.md`

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `_codex/tasks/015_PRD_DeadUnitTurnDesync_Coding.md`

## Verdict

Pass with residual risk noted.

## Findings

No actionable architecture findings.

## Architecture Notes

- The fix stays in the owning selection module, `TeamClass`, instead of adding
  scattered checks in `MouseControler`.
- Current turn selection, simulator/queue selection, and team-dead evaluation
  now share the same local eligibility rule.
- Re-checking eligibility after `CheckSpells()` directly addresses the
  deferred DOT/trap death sequence that could previously reset `Moved`.
- The implementation does not rename public or serialized fields.
- The implementation does not change gameplay float values or Unity assets.

## Residual Risk

- `MouseControler` currently sets victory UI and then continues toward
  `TM.AskWhosTurn()` without an early return. Because `IsMyTeamDEAD()` now uses
  the stricter eligibility rule, game-end behavior should be validated in Play
  Mode. This is separate from the reported live-combat dead-unit turn desync.
- Broader SFX/VFX/backend sequencing remains a documented out-of-scope risk.

## QA Recommendation

- Accept this task after Unity compile and Play Mode reproduction pass.
- Open a separate small task if victory UI still allows active-unit selection
  after a team is dead.
