# [TARENA] PRD: Audit Unified Skill Presentation Path

- Status: draft
- Type: PRD
- Area: Skills, Presentation, Frontend Reveal, Architecture Audit
- Label: ready-for-agent
- Related: `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`
- Related: `_codex/tasks/013_PRD_MigrateRemainingPassiveDeferredSkillPresentation.md`
- Related: `_codex/tasks/014_PRD_SkillPresentationWeaponTrails.md`

## Problem Statement

Skill presentation is intended to be one coherent layer that owns cast feedback,
projectile feedback, impact feedback, SFX, and target reaction reveal. Recent
testing showed that this is not yet guaranteed for every skill.

The concrete symptom was `Force_Pull`: its impact VFX appeared at the teleported
target position because it used a location-impact presentation call, but its
target reaction did not play because that call did not carry a frontend result
reveal. This exposed a broader risk: some skill paths may still trigger only a
piece of presentation, or may use direct gameplay/presentation calls that bypass
the shared catalog-driven routine.

From the player's perspective, this makes skills inconsistent. Some skills show
VFX/SFX and target reaction, some show only VFX/SFX, some show direct hit/death,
and some may still rely on old workarounds. From the developer's perspective,
adding or debugging skill feedback remains error-prone because callers must know
which helper includes which parts of the presentation flow.

## Solution

Audit every current skill execution path and classify whether it uses the single
intended presentation route. Then migrate or document every exception so that
all skills use one shared presentation layer for:

1. selected skill id,
2. caster,
3. target unit and/or target hex,
4. optional movement, teleport, pull, summon, trap, or projectile phase,
5. impact VFX/SFX,
6. frontend result reveal for every affected unit,
7. catalog-driven target reaction.

The desired outcome is that skill code submits a skill presentation result, and
the presentation layer decides how to play cast, projectile, impact, SFX, and
target reaction from the skill catalog. Skill bodies should not separately
decide whether a target reaction is `hit`, `buff`, or `debuff`, and should not
use partial presentation helpers for unit-affecting effects.

## User Stories

1. As a player, I want every skill to show cause and effect consistently, so
   that I can understand what happened in combat.
2. As a player, I want a pulled or teleported unit to react when the skill
   affects it, so that movement-only effects still feel readable.
3. As a player, I want target reaction, VFX, and SFX to happen in the same
   ordered presentation, so that feedback does not feel disconnected.
4. As a player, I want buff skills to play buff reactions, so that positive
   effects are readable without looking like damage.
5. As a player, I want debuff skills to play debuff reactions, so that negative
   status effects are readable.
6. As a player, I want damaging skills to still play hit or death reactions, so
   that combat damage remains clear.
7. As a player, I want projectile skills to reveal results only after the
   projectile reaches impact, so that timing matches the visual cause.
8. As a player, I want area skills to reveal reactions only on actual affected
   units, so that empty areas do not show fake hits.
9. As a player, I want trap and summon skills to show location feedback without
   fake unit reactions, so that non-unit effects are not misleading.
10. As a content author, I want target reaction type to live in the skill
    presentation catalog, so that I can tune `Hit`, `Buff`, `Debuff`, or `None`
    without editing skill code.
11. As a content author, I want missing VFX/SFX entries to remain safe no-ops,
    so that incomplete catalog setup does not break skill execution.
12. As a designer, I want the XML skill id to remain the join key, so that skill
    ownership, UI, execution, and presentation stay aligned.
13. As a developer, I want one presentation route for all skills, so that adding
    target reaction does not require skill-specific workarounds.
14. As a developer, I want unit-affecting skills to always carry explicit result
    reveals, so that target reaction cannot be accidentally skipped.
15. As a developer, I want location-only effects to be explicitly marked as
    location-only, so that absence of a target reaction is intentional.
16. As a developer, I want old direct presentation calls to be either removed or
    documented as allowed exceptions, so that future work does not copy them.
17. As a developer, I want old direct damage calls to be audited, so that hit or
    death feedback does not bypass the shared reveal path.
18. As a developer, I want deferred passive effects to be classified separately,
    so that runtime triggers are not hidden outside the normal skill audit.
19. As QA, I want a skill-by-skill checklist of routine category, catalog entry,
    impact anchor, target reaction, and reveal behavior, so that Play Mode
    testing can be systematic.
20. As QA, I want representative tests for direct, area, projectile, movement,
    pull, teleport, stance, trap, summon, and passive/deferred skills, so that
    no category remains unverified.

## Implementation Decisions

- Treat this PRD as an audit-first PRD. The first implementation step is a
  full skill presentation path inventory, not another isolated fix.
- The audit must include active cast skills, movement/reposition skills,
  trap/summon/split skills, stance/self-cast skills, basic attack integration,
  counterattack integration, ranged attack integration, and deferred/passive
  runtime triggers.
- The expected final interface is one deep skill presentation module that takes
  a skill presentation context and owns cast, projectile, impact, result reveal,
  and target reaction playback.
- The skill presentation context should include skill id, caster, target hex,
  target units, affected units, movement or reposition result, projectile
  intent, result reveals, and whether a location-only effect is intentional.
- The skill presentation catalog remains the authoring source for VFX, SFX,
  impact anchor, timing, and target reaction. Skill logic must not hard-code
  `Hit`, `Buff`, or `Debuff` for catalog-owned presentation decisions.
- Result kind and target reaction remain separate concepts. Result kind says
  what gameplay result happened. Target reaction says which frontend animation
  should play.
- Unit-affecting skills must pass at least one frontend result reveal for every
  unit that should react, even when the gameplay result is movement, pull,
  teleport, taunt, buff, debuff, or status rather than damage.
- Location-only skills must use location impact without fabricating unit result
  reveals. This is allowed only when the skill genuinely affects a hex, trap,
  summon point, terrain marker, or empty area.
- Partial presentation helpers may remain as internal implementation details of
  the presentation module, but skill bodies should not use them directly when a
  full routine is required.
- Direct calls that only play cast, only play impact, or only play projectile
  feedback are considered workarounds unless the skill is documented as
  location-only or caster-only.
- Direct animator calls from skill bodies are considered workarounds unless
  they are replaced by catalog-driven caster or target reaction playback.
- Direct damage methods that immediately play hit/death animation must be
  audited and either routed through frontend result reveal or documented as
  non-skill legacy combat paths.
- Deferred passive effects are not exempt. If they apply a unit-facing result,
  their trigger point should submit result reveal through the same presentation
  layer.
- Do not change gameplay balance values, cooldowns, targeting ranges, movement
  distances, status modifier values, XML skill ownership, or public/serialized
  field names.
- Do not edit Unity scenes, prefabs, Animator Controllers, materials,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.
- If the audit finds a legitimate exception, document it in the PRD completion
  notes with the reason and the manual Play Mode check that proves it is safe.

## Testing Decisions

- The main test surface is external behavior in Unity Play Mode: a skill is
  committed and the player sees ordered cast, projectile, impact, SFX, and
  target reaction feedback.
- Automated tests are optional only if a deterministic non-Unity seam exists.
  Do not create or edit `.asmdef` or `.asmref` to force test assembly setup.
- The audit should produce a skill-by-skill checklist with these columns:
  skill id, routine category, current presentation entry, impact anchor, target
  reaction, result reveal kind, affected unit source, and exception status.
- Static checks should search for direct usage of old or partial presentation
  calls from skill bodies.
- Static checks should search for direct damage calls that play immediate
  frontend feedback instead of producing frontend result reveals.
- Static checks should search for direct animator calls from skill execution
  code.
- Manual Play Mode testing should cover at least one skill from each category:
  direct target, instant AoE, projectile, multi-projectile, movement/approach,
  pull/reposition, teleport, stance/self-cast, trap, summon/split, passive
  deferred damage, passive deferred status, and empty AoE.
- `Force_Pull` should be a required regression check because it exposed the
  difference between impact VFX and target reaction reveal.
- `Fire_Ball` should remain a required regression check because it is the known
  good projectile path.
- `Heavy_Fists` should remain a required regression check because it combines
  movement setup with later impact/reveal.
- Testing should confirm that missing catalog entries do not crash gameplay, but
  also that missing catalog entries are reported in the audit checklist.

## Out of Scope

- Full skill-system rewrite.
- Replacing XML skill assignment.
- Renaming skill ids.
- Changing gameplay math, balance, cooldowns, targeting, movement, or status
  values.
- Creating final VFX/SFX assets.
- Editing Unity scenes, prefabs, Animator Controllers, materials, `.asmdef`,
  `.asmref`, generated files, or binary assets.
- Full Photon/PUN/RPC cleanup outside the skill presentation path.
- Adding random VFX/SFX variants or new content authoring models.
- Solving unrelated input bugs unless they directly block the skill
  presentation audit.

## Further Notes

This PRD exists because PRD 010 closed the first broad migration, but later
manual validation showed that "uses the presentation manager" is not strict
enough. A skill can use the manager for impact VFX/SFX while still bypassing
target reaction reveal.

The final standard should be stronger: every skill must either submit a full
presentation context through the shared skill presentation layer, or be
documented as an intentional location-only or caster-only exception.

The architecture goal is locality. Presentation decisions should be concentrated
in the skill presentation module and catalog, not spread through individual
skill bodies.
