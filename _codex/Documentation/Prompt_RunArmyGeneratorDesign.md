# [TARENA] Prompt: Run Army Generator Design

Use this prompt when designing TArena run starting armies, enemy armies, and
progression through generator parameters instead of fixed authored stack lists.

```text
[TARENA]
This is TArenaUnity3D. We are designing run army progression for the current
generator-first Offline Mode.

Do not propose exact fixed starting armies as current implementation truth.
Predefined/manual armies may be future scope, but this pass must tune generator
parameters.

Load and respect:
- AGENTS.md
- _codex/Context/CONTEXT-MAP.md
- _codex/Context/01_Game_Design_Document.md
- _codex/Context/19_Identity.md
- _codex/Context/GameplayFeelDoctrine.md
- _codex/Context/Reward_Design.md
- _codex/Context/18_Game_Difficulty.md
- _codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md
- _codex/Documentation/PRD030_OfflineDatabase_Map.md
- _codex/tasks/035_PRD_RandomStartingArmiesRoutes.md
- _codex/tasks/039_PRD_EnemyEncounterRuleCatalog.md
- _codex/tasks/040_PRD_FullEncounterMaterializationBattleLaunchLoop.md

Goal:
Design a playtestable Run Progression V0 using generator constraints.

Answer in these sections:

1. Starting Army Generator
- offerCount
- stackCount
- targetTotalValue
- min/max value band
- tier composition rules
- faction mix rules
- allowed unit/unlock pool for this playtest
- starting run gold
- reroll tokens
- battle skip tokens

2. Enemy Generator Bands
- Low node target relative to expected player army value
- Medium node target relative to expected player army value
- High node target relative to expected player army value
- Boss/final target relative to expected pre-final army value
- intended AI goal per band: TryToWin or DealMaximumLosses

3. Reward Growth Targets
- expected value gain after early battle
- expected value gain after mid battle
- expected value gain before final
- how AddStack/AddUnits/Promote/Downgrade should feel different
- what should remain deferred until recovery/skill rewards are production-ready

4. Map Progression
- expected army value before each node
- expected losses per band
- what the shop should repair or enable
- what makes the final battle a proof, not a random wall

5. Questions For Me
Ask only the smallest set of missing design questions needed to choose numbers.
Do not ask for fixed stack lists unless I explicitly switch to predefined armies.
```

## Notes

Use exact stack examples only as explanatory examples of possible generator
outputs, clearly labelled as examples. The current design source is generator
tuning, not authored army lists.
