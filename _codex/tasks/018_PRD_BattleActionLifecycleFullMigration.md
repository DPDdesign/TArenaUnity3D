# [TARENA] PRD: Battle Action Lifecycle Full Migration

- Status: draft
- Type: PRD
- Area: Turn Flow, Action Sequencing, Skills, Passive Effects, Movement, AI, Future Multiplayer
- Label: ready-for-agent
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- Related: `_codex/tasks/013_PRD_MigrateRemainingPassiveDeferredSkillPresentation.md`
- Related: `_codex/tasks/015_PRD_AuditUnifiedSkillPresentationPath.md`
- Related: `_codex/tasks/archive/015_PRD_DeadUnitTurnDesync_Coding.md`

## Problem Statement

Current battle action flow lets movement, skill logic, skill presentation,
passive/deferred effects, AI, RPC entrypoints, and turn selection progress
through separate legacy paths.

The visible symptom is that one unit can still be moving, resolving a projectile,
or playing a skill/result animation while `TurnManager` and `MouseControler`
already allow the next unit to act. That overlap can feel dynamic, but it is too
desync-prone for the current architecture. It can leave units visually between
hexes, allow stateful effects to resolve in unclear order, or let passive effects
appear simultaneous when they should be deterministic.

The desired correction is not to change skill names, values, targeting,
cooldowns, damage, animation timings, projectile timings, or presentation
content. The desired correction is to make one action lifecycle own when an
action is committed, when it is consumed, when blocking presentation is finished,
when automatic follow-up effects resolve, and when the next unit may act.

## Solution

Implement a full **Battle Action Lifecycle** migration.

Every state-changing battle action should go through one lifecycle:

```text
legacy validation / legal action intent
-> commit
-> action consumed
-> block input and turn advancement
-> resolve model state
-> await blocking presentation and unit animations
-> resolve nested sub-actions
-> resolve automatic follow-up actions
-> completion
-> allow next unit selection
```

For this PRD, `completion` means:

- tactical state is stable,
- movement has reached final logical and visual state,
- damage, healing, status, death, forced movement, and trap results are applied,
- projectile travel and impact reveal have completed when the action uses a
  projectile,
- all blocking unit animations have completed,
- nested sub-actions have completed,
- relevant end-of-own-turn automatic actions have completed,
- lifecycle cleanup has released input and turn advancement safely.

Presentation timing itself must remain unchanged. The lifecycle only changes
turn availability timing: the next actionable unit must wait until blocking
presentation is complete.

## User Stories

1. As a player, I want the next unit to become actionable only after the current
   action has visibly and tactically finished, so that the board never feels out
   of sync.
2. As a player, I want a moving unit to finish its movement before another unit
   can start a state-changing action, so that no unit appears stuck between
   hexes.
3. As a player, I want projectile skills to wait for projectile travel, impact,
   and target reaction before the next unit acts, so that cause and effect are
   readable.
4. As a player, I want hit, death, buff, debuff, defense, and cast animations to
   complete before the next action, so that results do not overlap into the next
   turn.
5. As a player, I want passive/deferred effects to resolve one at a time, so
   that two units do not appear to kill each other simultaneously.
6. As a player, I want passive effects at end of own turn to resolve before the
   next unit acts.
7. As a player, I want end-of-round passive effects to behave like automatic
   normal turns, using the same queue logic as unit turns.
8. As a designer, I want skill ids, skill slot order, cooldowns, target rules,
   damage, movement values, and presentation timings to remain unchanged.
9. As a designer, I want a unit with multiple passive effects to resolve them in
   equipped slot order, not all at once.
10. As a designer, I want initiative or movement-speed changes from one passive
    to affect the global queue only after that unit's passive package completes.
11. As a developer, I want `MouseControler`, `CastManager`, AI, and legacy RPC
    entrypoints to use the same execution path, so that action completion has
    one owner.
12. As a developer, I want `Moved`, `Waited`, defense state, cooldown usage, and
    similar action-consumption state to be owned by the lifecycle, not scattered
    across action bodies.
13. As a developer, I want skill presentation paths to be awaitable, so that
    lifecycle can wait for blocking presentation instead of fire-and-forget.
14. As a developer, I want nested effects such as movement-triggered traps or
    combat-triggered passives to resolve as sub-actions of the current action.
15. As a developer, I want missing presentation, missing catalog entries, invalid
    targets, or animation timeout cases to release the lifecycle safely instead
    of softlocking the battle.
16. As a future multiplayer developer, I want player input, AI decisions, and
    future network commands to converge on the same lifecycle execution path.
17. As QA, I want Play Mode checks that prove action overlap no longer allows
    stateful desync while preserving existing skill behavior.

## Implementation Decisions

- This is a full migration task, not a narrow gate around one or two actions.
- The Battle Action Lifecycle is the sole owner of action commit, action
  consumption, blocking action execution, automatic follow-up execution, and
  next-turn release.
- `TurnManager` must not select or expose a new actionable unit while lifecycle
  is busy.
- `MouseControler` must not use `CancelUpdateFunc()` as the semantic end of an
  action. It may remain as UI/input cleanup only.
- `CastManager` must not use `SetFalse()` as the semantic end of an action. It
  may remain as skill-mode cleanup only until it can be narrowed further.
- `Moved = true`, `Waited = true`, defense stance application, cooldown use, and
  action consumption must happen inside the lifecycle after a legal commit.
- Legal commit means the existing legacy checks accepted the action. This task
  must not build a full new validation module; see ADR 005.
- The lifecycle should carry both `skillSlot` and `skillId` for skill actions.
  `skillId` remains the XML/catalog/reflection identity. `skillSlot` preserves
  skill-slot order, cooldown association, UI association, and selected
  `skillN` animation.
- Skill presentation paths that block action completion must become awaitable.
  Fire-and-forget VFX/SFX tails may exist only after the blocking presentation
  has completed.
- For this migration, be conservative: projectile travel, impact, result reveal,
  and unit reaction/death animations are blocking.
- Do not shorten, reorder, or retime existing presentation. Wait for the current
  presentation timing to finish before allowing the next action.
- AI should only choose an action intent. It must execute through the same
  lifecycle as player actions.
- Existing `[PunRPC]` methods and other network-shaped legacy entrypoints should
  become transport/adapter entrypoints into the same lifecycle, not separate
  execution paths.
- Future multiplayer is not implemented in this task, but the lifecycle shape
  should keep input/AI/network intent separate from action execution.

## Action Categories To Migrate

All state-changing battle action categories must route through the lifecycle:

- normal movement,
- move-and-attack,
- basic melee attack,
- basic ranged attack,
- active skill casts,
- movement/approach skills such as `Rush`, `Slash`, `Heavy_Fists`, and similar
  paths,
- pull, teleport, forced movement, summon, split, trap placement, and
  trap-trigger paths,
- wait,
- defense,
- AI-selected actions,
- legacy RPC entrypoints that currently start movement, attacks, or skills,
- end-of-own-turn passive/deferred actions,
- end-of-round passive/deferred actions,
- movement-triggered sub-actions such as `Fire_Movement`,
- combat-triggered sub-actions such as `Stone_Skin` feedback or
  `Unstoppable_Light` modifier/feedback,
- death/result reveal paths that can be triggered by active, passive, trap, or
  deferred effects.

## Automatic Action Rules

Automatic actions use the same lifecycle as normal skill casts. The difference
is only the trigger source.

### End Of Own Turn

End-of-own-turn passive/deferred effects resolve after the unit's manual or AI
action reaches completion, but before the next unit is selected.

```text
Unit A manual/AI action
-> completion of that action
-> Unit A EndOfOwnTurn passive Skill1..Skill4 package
-> next unit selection
```

### End Of Round

End-of-round passive/deferred effects behave like an automatic series of normal
turns.

The system chooses the next unit using the same queue logic as the normal
future turn queue, resolves all eligible passive slots for that unit, then
re-checks/recomputes the global queue before choosing the next unit.

```text
choose next unit from current queue
-> resolve that unit's eligible passive slots Skill1, Skill2, Skill3, Skill4
-> re-check/recompute global queue
-> choose next unit
```

Do not snapshot the whole end-of-round queue up front. Queue changes from
completed unit packages may affect which unit is chosen next.

### Passive Slot Order

If a unit has multiple eligible automatic passive/deferred skills in the same
trigger window, resolve them by equipped slot order:

```text
Skill1 -> Skill2 -> Skill3 -> Skill4
```

Each slot is a separate automatic action and must reach completion before the
next slot resolves.

After each slot, perform a hard validity re-check:

- the unit still exists,
- the unit is alive and actionable for this trigger,
- the skill still exists in that slot,
- the trigger still makes sense,
- target/hex/affected units still exist when needed.

Do not reorder remaining slots of the same unit just because `Skill1` changed
initiative or movement speed. Global queue changes apply after the unit's
passive package completes.

## Sub-Action Rules

Nested effects caused by the current action resolve as sub-actions of that
action, not as unrelated global actions.

Examples:

- `Fire_Movement` triggered during movement is a movement sub-action.
- entering a trap during movement is a movement sub-action.
- trap damage causing death is nested under the trap sub-action.
- `Stone_Skin` damage-reduction feedback is a combat sub-action of the incoming
  hit.
- `Unstoppable_Light` can act as a combat calculation modifier inside the
  current damage resolve, with any feedback treated as combat sub-action
  presentation.

Nested sub-actions must complete before returning to their parent action.

## Failure And Timeout Rules

The lifecycle must never remain busy forever.

- If model resolve is invalid before commit, skip the action and do not consume
  it.
- If model resolve becomes partially invalid after commit, keep the main action
  consumed but skip invalid sub-effects after re-check.
- If presentation catalog, VFX, SFX, or optional presentation references are
  missing, log/no-op according to existing behavior and continue.
- If an animation or presentation wait cannot complete, timeout and cleanup to a
  model-safe state.
- If a unit movement visual ends too far from its final logical hex, cleanup
  should prefer deterministic model safety over visual overlap.
- After every skip, missing-presentation path, or timeout, release lifecycle
  cleanly and re-check the queue.

## Validation Boundary

Do not build a full action validation rewrite in this task.

Use current validation behavior as legacy adapters:

- existing `MouseControler` highlight and click checks,
- existing `CastManager` mode methods and skill-specific checks,
- existing path checks such as `TosterHexUnit.IsPathAvaible(...)`,
- existing turn eligibility checks in `TeamClass`.

For this task, `ValidatedAction` means the action passed existing legacy checks
closely enough to be executed by the lifecycle.

Create or use small adapter methods only when they reduce duplication or make a
legacy action enter the lifecycle cleanly. Do not duplicate large target, range,
cooldown, or pathfinding rule sets into the lifecycle.

## Testing Decisions

- Unity compile and Play Mode validation are user-side unless a specific Unity
  test command is explicitly allowed.
- Automated tests are optional only if a deterministic plain C# seam exists.
  Do not add `.asmdef` or `.asmref` files for test infrastructure.
- Static verification should search for remaining direct semantic action ends:
  `SetFalse()` as turn completion, `CancelUpdateFunc()` as turn completion,
  direct `Moved = true`, direct `Waited = true`, direct defense completion, and
  direct `StartCoroutine(...)` action execution that bypasses lifecycle.
- Static verification should search all `[PunRPC]` action entrypoints and
  confirm they enter lifecycle instead of owning action completion.
- Static verification should search `SkillPresentationManager` blocking paths
  and confirm lifecycle can await their completion.

## Manual Play Mode Validation

Manual validation should include at least:

1. Normal move: next unit cannot act until mover reaches final hex visually and
   logically.
2. Move-and-attack: next unit cannot act until movement, attack, hit/death
   reveal, and action cleanup complete.
3. Basic ranged attack: next unit cannot act until projectile/impact/reaction
   complete.
4. `Fire_Ball`: projectile travel, impact, damage reveal, and reactions block
   next action until complete.
5. `Rush`: movement uses current configured timing, post-arrival presentation
   completes, and next unit waits.
6. `Heavy_Fists`: approach movement and later impact/reveal complete before next
   unit acts.
7. Wait: wait consumes the unit and then resolves end-of-own-turn automatic
   actions before next unit.
8. Defense: defense state and defense animation complete before next unit.
9. End-of-own-turn passive package: a unit resolves eligible passive slots in
   slot order before next unit acts.
10. End-of-round passive queue: unit package, queue re-check, next unit package.
11. Two FireElemental-style passive cases: effects resolve sequentially, not as
    simultaneous mutual kills.
12. `Fire_Movement`: trap placement during movement resolves as movement
    sub-action without breaking movement completion.
13. Trap/DOT death: death reveal completes and dead unit does not receive a turn.
14. AI action: AI-selected action uses the same lifecycle and cannot issue a
    second action before completion.
15. Missing catalog/VFX/SFX case: no softlock; lifecycle releases safely.
16. Missing/invalid animation case: timeout/cleanup releases safely.

## Acceptance Criteria

Done when:

- one Battle Action Lifecycle path owns action commit, action consumption,
  blocking execution, automatic follow-up resolution, and next-turn release,
- all state-changing player, AI, skill, movement, wait, defense, passive,
  deferred, movement-triggered, combat-triggered, and legacy RPC action paths
  enter the lifecycle,
- `TurnManager` cannot expose a new actionable unit while lifecycle is busy,
- `MouseControler` and `CastManager` no longer use cleanup methods as semantic
  action completion signals,
- blocking skill presentation and unit animations are awaitable by lifecycle,
- projectile travel, impact, result reveal, and unit reaction/death animation
  block next action in this migration,
- end-of-own-turn automatic effects resolve before the next unit,
- end-of-round automatic effects resolve as unit packages with global queue
  re-check between packages,
- multiple passives on the same unit resolve in slot order and not all at once,
- nested sub-actions complete before parent action completes,
- missing presentation, invalid post-commit targets, and animation timeouts do
  not softlock the battle,
- existing skill names, skill slot order, XML skill ownership, cooldowns,
  targeting, ranges, damage/status values, movement values, initiative rules,
  animation timings, projectile timings, and presentation content are not
  intentionally changed,
- validation remains legacy-adapter based per ADR 005,
- manual Play Mode checklist is ready for Unity-side validation.

## Out Of Scope

- Full action validation rewrite; see ADR 005.
- New multiplayer transport, networking authority, rollback, replay, or sync
  protocol.
- Renaming public or serialized fields.
- Renaming skill ids or changing XML skill ownership.
- Changing cooldowns, targeting rules, ranges, damage, healing, status values,
  movement values, initiative, turn rules, animation timing, projectile timing,
  VFX/SFX content, or balance.
- Editing Unity assets, prefabs, scenes, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` unless the
  user explicitly expands the task.
- Creating new VFX/SFX assets.
- UI redesign beyond disabling actionable controls while lifecycle is busy.

## Further Notes

Design decisions confirmed during grill:

- Full migration is required; do not stop at a small representative slice.
- Architecture safety takes priority over preserving overlapping action feel in
  the first migration.
- Conservative blocking is acceptable first. Future feel work may later mark
  specific presentation tails as non-blocking after the lifecycle is safe.
- AI and future multiplayer should fit the same lifecycle route.
- Validation belongs to a future PRD, not this migration.
- `commit` means a legal action is accepted and consumed.
- `completion` means the action, blocking presentation, blocking unit
  animations, nested sub-actions, and relevant automatic follow-ups are fully
  done.

## Implementation - 2026-06-13

### What Changed

- No Inspector fields changed. No existing serialized/public fields were renamed or removed.
- `BattleActionLifecycle`: added a runtime-created lifecycle owner for action commit, action body, blocking presentation wait, and completion release. New non-Inspector timeout constants: `ActionBodyTimeoutSeconds = 15f` and `PresentationTimeoutSeconds = 15f`; useful values are positive seconds, lower values release sooner on stuck actions, higher values tolerate longer blocking work, and tuning should stay above the longest intended animation/presentation chain.
- `MouseControler`, `TurnManager`, `MostStupidAIEver`: player, RPC, and AI movement, move-attack, ranged attack, wait, defense, and skill completion now enter lifecycle wrappers; turn exposure and input return early while lifecycle/presentation is blocking. No fields added or tuned in these classes.
- `HexMap`: movement visual waits now run through `UnitMoveVisualWaitTimeoutSeconds = 5f`; useful values are positive seconds, lower values snap stuck movers earlier, higher values wait longer for slow visuals, and tuning should stay above normal movement animation duration.
- `SkillPresentationManager`: blocking presentation tracking was added with runtime counter `blockingPresentationCount`; it is not an Inspector/tuning field, should stay managed only by tracked presentation coroutines, and allows lifecycle to wait for projectile, impact, reveal, reaction, and death presentation.
- `CastManager`: added `[NonSerialized] public bool ActionInputBlockedByCommittedSkill`; false allows skill input, true blocks additional skill input after a committed async skill starts, and it is reset by `SetFalse()`. This is runtime state, not an Inspector tuning value.

### Automatic Test

- No EditMode tests were added. The changed behavior is coroutine-driven runtime sequencing across scene-owned `MonoBehaviour` components, `TosterView`, presentation managers, input/RPC adapters, and battle objects; there is no isolated deterministic test seam without scene/prefab setup or new `.asmdef` infrastructure.
- Tests are user-run in Unity. In Unity, open `Window > General > Test Runner`, select `EditMode`, and run the existing suite; expected result is that existing tests, if any are configured, still pass and no new PRD 018 tests appear.
- I did not run Unity, `dotnet`, builds, package restore, or external scripts, per project rules.

### Unity Test

#### Unity Setup

- No new scene objects, components, Inspector assignments, prefabs, materials, controller assets, `.inputactions`, `.asmdef`, or `.asmref` changes are required.
- Use the existing battle scene setup with its current `HexMap`, `MouseControler`, `TurnManager`, `CastManager`, `SkillPresentationManager`, skill presentation catalog, audio source, unit views, and UI references already wired.

#### Play Mode Test

- Press Play and verify normal move, move-and-attack, and basic ranged attack: the next unit must not become actionable until movement, projectile/attack, impact/reveal, reaction/death animation, and cleanup finish.
- Verify `Fire_Ball`, `Rush`, `Slash`, `Toxic_Fume`, and `Heavy_Fists`: committed skill input should be blocked while async movement/presentation is resolving, then the lifecycle should consume/release the turn once blocking presentation completes.
- Verify wait and defense: each consumes the acting unit through lifecycle and releases only after defense animation or wait cleanup completes.
- Verify end-of-own-turn and end-of-round automatic effects, `Fire_Movement`, trap/DOT death, and AI actions: nested presentation should complete before the next actionable unit is exposed.
- Verify missing catalog/VFX/SFX and a deliberately stuck movement animation path if practical: missing optional presentation should not softlock, and stuck movement should log a warning, snap to the logical hex, clear `AnimationIsPlaying`, and release.

### QA Verdict

- Final QA: Pass after focused follow-up.
- Final QA report: `_codex/tasks/QA/2026-06-13_1634_018_QA_ArchitectureReview_Followup.md`.
- Actionable findings: none remain in the follow-up pass.
- Follow-up fixes applied: `HexMap.DoUnitMoves(...)` now has a timeout/snap visual wait, and the completion protocol path for `MostStupidAIEver.cs` was corrected.
- Non-blocking observations: end-of-round passive/deferred package sequencing still needs Play Mode validation, and an old unreferenced `[PunRPC] heavy_fists(...)` body remains a multiplayer watch item because no in-repo Photon string call targets it.

### Notes

- The implementation intentionally avoids balance/value changes, skill id changes, cooldown/range/targeting changes, asset edits, prefab/scene edits, and validation rewrites.
- `SetFalse()` remains as skill-mode cleanup/signal; next-turn release for committed skill completion now routes through `MouseControler.TryCompleteSkillAction(...)` and lifecycle presentation waiting.
- Static text verification was performed with `rg`/PowerShell only; Unity compile and runtime behavior still need Editor validation.

### Next Steps

- Compile in the Unity Editor and fix any compiler errors Unity reports.
- Run the existing EditMode tests from Unity Test Runner.
- Execute the Play Mode checklist above, with special attention to passive package ordering, stuck movement timeout/snap, blocking projectile/reveal chains, AI action release, and the legacy multiplayer RPC surfaces.

## Fix - 2026-06-13 - Rush Counterattack Death Softlock

- Reported issue: `Rusher` used `Rush`, attacked, died from counterattack, and the game stayed stuck on `Rusher`.
- Cause: after death, `TosterHexUnit.Died(...)` removes the actor from its hex, but skill completion still resolved the actor with `hexMap.GetHexAt(...).Tosters[0]`. For a dead caster, that hex can be empty, so lifecycle completion never started.
- Fix: `MouseControler.EndSkillss(...)` now resolves skill completion through `ResolveSkillCompletionActor(...)`, using the hex occupant when present, then the team unit lists by last known hex, and finally the still-selected actor when the actor was removed from the hex by death.
- Automatic test: not added; this is a Unity Play Mode coroutine/lifecycle path.
- Manual check: repeat `Rush` into a counterattack that kills the `Rusher`; after death/reveal, the turn should release to the next valid unit instead of staying on `Rusher`.

## Fix - 2026-06-13 - Parallel Target Reveal Resolution

- Requirement correction: target results inside one skill/passive/effect should resolve concurrently, not target-by-target. The lifecycle should release only after the last target coroutine completes.
- Previous wrong behavior: shared reveal presentation used `PlayRevealsAndWait(...)` as a sequential loop, so AoE skills such as `Fire_Ball` could reveal target 1, then target 2, then target 3.
- Fix: `SkillPresentationManager.PlayRevealsAndWait(...)` now starts all target reveal coroutines together and waits until all have completed. `PlayImpactRevealsAfterDelay(...)` now spawns all target impact VFX first, then waits for parallel reveals.
- Scope: applies to shared instant-hit, projectile-impact, location-impact, skill, passive, and deferred-effect reveal lists that route through `SkillPresentationManager`.
- Automatic test: not added; this is coroutine/presentation timing in Unity Play Mode.
- Manual check: cast `Fire_Ball` on 3 targets. All 3 target hit/death/reaction reveals should start together after impact, and the next unit should become actionable only after the slowest reveal finishes.

## Fix - 2026-06-13 - End-Round Passive Stage Sequencing

- Reported issue: end-round passives still resolved as one synchronous sweep, so later passives such as `Fire_Skin` could start before all `Cold_Blood` resolves had finished.
- Requirement correction: end-round passive processing is staged globally by spell name. A stage such as `Cold_Blood` collects all currently alive units with that spell, starts their resolves together, waits for the last blocking presentation, then rebuilds the next stage such as `Fire_Skin`.
- Fix: `TurnManager` now starts new-round processing as a blocking `BattleActionLifecycleKind.Automatic` sequence and returns `null` until it completes. `TosterHexUnit` exposes grouped spell resolution and final cooldown/autocast queuing, while `TeamClass` resets only units still alive after all staged resolves.
- Death behavior: a unit killed by an earlier stage is filtered out before the next stage, so a Fire Elemental killed by `Cold_Blood` will not cast `Fire_Skin`.
- Automatic test: not added; this is Unity Play Mode coroutine/presentation sequencing.
- Manual check: end a round with at least one `Cold_Blood` unit and one `Fire_Skin` unit. All `Cold_Blood` results should resolve together first; only after the last reveal finishes should `Fire_Skin` start for surviving units.

## Fix - 2026-06-13 - Keep Current Unit Skill UI During Resolve

- Reported issue: when a unit such as `Rusher` committed `Rush`, `UICanvas.CurrentUnitPortrait` stayed correct but the skill icons/text disappeared during resolve.
- Cause: `MouseControler` intentionally sets `activeButtons = false` during blocking lifecycle resolve, and `UICanvas.Update()` used that flag both for input interactivity and for skill visibility.
- Fix: `UICanvas` now keeps rendering the selected unit's skill icons/text while `SelectedToster` exists, and uses `activeButtons` only to decide whether action/skill buttons are interactable.
- Automatic test: not added; this is scene UI behavior driven by Unity `Button` and `Image` references.
- Manual check: use `Rush` or another skill with visible resolve time. The current unit portrait and skill list should remain visible until the next unit is selected; buttons should remain non-interactable during resolve.
