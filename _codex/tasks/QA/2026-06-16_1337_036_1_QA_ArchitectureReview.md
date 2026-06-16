# [TARENA] 036.1 QA Architecture Review - Start Run Slot Availability

- Date: 2026-06-16
- Protocol: `_codex/tasks/QA/2026-06-16_1335_036_1_CodingCompletion_StartRunSlotAvailability.md`
- Task: `_codex/tasks/036_1_StartRunSlotAvailability_Coding.md`
- Verdict: Pass

## Findings

No actionable architecture findings.

## Review Notes

- Ownership is correct: Start Run UI controllers continue to consume adapter and
  service view data rather than querying SQLite directly.
- Persistence boundary is correct: locked state is calculated and no `IsLocked`
  field or schema change was introduced.
- DB-backed account facts are read through a Start Run availability source,
  keeping account XP and won-run summary checks out of UI.
- Generator integration is appropriately narrow: request-count support is added
  through an optional requested-offer source interface, preserving existing
  `IStartingArmyTemplateSource` callers.
- PRD035 direction is preserved: generated offers remain generated screen data;
  selected army persistence still happens only at Begin Run.
- TMP rule is respected: the new locked reason UI reference uses `TMP_Text` and
  does not introduce legacy `UnityEngine.UI.Text`.
- Unity asset boundary is respected: no prefabs, scenes, materials, generated
  files, `.asmdef`, or `.asmref` were edited.

## Non-Blocking Observations

- The new locked overlay and reason TMP fields require manual prefab/Inspector
  wiring before Play Mode validation.
- Slot 4 gets the demo reason when the generator can produce the fourth offer.
  If a non-request-aware source produces fewer templates, the service fails
  closed as `Coming soon`, which is acceptable fallback behavior for legacy
  sources.
- Generated-offer analytics remains intentionally out of scope and should stay
  in a separate PRD/schema decision.

## Test Gap

Focused EditMode tests are still needed after this QA pass for:

- slot rule matrix,
- requested generated offer count,
- Begin Run rejection for locked slots,
- DB-backed won-run and XP progress facts.

## Final Verdict

Pass. Continue to focused EditMode test authoring.
