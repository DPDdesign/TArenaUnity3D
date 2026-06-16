# [TARENA] Coding Completion - PRD 010 Migrate Remaining Skill Impact Routines

## Task

- `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/FrontendResultReveal.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`

## What Changed

- Extended `FrontendResultReveal` with `FrontendResultRevealKind` values for `Damage`, `Heal`, and `Status`.
- Kept hit/death animation reveal limited to damage results; heal/status results can now be scheduled through the shared result path without playing hit/death feedback.
- Added `BuildHealFrontendReveal`, `BuildStatusFrontendReveal`, and `DealMeDMGDefForFrontendReveal` helpers on `TosterHexUnit`.
- Added sequenced presentation routines for:
  - per-target projectile impact/reveal,
  - caster/hex/unit location effects,
  - hex/location impact followed by frontend result reveals.
- Migrated remaining listed active `CastManager` skills away from old direct PRD 003 helper calls:
  - direct/heal/status: `Skill1`, `Skill3`, `Hate`, `Tough_Skin`, `Blind_by_light`;
  - AoE/status/multi-target: `Skill2`, `Chope`, `Insult`, `Tank_Skill1`, `Defence_Ritual`, `Toxic_Fume`;
  - projectile: `Double_Throw`, `Axe_Rain`;
  - stance/self-cast: `Range_Stance_Barb`, `Melee_Stance_Barb`, `Range_Stance_Lizard`, `Melee_Stance_Lizard`, `Rage`, `Stone_Stance`, `Shapeshift`;
  - movement/reposition/trap/split: `TeleportOT`, `Rush`, `Slash`, `Tank_Skill2`, `Force_Pull`, `Long_Lick`, `Spike_Trap`, `Rope_Trap`, `Stone_Throw`.
- Converted PRD-owned skill RPC dispatches with available local equivalents to local calls:
  - `Rush`, `Slash`, `Hate`, `Tough_Skin`, `Force_Pull`, `Long_Lick`;
  - `Tank_Skill1` and `Toxic_Fume` now use local movement coroutines before sequenced presentation.
- Changed `MouseControler.DoMovesWithoutMoved` to public so `Toxic_Fume` can run its existing local movement coroutine without using the old RPC wrapper.

## Routine Category Checklist

- Direct damage: `Skill1`.
- Heal: `Skill3`.
- Direct status: `Hate`, `Tough_Skin`, `Blind_by_light`.
- Instant AoE damage/status: `Skill2`, `Chope`, `Insult`, `Tank_Skill1`, `Defence_Ritual`, `Toxic_Fume`.
- Projectile or thrown multi-target: `Double_Throw`, `Axe_Rain`.
- Stance/self-cast: `Range_Stance_Barb`, `Melee_Stance_Barb`, `Range_Stance_Lizard`, `Melee_Stance_Lizard`, `Rage`, `Stone_Stance`, `Shapeshift`.
- Movement, approach, pull, teleport, or reposition: `TeleportOT`, `Rush`, `Slash`, `Tank_Skill2`, `Force_Pull`, `Long_Lick`.
- Trap, summon, or split: `Spike_Trap`, `Rope_Trap`, `Stone_Throw`.
- Passive/deferred not migrated: `Cold_Blood`, `Massochism`, `Unstoppable_Light`, `Stone_Skin`, `Fire_Movement`, `Fire_Skin`, `Terrifying_Presence`, `Rotting`.

## Checks Performed

- Searched `CastManager.cs` for removed old helper call names:
  - `PresentCast`
  - `PresentImpact`
  - `PresentProjectile`
  - `AddHitTarget`
  - `hitTargets`
- Searched `CastManager.cs` for remaining old immediate migrated damage patterns:
  - `DealMePURE(100)`
  - `DealMeDMG(SelectedT())`
  - `DealMeDMGDef(`
  - `ShootME(SelectedT(), false)`
- Searched `CastManager.cs` for remaining migrated direct RPC calls. Remaining live RPC is `Cleanse`, which is outside the PRD 010 listed migration scope.

## Tests

- No EditMode tests added yet. The changed behavior depends on Unity coroutines, scene-owned `SkillPresentationManager`, `TosterView`, `HexClass`, and live unit/view setup.
- Unity compile and Play Mode validation must be run manually in the Unity Editor.

## Manual Unity Test Focus

- Direct target: `Skill1` or `Hate`.
- Heal: `Skill3`.
- AoE: `Chope`, `Defence_Ritual`, or `Toxic_Fume`.
- Projectile: `Double_Throw` and `Axe_Rain`.
- Approach/reposition: `Rush`, `Slash`, `Long_Lick`, `Force_Pull`.
- Trap/split: `Spike_Trap`, `Rope_Trap`, `Stone_Throw`.
- Stance/self-cast: one Barbarian stance, one Lizard stance, `Rage`, `Stone_Stance`, or `Shapeshift`.

## Known Exclusions

- No scenes, prefabs, catalog assets, Animator Controllers, `.asmdef`, `.asmref`, generated Unity files, XML unit ownership, or gameplay float values were edited.
- Full Photon/PUN/RPC cleanup remains a separate follow-up PRD.
