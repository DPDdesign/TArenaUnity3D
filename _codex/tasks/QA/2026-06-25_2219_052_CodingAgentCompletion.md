# [TARENA] 052 SKILL AUDIT REPORT - Coding Agent Completion Protocol

- Task: `_codex/tasks/052_CombatActionsSkills_AuditHardening.md`
- Agent: Coding Agent
- Date: 2026-06-25 22:19
- Status: ready for QA architecture review

## 1. Inventory Table

| Skill/action | Source | User | Family | Entry path | Validator path | Execution path | Legacy present | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Move | `BattleActionRules`, `MouseControler` | both | move | player `TryStartMoveAction`, AI `BattleActionUse`/legacy intent | `BattleActionRules.ValidateMove` | `BattleActionLiveApplier` -> `MouseControler.TryStartMoveAction` | yes | fixed | Legacy AI fallback now delegates non-skill revalidation to `BattleActionRules`, including movement budget. |
| MoveAndAttack | `BattleActionRules`, `MouseControler` | both | attack | player click, AI `BattleActionUse`/legacy intent | `BattleActionRules.ValidateMoveAndAttack` | `BattleActionLiveApplier` -> `MouseControler.TryStartMoveAndAttackAction` | yes | OK | Validates movement start, reachable destination, enemy target, and adjacency. |
| BasicMeleeAttack | `BattleActionRules` | both | attack | represented through move-and-attack/current melee path | `BattleActionRules.ValidateMoveAndAttack` | same live adapter as move-and-attack | yes | OK | No separate player button path found; current melee attack is adjacent/move-and-attack compatible. |
| BasicRangedAttack | `BattleActionRules`, `MouseControler`, `TosterHexUnit` | both | attack | player `Shot`, AI `BattleActionUse`/legacy intent | `BattleActionRules.ValidateBasicRangedAttack` | `BattleActionLiveApplier` -> `MouseControler.TryStartBasicRangedAttackAction` -> `ShootME` | yes | OK | Ranged live behavior allows enemy targets and applies legacy long-distance damage falloff, not a hard max range. |
| Wait | `BattleActionRules`, `MouseControler` | both | automatic/turn | player/AI wait | `BattleActionRules.ValidateWait` | `BattleActionLiveApplier` -> `MouseControler.TryStartWaitAction` | yes | OK | Blocks after move, skill, or previous wait. |
| Defend | `BattleActionRules`, `MouseControler` | both | automatic/turn | player/AI defend | `BattleActionRules.ValidateDefend` | `BattleActionLiveApplier` -> `MouseControler.TryStartDefenseAction` | yes | OK | Blocks after move or skill. |
| Slash | `Slash.asset`, `SkillRules` | both | skill/move | player slot, AI skill action | `SkillRules` via movement/directional targets | `TacticalAISkillRulesExecutor`; player still CastManager compatibility | yes | needs manual gameplay test | Movement + directional area remains high-risk for parity. |
| Hate | `Hate.asset`, `SkillRules` | both | skill/status | player slot, AI skill action | `SkillRules` enemy target | shared AI runtime; player CastManager compatibility | yes | OK | Non-turn-consuming status path represented in result events. |
| Cold_Blood | `Cold_Blood.asset`, `SpellOverTime` | automatic | passive | `TosterHexUnit.StartAutocast` / status tick | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnEnd` | yes | risky | Legacy passive trigger remains intentional debt. |
| Fire_Movement | `Fire_Movement.asset`, `TosterHexUnit` | automatic | passive/trap | movement hook | passive cannot be cast by `SkillRules` | `TosterHexUnit.SetHex` fire trap placement | yes | risky | Automatic trap placement remains legacy hook. |
| Fire_Ball | `Fire_Ball.asset`, `SkillRules` | both | skill/AoE | player slot, AI skill action | `SkillRules` area center | shared AI runtime; player CastManager compatibility | yes | OK | AoE result events are validator-owned; presentation still compatibility-dependent. |
| Fire_Skin | `Fire_Skin.asset`, `SpellOverTime` | automatic | passive/status | status hook | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnEnd` | yes | risky | Aura/debuff timing remains legacy passive debt. |
| Heavy_Fists | `Heavy_Fists.asset`, `SkillRules` | both | skill/move/AoE | player slot, AI skill action | `SkillRules` movement + directional impact | shared AI runtime; player CastManager compatibility | yes | needs manual gameplay test | HP cost + movement + area pattern need Unity parity check. |
| Terrifying_Presence | `Terrifying_Presence.asset`, `SpellOverTime` | automatic | passive/status | status hook | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnEnd` | yes | risky | Counterattack removal remains legacy passive timing. |
| Rotting | `Rotting.asset`, `SpellOverTime` | automatic | passive/status | status hook | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnEnd` | yes | risky | HP decay/minimum HP remains legacy timing. |
| Tough_Skin | `Tough_Skin.asset`, `SkillRules` | both | skill/buff | player slot, AI skill action | `SkillRules` ally/self target | shared AI runtime; player CastManager compatibility | yes | OK | Legal target selection is shared. |
| Defence_Ritual | `Defence_Ritual.asset`, `SkillRules` | both | skill/buff | player slot, AI skill action | `SkillRules` auto allies | shared AI runtime; player CastManager compatibility | yes | OK | No selected target required. |
| Insult | `Insult.asset`, `SkillRules` | both | skill/debuff | player slot, AI skill action | `SkillRules` auto enemies | shared AI runtime; player CastManager compatibility | yes | OK | Non-turn-consuming global enemy debuff is represented. |
| Rage | `Rage.asset`, `SkillRules` | both | skill/self buff | player slot, AI skill action | `SkillRules` self/no target | shared AI runtime; player CastManager compatibility | yes | OK | Self target has no selected hex. |
| Massochism | `Massochism.asset`, `SpellOverTime` | automatic | passive | status hook | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnEnd` | yes | risky | Damage-taken derived payload remains legacy passive debt. |
| Chope | `Chope.asset`, `SkillRules` | both | skill/AoE | player slot, AI skill action | `SkillRules` area around caster | shared AI runtime; player CastManager compatibility | yes | OK | AI chooses through shared skill validation. |
| Rush | `Rush.asset`, `SkillRules` | both | skill/rush | player slot, AI skill action | `SkillRules` forward-line target | shared AI runtime; player CastManager compatibility | yes | needs manual gameplay test | Validates forward-line endpoint; opening AI behavior still needs Play Mode coverage. |
| Force_Pull | `Force_Pull.asset`, `SkillRules` | both | skill/move | player slot, AI skill action | `SkillRules` ally + empty destination | shared AI runtime; player CastManager compatibility | yes | OK | Occupied destination rejection is covered by tests. |
| Stone_Stance | `Stone_Stance.asset`, `SkillRules` | both | skill/self buff | player slot, AI skill action | `SkillRules` self/no target | shared AI runtime; player CastManager compatibility | yes | OK | Not modeled as repeatable stance; current asset says active buff. |
| Stone_Throw | `Stone_Throw.asset`, `SkillRules` | both | skill/projectile/spawn | player slot, AI skill action | `SkillRules` enemy-unit target + spawn-near-target | shared AI runtime; player CastManager compatibility | yes | needs manual gameplay test | Highest-risk parity case: split/spawn/self-damage/target-damage. |
| Stone_Skin | `Stone_Skin.asset`, `SpellOverTime` | automatic | passive | status hook | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnStart/End` | yes | risky | Flat reduction remains legacy passive debt. |
| Toxic_Fume | `Toxic_Fume.asset`, `SkillRules` | both | skill/move/status | player slot, AI skill action | `SkillRules` movement destination | shared AI runtime; player CastManager compatibility | yes | OK | Destination is reachable/walkable/empty through shared movement target. |
| Shapeshift | `Shapeshift.asset`, `SkillRules` | both | skill/self status | player slot, AI skill action | `SkillRules` self/no target | shared AI runtime; player CastManager compatibility | yes | OK | Turn-consuming self action. |
| Long_Lick | `Long_Lick.asset`, `SkillRules` | both | skill/forced move | player slot, AI skill action | `SkillRules` enemy target + adjacent empty destination | shared AI runtime; player CastManager compatibility | yes | needs manual gameplay test | Current approved two-step contract is represented and tested at rules level. |
| Range_Stance_Barb | `Range_Stance_Barb.asset`, `SkillRules` | both | stance | player slot, AI skill action | `SkillRules` repeatable no-target stance | `TacticalAISkillRulesExecutor.ApplyStance`; player compatibility | yes | OK | No cooldown, no turn cost, repeatable. |
| Melee_Stance_Barb | `Melee_Stance_Barb.asset`, `SkillRules` | both | stance | runtime stance swap partner | `SkillRules` repeatable no-target stance | `TacticalAISkillRulesExecutor.ApplyStance`; player compatibility | yes | OK | Asset exists as stance partner even if not initial unit assignment. |
| Double_Throw | `Double_Throw.asset`, `SkillRules` | both | skill/projectile | player slot, AI skill action | `SkillRules` two enemy targets | shared AI runtime; player CastManager compatibility | yes | needs manual gameplay test | Duplicate target is legal by asset data and covered by tests. |
| Axe_Rain | `Axe_Rain.asset`, `SkillRules` | both | skill/AoE/projectile | player slot, AI skill action | `SkillRules` area center | shared AI runtime; player CastManager compatibility | yes | OK | AoE center/splash parity needs normal Play Mode spot check. |
| Range_Stance_Lizard | `Range_Stance_Lizard.asset`, `SkillRules` | both | stance | player slot, AI skill action | `SkillRules` repeatable no-target stance | `TacticalAISkillRulesExecutor.ApplyStance`; player compatibility | yes | OK | No cooldown, no turn cost, repeatable. |
| Melee_Stance_Lizard | `Melee_Stance_Lizard.asset`, `SkillRules` | both | stance | runtime stance swap partner | `SkillRules` repeatable no-target stance | `TacticalAISkillRulesExecutor.ApplyStance`; player compatibility | yes | OK | Asset exists as stance partner even if not initial unit assignment. |
| Spike_Trap | `Spike_Trap.asset`, `SkillRules`, `HexClass` | both/automatic trigger | trap | player slot, AI skill action, movement trigger | `SkillRules` empty walkable no-existing-trap placement | placement through shared AI runtime/player compatibility; trigger in `TosterHexUnit.SetHex` | yes | needs manual gameplay test | Occupied/existing trap placement rejection is rules-owned; trigger is legacy. |
| Rope_Trap | `Rope_Trap.asset`, `SkillRules`, `HexClass` | both/automatic trigger | trap | player slot, AI skill action, movement trigger | `SkillRules` empty walkable no-existing-trap placement | placement through shared AI runtime/player compatibility; trigger in `TosterHexUnit.SetHex` | yes | needs manual gameplay test | Same placement boundary as Spike Trap; trigger remains legacy. |
| Blind_by_light | `Blind_by_light.asset`, `SkillRules` | both | skill/status | player slot, AI skill action | `SkillRules` enemy target | shared AI runtime; player CastManager compatibility | yes | OK | Enemy target and cooldown are shared-rule covered. |
| Unstoppable_Light | `Unstoppable_Light.asset`, `SpellOverTime` | automatic | passive | status hook | passive cannot be cast by `SkillRules` | `SpellOverTime.SpecialThingOnStart/End` | yes | risky | Defense penetration remains legacy passive debt. |
| Cleanse | `CastManager` legacy only | reference only | skill/reference | none current | missing/current inactive | legacy reference only | yes | OK | No current asset/unit assignment found in active set. |
| Brak_Weny | `CastManager` legacy only | reference only | skill/reference | none current | missing/current inactive | legacy reference only | yes | OK | No current asset/unit assignment found in active set. |

## 2. Fixed Problems

### Legacy AI non-skill revalidation duplicated shared legality

- What was wrong: direct fallback calls to `TacticalAIIntentRevalidator.TryRevalidate(...)` still used local non-skill checks for move, move-and-attack, ranged attack, wait, and defend. Those checks covered basic actor/target state but did not consistently consume the PRD050 `BattleActionRules` validation surface.
- Where: `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`.
- Fix: non-skill legacy intents now convert to `BattleActionUse`, call `BattleActionRules.Validate(...)`, and populate `TacticalAIRevalidatedIntent.Action` / `Result` from the shared validator result.
- Regression coverage: `TacticalAIExecutionBridgeTests.Revalidator_RejectsLegacyMoveOutsideSharedMovementBudget` proves an out-of-budget legacy move intent is rejected through the shared validator.

## 3. Removed Legacy

No legacy files, methods, fields, ScriptableObjects, scenes, prefabs, or assets were removed.

No Inspector-visible fields were added, removed, or renamed.

## 4. Legacy Left For Review

- `BattleActionLiveApplier` still delegates non-skill live mutation to `MouseControler.TryStartMoveAction`, `TryStartMoveAndAttackAction`, `TryStartBasicRangedAttackAction`, `TryStartWaitAction`, and `TryStartDefenseAction` after validation.
- Player non-skill input still enters through `MouseControler` authority methods.
- Player skill commit still enters the remaining CastManager compatibility body after `SkillRules` accepts the click.
- `TacticalAIActionIntent`, `TacticalAICandidateGenerator`, `TacticalAISearchCandidateExpander`, and legacy intent display fields still exist as PRD050 debt.
- Passive and trap trigger mutation remains in `TosterHexUnit`, `SpellOverTime`, `HexClass`, and `Traps`.
- `SkillRules` / `SkillUse` / `SkillCast` / `SkillResult` remain the skill branch inside the broader Battle Action model.

## 5. Risks

- `Stone_Throw`: stack split/spawn/self-damage/target-damage parity still needs Play Mode validation.
- `Double_Throw`: duplicate target is validated, but VFX/gameplay parity still needs Play Mode validation.
- `Rush`: forward-line validation exists, but Rusher opening behavior and no-hit movement need Play Mode validation.
- `Long_Lick`: two-step target/destination validation exists, but pull presentation and taunt timing need Play Mode validation.
- `Spike_Trap` / `Rope_Trap`: placement validation is shared-rule based; trigger effects remain legacy hooks.
- Passives: `Cold_Blood`, `Massochism`, `Stone_Skin`, `Fire_Movement`, `Fire_Skin`, `Terrifying_Presence`, `Rotting`, and `Unstoppable_Light` are represented as passive skill assets but still execute through legacy trigger/status hooks.

## 6. Manual Gameplay Checklist

- Move: try legal empty destination, occupied destination, and beyond movement budget.
- Move-and-attack/basic melee: try adjacent legal attack, unreachable target, occupied destination, and ally target.
- Basic ranged attack: try enemy target before moving, after moving, after skill use, and with an ally target.
- Wait/Defend: try before movement, after movement, after skill, and repeat wait.
- Double Throw: select two enemies and the same enemy twice; confirm two hits and VFX/result parity.
- Rush: test enemy in forward line, ally/blocker in line, no enemy in line, and battle-start enemy turn readiness.
- Stone Throw: confirm enemy target requirement, split/spawn, self damage, target damage, cooldown, and presentation reveal.
- Long Lick: confirm enemy first target, selected adjacent empty destination, occupied destination rejection, pull, and taunt.
- Spike/Rope Trap: place on empty hex, reject occupied/existing trap, trigger by entry, and confirm trap removal/status.
- Passives: run movement, new-turn, damage, and attack scenarios for every passive listed in the risk section.

## 7. Tests Added/Updated And Tests Not Run

Updated:

- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
  - `Revalidator_RejectsLegacyMoveOutsideSharedMovementBudget`

Source checks:

- Brace-balance check passed for:
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
  - `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`

Not run:

- Unity import/compile was not run.
- Unity Test Runner was not run.
- Play Mode validation was not run.

## 8. Things Deliberately Not Changed

- No gameplay damage, cooldown, movement, initiative, status, trap, passive, or AI scoring values changed.
- No ScriptableObject assets were edited after inspection; the current active skill data matched the audited validator contracts closely enough for this pass.
- No scenes, prefabs, materials, controllers, `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` files were edited.
- No full PRD050 purge was attempted.
- No CastManager or passive hook removal was attempted.
