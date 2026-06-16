# 2026-06-14_1924 028_TURN-001 QA Architecture Review

## Verdict

Pass.

## Reviewed Protocol

- `_codex/tasks/QA/028_TURN-001_TurnActionSkillRules_Coding_Protocol.md`

## Files Reviewed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `_codex/tasks/archive/028_TURN-001_TurnActionSkillRules_Coding.md`

## Findings

No blocking findings.

## Architecture Notes

- The per-turn repeated skill rule now belongs to unit turn state through
  `TosterHexUnit` helper methods instead of being held only as an input-layer
  boolean. This is a reasonable local boundary for the current legacy structure.
- `MouseControler.CanStartSkill` is now the single validation source used by
  both execution and skill button interactability through `CanUseSkillSlot`.
  This reduces the previous UI/runtime drift.
- Movement completion now asks the same skill validator whether any legal AM
  skill remains. This correctly includes cooldown, passive, repeat-use, and
  waited-state checks.
- Capturing `selectedSkillId` before `CastManager` can mutate a skill slot
  protects stance toggles and targeted skill commits from slot-name drift.

## Non-Blocking Observations

- Action buttons are still coarser than skill buttons. WAIT is blocked by
  `WaitB` and `TryStartWaitAction`, but the shared action button UI does not yet
  expose separate per-action interactability for WAIT versus DEFENCE. This is
  outside the current task's acceptance criteria but should be considered when
  action button UX is revisited.
- The deeper architecture still has action validation spread across
  `MouseControler`, `TosterHexUnit`, and `UICanvas`. The current change keeps the
  fix small, but a future extraction of a battle action validator would make
  these rules easier to test without scene setup.

## Follow-Up Required

No follow-up code fixes required for this task.
