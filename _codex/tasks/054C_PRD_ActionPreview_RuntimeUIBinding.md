# [TARENA] PRD054C: Action Preview Runtime UI Binding

- Status: draft
- Type: PRD / coding task
- Area: Battle HUD Runtime, MouseControler Integration, Action Preview UI
- Label: ready-for-agent
- Created: 2026-06-30
- Parent PRD: `_codex/tasks/054_PRD_SkillIndicators_ActionPreviewUX.md`
- Depends On: `_codex/tasks/054A_PRD_ActionPreview_CombatForecastModel.md`
- Depends On: `_codex/tasks/054B_PRD_ActionPreview_PolishedUIPrefabs.md`
- Related: `_codex/Context/maps/ui-map.md`
- Related: `_codex/Context/maps/battle-action-api-map.md`
- Related: `_codex/agents/docs/codebase/battle-action-code-map.md`

## Goal

Wire the action preview forecast model into the battle UI using the polished
PRD054 prefabs.

The runtime should show the fixed bottom panel, per-unit screen-space badges,
and invalid target badge while preserving existing skill indicators and target
highlights.

For this task, scene hookup is manual-user territory unless explicitly
authorized later. The Coding Agent should prepare runtime scripts, binders, and
prefab-ready references, but should not guess or force final scene wiring.

## Scope

Do:

- Add or update `ActionPreviewController` on a GameObject named
  `Action Preview Controller`.
- Bind the bottom preview panel, unit badge prefab, invalid badge prefab, and
  badge Canvas through serialized fields.
- Show the selected skill/action name immediately after skill selection.
- Show `-` values in the bottom panel when no target forecast exists.
- On valid hover, request an action forecast and bind aggregate data to the
  bottom panel.
- On valid hover, spawn/update one screen-space badge per affected unit.
- Convert affected unit world positions into Canvas positions.
- Hide empty badge sections.
- On invalid hover, show the invalid badge with `Invalid target`.
- Keep invalid badge easy to disable later through a serialized toggle or
  optional prefab reference.
- Clear all badges and invalid state when targeting is cancelled, action is
  committed, selected skill changes, selected unit changes, turn changes, or
  action lifecycle blocks.
- Support selected skill preview, basic attack preview, and move-and-attack
  preview.
- Keep PRD053 skill indicators visible and independent.

Do not:

- Calculate damage or kills in UI.
- Calculate net value in UI.
- Decide skill legality in UI.
- Replace existing valid target highlights.
- Change gameplay numeric values.
- Modify PRD019 prefabs.
- Introduce legacy `UnityEngine.UI.Text`.

## Scene/Prefab Wiring

Preferred Unity setup:

- A dedicated Canvas contains action preview panel and badge parent areas.
- `Action Preview Controller` references that Canvas and the PRD054 prefabs.
- Bottom panel is fixed in the bottom-right battle HUD area.
- Unit badges are screen-space UI objects positioned from affected unit world
  positions.

This task should stop at runtime/prefab-ready integration plus exact manual
scene hookup instructions when the target battle scene hookup is not explicitly
assigned.

## Acceptance Criteria

Done when:

- Selecting a skill shows the action name in the bottom panel.
- Before hover, bottom panel value fields show `-`.
- Hovering a valid skill target shows aggregate summary and per-unit badges.
- Hovering an invalid skill target shows `Invalid target`.
- Basic attack hover can show damage/kills preview.
- Move-and-attack hover can show outgoing damage and retaliation own-loss
  preview.
- Retaliation appears in both per-unit badge data and aggregate summary when
  applicable.
- Badges are screen-space UI prefabs, not world-space text objects.
- One badge is shown per affected unit.
- Badge section roots are hidden when their data is empty.
- Existing PRD053 arrows/AoE/hex indicators keep working independently.
- All preview UI clears on cancel, commit, target change, selected unit change,
  turn change, and blocking action lifecycle.
- If scene hookup is not part of the current task permission, the task may
  complete with prefab-ready scripts plus a precise manual hookup checklist.

## Testing Decisions

Manual Play Mode validation is required:

1. Select a unit with a skill and confirm the panel shows the skill name and `-`
   values before target hover.
2. Hover a valid single-target damage skill and confirm one badge appears.
3. Hover an AoE/multi-target skill and confirm one badge per affected unit.
4. Hover an invalid target and confirm `Invalid target` appears.
5. Select a basic attack target and confirm attack preview appears.
6. Select a move-and-attack target that can retaliate and confirm own loss is
   included.
7. Cancel targeting and confirm all preview UI clears.
8. Commit action and confirm preview UI clears.
9. Change skill and confirm stale badges disappear.
10. Confirm skill indicators from PRD053 remain visible.

Automated tests should cover UI binder behavior where possible by binding DTOs
to presentation classes and asserting section visibility/text values.

## Out of Scope

- Creating polished prefabs; belongs to PRD054B.
- Combat forecast math; belongs to PRD054A.
- Special action edge-case expansion; belongs to PRD054D.
- Full battle HUD redesign.
- Final scene hookup when the user plans to wire the scene manually later.

## Notes For Coding Agent

`MouseControler` is a high-risk integration file. Keep changes small and use
new helper/controller classes where possible.

Use Codex 5.3 Spark subagents for bounded read-only checks if useful:

- one explorer can map current skill hover lifecycle,
- one explorer can map basic attack/move-and-attack hover entry points,
- one explorer can inspect existing UI update patterns.
