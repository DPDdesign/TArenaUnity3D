# 003 PRD Skill VFX/SFX Flow QA Architecture Review

## Reviewed Protocol

`_codex/tasks/QA/003_PRD_SkillVfxSfxFlow_Completion.md`

## Verdict

Pass with Unity validation required.

## Findings

- Presentation data is centralized in one `ScriptableObject` catalog and does
  not decide skill ownership. XML skill strings remain the join key for skills.
- Basic ranged attack has a separate catalog default entry, matching the PRD
  exception for non-XML ordinary ranged shots.
- `CastManager` changes are additive at commit points and do not change damage,
  cooldown, targeting, movement, or status values.
- Target-affecting AoE skills now derive impact placement from the actual
  affected-unit list. Empty AoE hits play no selected-hex impact fallback.
- AoE impact SFX is intentionally limited to one hit target while impact VFX can
  appear on every affected unit.
- Legacy skill projectile calls to `Axe(...)`, `FireBall(...)`, and
  `CastManager.Projectiles[index]` are no longer active in migrated call sites.
- `TosterHexUnit.ShootME(..., true)` no longer calls `HexMap.ThrowSomething`;
  basic ranged attack uses `SkillPresentationManager.PlayBasicRangedAttack`.
- Public serialized projectile fields were retained to avoid Unity scene/prefab
  reference churn. This leaves harmless legacy surface, not an active playback
  path.

## Risks

- Unity compile was not run by agent policy; the user must validate in Unity.
- The catalog asset and scene manager are not created automatically because PRD
  explicitly avoids scene/asset edits.
- `CastManager` remains a large legacy file, so future skill additions still need
  explicit presentation call-site review.
- Some legacy methods still route through local RPC handlers. Presentation was
  added to representative handler/commit points, but Unity Play Mode should
  check for accidental double-play on those paths.

## Recommended Next Slice

Create and wire the first catalog asset in Unity with placeholder VFX/SFX for:

- `defaultBasicRangedAttackEntry`,
- one stance toggle,
- one direct target skill,
- one AoE skill,
- one projectile skill,
- one trap or summon/split skill.
