# [TARENA] PRD054: Skill Indicators + Action Preview UX

- Status: draft
- Type: PRD / context
- Area: Battle Action API, Skill API, Combat Presentation, Battle HUD
- Label: ready-for-agent
- Created: 2026-06-30
- Depends On: `_codex/tasks/053A_PRD_SkillIndicatorRuntime.md`
- Related: `_codex/tasks/053B_PRD_SkillIndicatorCatalogSetup.md`
- Related: `_codex/Context/maps/battle-action-api-map.md`
- Related: `_codex/Context/maps/skill-api-map.md`
- Related: `_codex/Context/maps/combat-presentation-map.md`
- Related: `_codex/Context/maps/ui-map.md`
- Related: `_codex/agents/docs/codebase/battle-action-code-map.md`
- Related: `_codex/agents/docs/codebase/skills-effects-code-map.md`
- Task A: `_codex/tasks/054A_PRD_ActionPreview_CombatForecastModel.md`
- Task B: `_codex/tasks/054B_PRD_ActionPreview_PolishedUIPrefabs.md`
- Task C: `_codex/tasks/054C_PRD_ActionPreview_RuntimeUIBinding.md`
- Task D: `_codex/tasks/054D_PRD_ActionPreview_HardeningAndCoverage.md`

## Problem Statement

Players can currently see valid target highlights and PRD053 skill-shaped
indicators, but they cannot reliably preview the tactical consequence of a
hovered action. A selected skill, basic attack, or move-and-attack can damage
multiple stacks, kill units, apply statuses, change stats, move units, or cause
retaliation. The UI does not yet summarize that outcome before commit.

The player needs to know, before clicking, what the action will probably do:
per affected unit on the board, and as a compact total in the battle HUD.

This is especially important for a Heroes-like stack combat game because a
visually simple click can be a bad trade when retaliation kills the player's
own stack or when damage variance changes whether a target dies.

## Solution

Add an action preview system that uses the same canonical validation and
resolution path as real actions.

The system will show:

- a fixed bottom-right action preview panel connected to the battle HUD,
- screen-space per-unit preview badges anchored over affected units,
- an invalid-target badge during illegal hover,
- range forecasts for damage, kills, death certainty, and net economic value,
- a full-action summary that includes retaliation and own losses.

Existing skill indicators from PRD053 remain visible. This PRD extends them
with outcome preview UI; it does not replace target highlights, arrows, AoE
markers, selected skill highlights, or valid/invalid target highlights.

This PRD assumes PRD053A's core marker runtime is already accepted as the
foundation, even if later marker polish or edge-case follow-up work continues.

## Confirmed UX Decisions

- Preview uses a range forecast, not only a single exact roll.
- The bottom-right panel is a fixed HUD panel, not a tooltip following the
  mouse.
- The bottom panel is a summary for the whole action.
- The bottom panel shows the selected skill/action name immediately after
  selection.
- Before hover/target selection, summary values display `-`.
- Per-unit badge content appears after a valid hovered target produces affected
  unit data.
- Invalid hover shows an `Invalid target` screen-space badge for MVP.
- Invalid badge must be easy to disable later without changing preview logic.
- Per-unit badges are screen-space UI prefabs on a dedicated action preview
  Canvas.
- Badge positions are calculated from affected unit world positions into Canvas
  positions.
- One badge is shown per affected unit.
- Each badge can contain all local outcome sections for that unit.
- Badge sections hide when they have no data.
- Badge data does not need a source marker.
- The bottom panel separates enemy kills and own losses using icons, not text:
  red skull for enemy killed units, blue skull for own lost units.
- The bottom panel includes mini `StackRepresentation` rows/icons for killed
  unit types and counts.
- Fully killed stacks get a death marker: skull overlay plus greyed circle.
- Net value is an economic combat value delta:
  enemy value killed minus own value lost.
- Net value can be positive or negative.
- Net value uses unit economic cost from catalog data, not AI scoring.
- Existing skills currently do not trigger retaliation unless a future skill
  definition explicitly opts in.
- Basic attack and move-and-attack do trigger retaliation according to current
  rules.

## User Stories

1. As a player, I want the selected skill name to appear in a fixed preview panel, so that I know which action is being forecast.
2. As a player, I want the preview panel to show `-` before I hover a target, so that empty state is not confused with an invalid action.
3. As a player, I want each affected unit to receive one local badge, so that I can quickly read what happens to that unit.
4. As a player, I want badge sections to hide when empty, so that no unit shows irrelevant information.
5. As a player, I want damage ranges on badges, so that I understand variance before committing.
6. As a player, I want kill ranges on badges, so that I can see whether a stack will probably lose units.
7. As a player, I want guaranteed death and possible death to look different, so that I can distinguish certainty from risk.
8. As a player, I want kill ranges clamped to the current stack amount, so that a stack of 35 never displays `35-37`.
9. As a player, I want a guaranteed stack wipe to show a clear death marker, so that I know the stack will disappear.
10. As a player, I want status icons on badges, so that damage and status outcomes are visible together.
11. As a player, I want stat change icons on badges, so that buff and debuff effects are clear without large text.
12. As a player, I want utility movement icons on badges, so that push, pull, teleport, swap, or other movement effects are visible.
13. As a player, I want the bottom panel to summarize total enemy killed units, so that I can quickly understand offensive value.
14. As a player, I want the bottom panel to summarize own losses separately, so that retaliation and friendly-fire costs are not hidden.
15. As a player, I want red and blue skull icons for enemy kills and own losses, so that I do not need explanatory labels.
16. As a player, I want killed unit types shown with mini stack representations, so that I know what kind of units die.
17. As a player, I want net value to be positive or negative, so that I can read whether the exchange is economically favorable.
18. As a player, I want retaliation to be included in both badges and summary, so that the forecast covers the whole action.
19. As a player, I want an invalid badge on illegal hover, so that I know the target cannot be clicked.
20. As a designer, I want attack, buff, debuff, and utility preview sections to share one data contract, so that different prefabs can use the same binder.
21. As a designer, I want polished UI prefabs created under a PRD054 folder, so that the scene can be wired through Inspector fields.
22. As a developer, I want preview generated from battle action validation and resolution, so that UI does not duplicate combat rules.
23. As a developer, I want a shared damage range calculator, so that execution and preview use the same combat math.
24. As a developer, I want economic net value from unit catalog costs, so that forecast value matches game economy instead of AI heuristics.
25. As a developer, I want preview data to be immutable DTO-style data, so that UI cannot mutate battle state.
26. As a future AI developer, I want the same forecast model usable outside UI, so that tactical scoring can eventually reuse it.
27. As a future server developer, I want preview derived from snapshot and action definitions, so that server-side validation can recompute it.

## Implementation Decisions

- The preview architecture should be split into forecast generation and UI
  rendering.
- Forecast generation owns validation, affected-unit resolution, damage range,
  kill range, status/stat/utility effects, retaliation, and net economic value.
- UI rendering owns Canvas placement, prefab binding, section visibility, icons,
  TMP text, invalid badge visibility, and bottom panel presentation.
- UI rendering must not calculate damage, legality, affected units, unit cost,
  or retaliation.
- Forecasts should use `BattleActionUse` -> `BattleActionRules.Validate(...)`
  -> canonical action/skill resolution -> action preview result.
- Skill actions should continue to route skill-specific target validation
  through `SkillRules`.
- Basic attack and move-and-attack are in MVP.
- Pure movement preview without attack is later scope unless needed by a skill
  such as rush/teleport.
- Damage ranges must come from current actor and target stats, current statuses,
  buffs/debuffs, and defense/resistance rules.
- A shared combat damage range calculator should be extracted or introduced so
  preview and execution cannot drift.
- Retaliation is represented as part of the same action forecast, not as a
  separate UI mode.
- Skill definitions should support a future `triggersRetaliation`-style rule.
  Current skills default to no retaliation unless explicitly configured later.
- Net value uses unit economic cost from `UnitDefinitionAsset.cost` via
  `DataMapper.UnitDefinition.Cost` or an equivalent catalog-backed source.
- Existing AI scoring formulas must not be used for player-facing net value.
- If a catalog-backed cost cannot be resolved for a unit, the preview should
  report missing value data rather than silently using AI heuristics.
- Kill display clamps to live stack amount.
- If minimum kills equal live stack amount, display the live stack amount and a
  guaranteed death marker.
- If maximum kills reaches live stack amount but minimum kills does not, display
  the clamped range and a possible death marker.
- Bottom panel value fields display `-` until there is a target forecast.
- Per-unit badges are generated from `ActionPreviewUnitBadgeData` or equivalent
  per affected unit DTO.
- The bottom panel uses aggregate DTO data and should not recompute from badge
  UI state.
- `ActionPreviewController` lives on a GameObject named `Action Preview Controller`.
- `ActionPreviewController` should bind an existing, designer-authored Canvas,
  bottom panel prefab/instance, unit badge prefab, invalid badge prefab, and
  repeated stack representation prefab/parent fields.
- Prefab binding should follow the same approach as existing UI view scripts:
  serialized fields, TMP components, nested view classes, repeated `Transform
  parent + prefab`.
- Existing PRD019 assets are reference-only. Do not modify them.

## Task Breakdown

- Task A builds the combat forecast model and calculators with automated tests.
- Task B creates polished UI prefabs under `Assets/Resources/UI/PRD_54/ActionPreviewUX/` using project UI skills and reference prefabs.
- Task C wires runtime UI binding and hover integration through `ActionPreviewController`.
- Task D hardens coverage for edge cases, special actions, invalid states, and QA.

## Testing Decisions

Good tests should prove the externally visible contract:

- legal action produces expected affected units,
- damage ranges match canonical combat math,
- kill ranges clamp to stack amount,
- guaranteed and possible death states are distinct,
- retaliation contributes to own-loss summary,
- net value uses catalog cost,
- UI binders show and hide sections based on DTO data.

Automated tests should focus on pure forecast/calculator code and presentation
binders where feasible. Unity Play Mode validation is required for screen-space
badge placement, prefab wiring, and visual readability.

## Out of Scope

- Replacing PRD053 skill indicators.
- Replacing all skill execution paths.
- Changing skill balance, damage values, cooldown values, unit stats, movement
  values, or economic costs.
- Full server validation implementation.
- Full tactical AI rewrite.
- Pure movement-only preview unless needed by a skill forecast.
- Final visual redesign of the whole battle HUD.
- Editing PRD019 prefab assets.

## Further Notes

This PRD was prepared after a grill session. The major risk is not UI layout;
the major risk is preview/execution drift. Coding work should therefore start
with shared forecast calculation, then build UI on top of immutable forecast
data.

Use Codex 5.3 Spark subagents for bounded reconnaissance during implementation,
especially for independent checks such as damage parity, UI prefab references,
and edge-case coverage. The main Coding Agent remains responsible for final
integration and verification.
