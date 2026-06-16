# [TARENA] 011 Cleanup Legacy Topornik/Rzutnik Units Follow-up Completion

## Task

`_codex/tasks/011_CleanupLegacyTopornikRzutnikUnits_PrePRD010.md`

## QA Finding Addressed

`_codex/tasks/QA/2026-06-11_0000_011_QA_ArchitectureReview.md`

## Files Changed

- `TArenaUnity3D/Assets/Resources/Data/Units — kopia.xml`

## Behavior Or Setup Summary

Removed the same legacy `axe1` and `Lizard` unit entries from the duplicate
`Units — kopia.xml` resource file so stale `Topornik_Skill*` and
`Rzutnik_Skill*` ids no longer remain under `TArenaUnity3D/Assets`.

No runtime code was changed in this follow-up.

## Verification

- Searched all `TArenaUnity3D/Assets` for `Topornik_Skill[0-9]` and
  `Rzutnik_Skill[0-9]`; no matches remained.
- Searched all `TArenaUnity3D/Assets` for exact unit names
  `<Name>axe1</Name>` and `<Name>Lizard</Name>`; no matches remained.
- Parsed both `Units.xml` and `Units — kopia.xml` with PowerShell XML parsing
  successfully.
- Searched `CastManager` for callable legacy methods and old helper names; no
  matches remained.

## Unity Checks

Not run. Project rules say the user compiles and tests inside Unity unless a
specific Unity test command is allowed.

