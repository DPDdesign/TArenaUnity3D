# 019 Coding Agent Completion

## Task

`_codex/tasks/019_PRD_TrapSpawnModelPresentation.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/Traps.cs`
- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`

## Systems Touched

- Skill presentation catalog data model.
- Scene-level skill presentation manager.
- Trap persistent presentation lifecycle.
- Skill presentation context documentation.

## Behavior Or Setup Summary

- `SkillPresentationEntry` now exposes a `spawnModel` field for persistent
  board-state objects such as trap surface models.
- `SkillPresentationManager.SpawnPersistentModel(skillId, targetHex)` looks up
  the skill entry, instantiates `spawnModel` at the target hex, applies the
  existing spawn scale preset, parents the instance under the hex object, and
  returns the instance to the caller.
- `Traps.Traps` now stores the spawned model instance, shows it when the trap is
  revealed, and destroys it when the trap is removed.
- If no `spawnModel` is configured, `Traps` falls back to the existing legacy
  child-object visibility path for `Spike_Trap` and `Fire_Trap`.
- `Rope_Trap` now has a catalog-driven persistent model path once its
  `Spawn Model` field is assigned in Unity.

## Unity Checks

Manual Unity validation is required:

- Assign `SkillPresentationCatalog` on the scene `SkillPresentationManager`.
- In the catalog entry for `Rope_Trap`, assign a rope trap prefab/model to
  `Spawn Model`.
- Optionally assign `Spawn Model` for `Spike_Trap` and `Fire_Trap` to migrate
  them off legacy hex child objects.
- Enter Play Mode, select a Trapper, cast `Rope_Trap`, and confirm the caster
  sees the persistent model on the target hex after the skill resolves.
- Step an enemy onto the rope trap and confirm the trap removes its persistent
  model when consumed.
- Confirm existing `Spike_Trap` and `Fire_Trap` visuals still work when
  `Spawn Model` is not assigned.

## Intentionally Not Included

- No gameplay balance, cooldown, damage, duration, targeting, or movement rule
  changes.
- No prefab, scene, material, or catalog asset edits.
- No full multiplayer owner-only visibility filter. This task adds the
  persistent model path needed for owner-visible traps; network/local viewpoint
  filtering remains a later explicit step if multiplayer is restored.
- No automated EditMode tests. The changed behavior depends on scene
  `SkillPresentationManager`, catalog object references, hex GameObjects, and
  runtime prefab assignment.
