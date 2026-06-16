# [TARENA] PRD: Rush Early Cast SFX And Run Movement Override

- Status: draft
- Type: PRD
- Area: Skills, Combat Presentation, Movement Animation, SFX Timing
- Label: ready-for-agent
- Related: `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`
- Related: `_codex/tasks/015_PRD_AuditUnifiedSkillPresentationPath.md`
- Related: `_codex/tasks/archive/003_PRD_SkillVfxSfxFlow.md`
- Related: `_codex/tasks/archive/009_PRD_SkillImpactRoutineFlow.md`

## Architecture Update - 2026-06-13

This update supersedes the earlier early-cast-SFX direction in this PRD.

For movement/approach skills such as `Rush` and `Heavy_Fists`, the visual
`Cast` phase starts only after the movement/approach completes. The commit
starts movement and may play a commit-time cast SFX when the skill needs an
immediate audio acknowledgement. After arrival, the normal sequenced cast phase
plays: selected `skillN` animation, cast VFX, weapon trail, then impact
presentation according to the existing skill presentation timing. If cast SFX
already played at commit time, it must not replay after arrival.

`Rush` still uses the temporary movement animation override to play `run`
during movement instead of `walk`.

## Problem Statement

`Rush` currently presents too late and moves with the wrong animation language.
The skill is a forward pressure move: the Rusher commits, starts moving forward,
then resolves the skill presentation after arrival. At the moment, the cast SFX
is tied to the later sequenced presentation, so it cannot clearly communicate
the moment the player committed the skill. The movement path also uses the
normal walking animation, which makes a rush read like ordinary movement.

From the player's perspective, the intended order is:

1. The skill is committed and the unit starts movement.
2. The unit moves through the existing movement system, but visually uses `run`
   instead of `walk`.
3. After arrival, the normal visual cast phase starts: the caster plays the
   selected `skillN` animation, cast VFX, and weapon trail.
4. Impact feedback resolves after the post-arrival cast phase.

## Solution

Adjust the `Rush` presentation sequence without changing targeting, pathing,
movement distance, cooldowns, status values, or damage behavior.

`Rush` should play its configured cast SFX at movement commit time. Its
configured cast VFX and weapon trail belong to the normal post-movement cast
phase, and cast SFX must not replay after arrival.

During the movement phase, `Rush` should use the normal movement mechanism, but
with a temporary movement animation override set to `run`. The override should
be a small general mechanism on the unit movement model, not a Rush-only hack.
Normal movement remains `walk` when no override is active.

After movement completes, `Rush` should use the normal sequenced cast
presentation path without replaying cast SFX. That path plays the selected skill
animation (`skill1`, `skill2`, etc. from the current selected skill slot), cast
VFX, weapon trail, and then impact feedback.

## User Stories

1. As a player, I want `Rush` to start moving immediately when I commit it, so
   that the action feels responsive.
2. As a player, I want the Rusher to run during the rush movement, so that the
   movement reads as an aggressive skill rather than ordinary walking.
3. As a player, I want the skill animation to happen after the Rusher reaches
   the destination, so that the payoff happens at the correct place.
4. As a player, I want the impact VFX/SFX to happen after arrival, so that the
   feedback matches the actual rush result.
5. As a content author, I want post-arrival `Rush` feedback to use the existing
   `Rush` presentation catalog entry, so that I do not need a second skill
   identity.
6. As a content author, I want `Rush` cast VFX and weapon trail to play in the
   normal cast phase after movement, so that setup matches other approach-style
   skills.
7. As a developer, I want the movement animation override to be reusable, so
   that future movement skills can opt into `run` or another movement animation
   without duplicating Rush-specific code.
8. As a developer, I want ordinary movement to keep using `walk`, so that this
   task does not change baseline movement presentation.
9. As a developer, I want the override to be temporary and cleared after the
   movement coroutine finishes, so that later movement is not accidentally
   affected.
10. As QA, I want to verify that `Rush` plays cast SFX at commit and does not
    replay it after arrival, so that the presentation remains clean.
11. As QA, I want to verify that missing `Rush` SFX or presentation catalog data
    remains a safe no-op, so that incomplete Unity setup does not break the
    skill.
12. As QA, I want to verify both Rush cases, with and without an enemy at the
    target line, so that movement-only and attack-followup branches remain
    correct.

## Implementation Decisions

- Preserve the existing `Rush` gameplay flow: target selection, highlighted
  line, destination logic, temporary stat/status application, movement, optional
  target attack sequence, and turn state remain unchanged.
- Add a separate SFX-only commit path for `Rush`.
- Use the exact `Rush` skill id as the catalog lookup key for post-arrival cast
  and impact presentation.
- Treat `Rush` as an approach-style skill with immediate audio acknowledgement:
  commit SFX first, movement, post-arrival cast visuals/weapon trail, then
  impact.
- Add a general temporary movement animation override on the unit movement
  model. When no override is active, movement animation remains `walk`.
- `Rush` sets the movement animation override to `run` only for the duration of
  the movement coroutine and clears it afterward.
- The override must be cleared even if the movement target is invalid, the
  movement coroutine exits early, or the mover/presentation references are
  missing.
- Keep the movement system authoritative for position/path progress. The new
  override chooses animation state only; it must not alter movement speed,
  movement cost, pathfinding, facing, occupancy, or `Moved` state.
- Do not rename public or serialized fields.
- Do not edit Unity assets, prefabs, scenes, Animator Controllers, animation
  clips, materials, `.inputactions`, generated Unity files, `.asmdef`, or
  `.asmref`.

## Testing Decisions

- Unity compile and Play Mode validation are user-side for this project.
- A good test validates externally visible behavior: SFX timing, movement
  animation choice, post-arrival skill animation, impact timing, and absence of
  duplicate feedback.
- Manual Play Mode testing should cast `Rush` onto an empty valid highlighted
  hex and confirm:
  - cast SFX plays immediately at commit,
  - movement uses `run`,
  - no `walk` animation plays during the Rush movement,
  - `skillN`, cast VFX, and weapon trail play after arrival,
  - cast SFX does not replay after arrival,
  - impact feedback happens after arrival,
  - later ordinary movement still uses `walk`.
- Manual Play Mode testing should cast `Rush` toward an enemy and confirm:
  - movement uses `run`,
  - post-arrival visual cast and impact presentation happens before or
    alongside the intended target follow-up sequence according to the existing
    Rush timing,
  - hit/death/target reaction feedback is not duplicated.
- Manual Play Mode testing should run a normal non-Rush move after `Rush` and
  confirm the movement animation override was cleared.
- Manual Play Mode testing should temporarily test missing `Rush` cast SFX or
  missing catalog entry and confirm no crash or warning spam beyond existing
  scene setup warnings.
- No new automated test is required unless a pure C# seam is introduced for the
  movement animation override selection. If such a seam is added, test only the
  public behavior: override returns `run`, cleared override returns `walk`.

## Out of Scope

- Changing `Rush` damage, cooldown, target rules, movement distance, status
  values, or stat modifier values.
- Changing the general movement speed or movement timing.
- Adding new VFX/SFX assets.
- Editing or creating Unity catalog assets, prefabs, scenes, Animator
  Controllers, animation clips, or serialized asset references.
- Reworking the full skill presentation manager.
- Migrating unrelated movement skills.
- Building catalog/XML validation tooling.
- Global Photon/PUN/RPC cleanup.

## Further Notes

Diagnosis from code inspection:

- The existing `Rush` coroutine waits for movement to finish before calling the
  sequenced skill presentation. That is now the intended approach-skill
  architecture.
- The movement code currently plays `walk` directly during hex updates, so
  `Rush` needs a small animation selection seam rather than changes to pathing.
- Existing PRD 010 behavior says approach/movement skills should present visual
  cast/impact after movement. This task follows that rule for `Rush`, while
  still allowing commit-time cast SFX as immediate audio acknowledgement.

Grill decisions confirmed by the user:

1. `Rush` cast SFX plays at commit time and does not replay after arrival.
2. The post-arrival phase is the visual cast phase: play `skillN`, cast VFX,
   weapon trail, then impact presentation.
3. The `walk` to `run` movement animation swap should be a general temporary
   movement override, not a one-off Rush hack.

Minimal model:

- Unit movement animation override
- default movement animation = `walk`
- optional temporary override = `run`
- clear override after movement

- `Rush`
- commit = play `Rush` cast SFX and start movement
- movement = existing movement path with override `run`
- arrival = normal visual cast phase with selected `skillN`, cast VFX, weapon trail
- result = impact presentation/reveal

## Implementation - 2026-06-13

### What Changed

No Inspector fields changed.

`TosterHexUnit`:

- Added private `movementAnimationOverrideState` for movement animation
  selection. `null` or empty means normal `walk`; non-empty values such as
  `run` play that Animator state during movement. Tuning hint: use exact
  Animator state names only.
- Added `SetMovementAnimationOverride`, `ClearMovementAnimationOverride`, and
  `GetMovementAnimationState`.
- Changed `SetHex(...)` movement playback from hardcoded `walk` to
  `GetMovementAnimationState()`.

`SkillPresentationManager`:

- Added `PlayCastSfxOnly(skillId)` for commit-time audio acknowledgement.
- Added `PlaySequencedHexEffectWithoutCastSfx(...)` for post-arrival visual
  cast and impact playback without repeating cast SFX.
- Weapon trail playback now treats either `useWeaponTrails` or legacy
  `weaponTrailKey` as opt-in, preserving existing catalog entries.

`CastManager`:

- `RushMoveAttackAndPlayHexEffect(...)` plays `Rush` cast SFX at commit.
- Wrapped existing `DoMoves(...)` with temporary movement animation override
  `run`, cleared in `finally`.
- After movement, `RushMoveAttackAndPlayHexEffect(...)` calls the normal
  visual cast/impact path without repeating cast SFX.

### Automatic Test

No new EditMode tests were added. The repo has no project-owned EditMode test
assembly in the gameplay scripts, and creating `.asmdef`/`.asmref` files is out
of scope. The deterministic seam is small (`GetMovementAnimationState()`), but
adding a test harness would require test infrastructure changes.

Manual test runner check: in Unity, open `Window > General > Test Runner`, pick
`EditMode`, and run the existing available tests if desired. Expected result for
this task: no new PRD 017 test appears; Unity should compile the changed
scripts without C# errors.

### Unity Test

#### Unity Setup

- In the battle scene, keep the existing `SkillPresentationManager` with its
  `AudioSource` and assigned `SkillPresentationCatalog`.
- In the catalog entry for `Rush`, assign the intended `castSfx`.
- Confirm the Rusher Animator has `walk`, `run`, and the selected `skillN`
  state used by its skill slot.

#### Play Mode Test

- Cast `Rush` onto an empty highlighted valid hex.
- Expect `castSfx` immediately once at commit.
- Expect movement to use `run`, not `walk`.
- Expect selected `skillN`, cast VFX, weapon trail, and impact feedback after
  arrival.
- Expect `castSfx` not to replay after arrival.
- Move normally after Rush and expect normal movement to use `walk`.
- Cast `Rush` toward an enemy and expect the same sequence plus existing target
  follow-up behavior without duplicate hit feedback.

### QA Verdict

Final QA status: Pass.

QA report:
`_codex/tasks/QA/2026-06-13_1342_017_QA_ArchitectureReview.md`.

Actionable findings: none.

Non-blocking observations: Unity compile and Play Mode validation remain
manual. QA noted that content must have a `run` Animator state and configured
`Rush` cast SFX to fully validate the behavior.

Follow-up fixes applied: none needed.

### Notes

- No gameplay values, cooldowns, targeting, movement distance, movement speed,
  pathfinding, stat/status values, scenes, prefabs, Animator Controllers,
  animation clips, `.asmdef`, `.asmref`, or generated files were changed.
- Missing `SkillPresentationManager`, catalog, or `Rush` cast presentation remains a
  safe no-op under the existing presentation manager behavior.
- The movement override is general, but only `Rush` uses it in this task.

### Next Steps

- Let Unity compile scripts in the already open Editor.
- Optionally run existing EditMode tests in `Window > General > Test Runner`.
- Run the Play Mode checklist above for empty-hex Rush, enemy-target Rush, and
  normal movement after Rush.

## Fix - 2026-06-13

The initial implementation split `Rush` into immediate `castSfx` plus
post-arrival impact-only presentation. That prevented `WeaponTrail` from
playing with the post-arrival `skillN` animation because weapon trails are owned
by the normal cast phase.

The architecture was corrected again:

- `Rush` plays `castSfx` at movement commit.
- `Rush` then moves with movement animation override `run`.
- After movement, `Rush` plays selected `skillN`, cast VFX, weapon trail, and
  impact feedback without replaying cast SFX.
- Weapon trail activation now treats either `useWeaponTrails` or legacy
  `weaponTrailKey` as opt-in, so existing catalog entries such as `Chope` and
  `Rush` remain compatible without editing the catalog asset.

Manual validation should now expect `Rush` cast SFX at movement commit and
weapon trail after arrival with `skillN`.
