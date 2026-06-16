# 003 PRD Skill VFX/SFX Flow Coding Agent Completion

## Task

`_codex/tasks/archive/003_PRD_SkillVfxSfxFlow.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `_codex/tasks/archive/003_PRD_SkillVfxSfxFlow.md`

## Systems Touched

- Skill VFX/SFX presentation data model.
- Scene-level skill presentation playback.
- `CastManager` skill commit call sites.
- Target-affecting AoE impact placement.
- Basic ranged attack projectile presentation.
- Legacy projectile call sites for `Axe_Rain`, `Double_Throw`, and `Fire_Ball`.

## Behavior Or Setup Summary

Added an Inspector-authored `SkillPresentationCatalog` `ScriptableObject` with
per-skill entries for cast, projectile, and impact VFX/SFX. The catalog also has
a `defaultBasicRangedAttackEntry` for ordinary ranged shots, which are not XML
skills.

Added a scene-level `SkillPresentationManager` that references the catalog,
spawns VFX prefabs, plays SFX through `AudioSource.PlayOneShot`, moves projectile
VFX from caster to target/hex without Rigidbody physics, and warns once when the
scene manager/catalog/audio source setup is missing.

Wired representative active `CastManager` skill commit points across direct
target, self-cast, AoE, projectile, multi-target, movement/teleport/pull, trap,
summon/split, stance toggle, and RPC-deferred skill paths. Passive/info skill
selection remains no-op for active presentation.

Updated target-affecting AoE presentation so impact VFX is emitted on each
actually affected unit. If no unit is affected, the skill does not fall back to
an impact on the selected hex. Impact SFX is played once per AoE commit to avoid
stacking the same clip for every target.

Migrated basic ranged attack playback from `TosterHexUnit.Projectile` /
`HexMap.ThrowSomething(...)` to `SkillPresentationManager.PlayBasicRangedAttack`.
The old public serialized projectile fields and unused `HexMap.ThrowSomething`
method were left in place to avoid scene/prefab serialized-surface churn.

## Unity Checks

Not run. Project rules say the user compiles and tests inside Unity unless a
specific Unity test command is allowed.

Recommended manual checks in Unity:

- Follow the detailed `Skill VFX/SFX Setup` section in
  `_codex/Documentation/User_Setup_Guide.md`.
- Create a `SkillPresentationCatalog` asset from `TArena/Skill Presentation Catalog`.
- Add one `SkillPresentationManager` with an `AudioSource` to the battle scene
  and assign the catalog.
- Configure entries for `Range_Stance_Barb`, `Double_Throw`, `Axe_Rain`,
  `Fire_Ball`, `Skill3`, `Skill2`, `Spike_Trap`, `Stone_Throw`, and
  `defaultBasicRangedAttackEntry`.
- In Play Mode, test self-cast/status, instant target, AoE, projectile skill,
  stance toggle, basic ranged attack, movement/teleport/pull, trap placement,
  summon/split, multi-target, and one passive/info skill.
- Confirm target-affecting AoE impact VFX appears on each affected unit and
  does not appear on the selected hex when no unit is hit.
- Confirm missing per-skill entries silently no-op and a missing manager/catalog
  warns once without crashing.
- Confirm combat attack, hit, and death SFX still play.

## Intentionally Not Included

- No Unity scene, prefab, material, controller, `.inputactions`, `.asmdef`, or
  `.asmref` edits.
- No generated `.meta` file edits.
- No final VFX/SFX assets or catalog asset creation.
- No catalog/XML validator.
- No gameplay math, cooldown, targeting, movement, or turn-consumption changes.
- No Git, `dotnet`, Unity build, package restore, or SDK installation commands.
