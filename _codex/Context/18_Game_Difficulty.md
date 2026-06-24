# 18 Game Difficulty

Status: active draft
Project: TArenaUnity3D
Last updated: 2026-06-24

This file records TArena run difficulty and progression context. It is not a
final balance sheet. Use it to avoid confusing authored legacy examples with
the current generator-first run direction.

## Current Generator-First Rule

Current run progression design should tune generator inputs, not exact starting
army stack lists.

Current sources:

- `ArmyGeneratorRuleSet` controls generated army budget, stack count, tier caps,
  faction mix, starting gold, reroll tokens, and battle skip tokens.
- `RunGenerationSession` selects the starting-army rule set and enemy encounter
  rule catalog in Unity.
- `DeterministicRunGenerationCatalog` generates starting army offers and route
  topology from rule sets, unit pool, seed, and unlock context.
- `EnemyEncounterRuleCatalog` maps explicit node difficulty to enemy generator
  rule sets.
- `RewardMapMaterializedGenerator` scales materialized rewards from current
  army state after battle.

Do not treat `DefaultStartRunCatalog`, `DefaultRunMapPathCatalog`, or
`DefaultRunBattleEncounterCatalog` as current design truth for run progression.
They may remain in code for compatibility/history, but playtest design should
start from generator rule sets and materialized DB state.

## Current Known Rule-Set Anchors

Use these as code/context anchors, not as final balance:

- Starting-army PRD035 target: four stacks, generated offers, approximate
  `1600-1700` target value, hard validation band `1450-1750`, one unlocked
  legal skill per stack, one starting reroll, `150` starting run gold, zero
  battle skip tokens.
- Enemy generation PRD040 direction: generate enemy army snapshots for battle
  and final nodes at run start using the same deterministic army-generation path
  as starting armies, with enemy rule sets selected by node difficulty.
- Reward V1 direction: rewards are materialized from planned operation slots;
  current normal operation families are add stack, add units, promote stack,
  and downgrade stack.

## Playtest Design Questions

When designing a run balance pass, answer these before writing exact armies:

- What starting-army value band should the generator target?
- How many stack offers should appear and how many stacks should each army have?
- Which tier compositions are legal at run start?
- Which factions and unit unlocks are available for this playtest?
- How much starting run gold creates one meaningful shop decision without
  solving the run?
- What value ratio should Low, Medium, High, and Boss enemy rule sets target
  relative to expected player army value at that point?
- How much growth should each reward-producing node add after expected losses?

## What Must Not Be Copied Here

- Do not import design decisions, current state, tasks, milestones, enemy lists,
  skills, map designs, or gameplay truth from another project unless the user
  explicitly asks for a comparison or migration note.
- Do not reference another project's local files as default context.
