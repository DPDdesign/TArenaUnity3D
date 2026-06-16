# 028_TURN-001 Coding Agent Completion

## Task

Implement current battle action rules for WAIT, DEFENCE, MOVE, NI skills, AM
skills, and repeated skill usage.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `_codex/tasks/archive/028_TURN-001_TurnActionSkillRules_Coding.md`

## Systems Touched

- Battle action lifecycle entry points in `MouseControler`.
- Per-unit turn action state in `TosterHexUnit`.
- New-turn state reset in `TeamClass`.
- Skill button interactability in `UICanvas`.

## Behavior Or Setup Summary

- Added per-turn skill id tracking on `TosterHexUnit` so the game can block the
  same non-toggle skill from being used twice while still allowing different
  legal skills in the same turn window.
- `Melee_Stance*` and `Range_Stance*` are treated as repeatable toggle skills.
- Movement no longer ends the unit's turn only because any skill was used
  earlier. It now ends after movement when there is no legal skill available
  through the same validation used by real skill execution.
- `CanStartSkill` now blocks passives, waited units, cooldowns, repeated
  non-toggle skill ids, skills without AM after movement, and AM-only skills
  before movement.
- Skill UI now calls `MouseControler.CanUseSkillSlot` so displayed button state
  follows the same validation as actual skill execution.
- Targeted skill execution now uses the captured selected skill id instead of
  rereading a possibly changed slot name.

## Unity Checks

Manual Play Mode checks needed:

- WAIT as first action moves the unit to the end of queue and a second WAIT is
  blocked.
- DEFENCE as first action applies defence and ends that unit's turn.
- MOVE first ends the turn when no AM skill is legal.
- MOVE first keeps the unit active when a legal AM skill is available.
- NI skill first allows another different NI skill and then movement.
- NI skill, movement, then legal AM skill is allowed.
- Reusing the same non-toggle skill in the same turn window is blocked.
- Melee/Range stance toggles remain usable as repeatable toggles.
- Skill buttons grey/interactable state matches the above cases.

## Intentionally Not Included

- No XML data edits.
- No prefab, scene, material, controller, or Unity asset edits.
- No command-line Unity, dotnet, or build execution.
- No broader CastManager refactor.
