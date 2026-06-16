# 028_TURN-001_TurnActionSkillRules_Coding

- Status: closed
- Type: Coding
- Area: Battle turns, skills, UI
- Owner: Coding Agent

## Goal

Fix the battle action rules so WAIT, DEFENCE, movement, NI skills, AM skills,
and repeated skill use follow the current gameplay contract.

## Scope

Do:

- Allow WAIT only as the first action; it moves the unit to the end of the
  queue and cannot be used twice in the same turn window.
- Allow DEFENCE only as the first action; it applies defence and ends the
  unit's turn.
- After MOVE, allow only legal AM skills.
- After a first NI skill, allow other legal NI skills, allow movement, and then
  allow legal AM skills after that movement.
- Prevent using the same skill twice in the same turn window, except
  Melee_Stance and Range_Stance toggles.
- Ensure MOVE automatically ends the unit's turn when no legal AM skill remains
  available after movement.
- Keep the UI skill buttons aligned with the same runtime validation.

Do not:

- Edit prefabs, scenes, Unity assets, XML data, or serialized field names.
- Redesign the skill system or replace CastManager reflection routing.
- Run Unity or external build/test commands.

## Acceptance Criteria

Done when:

- WAIT before any other action still works and a second WAIT is blocked.
- DEFENCE before any other action ends the unit turn.
- MOVE first ends the unit turn automatically if no legal AM skill is available.
- MOVE first leaves the unit active if a legal AM skill is available.
- NI skill first does not block different NI skills.
- NI skill first allows movement when the NI skill permits movement after use.
- NI skill, then movement, then AM skill is allowed when the AM skill is legal.
- The same non-toggle skill cannot be used twice in the same turn window.
- Melee_Stance and Range_Stance remain repeatable toggles.
- UI skill button interactability reflects the same rules used by actual skill
  execution.

## Implementation - 2026-06-14

### What Changed

No Inspector fields changed.

- `MouseControler`: added captured `selectedSkillId`, per-slot
  `CanUseSkillSlot`, repeat-skill validation, toggle-skill exception handling,
  passive-skill blocking, and movement completion that ends the unit turn only
  when no legal post-move AM skill remains.
- `TosterHexUnit`: added per-turn used skill id storage plus helper methods to
  clear, query, and add skill ids safely.
- `TeamClass`: resets the new per-turn used skill id state at new-turn reset.
- `UICanvas`: skill buttons now use `MouseControler.CanUseSkillSlot`, so UI
  availability follows the same rules as actual skill execution.
- `_codex/tasks`: added the formal task, Coding Agent protocol, and QA review
  report for this implementation workflow.

### Automatic Test

No EditMode tests were added. The project has no local game-code EditMode test
folder or `Assets/1_Scripts/Tests/EditMode` setup; only vendor/plugin tests were
found. Creating a new test assembly or `.asmdef` was intentionally avoided.

Run validation manually in Unity Test Runner only if you later add a local game
test assembly. For this change, use the Play Mode checks below.

### Unity Test

#### Unity Setup

- Open the battle scene with a normal `MouseControler`, `TurnManager`,
  `UICanvas`, and units already wired as before.
- Use units that expose: WAIT, DEFENCE, a normal move, at least one `NI` skill,
  at least one `AM` skill, and stance toggle skills if available.
- No new Inspector fields need assignment.

#### Play Mode Test

- Use WAIT as the first action: the unit should move to the end of the queue and
  should not be able to WAIT again in that same turn window.
- Use DEFENCE as the first action: the unit should enter defence and stop acting.
- Move first with no legal AM skill available: the unit should end its turn
  automatically.
- Move first with a legal AM skill available: the unit should remain active and
  only legal AM skill buttons should be usable.
- Use an NI skill first: a different legal NI skill should still be usable, but
  the same non-toggle skill should not be usable again.
- Use NI skill, then move, then AM skill: this should be allowed when the AM
  skill is legal.
- Use Melee/Range stance toggles: the toggle pair should remain repeatable.

### QA Verdict

Pass.

- QA report:
  `_codex/tasks/QA/2026-06-14_1924_028_TURN-001_QA_ArchitectureReview.md`
- No actionable QA findings.
- No follow-up fix round was required.
- Non-blocking observation: action buttons remain coarse; WAIT is blocked by
  runtime checks, but the shared action button UI still does not have separate
  per-action interactability for WAIT versus DEFENCE.

### Notes

- XML data, prefabs, scenes, materials, and serialized field names were not
  changed.
- The fix keeps validation in the existing legacy `MouseControler` flow instead
  of extracting a new battle action validator.
- Command-line Unity, dotnet, and build/test commands were not run.

### Next Steps

- Run the Play Mode checks above in the already open Unity Editor.
- Watch especially `NI skill -> Move -> AM skill` and repeated non-toggle skill
  attempts, because those were the broken cases.

## Closure - 2026-06-14

Status: closed after user confirmed the flow works preliminarily.

Follow-up design truth was captured in:

- `_codex/Context/BattleActionRules.md`

Final clarifications captured after implementation:

- All active non-passive skills are usable before movement by default.
- `AM` is not a default flag; it is explicit opt-in for use after movement.
- `AM` was removed from all current skills for now.
- `NI` remains the flag for skills that do not end the turn.
- Melee Stance and Range Stance are not turn actions; they are free attack-mode
  toggles and do not affect queue, movement, cooldown, or turn completion.
