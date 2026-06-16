# [TARENA] 035 Follow-Up Coding Completion - Random Starting Armies And Route Maps

- Date: 2026-06-16
- Task: `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
- Prior QA: `_codex/tasks/QA/2026-06-16_1430_035_QA_ArchitectureReview.md`
- Status: implemented-manual-test-pending

## Scope

Applied the focused QA follow-up for the PRD035 generated starting army race
limit.

## Completed

- Changed generated army construction to select a one- or two-race unit pool
  before choosing tier composition.
- Limited all generated stack picks to the selected race pool.
- Changed tier-composition selection to run against the race-filtered pool, so
  the generator prefers only PRD-approved tier mixes that are satisfiable
  without exceeding the two-race limit.
- Kept fallback explicit when no valid two-race pool can satisfy the current
  unlock/unit set.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/DeterministicRunGenerationCatalog.cs`

## Validation Performed

- Static review of generator selection flow after the race-pool change.
- Confirmed there is no remaining `usedRaces` fallback path that can silently
  select a third race.

## Not Run

Unity compilation, EditMode execution, and Play Mode execution were not run in
this Codex pass, in line with the project rule that the user compiles/tests in
Unity unless explicitly allowed.

## Notes

- No Unity assets, prefabs, scenes, materials, controllers, `.asmdef`, or
  `.asmref` files were edited.
