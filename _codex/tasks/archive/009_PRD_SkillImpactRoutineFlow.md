# [TARENA] PRD: Skill And Combat Impact Routine Flow

- Status: closed-implemented-validated
- Type: PRD
- Area: Skills, Combat Presentation, Combat Events, Frontend Reveal
- Label: closed

## Problem Statement

PRD 3 added a simple data-driven skill VFX/SFX layer with cast, projectile, and
impact presentation calls. That first pass is useful for authoring assets, but
it is still fire-and-forget: skill gameplay effects usually resolve immediately,
while projectile and impact presentation can play independently from the real
visual arrival moment.

Gameplay/result state should still resolve immediately on the authoritative
side. This matters for future multiplayer: damage, status, cooldown, and turn
state should not wait for local presentation. The frontend/presentation layer
may reveal or refresh the already-resolved result only after the correct
animation, projectile, and impact timing.

The next step is to make skill presentation and combat damage presentation use
one frontend reveal path. Attack presentation already has an event shape where
hit, impact, and death feedback are tied to real combat moments. Skill VFX/SFX
should move toward the same model, and basic attacks and counterattacks should
also route their hit/death reveal through the same frontend routine:

1. The skill is committed.
2. Optional projectile presentation travels.
3. `onImpact` happens when the projectile or instant effect actually reaches
   the impact point.
4. `onHit` happens for every unit actually affected by the skill.
5. Impact VFX/SFX, hit reactions, hit SFX, and later death flow are triggered
   from those routines instead of ad hoc skill-body calls.

For example, a fireball explosion should not happen at cast time or as an
unrelated fallback. It should happen when the fireball projectile reaches the
impact point. Then each actually hit unit should receive the skill hit routine.

## Goals

- Introduce explicit skill presentation routines equivalent in spirit to attack
  presentation routines, especially `onImpact` and `onHit`.
- Introduce one shared frontend reveal path for skill hits, basic attack hits,
  and counterattack hits.
- Make projectile skill impact VFX/SFX play only after the projectile visual
  reaches its destination.
- Make skill `onHit` run for each actually affected unit.
- Make hit/death animation and hit/death SFX play from the shared frontend
  reveal path in the first implementation slice.
- Preserve the PRD 1/2 rule that hit feedback is tied to real damage/status
  events, not just selected targeting positions.
- Keep authoritative damage/status/result application immediate, while allowing
  frontend hit/death/status reveal to wait for skill impact timing.
- Preserve PRD 3 catalog authoring: content still assigns cast, projectile, and
  impact VFX/SFX through `SkillPresentationCatalog`.
- Avoid changing skill damage, cooldown, targeting, movement, or balance values.

## Non-Goals

- Do not rewrite the whole skill system.
- Do not move all skill data out of XML.
- Do not introduce final VFX/SFX assets.
- Do not edit prefabs, scenes, Animator Controllers, `.asmdef`, `.asmref`, or
  generated Unity files as part of the coding task.
- Do not change combat balance values.
- Do not make authoritative gameplay state wait for presentation.
- Do not make every skill's frontend feedback wait for presentation unless the
  PRD explicitly marks that skill category as sequenced.
- Do not rewrite the whole attack, counterattack, or damage system.

## User Stories

1. As a player, I want projectile skill explosions to happen when the projectile arrives, so that the visual cause and effect are readable.
2. As a player, I want every unit hit by an AoE skill to receive hit feedback, so that I can see who was affected.
3. As a player, I want skills, basic attacks, and counterattacks to feel consistent, so that hit, impact, and death feedback follows the same combat language.
4. As a designer, I want skill impact VFX/SFX to still be configured in the catalog, so that setup remains simple.
5. As a developer, I want one skill impact routine instead of scattered per-skill impact calls, so that future skills are easier to wire correctly.
6. As a developer, I want skill `onHit` to receive the actual affected unit, so that hit animation, hit SFX, death animation, death SFX, and future status feedback can be unified.
7. As a QA tester, I want to test projectile arrival, impact, and per-target hit feedback separately, so that timing bugs are visible.

## Proposed Model

Add a small event/routine layer that sits between skill/attack commit logic and
presentation playback.

Suggested concepts:

- `FrontendResultReveal`
  - source action type: skill, basic attack, counterattack
  - source unit
  - target unit
  - resolved result snapshot
  - hit or death reveal decision
- `SkillCastContext`
  - skill id
  - caster
  - selected hex, when relevant
  - selected target unit, when relevant
  - affected target units
  - impact hex or impact unit, when relevant
  - immediate result snapshot, when needed by the frontend reveal
- `OnSkillCast`
  - plays cast VFX/SFX
  - can start projectile presentation
- `OnSkillImpact`
  - runs when an instant skill resolves or when projectile travel completes
  - plays impact VFX/SFX at the resolved impact point
  - dispatches hit routines for affected units
- `OnSkillHit`
  - runs once per affected unit
  - reveals already-resolved hit/death/status feedback on the frontend
  - calls hit/death animation and hit/death SFX through the shared frontend
    reveal path
  - should not run for empty targeting or missed/invalid targets
- `OnAttackHit`
  - runs for basic attacks and counterattacks when their attack timing reaches
    the hit moment
  - uses the same frontend reveal path as `OnSkillHit`

## Implementation Decisions To Make

- Decide whether the first sequenced slice should cover only projectile skills
  (`Fire_Ball`, `Axe_Rain`, `Double_Throw`, basic ranged attack) or also
  instant AoE skills.
- Decide how to avoid double-hit feedback when skills internally call existing
  damage methods that already trigger combat hit/death behavior.

## Confirmed Decisions

- Authoritative damage/status/result application remains immediate for all
  skills. Sequencing affects frontend presentation and result reveal, not
  gameplay resolution.
- In the future multiplayer model, the backend/server result is applied first
  and the frontend receives or reveals that result after the appropriate skill
  animation, projectile, and impact timing.
- The frontend reveal path must be transport-agnostic. Do not design PRD 009
  around Photon RPCs; current RPC methods are legacy execution surfaces only.
- For split execution skills, frontend reveal/presentation should be emitted
  only from the transport-agnostic authoritative effect handler. The local cast
  body should select targets, build the command/context, and submit it without
  playing reveal itself.
- The current local code has no clean backend/frontend split yet. The coding
  task should therefore introduce the smallest result/presentation adapter
  needed for migrated skills instead of delaying actual gameplay state.
- `OnSkillHit` should call hit/death animation and hit/death SFX in the first
  implementation slice.
- Basic attacks and counterattacks should also route their hit/death reveal
  through the same new frontend reveal path.
- Existing attack/counterattack timing remains the timing source for basic
  melee flow: the reveal happens at the attack hit moment, not at action start.
- When authoritative damage kills a unit, logical board state updates
  immediately. The unit may be removed from gameplay occupancy/team state right
  away, while the frontend uses a captured view/result snapshot to reveal death
  at the correct impact/hit timing.
- Multi-target and AoE impact SFX plays once per impact cluster by default.
  Per-target hit/death SFX can still play through the shared frontend reveal
  path. Per-target impact SFX is deferred unless a later task adds explicit
  opt-in behavior.
- The first implementation slice should include an instant AoE category, but
  should not use legacy/test placeholder skills `Skill1`, `Skill2`, or `Skill3`
  as production validation targets. Use a production-used AoE skill instead;
  a skill merely existing in XML is not enough if it is an unused cleanup target.
- `Topornik_Skill1` and `Tank_Skill1` are unused cleanup targets and should not
  be used as PRD 009 representative validation skills.
- Use `Heavy_Fists` as the representative production-used instant AoE target for
  this PRD.
- Use `Fire_Ball` as the first representative projectile skill for sequenced
  projectile impact timing. Treat `Double_Throw` and `Axe_Rain` as later or
  optional multi-projectile follow-up candidates.
- Basic ranged attack is included in the first PRD 009 implementation slice and
  should use the same shared frontend reveal path.

## Acceptance Criteria

- Projectile skill impact VFX/SFX does not play until the projectile visual
  reaches its destination.
- Authoritative damage/status/result state is applied immediately and is not
  delayed by local animation, projectile, or VFX timing.
- `Fire_Ball` can show projectile travel, then explosion/impact, then per-unit
  hit feedback for actual targets.
- `Fire_Ball` is the first required projectile validation target for the PRD
  009 implementation slice.
- Frontend hit/death/status reveal for migrated sequenced skills happens from
  `OnSkillHit`, after the relevant impact timing.
- Basic attack and counterattack hit/death feedback is routed through the same
  frontend reveal path as skill hits.
- Split execution skills such as `Heavy_Fists` do not play frontend reveal from
  both the local cast body and the authoritative effect handler.
- Lethal hits update board/gameplay state immediately, but the visible death
  reveal can still wait for the shared frontend reveal path.
- Empty AoE/projectile targeting does not trigger per-unit `OnSkillHit`.
- Target-affecting AoE skills can call `OnSkillHit` once per affected unit.
- Multi-target and AoE impact VFX can appear on multiple targets/hexes, but the
  default impact SFX plays once for the cluster.
- Skill presentation still uses `SkillPresentationCatalog` entries from PRD 3.
- Existing basic ranged attack presentation remains catalog-driven.
- Existing basic ranged attack hit/death reveal uses the shared frontend reveal
  path in the first implementation slice.
- No old hard-coded projectile path is reintroduced for migrated skills.
- Missing catalog entries, VFX, or SFX remain safe no-ops.
- No gameplay balance values are changed.

## Manual Test Plan

- Test one basic ranged attack and confirm projectile, impact, and hit feedback
  are ordered correctly.
- Test one melee attack and one counterattack and confirm hit/death animation
  and SFX come from the shared frontend reveal path at the hit moment.
- Test `Fire_Ball` against multiple units and confirm explosion happens after
  projectile arrival, then each actual hit unit receives hit feedback.
- Test a multi-target or AoE impact and confirm impact SFX plays once, while
  per-target hit/death SFX can still play through frontend reveal.
- Test a migrated damaging skill and confirm damage/status/result state is
  resolved immediately while frontend reveal waits for impact.
- Test `Fire_Ball` or another AoE against an empty target area and confirm no
  unit hit feedback plays.
- Test one instant direct skill and confirm it can run impact/hit routines
  without projectile travel.
- Test `Heavy_Fists` as the representative production-used instant AoE skill and
  confirm `OnSkillHit` runs for each affected unit. Do not use legacy/test
  placeholder `Skill1`, `Skill2`, `Skill3`, or unused cleanup-target skills as
  the representative acceptance target.
- Test `Heavy_Fists` and confirm the local cast body does not double-play reveal
  in addition to the authoritative effect handler.
- Test missing catalog entry/clip cases and confirm no crash or warning spam.

## Relationship To Previous PRDs

- PRD 1 established that hit/death animation should follow real damage events.
- PRD 2 established that combat SFX should follow real attack, hit, and death
  events.
- PRD 3 established the skill VFX/SFX catalog and simple presentation manager.
- This PRD reworks PRD 3 playback timing so skill impact and hit presentation
  follows attack-like routines instead of being scattered directly in skill
  bodies.

## Implementation - 2026-06-11

### What Changed

No Inspector fields changed.

`FrontendResultReveal.cs` / `FrontendResultReveal`: added a runtime-only result
snapshot for source type, source unit, target unit, target view, damage, and
survival result. These fields are not Inspector-tuned; they affect when already
resolved hit/death feedback is revealed. `Damage <= 0` suppresses hit/death
reveal, while higher positive values reveal the same already-applied result;
tuning remains in existing combat math, not this snapshot.

`TosterHexUnit.cs` / `TosterHexUnit`: added shared damage/reveal helpers and
routed melee attack, counterattack, and basic ranged hit/death feedback through
them. `ShootME(..., true)` now applies damage immediately and reveals after
basic ranged projectile impact; `ShootME(..., false)` remains legacy immediate
behavior for non-migrated skill callers.

`SkillPresentationManager.cs` / `SkillPresentationManager`: added sequenced
projectile and instant hit flows that wait for optional skill animation, play
cast/projectile/impact presentation, then dispatch per-target reveal. Projectile
SFX now loops only for projectile lifetime. Missing manager/catalog entries
still fall back to hit/death reveal.

`CastManager.cs` / `CastManager`: migrated `Fire_Ball` to projectile arrival
impact timing and per-target reveal. Migrated `Heavy_Fists` to instant AoE
impact timing and per-target reveal without the old immediate `JustDmg` reveal
path. `Double_Throw` and `Axe_Rain` were intentionally not migrated.

### Automatic Test

No new EditMode tests were added because this repo does not have a project-owned
EditMode test assembly for these gameplay scripts, and the project rules forbid
creating or editing `.asmdef` files without permission.

Tests are run manually by the user in Unity. Open `Window > General > Test
Runner`, select `EditMode`, and run the available tests after Unity compiles.
Expected result for PRD 009 specifically: no new PRD 009 test appears, and Unity
should report no C# compile errors from the changed scripts.

### Unity Test

#### Unity Setup

Open the battle scene. Ensure there is one `SkillPresentationManager` component
with an `AudioSource` and an assigned `SkillPresentationCatalog`.

In the catalog, configure entries for `Fire_Ball`, `Heavy_Fists`, and
`defaultBasicRangedAttackEntry`. Assign a projectile VFX/SFX for `Fire_Ball`
and the basic ranged entry if you want to verify projectile timing. Assign
impact VFX/SFX for `Fire_Ball`, `Heavy_Fists`, and basic ranged impact.

Place or select units that can perform a melee attack, counterattack, basic
ranged attack, `Fire_Ball`, and `Heavy_Fists`.

#### Play Mode Test

Press Play. Perform one melee attack and one counterattack; hit/death animation
and SFX should happen at the attack hit moment.

Perform one basic ranged attack; damage should apply immediately, then
projectile travel, impact VFX/SFX, and target hit/death reveal should occur in
that order.

Cast `Fire_Ball` on one unit, multiple units, and an empty area. Projectile
impact should wait for arrival; each actual target should reveal hit/death once;
the empty area should not play per-unit hit/death reveal.

Cast `Heavy_Fists` on multiple affected units. Impact SFX should play once for
the cluster, while each affected unit gets its own hit/death reveal without
double feedback.

### QA Verdict

Final QA status: Pass.

QA report: `_codex/tasks/QA/2026-06-11_1707_009_PRD_SkillImpactRoutineFlow_QA_ArchitectureReview.md`.

Actionable findings: none.

Non-blocking observations: `SkillPresentationManager` now coordinates basic
ranged reveal timing because basic ranged projectile presentation already lives
there; a later cleanup may split generic reveal orchestration if more non-skill
actions migrate. `DealMePURE(false)` now defers immediate death flattening so
death reveal can play later; watch legacy callers in Play Mode.

Follow-up fixes applied: none needed after QA.

### Notes

Unity compile, Unity Test Runner, and Play Mode checks were not run by the
agent, per project policy.

User-side Play Mode validation update, 2026-06-11: `Fire_Ball` behavior was
confirmed working by the user. `Default Basic Ranged Attack` behavior was also
confirmed working by the user. `Heavy_Fists` rework was later confirmed working
by the user after the follow-up `Approach` fix below.

Follow-up fix, 2026-06-11: `Heavy_Fists` now includes the missing optional
`Approach` layer. After target selection, the caster first moves to the chosen
approach hex through `DoMovesST`; only after movement finishes does the skill
apply damage, play cast/impact presentation, and reveal hit/death feedback.
Manual validation should check `Select -> Approach -> Cast/Impact/Hit Reveal`.

Final user-side Play Mode validation update, 2026-06-11: PRD 009 is closed as
implemented and validated.

No scenes, prefabs, materials, controllers, generated `.meta`, `.inputactions`,
`.asmdef`, `.asmref`, XML skill ids, cooldowns, targeting rules, movement
values, damage values, or balance values were edited.

Missing `SkillPresentationManager` or catalog entries no longer hide hit/death
reveal, but they still mean VFX/SFX will not play until the scene/catalog is
wired.

### Next Steps

Run Unity compile and the Unity Test Runner manually.

Run the Play Mode checks above for melee/counterattack, basic ranged attack,
`Fire_Ball`, and `Heavy_Fists`.

After this slice validates in Unity, migrate `Double_Throw` and `Axe_Rain` in a
separate task if their multi-projectile timing should join the same routine.
