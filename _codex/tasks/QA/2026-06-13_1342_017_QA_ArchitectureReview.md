# [TARENA] QA Architecture Review: PRD 017 Rush Early Cast SFX And Run Movement Override

- Task: `_codex/tasks/017_PRD_RushEarlyCastSfxRunMovementOverride.md`
- Protocol: `_codex/tasks/QA/2026-06-13_1342_017_CodingCompletion_RushSfxRunOverride.md`
- Reviewed: 2026-06-13 13:42
- Verdict: Pass

## Scope Reviewed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`

## Findings

No actionable findings.

## Architecture Notes

- The movement animation override is correctly owned by `TosterHexUnit`, where
  the movement animation state is selected during `SetHex(...)`.
- `Rush` uses the override only around the existing `DoMoves(...)` coroutine,
  so the movement/pathing system remains unchanged.
- The override is cleared in a `finally` block, which reduces the risk of later
  ordinary movement inheriting `run`.
- The early Rush audio path uses the existing `SkillPresentationCatalog` lookup
  and plays only `castSfx`, avoiding a second skill identity or unit-local audio
  configuration.
- The post-arrival path uses a separate impact-only sequence, so regular
  sequenced presentation behavior remains unchanged for other skills.
- No serialized fields, public fields, Unity assets, prefabs, scenes, Animator
  Controllers, gameplay float values, targeting rules, cooldowns, movement
  distances, or damage/status values were changed.

## Residual Risk

- Unity compile was not run by the agent, per project policy.
- Play Mode validation is still required to confirm the Rusher Animator has a
  `run` state and that the configured `Rush` catalog entry has the intended
  `castSfx`.
- If a future movement skill also needs animation override, it should use the
  same `TosterHexUnit` methods rather than writing directly to Animator state.

## QA Recommendation

Proceed to manual Unity validation. Prioritize:

1. `Rush` to empty highlighted hex: immediate `castSfx`, `run` movement,
   post-arrival `skillN`, impact feedback, no repeated cast SFX.
2. `Rush` toward enemy: same sequence plus existing target follow-up behavior.
3. Normal movement after Rush: animation returns to `walk`.

