# [TARENA] 036.1 Coding Completion - Start Run Slot Availability

- Date: 2026-06-16
- Task: `_codex/tasks/036_1_StartRunSlotAvailability_Coding.md`
- Status: implemented-qa-review-pending

## Scope

Implemented calculated Start Run visual slot availability for generated
starting army offers.

## Completed

- Added request-count-aware starting army source support so the PRD035 generator
  can produce one generated offer per requested Start Run UI slot.
- Extended Start Run option DTOs with visual slot index, calculated locked
  state, and lock reason.
- Added slot availability rules:
  - Slot 1 unlocked,
  - Slot 2 locked by `Win A Run` until a won run summary exists,
  - Slot 3 locked by `Reach Level 5` until account level 5,
  - Slot 4 locked by `Unavailable in DEMO`,
  - Slot 5+ locked by `Coming soon`.
- Added a DB-backed Start Run availability source that reads account XP and
  active won run summaries through the Offline DB boundary.
- Wired production Offline Mode composition to use the DB-backed availability
  source.
- Updated Start Run screen controller to request generated offers from
  Inspector-wired army-card count and to pass that count into Begin Run.
- Updated Start Run army-card binding to expose a serialized locked overlay
  GameObject and TMP reason text, set locked state, and disable locked button
  interaction.
- Kept old BuildScreen/BeginRun overloads for existing tests and adjacent
  screens.
- Kept lock state calculated only; no `IsLocked` persistence or schema change
  was added.
- Did not add generated offer analytics persistence.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/OfflineStartRunAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/OfflineStartRunSlotAvailabilitySource.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/CampaingSelectionScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunArmyCardView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/DeterministicRunGenerationCatalog.cs`

## Validation Performed

- Static inspection of changed files and call sites.
- Checked existing Start Run and PRD35 test call sites for API compatibility.
- Did not run Unity compilation or EditMode tests automatically.

## Not Run

- Unity compilation.
- Unity EditMode tests.
- Play Mode validation.

## Notes

- Two new Inspector fields must be wired manually on each Start Run army card:
  locked overlay GameObject and locked reason TMP text.
- This task is UI-facing but no prefab or Unity asset files were edited.
- Focused EditMode tests should be added after QA review per implement-task
  workflow.
