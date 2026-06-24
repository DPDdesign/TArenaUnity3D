# [TARENA] PRD047: Tactical AI Async Decision Pipeline

- Status: implemented-qa-pass-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Async Planning, Battle Action Lifecycle
- Label: ready-for-agent
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/archive/046_D_PRD_TacticalAI_SearchScoring.md`
- Depends on: `_codex/tasks/archive/046_G_PRD_TacticalAI_LiveEnemyTurnIntegration.md`

## Problem Statement

Tactical AI V1 can now choose and execute legal enemy actions, including skills,
but the live combat flow freezes before every enemy move. The freeze is visible
as animation and whole-game stutter, not as a deliberate pause. The likely cause
is that the 3-ply planner runs synchronously on the Unity main thread from the
enemy turn entry point.

The current 300 ms AI decision watchdog does not solve this. It bounds how long
the planner may run, but while it runs synchronously the Unity main thread still
does not tick. Animator updates, projectile presentation, input polling, and
other frame work all wait.

This is an architectural problem. Combat state is resolved logically before
some visual presentation finishes. For example, once a projectile action has
committed its damage result, the future battle state is already known even if
the projectile is still flying on screen. AI planning should be able to start
from that committed logical state while presentation continues, then execute
the chosen action on the main thread when the live battle flow is ready.

## Solution

Build an async Tactical AI decision pipeline.

The live battle flow should capture an immutable planning input on the main
thread after logical battle state is committed, copy all skill metadata and
profile data needed by the pure planner, then run the Tactical AI search on a
worker task. The task must not touch Unity objects, Unity APIs, live battle
objects, `DataMapper`, `Resources`, or scene state. It only reads copied data
and returns a `TacticalAISearchPlan`.

While the worker task is planning, presentation can continue normally. Movement
animations, projectile visuals, skill presentation, UI, music, and other frame
work must not freeze because the planner is off the main thread.

When the battle action lifecycle reaches a point where the next AI action may
start, the main thread should consume the latest completed async decision if it
matches the current battle state, revalidate it through the existing live
execution bridge, and execute it through the same `MouseControler`,
`CastManager`, and `BattleActionLifecycle` paths as the synchronous V1
integration. If the async result is missing, stale, faulted, cancelled, or
rejected, the system must fall back safely without blocking the main thread.

## User Stories

1. As a player, I want enemy thinking not to freeze animations, so that combat
   feels continuous.
2. As a player, I want projectile and skill presentation to continue while the
   enemy thinks, so that enemy turns do not look like hard pauses.
3. As a player, I want AI decisions to remain legal, so that async planning
   never creates unfair actions.
4. As a player, I want AI actions to start quickly when presentation is done,
   so that enemy turns feel responsive.
5. As a player, I want music and non-combat presentation to keep running during
   AI decisions, so that the whole game does not appear stopped.
6. As a designer, I want the existing Tactical AI profile to keep controlling
   planning strength, so that async execution does not change difficulty rules.
7. As a designer, I want the 300 ms budget to remain a decision budget, not a
   visible freeze budget, so that slow thinking is hidden behind presentation
   where possible.
8. As a designer, I want AI planning to begin from committed logical state, so
   that visual delays do not postpone decisions unnecessarily.
9. As a designer, I want stale decisions to be discarded, so that AI never acts
   on a board state that changed after the snapshot was captured.
10. As a designer, I want fallback behavior to remain available, so that combat
    cannot deadlock when async planning fails.
11. As a developer, I want worker-thread planning to read only immutable DTOs,
    so that Unity thread-safety rules are not violated.
12. As a developer, I want skill metadata copied before planning starts, so
    that the worker thread does not touch `DataMapper` or `Resources`.
13. As a developer, I want profile data copied before planning starts, so that
    the worker thread does not touch `ScriptableObject` state.
14. As a developer, I want live execution to remain on the main thread, so that
    scene mutation stays Unity-safe.
15. As a developer, I want live execution to keep using the current revalidation
    bridge, so that cached or async decisions cannot bypass legality.
16. As a developer, I want diagnostics for async start, completion, stale
    result, fault, cancellation, revalidation failure, and fallback, so that
    Play Mode behavior can be understood.
17. As a developer, I want the async runner to be testable without scene
    objects where possible, so that state transitions and stale-result handling
    can be covered by EditMode tests.
18. As QA, I want a deterministic fake async planner seam, so that tests can
    prove the main-thread consumer does not block.
19. As QA, I want manual Play Mode checks for enemy movement and skill use, so
    that async planning is verified in live combat.
20. As QA, I want no Unity asset changes in this slice, so that the change can
    be reviewed as C# architecture only.

## Implementation Decisions

- This PRD replaces the synchronous live enemy-turn AI decision path with an
  async decision pipeline. It does not change Tactical AI scoring, gameplay
  balance, unit stats, skill effects, cooldowns, or movement values.
- Planning must split into three phases:
  `capture input on main thread`, `search on worker thread`, and `consume result
  on main thread`.
- The worker task must receive a complete immutable planning input. The input
  includes the battle snapshot, copied resolved profile data, and copied skill
  metadata.
- The worker task must not use Unity APIs or live Unity objects. It must not
  access `MonoBehaviour`, `ScriptableObject`, `Resources`, `DataMapper`,
  `HexClass`, `TosterHexUnit`, `MouseControler`, `CastManager`, `TurnManager`,
  `GameObject`, `Transform`, `Debug.Log`, or scene lookups.
- The existing pure planner remains the planning engine. The async pipeline
  should adapt around it rather than rewriting search and scoring.
- Live execution remains authoritative. Completed async plans must still be
  passed through the current live revalidation and execution bridge before any
  action starts.
- The async result is advisory. It is valid only when the planned snapshot hash,
  active actor id, and profile identity still match the current live battle
  state at consumption time.
- If the async result is stale, reject it and start a fresh decision or fall
  back according to the local flow. Never execute stale intents.
- If the async task faults, log a bounded diagnostic and fall back. Do not let
  task exceptions escape into Unity update loops.
- If the async task is still running when the AI action is requested, the main
  thread may wait only by yielding frames, not by blocking. A small visible
  thinking delay is acceptable; a frozen frame is not.
- The system should prefer starting the next AI decision as early as possible
  after logical state commit. It should not wait for all presentation to finish
  when the next committed logical state is already known.
- The battle lifecycle currently distinguishes action body and blocking
  presentation. This PRD needs a main-thread hook or equivalent signal for
  "logical battle state committed and safe to snapshot" separate from
  "presentation finished and next action may execute."
- The hook must not change action semantics. It only allows read-only snapshot
  capture and async planning after live model mutation is complete.
- If a safe logical-commit hook cannot be identified for every action type in
  this slice, implement the async runner first at the existing enemy-turn entry
  point, then add early-start hooks only for action types where commit timing is
  clear and testable.
- The first implementation should support the existing enemy AI integration
  point and preserve the old legacy AI fallback.
- The old legacy fallback remains temporary safety behavior. Async Tactical AI
  should try first, but fallback can execute when async planning cannot produce
  a valid live action.
- Add a copied skill metadata provider. It should be built on the main thread
  using existing skill ids and current metadata source, then used safely by the
  worker thread.
- Add a copied profile value path if the current resolved profile is not already
  safe as a plain data object. The worker thread should read value data, not a
  live `ScriptableObject`.
- The async runner should own task lifecycle state: idle, running, completed,
  consumed, stale, faulted, and cancelled.
- The async runner should prevent overlapping decisions for the same actor and
  snapshot. Starting a newer decision should make older unfinished results
  stale.
- The async runner should expose a small main-thread API for starting planning,
  polling/completing results, cancelling stale work, and executing completed
  plans through the live bridge.
- No cancellation token is required for correctness if stale results are
  discarded, but cancellation may be added if it keeps task cleanup simple.
- Do not use `Task.Result`, `Task.Wait`, blocking locks, busy waits, or sleeps
  on the Unity main thread.
- Do not use `Thread.Sleep` to simulate thinking time. Use coroutine frame
  yielding or realtime waits on the main thread if a visible delay is needed.
- Diagnostic logs should be bounded and event-based, not every frame.
- Suggested diagnostic events:
  async planning started, async planning completed, async planning watchdog
  expired, async result consumed, async result stale, async result faulted, live
  revalidation rejected result, legacy fallback started.
- The implementation should prefer deep modules:
  async decision runner, immutable planning input, copied skill metadata
  provider, copied profile/provider adapter, lifecycle/main-thread integration,
  and diagnostics formatting.

## Runtime Flow

Target steady-state flow:

```text
Battle action mutates logical model
-> logical commit is complete
-> main thread captures BattleSnapshot for next action opportunity
-> main thread copies TacticalAI profile values
-> main thread copies skill metadata needed by that snapshot
-> worker task builds TacticalAISearchPlan
-> presentation continues on main thread
-> battle lifecycle reaches next-action-safe point
-> main thread checks completed async result
-> if result matches current state: live revalidate and execute
-> if result missing/stale/faulted/rejected: fallback or fresh async decision
```

Initial integration flow if early logical-commit hooks are too risky:

```text
Enemy turn entry point reached
-> main thread captures BattleSnapshot
-> main thread copies profile and skill metadata
-> start worker task
-> coroutine yields frames while task runs
-> completed result returns to main thread
-> live revalidate and execute
-> fallback on failure
```

The initial integration flow still removes hard freezes because the planner is
off the main thread. The steady-state flow adds better combat feel by hiding
enemy thinking behind already-running presentation.

## Testing Decisions

- Good tests for this PRD assert external behavior of the async pipeline:
  non-blocking main-thread flow, correct stale-result rejection, correct
  fallback behavior, and main-thread-only live execution.
- Do not test private task internals when a public state transition or result
  can be tested.
- Add EditMode tests for the copied skill metadata provider. Build metadata on
  the main thread seam, then verify the copied provider can answer skill
  metadata without touching the live metadata source.
- Add EditMode tests for planning input construction using handcrafted
  snapshots, copied profile values, and copied metadata.
- Add EditMode tests for async runner state transitions with a fake planner:
  idle to running, running to completed, completed to consumed, faulted to
  fallback, stale result rejected, newer decision supersedes older result.
- Add EditMode tests proving the main-thread consumer never blocks on an
  incomplete fake task. The expected behavior is "not ready yet" or coroutine
  yield, not synchronous wait.
- Add EditMode tests that completed async plans are rejected when snapshot hash
  or active unit id differs from live state at consumption time.
- Add EditMode tests that a completed matching plan is passed to the live
  execution seam once and only once.
- Add EditMode tests that task exceptions are converted to a faulted result and
  do not throw through the Unity caller.
- Keep existing Tactical AI search/scoring tests as planner coverage. Do not
  duplicate 3-ply scoring tests in this PRD except where async input copying
  changes behavior.
- Use Play Mode/manual validation for live scene timing:
  enemy move, move-and-attack, ranged attack, wait, defend, and at least one
  skill intent should run through async planning and live execution.
- Manual Play Mode validation should watch for animation/audio continuity while
  AI planning is active.
- Manual Play Mode validation should confirm Unity Console shows bounded async
  diagnostics, not per-frame spam.
- Unity compilation and Unity Test Runner execution remain user-side unless a
  later implementation task explicitly allows running Unity commands outside
  the editor.

## Acceptance Criteria

- Enemy Tactical AI planning no longer runs synchronously on the Unity main
  thread.
- AI search runs in a worker task using only copied immutable DTO data.
- Worker-thread planning does not access Unity APIs, live scene objects,
  `DataMapper`, `Resources`, `ScriptableObject` state, or `Debug.Log`.
- Skill metadata needed by planning is copied on the main thread before the
  worker task starts.
- Profile data needed by planning is copied or resolved into worker-safe value
  data before the worker task starts.
- Completed async plans are consumed only on the main thread.
- Live revalidation and live execution still use the existing Tactical AI
  execution bridge and battle lifecycle path.
- Stale async results are rejected before execution.
- Faulted async tasks fall back safely and produce bounded diagnostics.
- The main thread never calls blocking task waits.
- Existing legacy fallback remains available.
- In Play Mode, enemy AI thinking no longer visibly freezes combat animations
  or whole-game frame flow before enemy movement.
- No Unity scenes, prefabs, materials, controllers, `.inputactions`, generated
  Unity files, `.asmdef`, or `.asmref` are edited for this slice.
- No gameplay float values are changed.
- No public or serialized fields are renamed.

## Out of Scope

- Rewriting Tactical AI search and scoring.
- Changing AI profile tuning values.
- Changing skill effects, skill cooldowns, skill targeting, unit stats,
  movement values, damage formulas, initiative, or turn-order rules.
- Full extraction of skill prediction from legacy skill code.
- Removing `MostStupidAIEver`.
- Removing the legacy fallback path.
- Rewriting `TurnManager`.
- Rewriting `BattleActionLifecycle` beyond the minimal hook/state needed for
  async planning integration.
- Persisting tactical battle snapshots to SQLite.
- Multiplayer authority, rollback, online sync, or network determinism work.
- Editing Unity assets or scene wiring.

## Further Notes

- The current synchronous path is acceptable as a functional prototype, but it
  is not acceptable as the final combat feel because the decision budget is paid
  as a visible frozen frame.
- The async planner must treat `BattleSnapshot` and skill/profile copies as the
  contract. If a future planner needs more data, add it to copied DTOs rather
  than reaching back into live Unity objects.
- Early planning after logical commit is the desired final flow. It lets the AI
  think during presentation that already has a known logical outcome.
- If early planning is only partly implemented in this slice, document which
  action types start planning early and which still start at enemy-turn entry.
- The coding agent should keep this change narrow and C#-only, then leave
  Unity compile and Play Mode validation to the user unless explicitly allowed.

## Implementation - 2026-06-24

### What Changed

- `TacticalAIAsyncDecisionPipeline.cs`
  - Added `TacticalAICopiedSkillMetadataProvider`, which copies only the skill metadata referenced by the captured `BattleSnapshot` before worker-thread planning starts.
  - Added `TacticalAIAsyncTurnIntegrator`, which splits Tactical AI into main-thread snapshot/profile/metadata capture, worker-task search, and main-thread result consumption through the existing execution bridge.
  - The async integrator rejects stale results when snapshot hash, active actor id, or profile hash changes before consumption, and it converts worker faults/cancelled runs into safe legacy fallback results.
- `TacticalAILiveTurnIntegrator.cs`
  - Exposed `BuildExecutionFallbackReason(...)` so the new async path reuses the same fallback reason mapping as the synchronous path.
- `MostStupidAIEver.cs`
  - Replaced the synchronous tactical planner call with a coroutine that starts async planning, yields frames while the worker task runs, then either starts the validated action or falls back to the legacy AI path.
  - Prevents overlapping AI-planning coroutines on the component and clears the coroutine handle on disable.
- No Inspector fields changed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIAsyncDecisionPipelineTests.cs`.
- The tests are pure EditMode logic checks and do not require scenes or prefabs because they use handcrafted `BattleSnapshot` data, fake metadata providers, and injected async/execution seams.
- The file checks:
  - copied skill metadata is captured once and remains usable without further live-provider reads;
  - the async integrator does not report a terminal result while the worker plan is still incomplete;
  - a completed plan is rejected as stale when the current snapshot hash changes before consumption;
  - a worker-thread planner fault becomes a fallback result instead of throwing through the Unity caller.
- Run them manually in Unity Test Runner at `Window > General > Test Runner`, switch to `EditMode`, and run `TacticalAIAsyncDecisionPipelineTests`.
- Expected result: all tests in `TacticalAIAsyncDecisionPipelineTests` pass. Tests were not run automatically in this workflow.

### Unity Test

#### Unity Setup

- Open the tactical battle scene you already use for enemy AI validation.
- Keep the existing scene wiring for `MouseControler`, `MostStupidAIEver`, `TurnManager`, `HexMap`, and `BattleActionLifecycle`.
- No new Inspector assignments or scene objects are required by this slice.

#### Play Mode Test

- Enter Play Mode with enemy AI enabled and advance to an enemy-controlled turn.
- Watch enemy turns that previously froze before movement/attack selection.
- Expected observation: the game should keep rendering frames while the async planner runs, then the enemy action should start through the existing live execution path after the worker result is consumed.
- Watch the Unity Console for bounded tactical AI diagnostics such as `async-start`, `async-complete`, stale/fault warnings when applicable, and the existing fallback warning when async execution cannot start.
- If async planning cannot produce a usable live action, expected observation: the legacy fallback AI still acts and the turn does not deadlock.

### QA Verdict

- Final QA verdict: Pass.
- QA report path: `_codex/tasks/QA/2026-06-24_2258_047_QA_ArchitectureReview.md`
- Actionable findings: none.
- Non-blocking observations: the async completion log currently prefixes the existing synchronous plan log, so the message contains two tactical-AI tags; this is log-formatting only and not a blocker.
- Follow-up fixes applied: none were requested by QA after the review pass.

### Notes

- This slice intentionally uses the PRD's lower-risk initial integration path at the existing enemy-turn entry point. It does not add earlier logical-commit hooks to `BattleActionLifecycle` yet.
- Live execution remains main-thread-only and still routes through the existing `TacticalAIExecutionBridge` revalidation path.
- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, generated Unity files, or gameplay float values were changed.
- Unity compilation, EditMode execution, and Play Mode verification were not run automatically.

### Next Steps

- Run `TacticalAIAsyncDecisionPipelineTests` manually in Unity EditMode Test Runner.
- Run the Play Mode enemy-turn checks above and confirm there is no visible planning freeze before enemy actions.
- If this slice behaves correctly, the next focused follow-up is moving async planning earlier in the battle lifecycle so more thinking time is hidden behind already-running presentation.
