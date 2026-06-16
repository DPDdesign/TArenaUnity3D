# [TARENA] Follow-up Completion - PRD019 Run Battle Encounter Payload

- Date: 2026-06-15 01:21
- Task: `_codex/tasks/022_PRD019_RunBattle.md`
- Follow-up for: `_codex/tasks/QA/2026-06-15_0116_022_QA_ArchitectureReview.md`
- Status: ready-for-final-qa

## Scope

Applied focused fixes for the initial QA findings.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/RunBattleService.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunBattleServiceTests.cs`
- `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`

## Fixes

- `RunBattleService.PrepareBattle(...)` now rejects missing `EncounterId` with
  `RunBattleError.MissingEncounter`.
- `RunBattleServiceTests` now use the same explicit encounter ids as
  `DefaultRunBattleEncounterCatalog`, including `enc-iron-border-clash` and
  `enc-final-proof`.
- Added `PrepareBattle_RejectsMissingEncounterId`.
- Task 22 mockup now shows explicit encounter ids aligned with the C# default
  catalog.
- Task 22 mockup uses the Iron Line V1 encounter catalog for this screen so the
  displayed payload matches the implemented local catalog.

## Validation

- Ran `node --check _codex\Gen_Im\RETSOT ONLINE\src\game\ui.js`: passed.
- Grep confirmed no remaining `encounter:n1` or stale `enc-border-clash`
  placeholder in the Task 22 mockup/test files.
- Unity EditMode tests were updated but not run automatically, per project
  policy.
