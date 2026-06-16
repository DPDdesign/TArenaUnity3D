# 020 QA Architecture Review

## Reviewed Protocol

- `_codex/tasks/QA/2026-06-14_1934_020_CodingCompletion_StartRun.md`

## Verdict

Follow-up required.

## Findings

### 1. Stale mockup anchor after merging Scene 1 and Scene 2

- Severity: Medium
- File: `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`
- Evidence: `select-mockup-starting` still calls
  `scrollToMockup("mockup-20-details")`, but the `mockup-20-details` section was
  removed when Starting Army selection and inspector were merged into one scene.
- Risk: selecting a starting army no longer scrolls/focuses to a valid section,
  so the product-owner mockup can appear unresponsive or inconsistent with the
  corrected scene flow.
- Required fix: change the scroll target to the combined scene anchor
  `mockup-20-armies` or remove the extra scroll.

## Non-Blocking Observations

- The new `RunMetagame/StartRun` code is separated from legacy
  `PanelArmii.BuildG`, build files, `PlayerPrefs`, PlayFab, PUN, Photon, scenes,
  and prefabs.
- `StartRunService` exposes mode-neutral command/result data and keeps the
  Offline implementation behind an adapter boundary.
- The current record store is in-memory. This is acceptable for the first
  boundary slice, but the next persistence task should replace it with a real
  local adapter without changing `StartRunService`.
- Starting currency is explicitly `0` because no grill decision confirmed run
  starting currency.

## Follow-Up Required

Yes. Apply the stale-anchor fix only, then run a final QA pass against the
follow-up protocol.
