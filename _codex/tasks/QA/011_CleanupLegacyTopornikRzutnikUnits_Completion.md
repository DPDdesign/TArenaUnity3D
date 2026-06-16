# [TARENA] 011 Cleanup Legacy Topornik/Rzutnik Units Completion

## Task

`_codex/tasks/011_CleanupLegacyTopornikRzutnikUnits_PrePRD010.md`

## Files Changed

- `TArenaUnity3D/Assets/Resources/Data/Units.xml`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`

## Systems Touched

- XML unit ownership data loaded from `Resources/data/Units`.
- `CastManager` reflection-callable skill methods.
- `Double_Throw` internal target selection state names.

## Behavior Or Setup Summary

Removed the legacy `axe1` unit entry from `Units.xml`, including its
`Rzutnik_Skill1`, `Rzutnik_Skill2`, and `Rzutnik_Skill3` assignments.

Removed the legacy `Lizard` unit entry from `Units.xml`, including its
`Topornik_Skill1` and `Topornik_Skill2` assignments.

Removed callable `Topornik_Skill1`, `Topornik_Skill1M`, `Topornik_Skill2`,
`Topornik_Skill2M`, `Topornik_Skill3`, and `Topornik_Skill3M` methods from
`CastManager`.

Removed callable `Rzutnik_Skill1`, `Rzutnik_Skill1M`, `Rzutnik_Skill2`,
`Rzutnik_Skill2M`, `Rzutnik_Skill3`, `Rzutnik_Skill3M`, `Rzutnik_Skill4`, and
`Rzutnik_Skill4M` methods from `CastManager`.

Removed the unused `SelectMultipleEnemy` helper.

Kept `Double_Throw` behavior intact by moving the two-target selection state
into `doubleThrowTargets` and `doubleThrowTargetCounter`, replacing the old
legacy `Rzutnik_Skill1_*` field names that were shared by `Double_Throw`.

Removed an empty legacy `Rzutnik` template region at the bottom of
`CastManager` so text searches no longer report stale placeholder references.

## Verification

- Parsed `Units.xml` with PowerShell XML parsing successfully.
- Searched `Units.xml` and `CastManager` for `Topornik_Skill`,
  `Rzutnik_Skill`, `axe1`, `Lizard`, `Rzutnik`, `Topornik`,
  `SelectMultipleEnemy`, and `Array`; no runtime references remained.
- Confirmed remaining `Units.xml` skill entries still exist for the remaining
  units.

## Unity Checks

Not run. Project rules say the user compiles and tests inside Unity unless a
specific Unity test command is allowed.

Recommended manual checks in Unity:

- Confirm unit data loads without `Lizard` and `axe1`.
- Confirm skill buttons still populate for remaining units.
- Confirm selecting remaining units does not try to invoke removed
  Topornik/Rzutnik skill ids.
- Confirm there are no compile errors from removed `CastManager` methods or
  helpers.

## Intentionally Not Included

- No scenes, prefabs, materials, Animator Controllers, generated Unity files,
  `.asmdef`, `.asmref`, or asset metadata were edited.
- No binary icons, sprites, models, or other assets were deleted.
- No broader Photon/PUN/RPC cleanup was performed.
- No remaining skill gameplay values or XML skill assignments were changed.

