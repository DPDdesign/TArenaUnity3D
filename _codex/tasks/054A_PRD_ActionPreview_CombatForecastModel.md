# [TARENA] PRD054A: Action Preview Combat Forecast Model

- Status: draft
- Type: PRD / coding task
- Area: Battle Action API, Skill API, Combat Calculation
- Label: ready-for-agent
- Created: 2026-06-30
- Parent PRD: `_codex/tasks/054_PRD_SkillIndicators_ActionPreviewUX.md`
- Follow-up: `_codex/tasks/054B_PRD_ActionPreview_PolishedUIPrefabs.md`
- Follow-up: `_codex/tasks/054C_PRD_ActionPreview_RuntimeUIBinding.md`
- Related: `_codex/Context/maps/battle-action-api-map.md`
- Related: `_codex/Context/maps/skill-api-map.md`
- Related: `_codex/agents/docs/codebase/battle-action-code-map.md`
- Related: `_codex/agents/docs/codebase/skills-effects-code-map.md`

## Goal

Create the canonical non-UI action forecast model for skill, basic attack, and
move-and-attack preview.

The output should be immutable data that UI, future AI, and future server-side
validation can consume. This task should not create prefabs or battle HUD UI.

## Scope

Do:

- Add an action preview request/result shape for selected action plus hovered
  target data.
- Route preview validation through `BattleActionRules` and `SkillRules`.
- Support skill actions, basic ranged/melee attack, and move-and-attack.
- Include retaliation in preview for basic attack and move-and-attack.
- Add a future-compatible skill rule flag or preview decision point for skills
  that may later trigger retaliation.
- Add a shared damage range calculation path used by preview and suitable for
  execution parity.
- Include current actor/target stats, current statuses, stat modifiers,
  defense/resistance rules, and damage range.
- Compute per-unit damage range, kill range, status effects, stat changes, and
  utility movement effects.
- Clamp kill range to current live stack amount.
- Distinguish guaranteed death and possible death.
- Compute aggregate enemy killed units, own lost units, killed unit type
  breakdown, and net economic value range.
- Compute net value from unit catalog cost, not AI scoring.
- Use `UnitDefinitionAsset.cost` through `DataMapper.UnitDefinition.Cost` or an
  equivalent catalog-backed cost provider.
- Treat `DataMapper`/`UnitCatalog` as the catalog-backed authority for unit
  cost. `BattleUnitSnapshot` currently does not carry unit cost, so the value
  calculator must join preview units back to catalog data by stable unit id/name.
- Add focused automated tests for forecast math and range semantics.

Do not:

- Add or edit UI prefabs.
- Add scene GameObjects.
- Modify PRD019 assets.
- Change gameplay float/int balance values.
- Rename public or serialized fields without permission.
- Use tactical AI scoring as player-facing economic value.
- Duplicate combat rules in `MouseControler`, UI scripts, or presentation
  scripts.

## Acceptance Criteria

Done when:

- A valid skill action can produce an action preview result without mutating
  live battle state.
- A valid basic attack can produce preview damage range, kill range, and
  affected target data.
- A valid move-and-attack can include both outgoing damage and retaliation
  damage in one action preview result.
- Retaliation contributes to own-loss aggregate data.
- Existing current skills default to no skill-triggered retaliation unless an
  explicit skill definition/config says otherwise.
- Damage range comes from a shared combat calculation path, not UI code.
- The implementation explicitly resolves the current split between newer
  `BattleActionRules` damage and legacy `TosterHexUnit` damage formulas before
  exposing a forecast as canonical.
- Kill range is clamped to live stack amount.
- A result like `35-37` kills against a stack of 35 is represented as `35` with
  guaranteed death.
- A result like `32-37` kills against a stack of 35 is represented as `32-35`
  with possible death.
- Net value is calculated from catalog-backed unit cost.
- Missing unit economic cost is surfaced as a preview data issue instead of
  silently falling back to AI scoring.
- Automated tests cover basic attack range, retaliation own loss, kill clamp,
  guaranteed death, possible death, and net value from catalog cost.

## Suggested Model Shape

Names may change during implementation, but the model should preserve these
concepts:

- `ActionPreviewRequest`: actor, action kind, skill slot/id when relevant,
  selected/hovered target hexes, action seed/index.
- `ActionPreviewResult`: validity, reject reason, action label, aggregate
  summary, per-unit badge data, effect list.
- `ActionPreviewUnitBadgeData`: unit id, unit type/id, team relation, damage
  range, kill range, death certainty, statuses, stat changes, utility effects.
- `ActionPreviewAggregateData`: enemy killed units, own lost units, per-unit
  killed breakdowns, net value min/max.
- `ActionPreviewEffectData`: typed event-level data for attack, buff, debuff,
  utility, status, movement, spawn, trap, or other effects.

## Testing Decisions

Use EditMode tests for pure model/calculator behavior. Prefer building small
snapshot fixtures over using live scene objects. Similar precedent exists in
BattleAction and SkillRules tests.

Manual Unity validation is not the primary test for this task because no visual
UI should be created here.

## Out of Scope

- Polished UI prefabs.
- Screen-space badge positioning.
- Bottom-right panel binding.
- Invalid target badge rendering.
- Full skill execution migration away from legacy compatibility code.
- AI scoring changes.

## Notes For Coding Agent

Important current-code risks:

- Newer `BattleActionRules` basic attack result generation and legacy
  `TosterHexUnit` damage execution do not appear to be one shared calculator
  yet. This task must not paper over that by inventing a third UI formula.
- `BattleUnitSnapshot` has combat stats but no economic cost field. Net value
  must use catalog lookup through `DataMapper`/`UnitCatalog`, not snapshot math
  or AI scoring.
- Basic ranged attack legality currently relies on current battle-action rules.
  Do not introduce new range constraints as part of preview unless execution
  validation changes in the same canonical path and tests cover it.

Use Codex 5.3 Spark subagents for bounded checks if helpful:

- one explorer can inspect damage execution parity,
- one explorer can inspect unit cost/catalog value sources,
- one explorer can inspect existing tests that should be mirrored.

Do not delegate the final model design decision; keep the main integration
under the primary agent.
