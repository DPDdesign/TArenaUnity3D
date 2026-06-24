# [TARENA] PRD046-F: Tactical AI CastManager Skill Bridge

- Status: implemented-qa-pass-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Skills, CastManager Bridge
- Label: needs-grill
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/archive/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Depends on: `_codex/tasks/archive/046_C_PRD_TacticalAI_LifecycleExecutionBridge.md`
- Related: `_codex/Documentation/ADR_013_TacticalAI_CastManagerSkillBridge.md`
- Related: `_codex/tasks/034_PRD_FutureSkillLogicAndTargetingExtraction.md`

## Problem Statement

Tactical AI V1 must use skills, but the project is not doing a full skill
prediction rewrite now. Current skill legality and execution are still owned by
legacy `CastManager`, `MouseControler`, skill ids, cooldowns, targeting flags,
and runtime scene state.

Build a bridge that lets AI consider every currently legal active skill as a
candidate, score skills approximately in pure planning, and execute selected
skills through the existing live skill path.

## Scope

This PRD covers all currently legal active skills as AI candidates. It does not
create a new skill prediction catalog and does not migrate skill execution out
of `CastManager`.

## Implementation Decisions

- All currently legal active skills should be eligible as AI candidates.
- Passive skills are not direct action candidates.
- Stance/toggle skills may be candidates when legal and useful.
- Range stance or similar attack-enabling toggle skills may receive a positive
  heuristic when they improve threat, attack access, or future damage.
- The AI must not mutate live Unity objects during skill planning.
- Skill prediction in V1 is approximate.
- Live execution is authoritative.
- Selected skill intents must revalidate live state immediately before
  execution.
- No separate `SkillPredictionCatalog` is allowed in V1.
- Existing skill ids remain the join key.
- If CastManager bridge performance or prediction quality becomes poor, the
  fix is future skill extraction/prediction work, not a second temporary skill
  catalog.

## Planning Rule

Planning may use:

- skill id,
- skill slot,
- cooldown,
- action flags such as `NI` and `AM`,
- active/passive type from current skill data,
- approximate targeting shape from current bridge behavior where safe,
- conservative heuristics for complex skills.

Planning must not:

- call skill execution methods,
- call `CastManager.startSpell` as prediction,
- mutate `CastManager` mode state in a way that affects live play,
- mutate `TosterHexUnit`,
- alter cooldowns,
- send chat messages,
- play VFX/SFX/animations.

When a skill is too uncertain to predict cleanly, planning should keep the
heuristic bounded and log enough development context to identify that skill for
future extraction.

## Execution Rule

Execution flow:

```text
Selected skill intent
-> resolve live actor
-> sanity-check skill slot and skill id
-> revalidate cooldown/action state/target state
-> use existing MouseControler/CastManager skill flow
-> complete through BattleActionLifecycle
```

If live skill execution rejects the intent, fallback to the next legal intent.
The rejection path should emit a `Debug.LogWarning` or equivalent diagnostic
with actor id, skill slot, skill id, target/destination when available, and the
failure reason when known.

## Approximate Skill Scoring

V1 may score skills with categories such as:

- direct enemy damage,
- AoE damage opportunity,
- self/ally defensive value,
- movement/approach value,
- disabling/debuff value,
- stance/toggle value,
- trap/summon/complex value using conservative heuristics.

Unknown or hard-to-predict skills should not crash planning. They can receive a
bounded heuristic score and rely on live execution for truth.

## Testing Decisions

Add deterministic tests where pure seams exist for:

- passive skills are excluded,
- cooldown-blocked skills are excluded,
- used non-toggle skills are excluded,
- skill slot and skill id stay paired,
- approximate scoring does not mutate snapshot or live objects,
- selected skill intent is revalidated before execution,
- rejected live skill execution falls back to the next intent and logs the
  rejected skill context.

Manual Play Mode validation should cover:

- several direct damage skills,
- AoE skill,
- move/approach skill,
- self/stance or buff skill,
- illegal target rejection,
- cooldown rejection,
- lifecycle completion after skill execution.

## Acceptance Criteria

Done when:

- every currently legal active skill can be represented as a candidate,
- AI planning does not mutate live skill/runtime state,
- selected skill intents execute through current `CastManager` and lifecycle
  paths,
- failed live skill execution falls back safely,
- failed live skill execution logs enough context for later skill-bridge
  improvement,
- no second skill prediction catalog is introduced,
- existing skill ids, cooldowns, targeting, damage, statuses, movement, and
  presentation behavior are not intentionally changed.

## Out Of Scope

- Rewriting `CastManager`.
- Extracting full skill targeting/validation/execution.
- Creating a separate skill prediction catalog.
- Changing skill ids, cooldowns, range, damage, targeting, movement, status,
  VFX/SFX, animation, or turn rules.

## Implementation - 2026-06-24

### What Changed

- Added `TacticalAICastManagerSkillIntentExecutor` as the default `ITacticalAISkillIntentExecutor`. It resolves the live actor/target from a revalidated intent and delegates skill execution to `MouseControler.TryStartSkillAction(...)`.
- Updated `TacticalAIExecutionBridge` so skill intents now have a default CastManager executor. Existing `TacticalAISnapshotProbe` execution uses this automatically.
- Extended `TacticalAIIntentRevalidator` so skill intents preserve and recheck optional `TargetHex`, `TargetUnitId`, and `DestinationHex` before execution.
- Added `MouseControler.TryStartSkillAction(...)` for one explicit validated skill command. It checks live slot/id, action state, CastManager mode/cast methods, target flags, and explicit target hexes before invoking `CastManager.startSpell(...)`.
- Updated `CastManager` with method-existence helpers and `CancelPreparedSkillWithoutCommit()` so rejected AI/network skill attempts can clear prepared mode state without sending false chat/completion.
- No Inspector fields changed.

### Automatic Test

- Updated `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`.
- Added coverage for skill slot/id preservation, target hex carry-through, used non-toggle skill rejection, and repeatable toggle acceptance after prior use.
- These tests use handcrafted `BattleSnapshot` data and do not require scene or prefab setup.
- Tests were not run automatically. Run them manually in Unity: `Window > General > Test Runner`, `EditMode`, then run `TacticalAIExecutionBridgeTests`. Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No scene, prefab, material, controller, `.inputactions`, `.asmdef`, or `.asmref` edits are required.
- Open an existing tactical battle scene with `HexMap`, `MouseControler`, `TurnManager`, `CastManager`, and `BattleActionLifecycle`.
- For manual probing, add or use `TacticalAISnapshotProbe` on a temporary scene object if it is not already present.

#### Play Mode Test

- Enter Play Mode and wait until a tactical unit is active.
- Use `TacticalAISnapshotProbe` to capture a snapshot, generate candidates, then run `Execute Tactical AI Candidates Through Bridge`.
- Verify skill intents with explicit valid targets route through `MouseControler` and `CastManager`, then complete through `BattleActionLifecycle`.
- Verify stale actor, cooldown, invalid target, and missing target cases reject and continue through the PRD046-C fallback queue.
- Verify staged multi-click skills reject with a diagnostic reason instead of leaving CastManager in a hidden prepared state.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-24_2126_046_F_QA_ArchitectureReview.md`
- Actionable findings: none.
- Non-blocking observations: target-aware skill candidates still need to be emitted by future candidate/search work; staged legacy skills need explicit multi-step intent representation before enabling.
- Follow-up fixes applied: none required after QA.

### Notes

- The bridge does not choose targets. It executes the explicit target/destination carried by the intent, which keeps the path compatible with future multiplayer/network command validation.
- Current `TacticalAICandidateGenerator` still emits generic skill candidates without target geometry, so those will reject until a planner supplies explicit skill targets.
- No `SkillPredictionCatalog` was added and no skill balance values were intentionally changed.

### Next Steps

- Run `TacticalAIExecutionBridgeTests` manually in Unity EditMode Test Runner.
- Run a Play Mode pass with `TacticalAISnapshotProbe.Execute Tactical AI Candidates Through Bridge`.
- In the next AI planning slice, emit target-aware skill intents for direct enemy, ally/self, empty-hex, AoE, and movement/approach skill categories.
