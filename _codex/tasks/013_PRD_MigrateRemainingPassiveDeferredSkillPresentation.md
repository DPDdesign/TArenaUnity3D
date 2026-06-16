# [TARENA] PRD: Migrate Remaining Passive And Deferred Skill Presentation

- Status: draft
- Type: PRD
- Area: Skills, Passive Skills, Deferred Presentation
- Label: needs-review
- Created: 2026-06-12

## Problem Statement

PRD 010 migrated the active cast paths to `SkillPresentationManager`, but some
skills still have no migrated presentation because their real trigger is not a
normal cast click. These skills are mostly autocast passives or deferred runtime
effects driven by movement, turn ticking, aura pulses, or combat calculation.

These are not a separate passive-presentation system. They are normal unit
skills that should use the same presentation layer as active skills. The only
difference is who starts the cast:

- active skills are started by player or AI skill selection,
- these skills are started automatically by runtime triggers.

The implementation should therefore route each runtime trigger into the normal
skill presentation flow instead of inventing separate passive events such as
`PassiveApplied` or `PassivePulse`.

This PRD tracks the remaining skill names, their runtime trigger, and the
presentation behavior that should be migrated.

## Verified Current Code Shape

- `TosterHexUnit.StartAutocast()` lists all eight skills below as autocasts and
  applies an initial one-turn `SpellOverTime`.
- `TosterHexUnit.CheckSpells()` ticks existing spells, removes expired spells,
  then re-applies autocast skills every turn.
- `SpellOverTime.SpecialThingOnStart()` and `SpecialThingOnEnd()` contain most
  of the real runtime logic for these skills.
- `Fire_Movement` also has a movement trigger in `TosterHexUnit.SetHex()`:
  when the unit leaves a hex, the old hex receives `Fire_Trap`.
- `Stone_Skin` and `Unstoppable_Light` affect combat calculation through
  `FlatDMGReduce` and `DefensePenetration`.

## Remaining Skills

| Skill | Runtime trigger | Presentation target |
| --- | --- | --- |
| `Cold_Blood` | End-turn / spell expiry. Splash damage is dealt around the caster based on received damage. | Cast as a normal AoE skill around the caster. It should play caster animation, then VFX/SFX and hit feedback on affected enemy units. |
| `Fire_Movement` | Movement step. Unit leaves `Fire_Trap` on the hex it moves out of. | Cast as a normal hex-targeted skill on every left/crossed hex. Usually VFX/SFX only, without interrupting movement with caster animation. |
| `Fire_Skin` | End-turn / spell expiry. Nearby enemy units receive movement, initiative, and resistance debuffs. | Cast as a normal AoE skill around the caster. It should play caster animation and show the aura/debuff on affected enemy units. |
| `Massochism` | End-turn / spell expiry. Damage taken during the cycle is converted into pure damage for a later attack. | No dedicated trigger VFX is required for the deferred damage-bank load. Future UI should instead expose the stored bonus damage value as a stack/count-style indicator on the owning unit or skill slot. Follow-up attack visualization can be considered separately if needed. |
| `Rotting` | End-turn / spell expiry. Caster loses HP but should not die from this passive tick. | Cast as a normal self skill on the caster with caster animation and self-decay VFX/SFX. |
| `Stone_Skin` | Combat damage calculation. Flat incoming damage reduction is active while the autocast spell is present. | At minimum cast as a normal self skill when the effect is active. If damage reduction feedback is added, it should be a normal impact-style presentation on the defender when incoming damage is reduced. |
| `Terrifying_Presence` | End-turn / spell expiry. Nearby units lose counterattack capability through a timed debuff. | Cast as a normal AoE skill around the caster. It should play caster animation and show fear/debuff feedback on affected enemy units. |
| `Unstoppable_Light` | Combat damage calculation. Attacks ignore most target defense while active. | Can be migrated into the normal self-skill presentation path for consistency, but does not need extra feedback if the effect reads clearly enough through attacks. |

## Presentation Decisions

- Runtime-triggered skills must use the same `SkillPresentationManager` and
  `SkillPresentationCatalog` model as active skills.
- Do not add a separate passive presentation event taxonomy. These are normal
  skills with automatic triggers.
- End-turn triggered skills should play caster animations for every unit that
  owns and triggers that skill.
- Movement-triggered skills may skip caster animation when playing the animation
  would interrupt movement readability or pacing.
- Combat-calculation-triggered feedback, such as `Stone_Skin` reducing incoming
  damage, should be impact-style feedback on the unit that received the attack.
- Enemy units and AI-controlled units must use the same presentation route as
  player units. Ownership should not change presentation behavior.

## Future PRD Candidate

End-turn skill casting order should be verified and probably tracked in a
separate PRD. The desired direction is that end-turn runtime casts execute in
initiative / movement-queue order, using the same normal cast presentation rules
as active skills.

That topic touches `TurnManager`, team turn ticking, and action sequencing, so
it should not be solved inside this presentation-migration PRD unless the
implementation naturally exposes a small safe hook.

## Scope Notes

- Do not change gameplay values, durations, ranges, damage, cooldowns, or status
  math.
- Do not require player-click targeting for these skills.
- Presentation should attach to the real automatic trigger point: movement
  trail, end-turn trigger, aura effect, or combat calculation.
- Keep `SkillPresentationCatalog` skill ids identical to the listed names.
- Keep skill ownership sourced from XML skill ids; the presentation catalog must
  not decide which unit owns which skill.

## Implementation - 2026-06-12

### What Changed

- Runtime-triggered passive unit skills now route through the existing
  `SkillPresentationManager` instead of a separate passive presentation system.
- `Cold_Blood`, `Fire_Skin`, and `Terrifying_Presence` build normal
  `FrontendResultReveal` lists and play sequenced instant-hit skill
  presentation with the owning unit's skill-slot animation.
- `Massochism` and `Rotting` play sequenced caster/self effects when their
  end-turn spell cycle expires.
- Future UI follow-up: add a `Massochism` stored-bonus-damage indicator instead
  of introducing extra trigger-time VFX.
- `Fire_Movement` plays a hex-targeted skill presentation on the hex the unit
  leaves when it creates `Fire_Trap`.
- `Stone_Skin` damage-reduction feedback is tied to actual damage reveal timing
  instead of the shared damage-calculation helpers, so AI/simulation damage
  queries should not spawn false VFX.
- `Unstoppable_Light` remains intentionally presentation-light; PRD 013 treats
  it as complete without additional VFX/SFX.
- `_codex/Documentation/SkillPresentationSetup.html` now marks the PRD 013
  skills as migrated and documents the required catalog setup.

### Automatic Test

- No automated tests were added. The touched behavior depends on Unity scene
  objects, unit views, `SkillPresentationManager`, catalog entries, and runtime
  turn/movement flow.
- Static text verification was performed for the new presentation call sites
  and the updated HTML rows.

### Unity Test

#### Unity Setup

- Ensure the battle scene has a `SkillPresentationManager` with a
  `SkillPresentationCatalog` assigned.
- Add catalog entries for `Cold_Blood`, `Fire_Movement`, `Fire_Skin`,
  `Massochism`, `Rotting`, `Stone_Skin`, and `Terrifying_Presence`.
- Assign cast/impact VFX and SFX according to
  `_codex/Documentation/SkillPresentationSetup.html`.

#### Play Mode Test

- Start a match with units that own the PRD 013 skills.
- Move a `Fire_Movement` unit and verify each left hex can play the configured
  fire-trail impact while still creating `Fire_Trap`.
- Advance turns and verify `Cold_Blood`, `Fire_Skin`,
  `Terrifying_Presence`, `Massochism`, and `Rotting` trigger through normal
  skill presentation instead of silently applying logic.
- Attack a `Stone_Skin` unit and verify the configured impact feedback appears
  when damage is applied.

### QA Verdict

- Formal QA review was not run in this pass.

### Notes

- Gameplay values, durations, ranges, damage formulas, cooldowns, and XML skill
  ownership were not changed.
- End-turn ordering by initiative remains a separate future PRD, as agreed.
- Status target selection keeps the existing runtime behavior; this pass does
  not change ally/enemy filtering.

### Next Steps

- Wire the catalog entries in Unity.
- Run the Play Mode checks above in the Unity Editor.
- Create the separate PRD for initiative-ordered end-turn runtime casts.
