# [TARENA] 052 Combat Actions And Skills Full Audit Hardening

- Status: draft
- Type: implementation task
- Area: tactical battle, skills, basic actions, Battle Action API, validator, AI
- Owner: Coding Agent
- Source prompt: `C:\Users\piotr\.codex\attachments\8afd851d-3ae2-46aa-a581-22353477b204\pasted-text.txt`
- Depends on: `_codex/tasks/051_CombatAPIValidatorAI_AuditHardening.md`

## Goal

After task 051 is complete, perform a concrete per-action and per-skill audit
and hardening pass for all current combat actions:

- player skills,
- enemy skills,
- basic melee/ranged attack,
- move and move-and-attack,
- rush/charge/leap-like movement actions,
- projectile/throw-like skills,
- AoE skills,
- buffs/debuffs/status effects,
- passive/triggered effects that are part of combat execution.

Keep the original prompt goal: verify every skill and basic action uses the
current Combat/Battle Action API and validator boundary correctly, fix obvious
integration bugs, remove safe legacy bypasses, and prepare the code for the
project owner's manual gameplay test phase in Unity.

Do not change skill design, combat design, balance, gameplay values, or AI
personality.

## Required Previous Result

Start only after `_codex/tasks/051_CombatAPIValidatorAI_AuditHardening.md` has
produced its completion report.

Use that report as the current truth for:

- the actual request -> validation -> execution -> result flow,
- known validator gaps,
- known AI bypasses or safe legacy adapters,
- battle readiness guards,
- remaining `TODO_LEGACY_REVIEW` markers.

Do not redo 051 from scratch unless its report is missing or contradicted by
code inspection.

## Current Project State To Assume

Current active skill identity is SO-backed:

- `SkillDefinitionAsset.skillName` is the canonical skill id.
- `SkillCatalog.asset` and `Resources/0_Data/Skills/*.asset` are current skill
  definition data.
- `UnitCatalog.asset` and `Resources/0_Data/Units/*.asset` decide which skills
  units legally own.
- Runtime unit skill ids are loaded into `TosterHexUnit.skillstrings`.
- `skills.xml` is legacy/migration history unless current code proves a runtime
  dependency.

Current validator/API state:

- `BattleActionUse`, `BattleAction`, `BattleActionResult`, and
  `BattleActionRules` exist for the broader PRD050 action API.
- `SkillUse`, `SkillContext`, `SkillRules`, `SkillCast`, `SkillTarget`,
  `SkillResult`, and `SkillQuery` still exist as the PRD049 skill API and are
  currently used by player skill targeting and AI skill planning/execution.
- `BattleActionRules.ValidateSkill(...)` delegates to `SkillRules`.
- `BattleActionRules.Apply(...)` converts `SkillResult` into
  `BattleActionResult` events.

Current legacy debt to watch:

- `CastManager` remains in the project and still has compatibility execution
  paths.
- `MouseControler` remains player input/UI adapter and still exposes
  `TryStartMoveAction`, `TryStartMoveAndAttackAction`,
  `TryStartBasicRangedAttackAction`, `TryStartWaitAction`,
  `TryStartDefenseAction`, and `TryStartSkillAction`.
- `TacticalAIActionIntent`, `TacticalAICandidateGenerator`,
  `TacticalAISearchCandidateExpander`, and `TacticalAIIntentRevalidator` remain
  for non-skill AI compatibility unless task 051 removed or rerouted them.
- Passive/trigger logic can still exist in `TosterHexUnit`, `SpellOverTime`,
  `HexClass`, and related hooks.

## Required Sources

Read these before changing code:

- the completion report from `_codex/tasks/051_CombatAPIValidatorAI_AuditHardening.md`
- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- `_codex/agents/docs/codebase-map.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/Context/BattleActionRules.md`
- `_codex/Context/AI_Context.md`
- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Documentation/CurrentSkills.md`
- `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`
- `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/archive/049ABC_PRD_SkillAPIAndFullMigration.md`
- `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
- `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`

Do not use another project's context.

## Required Code And Data Inspection

Do not scan the whole `Assets` folder. Focus on `Assets/Scripts` and directly
related battle/skill data.

Core code:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TurnManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillUse.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillContext.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/SkillCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/DataMapper.cs`

Directly related data:

- `TArenaUnity3D/Assets/Resources/0_Data/SkillCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Units/*.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/skills.xml` only as legacy reference
  if current code still references it.

Tests:

- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillRulesTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAICandidateGeneratorTests.cs`
- any new/updated tests created by task 051.

## Active Skill Inventory To Cover

Use `_codex/Context/09_CurrentSkills.md` and current unit/skill catalog data to
verify this active set. Do not skip a current active asset-backed or
unit-assigned skill because it already has SO data.

Current expected active skills by unit:

| Unit | Skills |
| --- | --- |
| `Axeman` | `Slash`, `Hate`, `Cold_Blood` |
| `FireElemental` | `Fire_Movement`, `Fire_Ball`, `Fire_Skin` |
| `FleshGolem` | `Heavy_Fists`, `Terrifying_Presence`, `Rotting` |
| `Healer` | `Tough_Skin`, `Defence_Ritual` |
| `HeavyHitter` | `Insult`, `Rage`, `Massochism` |
| `Rusher` | `Chope`, `Rush` |
| `Specialist` | `Force_Pull`, `Stone_Stance` |
| `StoneGolem` | `Stone_Throw`, `Stone_Skin` |
| `Tank` | `Toxic_Fume`, `Shapeshift`, `Long_Lick` |
| `Thrower` | `Range_Stance_Barb`, `Double_Throw`, `Axe_Rain` |
| `Trapper` | `Range_Stance_Lizard`, `Spike_Trap`, `Rope_Trap` |
| `Wisp` | `Blind_by_light`, `Unstoppable_Light` |

Reference-audit-only skills from PRD049ABC:

- `Cleanse`
- `Brak_Weny`

Migrate/fix reference-only skills only if current code or data proves a live
runtime reference.

## Required Inventory Output

Create an audit table in the completion report with one row per skill/action:

- skill/action name,
- script/class or asset source,
- user: player / enemy / both / automatic,
- action family: move / attack / skill / stance / passive / trap / automatic,
- current entry path,
- current validator path (`BattleActionRules`, `SkillRules`, legacy adapter, or
  missing),
- current execution path (`BattleActionResult`, `SkillResult` runtime adapter,
  `MouseControler.TryStart*`, `CastManager`, passive hook, etc.),
- legacy code present: yes/no,
- status: OK / fixed / risky / needs manual gameplay test,
- notes.

Basic actions that must appear in the table:

- `Move`
- `MoveAndAttack`
- `BasicMeleeAttack`
- `BasicRangedAttack`
- `Wait`
- `Defend`

## Per-Skill / Per-Action Audit Checklist

For every skill and basic action, check:

Validation:

- actor exists,
- actor is alive/actionable,
- actor is the active unit or automatic trigger context is valid,
- battle/lifecycle is ready and not blocking,
- target exists where required,
- target is alive/legal,
- target team/friendly-fire rule is correct,
- destination exists,
- destination is walkable and unoccupied when required,
- range/path/line/position rule is checked,
- cooldown/cost/turn/action-state rule is checked,
- passive skills cannot be directly cast,
- stance skills remain repeatable/free where intended,
- no action can execute from stale legacy state.

Execution:

- execution consumes a validated `BattleAction` / `SkillCast` or documented
  safe legacy adapter,
- execution does not choose a different target than the validator accepted,
- execution does not use UI hover/click state as gameplay truth,
- execution does not calculate gameplay damage/status from VFX, SFX, animation
  events, or presentation timing,
- result events or equivalent runtime effects match the validated action,
- AI and player paths do not diverge without a documented reason.

Integration:

- AI can use the action only through shared validation or documented current
  adapter revalidation,
- player can use the action only through shared validation or documented current
  adapter validation,
- preview/scoring/result data comes from the same rule/effect model when
  available,
- presentation is response to result, not the source of truth.

Cleanup:

- remove safe dead legacy paths only after a reference audit,
- do not keep two parallel runtime authorities for the same migrated path if a
  narrow removal is safe,
- if uncertain, mark `TODO_LEGACY_REVIEW` and report the exact reason.

## Known Risk Cases To Prioritize

Double Throw:

- both target selections must be validated,
- duplicate target remains legal only if current rule data says so,
- both hits must be represented by validated result/preview data,
- VFX/animation must not be the gameplay damage trigger,
- missing VFX must not imply missing gameplay execution.

Rusher / Rush / Charge:

- AI cannot use Rush before battle readiness is valid,
- Rush cannot execute illegally at battle start,
- move-to-target and attack-in-place must be distinct in validation/result data,
- AI must not choose Chop in place when it should first move toward the player,
- AI may choose Rush only when the validator accepts it.

Basic Attack / Chop:

- target must be legal and alive,
- melee adjacency/range must be checked,
- AI cannot attack out of range,
- player and enemy should use the same validation boundary or a documented
  compatibility adapter.

Move:

- destination must be legal, walkable, and unoccupied,
- path/movement budget must be checked,
- move cannot execute before battle readiness,
- AI and player should use the same API or a documented compatibility adapter.

Stone Throw:

- enemy-unit target contract must remain from PRD049ABC,
- stack split/spawn/self-damage/target-damage parity is high risk,
- inspect `TacticalAISkillRulesExecutor` and `SkillRules` event handling.

Long Lick:

- current contract is enemy target plus selected adjacent empty destination,
  not legacy first-empty auto-destination.

Traps:

- `Spike_Trap` and `Rope_Trap` must reject occupied hexes and existing traps,
  then automatic trigger logic must not bypass the action/result model without
  being reported.

Passives:

- `Cold_Blood`, `Massochism`, `Stone_Skin`, `Fire_Movement`, `Fire_Skin`,
  `Terrifying_Presence`, `Rotting`, and `Unstoppable_Light` may still depend on
  `TosterHexUnit`, `SpellOverTime`, or `HexClass`.
- Do not redesign passive timing. Verify and report whether each passive has a
  shared-action representation or remains legacy trigger debt.

## Fixing Rules

- Keep fixes small and local to the action/skill integration defect.
- If a current action already has a `BattleActionRules` path, prefer routing
  through it instead of adding new validation logic elsewhere.
- If the issue is skill-only, prefer `SkillRules` / `SkillDefinitionAsset` rule
  data over `CastManager` or UI state.
- Do not change gameplay numeric values.
- Do not rename skill ids, public fields, or serialized fields.
- Do not edit assets unless the specific fix requires skill definition data and
  project rules permit it. If asset edits are needed but not safe in this task,
  write an asset migration checklist instead.
- Do not remove large legacy systems without proof of no runtime references.

## Explicit Non-Goals

- No skill rebalance.
- No new skills.
- No skill redesign.
- No AI personality/scoring redesign.
- No gameplay feel polish.
- No manual gameplay judgment about whether a skill is fun, weak, strong, or
  readable.
- No server, networking, multiplayer, rollback, replay, or persistence work.
- No scene, prefab, material, Animator Controller, `.inputactions`,
  generated Unity file, `.asmdef`, or `.asmref` edits.
- No full PRD050 purge beyond narrow safe cleanup discovered by this audit.

## Testing

If existing tests cover the touched area, update/add focused EditMode tests.
Prefer existing test files over creating a broad new test framework.

Recommended tests where feasible:

- legal move,
- illegal move,
- legal basic attack,
- illegal basic attack out of range,
- legal representative skill,
- illegal skill target,
- AI illegal action rejected before live execution,
- Double Throw validates two selected targets,
- Rush rejects illegal endpoint/position,
- Stone Throw validates enemy-unit target,
- trap placement rejects occupied hex.

If a pure automated test is not practical, add a dev-only validation checklist
or completion-report manual checklist. Do not add noisy runtime logs. Any new
log must help identify illegal action, skipped validation, or AI before battle
readiness, and must not spam normal play.

Unity compilation and Unity Test Runner execution remain manual unless the user
explicitly authorizes a Unity command.

## Completion Report Required

Write a completion protocol under `_codex/tasks/QA/` titled
`SKILL AUDIT REPORT` and include:

1. Inventory table for every checked action/skill with:
   - name,
   - script/class or asset source,
   - user,
   - status,
   - notes.
2. Fixed problems:
   - what was wrong,
   - where,
   - how it was fixed.
3. Removed legacy:
   - files/methods/paths,
   - why removal was safe.
4. Legacy left for review:
   - every `TODO_LEGACY_REVIEW`,
   - why it was not removed.
5. Risks:
   - which skills still need special manual testing,
   - which paths still depend on animation/VFX/SFX/UI or legacy hooks.
6. Manual gameplay checklist for the project owner:
   - legal target checks,
   - out-of-range rejection checks,
   - AI use checks,
   - Double Throw VFX/gameplay parity,
   - Rusher/Rush opening checks,
   - Stone Throw stack split/spawn checks,
   - trap placement and trigger checks.
7. Tests added/updated and tests not run.
8. Things deliberately not changed.

## Acceptance Criteria

- Every active skill and basic action is inventoried.
- Every active skill and basic action has a documented validator path.
- Obvious validation bypasses are fixed or marked `TODO_LEGACY_REVIEW` with a
  concrete reason.
- AI cannot execute a skill/action only because it bypassed the validator.
- Player and enemy paths use the same validation boundary where current
  architecture supports it.
- Legacy is removed when safe, otherwise explicitly documented.
- Gameplay numeric values are unchanged unless a refactor miswire fix required
  a documented correction.
- Code and report are ready for the project owner's manual Unity gameplay test
  phase.

## Implementation - 2026-06-25

### What Changed

- `TacticalAIIntentRevalidator`: non-skill legacy intent revalidation now converts move, move-and-attack, basic ranged attack, wait, and defend intents into `BattleActionUse` and validates them through `BattleActionRules.Validate(...)` before returning a revalidated action/result.
- `TacticalAIExecutionBridgeTests`: added `Revalidator_RejectsLegacyMoveOutsideSharedMovementBudget`, covering the old fallback path against an out-of-budget move intent.
- `_codex/tasks/QA/2026-06-25_2219_052_CodingAgentCompletion.md`: added the required per-action/per-skill audit inventory and manual gameplay checklist.
- No Inspector fields changed.
- No ScriptableObject assets changed.

### Automatic Test

- Added/updated EditMode test file: `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`.
- New test checks that a direct legacy AI move intent beyond the actor's movement budget is rejected by the shared `BattleActionRules` validator.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, then run `TacticalAIExecutionBridgeTests`.
- Expected result: `Revalidator_RejectsLegacyMoveOutsideSharedMovementBudget` passes with the existing suite.
- Tests are run manually by the user in Unity; Unity Test Runner was not run automatically.

### Unity Test

#### Unity Setup

- Open Unity and let the changed `.cs` files import/compile.
- No scene, prefab, material, controller, `.asmdef`, `.asmref`, or Inspector setup changes are required.
- Keep the existing battle scene references for `HexMap`, `MouseControler`, `TurnManager`, `BattleActionLifecycle`, `DataMapper`, skill catalog, and unit catalog.

#### Play Mode Test

- Start a tactical battle with enemy AI enabled.
- Confirm enemy AI can still execute move, move-and-attack, basic ranged attack, wait, defend, and skill actions.
- Try or force an AI movement plan that is stale or outside current movement budget; expected result is rejection before live execution, not a forced move.
- Run the task 52 manual checklist from `_codex/tasks/QA/2026-06-25_2219_052_CodingAgentCompletion.md`, prioritizing `Double_Throw`, `Rush`, `Stone_Throw`, `Long_Lick`, `Spike_Trap`, `Rope_Trap`, and passives.

### QA Verdict

- Final QA status: PASS.
- Completion protocol: `_codex/tasks/QA/2026-06-25_2219_052_CodingAgentCompletion.md`.
- QA report: `_codex/tasks/QA/2026-06-25_2220_052_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: `TacticalAIIntentRevalidator` still contains old unreachable non-skill switch cases after the new shared-validator branch; broader player input, CastManager compatibility, passive hooks, trap triggers, and legacy AI intent symbols remain PRD050 debt.
- Follow-up fixes applied after QA: none required.

### Notes

- This was an audit hardening pass, not a full PRD050 purge.
- No gameplay numeric values changed.
- No asset, scene, prefab, material, controller, generated Unity file, `.inputactions`, `.asmdef`, or `.asmref` file was edited.
- Player skill commits still pass through CastManager compatibility after `SkillRules` validation.
- Passive and trap trigger execution still depends on legacy hooks and needs manual gameplay validation.

### Next Steps

- Let Unity import/compile the changed C# files.
- Run `TacticalAIExecutionBridgeTests` in Unity Test Runner EditMode.
- Run the Play Mode checklist above, especially the high-risk skills and passive/trap scenarios.
