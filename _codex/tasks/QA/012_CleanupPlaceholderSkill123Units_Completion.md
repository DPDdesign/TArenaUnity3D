# [TARENA] 012 Cleanup Placeholder Skill1/Skill2/Skill3 Units Completion

## Task

`_codex/tasks/012_CleanupPlaceholderSkill123Units_PrePRD010.md`

## Files Changed

- `TArenaUnity3D/Assets/Resources/Data/Units.xml`
- `TArenaUnity3D/Assets/Resources/Data/Units — kopia.xml`
- `TArenaUnity3D/Assets/Resources/Data/Units_Mapping_Template.xml`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/Skill1.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`

## Systems Touched

- XML unit ownership data loaded from `Resources/data/Units`.
- Placeholder unit XML/template data.
- `CastManager` reflection-callable skill methods.
- PRD 010 migration scope.

## Behavior Or Setup Summary

Removed placeholder unit entries `TosterHEAL`, `TosterDPS`, and `TosterTANK`
from the canonical `Units.xml`.

Removed the same placeholder entries from the stale duplicate
`Units — kopia.xml`.

Removed placeholder entries from `Units_Mapping_Template.xml`, leaving the XML
root intact.

Deleted the obsolete source file `Skill1.cs`, which only contained a legacy
`Skill1 : SkillsDefault` prototype.

Removed callable `Skill1`, `Skill1M`, `Skill2`, `Skill2M`, `Skill3`, and
`Skill3M` methods from `CastManager`.

Removed the `kochamizabelke` field, which was only used by `Skill1`.

Removed a stale commented `new Skill1()` prototype from `TosterHexUnit` and
updated a documentation-only XML layout comment from the old placeholder
`TosterDPS` name to neutral `UnitName`.

Updated PRD 010 so `Skill1`, `Skill2`, and `Skill3` are not migration or manual
test targets and are listed with their removed carrier units as pre-PRD cleanup.

## Verification

- Parsed `Units.xml`, `Units — kopia.xml`, and `Units_Mapping_Template.xml`
  successfully.
- Searched all `TArenaUnity3D/Assets` for exact placeholder unit entries and
  exact placeholder skill ids; no `<Name>TosterHEAL</Name>`,
  `<Name>TosterDPS</Name>`, `<Name>TosterTANK</Name>`, `>Skill1<`,
  `>Skill2<`, or `>Skill3<` matches remained.
- Confirmed `CastManager` no longer exposes exact callable `Skill1`,
  `Skill1M`, `Skill2`, `Skill2M`, `Skill3`, or `Skill3M` methods.
- Confirmed no `public class Skill1`, `new Skill1`, or `kochamizabelke`
  references remain under `TArenaUnity3D/Assets`.
- Confirmed `CastManager` now flows from `getHexNextToMouse(...)` directly into
  `TeleportOT`.

## Unity Checks

Not run. Project rules say the user compiles and tests inside Unity unless a
specific Unity test command is allowed.

Recommended manual checks in Unity:

- Confirm unit data loads without `TosterHEAL`, `TosterDPS`, and `TosterTANK`.
- Confirm skill buttons still populate for remaining units.
- Confirm selecting remaining units does not try to invoke removed placeholder
  skill ids.
- Confirm there are no compile errors from removed `CastManager` methods or
  helpers.

## Intentionally Not Included

- No scenes, prefabs, materials, Animator Controllers, generated Unity files,
  `.asmdef`, `.asmref`, or asset metadata were edited.
- No binary icons, sprites, models, or other assets were deleted.
- `Skill1.cs.meta` was intentionally left untouched because this task did not
  permit Unity metadata edits.
- No broader Photon/PUN/RPC cleanup was performed.
- No remaining skill gameplay values or remaining unit skill assignments were
  intentionally changed.
