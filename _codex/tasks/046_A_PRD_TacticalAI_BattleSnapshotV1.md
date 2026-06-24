# [TARENA] PRD046-A: Tactical AI BattleSnapshot V1

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, Battle Snapshot
- Label: needs-grill
- Parent: `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`
- Related: `_codex/Context/BattleActionRules.md`
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- Related: `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

## Problem Statement

Tactical AI needs to reason about the current battle without mutating live
Unity scene objects. Current tactical state is spread across `HexMap`,
`HexClass`, `TeamClass`, `TosterHexUnit`, `TurnManager`, `MouseControler`, and
`CastManager`, so search cannot safely run against those objects directly.

Build a pure `BattleSnapshot` V1 that captures enough current battle state for
AI planning, hashing, candidate generation, turn-order estimation, and later
future persistence/replay compatibility.

## Scope

This PRD creates the snapshot model and snapshot builder only. It does not
choose AI actions, execute actions, create the search engine, or rewrite skill
prediction.

## Implementation Decisions

- `BattleSnapshot` must not store `HexClass`, `TosterHexUnit`, `GameObject`,
  `MonoBehaviour`, `Transform`, or other Unity object references.
- A live adapter may read Unity objects to build a snapshot.
- Snapshot data should be deterministic for hashing and tests.
- Snapshot state should capture raw runtime truth where possible.
- If a value is derived at runtime, it does not need to be pre-flattened into
  the snapshot unless AI planning needs that exact derived value.
- Statuses must be visible in the snapshot with remaining duration or turn
  count.
- Stat-modifying statuses do not need to be immediately baked into final stats
  if current runtime code calculates them dynamically.
- Tactical battle-state snapshots are separate from PRD030 army snapshots.
- V1 snapshots may remain in memory. The shape should stay compatible with a
  future persisted battle-state payload linked to Run Battle.

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

