# [TARENA] PRD055C: Legacy Damage Trace Cleanup

- Status: closed
- Type: PRD / cleanup / QA follow-up
- Area: Combat Calculation, Legacy Cleanup
- Label: closed
- Created: 2026-06-30
- Parent: `_codex/tasks/055_PRD_CombatDamageParity_CanonicalCalculator.md`
- Depends on: `_codex/tasks/archive/055A_PRD_CoreCombatDamageService_BasicCombat.md`
- Depends on: `_codex/tasks/archive/055B_PRD_SkillDamageMigration_CombatDamageService.md`

## Goal

Remove legacy damage traces after combat and skill damage have migrated to
`CombatDamageService`.

This is not a compatibility task. It is a cleanup and verification task to make
sure old damage paths cannot silently reappear.

## Hard Rules

- No workaround damage paths.
- No fallback to legacy damage.
- No compatibility wrappers around old damage methods.
- No `[Obsolete]` legacy damage methods left as a soft migration path.
- No hidden `damage = 0` fallback.
- If old damage code is still needed, the task must identify the unmigrated
  dependency instead of hiding it.

## Audit Targets

Search for and resolve traces such as:

- `CalculateDamageBetweenTosters`,
- `ReCalculateDamageBetweenTosters`,
- manual `Random.Range` damage rolls,
- manual attack-vs-defense damage scaling,
- manual `SpecialResistance` or incoming damage reduction math,
- manual `SpecialDMGModificator` or outgoing damage reduction math,
- manual `FlatDMGReduce` damage math,
- manual `SpecialPUREDMG` damage consumption,
- manual `HATED` damage bonus,
- duplicated skill damage math that should use `CombatDamageService`.

## Scope

Do:

- Audit code for legacy damage paths.
- Remove old methods when all call sites are migrated.
- Remove transitional comments or wrappers that invite legacy reuse.
- Update tests that still assert legacy-only behavior.
- Add regression tests or compile-time checks where practical to keep damage
  centralized.

Do not:

- Edit Unity assets, prefabs, scenes, materials, controllers,
  `.inputactions`, `.asmdef`, or `.asmref` files.
- Delete unrelated legacy code outside damage calculation.
- Rebalance damage values beyond PRD055 decisions.

## Acceptance Criteria

Done when:

- No production code path calculates combat-style damage outside
  `CombatDamageService` / `CombatDamageCalculator`.
- Old legacy damage methods are removed if no legal call sites remain.
- Any remaining non-service damage code is explicitly documented as
  non-combat-style damage and not a fallback path.
- No direct global random damage roll remains for committed combat-style
  damage.
- Tests prove basic combat and migrated skill damage still pass after cleanup.
- A concise cleanup report lists removed traces and any intentionally retained
  non-combat-style damage code.

## Implementation - 2026-06-30

### What Changed

- `LiveCombatDamageResolver`: added a live Unity-object adapter that builds the
  current battle snapshot, resolves `TosterHexUnit` objects to runtime unit ids,
  and calls `CombatDamageService.Default`. No Inspector fields changed.
- `TosterHexUnit`: removed `CalculateDamageBetweenTosters*`,
  `ReCalculateDamageBetweenTosters*`, and the empty `CalculateResult(...)`.
  Existing live attack/skill damage entry points now request committed damage
  from `CombatDamageService`; pure damage clears only when the service result
  says it was consumed. No Inspector fields changed.
- `BattleActionAutomaticResultApplier`: `Fire_Trap` and `Spike_Trap` DOT setup
  now uses canonical combat damage; failures log a clear error instead of
  adding hidden zero-damage status events. No Inspector fields changed.
- `MostStupidAIEver`: old disabled fallback AI heuristics no longer call legacy
  damage methods; they use canonical committed damage prediction. No Inspector
  fields changed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/LegacyDamageTraceCleanupTests.cs`.
- The tests scan production `Assets/Scripts/Lesisz` scripts for deleted legacy
  damage method names and enforce that `Random.Range` remains only in documented
  non-committed-damage files.
- Tests were not run automatically. Run them manually in Unity Test Runner:
  `Window > General > Test Runner > EditMode`, then run
  `LegacyDamageTraceCleanupTests`, `CombatDamageServiceTests`,
  `BattleActionRulesTests`, and `BattleActionAutomaticResultApplierTests`.
  Expected result: all pass.

### Unity Test

#### Unity Setup

- No new scene objects, components, prefabs, assets, or Inspector assignments
  are required.
- Use an existing battle scene with the normal `DataMapper`, unit catalog, skill
  catalog, `HexMap`, `MouseControler`, and `BattleActionLifecycle` setup.

#### Play Mode Test

- Start a battle and perform a basic melee attack, basic ranged attack, and
  move-and-attack with retaliation; expected: damage applies once per event and
  no legacy damage error appears.
- Cast a combat-style skill that still reaches old `CastManager` presentation
  helpers; expected: damage resolves through canonical service and presentation
  still plays.
- Trigger `Fire_Trap` and `Spike_Trap`; expected: DOT/status events are created
  when actor/target catalog data is valid.
- Validate a pure-damage bonus hit; expected: pure damage is consumed only on a
  committed hit.

### QA Verdict

- Final QA verdict: pass.
- QA report: `_codex/tasks/QA/2026-06-30_1800_055C_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observation: legacy live entry points now depend on current-scene
  snapshot readiness, so Play Mode validation should include old `CastManager`
  and trap paths.
- Follow-up fixes after QA: none required.

### Notes

- Remaining `Random.Range` occurrences are intentionally retained for battle
  seed generation, SFX selection, and material selection; they are not committed
  combat damage rolls.
- `DealMeDMG...` method names remain as legacy application/presentation entry
  points, but no longer contain damage formula logic.
- Unity compile and tests were not run by the agent.

### Next Steps

- Run the EditMode tests listed above in Unity Test Runner.
- Run the Play Mode checks for basic combat, ranged combat, retaliation,
  legacy-presented combat-style skills, traps, and pure damage consumption.
