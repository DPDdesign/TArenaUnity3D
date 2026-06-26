# TArenaUnity3D Combat Presentation Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when a task touches combat animations, hit/death reactions, combat
SFX, background music, projectile presentation, skill VFX/SFX, unit model
presentation, or persistent board-state visuals such as trap surfaces.

This active map should describe current code and presentation boundaries, not
history. Use `_codex/Context/maps/combat-skill-ai-history-risks-map.md` only
for regression debugging, bug archaeology, or explicit historical questions.

## Sources

Read the smallest relevant subset:

- `_codex/Documentation/User_Setup_Guide.md`
- `_codex/Documentation/CurrentSkills.md` when skill presentation is involved
- `_codex/Context/09_CurrentSkills.md` when skill ids are involved
- `_codex/Context/10_Skill_Design_Rules.md` when skill presentation catalog
  boundaries are involved
- `_codex/agents/docs/codebase/skills-effects-code-map.md`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterView.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatSfxManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterSfxSet.cs`

## Current Responsibility Boundary

Combat presentation owns what the player sees and hears during combat:

- unit animation state presentation,
- hit/death feedback,
- combat SFX playback,
- background music playback,
- projectile/VFX presentation,
- persistent visual markers for board-state effects.

Combat presentation should not own skill legality, damage calculation, target
validation, turn lifecycle, AI scoring, persistence, or unit skill ownership.

## Current Operating Rules

- Each unit model can define `attack`, `hit`, and `death` clips on the same
  GameObject as its Animator through `TosterSfxSet`.
- The scene owns one manually placed `CombatSfxManager` with one `AudioSource`.
- Combat SFX playback is global/2D and uses `AudioSource.PlayOneShot`, so
  multiple combat SFX can overlap.
- The scene can own a separate `BackgroundMusicManager` with its own
  `AudioSource`. It autoplays a configured looping music clip and should not
  share the combat SFX `AudioSource`.
- Skill assignment is unit-catalog-driven; presentation data must not decide
  which skills a unit owns.
- Skill presentation should use the skill string/id as the join key.
- Persistent board-state visuals such as trap surface models should route
  through presentation data, while gameplay state remains in gameplay classes
  such as `HexClass` and `Traps`.
