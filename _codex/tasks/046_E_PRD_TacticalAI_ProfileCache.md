# [TARENA] PRD046-E: TacticalAIProfile Fixed Budget And Plan Cache

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, ScriptableObject Profile, Cache
- Label: needs-grill
- Parent: `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`

## Problem Statement

Tactical AI must be tunable by designers, but its strength must not scale with
the player's computer. A faster CPU or more RAM must not make the AI search
deeper or wider than intended.

Create `TacticalAIProfile` as a `ScriptableObject` type that defines fixed AI
budget, scoring weights, action biases, and watchdog settings. Add a safe
advisory plan cache keyed by snapshot and profile state.

## Scope

This PRD defines profile data, runtime default behavior, fixed-budget rules,
and cache behavior. It does not require creating or wiring a `.asset` instance.

## Implementation Decisions

- Add a `TacticalAIProfile : ScriptableObject` type.
- AI may reference a `TacticalAIProfile` asset when one is assigned.
- If no asset is assigned, AI uses a runtime default Normal profile.
- Runtime default values may live in C# factory/static method.
- Profile fixed limits define AI strength.
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
```

## Fixed Budget Rule

AI strength is defined by profile settings:

```text
depth
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
- runtime default Normal profile exists,
- fixed budget limits are profile-driven,
- watchdog is implemented as safety only,
- cache keys include snapshot hash, active unit id, and profile hash,
- cache never bypasses live revalidation.

## Out Of Scope

- Creating or wiring a `.asset` file unless explicitly scoped later.
- Dynamic CPU-scaled AI strength.
- Difficulty stat bonuses.
- Hidden cooldown, damage, movement, or legality changes.

