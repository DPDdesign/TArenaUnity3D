# [TARENA] PRD: Migrate Remaining Skill Impact Routines

- Status: draft
- Type: PRD
- Area: Skills, Combat Presentation, Impact Timing, Frontend Reveal
- Label: needs-review
- Requires: `_codex/tasks/011_CleanupLegacyTopornikRzutnikUnits_PrePRD010.md`
- Requires: `_codex/tasks/012_CleanupPlaceholderSkill123Units_PrePRD010.md`

## Problem Statement

PRD 009 proved the shared combat frontend reveal model on the representative
slice: melee attack, counterattack, basic ranged attack, `Fire_Ball`, and
`Heavy_Fists`. That slice also exposed a missing phase in the model: some skills
select targets first, then move or reposition the caster or another unit before
the actual cast/impact/readout should happen.

The remaining skills still mostly use the older PRD 003 fire-and-forget
presentation helpers such as `PresentCastAndImpactAtUnit`,
`PresentImpactOnHitTargets`, and `PresentProjectileToUnit`. Those helpers can
play impact feedback before projectile arrival, before approach movement
finishes, or without routing per-target hit/death feedback through the shared
PRD 009 reveal path.

The next migration should make remaining active cast/commit skill paths in
`CastManager` use a consistent presentation routine model:

1. Select targets.
2. Optional approach or movement setup.
3. Cast.
4. Optional projectile travel.
5. Impact.
6. Per-target frontend result reveal when the skill applies a unit result.

## Goals

- Migrate all remaining active `CastManager` skill presentation call sites from
  direct PRD 003 helpers to PRD 009-style sequenced routines.
- Keep PRD 010 presentation-only: preserve existing gameplay values, targeting,
  cooldowns, ranges, movement distances, and status/stat modifier values.
- Treat `Approach` as an explicit optional phase for skills that move the caster
  or another unit before cast/impact should be presented.
- Keep damage, healing, status, movement, cooldown, targeting, and turn state
  application authoritative and immediate at the correct gameplay point.
- Route every damaging skill hit through the shared frontend reveal path exactly
  once.
- Generalize frontend result reveal for unit results so damage, healing, and
  status feedback can share the same reveal timing without faking hit/death
  feedback for non-damage results.
- Keep impact VFX on every actually affected unit for target-affecting AoE
  skills.
- Keep impact SFX once per impact cluster by default, unless a later task adds
  explicit per-target impact SFX behavior.
- Preserve `SkillPresentationCatalog` authoring and existing skill id strings.
- Remove or bypass old Photon/PUN/RPC branches only where they are directly part
  of a migrated skill path, replacing them with local authoritative calls that
  preserve the same gameplay behavior.

## Non-Goals

- Do not rewrite the whole skill system.
- Do not move skill assignment out of XML.
- Do not rename skill ids.
- Do not change skill damage, healing, cooldown, range, target filters, movement
  distances, or stat modifiers.
- Do not change XML skill ownership except through the required pre-task that
  removes legacy `Lizard` and `axe1` unit entries.
- Do not edit Unity scenes, prefabs, Animator Controllers, materials,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.
- Do not create final VFX/SFX assets or catalog assets.
- Do not migrate passive-only skills into active cast presentation unless their
  real trigger point is also migrated.
- Do not migrate passive or deferred runtime trigger skills in this PRD, even
  if they have real gameplay effects through movement, turn start,
  `SpellOverTime`, traps, or other non-cast systems.
- Do not perform global Photon/PUN/RPC cleanup. A full Photon/PUN/RPC cleanup is
  a required follow-up PRD after this migration.
- Do not migrate or preserve legacy Topornik/Rzutnik skills. They are removed
  by the required pre-task before PRD 010 implementation.
- Do not migrate or preserve placeholder `Skill1`, `Skill2`, or `Skill3`.
  They and their placeholder carrier units are removed by the required
  pre-task before PRD 010 implementation.

## User Stories

1. As a player, I want every active skill to present cause and effect in the
   order it actually happens.
2. As a player, I want skills that move before attacking to show movement first
   and cast/impact only after the unit arrives.
3. As a player, I want every unit hit by an AoE to receive readable feedback.
4. As a player, I want projectile skills to impact only after projectile arrival.
5. As a designer, I want all migrated skills to keep using existing catalog
   entries and exact skill ids.
6. As a developer, I want one routine model for cast/projectile/impact/reveal
   instead of scattered per-skill presentation calls.
7. As QA, I want a skill-by-skill checklist that says which phase model each
   skill uses.

## Proposed Routine Model

`SkillRoutineContext` should extend or wrap the PRD 009 model with:

- `skillId`
- `caster`
- `selectedSkillSlot`
- `selectedHex`
- `selectedTarget`
- `affectedUnits`
- `affectedHexes`
- `approachUnit`
- `approachHex`
- `impactTargets`
- `frontendResultReveals`

`frontendResultReveals` should represent unit results, not only damage. Allowed
result kinds:

- `Damage`: may reveal hit/death feedback.
- `Heal`: may reveal heal feedback.
- `Status`: may reveal buff/debuff/status feedback.

`FrontendResultReveal` must still only represent a concrete result applied to a
target unit. Hex-only, trap setup, summon/split, pure movement endpoints, and
stance presentation without a real unit status should use impact presentation
targets without fabricating a frontend result reveal.

Routine phases:

- `Select`: existing targeting/mode methods choose valid targets.
- `Approach`: optional movement/repositioning before cast or impact.
- `Cast`: caster animation and cast VFX/SFX.
- `Projectile`: optional projectile VFX/SFX travel.
- `Impact`: impact VFX/SFX at target unit, target hex, caster, or area center.
- `Result Reveal`: per-target damage/heal/status frontend reveal for actual
  affected units.

If a skill has no affected unit, it may still play cast and location impact when
the skill genuinely affects a hex, trap, summon point, movement endpoint, or
terrain. Empty target-affecting AoE should not fabricate unit hit reveal.

## Migration Scope

Already migrated by PRD 009:

- melee attack
- counterattack
- basic ranged attack
- `Fire_Ball`
- `Heavy_Fists`

Remaining active call sites to migrate from `CastManager`, after the required
legacy cleanup tasks:

- Direct target or instant unit effects: `Hate`, `Tough_Skin`,
  `Blind_by_light`.
- Unit-affecting AoE or multi-target effects: `Chope`, `Insult`,
  `Tank_Skill1`, `Defence_Ritual`, `Toxic_Fume`.
- Projectile or thrown multi-target effects: `Double_Throw`, `Axe_Rain`.
- Stance and self-cast effects: `Range_Stance_Barb`, `Melee_Stance_Barb`,
  `Range_Stance_Lizard`, `Melee_Stance_Lizard`, `Rage`, `Stone_Stance`,
  `Shapeshift`.
- Movement, approach, pull, teleport, or reposition effects: `TeleportOT`,
  `Rush`, `Slash`, `Tank_Skill2`, `Force_Pull`, `Long_Lick`.
- Trap, summon, or split effects: `Spike_Trap`, `Rope_Trap`, `Stone_Throw`.

Legacy skills removed or excluded before this PRD:

- `Skill1`
- `Skill2`
- `Skill3`
- `Topornik_Skill1`
- `Topornik_Skill2`
- `Topornik_Skill3`
- `Rzutnik_Skill1`
- `Rzutnik_Skill2`
- `Rzutnik_Skill3`
- `Rzutnik_Skill4`

Legacy placeholder units removed before this PRD:

- `TosterHEAL`
- `TosterDPS`
- `TosterTANK`

Passive/info/deferred skills should be audited but not migrated in PRD 010.
Examples deferred to a later PRD:

- `Cold_Blood`
- `Massochism`
- `Unstoppable_Light`
- `Stone_Skin`
- `Fire_Movement`
- `Fire_Skin`
- `Terrifying_Presence`
- `Rotting`

## Implementation Decisions

- Build the smallest shared routine adapter needed by the listed skills rather
  than adding one-off sequencing logic to each skill body.
- PRD 010 changes presentation flow only. If a migrated skill currently uses a
  Photon/PUN/RPC branch for the same skill effect or presentation, replace that
  specific branch with a local authoritative call that preserves identical
  gameplay behavior.
- Limit Photon/PUN/RPC removal to branches directly used by migrated skills.
  Do not use this PRD as a global networking cleanup.
- Keep `SkillPresentationManager` as the scene playback owner for VFX/SFX, but
  avoid letting it own gameplay rules or target selection.
- Keep `TosterHexUnit` as the owner of unit result application and frontend
  result reveal helpers.
- Add explicit approach support so a caller can run movement first and continue
  cast/impact/reveal only after movement finishes.
- Preserve immediate gameplay state application at the correct gameplay moment:
  after approach if the skill's effect only exists after approach, immediately
  at commit for skills with no approach.
- For lethal migrated hits, keep logical board removal immediate and reveal
  death using captured target view/result snapshots.
- For AoE, collect actual affected units first, then generate one reveal per
  affected unit result when the skill applies damage, healing, or status.
- For multi-projectile skills, each projectile should own its own arrival and
  impact timing. Default impact SFX should still avoid uncontrolled audio spam.
- For location-only skills, use selected/affected hex impact without unit hit
  reveal.
- For self-cast buffs and self-applied statuses, generate a `Status` frontend
  result reveal on the caster when a real unit status/effect is applied.
- For pure stance toggles without a real unit status object, use cast/impact at
  caster without a frontend result reveal.
- Keep a follow-up note for full Photon/PUN/RPC cleanup. Do not draft or
  implement that full cleanup in PRD 010.

## Acceptance Criteria

- Every active migrated skill has a documented routine category: direct,
  instant AoE, projectile, approach, stance/self-cast, trap, summon/split,
  movement/pull/teleport, or passive/deferred.
- No migrated skill plays both old direct impact presentation and new sequenced
  impact presentation for the same committed effect.
- Approach skills play cast/impact only after the relevant movement/reposition
  completes.
- Projectile skills play impact only after projectile arrival.
- Target-affecting AoE skills play impact VFX on each actually affected unit.
- Target-affecting AoE skills do not run per-unit hit reveal for empty selected
  areas.
- Damaging migrated skills route hit/death feedback through the shared frontend
  reveal path exactly once.
- Healing migrated skills route heal feedback through the shared frontend result
  reveal path when they apply a unit heal.
- Status migrated skills route status feedback through the shared frontend
  result reveal path when they apply a unit buff/debuff/status, including
  self-cast status reveals on the caster.
- Location-only, trap, summon/split, pure movement, and pure stance effects use
  impact presentation targets without faking damage/heal/status result reveal.
- Missing catalog entries, VFX, SFX, Animator, or animation states remain safe
  no-ops or fallbacks.
- `SkillPresentationCatalog` entries remain keyed by existing skill ids.
- Migrated skill paths no longer play old direct PRD 003 presentation helpers or
  old Photon/PUN/RPC presentation branches for the same committed effect.
- Any Photon/PUN/RPC removal is limited to the migrated skill path and preserves
  the same gameplay effect order and values.
- PRD 010 includes a follow-up note that full Photon/PUN/RPC cleanup is required
  in a separate future PRD.
- No gameplay balance values, targeting rules, cooldowns, public fields,
  serialized field names, scenes, prefabs, or generated files are changed.
- XML skill ownership is unchanged in PRD 010 except for the required pre-task
  that removes `Lizard`, `axe1`, and their legacy Topornik/Rzutnik skill
  assignments before implementation.

## Manual Test Plan

- Test one direct target skill and confirm cast, impact, and result feedback are
  ordered correctly.
- Test one instant AoE skill with one target, multiple targets, and no targets.
- Test `Double_Throw` and `Axe_Rain` and confirm projectile arrival drives
  impact timing.
- Test one stance toggle for Barbarian and one for Lizard.
- Test one self-cast status and confirm status feedback reveals on the caster
  after cast/impact timing.
- Test one heal and confirm heal feedback does not play hit/death feedback.
- Test one approach skill and confirm movement completes before cast/impact.
- Test one pull or reposition skill and confirm impact appears at the final
  affected position.
- Test one trap skill and confirm impact appears on the trap hex without unit
  hit reveal.
- Test `Stone_Throw` and confirm split/summon impact feedback does not alter
  existing amount/damage behavior.
- Test one passive/info skill and confirm it does not play active cast feedback.
- Confirm passive/deferred runtime trigger skills such as `Fire_Movement`,
  `Fire_Skin`, `Terrifying_Presence`, and `Rotting` are not migrated by this
  PRD.
- Confirm migrated skill paths no longer depend on their old skill-specific
  Photon/PUN/RPC branch for presentation or mirrored execution.
- Test missing catalog entries and clips and confirm no crash or warning spam.

## Relationship To Previous PRDs

- PRD 003 created the catalog-driven cast/projectile/impact VFX/SFX layer.
- PRD 009 created the shared frontend result reveal path and validated it on
  `Fire_Ball`, `Heavy_Fists`, basic ranged attack, melee attack, and
  counterattack.
- This PRD completes the migration for remaining active skills and formalizes
  the optional `Approach` phase discovered during `Heavy_Fists` validation.
- A follow-up PRD is required for full Photon/PUN/RPC cleanup across
  `CastManager`, `MouseControler`, `HexMap`, and related startup/input paths.

## Implementation - 2026-06-11

### What Changed

- `CastManager`: migrated remaining active PRD 010 skill paths from old direct presentation helpers to sequenced routines for direct, AoE, projectile, stance, movement/reposition, trap, and split/summon categories.
- `CastManager`: damaging migrated paths now build `FrontendResultReveal` snapshots instead of triggering immediate hit/death animation through old damage calls.
- `CastManager`: heal/status skills now use heal/status result reveals without faking hit/death animation.
- `SkillPresentationManager`: added sequenced caster/hex/unit effects, per-target projectile impact/reveal, and hex impact followed by result reveal.
- `FrontendResultReveal`: added `ResultKind` with values `Damage`, `Heal`, and `Status`; lower/higher tuning does not apply because this is a fixed result category, not a numeric Inspector value.
- `TosterHexUnit`: added helpers for heal/status reveals and fixed-damage reveal snapshots.
- `MouseControler`: made `DoMovesWithoutMoved` public so `Toxic_Fume` can use its existing local movement coroutine instead of its old RPC wrapper.
- No Inspector fields changed.

### Automatic Test

- No EditMode tests were added. This migration depends on Unity coroutines, scene-owned `SkillPresentationManager`, `TosterView`, live `HexClass`/unit setup, and catalog/Animator data.
- Automatic checks performed with targeted searches: no remaining old `CastManager` helper call names (`PresentCast`, `PresentImpact`, `PresentProjectile`, `AddHitTarget`, `hitTargets`), no old immediate migrated damage patterns (`DealMePURE(100)`, `DealMeDMG(SelectedT())`, `DealMeDMGDef(`, `ShootME(SelectedT(), false)`), and balanced braces in changed C# files.
- Unity tests are run manually by the user in Unity Test Runner; no new test entry is expected from this task.

### Unity Test

#### Unity Setup

- Open the project in Unity and let scripts compile.
- Ensure the battle scene has its existing `SkillPresentationManager`, `SkillPresentationCatalog`, unit prefabs, `TosterView` components, and Animator setup assigned as before.
- No new scene objects, components, prefabs, catalog entries, Animator Controllers, or Inspector field assignments are required by this implementation.

#### Play Mode Test

- Test one direct target skill such as `Hate` and confirm animation, cast,
  impact, and result feedback play in order.
- Test one remaining healing/status skill and confirm non-damage presentation
  does not play hit/death feedback.
- Test one AoE skill such as `Chope`, `Defence_Ritual`, or `Toxic_Fume` with zero, one, and multiple affected units.
- Test `Double_Throw` and `Axe_Rain` and confirm projectile arrival drives impact/reveal timing.
- Test `Rush`, `Slash`, `Long_Lick`, and `Force_Pull` and confirm movement/reposition happens before cast/impact presentation.
- Test `Spike_Trap`, `Rope_Trap`, and `Stone_Throw` and confirm trap/split presentation does not fabricate unit hit feedback.
- Test one Barbarian stance, one Lizard stance, `Rage`, `Stone_Stance`, or `Shapeshift` and confirm self/stance presentation remains safe.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-11_1958_010_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: Unity compile and Play Mode validation were not run and remain manual.
- Follow-up fixes applied after QA: none needed.

### Notes

- No gameplay float values, cooldowns, targeting ranges, XML skill ownership, scenes, prefabs, catalog assets, Animator Controllers, `.asmdef`, `.asmref`, generated Unity files, or binary assets were edited.
- Remaining live `Cleanse` RPC is outside the PRD 010 listed migration scope.
- Full Photon/PUN/RPC cleanup remains a separate follow-up PRD.

### Next Steps

- Run Unity compile in the already open Editor.
- Open Unity Test Runner if desired; no new EditMode test entry is expected.
- Run the Play Mode checklist above, prioritizing `Double_Throw`, `Axe_Rain`, `Slash`, `Toxic_Fume`, `Long_Lick`, and `Stone_Throw`.
