# [TARENA] 051 PRD PRD050 Unity Validation And Legacy Scene Cleanup

- Status: ready-for-agent
- Type: validation / cleanup PRD
- Area: tactical battle, Unity validation, scene-wired legacy cleanup
- Source task: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
- Source closure report: `_codex/tasks/QA/2026-06-26_050_FinalClose_CodingAgentCompletion.md`
- Triage label: ready-for-agent

## Problem Statement

PRD050 is code-closed, but the Unity project still needs manual compile,
EditMode, and Play Mode parity validation before the migration can be treated
as production-verified. One legacy scene-wired component, `CastManager`, also
remains in the project as a non-authoritative holder because removing it safely
may require scene or prefab edits.

## Solution

Validate the PRD050 migration inside Unity, document any regressions as focused
follow-up fixes, and only then decide whether scene-safe `CastManager` removal
or further reduction is possible. The tactical runtime authority should remain:

- `BattleActionUse`
- `BattleAction`
- `BattleActionResult`
- result apply

## User Stories

1. As a player, I want Slash movement highlights to show the full legal movement area, so that the skill target flow is understandable.
2. As a player, I want Slash attack highlights to appear after selecting the movement hex, so that I can choose the second skill target.
3. As a player, I want Slash cast into empty space to still consume the unit action, so that failed or non-damaging skill use follows the same turn rules.
4. As a player, I want basic melee counterattacks to trigger when legal, so that melee risk is preserved after the Battle Action migration.
5. As a player, I want Rope Trap to trigger, apply its status, and remove itself as before, so that trap behavior stays stable.
6. As a player, I want Fire Trap to affect enemies but not its owner, so that trap ownership behavior stays stable.
7. As a player, I want Spike Trap to apply movement reduction, damage over time, path trimming, and trap removal as before, so that movement disruption stays stable.
8. As a player, I want Fire Movement to place and reveal Fire Trap correctly, so that passive movement effects still work.
9. As a player, I want new-turn passive/status ticks to resolve in the existing visual order, so that battle feedback remains readable.
10. As a player, I want autocast passive statuses to be queued at turn start as before, so that unit build behavior remains stable.
11. As a developer, I want Unity compile to pass after PRD050, so that the Battle Action migration is actually shippable.
12. As a developer, I want EditMode tests to cover passive/cooldown skill filtering and automatic result generation, so that the migration does not regress silently.
13. As a developer, I want any remaining `CastManager` usage to be documented or removed safely, so that runtime authority does not drift back to legacy skill execution.

## Implementation Decisions

- Treat this PRD as validation-first. Do not make gameplay float/value changes without a separate approval.
- Do not edit scenes, prefabs, materials, animator controllers, `.inputactions`, `.asmdef`, or `.asmref` unless a future task grants exact path-specific permission.
- Keep `CastManager` removal as optional cleanup until Unity scene wiring is inspected safely.
- If a validation failure appears, prefer focused fixes that preserve the PRD050 authority model instead of restoring legacy execution paths.
- If `CastManager` must stay scene-wired, it must remain non-authoritative: no active `startSpell` or `getMode` runtime callsites.
- If passive/trap/automatic behavior needs changes, keep mutations behind `BattleActionResult` apply surfaces.

## Testing Decisions

- Unity compile after script reload is required before any Play Mode verdict.
- EditMode tests to run:
  - `BattleActionLegalActionGenerationTests.PassiveAndCooldownBlockedSkills_AreNotLegalActions`
  - `BattleActionAutomaticResultApplierTests`
  - `BattleActionRulesTests`
  - `TacticalAIExecutionBridgeTests`
  - `TacticalAISearchScoringTests`
- Play Mode checks to run:
  - Slash full movement highlight.
  - Slash second attack highlight.
  - Slash cast into empty/no-damage target still ends the unit action.
  - Basic melee counterattack applies.
  - Rope Trap trigger/status/removal.
  - Fire Trap enemy-only trigger.
  - Spike Trap status/path trim/removal.
  - Fire Movement trap placement/reveal.
  - New-turn passive/status tick sequence.
  - Autocast passive status queueing.
- Good tests should assert visible behavior or public Battle Action contracts, not private helper method details.

## Out of Scope

- New tactical gameplay features.
- Balance/value changes.
- Scene or prefab edits without explicit future permission.
- Reintroducing `CastManager` as a runtime authority.
- Rewriting `SkillRules` unless validation finds a concrete defect.

## Further Notes

PRD050 can remain closed as a code task. This PRD tracks the remaining Unity
validation and optional scene-wired legacy cleanup after closure.

