# [TARENA] PRD055B: Skill Damage Migration To Combat Damage Service

- Status: closed
- Type: PRD / coding task
- Area: Skill Damage, Combat Calculation
- Label: closed
- Created: 2026-06-30
- Parent: `_codex/tasks/055_PRD_CombatDamageParity_CanonicalCalculator.md`
- Depends on: `_codex/tasks/archive/055A_PRD_CoreCombatDamageService_BasicCombat.md`
- Related: `_codex/agents/docs/codebase/skills-effects-code-map.md`

## Goal

Migrate skill damage that uses combat-style unit-vs-unit damage math to
`CombatDamageService`.

After this task, skill damage that depends on attacker stats, defender stats,
stack amount, damage modifiers, resistance, flat reduction, pure damage,
defense penetration, or hated relationships must use the same deterministic
damage authority as basic combat.

## Scope

Do:

- Identify current skill damage paths that call or duplicate
  `CalculateDamageBetweenTosters(...)`,
  `ReCalculateDamageBetweenTosters(...)`, or equivalent combat damage math.
- Route those paths through `CombatDamageService`.
- Preserve skill-specific targeting, legality, presentation, cooldown, and
  animation responsibilities outside the damage service.
- Support skill damage scale/modifier inputs where a skill intentionally deals
  a fraction or multiplier of combat damage.
- Preserve deterministic committed damage from snapshot/catalog/action/skill
  inputs.
- Preserve no-fallback behavior from PRD055.

Do not:

- Rewrite all skill execution architecture.
- Rebuild `CastManager` broadly.
- Change skill VFX/SFX or animation sequencing unless required for damage event
  correctness.
- Edit Unity assets, prefabs, scenes, materials, controllers,
  `.inputactions`, `.asmdef`, or `.asmref` files.

## Required Behavior

Skill damage must not use:

- global `UnityEngine.Random` state for committed damage,
- legacy live `TosterHexUnit` damage methods,
- duplicated attack-vs-defense math,
- fallback damage if catalog/snapshot data is missing.

If skill damage cannot be calculated deterministically, the skill result must
fail with a clear error and log rather than falling back to legacy damage.

## Acceptance Criteria

Done when:

- All skill damage paths in scope use `CombatDamageService`.
- No migrated skill damage path recomputes formula pieces manually.
- Skill damage can pass a clear skill roll purpose so rolls do not collide with
  basic attack or retaliation.
- Skill damage supports deterministic replay/server/reconnect behavior.
- Missing catalog/snapshot data fails clearly.
- Tests cover at least:
  - one direct combat-style skill damage path,
  - one scaled combat-style skill damage path, if such a path exists,
  - deterministic repeated skill damage from the same inputs,
  - no fallback to legacy damage when required data is missing.

## Notes

This task may expose skills whose damage rules are not combat-style. Those
skills should be documented as out of scope for the combat damage calculator
instead of forcing them into the wrong model.

## Implementation - 2026-06-30

### What Changed

- `BattleActionRules`: combat-style skill `DamageApplied` events now resolve
  committed damage through `CombatDamageService` for
  `BasicAttackDamage` and `RangedBasicAttackDamage`. Roll purpose is distinct
  per skill/effect/target.
- `TacticalAISkillRulesExecutor`: removed live fallback damage calculation;
  skill damage application now uses the committed result event amount only.
- `TacticalAISearchScoring`: snapshot AI planning/simulation now injects a
  snapshot-backed combat catalog explicitly.
- Empty combat-style skill hit areas now commit without damage events instead
  of rejecting the skill result. This preserves `Slash` behavior on empty
  impact hexes.
- Empty target lists for target-based skill status events now emit no event
  instead of creating an empty target id. This preserves `Toxic_Fume` self
  status on empty areas without applying `Taunt` to the caster.
- No Inspector fields changed. No serialized/public gameplay tuning fields were
  added, removed, or renamed.

### Automatic Test

- Added focused EditMode coverage in
  `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`.
- Tests cover direct combat-style skill damage, scaled skill damage,
  deterministic repeated skill damage, and missing catalog rejection without a
  damage fallback.
- Added a regression test for `Slash` committing movement into an empty impact
  area without emitting damage.
- Added a regression test for `Toxic_Fume` committing movement and self-status
  into an empty affected area without emitting an empty-target `Taunt`.
- Tests were not run automatically. Run them manually in Unity Test Runner:
  `Window > General > Test Runner > EditMode`, then run
  `BattleActionRulesTests`. Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No scene, prefab, Inspector assignment, or asset setup is required for the
  new automated tests.
- For Play Mode validation, use an existing battle scene with DataMapper unit
  and skill catalogs loaded normally.

#### Play Mode Test

- Start a battle with a unit that has a combat-style damage skill such as
  `Slash`, `Rush`, `Double_Throw`, `Heavy_Fists`, `Chope`, `Axe_Rain`, or
  `Fire_Ball`.
- Cast the skill on a legal enemy target and verify damage is applied once,
  cooldown/turn cost still apply, and no skill damage fallback behavior appears.
- Try an AI turn where it evaluates or uses a combat-style damage skill and
  verify the action is not rejected when snapshot catalog ids are valid.

### QA Verdict

- Final QA verdict: pass.
- Final report:
  `_codex/tasks/QA/2026-06-30_055B_QA_ArchitectureReview_Final.md`.
- Initial QA found one actionable issue: snapshot AI simulation needed an
  explicit combat catalog instead of the default live DataMapper service.
- Follow-up fix was applied in `TacticalAISearchScoring.cs`; no QA-blocking
  findings remain.

### Notes

- `FixedDamageThroughDefense`, `PureDamage`, `DamageOverTime`, and
  `PercentOfDamageTaken` were not forced into `CombatDamageService`.
- Existing legacy damage calls in traps, old AI, and `TosterHexUnit` remain for
  PRD055C or separate legacy cleanup.
- Unity compile and tests were not run by the agent.

### Next Steps

- Run `BattleActionRulesTests` in Unity EditMode Test Runner.
- In Play Mode, manually validate at least one direct and one scaled
  combat-style skill.
- Specifically validate Axeman `Slash` on an empty impact area: movement and
  skill presentation should commit, with no damage event required.
- Specifically validate `Toxic_Fume` on an empty affected area: movement and
  caster self-status should commit, with no affected-unit `Taunt` event.
- Continue PRD055C after Unity validation to remove remaining legacy damage
  traces.

## Closure - 2026-06-30

- Closed after final QA architecture verdict passed and follow-up snapshot
  catalog issue was resolved.
- Post-QA user-reported empty-target regressions were addressed for
  combat-style damage and target-based status events.
- Final agent sweep found no remaining empty-target event generation for
  migrated damage/status skill effects.
- Unity compile and tests remain manual verification items.
