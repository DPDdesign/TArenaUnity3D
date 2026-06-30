# [TARENA] PRD055A: Core Combat Damage Service For Basic Combat

- Status: completed
- Type: PRD / coding task
- Area: Combat Calculation, Battle Action API
- Label: completed
- Created: 2026-06-30
- Parent: `_codex/tasks/055_PRD_CombatDamageParity_CanonicalCalculator.md`
- Blocks: `_codex/tasks/054A_PRD_ActionPreview_CombatForecastModel.md`
- Related: `_codex/Context/maps/battle-action-api-map.md`
- Related: `_codex/agents/docs/codebase/battle-action-code-map.md`

## Goal

Introduce `CombatDamageService` and a pure `CombatDamageCalculator`, then route
basic combat damage through them.

This task covers:

- basic ranged attack,
- melee/basic move-and-attack damage,
- retaliation damage,
- deterministic committed damage,
- real forecast min/max/committed damage,
- fail-fast catalog/snapshot error handling.

## Required Model

Add or adapt model types for these concepts:

- `CombatDamageService`
  - resolves snapshot units,
  - resolves catalog data through an injected interface,
  - builds calculator input,
  - returns a result or a clear error.
- `CombatDamageCalculator`
  - pure deterministic math only.
- `ICombatUnitCatalog` or equivalent
  - resolves `CatalogUnitId` to base stats.
- `CombatDamageInput`
  - complete attacker and defender stats/state,
  - roll seed/index/purpose,
  - damage scale if needed.
- `CombatDamageForecast`
  - `MinDamage`,
  - `MaxDamage`,
  - `CommittedDamage`.
- `CombatDamageResult`
  - committed amount,
  - forecast,
  - whether actor pure damage was consumed.

Names may change, but the responsibility split must remain.

## Snapshot Requirements

`BattleUnitSnapshot` must include:

- `RuntimeUnitId`,
- `CatalogUnitId`,
- `Amount`,
- combat status/modifier state needed by the service,
- outgoing damage reduction mapped from legacy `SpecialDMGModificator`,
- incoming damage reduction mapped from legacy `SpecialResistance`,
- flat damage reduction mapped from `FlatDMGReduce`,
- pure damage mapped from `SpecialPUREDMG`,
- `DefensePenetration`,
- `HatedTargetUnitId`.

`RuntimeUnitId` identifies the stack in this battle. `CatalogUnitId` identifies
the unit definition in the catalog/database.

## Formula

Use the umbrella decisions from PRD055:

- inclusive base roll: `MinDamage..MaxDamage`,
- deterministic local roll,
- final `Math.Ceiling`,
- attack-vs-defense `+4%` / `-1.4%`,
- `DefensePenetration` as `0..1`,
- outgoing damage reduction,
- incoming damage reduction,
- `FlatDMGReduce` capped to at most 70% total reduction from pre-flat damage,
- `HATED` +50% when target ids match,
- pure damage consumed only on committed damage events.

## Battle Action Integration

`BattleActionRules` must keep action validation ownership.

Replace simplified damage generation in:

- `AddBasicAttackDamageEvent(...)`,
- `AddCounterattackDamageEvent(...)`,
- `ResolveBasicAttackDamage(...)` or its replacement.

The new flow should:

1. validate the action as today,
2. ask `CombatDamageService` for damage,
3. reject the action/result if the service returns a deterministic data error,
4. emit `DamageApplied` with the service result amount,
5. include `ConsumesActorPureDamage` or equivalent on the event when needed.

Retaliation uses the same service and calculator with a distinct roll purpose.

## Failure Rules

No fallbacks.

If any required data is missing:

- missing actor snapshot,
- missing target snapshot,
- missing catalog unit,
- invalid catalog damage data,
- unsupported deterministic input,

then the action must be rejected with a clear reason and log.

Do not emit `0 damage` as a fallback.

## Acceptance Criteria

Done when:

- `BattleActionRules` no longer uses simplified `MinDamage..MaxDamage` only
  damage for basic combat.
- Basic ranged attack, move-and-attack, and retaliation use
  `CombatDamageService`.
- The same calculator returns real min damage, max damage, and deterministic
  committed damage.
- Committed damage is reproducible from snapshot/catalog/action inputs.
- `RuntimeUnitId` and `CatalogUnitId` are available on battle unit snapshots.
- Missing catalog/snapshot data rejects the action with a clear reason.
- Pure damage is consumed through explicit damage event data.
- No Unity global random state is used for committed damage.
- PRD054A can consume the forecast output without duplicating combat math.

## Tests

Add focused EditMode tests for:

- basic attack includes stack amount,
- inclusive max damage roll behavior,
- deterministic committed damage is stable for the same inputs,
- different roll purposes separate attack and retaliation rolls,
- attack-vs-defense scaling,
- defense penetration,
- outgoing damage reduction,
- incoming damage reduction,
- flat damage reduction with 70% maximum reduction,
- hated target +50% damage,
- pure damage contributes and is marked consumed,
- forecast min/max/committed use the same formula,
- missing catalog unit rejects the action,
- move-and-attack includes retaliation from the same service.

## Out Of Scope

- Migrating skill damage. That is PRD055B.
- Cleaning all legacy damage traces. That is PRD055C.
- Building action preview UI.
- Editing Unity assets, prefabs, scenes, materials, controllers,
  `.inputactions`, `.asmdef`, or `.asmref` files.

## Implementation - 2026-06-30

### What Changed

- No Inspector fields changed.
- Added `CombatDamageModels.cs`: `CombatDamageRequest`, `CombatDamageInput`,
  `CombatDamageForecast`, `CombatDamageResult`, `ICombatUnitCatalog`, and pure
  `CombatDamageCalculator`. Request/input fields cover actor/target ids,
  action index/seed, roll purpose, damage scale, stackability, pure-damage
  consumption, attack/defense/damage stats, reductions, penetration, and hated
  target. Use non-negative damage/amount values, `DefensePenetration` in `0..1`,
  and percent reductions where higher values reduce damage more.
- Added `CombatDamageService.cs`: resolves snapshots and catalog entries,
  builds calculator input from catalog base stats plus snapshot modifiers, and
  returns explicit errors for missing/invalid data. `DamageScale` defaults to
  `1.0`; lower values reduce future skill damage, higher values amplify it.
- Updated `BattleUnitSnapshot`: added `CatalogUnitId`, stat modifier fields,
  outgoing/incoming damage reduction, flat reduction, pure damage, defense
  penetration, and hated target id. Higher reduction fields lower combat damage;
  higher `PureDamage` adds one-use bonus damage; tuning hint: keep penetration
  clamped to authored `0..1` values.
- Updated `BattleActionResultEvent`: added `ConsumesActorPureDamage`, a bool
  consumed by live/simulated appliers. `true` clears actor pure damage after a
  committed damage event; `false` leaves it untouched for forecasts/non-damage.
- Updated `BattleActionRules`: basic ranged, melee/move-and-attack, and
  retaliation damage now use `CombatDamageService`; missing data rejects the
  result instead of emitting fallback `0` damage.
- Updated snapshot/live/simulation paths: live snapshots populate the new
  fields, snapshot clone/hash preserves them, AI simulation carries them through
  clone/spawn, and live/simulated application clears pure damage only from the
  event actor when the event says so.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/CombatDamageServiceTests.cs`.
  It covers stack amount, inclusive max roll, deterministic roll stability,
  separate roll purposes, attack-vs-defense scaling, defense penetration,
  outgoing/incoming reductions, flat reduction 70% cap, hated +50%, pure damage
  consumption, forecast min/max/committed parity, missing catalog rejection, and
  move-and-attack retaliation through the same service.
- Updated `BattleActionRulesTests.cs` to use an injected test combat catalog for
  basic damage assertions.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, then run
  `CombatDamageServiceTests` and `BattleActionRulesTests`. Expected result: all
  tests green. I did not run Unity tests automatically.

### Unity Test

#### Unity Setup

- No new components or Inspector assignments are required.
- Ensure the battle scene uses the existing `DataMapper`/unit catalog and that
  live `TosterHexUnit.Name` values match catalog unit names.
- Use existing units with basic ranged and melee/move-and-attack actions; no
  prefab or scene asset edits are needed for this task.

#### Play Mode Test

- Start a battle and perform a basic ranged attack. Expected: damage is no
  longer just raw `MinDamage..MaxDamage`; stack amount, attack/defense, and
  reductions influence the amount.
- Perform melee move-and-attack against a target with counterattacks. Expected:
  outgoing damage and retaliation both resolve, and retaliation uses a separate
  deterministic roll purpose.
- Trigger a one-use pure damage state, then perform a committed basic damage
  event. Expected: pure damage contributes once and is cleared only from the
  damage actor.
- Temporarily break catalog identity in a test scene only if needed. Expected:
  the action result rejects with a clear reason instead of applying `0` damage.

### QA Verdict

- QA verdict: Pass, no follow-up required before tests.
- QA report: `_codex/tasks/QA/2026-06-30_1647_PRD055A_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Follow-up update: Tactical AI damage prediction now uses deterministic
  `CombatDamageService` committed damage via snapshot seed, action index, and
  action seed. Generated Tactical AI actions carry snapshot action index/seed,
  live snapshots read seed/index from `BattleActionLifecycle`, and simulated
  snapshots preserve seed while advancing action index after applied actions.
- Non-blocking observations: skill/passive legacy damage calls remain for
  PRD055B/PRD055C.

### Notes

- PRD055B can reuse `CombatDamageService.CalculateDamage(...)` with
  `CombatDamageRequest` for skill damage migration.
- PRD055C still needs to remove legacy `TosterHexUnit.CalculateDamageBetweenTosters(...)`
  traces after skill migration.
- Tactical AI exposes `TacticalAIDamagePredictor.PredictCommittedDamage(...)`
  and `TryPredictCommittedDamage(...)` for seed-aware PRD055B integration.
- I did not run Unity compile, Play Mode, EditMode tests, `dotnet`, or external
  build tooling.

### Next Steps

- Run the EditMode tests listed above in Unity Test Runner.
- Run the Play Mode checks for ranged attack, move-and-attack retaliation, and
  pure damage consumption.
- Continue with PRD055B for skill damage migration after PRD055A validates in
  Unity.
