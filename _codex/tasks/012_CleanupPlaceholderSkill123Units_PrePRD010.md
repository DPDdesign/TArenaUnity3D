# [TARENA] Cleanup Task: Remove Placeholder Skill1/Skill2/Skill3 Units Before PRD 010

- Status: implemented
- Type: cleanup
- Area: Skills, XML Unit Assignment, Legacy CastManager Cleanup
- Label: quick-task
- Blocks: `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`

## Problem Statement

PRD 010 should migrate only real remaining active skill presentation paths.
`Skill1`, `Skill2`, and `Skill3` are placeholder legacy skills and should not be
migrated.

The placeholder units `TosterHEAL`, `TosterDPS`, and `TosterTANK` exist as
carriers for those placeholder skills. They should be removed before PRD 010 so
the migration scope is limited to real units and real skill ids.

## Scope

- Remove `TosterHEAL`, `TosterDPS`, and `TosterTANK` unit entries from
  `TArenaUnity3D/Assets/Resources/Data/Units.xml`.
- Remove matching stale entries from `Units — kopia.xml` if present.
- Remove `Skill1`, `Skill1M`, `Skill2`, `Skill2M`, `Skill3`, and `Skill3M`
  from `CastManager`.
- Remove helper fields used only by these placeholder skills.
- Update PRD 010 so `Skill1`, `Skill2`, and `Skill3` are not migration or test
  targets.

## Non-Goals

- Do not migrate these skills to the new presentation routine model.
- Do not change gameplay values for remaining units or remaining skills.
- Do not edit scenes, prefabs, materials, Animator Controllers, generated Unity
  files, `.asmdef`, `.asmref`, or asset metadata.
- Do not perform broader Photon/PUN/RPC cleanup.
- Do not delete icons, sprites, models, or other binary assets unless a later
  task explicitly allows asset cleanup.

## Acceptance Criteria

- `Units.xml` no longer contains unit entries named `TosterHEAL`, `TosterDPS`,
  or `TosterTANK`.
- No unit in `Units.xml` references exact placeholder skill ids `Skill1`,
  `Skill2`, or `Skill3`.
- `CastManager` no longer exposes callable `Skill1`, `Skill1M`, `Skill2`,
  `Skill2M`, `Skill3`, or `Skill3M` methods.
- PRD 010 no longer asks the implementation agent to migrate or manually test
  `Skill1`, `Skill2`, or `Skill3`.
- Existing remaining unit skill assignments are unchanged.
- No Unity asset, scene, prefab, controller, `.asmdef`, `.asmref`, or generated
  file is edited.

## Manual Test Plan

- In Unity, confirm unit data loads without `TosterHEAL`, `TosterDPS`, and
  `TosterTANK`.
- Confirm skill buttons still populate for remaining units.
- Confirm selecting remaining units does not try to invoke removed
  `Skill1`, `Skill2`, or `Skill3` ids.
- Confirm there are no compile errors from removed `CastManager` methods or
  helpers.

## Implementation - 2026-06-11

### What Changed

- `Units.xml`: removed `TosterHEAL`, `TosterDPS`, and `TosterTANK`, including
  exact placeholder skill assignments `Skill1`, `Skill2`, and `Skill3`.
- `Units — kopia.xml`: removed the same stale placeholder unit entries.
- `Units_Mapping_Template.xml`: removed placeholder carrier unit entries and
  exact placeholder skill ids, leaving the XML root intact.
- `Skill1.cs`: deleted the obsolete legacy prototype source file.
- `CastManager`: removed callable `Skill1`, `Skill1M`, `Skill2`, `Skill2M`,
  `Skill3`, and `Skill3M` methods.
- `CastManager`: removed `kochamizabelke`, a private counter used only by
  `Skill1`. No tuning fields or Inspector fields changed.
- `TosterHexUnit`: removed a stale commented `new Skill1()` prototype and
  updated a documentation-only XML layout comment from `TosterDPS` to neutral
  `UnitName`.
- PRD 010: removed `Skill1`, `Skill2`, and `Skill3` from migration/test scope
  and listed them with `TosterHEAL`, `TosterDPS`, and `TosterTANK` as required
  pre-cleanup exclusions.

### Automatic Test

- No EditMode tests were added. This task removes placeholder XML data and
  reflection-callable methods rather than adding isolated runtime logic.
- Automatic checks performed: `Units.xml`, `Units — kopia.xml`, and
  `Units_Mapping_Template.xml` parsed successfully; exact searches across
  `TArenaUnity3D/Assets` found no `<Name>TosterHEAL</Name>`,
  `<Name>TosterDPS</Name>`, `<Name>TosterTANK</Name>`, `>Skill1<`, `>Skill2<`,
  or `>Skill3<`; targeted `CastManager` searches found no exact removed method
  names; full `Assets` searches found no `public class Skill1`, `new Skill1`,
  or `kochamizabelke`.
- Unity tests are run manually by the user in Unity Test Runner; no new test
  entries are expected for this cleanup.

### Unity Test

#### Unity Setup

- Open the project in Unity and let scripts compile.
- No new scene objects, components, Inspector assignments, prefabs, or assets
  need to be added.

#### Play Mode Test

- Start the normal unit selection or battle setup flow and confirm unit data
  loads without `TosterHEAL`, `TosterDPS`, and `TosterTANK`.
- Select several remaining units and confirm skill buttons still populate.
- Confirm no remaining unit attempts to invoke `Skill1`, `Skill2`, or
  `Skill3`.
- Confirm the Unity Console has no compile errors from removed `CastManager`
  methods or helpers.

### QA Verdict

- Final QA verdict: Pass.
- Final QA report:
  `_codex/tasks/QA/2026-06-11_012_QA_ArchitectureReview_Final.md`.
- No actionable findings remain.
- Non-blocking observation: identifiers such as `Tank_Skill1` and
  `Unit_Name_Skill1` remain valid unrelated strings and are not exact
  placeholder skill ids.

### Notes

- No scenes, prefabs, materials, Animator Controllers, generated Unity files,
  `.asmdef`, `.asmref`, metadata, sprites, icons, models, or binary assets were
  edited.
- `Skill1.cs.meta` was intentionally left untouched because metadata edits were
  outside the allowed scope.
- No broader Photon/PUN/RPC cleanup was performed.
- Remaining skill gameplay values and remaining unit skill assignments were not
  intentionally changed.

### Next Steps

- Run Unity compile in the already open Editor.
- In Play Mode, verify remaining unit data and skill buttons still load.
- PRD 010 can now proceed without `Skill1/2/3` placeholder migration noise.
