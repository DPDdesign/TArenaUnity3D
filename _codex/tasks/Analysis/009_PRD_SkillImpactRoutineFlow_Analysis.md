# 009 PRD Skill And Combat Impact Routine Flow Analysis

## Task

`_codex/tasks/009_PRD_SkillImpactRoutineFlow.md`

## Context Loaded

- `_codex/tasks/archive/003_PRD_SkillVfxSfxFlow.md`
- `_codex/tasks/QA/003_PRD_SkillVfxSfxFlow_QAReview.md`
- `_codex/tasks/QA/003_PRD_SkillVfxSfxFlow_Completion.md`
- `_codex/tasks/001_PRD_CombatHitAnimationFlow.md`
- `_codex/tasks/archive/002_PRD_CombatSfxFlow.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Documentation/CurrentSkills.md`
- Relevant code in `CastManager`, `SkillPresentationManager`,
  `SkillPresentationCatalog`, `TosterHexUnit`, and `TosterView`.

## Readiness Verdict

PRD 009 now has the key timing and reveal decisions recorded:
authoritative damage/status/result application remains immediate, while
frontend presentation and result reveal can be delayed until animation,
projectile, impact, and hit routines reach the correct moment.

`OnSkillHit` should call hit/death animation and hit/death SFX in the first
implementation slice. Basic attacks and counterattacks should route their
hit/death reveal through the same frontend reveal path.

## Current Code Diagnosis

- Selection is already mostly separated. `{SkillName}M` methods configure
  targeting flags such as enemy/friendly/radius/self-cast selection and usually
  do not play skill VFX/SFX.
- Commit is not separated. `{SkillName}` methods currently mix damage/heal/status
  application, cooldown/state cleanup, animator calls, and presentation calls.
- PRD 003 added `SkillPresentationCatalog` and `SkillPresentationManager` with
  cast, projectile, and impact playback. The manager can move a projectile and
  play impact after arrival, but it does not own a full skill cast context or
  per-target hit routine.
- Current `CastManager` helpers call `PlayCast`, `PlayImpact`, and
  `PlayProjectile` directly. Fire_Ball currently plays projectile travel-only
  to the selected hex, then immediately plays target impacts from the collected
  hit list.
- `TosterView` already has coroutine helpers for waiting on animator states and
  animation progress.
- `TosterHexUnit.DealMePURE(...)` defaults to immediate hit/death animation.
  `Died()` immediately marks the unit dead, removes it from its hex/team lists,
  and visually flattens the model. This makes presentation-only projectile delay
  unsafe for lethal skill hits unless migrated skills route frontend reveal
  through a small result/presentation adapter.
- `AttackMeSequence(...)` already has attack/counterattack animation timing, but
  it applies damage at the hit moment and then directly plays hit/death response.
  It should become a caller of the shared frontend reveal path instead of owning
  hit/death reveal itself.
- `AttackMe(...)` and `ShootME(...)` are more immediate paths and should also be
  reviewed so basic attacks and ranged attacks do not bypass the new reveal
  path.
- `Heavy_Fists` currently has a local cast body plus a legacy Photon
  `heavy_fists(...)` RPC handler. Photon is not the target multiplayer
  transport, so PRD 009 should treat this only as evidence that the skill has
  split local/authoritative execution surfaces. Its migration must choose one
  transport-agnostic authoritative effect/reveal decision point and avoid
  playing frontend reveal once locally and once again through the legacy
  mirrored path.

## Confirmed Timing Decision

- Damage, status, cooldown, turn use, and other authoritative result state should
  be applied immediately.
- The frontend may receive, reveal, or refresh that already-resolved result after
  the skill animation, projectile travel, and impact timing.
- This is the intended multiplayer-compatible model: backend/server state moves
  first, client presentation catches up at readable moments.
- The model must not depend on Photon. Current RPC methods are legacy examples
  of mirrored execution, not the future networking API.
- For split execution skills, frontend reveal/presentation should be emitted
  only from the transport-agnostic authoritative effect handler. The local cast
  body should select targets, build the command/context, and submit it without
  playing reveal itself.
- The current local code does not yet have a clean backend/frontend boundary.
  PRD 009 should not pretend that it does. The implementation should add the
  smallest adapter needed for migrated skills to avoid double feedback and to
  delay frontend hit/death/status reveal without delaying gameplay state.
- The shared frontend reveal path should be used by skills, basic attacks, and
  counterattacks.
- In the first slice, the reveal path should call existing hit/death animation
  and hit/death SFX behavior rather than only exposing a placeholder hook.
- When authoritative damage kills a unit, board/gameplay state updates
  immediately. The frontend death reveal may still be delayed, using a captured
  view/result snapshot instead of relying on the killed unit still being present
  in gameplay occupancy lists.
- Multi-target and AoE impact SFX should play once per impact cluster by
  default. Per-target hit/death SFX may still play through the shared frontend
  reveal path. Per-target impact SFX is deferred unless a later task adds an
  explicit opt-in.
- The first implementation slice should include instant AoE behavior, but
  `Skill1`, `Skill2`, and `Skill3` should not be used as production validation
  targets. They can remain code examples, but acceptance should use a
  production-assigned skill.
- `Topornik_Skill1` and `Tank_Skill1` are unused cleanup targets and should not
  be used as representative validation skills.
- `Heavy_Fists` is the selected representative production-used instant AoE for
  PRD 009.
- `Fire_Ball` is the selected representative projectile skill for sequenced
  projectile impact timing. `Double_Throw` and `Axe_Rain` are later or optional
  multi-projectile follow-up candidates.
- Basic ranged attack is included in the first PRD 009 implementation slice and
  should use the same shared frontend reveal path.

## Recommended Target Flow

Use this as the implementation model for PRD 009:

1. Selection Phase
   - Only choose/collect legal target units, target hexes, or target areas.
   - No skill cast VFX, no projectile VFX, no impact VFX, no skill SFX.
   - UI SFX/outline feedback can remain a later task.

2. Commit Snapshot Phase
   - Once selection is complete, capture a `SkillCastContext`.
   - Context should include skill id, caster, selected skill slot, selected hex,
     selected target units, affected units, affected hexes, and per-target
     resolved result data needed by the frontend reveal.
   - Apply authoritative damage/status/result state immediately.
   - Suppress immediate hit/death frontend reveal on migrated paths so the
     shared reveal path owns it exactly once.
   - For lethal results, capture the target view/result data before logical
     removal makes the target hard to find through hex/team lists.

3. Animation Phase
   - Play the caster skill animation immediately after commit.
   - Default state name should be `Skill{selectedSlot + 1}` to match the current
     code convention.
   - If no Animator/state is available, continue safely after a short max wait.
   - No cast VFX/SFX should play until this phase finishes.

4. Cast Phase
   - Play cast VFX/SFX once after the skill animation finishes.
   - This is one cast event even when the skill has multiple targets.

5. Projectile Phase
   - If the skill has projectiles, spawn one projectile per impact destination
     or affected target, depending on skill category.
   - Projectile VFX starts with cast VFX/SFX.
   - Projectile SFX should loop for the projectile lifetime and stop on arrival.
     PRD 003 currently uses one-shot projectile SFX, so PRD 009 must explicitly
     add loop lifetime behavior.

6. Impact Phase
   - For projectile skills, impact runs when each projectile reaches its own
     destination.
   - For instant skills, impact runs after animation/cast, optionally with the
     existing configured delay.
   - Multi-target and AoE skills should support VFX per actual target/hex.
   - Impact SFX policy should be explicit to avoid accidental audio spam:
     one impact SFX per impact cluster by default.

7. Hit Phase
   - Run once per actually affected unit.
   - Empty selected hexes do not receive unit hit routines.
   - Frontend hit/death/status reveal should happen from this phase, not from
     scattered skill bodies.

8. Attack/Counterattack Reveal
   - Basic attacks and counterattacks use their existing attack timing as the
     reveal trigger.
   - At the attack hit moment, call the same frontend reveal path used by
     `OnSkillHit`.

## Recommended First Implementation Slice

- Keep XML skill ids and `SkillPresentationCatalog` authoring from PRD 003.
- Add a small skill sequence/routine layer instead of rewriting the skill system.
- Keep authoritative gameplay application immediate; only sequence presentation
  and frontend reveal.
- Route basic attack and counterattack hit/death reveal through the same new
  frontend reveal helper.
- Cover a narrow representative slice first:
  - `Fire_Ball`: one cast, projectile to selected hex or targets, delayed
    impact/hit after arrival.
  - Defer `Double_Throw` and `Axe_Rain` unless the first implementation needs an
    explicit multi-projectile follow-up validation case.
  - `Heavy_Fists` as the production-used instant AoE to validate per-target hit
    routines without projectile travel. Avoid using legacy/test placeholder
    `Skill1`, `Skill2`, `Skill3`, or unused cleanup-target skills such as
    `Topornik_Skill1` and `Tank_Skill1` as acceptance targets.
  - Basic ranged attack, basic melee attack, and counterattack reveal through
    the shared frontend reveal path.
- Do not change damage values, cooldown values, targeting rules, XML skill
  assignment, public/serialized field names, prefabs, scenes, or assets.

## Minimal Model

- `SkillCastContext`
  - `skillId`
  - `caster`
  - `selectedSkillSlot`
  - `selectedHex`
  - `selectedTarget`
  - `impactTargets`

- `SkillImpactTarget`
  - `targetUnit`
  - `targetHex`
  - `playProjectile`
  - `resolvedResult`

- `SkillPresentationManager`
  - play skill animation and wait
  - play cast once
  - play projectile per impact target
  - call impact after arrival
  - call frontend hit routine per actual affected unit

- `FrontendResultReveal`
  - source action type
  - source unit
  - target unit
  - resolved result snapshot
  - reveal hit animation/SFX or death animation/SFX

Example:

- `Fire_Ball`
  - selection collects selected AoE hex
  - commit snapshots actual affected targets
  - caster plays `Skill{slot + 1}`
  - cast VFX/SFX plays once
  - projectile travels
  - impact VFX plays on each actual affected target or configured AoE hex
  - each actual affected unit receives frontend hit/death/status reveal

## Acceptance Criteria To Add Or Tighten

- Opening targeting mode does not play cast, projectile, impact, or skill SFX.
- After final target selection, caster skill animation starts before cast VFX/SFX.
- Cast VFX/SFX starts only after the skill animation completes or times out.
- Multi-target skills play cast once and projectile/impact per affected target
  or destination.
- Projectile SFX loops only while the projectile exists and stops on arrival.
- Impact VFX/SFX does not play before the relevant projectile reaches its
  destination.
- `OnSkillHit` runs once per actual affected unit and never for an empty
  selected hex.
- Basic attack and counterattack hit/death feedback uses the same frontend
  reveal path as skill hit feedback.
- Authoritative damage/status/result state is applied immediately and does not
  wait for local presentation.
- Frontend hit/death/status reveal for migrated sequenced skills happens after
  impact timing.
- Lethal hits update logical board state immediately while visible death reveal
  can wait for the shared frontend reveal path.
- Multi-target and AoE impact VFX may appear on multiple targets/hexes, but
  impact SFX plays once per cluster by default.
- Frontend reveal happens exactly once per affected unit/result; migrated paths
  do not also trigger immediate hit/death feedback through `DealMePURE`.
- Missing catalog entries, VFX, SFX, Animator, or animation state remain safe
  no-ops/fallbacks.
- No gameplay float values, cooldown values, targeting rules, or XML skill ids
  are changed.

## Manual Test Focus

- Select a target and cancel/retarget: no cast/projectile/impact skill feedback.
- Commit `Fire_Ball` against one unit: animation, cast, projectile, impact, hit
  order is readable.
- Commit `Fire_Ball` against multiple units: one cast, correct projectiles,
  impact/hit per actual target.
- Commit a multi-target or AoE skill and confirm impact SFX plays once for the
  cluster, while per-target hit/death SFX can still play through reveal.
- Commit a projectile skill that kills a unit: authoritative result is resolved
  immediately, while frontend death reveal happens at hit phase after impact.
- Run a melee attack with no counterattack: attacker animation reaches hit
  moment, then target hit/death reveal plays from the shared path.
- Run a melee attack with counterattack: both directions reveal hit/death through
  the shared path at their own hit moments.
- Run a basic ranged attack: projectile/impact timing remains readable and
  hit/death reveal uses the shared path.
- Commit `Heavy_Fists` with no affected units: no unit hit routine.
- Commit `Heavy_Fists` with multiple affected units: per-target VFX/hit routine
  without projectile travel, and no double reveal between local and legacy
  mirrored execution paths.
- Missing catalog entry and missing Animator/state remain safe.

## Next Grill Question

Is PRD 009 now ready to mark `ready-for-agent`, or should it remain
`needs-review` for one more architecture pass?
