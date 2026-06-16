# 020 Follow-Up Coding Completion

## Task

Follow-up fix for QA finding in
`_codex/tasks/QA/2026-06-14_1935_020_QA_ArchitectureReview.md`.

## Files Changed

- `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`

## Systems Touched

- Task 20 HTML/JS mockup navigation only.

## Behavior Or Setup Summary

- Updated `select-mockup-starting` to scroll to the merged
  `mockup-20-armies` scene instead of the removed `mockup-20-details` anchor.
- The mockup now consistently treats Starting Army selection and the inspector
  as one scene.

## Unity Checks

- No Unity test run was executed automatically.
- Static JavaScript syntax check passed with `node --check`.
- Searched the mockup for stale `mockup-20-details` references; none remain.

## Intentionally Not Included

- No runtime C# changes in this follow-up.
- No Unity scene, prefab, Canvas, material, generated Unity file, `.asmdef`, or
  `.asmref` edits.
