# TArenaUnity3D Battle Action Codebase Map

Status: active
Last updated: 2026-06-26

## Combat API, Action Validation, And Battle Flow After PRD046-052

Key files:

- `Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `Assets/Scripts/Lesisz/MostStupidAIEver.cs`

Responsibilities:

- represent submitted tactical action requests as `BattleActionUse`,
- normalize validated actions as `BattleAction`,
- expose result/preview events as `BattleActionResult`,
- validate move, move-and-attack, basic ranged attack, wait, defend, stance, and
  skill actions from snapshot/data,
- reject actions while the battle is blocking or resolving a new-turn sequence,
- keep Tactical AI planning on copied snapshot/profile/skill-spec data,
- revalidate AI actions against the current live snapshot before any live
  execution,
- preserve live mutation parity through existing adapters until the remaining
  PRD050 work replaces them.

Current battle-action flow:

1. `HexMap.Start()` generates the map and `TeamClass` generates units.
2. `HexMap.IsBattleReadyForTacticalActions` becomes the scene-level readiness
   gate for tactical actions.
3. `MouseControler` selects the active unit and remains the player input adapter.
4. `MostStupidAIEver.AskAIwhattodo()` refuses to start AI until `MouseControler`,
   selected unit, `HexMap.IsBattleReadyForTacticalActions`, no lifecycle block,
   and two stable ready frames are present.
5. `BattleSnapshotLiveAdapter` builds a snapshot only after the battle is ready.
6. `BattleActionRules.Validate(...)` rejects blocked/new-turn states and checks
   action legality from snapshot/data.
7. `TacticalAIAsyncTurnIntegrator` plans on copied snapshot/profile/skill spec
   data and returns planned actions to the main thread.
8. `TacticalAIExecutionBridge` revalidates the selected action against the
   current live snapshot.
9. `BattleActionLiveApplier` applies validated actions. Current non-skill live
   mutation still delegates to `MouseControler.TryStart*`; current skill AI
   mutation uses `TacticalAISkillRulesExecutor`.
10. `BattleActionLifecycle`, `TurnManager`, skill presentation, VFX/SFX, and
    result reveal then drive turn release and next active unit selection.

Dependencies:

- Skill actions delegate validation/preview to the PRD49 skill API:
  `SkillRules`, `SkillUse`, `SkillCast`, `SkillResult`, `SkillContext`, and
  `SkillDefinitionSpec`.
- Skill definitions come from `SkillDefinitionAsset` via `SkillCatalog` /
  `DataMapper`.
- Tactical AI profile/search still uses `TacticalAIProfile`,
  `TacticalAISearchScoring`, and the remaining legacy intent/candidate
  compatibility surfaces.
- Battle readiness depends on `HexMap`, `MouseControler`, `TurnManager`, and
  `BattleActionLifecycle`.

Do not change without a focused task:

- gameplay numeric values for movement, damage, cooldowns, range, status, rush
  behavior, trap effects, or passive values,
- public/serialized Inspector fields on `MouseControler`, `CastManager`,
  `HexMap`, `TosterHexUnit`, or skill/profile assets,
- scene, prefab, material, animator, `.inputactions`, `.asmdef`, or `.asmref`
  files without explicit user permission,
- `CastManager` behavior as a broad cleanup. Replace one proven path at a time.

Known risks after PRD046-052 cleanup:

- PRD050 is partially implemented, not closed. Runtime/test surfaces still
  reference `TacticalAIActionIntent`, `TacticalAICandidateGenerator`,
  `TacticalAISearchCandidateExpander`, `TacticalAIIntentRevalidator`,
  `LegacyIntent`, and related probe/execution intent fields.
- `BattleActionLiveApplier` still delegates non-skill live mutation through
  `MouseControler.TryStartMoveAction`,
  `MouseControler.TryStartMoveAndAttackAction`,
  `MouseControler.TryStartBasicRangedAttackAction`,
  `MouseControler.TryStartWaitAction`, and
  `MouseControler.TryStartDefenseAction`.
- Player skill commit still validates through `SkillRules` but can execute
  through `CastManager.startSpell(...)` compatibility.
- Passive and trap trigger mutation remains in legacy hooks such as
  `TosterHexUnit`, `SpellOverTime`, `HexClass`, and `Traps`.
- `Stone_Throw` needs manual parity validation for actor stack split/spawn
  amount because current effect data does not fully express "half current
  stack".
- `Double_Throw` validation has focused tests, but live projectile/VFX timing
  remains a manual Play Mode check.
- Rusher illegal Rush and battle-readiness risks are closed at code-structure
  level by shared validation/readiness guards, but Rusher tactical choice
  quality still needs manual gameplay observation before any scoring fix.
