# [TARENA] Coding Completion - PRD 014 Weapon Trails

## Task

- `_codex/tasks/014_PRD_SkillPresentationWeaponTrails.md`

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterView.cs`

## What Changed

- Added optional weapon trail authoring fields to `SkillPresentationEntry`:
  - `weaponTrailKey`
  - `weaponTrailDurationSeconds`
- Added `TosterView.TryPlayWeaponTrail(...)`.
- `TosterView` now resolves a child `TrailRenderer` by matching the child
  GameObject name against `weaponTrailKey`, case-insensitive, including inactive
  children.
- Weapon trail playback enables the child GameObject and `TrailRenderer`, clears
  stale trail data, emits for the configured duration, then disables emission and
  clears the trail.
- Repeated casts restart the trail coroutine and preserve the original active
  and enabled state until the final duration completes.
- `SkillPresentationManager.PlayCast(...)` now starts the configured weapon
  trail at the same moment as existing cast VFX/SFX presentation.
- If `weaponTrailDurationSeconds <= 0`, the manager uses a default presentation
  duration of `0.35` seconds.
- If a configured trail key is missing under the caster `TosterView`, playback
  safely continues without the trail and logs at most one warning per missing
  key.

## Scope Boundaries

- No gameplay damage, cooldown, targeting, movement, or turn values changed.
- No skill ids or XML skill ownership changed.
- No Unity scenes, prefabs, materials, Animator Controllers, animation clips,
  `.asset`, `.asmdef`, `.asmref`, or generated Unity files were edited.
- No separate weapon presentation manager was added.
- No runtime trail prefab spawning or attachment was added.

## Static Checks

- Targeted search confirmed new weapon trail code is limited to the three
  intended scripts.
- Existing sequenced skill presentation remains routed through
  `SkillPresentationManager`.
- Weapon trail starts from the existing `PlayCast(...)` point, preserving the
  current `spellPresentationDelay` contract.

## Automatic Tests

- No EditMode tests were added before QA review.
- This first implementation is coroutine and `TrailRenderer` behavior tied to
  Unity components and scene/prefab setup.
- Unity compile and Play Mode validation remain manual in the Unity Editor per
  project policy.

## Manual Unity Validation Needed

1. In the skill presentation catalog, configure a physical skill such as
   `Slash`.
2. Set `weaponTrailKey` to the child GameObject name that contains the
   `TrailRenderer`, for example `WeaponTrail`.
3. Set `weaponTrailDurationSeconds`, for example `0.35`.
4. If the trail should start immediately with the animation, set
   `spellPresentationDelay = 0` for that entry.
5. In Play Mode, use the configured skill and confirm:
   - the trail appears on the weapon/view,
   - the trail disables after the duration,
   - repeated casts do not leave it enabled,
   - hit/death/result reveal still happens once,
   - missing trail setup does not block result reveal,
   - projectile skills still use projectile presentation normally.
