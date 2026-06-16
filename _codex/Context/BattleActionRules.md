# Battle Action Rules

Status: active
Type: context
Area: battle turns, skills, UI
Audience: agents working on battle rules, skill validation, or related UI

## Problem Statement

The battle turn flow became hard to reason about after queue, skill UI, and
TextMeshPro changes landed close together. From the player perspective, actions
must be predictable: WAIT, DEFENCE, movement, skills, and stance toggles each
need a clear cost in the current unit's turn. The UI must also keep rendering
skill buttons, cooldowns, chat messages, and chat controls after TextMeshPro and
prefab hierarchy changes.

## Solution

Define a stable battle action contract and keep runtime validation, skill button
state, and data flags aligned with that contract.

The current accepted rule set is:

- WAIT is available only before movement and before non-stance skills. It moves
  the unit to the end of the queue and cannot be used twice in the same turn
  window.
- DEFENCE is available only before movement and before non-stance skills. It
  applies defence and ends the unit's turn.
- Any active non-passive skill may be used before movement.
- A skill without `NI` ends the unit's turn when used.
- A skill with `NI` does not end the unit's turn; after it, the unit may use
  other legal skills and may still move.
- After movement, only skills with `AM` are legal.
- The same non-toggle skill cannot be used twice in the same turn window.
- Melee Stance and Range Stance are not turn actions. They are free toggles that
  only switch whether the unit attacks in melee or range mode. They do not
  affect movement, queue position, cooldown, or turn completion.
- For now, no skills carry `AM`; future skills can opt into post-move use by
  adding `AM` explicitly.

The current accepted UI stability scope is:

- UI text references are TextMeshPro-based.
- `UICanvas` should tolerate missing optional TMP/UI references during prefab
  migration instead of crashing the battle loop.
- Skill name text is optional and controlled by `ShowSkillName`.
- Cooldown text and cooldown fill are the required skill availability feedback.
- `TypeT` is legacy UI-cache data and should not be required for passive/active
  skill logic.
- Chat uses TextMeshProUGUI message rows.
- Chat prefab layout must keep content, text rows, scrollbar, top arrow, and
  bottom arrow visible after hierarchy changes.

## User Stories

1. As a player, I want WAIT to move my unit to the end of the queue, so that I
   can delay without ending the whole round logic unpredictably.
2. As a player, I want WAIT to be blocked after movement, so that I cannot stack
   multiple tempo actions in one turn window.
3. As a player, I want WAIT to be blocked after using a real skill, so that
   skill use has a clear action cost.
4. As a player, I want DEFENCE to be available as an opening action, so that I
   can commit a unit to a defensive turn.
5. As a player, I want DEFENCE to end the unit's turn, so that defence is a
   meaningful commitment.
6. As a player, I want any active skill to be usable before movement, so that I
   do not need special flags for normal pre-move skill use.
7. As a player, I want a non-`NI` skill to end the unit's turn, so that powerful
   skills have an obvious cost.
8. As a player, I want an `NI` skill to leave the unit active, so that utility
   skills can be chained intentionally.
9. As a player, I want an `NI` skill to still allow movement, so that support
   actions can happen before repositioning.
10. As a player, I want post-move skills to require `AM`, so that moving first
    has a restricted and readable follow-up rule.
11. As a player, I want the same non-toggle skill blocked after I use it once,
    so that I cannot repeat a skill in the same turn window.
12. As a player, I want Melee/Range Stance to stay repeatable, so that I can
    correct attack mode without spending the turn.
13. As a player, I want stance toggles to avoid cooldown and queue changes, so
    that they feel like mode switches rather than skills.
14. As a player, I want skill buttons to grey out exactly when the runtime would
    reject them, so that UI state teaches the rules.
15. As a player, I want cooldown overlays and numbers to remain visible after
    TextMeshPro migration, so that I can understand skill availability.
16. As a player, I want chat messages to render after the UI hierarchy changes,
    so that combat feedback remains readable.
17. As a developer, I want skill flags to mean one thing, so that XML data does
    not encode hidden default behavior.
18. As a developer, I want missing UI references to fail softly where possible,
    so that prefab migration mistakes do not stop the battle loop.
19. As a developer, I want chat prefab layout to be sane by default, so that
    arrows, content, and text are visible after instantiation.
20. As a designer, I want `AM` absent by default, so that post-move skill use is
    a deliberate opt-in.

## Implementation Decisions

- Keep the current legacy battle action flow, but centralize actual skill
  availability checks behind one runtime validator used by both execution and
  skill button interactability.
- Track used skill ids per unit turn window, not just a global "used any skill"
  boolean.
- Keep the global "used any skill" state for coarse action buttons such as WAIT
  and DEFENCE.
- Treat stance skill ids as a special category: repeatable, no cooldown, no used
  skill id entry, no movement lock, no turn completion.
- Treat passive skills as never directly castable from action buttons.
- Treat missing `AM` as "not usable after movement".
- Treat missing `NI` as "ends the turn when used".
- Keep `Flags` in skill XML as optional text. Empty `Flags` means no optional
  behavior.
- Keep `NI` and `AM` as independent flags:
  - `NI`: can continue after skill.
  - `AM`: can be used after movement.
- Keep `Melee_Stance` and `Range_Stance` outside the flag model because they
  are attack-mode toggles, not turn actions.
- Preserve TextMeshPro-based UI fields and make UI code tolerant of missing
  optional references during migration.
- Do not rely on `TypeT` for `UnUseSkill`; resolve passive skill type from the
  selected unit skill id and skill data instead.
- Make chat rendering resilient to missing text template references by creating
  a fallback TextMeshProUGUI entry when needed.
- Keep chat prefab layout visible by default: fixed-size root, full viewport,
  visible top/bottom arrows, content area with right-side space for arrows, and
  a normal TMP text row template.

## Testing Decisions

- The primary validation is manual Play Mode testing in the already open Unity
  Editor because the project currently has no local game-code EditMode test
  assembly.
- Good tests should verify player-visible action outcomes, not private flags:
  queue position, whether the unit remains active, visible skill button state,
  cooldown behavior, stance mode, and chat rendering.
- Battle action validation is a future candidate for extraction into a deep,
  isolated module that can be tested without scene/prefab setup.
- Manual scenarios to keep as regression checks:
  - WAIT first, then ensure second WAIT is blocked.
  - DEFENCE first, then ensure turn ends.
  - Non-`NI` skill first, then ensure turn ends.
  - `NI` skill first, then another different legal skill, then movement.
  - Movement first, then ensure only `AM` skills can follow.
  - Stance toggle before and after other legal states, then ensure it does not
    consume turn or cooldown.
- Chat message creation after a skill message, then ensure visible text and
  arrows remain inside the chat panel.
- Missing optional TMP lists in `UICanvas` should not throw
  `NullReferenceException`; unavailable UI elements should simply skip visual
  updates.

## Out of Scope

- Rebuilding the full skill system.
- Replacing reflection-based skill method routing.
- Reworking the whole turn manager or queue algorithm.
- Adding new `AM` skills right now.
- Designing final UI art for chat, skill buttons, or cooldown overlays.
- Creating new Unity test assemblies or `.asmdef` files without explicit
  permission.
- Cleaning old Photon/PlayFab coupling.

## Further Notes

- Current accepted XML state removes `AM` from all skills. `NI` remains only on
  skills that should allow continued action after use.
- The current implementation is intentionally local and pragmatic. A future
  architecture task should extract battle action validation from the input/UI
  monolith into a small, testable rules module.
- Chat prefab instance overrides in a scene can still hide fixed prefab layout.
  If a scene instance keeps old positions, revert prefab overrides or replace
  the scene instance with the updated prefab.

## Captured From

This context was extracted on 2026-06-15 from the closed implementation/task
thread around `028_TURN-001` and the former `029` task file because it defines
ongoing agent guidance, not a new PRD.

Captured scope includes:

- battle action rule stabilization,
- skill flag cleanup,
- stance toggle behavior,
- TextMeshPro field migration hardening,
- `UICanvas` null-safety for missing TMP/UI references,
- chat message fallback for missing TMP text template,
- `Chat.prefab` and `Chat 1.prefab` layout fixes for visible arrows/content.
