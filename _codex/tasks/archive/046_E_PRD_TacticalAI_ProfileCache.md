# [TARENA] PRD046-E: TacticalAIProfile Fixed Budget And Plan Cache

- Status: implemented-qa-pass-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, ScriptableObject Profile, Cache
- Label: needs-grill
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`

## Problem Statement

Tactical AI must be tunable by designers, but its strength must not scale with
the player's computer. A faster CPU or more RAM must not make the AI search
deeper or wider than intended.

Create `TacticalAIProfile` as a `ScriptableObject` type and a Normal profile
asset that define fixed AI strength budget, scoring weights, action biases, and
watchdog settings. Add a safe advisory plan cache keyed by snapshot and profile
state.

## Scope

This PRD defines profile data, runtime default behavior, fixed-budget rules,
the initial Normal profile asset, and cache behavior. It does not require scene
or prefab wiring of that asset.

## Implementation Decisions

- Add a `TacticalAIProfile : ScriptableObject` type.
- Add an initial Normal `TacticalAIProfile` asset in the project asset tree.
- AI may reference a `TacticalAIProfile` asset when one is assigned.
- If no asset is assigned, AI uses a runtime default Normal profile.
- Runtime default values may live in C# factory/static method.
- Profile fixed limits define AI strength, not hardware performance.
- The 300 ms limit is a watchdog/failsafe, not the main strength budget.
- Faster machines must not search beyond profile depth, beam width, or
  candidate limits.
- Cache is advisory only and never bypasses live revalidation.

## Profile Model

Minimal model:

```text
TacticalAIProfile
- difficultyName
- searchDepthPlies
- decisionWatchdogMs
- ownActionBeam
- enemyResponseBeam
- maxCandidatesPerActionType
- maxSkillCandidates
- maxMoveCandidates
- maxAttackCandidates
- maxFallbackCandidates
- requireOpponentResponseWhenReachable
- deterministicTieBreakMode
- stableTieBreakSeed
- scoringWeights
- actionTypeBiases

TacticalAIScoringWeights
- enemyValueRemoved
- ownValueLost
- enemyStackKillBonus
- ownStackLossPenalty
- winScore
- lossScore
- damageEfficiency
- positionSafety
- threatControl
- progressTempo

TacticalAIActionTypeBiases
- skill
- attack
- moveAndAttack
- move
- defend
- wait
```

Recommended Normal defaults:

```text
searchDepthPlies = 3
decisionWatchdogMs = 300
ownActionBeam = 8
enemyResponseBeam = 5
requireOpponentResponseWhenReachable = true
```

## Fixed Budget Rule

AI strength is defined by profile settings:

```text
depth and opponent-response requirement
beam width
candidate caps
stable ordering
scoring weights
```

The watchdog exists to prevent stalls:

```text
if watchdog expires:
  return best completed depth if available
  else fallback chain
```

The watchdog must not be used as "search as much as possible in 300 ms".

## Cache Model

```text
TacticalAIPlanCacheKey
- snapshotHash
- activeUnitId
- profileHash

TacticalAIPlanCacheValue
- orderedActionIntents
- bestScore
- completedDepth
```

Rules:

- cache hit is advisory only,
- selected cached intent must be live-revalidated,
- stale/mismatched cache is discarded,
- cache cannot increase depth, beam width, or candidate count,
- profile changes must change `profileHash`,
- cache can be ignored safely if unavailable.

## Testing Decisions

Add deterministic tests for:

- runtime default Normal profile values,
- initial Normal profile asset exists with the recommended default values,
- profile hash changes when budget or weights change,
- faster execution does not increase searched depth/candidate count,
- watchdog fallback,
- cache hit by matching snapshot/profile,
- cache rejection on snapshot hash mismatch,
- cache rejection on profile hash mismatch,
- cached intent still goes through revalidation path.

## Acceptance Criteria

Done when:

- `TacticalAIProfile` ScriptableObject type exists,
- initial Normal profile asset exists,
- runtime default Normal profile exists,
- fixed budget limits are profile-driven,
- watchdog is implemented as safety only,
- cache keys include snapshot hash, active unit id, and profile hash,
- cache never bypasses live revalidation.

## Out Of Scope

- Wiring the profile asset into scenes, prefabs, or run-level setup unless
  explicitly scoped later.
- Dynamic CPU-scaled AI strength.
- Difficulty stat bonuses.
- Hidden cooldown, damage, movement, or legality changes.

## Implementation - 2026-06-24

### What Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIProfile.cs` adds the core Tactical AI profile seam: `TacticalAIProfile` asset data, runtime Normal fallback resolution, deterministic profile hashing, a fixed-budget helper, and advisory plan-cache key/value types tied to `BattleSnapshot`.
- `TArenaUnity3D/Assets/Resources/0_Data/TacticalAIProfile_Normal.asset` adds the first real Normal profile asset with the recommended defaults from PRD046-E.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIProfileTests.cs` adds focused EditMode coverage for runtime defaults, asset defaults, profile-hash invalidation, fixed-budget clamps, watchdog fallback, and advisory cache hit/miss behavior.
- `TacticalAIProfile.difficultyName` affects profile identity and logging. Useful range: short stable labels such as `Normal`, `Hard`, `Boss`. Lower/higher does not change gameplay directly; changing the label helps readability but hash/behavior comes from the real tuning fields. Tuning hint: keep names stable and use the hash, not the label, as the real cache invalidation signal.
- `TacticalAIProfile.searchDepthPlies` affects how many tactical opportunities the future search may inspect. Useful range: `1-5`. Lower values make AI shallower and faster; higher values make it deeper and more expensive. Tuning hint: keep `Normal` at `3` until PRD046-D lands so one action, one response, and one follow-up stay aligned.
- `TacticalAIProfile.decisionWatchdogMs` affects the safety cutoff, not AI strength. Useful range: about `100-500`. Lower values make watchdog fallback trigger sooner; higher values allow more time before safety fallback. Tuning hint: do not use this as a strength dial; use depth, beams, and candidate caps first.
- `TacticalAIProfile.ownActionBeam` affects how many top AI-side branches survive pruning per ply. Useful range: about `4-16`. Lower values make planning narrower and cheaper; higher values keep more options and raise cost. Tuning hint: raise this before raising depth if AI misses obvious aggressive branches.
- `TacticalAIProfile.enemyResponseBeam` affects how many opponent-response branches survive pruning. Useful range: about `3-12`. Lower values make counterplay coverage thinner; higher values make AI respect more opponent replies. Tuning hint: raise this when AI sees its own plan but undervalues the enemy answer.
- `TacticalAIProfile.maxCandidatesPerActionType` affects the global per-type cap before search/scoring. Useful range: about `4-16`. Lower values prune more aggressively; higher values let more legal options through. Tuning hint: keep this at or above any specific skill/move/attack cap you want to matter.
- `TacticalAIProfile.maxSkillCandidates` affects how many skill candidates survive within the skill bucket. Useful range: about `2-12`. Lower values make skill consideration narrower; higher values let more legal skills compete. Tuning hint: raise this when kit-heavy units feel underused.
- `TacticalAIProfile.maxMoveCandidates` affects how many move-only candidates survive within the move bucket. Useful range: about `2-12`. Lower values make repositioning more selective; higher values broaden movement exploration. Tuning hint: raise this only when position play matters more than immediate damage lines.
- `TacticalAIProfile.maxAttackCandidates` affects how many direct attack candidates survive within the attack bucket. Useful range: about `2-12`. Lower values focus on the clearest attacks; higher values preserve more trade options. Tuning hint: keep this high enough that obvious kills are not pruned too early.
- `TacticalAIProfile.maxFallbackCandidates` affects how many fallback intents remain cached after the best line. Useful range: about `1-8`. Lower values make fallback shorter; higher values keep more safe backup actions. Tuning hint: raise this only if live revalidation starts rejecting the top line often in later slices.
- `TacticalAIProfile.requireOpponentResponseWhenReachable` affects whether Normal-depth planning must include at least one reachable enemy reply. Useful range: `false` or `true`. `false` allows own-side-only depth completion; `true` enforces counterplay coverage when available. Tuning hint: keep `Normal` at `true` to avoid fake "3-ply" lines that never see a response.
- `TacticalAIProfile.deterministicTieBreakMode` affects stable ordering when scores tie. Useful range: currently `StableSeededOrder`. Lower/higher does not apply yet because only one deterministic mode is implemented. Tuning hint: keep one stable mode until later AI slices prove a real need for alternatives.
- `TacticalAIProfile.stableTieBreakSeed` affects seeded deterministic ordering for equal-score branches. Useful range: any non-negative integer, typically small fixed values. Lower/higher changes deterministic branch preference without changing legality. Tuning hint: change this only when you intentionally want a different stable flavor, not for strength.
- `TacticalAIProfile.scoringWeights` affects future search evaluation: `enemyValueRemoved`, `ownValueLost`, `enemyStackKillBonus`, `ownStackLossPenalty`, `winScore`, `lossScore`, `damageEfficiency`, `positionSafety`, `threatControl`, and `progressTempo`. Useful ranges: usually small positive floats for heuristics and larger terminal values for win/loss. Lower values weaken that scoring signal; higher values strengthen it. Tuning hint: keep `ownValueLost` slightly above `enemyValueRemoved` on Normal so bad trades stay unattractive.
- `TacticalAIProfile.actionTypeBiases` affects tie-break nudges between `skill`, `attack`, `moveAndAttack`, `move`, `defend`, and `wait`. Useful range: small magnitudes around `-0.25` to `0.25`. Lower values discourage that action type; higher values prefer it when board value is similar. Tuning hint: keep these small so legality and board score dominate instead of bias-only behavior.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIProfileTests.cs`.
- The tests check runtime Normal defaults, the real Normal profile asset defaults, profile-hash changes after budget/weight edits, fixed-budget depth/beam/candidate caps, watchdog fallback selection, and advisory cache hit/miss behavior for matching and mismatched snapshot/profile hashes.
- These tests do not require scene or prefab setup because they use pure profile data plus handcrafted `BattleSnapshot` inputs through `BattleSnapshotBuilder`.
- Tests were not run automatically. Run them manually in Unity at `Window > General > Test Runner`, open the `EditMode` tab, and run `TacticalAIProfileTests`. Expected result: all tests pass.

### Unity Test

#### Unity Setup

- No scene, prefab, or Inspector wiring is required for runtime gameplay because this slice does not attach the profile to live AI yet.
- In the Project window, open `Assets/Resources/0_Data/TacticalAIProfile_Normal.asset` and confirm the Normal values: depth `3`, watchdog `300`, own beam `8`, enemy beam `5`, opponent-response requirement enabled, and the candidate caps shown above.
- Open `Window > General > Test Runner`, switch to `EditMode`, and select `TacticalAIProfileTests` before pressing Run.

#### Play Mode Test

- Press Play in an existing tactical battle scene and confirm battles behave exactly as before; this slice should not change movement, attacks, skills, cooldowns, or turn flow yet.
- If you inspect a debugger watch or temporary call site that resolves `TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null)`, confirm it returns the code-defined Normal values even with no assigned asset reference.
- If you temporarily inspect later integration code that uses `TacticalAIFixedBudget`, confirm fast execution time does not allow depth beyond `3` or beams beyond `8`/`5`; only the watchdog should stop search early.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-24_2018_046_E_QA_ArchitectureReview.md`
- Actionable findings: none.
- Non-blocking observations: the split between explicit asset loading and runtime default resolution matches PRD046-E well; the watchdog remains a safety cutoff instead of a strength dial; the advisory cache stays out of live validation ownership.
- Follow-up fixes applied: none required after QA.

### Notes

- Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.
- `TacticalAIPlanCache` is intentionally advisory only in this slice. It stores ordered fallback intents by `snapshotHash`, `activeUnitId`, and `profileHash`, but it does not execute or validate them.
- `ResolveAssignedOrRuntimeDefault(null)` intentionally returns a code-defined Normal profile instead of auto-binding the `Resources` asset, because this PRD explicitly keeps asset wiring out of scope.
- This slice intentionally stops before PRD046-B, PRD046-C, PRD046-D, and PRD046-F integration, so there is no live Tactical AI behavior change yet.

### Next Steps

- Run `TacticalAIProfileTests` manually in Unity EditMode Test Runner.
- Let Unity import and serialize `TacticalAIProfile_Normal.asset`, then confirm the Inspector values stayed intact.
- Use this profile/budget/cache seam as the configuration source for PRD046-B candidate generation and PRD046-D search.
- Keep live intent revalidation and execution routing in PRD046-C/046-F so cached plans never bypass legality checks.
