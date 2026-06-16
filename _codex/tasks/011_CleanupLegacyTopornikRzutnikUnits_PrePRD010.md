# [TARENA] Cleanup Task: Remove Legacy Topornik/Rzutnik Units Before PRD 010

- Status: implemented
- Type: cleanup
- Area: Skills, XML Unit Assignment, Legacy CastManager Cleanup
- Label: quick-task
- Blocks: `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`

## Problem Statement

PRD 010 should migrate the active skill presentation model for skills that
remain in the game. The legacy `Lizard` and `axe1` unit entries are cleanup
targets and should not be migrated.

`Units.xml` still assigns `Topornik_Skill*` and `Rzutnik_Skill*` skills through
those units. Leaving them in place creates noise in PRD 010 and risks spending
implementation work on skills that should be removed.

## Scope

- Remove the `Lizard` unit entry from `TArenaUnity3D/Assets/Resources/Data/Units.xml`.
- Remove the `axe1` unit entry from `TArenaUnity3D/Assets/Resources/Data/Units.xml`.
- Remove legacy `Topornik_Skill1`, `Topornik_Skill2`, `Topornik_Skill3`,
  `Rzutnik_Skill1`, `Rzutnik_Skill2`, `Rzutnik_Skill3`, and `Rzutnik_Skill4`
  methods and mode methods from `CastManager`.
- Remove helper fields or helper methods used only by those legacy Topornik or
  Rzutnik skills.
- Update PRD 010 migration scope so Topornik/Rzutnik skills are not listed as
  migration targets.

## Non-Goals

- Do not migrate these skills to the new presentation routine model.
- Do not change gameplay values for remaining units or remaining skills.
- Do not edit scenes, prefabs, materials, Animator Controllers, generated Unity
  files, `.asmdef`, `.asmref`, or asset metadata.
- Do not perform broader Photon/PUN/RPC cleanup.
- Do not delete icons, sprites, models, or other binary assets unless a later
  task explicitly allows asset cleanup.

## Acceptance Criteria

- `Units.xml` no longer contains unit entries named `Lizard` or `axe1`.
- No unit in `Units.xml` references `Topornik_Skill*` or `Rzutnik_Skill*`.
- `CastManager` no longer exposes callable `Topornik_Skill*` or
  `Rzutnik_Skill*` execution/mode methods.
- PRD 010 no longer asks the implementation agent to migrate Topornik/Rzutnik
  skills.
- Existing remaining unit skill assignments are unchanged.
- No Unity asset, scene, prefab, controller, `.asmdef`, `.asmref`, or generated
  file is edited.

## Manual Test Plan

- In Unity, confirm unit data loads without `Lizard` and `axe1`.
- Confirm skill buttons still populate for remaining units.
- Confirm selecting remaining units does not try to invoke removed
  Topornik/Rzutnik skill ids.
- Confirm there are no compile errors from removed `CastManager` methods or
  helpers.

## Implementation - 2026-06-11

### What Changed

- `Units.xml`: removed the `axe1` and `Lizard` unit entries, including their
  `Rzutnik_Skill*` and `Topornik_Skill*` assignments.
- `Units — kopia.xml`: removed the same stale `axe1` and `Lizard` entries found
  during QA follow-up, so legacy skill ids no longer remain under `Assets`.
- `CastManager`: removed callable `Topornik_Skill1/2/3` and
  `Rzutnik_Skill1/2/3/4` methods plus their `M` mode methods and the unused
  `SelectMultipleEnemy` helper.
- `CastManager`: replaced legacy private `Rzutnik_Skill1_*` target state used
  by `Double_Throw` with private `doubleThrowTargets` and
  `doubleThrowTargetCounter`. `doubleThrowTargets` always holds two selected
  targets; `doubleThrowTargetCounter` should stay in range `0..2`, where `2`
  commits the existing two-throw behavior. These are internal state fields, not
  tuning fields.
- No Inspector fields changed.

### Automatic Test

- No EditMode test files were added. This task removes legacy XML data and
  reflection-callable methods rather than adding isolated runtime logic.
- Automatic checks performed: both unit XML files parsed successfully; exact
  searches across `TArenaUnity3D/Assets` found no `Topornik_Skill[0-9]`,
  `Rzutnik_Skill[0-9]`, `<Name>axe1</Name>`, or `<Name>Lizard</Name>`; targeted
  `CastManager` searches found no removed callable method/helper names.
- Unity tests are run manually by the user in Unity Test Runner; no new test
  entries are expected for this cleanup.

### Unity Test

#### Unity Setup

- Open the project in Unity and let scripts compile.
- No new scene objects, components, Inspector assignments, prefabs, or assets
  need to be added.

#### Play Mode Test

- Start the normal unit selection or battle setup flow and confirm unit data
  loads without `axe1` or `Lizard`.
- Select several remaining units and confirm skill buttons still populate.
- Confirm no remaining unit attempts to invoke `Topornik_Skill*` or
  `Rzutnik_Skill*`.
- Confirm the Unity Console has no compile errors from removed `CastManager`
  methods or helpers.

### QA Verdict

- Final QA verdict: Pass.
- Final QA report:
  `_codex/tasks/QA/2026-06-11_1933_011_QA_ArchitectureReview_Final.md`.
- Initial QA found stale legacy entries in `Units — kopia.xml`; follow-up fix
  removed them and the final QA pass had no actionable findings.
- Non-blocking observation: `SuperLizard`, `Range_Stance_Lizard`, and
  `Melee_Stance_Lizard` remain valid unrelated identifiers.

### Notes

- No scenes, prefabs, materials, Animator Controllers, generated Unity files,
  `.asmdef`, `.asmref`, metadata, sprites, icons, models, or binary assets were
  edited.
- No broader Photon/PUN/RPC cleanup was performed.
- Remaining skill gameplay values and remaining unit skill assignments were not
  intentionally changed.

### Next Steps

- Run Unity compile in the already open Editor.
- In Play Mode, verify remaining unit data and skill buttons still load.
- PRD 010 can now proceed without Topornik/Rzutnik migration noise.
