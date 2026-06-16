# 009 PRD Skill Impact Routine Flow QA Architecture Review

## Protocol

`_codex/tasks/QA/009_PRD_SkillImpactRoutineFlow_Completion.md`

## Verdict

Pass.

The implementation is scoped to the PRD 009 representative slice and does not
introduce a broad skill-system rewrite. The new reveal snapshot is small,
transport-agnostic, and independent of Photon/PUN. `SkillPresentationManager`
owns sequencing from cast/projectile/impact into reveal, while
`TosterHexUnit` remains the owner of damage application and hit/death animation
helpers.

## Files Reviewed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/FrontendResultReveal.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`

## Findings

No actionable architecture findings.

## Checks

- Shared reveal model is plain C# and has no Inspector-facing serialized
  surface.
- Basic attack and counterattack hit/death feedback now use the same reveal
  helper that skill hits use.
- Basic ranged attack damage still applies immediately, and catalog projectile
  impact now dispatches the captured reveal afterward.
- `Fire_Ball` no longer plays impact immediately after starting projectile
  travel; impact and hit reveal are sequenced through
  `PlaySequencedProjectileHits`.
- `Heavy_Fists` no longer routes representative damage through the legacy
  immediate `JustDmg` reveal path; it captures result reveal and plays it once
  through `PlaySequencedInstantHits`.
- Missing manager/catalog entries fall back to hit/death reveal instead of
  silently hiding combat result feedback.
- No scenes, prefabs, controllers, `.asmdef`, `.asmref`, `.inputactions`, XML
  skill ids, cooldowns, targeting values, movement values, or damage values were
  edited.

## Non-Blocking Observations

- `SkillPresentationManager` now also coordinates basic ranged reveal timing.
  This is acceptable for the current PRD slice because ranged projectile
  presentation already lives there, but a later cleanup could split generic
  frontend reveal orchestration from skill presentation if more non-skill action
  types migrate.
- `DealMePURE(false)` now defers immediate death flattening so death reveal can
  play later. This improves the migrated reveal model but should be watched in
  Play Mode for any legacy caller that expected instant flattening.
- No Unity compile or Play Mode validation was run by the agent per project
  policy.

## Recommended Manual Validation

- Compile in Unity and open the battle scene.
- Test melee attack, counterattack, basic ranged attack, `Fire_Ball`, and
  `Heavy_Fists`.
- Specifically watch lethal hits: logical removal should happen immediately,
  while the visible death reveal should still play at hit/impact timing.
