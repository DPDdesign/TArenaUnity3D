# [TARENA] QA Architecture Review - PRD019 Run Battle

- Date: 2026-06-15 01:16
- Reviewed protocol: `_codex/tasks/QA/2026-06-15_0109_022_CodingCompletion_RunBattle.md`
- Task: `_codex/tasks/022_PRD019_RunBattle.md`
- Verdict: Follow-up required

## Findings

### 1. Mockup encounter id does not match the C# payload contract

- Severity: Medium
- Files:
  - `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`
  - `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/DefaultRunBattleEncounterCatalog.cs`

The Task 22 mockup displays `encounter:<nodeId>` as the payload encounter id,
while the C# catalog and tests use explicit ids such as `enc-border-clash` and
`enc-final-proof`.

This weakens the requested mockup-vs-code alignment. The mockup should show the
same explicit encounter vocabulary as the implementation, not a placeholder
shape that cannot be submitted to `RunBattleService.PrepareBattle(...)`.

### 2. `PrepareBattle` can accept a missing encounter id through route-node fallback

- Severity: Medium
- File: `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/RunBattleService.cs`

The PRD asks for an explicit run-battle payload containing both route node id
and encounter id. Current service validation requires the route node but not the
encounter id, then asks the encounter source to find an encounter by route node
or encounter id.

Current PRD019 route node ids in the prototype are reused across routes
(`n1`, `n4`, `n5`), so route-node lookup alone is not stable enough for an
online-ready battle payload. The service should require an explicit
`EncounterId` and treat route-node-only lookup as insufficient in this slice.

## Non-blocking Observations

- The code correctly keeps `HexMap`, `TeamClass`, PlayerPrefs, local files, and
  current battle spawning behind an adapter record rather than editing legacy
  gameplay paths.
- The new tests are focused on domain behavior and do not require scenes or
  prefabs.
- The HTML mockup correctly avoids a new tactical HUD and stays at run battle
  context/transition level.

## Required Follow-up

- Make `RunBattleService.PrepareBattle(...)` reject missing `EncounterId`.
- Update Task 22 mockup to display encounter ids matching the C# catalog.
- Add or update a test proving missing encounter id is rejected.
