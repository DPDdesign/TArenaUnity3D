# [TARENA] 011 QA Architecture Review

## Task

`_codex/tasks/011_CleanupLegacyTopornikRzutnikUnits_PrePRD010.md`

## Protocol Reviewed

`_codex/tasks/QA/011_CleanupLegacyTopornikRzutnikUnits_Completion.md`

## Verdict

Needs focused follow-up.

## Findings

### 1. Stale duplicate unit XML still contains removed legacy units

`TArenaUnity3D/Assets/Resources/Data/Units.xml` and `CastManager` satisfy the
task acceptance, but a nearby duplicate resource file still contains the exact
legacy unit entries and skill ids:

- `TArenaUnity3D/Assets/Resources/Data/Units — kopia.xml`
- `axe1`
- `Lizard`
- `Rzutnik_Skill1`, `Rzutnik_Skill2`, `Rzutnik_Skill3`
- `Topornik_Skill1`, `Topornik_Skill2`

This is not the canonical file loaded by the known `Resources.Load("data/Units")`
path, but leaving stale legacy unit data under `Assets/Resources/Data` weakens
the cleanup and can mislead future searches or tools.

Recommended fix: remove the same `axe1` and `Lizard` entries from
`Units — kopia.xml`, then rerun the legacy-id searches across
`TArenaUnity3D/Assets`.

## Non-Blocking Observations

- `Double_Throw` retained behavior while its internal two-target state was
  renamed from legacy `Rzutnik_Skill1_*` names to `doubleThrow*` names.
- `SuperLizard`, `Range_Stance_Lizard`, and `Melee_Stance_Lizard` remain valid
  unrelated identifiers and are not part of the cleanup.

## Scope Check

- No scenes, prefabs, generated Unity files, `.asmdef`, `.asmref`, or asset
  metadata were edited in the reviewed implementation.
- No broader Photon/PUN/RPC cleanup was performed.

