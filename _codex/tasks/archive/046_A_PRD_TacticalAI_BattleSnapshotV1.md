# [TARENA] PRD046-A: Tactical AI BattleSnapshot V1

- Status: implemented-qa-pass-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Battle Snapshot
- Label: needs-grill
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Related: `_codex/Context/BattleActionRules.md`
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- Related: `_codex/Documentation/ADR_014_TacticalAI_MultiplayerCompatibleValidation.md`
- Related: `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

## Problem Statement

Tactical AI needs to reason about the current battle without mutating live
Unity scene objects. Current tactical state is spread across `HexMap`,
`HexClass`, `TeamClass`, `TosterHexUnit`, `TurnManager`, `MouseControler`, and
`CastManager`, so search cannot safely run against those objects directly.

Build a pure `BattleSnapshot` V1 that captures enough current battle state for
AI planning, hashing, candidate generation, turn-order estimation, and future
replay/online sync compatibility.

## Scope

This PRD creates the snapshot model and snapshot builder only. It does not
choose AI actions, execute actions, create the search engine, or rewrite skill
prediction.

## Implementation Decisions

- `BattleSnapshot` must not store `HexClass`, `TosterHexUnit`, `GameObject`,
  `MonoBehaviour`, `Transform`, or other Unity object references.
- A live adapter may read Unity objects to build a snapshot.
- Snapshot data should be deterministic for hashing and tests.
- The snapshot shape should be designed as the future tactical battle-state
  payload for replay and online sync, not as an AI-only private model.
- Snapshot state should capture raw runtime truth where possible.
- If a value is derived at runtime, it does not need to be pre-flattened into
  the snapshot unless AI planning needs that exact derived value.
- Statuses must be visible in the snapshot with remaining duration or turn
  count.
- Stat-modifying statuses do not need to be immediately baked into final stats
  if current runtime code calculates them dynamically.
- Tactical battle-state snapshots are separate from PRD030 army snapshots.
- V1 snapshots may remain in memory. The shape must stay compatible with a
  future persisted battle-state payload linked to Run Battle, for example a
  future `run_battle_state_snapshots` table keyed by `run_battle_id`, action
  order, snapshot hash, and serialized snapshot payload.

## Snapshot Contents

Minimal required model:

```text
BattleSnapshot
- map width and height
- hex list
- unit list
- activeUnitId
- current turn/action state
- snapshotHash

BattleHexSnapshot
- q/c coordinate
- r coordinate
- isWalkable
- occupyingUnitId
- trap/status markers needed by V1 planning, if visible in runtime

BattleUnitSnapshot
- runtimeUnitId
- teamIndex
- rosterIndexWithinTeam
- unitName / unitType
- hex coordinate
- amount
- tempHP
- base HP
- attack
- defense
- movement speed
- initiative
- minDamage
- maxDamage
- isAlive
- isRange
- waited
- moved
- movedThisTurn
- usedSkillThisTurn
- usedSkillIdsThisTurn
- canMoveAfterSkillThisTurn
- cooldowns by skill slot
- skill ids by slot
- status list

BattleStatusSnapshot
- statusId / source skill id when available
- sourceUnitId when available
- remainingDurationOrTurns
- runtime effect fields needed by V1 planning
```

## Runtime Unit Identity

Use a battle-local runtime id:

```text
unitId = "team-{teamIndex}-slot-{rosterIndexWithinTeam}"
```

Rules:

- This is not a database id.
- This is not a saved-army id.
- It only needs to be stable within one tactical battle.
- Live revalidation must sanity-check the id against current team, roster slot,
  unit name/type, source hex, alive/actionable state, and stack amount where
  useful.

## Hashing

`snapshotHash` should be stable for the same tactical state and change when
planning-relevant state changes:

- unit positions,
- alive/dead state,
- amount/temp HP,
- active unit,
- action state,
- skill cooldowns,
- used skill ids,
- statuses and remaining duration,
- trap/hex markers included in V1 planning.

The hash must not depend on object instance ids, memory addresses, scene object
names, or list enumeration that can change nondeterministically.

## Testing Decisions

Add deterministic EditMode tests where possible for:

- building a snapshot from small handcrafted plain data or controlled adapters,
- stable unit ids,
- hash stability for identical state,
- hash changes for position, amount, cooldown, active unit, and status changes,
- no Unity object references in the snapshot model.

Unity Play Mode/manual testing remains required for live-scene snapshot
construction.

## Acceptance Criteria

Done when:

- a pure `BattleSnapshot` model exists,
- a live battle adapter can build it from current tactical runtime state,
- the snapshot contains map, units, active unit, action state, cooldowns, skill
  ids, and visible statuses with remaining duration,
- the snapshot shape is suitable for future replay/online sync and does not
  overload PRD030 army snapshots,
- `runtimeUnitId` is stable within the battle,
- `snapshotHash` is deterministic and planning-relevant,
- no snapshot model stores Unity object references,
- runtime-derived stats are handled according to the raw-state rule above.

## Out Of Scope

- AI action selection.
- 3-ply search.
- Skill prediction rewrite.
- Persisting tactical snapshots to SQLite in V1.
- Changing gameplay values, status rules, damage rules, turn rules, or skill
  ids.
- Editing scenes, prefabs, materials, controllers, `.inputactions`,
  `.asmdef`, `.asmref`, or Unity asset instances unless later explicitly
  scoped.

## Implementation - 2026-06-24

### What Changed

- Added a pure BattleSnapshot V1 model in `BattleSnapshotModels.cs` for battle turn state, hexes, units, statuses, and stable runtime unit ids.
- Added `BattleSnapshotBuilder.cs` to normalize ordering and compute a deterministic `snapshotHash` from planning-relevant tactical state.
- Added `BattleSnapshotLiveAdapter.cs` to read current battle state from `HexMap`, `TeamClass`, `TosterHexUnit`, `SpellOverTime`, `TurnManager`, `MouseControler`, and `BattleActionLifecycle` without storing Unity object references in the snapshot.
- Added read-only accessors in `BattleActionLifecycle`, `SpellOverTime`, and `TurnManager` so the live adapter can read action-state and status-effect details without mutating runtime systems.
- Added focused EditMode coverage in `BattleSnapshotBuilderTests.cs` for runtime ids, hash stability, hash change triggers, and model purity.
- No Inspector fields changed.

### Automatic Test

- Added `BattleSnapshotBuilderTests` under `Assets/Scripts/Tests/EditMode/`.
- The tests check stable runtime unit id formatting, hash stability for equivalent state, hash changes for position/amount/cooldown/active-unit/status changes, and that snapshot model field types do not reference `UnityEngine.Object`.
- These tests do not require scene or prefab setup because they build handcrafted plain snapshot data and hash it through the pure builder.
- Tests were not run automatically. Run them manually in Unity Test Runner at `Window > General > Test Runner`, then choose the `EditMode` tab and run `BattleSnapshotBuilderTests`. Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No new Inspector assignments are required.
- Open an existing tactical battle scene that already contains `HexMap`, `MouseControler`, `TurnManager`, and `BattleActionLifecycle` at runtime.
- Enter Play Mode until units are spawned and at least one active unit exists.
- If you want live verification during this task scope, inspect `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()` through an attached debugger, watch expression, or temporary local debug hook.

#### Play Mode Test

- Start a tactical battle and wait until the first unit becomes active.
- Trigger or observe a state with cooldowns, a visible status effect, and a trap if available.
- Capture a snapshot through `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()`.
- Confirm the result contains map width/height and a full hex list.
- Confirm the result contains unit ids in `team-{teamIndex}-slot-{rosterIndexWithinTeam}` format.
- Confirm the result contains the current `ActiveUnitId`.
- Confirm the result contains turn/action state from the current round and lifecycle blocking state.
- Confirm the result contains skill ids and cooldowns by slot.
- Confirm the result contains explicit status entries with remaining duration/turn count.
- Confirm the result contains trap markers on hexes when present.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-24_1947_046_A_QA_ArchitectureReview.md`
- Actionable findings: none.
- Non-blocking observations: the convenience `BuildCurrentSceneSnapshot()` helper is fine for manual verification, but future high-frequency AI planning should prefer the overload that accepts explicit scene references.
- Follow-up fixes applied: none required after QA.

### Notes

- Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.
- Unit stats in the snapshot stay as raw runtime values while status modifiers remain explicit in `BattleStatusSnapshot`, matching the PRD rule to avoid prematurely baking dynamic status effects into final derived stats.
- This slice intentionally stops at snapshot model/building. It does not add action-intent generation, execution bridging, persistence, or tactical AI search.

### Next Steps

- Run `BattleSnapshotBuilderTests` in Unity EditMode Test Runner.
- Do one Play Mode verification pass on a tactical battle scene and inspect a live `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()` result.
- Use this snapshot seam as the input boundary for PRD046-B candidate generation and later PRD046-C live revalidation/execution work.
