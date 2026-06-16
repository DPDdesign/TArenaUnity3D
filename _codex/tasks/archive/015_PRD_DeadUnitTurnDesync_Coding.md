# [TARENA] PRD: Prevent Dead Units From Receiving Turns

- Status: closed-implemented-pending-unity-validation
- Type: PRD / Coding
- Area: Turn Manager, Unit Death, Damage Over Time, Traps, Skill Execution Order
- Label: ready-for-agent

## Problem Statement

Players can see a unit die from deferred fire trap damage, including its death
visual and cleared amount label, but the turn system can still select that dead
unit as the active unit. The UI then asks the player to move a unit that is
already dead.

The concrete observed case was a `HeavyHitter` dying from passive
`Fire_Movement` / `Fire_Trap` damage over time. The backend damage/death state
updated enough to play the death visual, but the turn-selection path still let
the dead unit enter the current turn or queue.

This creates a gameplay desync between:

- damage/death resolution,
- turn queue selection,
- mouse input state,
- frontend death/VFX/SFX presentation timing.

## Solution

Dead or zero-amount units must be ineligible for turn selection, turn previews,
and new-turn reset logic. If damage over time kills a unit during new-turn
processing, the same new-turn pass must not reset that unit back into an
actionable state.

The smallest implementation should keep current gameplay values and presentation
behavior intact while hardening the existing turn-selection rules.

## User Stories

1. As a player, I want a dead unit to never receive an active turn, so that the
   battle flow does not ask me to move a corpse.
2. As a player, I want the top turn queue to exclude units that just died, so
   that the queue matches the board state.
3. As a player, I want damage over time deaths to be final before the next unit
   is selected, so that traps feel fair and readable.
4. As a player, I want `Fire_Trap` deaths to behave like normal combat deaths,
   so that passive hazards do not create special-case bugs.
5. As a player, I want a unit with amount `0` to be treated as dead by the turn
   system, so that labels and input state do not disagree.
6. As a designer, I want trap damage values, durations, and skill values to
   stay unchanged, so that this fix does not rebalance combat.
7. As a designer, I want current death visuals to remain unchanged, so that the
   bug fix does not create new art or animation requirements.
8. As a developer, I want one eligibility rule for turn selection, so that
   current turns and queue previews do not drift.
9. As a developer, I want new-turn status processing to re-check life after
   damage over time, so that `Moved` cannot be reset after death.
10. As a developer, I want the fix to stay inside existing turn/death modules,
    so that skill and presentation systems are not broadly refactored.
11. As QA, I want a reproducible Play Mode path using FireElemental fire trails,
    so that the original report can be verified.
12. As QA, I want normal alive units to still appear in the turn queue, so that
    the fix does not block legitimate turns.
13. As QA, I want manual checks for wait/defense/move after a DOT death, so that
    input state still advances to the next valid unit.
14. As a future maintainer, I want the remaining presentation-order risk
    documented, so that later VFX/SFX work can address it deliberately.
15. As a future maintainer, I want this task to avoid large architecture work,
    so that the local gameplay recovery path remains small and safe.

## Implementation Decisions

- Modify the existing team turn-selection module instead of adding a new system.
- Add a single local eligibility rule for selecting or previewing units in the
  turn queue: the unit must exist, must not be marked dead, and must have a
  positive amount.
- Apply that eligibility rule to current-turn selection and simulator/queue
  selection.
- During new-turn processing, check eligibility before ticking ongoing effects,
  then check it again after ongoing effects because damage over time can kill
  the unit.
- Keep the existing death implementation authoritative for setting dead state,
  removing the unit from its hex, clearing amount text, and playing death
  presentation.
- Treat `Amount <= 0` as non-actionable even if a legacy path forgot to set the
  dead flag.
- Keep fire trap, fire movement, cooldown, initiative, movement speed, damage,
  and presentation timing values unchanged.
- Do not redesign skill execution order in this task. Document the remaining
  architecture risk: several skill and deferred-effect paths still calculate
  backend results before asynchronous frontend reveal/VFX completes.

## Testing Decisions

- Test external behavior: after a unit dies from deferred damage, it is not
  selected for a turn and does not appear as a valid queue candidate.
- The best regression seam is the team turn-selection module because it owns
  the decision that allowed dead units back into the turn.
- If no existing project EditMode test assembly is available, do not add a new
  `.asmdef` or test assembly in this task. Use manual Unity Play Mode checks and
  targeted static inspections instead.
- Manual Play Mode verification should reproduce the original case:
  FireElemental leaves fire, HeavyHitter enters it, deferred fire damage kills
  HeavyHitter, then the next active unit is not HeavyHitter.
- QA should also check that alive units still cycle normally after the DOT death.

## Out of Scope

- No gameplay float/value changes.
- No trap damage, trap duration, cooldown, initiative, or movement rebalance.
- No Unity asset, prefab, scene, material, Animator Controller, `.asmdef`, or
  `.asmref` edits.
- No full `MouseControler` refactor.
- No full `TurnManager` rewrite.
- No full skill execution pipeline rewrite.
- No global frontend/backend sequencing overhaul.
- No Photon/PUN/RPC cleanup.
- No new VFX/SFX assets.

## Further Notes

Observed risk chain:

1. A fire movement trail creates a fire trap.
2. Entering that trap adds damage over time.
3. New-turn status processing ticks the damage over time.
4. Damage can call death logic and mark the unit dead.
5. The same new-turn pass can then reset movement state unless it re-checks
   death.
6. Turn selection can choose the dead unit unless it filters dead or zero-amount
   units.

Acceptance criteria:

- A unit killed by fire trap damage over time keeps `Moved` in a non-actionable
  state after death.
- A unit with `isDead == true` is never returned by current-turn selection.
- A unit with `Amount <= 0` is never returned by current-turn selection.
- Queue preview does not show newly dead zero-amount units.
- Alive units continue to receive turns normally.
- No public or serialized fields are renamed.
- No gameplay values are changed.

## Implementation - 2026-06-12

### What Changed

- Added one local turn eligibility rule in
  `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`:
  a unit must be non-null, not dead, and have `Amount > 0`.
- Routed `AskForUnit()` through that rule, so current turn selection cannot
  return dead or zero-amount units.
- Routed `AskForUnitSimulator()` through the same rule, so queue preview uses
  the same eligibility as real turn selection.
- Updated `NewTurn()` to re-check eligibility after `CheckSpells()`, preventing
  DOT or trap death from being followed by `Moved = false`.
- Updated `IsMyTeamDEAD()` to use the same eligibility rule, so legacy
  zero-amount units are treated as non-actionable for win-state checks.

### Automatic Test

- No Unity or `dotnet` build was run, per project rules.
- No EditMode tests were added because this project currently exposes only
  vendor plugin tests and no local TArena gameplay test assembly.
- Static checks performed:
  - inspected `TeamClass.cs` after the edit,
  - searched `TurnManager` and `MouseControler` turn-selection call sites,
  - confirmed current turn selection and queue preview flow through
    `TeamClass` eligibility.

### Unity Test

Unity Setup:

- No Unity assets, prefabs, scenes, materials, controllers, `.asmdef`, or
  `.asmref` files were changed.
- No public or serialized fields were renamed.
- No gameplay values were changed.

Play Mode Test:

- Reproduce the reported case: FireElemental creates fire, HeavyHitter enters
  it, deferred fire damage kills HeavyHitter.
- Verify HeavyHitter death presentation still plays and its amount label remains
  non-actionable.
- Verify the next selected unit is not HeavyHitter.
- Verify the top turn queue does not show the dead zero-amount HeavyHitter as a
  valid upcoming unit.
- Verify normal alive units still cycle through move, wait, and defense.

### QA Verdict

- Final QA verdict: Pass with residual risk noted.
- QA report:
  `_codex/tasks/QA/2026-06-12_1205_015_QA_ArchitectureReview.md`.
- Completion protocol:
  `_codex/tasks/QA/2026-06-12_1205_015_CodingCompletion_DeadUnitTurnDesync.md`.

### Notes

- The fix is intentionally narrow and lives in the team turn-selection module.
- Broader SFX/VFX/backend sequencing remains out of scope for this task.
- Existing `MouseControler` win-panel flow still continues after setting the
  win UI; this is a separate game-end-control-flow risk, not part of the
  reported live-combat dead-unit selection bug.

### Next Steps

- Compile in Unity.
- Run the Play Mode reproduction above.
- If game-end flow still selects a unit after victory, split that into a
  separate small task for `MouseControler` early-return behavior.

## Closure - 2026-06-12

- Closed as implemented with QA pass and Unity validation pending.
- Archived to `_codex/tasks/archive/`.
- Related active tasks checked; broader skill presentation tasks remain
  separate and this fix does not close them.
- Recommended next smallest production step: Unity Play Mode validation of the
  FireElemental/HeavyHitter DOT death reproduction, then a separate
  `MouseControler` victory early-return task only if the game-end flow still
  requests an active unit after victory.
