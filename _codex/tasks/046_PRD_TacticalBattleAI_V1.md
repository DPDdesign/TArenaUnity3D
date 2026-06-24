# [TARENA] PRD: Tactical Battle AI V1

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, Battle Snapshot, Action Validation, Skills
- Label: needs-grill
- Related: `_codex/Context/AI_Context.md`
- Related: `_codex/Context/BattleActionRules.md`
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- Related: `_codex/Documentation/ADR_013_TacticalAI_CastManagerSkillBridge.md`
- Related: `_codex/tasks/018_PRD_BattleActionLifecycleFullMigration.md`
- Related: `_codex/tasks/034_PRD_FutureSkillLogicAndTargetingExtraction.md`
- Related: `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

## Split PRDs

PRD046 is split into child PRDs for review and implementation planning:

- `_codex/tasks/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- `_codex/tasks/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- `_codex/tasks/046_C_PRD_TacticalAI_LifecycleExecutionBridge.md`
- `_codex/tasks/046_D_PRD_TacticalAI_SearchScoring.md`
- `_codex/tasks/046_E_PRD_TacticalAI_ProfileCache.md`
- `_codex/tasks/046_F_PRD_TacticalAI_CastManagerSkillBridge.md`

Current confirmed direction:

- Tactical AI V1 still targets 3-ply search from the first implementation.
- A ply follows estimated snapshot turn order, not strict team alternation.
- All active current skills may be considered as candidates.
- Skill prediction is approximate in V1; no full skill prediction rewrite and
  no separate skill prediction catalog.
- Selected actions and skills must revalidate live state and execute through
  existing `MouseControler`, `CastManager`, and `BattleActionLifecycle` paths.
- `TacticalAIProfile` defines fixed depth, beam widths, candidate caps,
  scoring weights, action biases, deterministic ordering, and watchdog.
- AI strength is profile-budgeted, not CPU-scaled. The 300 ms decision budget
  is a watchdog/failsafe.
- A simple advisory cache is allowed, keyed by snapshot hash, active unit id,
  and profile hash, but it never bypasses live revalidation.

## Problem Statement

The current battle AI is too shallow for TArena tactical battles. It mainly
looks for a direct move-and-attack opportunity or moves toward the nearest
enemy, so it does not play like a win-focused opponent. It does not reason
about the whole battle state, does not evaluate counterplay, does not make
meaningful skill choices, and is hard to tune through difficulty or run setup.

The desired first AI is not a strategic run AI. It is a Tactical Battle AI that
plays the current battle to win, uses legal actions, can use skills through the
current skill system, and can be tuned through a `ScriptableObject` profile
rather than code changes.

## Solution

Build Tactical Battle AI V1 as a team-level tactical brain.

The AI reads a full battle snapshot, evaluates the whole map and both teams,
searches several plies ahead within a compute budget, and outputs one legal
action intent for the currently active unit. The selected intent must be
revalidated against the live battle state and executed through the existing
battle action lifecycle.

For the first supported profile, `Normal`, the AI should target:

- full information, same as the player,
- win-focused scoring,
- 3-ply search depth,
- about 300 ms synchronous decision budget,
- async/background planning during the player turn where possible,
- cached plans invalidated by battle snapshot hash,
- legal skill usage through the current `CastManager` bridge,
- no hidden stat bonuses, illegal casts, or rule bypasses.

Difficulty affects only search depth, beam width, time budget, and profile
weights. It must not change unit stats, cooldowns, damage, targeting rules, or
legal action rules.

## User Stories

1. As a player, I want the enemy AI to play to win, so that battles feel like
   tactical contests rather than scripted movement.
2. As a player, I want the AI to use the same legal actions I can use, so that
   wins and losses feel fair.
3. As a player, I want the AI to use skills, attacks, wait, and defend, so that
   enemy turns use the full battle system.
4. As a player, I want the AI to consider my possible response, so that it does
   not make obviously bad trades.
5. As a player, I want the AI to value killing enemy stacks, so that it tries
   to close games instead of only moving.
6. As a player, I want the AI to avoid needless losses, so that it protects its
   own army when that helps it win.
7. As a player, I want the AI to avoid stalling, so that it does not choose
   endless defensive or waiting lines when progress is available.
8. As a designer, I want AI difficulty to be controlled by a profile, so that I
   can tune Normal or later profiles without code edits.
9. As a designer, I want `Normal` difficulty to search about 3 plies, so that
   it can see one action, one response, and one follow-up.
10. As a designer, I want the AI profile to expose compute limits, so that I
    can trade strength against thinking time.
11. As a designer, I want skill usage to be slightly preferred over plain
    attack when both are good, so that units feel like their kits matter.
12. As a designer, I want attacks to be preferred over wait and defend when
    they create value, so that the AI is proactive.
13. As a designer, I want wait and defend to remain legal fallback actions, so
    that the AI can still act when no aggressive action is good.
14. As a designer, I want movement value to come from resulting position and
    follow-up options, so that movement is not rewarded just for happening.
15. As a developer, I want the AI to read a pure battle snapshot, so that search
    does not mutate live Unity scene objects.
16. As a developer, I want the AI to output an action intent, so that execution
    stays separate from planning.
17. As a developer, I want AI, player input, and future network commands to
    converge on the same validation seam, so that action legality does not
    drift by caller.
18. As a developer, I want the selected AI action to run through the battle
    lifecycle, so that turn completion, blocking presentation, and action
    consumption remain centralized.
19. As a developer, I want background AI planning to be advisory, so that stale
    cached plans are never executed after the board changes.
20. As a developer, I want a fallback chain, so that the AI always produces a
    valid action or a safe no-op when search is interrupted.
21. As a developer, I want the AI to be ready for future persisted battle-state
    snapshots, so that online sync, replay, and AI can share one state shape.
22. As a future multiplayer developer, I want battle snapshots to be
    deterministic-friendly and free of Unity object references, so that they can
    later support sync or replay.
23. As QA, I want a deterministic test seam for scoring and action selection,
    so that AI behavior can be regression tested without full scene setup.
24. As QA, I want Play Mode checks for live execution, so that AI-selected
    skills and actions are proven to enter the same runtime path as player
    actions.

## Implementation Decisions

- Tactical Battle AI V1 is a tactical battle feature only. It does not choose
  run paths, encounters, rewards, saved-army defence, or strategic opponent
  selection.
- The AI is a team-level tactical brain. It evaluates the whole battle map and
  both teams, but emits exactly one action intent for the currently active unit.
- The AI uses full information. Player and AI both have full visible battle
  information for this scope.
- The AI must be 100 percent legal. Difficulty must never grant hidden stats,
  ignore cooldowns, ignore targeting rules, bypass action timing, or execute
  invalid skills.
- The AI output is a battle action intent, not direct mutation of the board.
  The live scene must revalidate the intent and execute it through the current
  battle lifecycle.
- Introduce a pure `BattleSnapshot` model for AI planning. It contains the full
  tactical map, units/stacks, teams, positions, health/amounts, turn/action
  state, cooldown-relevant state, status-relevant state needed by V1, and stable
  skill ids. It contains no Unity object references.
- Treat `BattleSnapshot` as future-compatible with persistence, replay, and
  online sync. V1 can keep snapshots in memory, but the shape should be able to
  become the payload for a future battle-state table linked to run battles.
- Snapshot updates for AI planning should happen after completed actions, once
  the action lifecycle has returned the model to a stable state.
- Keep army snapshots and tactical battle-state snapshots separate. Army
  snapshots remain run/metagame state. Full battle snapshots are tactical map
  state.
- Add a `TacticalAIProfile` `ScriptableObject` for all AI tuning parameters.
  It should be attachable from run/encounter setup later, but V1 may use a
  default Normal profile if no run-level reference is available.
- `TacticalAIProfile` contains parameters, not runtime search state.
- The Normal profile should use 3 plies as the starting search depth.
- The Normal profile should use about 300 ms as the synchronous decision budget.
- The Normal profile should expose beam widths. Initial defaults are own action
  beam 8 and enemy response beam 5.
- The profile should expose `maxActionSequenceLength = 3` for legal sequences
  inside one action opportunity.
- A move-and-attack is treated as one composite action for sequence planning.
- The profile should expose scoring weights for enemy value removed, own value
  lost, kill bonus, own stack loss penalty, action type bias, and tempo/progress.
- Initial scoring direction: enemy value removed is positive, own value lost is
  negative and slightly stronger, killing a stack is a bonus, losing a stack is
  a penalty.
- The AI should include a tempo/progress weight so it does not choose endless
  waiting, defending, or non-progressing repositioning when useful progress is
  available.
- Action type bias is only a tie-breaker or small nudge. Skill should be
  slightly preferred, then attack, then wait, then defend, but board value and
  legality dominate.
- Movement has no inherent positive bias. Movement is valuable when it enables
  damage, skill use, safer position, better threat control, or better future
  action.
- Search should use pruning/beam search rather than enumerating every possible
  long line to completion. It can discard weak candidate branches once they are
  clearly worse under the profile budget.
- Candidate pruning must avoid dropping tactically critical actions too early,
  especially stack kills, high-damage skills, moves out of lethal danger, and
  legal actions that prevent an immediate loss.
- The fallback chain is: legal cached plan for matching snapshot hash, best
  completed shallower search, greedy immediate legal action, defend, wait.
- Background planning may run during the player's turn, but it is advisory.
  Cached plans must be keyed by snapshot hash and revalidated before execution.
- V1 skill use goes through the existing skill bridge described by ADR 013.
  The AI may treat skills approximately during search, but live execution must
  still use the existing skill legality/execution path.
- Do not create a separate skill prediction catalog in V1. Future skill
  prediction data should extend the existing skill catalog and stable skill id
  model.
- Full extraction of skill targeting, validation, prediction, and execution out
  of the legacy skill flow is out of scope for this PRD and remains covered by
  the future skill extraction PRD.
- The implementation should prefer deep, testable modules:
  `BattleSnapshot` construction and hashing, action intent generation,
  validation adapter, search/evaluator, profile data, cache/background planner,
  and live execution adapter.

## Testing Decisions

- Good AI tests should assert externally observable decisions and legality, not
  private search internals.
- Add deterministic EditMode-style tests where a pure C# seam exists, especially
  for snapshot hashing, candidate generation, fallback selection, scoring, and
  profile-driven budget behavior.
- Test the Normal profile against small handcrafted battle snapshots:
  immediate kill, avoid own stack loss, prefer winning trade, avoid illegal
  skill, move to attack, no attack available, defend fallback, wait fallback,
  and stale cache invalidation.
- Test that changing depth/budget/profile weights changes search behavior
  without changing action legality.
- Test that the AI emits one intent for the active unit while evaluating the
  whole team state.
- Test that cached plans are rejected when the snapshot hash does not match.
- Test that every selected intent is passed through the validator seam before
  live execution.
- Test that skill ids remain stable across snapshot, intent, validation, and
  execution.
- Use Play Mode/manual validation for live scene behavior that depends on
  `MonoBehaviour`, presentation, path visuals, and legacy skill execution.
- Manual Play Mode checks should cover movement, move-and-attack, basic ranged
  attack, wait, defend, at least several active skills, skill cooldown/target
  rejection, action lifecycle blocking, and turn release after AI action.
- Unity compilation and Unity Test Runner execution remain user-side unless a
  future implementation task explicitly allows running Unity commands outside
  the editor.

## Out of Scope

- Strategic/run AI.
- Encounter selection, route selection, reward selection, shop behavior, saved
  army defence selection, or async offence/defence matchmaking.
- Hidden AI stat bonuses, extra actions, cooldown cheating, target cheating, or
  difficulty rules that change gameplay legality.
- Full multiplayer, rollback, online authority, or network transport.
- Persisting every tactical battle snapshot into SQLite in V1.
- Rewriting `CastManager`.
- Extracting all skill targeting, validation, prediction, and execution into
  data in V1.
- Creating a separate skill identity system or a second skill prediction
  catalog.
- Changing skill ids, cooldowns, targeting, ranges, damage, healing, statuses,
  movement values, initiative, turn rules, presentation timing, or balance.
- Editing Unity scenes, prefabs, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` unless a
  later implementation task explicitly expands scope.

## Further Notes

VCMI/Heroes-style AI research supports this direction at a pattern level:
battle AI benefits from explicit candidate actions, hypothetical battle state,
damage/value caches, threat maps, target evaluators, and budgeted search. This
PRD uses those ideas only as architecture inspiration; it does not copy VCMI
code.

Useful references from the research pass:

- `https://github.com/vcmi/vcmi/tree/develop/AI/BattleAI`
- `https://github.com/vcmi/vcmi/tree/develop/AI/Nullkiller2`

Minimal model:

```text
TacticalAIProfile
- difficultyName
- searchDepthPlies
- decisionBudgetMs
- ownActionBeam
- enemyResponseBeam
- maxActionSequenceLength
- scoringWeights
- actionTypeBiases

BattleSnapshot
- map
- units
- teams
- activeUnit
- turnState
- actionState
- skillIds
- snapshotHash

TacticalAIBrain
- read snapshot
- generate legal candidates
- search by profile budget
- score whole battle state
- return one action intent

AI execution
- recheck snapshot/hash
- validate intent
- execute through lifecycle
- discard stale cache
```

Acceptance is reached when a Normal-profile Tactical AI can choose and execute
legal win-focused actions for the active enemy unit, use current skills through
the legacy bridge, respect the battle lifecycle, and expose all meaningful
strength/tuning parameters through `TacticalAIProfile`.
