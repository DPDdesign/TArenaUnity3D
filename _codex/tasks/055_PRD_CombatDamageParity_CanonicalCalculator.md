# [TARENA] PRD055: Combat Damage Parity And Canonical Calculator

- Status: draft
- Type: PRD / coding task
- Area: Battle Action API, Combat Calculation, Legacy Combat Parity
- Label: draft
- Created: 2026-06-30
- Blocks: `_codex/tasks/054A_PRD_ActionPreview_CombatForecastModel.md`
- Related: `_codex/tasks/054_PRD_SkillIndicators_ActionPreviewUX.md`
- Related: `_codex/Context/maps/battle-action-api-map.md`
- Related: `_codex/agents/docs/codebase/battle-action-code-map.md`
- Related: `_codex/agents/docs/codebase/skills-effects-code-map.md`
- Child task: `_codex/tasks/archive/055A_PRD_CoreCombatDamageService_BasicCombat.md`
- Child task: `_codex/tasks/055B_PRD_SkillDamageMigration_CombatDamageService.md`
- Child task: `_codex/tasks/archive/055C_PRD_LegacyDamageTraceCleanup.md`

## Grill Decision Update - 2026-06-30

This PRD is now the umbrella and decision record for the combat damage service
work. Implementation is split into child tasks:

1. PRD055A: core `CombatDamageService`, pure calculator, catalog interface, and
   basic combat migration.
2. PRD055B: skill damage migration to `CombatDamageService`.
3. PRD055C: cleanup of legacy damage traces after migration.

These decisions supersede any older wording in this file that implies partial
parity, fallback damage, exclusive `MaxDamage` behavior, or leaving legacy
damage paths in place.

Hard rules:

- No workaround damage paths.
- No fallback to legacy damage.
- No fallback to incomplete snapshot stats.
- No silent `0 damage` fallback.
- If deterministic damage cannot be calculated, reject the action/result with
  a clear reason and log.
- `BattleActionRules` remains the action legality authority.
- `CombatDamageService` owns damage input resolution.
- `CombatDamageCalculator` owns only pure deterministic math.

Required architecture:

- `BattleActionRules`
  - validates legality,
  - identifies actor/target/action kind/roll purpose,
  - calls `CombatDamageService`,
  - does not own damage formula details.
- `CombatDamageService`
  - resolves `RuntimeUnitId` from snapshot state,
  - resolves `CatalogUnitId` through an injected catalog interface,
  - combines catalog base stats, snapshot modifiers/statuses, and combat state,
  - calls the pure calculator.
- `CombatDamageCalculator`
  - has no Unity/global random/catalog/database dependency,
  - returns real forecast min/max and deterministic committed damage.

Identity decisions:

- Add `CatalogUnitId` alongside `RuntimeUnitId`.
- `RuntimeUnitId` identifies the concrete battle stack and slot.
- `CatalogUnitId` identifies the unit definition in catalog/database.
- Do not rely on display names as catalog identity.

Catalog decision:

- `CombatDamageService` reads catalog data through an interface, not directly
  through `DataMapper.Instance`.
- Unity can provide a `DataMapper`-backed implementation.
- Server code can provide a database-backed implementation.
- Do not build a production fake catalog layer.

Formula decisions:

- Base damage roll is inclusive: `MinDamage..MaxDamage`.
- Forecast returns real `minDamage`, real `maxDamage`, and deterministic
  `committedDamage`.
- Committed damage must be deterministic from snapshot/catalog/action inputs.
- Do not use global `UnityEngine.Random` state for committed damage.
- Final damage uses `Math.Ceiling`.
- Attack-vs-defense remains legacy-style: `+4%` per attack advantage point and
  `-1.4%` per defense advantage point.
- `DefensePenetration` remains `0..1`; `0.7` means ignore 70% of defender
  defense before attack-vs-defense scaling.
- `SpecialDMGModificator` is modeled as outgoing damage reduction; positive
  values reduce damage.
- `SpecialResistance` is modeled as `IncomingDamageReductionPercent`; positive
  values reduce incoming damage.
- `FlatDMGReduce` reduces by `FlatDMGReduce * attacker amount`, capped so it
  can reduce at most 70% of pre-flat-reduction damage. Example: `100` can go
  no lower than `30`.
- `HATED` must be represented as `HatedTargetUnitId`; matching target id gives
  the legacy +50% damage relationship.
- `SpecialPUREDMG` is a one-use attacker bonus. Forecast can show it, but only
  committed damage events consume it.

Event decisions:

- `DamageApplied` must carry whether actor pure damage was consumed, for
  example `ConsumesActorPureDamage`.
- The live applier clears `SpecialPUREDMG` only for that event actor and only
  when the event says so.
- Retaliation uses the same service/calculator as normal attack, with reversed
  actor/target and a distinct roll purpose such as `retaliation`.

Cleanup decision:

- After PRD055A and PRD055B, PRD055C must remove legacy damage traces.
- Old legacy damage methods should be deleted when all call sites are migrated.
- Do not leave compatibility wrappers or `[Obsolete]` legacy damage methods as
  soft migration paths.

## Goal

Resolve the current damage calculation split between `BattleActionRules` and
legacy `TosterHexUnit` before action preview becomes canonical.

The project needs one shared combat damage calculation path that can produce:

- a deterministic single rolled damage value for committed actions,
- a min/max damage range for forecasts,
- matching results for basic ranged attack, melee attack, move-and-attack, and
  retaliation.

PRD054A should depend on this work. The action preview model must not expose a
forecast based on a third formula or a known-incomplete `BattleActionRules`
formula.

## Problem Statement

`BattleActionRules` already validates tactical actions and emits
`BattleActionResult` events, including `DamageApplied` events for basic attack
and move-and-attack.

Current `BattleActionRules` damage for basic attack is simplified:

- uses `MinDamage..MaxDamage`,
- uses a deterministic seed,
- does not multiply by attacker stack amount,
- does not apply attack-vs-defense scaling,
- does not apply resistance or damage modifiers,
- does not apply flat damage reduction,
- does not account for pure damage or special legacy relationships.

Legacy `TosterHexUnit.CalculateDamageBetweenTosters(...)` uses richer combat
math:

- attacker stack amount,
- `Attack` vs `Defense`,
- `DefensePenetration`,
- `SpecialResistance`,
- `SpecialDMGModificator`,
- `FlatDMGReduce`,
- `SpecialPUREDMG`,
- `HATED` bonus,
- minimum final damage clamp.

This means current committed action results can diverge from legacy combat
expectations depending on the execution path. It also means PRD054A cannot
truthfully forecast combat outcomes until the damage authority is unified.

## Current Code Surfaces

- `Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
  - `Apply(...)`
  - `AddBasicAttackDamageEvent(...)`
  - `AddCounterattackDamageEvent(...)`
  - `ResolveBasicAttackDamage(...)`
- `Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
  - `BattleActionResult`
  - `BattleActionResultEvent`
  - `BattleActionResultEventType.DamageApplied`
- `Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
  - applies `DamageApplied` event amounts to live `TosterHexUnit` objects
    without recomputing damage.
- `Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
  - `CalculateDamageBetweenTosters(...)`
  - `ReCalculateDamageBetweenTosters(...)`
  - legacy attack/counterattack methods.
- `Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`
  - current basic ranged deterministic damage and move-and-attack retaliation
    event coverage.

## Scope

Do:

- Introduce or extract a shared combat damage calculator for basic combat
  damage.
- Support both single deterministic roll and min/max range calculation from the
  same math.
- Make `BattleActionRules` use the shared calculator for:
  - basic ranged attack,
  - melee/basic move-and-attack damage,
  - retaliation damage.
- Preserve current action validation responsibilities in `BattleActionRules`.
- Preserve current `BattleActionResult` event flow where possible.
- Include actor and defender snapshot stats, current statuses, and stat
  modifiers that map to the legacy damage formula.
- Document any legacy fields that cannot currently be represented in
  `BattleUnitSnapshot`.
- Avoid changing gameplay balance constants unless the implementation proves
  the change is only correcting the formula authority.
- Add focused EditMode tests for parity and range behavior.

Do not:

- Build action preview UI.
- Add PRD054 badge/panel DTOs.
- Edit prefabs, scenes, materials, controllers, `.inputactions`, `.asmdef`, or
  Unity asset files.
- Use tactical AI scoring as damage or value truth.
- Rewrite all skill execution.
- Broadly refactor `TosterHexUnit` or `MouseControler`.

## Required Design Decisions

The implementation must explicitly decide and document:

- Which parts of legacy `TosterHexUnit.CalculateDamageBetweenTosters(...)`
  are canonical for basic attack damage.
- How deterministic committed damage maps to the legacy random roll.
- Damage roll bounds are inclusive: `MinDamage..MaxDamage`.
- How `SpecialPUREDMG` is handled, because the legacy method consumes and
  clears it on the attacker.
- How `HATED` is represented outside live object references, if needed.
- Whether missing snapshot fields should block canonical parity now or be
  added to `BattleUnitSnapshot` in this task.

## Suggested Model Shape

Names may change during implementation, but the model should preserve these
concepts:

- `CombatDamageInput`: actor stats, defender stats, action kind/source,
  damage scale/modifier, roll seed/index/purpose when needed.
- `CombatDamageRange`: minimum damage, maximum damage, and flags for missing
  or approximate data.
- `CombatDamageRoll`: resolved single damage value plus the range it came from.
- `CombatDamageService`: snapshot/catalog resolver used by `BattleActionRules`,
  future PRD054A forecast generation, and later skill damage migration.
- `CombatDamageCalculator`: pure deterministic calculator called by
  `CombatDamageService`.

## Acceptance Criteria

Done when:

- `BattleActionRules` basic attack damage no longer uses the old simplified
  `MinDamage..MaxDamage` only path.
- Basic ranged attack, move-and-attack, and retaliation use the shared combat
  damage calculator.
- The same calculator can return both a committed single damage value and a
  forecast range.
- Stack amount, attack-vs-defense scaling, resistance, damage modifier, flat
  damage reduction, and pure damage handling are implemented for PRD055A basic
  combat. Missing required data rejects the action/result instead of falling
  back.
- Current retaliation event behavior is preserved, but retaliation damage uses
  the shared calculator.
- Tests cover:
  - basic attack damage includes stack amount,
  - attack-vs-defense affects damage,
  - defender resistance affects damage,
  - flat damage reduction affects damage and respects the 70% maximum
    reduction rule,
  - move-and-attack includes retaliation damage from the same calculator,
  - range min/max and committed roll use the same formula,
  - missing required data rejects deterministically rather than being silently
    ignored.
- PRD054A can consume the calculator for forecast ranges without duplicating
  combat math.

## Testing Decisions

Use EditMode tests with pure `BattleSnapshot` / `BattleUnitSnapshot` fixtures.
Mirror helper style from:

- `Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`
- `Assets/Scripts/Tests/EditMode/SkillRulesTests.cs`
- `Assets/Scripts/Tests/EditMode/BattleSnapshotBuilderTests.cs`

Manual Unity validation is secondary for this PRD. The main proof should be
automated parity and range tests.

## Out Of Scope

- Action preview panel and badge UI.
- Economic net value calculation.
- Kill range aggregation.
- PRD055B skill damage migration implementation.
- PRD055C legacy damage trace cleanup.
- AI scoring changes.

## Notes For Coding Agent

This PRD is a prerequisite for PRD054A. Do not start by building preview DTOs
around the current simplified `BattleActionRules` damage.

Preferred sequence:

1. Add characterization tests that expose the current split.
2. Add `CatalogUnitId` and required combat modifier state to battle snapshots.
3. Introduce catalog-interface-backed `CombatDamageService`.
4. Extract deterministic damage math into a pure calculator.
5. Route `BattleActionRules` damage event generation through the service.
6. Add forecast min/max/committed output from the same math.
7. Leave PRD054A UI/forecast aggregation to a later task after this service is
   stable.
