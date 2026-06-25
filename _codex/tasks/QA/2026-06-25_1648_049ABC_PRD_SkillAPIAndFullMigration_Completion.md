# 049ABC PRD Skill API And Full Migration - Coding Completion

Date: 2026-06-25
Agent: Coding Agent
Task: `_codex/tasks/049ABC_PRD_SkillAPIAndFullMigration.md`

## Changed Files

- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/DataMapper.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillDefinitionMigrationDefaults.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillContext.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillUse.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillTarget.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillRulesTests.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`

## Implementation Summary

- Extended `SkillDefinitionAsset` in place with serialized `ActivationRuleData`, `TargetingRuleData`, `ResolutionRuleData`, and ordered `SkillEffect[]`.
- Added the shared PRD49 API surface: `SkillContext`, `SkillUse`, `SkillCast`, `SkillTarget`, `SkillResult`, `SkillRules`, `SkillQuery`, and `ISkillRuntime`.
- Added `SkillDefinitionMigrationDefaults` so missing/old asset data still resolves to current active skill behavior while all 33 skill assets are migrated.
- Migrated all current `Resources/0_Data/Skills/*.asset` skill assets with activation, targeting, resolution, and effect data.
- Added deterministic action seed fields to `BattleSnapshot`: `GameSeed`, `BattleId`, and `NextActionIndex`; the snapshot hash now includes them.
- Updated Tactical AI skill metadata to read activation rules from `SkillDefinitionAsset`.
- Updated skill button cooldown-fill max to read `SkillDefinitionAsset.ActivationRule.cooldownTurns` before parsing legacy description text.
- Updated `MouseControler` to use `SkillRules.CanUse` for skill start eligibility and `SkillRules.GetTargets` / `Validate` at the click boundary before legacy live bodies run.
- Updated `CastManager` permission helpers to read SO activation rules first.
- Changed `Long_Lick` live behavior from first-empty auto destination to the PRD49ABC two-step target flow: enemy target, then selected adjacent empty destination.
- Added focused EditMode tests for trap legality, duplicate `Double_Throw`, `Force_Pull` occupied destination rejection, `Stone_Throw` enemy-unit target contract, passive exclusion, and stance repeatability.

## Migrated Skill Assets

`Axe_Rain`, `Blind_by_light`, `Chope`, `Cold_Blood`, `Defence_Ritual`, `Double_Throw`, `Fire_Ball`, `Fire_Movement`, `Fire_Skin`, `Force_Pull`, `Hate`, `Heavy_Fists`, `Insult`, `Long_Lick`, `Massochism`, `Melee_Stance_Barb`, `Melee_Stance_Lizard`, `Rage`, `Range_Stance_Barb`, `Range_Stance_Lizard`, `Rope_Trap`, `Rotting`, `Rush`, `Shapeshift`, `Slash`, `Spike_Trap`, `Stone_Skin`, `Stone_Stance`, `Stone_Throw`, `Terrifying_Presence`, `Tough_Skin`, `Toxic_Fume`, `Unstoppable_Light`.

## Unit Skill Assignment Audit

- `Axeman`: `Slash`, `Hate`, `Cold_Blood`
- `FireElemental`: `Fire_Movement`, `Fire_Ball`, `Fire_Skin`
- `FleshGolem`: `Heavy_Fists`, `Terrifying_Presence`, `Rotting`
- `Healer`: `Tough_Skin`, `Defence_Ritual`
- `HeavyHitter`: `Insult`, `Rage`, `Massochism`
- `Rusher`: `Chope`, `Rush`
- `Specialist`: `Force_Pull`, `Stone_Stance`
- `StoneGolem`: `Stone_Throw`, `Stone_Skin`
- `Tank`: `Toxic_Fume`, `Shapeshift`, `Long_Lick`
- `Thrower`: `Range_Stance_Barb`, `Double_Throw`, `Axe_Rain`
- `Trapper`: `Range_Stance_Lizard`, `Spike_Trap`, `Rope_Trap`
- `Wisp`: `Blind_by_light`, `Unstoppable_Light`

## Known Limits

- The live low-level effect mutation path still calls existing `CastManager` method bodies after `SkillRules` validation. The new API is now the start/target/query source, but full generic live mutation for every skill remains a follow-up risk.
- Passive hooks in `TosterHexUnit`, `SpellOverTime`, and `HexClass` are represented as skill metadata/effect outlines, but their trigger mutation was not fully extracted out of legacy hooks in this pass.
- No Unity compilation or Unity Test Runner execution was performed by command line, per project rules.

## Manual Verification Needed

- Run `SkillRulesTests` in Unity Test Runner EditMode.
- Run Play Mode checks for every skill listed in the PRD manual validation table, with special focus on `Spike_Trap`, `Rope_Trap`, `Double_Throw`, `Force_Pull`, `Long_Lick`, `Stone_Throw`, and `Heavy_Fists`.
- Confirm migrated `.asset` fields import cleanly in Unity and that skill buttons/right-click info still display the same ids/icons/text.
