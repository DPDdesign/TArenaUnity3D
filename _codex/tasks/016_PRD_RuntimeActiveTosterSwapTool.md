# 016 PRD Runtime Active Toster Swap Tool

Status: ready-for-agent
Labels: ready-for-agent, prd, runtime-tooling, battle-ui, testing
Project: TArenaUnity3D
Date: 2026-06-13

## Problem Statement

Testing unit behavior on the map is currently slow because the tester must set
up armies or saved builds before entering battle. When the active toster is
already selected in combat, there is no direct test-only UI action that replaces
that unit with another unit definition while keeping the same tactical context.

The user wants a simple battle UI tool: when a toster is active, click a
`Change Toster` action, choose any available toster type, and immediately test
that unit's skills, movement, stats, model, portrait, and turn behavior in the
same map position.

## Solution

Add a runtime-only debug/test tool for the battle scene that swaps the currently
active `TosterHexUnit` to another unit definition loaded from existing unit
data.

The swap must preserve tactical identity and turn context:

- keep the same active unit object reference,
- keep the same team,
- keep the same team list position,
- keep the same hex/map position,
- keep the same active selection,
- keep turn order tie-break ordering,
- keep stack amount unless explicitly changed by a future tool.

The swap must replace the unit definition data and presentation:

- load the new unit's base stats,
- load the new unit's skill ids,
- reset skill cooldown slots to match the new skills,
- refresh passive/autocast setup for the new skills,
- load the new unit prefab/model,
- refresh the amount text,
- refresh active unit UI, portrait, queue icon, and skill buttons through the
  existing UI update paths.

This is a developer/testing feature only. It must not persist to saved builds,
army selection, player progression, shop ownership, or multiplayer state.

## User Stories

1. As a designer, I want to replace the active toster during battle, so that I
   can test a unit without rebuilding armies.
2. As a designer, I want the replacement list to come from the existing unit
   data, so that every real unit type is available.
3. As a designer, I want the changed unit to remain the active unit, so that I
   can immediately move, attack, or cast skills with it.
4. As a designer, I want the changed unit to stay on the same hex, so that I can
   test the same map situation with different units.
5. As a designer, I want the changed unit to stay in the same team, so that ally
   and enemy relationships do not change.
6. As a designer, I want the changed unit to keep the same team list position,
   so that initiative tie-break order is not silently changed.
7. As a designer, I want the changed unit to keep the same stack amount, so that
   I can compare unit behavior at the same army size.
8. As a designer, I want the changed unit to load its own skills, so that skill
   buttons match the replacement unit.
9. As a designer, I want skill cooldown slots to be rebuilt for the new skill
   list, so that old cooldowns do not point at the wrong skills.
10. As a designer, I want passive and autocast effects to be initialized for the
    replacement unit, so that passive-heavy units can be tested correctly.
11. As a designer, I want old temporary status effects to be handled
    predictably, so that a swap does not leave invisible modifiers from the
    previous unit type.
12. As a designer, I want the replacement unit to use its own model, so that I
    can verify visual identity and animation states.
13. As a designer, I want the replacement unit to use its own portrait, so that
    battle UI reflects the chosen unit.
14. As a designer, I want the replacement unit to use its own movement speed, so
    that movement highlighting updates after the swap.
15. As a designer, I want the replacement unit to use its own initiative, so that
    later turn queue previews reflect the new unit definition.
16. As a designer, I want the current UI skill buttons to update immediately, so
    that I can see which skills are available.
17. As a designer, I want passive skills to remain non-clickable in the existing
    UI behavior, so that the debug tool does not break skill type rules.
18. As a designer, I want the tool to be available only when there is an active
    selected toster, so that it cannot run in invalid battle states.
19. As a designer, I want the tool to fail clearly when a unit definition or
    prefab is missing, so that data problems are visible during testing.
20. As a developer, I want the swap to use the existing unit data mapper, so that
    it does not add another XML parser or duplicate unit loading rules.
21. As a developer, I want the swap logic isolated from UI controls, so that it
    can be tested without clicking buttons.
22. As a developer, I want the swap to avoid serialized field renames, so that
    existing Unity scene references remain intact.
23. As a developer, I want the first version to avoid editing prefabs and scene
    assets unless explicitly approved, so that the change stays small.
24. As a tester, I want the selected unit outline/highlight to remain coherent,
    so that I still know which unit is active after swapping.
25. As a tester, I want the tool to be runtime-only, so that closing the match
    does not alter saved army builds.
26. As a tester, I want the list to include units regardless of shop ownership,
    so that locked or normally unavailable units can still be tested.
27. As a tester, I want the list to be simple and searchable or compact enough
    for the current unit count, so that switching units is faster than leaving
    combat.
28. As a tester, I want the old model to be removed from the hex when the new
    model appears, so that there are not two visuals for one unit.
29. As a tester, I want the new model to preserve team-facing orientation, so
    that left/right team presentation remains readable.
30. As a tester, I want the changed unit to keep action availability state, so
    that swapping during an active turn does not unexpectedly end the turn.

## Implementation Decisions

- Build this as a battle-scene debug/test tool, not as a production gameplay
  mechanic.
- The source of available unit types is the existing unit definition cache. Do
  not read unit XML manually in the new feature.
- The core behavior should live in a small runtime swap service or component
  with one clear operation: swap this existing active unit to the selected unit
  definition.
- The UI should be a thin wrapper over that operation. It should read the active
  unit from the current input/controller bridge and show a simple list of unit
  names.
- The selected `TosterHexUnit` object should be mutated in place. Do not remove
  it from its team and create a replacement object for the first version.
- The swap must preserve team ownership, team list index, hex occupancy, stack
  amount, selected state, moved/waited/defense flags, and current active unit
  reference.
- The swap should reload base stats, skill ids, sprite reference, and prefab
  reference from the selected unit definition.
- The swap should rebuild cooldowns from scratch, one zero cooldown per new
  skill slot.
- The swap should clear old temporary spell/status modifiers before applying the
  new unit's passive/autocast setup. This avoids carrying hidden modifiers from
  the previous unit type into the new unit.
- The swap should reset transient skill targeting state if a skill was being
  aimed when the tester opens the change tool.
- The swap should rebuild or replace the model GameObject under the same hex
  parent, then reconnect the unit view callback used for movement animation.
- The swap should call the same view-facing logic already used when units are
  generated, so team side orientation remains consistent.
- The swap should refresh amount text after the new view exists.
- The first version should not change saved army files, player preferences,
  shop ownership, progression, or menu army selection.
- The first version should be local runtime tooling. It should not attempt to
  synchronize through Photon/PUN paths.
- Missing data should produce a clear Unity log error and leave the original
  unit unchanged when possible.
- The UI entry point can initially be attached by the user in Unity, because
  this PRD does not authorize scene or prefab edits.

## Testing Decisions

- Good tests should verify externally visible behavior: the active unit remains
  selected, its unit definition changes, its skills and stats come from the new
  definition, and its tactical position/order are preserved.
- Test the core swap logic separately from UI button wiring if possible.
- Add focused tests or manual Unity verification around:
  - swapping active Rusher to FireElemental updates stats, skills, portrait
    reference, prefab reference, and cooldown count,
  - swapping does not move the unit to another hex,
  - swapping does not change team membership or team list index,
  - swapping preserves stack amount,
  - swapping clears old cooldowns and status modifiers,
  - swapping reinitializes passive/autocast setup,
  - swapping while no active unit exists fails without changing state.
- The user compiles and tests inside Unity unless a specific Unity test command
  is later allowed.
- Manual verification should include selecting the changed active unit, moving
  it, casting one active skill, checking passive-skill button behavior, and
  confirming the turn queue/portrait update after the swap.

## Out of Scope

- No production gameplay shapeshift mechanic.
- No army builder changes.
- No saved build mutation.
- No shop/inventory/progression unlock rules.
- No balance changes to unit stats, movement, initiative, damage, or skills.
- No renaming existing public or serialized fields.
- No scene, prefab, material, controller, input action, asmdef, or generated
  Unity asset edits unless explicitly approved later.
- No multiplayer synchronization for the swap tool.
- No full refactor of `TosterHexUnit`, battle UI, skill execution, or turn
  management.
- No new unit definitions.
- No new skill definitions.
- No model or animation asset creation.

## Further Notes

Internal grill decisions:

- Should the tool swap any unit or only friendly units? Recommendation: any
  active unit, because this is a test tool and enemy testing is useful.
- Should stack amount reset to the new unit's default? Recommendation: preserve
  amount, because the user asked to keep the unit active and order stable, and
  amount is part of the current tactical scenario.
- Should current HP ratio be preserved? Recommendation for v1: reset `TempHP`
  to the new unit's base HP while preserving `Amount`. It is predictable and
  matches "load stats". HP-ratio preservation can be a later option.
- Should old status effects survive? Recommendation: no. Clear them to avoid
  invisible modifiers from the previous unit type.
- Should old cooldown values map by skill name when the new unit shares a skill?
  Recommendation for v1: no. Rebuild cooldowns to zero for faster testing and
  fewer edge cases.
- Should this be implemented as delete-and-respawn? Recommendation: no. Mutate
  the existing `TosterHexUnit` in place to preserve active references and team
  ordering.
- Should this be editor-only preprocessor code? Recommendation: prefer a
  runtime debug component that is simply not placed in production scenes/builds
  until the project has a formal debug tooling policy.

Minimal model:

- RuntimeTosterSwapTool
- active unit provider
- unit definition list from data mapper
- swap operation
- debug UI list
- model/view refresh

Example:

- active unit: currently selected `TosterHexUnit`
- selected type: `FireElemental`
- preserved: team, team index, hex, amount, selected state
- reloaded: stats, skills, cooldown slots, sprite, prefab, passive/autocasts
