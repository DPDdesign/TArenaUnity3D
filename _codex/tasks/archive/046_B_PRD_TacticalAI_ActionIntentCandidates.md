# [TARENA] PRD046-B: Tactical AI Action Intents And Candidates

- Status: implemented-qa-pass-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Action Intents, Candidate Generation
- Label: needs-grill
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/archive/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Related: `_codex/Context/BattleActionRules.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`

## Problem Statement

Tactical AI needs an explicit action-intent model so planning can remain
separate from live scene mutation. Current player and AI actions are routed
through legacy methods on `MouseControler`, `TosterHexUnit`, and `CastManager`.
The AI must not directly call those paths during planning.

Create a pure intent and candidate-generation layer that can enumerate legal
snapshot-level actions from a `BattleSnapshot`.

## Scope

This PRD defines the action intent model and candidate generation for:

- wait,
- defend,
- move,
- move-and-attack,
- basic ranged attack,
- every currently legal active skill as a skill intent.

Skill prediction remains approximate and is detailed in PRD046-F. This PRD
only requires that skill candidates can be represented and considered.

## Implementation Decisions

- AI outputs an action intent, not direct board mutation.
- Candidate generation reads `BattleSnapshot`, not live scene objects.
- Live execution revalidates an intent before execution.
- Move-and-attack is one composite action.
- Candidate generation should intentionally produce legal candidates according
  to snapshot-level rules. It should not emit broad "maybe legal" placeholders
  just to let execution reject them later.
- Candidate generation should produce a stable ordering before scoring.
- Stance/toggle skills are represented as skill intents only if they are legal
  and useful to consider.
- Passive skills are not direct action candidates.
- Candidate generation should not create a second action validation authority.
  It must be as legal as the snapshot model can support, while live execution
  remains authoritative for final validation.

## Intent Model

Minimal model:

```text
TacticalAIActionIntent
- actionType
- actorUnitId
- sourceHex
- destinationHex optional
- targetUnitId optional
- targetHex optional
- skillSlot optional
- skillId optional
- predictedPriority / candidate metadata optional

Action types
- Wait
- Defend
- Move
- MoveAndAttack
- BasicRangedAttack
- Skill
```

## Candidate Rules

Candidate generation must consider:

- active unit only for the current ply,
- current movement/action flags,
- wait/defend restrictions from `BattleActionRules`,
- occupied and empty hexes,
- movement range approximation,
- melee adjacency for move-and-attack,
- basic ranged attack availability for ranged units,
- active skill cooldowns and used-skill rules,
- skill ids by slot.

The first candidate generator should keep movement bounded by the profile
limits defined in PRD046-E, such as max move candidates and max candidates per
action type. Candidate ordering and pruning should align with the main scoring
direction: progress/attack when useful, defense when valuable, and strong
penalty for avoidable own losses.

## Skill Candidate Rule

All currently legal active skills may be candidates. Passive skills are skipped.
This does not mean pure prediction for every skill is perfect in V1.

Planning:

```text
Skill candidate legal in the snapshot-level model
-> approximate score
-> selected only if it wins search
```

Execution:

```text
Selected skill intent
-> live revalidation
-> CastManager / MouseControler / BattleActionLifecycle path
```

## Testing Decisions

Add deterministic tests for:

- wait candidate available only before movement and before non-stance skills,
- defend candidate available only before movement and before non-stance skills,
- move candidates exclude occupied destinations,
- move-and-attack candidates include legal adjacent attack positions,
- basic ranged attack candidates target enemies,
- passive skills are not candidates,
- cooldown-blocked skills are not candidates,
- candidate ordering is stable.

## Acceptance Criteria

Done when:

- a pure `TacticalAIActionIntent` model exists,
- a candidate generator can enumerate basic action candidates from a snapshot,
- all currently legal active skills can be represented as candidates,
- candidate generation does not mutate live Unity objects,
- generated candidates are stable and profile-limit aware,
- live execution remains the authority for final legality.

## Out Of Scope

- Executing intents.
- Full action validation rewrite.
- Perfect skill simulation.
- Changing skill ids, cooldowns, targeting, range, damage, movement, status, or
  turn rules.

## Implementation - 2026-06-24

### What Changed

- Added a pure Tactical AI intent model in `TacticalAIActionIntent.cs` for:
  - explicit action types: `Wait`, `Defend`, `Move`, `MoveAndAttack`, `BasicRangedAttack`, `Skill`;
  - snapshot-safe source/destination/target hex coordinates;
  - actor id, target unit id, and skill slot/id pairing;
  - deterministic stable-order metadata for candidate tie-breaks.
- Added `TacticalAICandidateGenerationOptions` in `TacticalAIActionIntent.cs` so candidate generation already has profile-style caps before PRD046-E lands:
  - `MaxCandidatesPerActionType`;
  - `MaxSkillCandidates`;
  - `MaxMoveCandidates`;
  - `MaxAttackCandidates`.
- Added a skill metadata provider seam in `TacticalAIActionIntent.cs`:
  - `ITacticalAISkillMetadataProvider`;
  - `TacticalAISkillMetadata`;
  - `TacticalAIDataMapperSkillMetadataProvider`.
- Added a pure `TacticalAICandidateGenerator` in `TacticalAICandidateGenerator.cs` that reads only `BattleSnapshot` data and emits snapshot-level candidates for:
  - wait;
  - defend;
  - move to reachable empty hexes;
  - melee `MoveAndAttack` from a reachable adjacent hex, including the current hex when already adjacent;
  - `BasicRangedAttack` against alive enemy units for ranged actors;
  - legal active skill slot/id candidates while excluding passive, cooldown-blocked, already-used, or post-move illegal skills.
- Added deterministic EditMode coverage in `TacticalAICandidateGeneratorTests.cs` for wait/defend gating, occupied move exclusion, melee move-and-attack adjacency, ranged enemy targeting, passive/cooldown skill exclusion, and stable ordering across equivalent snapshots.
- No Inspector fields changed.

### Automatic Test

- Added `TacticalAICandidateGeneratorTests` under `Assets/Scripts/Tests/EditMode/`.
- These tests check:
  - `WaitAndDefend_AreAvailableOnlyBeforeMovementAndNonToggleSkillUse`: wait/defend appear only in legal pre-move, pre-skill states.
  - `MoveCandidates_ExcludeOccupiedDestinations`: movement does not emit occupied hexes.
  - `MoveAndAttackCandidates_IncludeCurrentHexWhenAlreadyAdjacent`: melee attack intent exists even when no reposition is needed.
  - `BasicRangedAttackCandidates_TargetEnemyUnitsOnly`: ranged candidates target enemies, not allies.
  - `PassiveAndCooldownBlockedSkills_AreNotCandidates`: passive and cooldown-blocked skills are filtered while legal active slot/id pairs remain.
  - `CandidateOrdering_IsStableAcrossEquivalentSnapshots`: equivalent snapshot state yields the same deterministic candidate order.
- The tested logic does not require scene or prefab setup because the tests build handcrafted `BattleSnapshot` data and run the generator as a pure C# seam.
- Tests were not run automatically. Run them manually in Unity Test Runner:
  - open `Window > General > Test Runner`;
  - choose the `EditMode` tab;
  - run `TacticalAICandidateGeneratorTests`.
- Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No new Inspector assignments are required.
- No scene, prefab, material, controller, `.inputactions`, `.asmdef`, or `.asmref` changes are required for this slice.
- Open an existing tactical battle scene that already contains `HexMap`, `MouseControler`, `TurnManager`, and the PRD046-A snapshot seam.
- For manual runtime inspection, use a debugger watch, immediate evaluation, or a temporary local debug hook that calls:
  - `BattleSnapshot snapshot = BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot();`
  - `var candidates = TacticalAICandidateGenerator.GenerateCandidates(snapshot);`

#### Play Mode Test

- Enter Play Mode and wait until one tactical unit becomes the active unit.
- Capture a live `BattleSnapshot` from `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()`.
- Generate candidates from that snapshot with `TacticalAICandidateGenerator.GenerateCandidates(snapshot)`.
- Confirm the result contains intents only for the active unit id from the snapshot.
- Confirm `Wait` is present only before movement and before non-toggle skill use.
- Confirm `Defend` is absent after moved/used-skill states and otherwise appears as a legal fallback.
- Confirm move candidates target reachable empty hexes only.
- Confirm melee units emit `MoveAndAttack` candidates for alive enemies when an adjacent reachable attack position exists.
- Confirm ranged units emit `BasicRangedAttack` candidates against alive enemy units.
- Confirm passive or cooldown-blocked skills are absent while legal active skill slot/id pairs remain present.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-24_2037_046_B_QA_ArchitectureReview.md`
- Actionable findings: none.
- Non-blocking observations:
  - skill candidates are intentionally conservative in this slice and do not yet expand full target geometry;
  - if future AI planning must support non-legacy map topology, snapshot/model seams should carry that topology explicitly instead of inferring the current legacy neighbour contract.
- Follow-up fixes applied: none required after QA.

### Notes

- This slice stops at pure intent and candidate generation. It does not execute intents, revalidate live state, search plies ahead, score branches, or create the later `TacticalAIProfile` asset path.
- Skill candidates currently represent legal active slot/id opportunities without mutating live `CastManager` mode state during planning.
- Movement and melee reachability use a pure snapshot neighbour/path approximation aligned with the current legacy tactical map contract.
- Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

### Next Steps

- Run `TacticalAICandidateGeneratorTests` manually in Unity EditMode Test Runner.
- Do one Play Mode inspection pass in a tactical battle scene and verify the generated candidate set against the current active unit state.
- Use this intent/candidate seam as the planning input for PRD046-C live execution bridging and PRD046-D search/scoring work.
