# TArenaUnity3D Run Metagame Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when the current prompt, selected task, or brief explicitly touches
run routes, reward cards, shops, army saving, account progress, offence/defence,
ranking, run AI goals, Offline Mode database state, or data movement between
run screens.

This map is not global startup context. PRD019/PRD030 maps are task-scoped and
should be loaded only when the current task explicitly requires that PRD scope.

## Task-Scoped PRD Maps

For explicit PRD019/PRD030 run-metagame, Offline Mode screen, persistence,
database state, or screen data-flow tasks, read:

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

These maps are relevant to Start Run, Run Map, Run Battle, Reward Map, Run
Shop, Summary Value, Saved Armies, Battle Result, shared army snapshots,
Offline Mode database adapters, and SQLite table ownership.

Current PRD030 run-context rule:

- `OfflineRunContextDbReader` is the shared run-context read side used by
  frontend screen controllers/adapters for persisted run state.
- `OfflineRunContextDbWriter` is the shared write side for creating and updating
  `offline_runs`.
- Start Run, Run Map, Run Battle, Reward Map, Run Shop, and Summary Value should
  not own direct `INSERT/UPDATE offline_runs` SQL outside the writer.
- When a run-metagame UI needs current army, currency, node, summary, latest
  battle result, or Start Run-created record state, route through reader/service
  APIs instead of serialized placeholder ids or ad hoc DB queries.

## Design Sources

Use the smallest relevant subset:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/Reward_Design.md`
- `_codex/Context/08_Current_Mechanics.md` when checking what exists now
- `_codex/Context/18_Game_Difficulty.md` when tuning risk/reward or encounter
  difficulty

## Confirmed Design Boundaries

- Run map is node-route style, not a spatial adventure map.
- Current Start Run army direction is generator-first. Do not design or report
  exact starting stack lists as current truth unless the user explicitly asks
  for authored/predefined armies. For current playtest design, tune
  `ArmyGeneratorRuleSet`, `RunGenerationSession`, unit unlock pool, stack count,
  tier caps, faction mix, starting gold, reroll tokens, and target value bands.
- Current enemy encounter direction is generator-first. Battle/final nodes use
  explicit encounter difficulty and `EnemyEncounterRuleCatalog` to select an
  enemy `ArmyGeneratorRuleSet`; predefined/manual enemy armies are future or
  special-case scope, not the default V1 balance source.
- Legacy authored catalogs such as `DefaultStartRunCatalog`,
  `DefaultRunMapPathCatalog`, and `DefaultRunBattleEncounterCatalog` are not
  the gameplay-design source for current run progression. Treat them as legacy
  compatibility/test history unless a task explicitly targets them.
- Standard battle reward is a fast 1-of-3 card choice.
- Current V1 Reward Map is materialized and DB-backed: normal battle wins route
  to Reward Map, concrete reward rows are loaded from persistence, clicking a
  legal card applies immediately, and successful apply returns to Run Map.
- Closed PRD042/043/045 reward-flow contract: run generation stores three
  unresolved planned normal reward opportunities per reward-producing node;
  battle completion resolves those exact planned operation slots into concrete
  cards; Reward Map reloads persisted card identity/state and must not reroll
  operation types at screen time.
- Closed PRD042 fallback rule: a single impossible normal slot stays disabled;
  emergency `RunGold` appears only when all three normal slots are impossible.
  The fallback is explicit persisted card state, not a replacement for a normal
  slot.
- Closed PRD043 persistence rule: reward card state is explicit
  (`reward_id`, `reward_slot_index`, legal/error, fallback, selected/applied).
  Do not use display text or `template_id` alone as domain identity.
- Closed PRD044 handoff rule: battle-to-reward stack reconciliation prefers
  stack id, then battle-input order, then legacy unit-id fallback, so duplicate
  same-unit stacks do not collapse into one ambiguous reward target.
- Current V1 reward value parity comes from PRD41: rewards scale from average
  live stack value; Mass/Width favor raw point growth, Promote/Downgrade favor
  army shape; Reward Map UI should show concrete before/after results, not the
  hidden value formula.
- Remaining reward-flow follow-up: `RecruitReward` nodes receive unresolved
  opportunity rows, but direct no-battle Reward Map resolution still needs a
  focused implementation task.
- Shop uses one main run currency and is a breathing-room node.
- Account progress unlocks units, skills, and saved army slots.
- Saved army slots start at 2 and may grow to about 3-5 through metaprogress.
- Any saved army can be used for offence; one saved army is current defence.
- Offence/defence rewards are ranking plus account experience.
- Ranking should behave like an ELO-style system to discourage farming weaker
  opponents.
- Defence is AI-controlled; active PvP is not the current model.
- Run AI may have different goals, including "try to win" and "deal maximum
  losses"; offence/defence starts with one win-focused chess-like AI.
- Event nodes, route-specific battle map rules, and selectable defence AI types
  are future/open scope.
- Online random generation is server-authoritative. Random numbers for online
  armies, routes, rewards, rerolls, and other run-critical outcomes must be
  calculated by the online authority/backend or delivered as server-owned
  results. Offline/client runtime RNG is acceptable only for local playtest
  mocks and deterministic replay tooling, not trusted online state.

Do not implement metagame systems ahead of local battle recovery unless the user
explicitly requests that work.
