# [TARENA] PRD054D: Action Preview Hardening And Coverage

- Status: draft
- Type: PRD / QA hardening task
- Area: Combat Preview Edge Cases, UI Coverage, QA
- Label: ready-for-agent
- Created: 2026-06-30
- Parent PRD: `_codex/tasks/054_PRD_SkillIndicators_ActionPreviewUX.md`
- Depends On: `_codex/tasks/054A_PRD_ActionPreview_CombatForecastModel.md`
- Depends On: `_codex/tasks/054B_PRD_ActionPreview_PolishedUIPrefabs.md`
- Depends On: `_codex/tasks/054C_PRD_ActionPreview_RuntimeUIBinding.md`

## Goal

Harden action preview coverage after the MVP model and UI are integrated.

This task closes edge cases and verifies that preview remains trustworthy
across skill families, invalid states, status/stat/utility effects, and
retaliation cases.

## Scope

Do:

- Audit current skills for preview coverage across attack, buff, debuff,
  utility, status-only, AoE, multi-target, movement, teleport/pull/push, trap,
  spawn, self-target, ally-target, empty-hex target, and no-target categories.
- Ensure unsupported or not-yet-previewable cases degrade to clear `-` or
  explicit missing preview data instead of false numbers.
- Verify range semantics for deterministic and variable damage.
- Verify death certainty overlays for guaranteed and possible stack wipes.
- Verify status icons and stat-change icons can coexist with damage/kills.
- Verify utility movement icons can coexist with statuses.
- Verify invalid target badge behavior does not persist after hover changes.
- Verify bottom panel aggregate values include all affected units and own losses.
- Verify net value is catalog cost based.
- Add missing tests for edge cases discovered in the audit.

Do not:

- Change skill balance.
- Change unit economic costs.
- Rewrite the full skill system.
- Replace PRD053 indicators.
- Modify PRD019 assets.

## Required Edge Cases

Cover or explicitly document:

- Double Throw / multi-target projectile.
- Basic ranged attack.
- Basic melee move-and-attack.
- Retaliation that kills the actor's units.
- AoE damage.
- Push/pull/knockback.
- Teleport/swap-style movement if present.
- Buff.
- Debuff.
- Status without damage.
- Heal/resurrect if present or future-facing.
- Summon/spawn.
- Defensive action if preview is later enabled.
- Wait action if preview is later enabled.
- Self-target skill.
- Empty-hex target skill.
- Friendly fire.
- Skills that affect terrain, traps, or zones.
- Skills with no current previewable numeric outcome.

## Acceptance Criteria

Done when:

- Every current active skill has a documented preview support status.
- Unsupported skill/effect families fail visibly and safely instead of showing
  misleading values.
- Automated tests cover at least one example each for attack, status/stat
  effect, utility/movement effect, retaliation, AoE/multi-target, invalid
  target, and net value.
- Manual Play Mode QA covers at least one skill from each supported family.
- The bottom panel summary remains correct with 5+ affected units.
- Badge clutter remains acceptable with 5+ affected units or the task records a
  concrete follow-up for overflow behavior.
- Invalid target badge clears on target change, cancel, commit, and turn change.
- No stale preview remains after action lifecycle blocking starts.
- Existing skill indicators and valid target highlights still work.

## Testing Decisions

Use a mix of EditMode and manual Play Mode checks:

- EditMode for forecast math, aggregation, cost, and DTO binding.
- Manual Play Mode for screen-space badge placement, clutter, clear timing, and
  indicator coexistence.

Recommended manual matrix:

- 1920x1080
- 2560x1440
- 2560x1600
- 3840x2160
- 1366x768

At each resolution, verify bottom-right panel readability, badge readability,
and no overlap with critical HUD controls.

## Out of Scope

- New feature design beyond hardening existing PRD054 behavior.
- AI strategy improvements.
- Server implementation.
- Full accessibility overhaul.

## Notes For Coding Agent

Use Codex 5.3 Spark subagents for parallel read-only audits:

- one explorer for current skill families and missing cases,
- one explorer for existing tests and likely new test fixtures,
- one explorer for UI clutter/responsive risks from existing battle HUD docs.

Do not let a subagent make broad code changes in this hardening task unless its
write scope is narrow and isolated.
