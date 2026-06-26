# 049ABC PRD Skill API And Full Migration

- Status: closed - accepted 2026-06-25 with runtime execution follow-up deferred to PRD49ED/PRD49E
- Type: execution PRD
- Area: battle skills, `SkillDefinitionAsset`, targeting, validation, execution, UI, AI/API preparation
- Owner: Coding Agent
- Sources merged: `_codex/tasks/archive/049A_PRD_TacticalActionValidationSO_UI_VerticalSlice.md` and `_codex/tasks/archive/049AC_PRD_SkillTargetAndEffectDataModel.md`
- Source note: no separate local `049BC` file was found. Current local B/C material is `049AC`, which supersedes archived `049B` and `049C`.
- Audience: Coding Agent implementing Unity/C# changes, not project-owner briefing.

## 1. Technical Goal

Replace the current legacy 2019 skill execution architecture with a small 2026 skill API while preserving current gameplay behavior.

The implementation goal is not to rebalance or redesign skills. Every current skill used by the demo must keep the same cooldowns, ranges, target legality, damage formulas, status values, turn cost, movement behavior, UI identity, and presentation feel.

The new API must make existing skills available through one shared rule path for:

- player input,
- future Tactical AI decision making in PRD49D,
- future server-side validation / anti-cheat.

This PRD does not implement full AI scoring and does not implement a backend validator. It creates the API, data model, runtime bridge, and full current-skill migration needed before PRD49D.

## 2. Implementation Scope

Implement the full migration in code and approved skill definition data:

- extend the existing `SkillDefinitionAsset`;
- keep `SkillDefinitionAsset.skillName` as the canonical skill id;
- migrate current skill text, activation rules, target rules, resolution rules, and effect data into the existing skill SO model;
- add a small shared skill rules/query API;
- route player skill target highlights and commits through the new API;
- route all active demo skills through the new API;
- migrate passive and stance behavior enough that future AI/server code can reason about them;
- keep `SkillPresentationCatalog` as presentation data joined by skill id;
- keep unit skill ownership in `UnitCatalog` / unit definition assets;
- leave run/offline database storage out of skill authoring truth.

Current active asset-backed skills under `TArenaUnity3D/Assets/Resources/0_Data/Skills/` are in scope. Legacy `CastManager` methods without current skill assets or unit assignment are reference-audit only unless implementation finds a live demo reference.

## 3. Non-Goals

- Do not change gameplay balance.
- Do not add new skills.
- Do not remove existing demo skills.
- Do not change unit skill ownership or skill slot order.
- Do not rename skill ids.
- Do not create `SkillDefinition2`, `NewSkillAsset`, `AbilityDefinition`, `CombatAbilityAsset`, `SkillDataV2`, or any parallel skill-definition asset.
- Do not implement PRD49D AI scoring, AI personality, difficulty behavior, or tactical planning weights.
- Do not implement a multiplayer backend or server.
- Do not move VFX/SFX into `SkillDefinitionAsset`.
- Do not move authored skill truth into SQLite.
- Do not keep `skills.xml` as a runtime fallback after migrated SO fields cover the same data.
- Do not add broad framework layers, service locators, event buses, command pipelines, or one-off per-skill validators.

## 4. Source Truth In Code

Files / systems to inspect before implementation:

- `_codex/tasks/archive/049A_PRD_TacticalActionValidationSO_UI_VerticalSlice.md`
- `_codex/tasks/archive/049AC_PRD_SkillTargetAndEffectDataModel.md`
- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Documentation/ADR_013_TacticalAI_CastManagerSkillBridge.md`
- `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`
- `_codex/Documentation/ADR_016_PRD049B_MovementSkillTwoStepUXDebt.md`
- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/SkillCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/DataMapper.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/RightClickInfoSkill.cs`
- `TArenaUnity3D/Assets/SkillInfoPresentation.cs`
- `TArenaUnity3D/Assets/UnitRepresentation.cs`
- `TArenaUnity3D/Assets/StackRepresentation.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/SkillCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Units/*.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/skills.xml`

Legacy behavior is the source of truth when PRD text is incomplete. If current `CastManager` behavior and old XML text disagree, the implementation must preserve the working runtime behavior and document the mismatch in the completion note.

## 5. Current Legacy Problem

Current skill behavior is split across string-coupled surfaces:

- unit assets store skill ids in `skillNames`;
- `TosterHexUnit.InitateType(...)` copies them into `TosterHexUnit.skillstrings`;
- `UICanvas` reads `SelectedToster.skillstrings` and `SelectedToster.cooldowns`;
- right-click info reads `DataMapper.Instance.FindSkill(skillId)`;
- `DataMapper` reads `SkillDefinitionAsset` only as text/type/flags;
- `CastManager.getMode(spellID, ST)` invokes `{SkillName}M` by reflection;
- `CastManager.startSpell(spellID, hex)` invokes `{SkillName}` by reflection;
- `MouseControler` owns target state, highlighting, selection, cooldown application, and commit;
- passive and trap behavior is mixed into `TosterHexUnit`, `SpellOverTime`, and `HexClass`;
- Tactical AI currently receives only shallow skill metadata and may still bridge through `CastManager`.

This creates hidden rules that AI and future server validation cannot query without live Unity objects and reflection side effects.

## 6. Target API

Use short, concrete names. The implementation may adjust exact signatures to fit the existing codebase, but responsibilities must stay stable.

Required API surface:

- `SkillRules`
  - `CanUse(SkillContext context)`
  - `GetTargets(SkillContext context, IReadOnlyList<HexCoord> selectedTargets)`
  - `Validate(SkillUse use, SkillContext context)`
  - `Preview(SkillCast cast, SkillContext context)`
  - `Apply(SkillCast cast, SkillContext context, ISkillRuntime runtime)` or equivalent runtime adapter
  - owns the single legality path for player, future AI, and future server validation.
- `SkillContext`
  - contains `BattleSnapshot`, actor id, skill definition, turn/action state, and optional deterministic action seed data;
  - does not read live Unity scene objects as fallback.
- `SkillUse`
  - untrusted request from player, UI, future AI, or future server command;
  - contains actor id, skill id, and ordered selected hexes;
  - does not contain trusted damage, affected units, movement destination, or result data.
- `SkillCast`
  - validated normalized action;
  - contains actor id, skill id, selected hexes, destination hex, impact hex, target units, affected units, affected hexes, cost/cooldown data, and resolved effect targets.
- `SkillResult`
  - ordered result events emitted by execution;
  - examples: `UnitMoved`, `DamageApplied`, `StatusApplied`, `TrapPlaced`, `TrapTriggered`, `UnitSpawned`, `StackAmountChanged`, `CooldownApplied`, `TurnCostApplied`.
- `SkillTarget`
  - simple data describing legal target hexes/roles for UI and future AI.
- `SkillEffect`
  - data-backed effect execution entry; effects execute in authoring order.
- `SkillQuery`
  - thin helper for future AI/server code:
    - get usable skill ids for actor,
    - get legal targets for a skill,
    - preview a candidate cast,
    - validate a submitted cast.

Validation and preview must be pure where feasible. Live mutation belongs only to the apply/runtime adapter.

One skill action has one source of truth:

- `CanUse` checks whether the actor may start the skill.
- `GetTargets` returns legal next target choices for the current partial selection.
- `Validate` resolves a complete `SkillUse` into one trusted `SkillCast`.
- `Preview` reads that `SkillCast`; it must not resolve separate targets.
- `Apply` consumes that same `SkillCast`; it must not choose targets again.

Cooldowns, turn cost, after-move legality, and movement-after-skill legality must be calculated from `BattleSnapshot` plus `SkillDefinitionAsset` rule data. UI state must not be the authority for those values.

## 7. `SkillDefinitionAsset` Changes

Extend `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`. Do not replace it.

Current fields must remain compatible:

- `skillName`
- `type`
- `info`
- `flags`
- current getters and `ToSkillDefinition()`

Add grouped serialized data:

- `ActivationRuleData`
  - activation kind: `Active`, `Passive`, `Stance`
  - cooldown turns
  - consumes turn
  - can use after move
  - can move after use
  - repeatable in turn
  - blocks wait/defend as legacy behavior requires
- `TargetingRuleData`
  - target family: `Self`, `UnitTarget`, `HexTarget`, `Movement`
  - ordered target roles
  - target count
  - team filter
  - occupancy requirements
  - empty destination requirements
  - walkable requirements
  - range/radius/line constraints
  - duplicate-target permission
- `ResolutionRuleData`
  - direct unit
  - empty hex placement
  - area around selected hex
  - area around caster
  - line/rush scan
  - move then area
  - pull/teleport target to destination
  - spawn placement choice
- `SkillEffect[] effects`
  - ordered effect list.

Supporting `StatusDefinition` and `TrapDefinition` assets are allowed only as narrow data helpers referenced by `SkillDefinitionAsset`. They must not become a second skill-definition system.

`skills.xml` may be used as migration input only. After migration, runtime UI and rules must read from `SkillDefinitionAsset` / `SkillCatalog`.

## 8. New Or Changed Classes

Likely files to modify:

- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/SkillCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/DataMapper.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/RightClickInfoSkill.cs`
- `TArenaUnity3D/Assets/SkillInfoPresentation.cs`
- `TArenaUnity3D/Assets/UnitRepresentation.cs`
- `TArenaUnity3D/Assets/StackRepresentation.cs`

Likely new files under `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/`:

- `SkillRules.cs`
- `SkillContext.cs`
- `SkillUse.cs`
- `SkillCast.cs`
- `SkillTarget.cs`
- `SkillResult.cs`
- `SkillEffect.cs`
- `SkillQuery.cs`
- `SkillRuntime.cs` or a similarly short runtime adapter name
- `StatusDefinition.cs` if needed by migrated statuses
- `TrapDefinition.cs` if needed by migrated traps

This list is an allowed maximum, not a required framework. Merge or omit files when they stay thin wrappers. In particular:

- `SkillQuery` may be folded into `SkillRules` if it only forwards calls.
- `SkillEffect` may be a serializable data type inside or next to `SkillDefinitionAsset`; it does not need a separate runtime framework.
- `SkillRuntime` should remain a small live-apply adapter, not a service layer.

Likely tests under `TArenaUnity3D/Assets/Scripts/Tests/EditMode/`:

- `SkillRulesTests.cs`
- `SkillTargetTests.cs`
- `SkillResultTests.cs`
- focused per-family tests where the single file becomes too large.

Do not add files with long generic names.

## 9. Naming Rules

Use short names:

- `SkillRules`
- `SkillCast`
- `SkillUse`
- `SkillTarget`
- `SkillResult`
- `SkillEffect`
- `SkillQuery`
- `SkillContext`

Avoid names like:

- `ComprehensiveSkillExecutionValidationProcessor`
- `AdvancedAbilityDecisionSupportSystem`
- `SkillDefinitionRuntimeExecutionBridgeManager`
- `UniversalCombatActionResolutionOrchestrator`

Do not duplicate responsibilities under multiple names. If `SkillRules` validates target legality, do not add another `SkillLegalityManager` that validates the same thing.

## 10. Full Skill Migration Plan

Migration order:

1. Add `SkillDefinitionAsset` schema while preserving current text/type/flags behavior.
2. Add pure DTO/API classes and tests without routing player input yet.
3. Extend `BattleSnapshot` with missing validation data only when required.
4. Implement activation and cost rules:
   - cooldown,
   - before/after move legality,
   - turn consumption,
   - repeatable stance toggles,
   - passive exclusion from active casts.
5. Implement target families:
   - `Self`,
   - `UnitTarget`,
   - `HexTarget`,
   - `Movement`.
6. Implement effect types needed by current skills only:
   - `Damage`,
   - `ApplyStatus`,
   - `PlaceTrap`,
   - `MoveUnit`,
   - `ModifyStackAmount`,
   - `SpawnUnit`,
   - `ApplyHpCostOrSelfDamage`,
   - `SetStanceMode` / `ToggleStance`.
7. Migrate representative hard skills first:
   - `Spike_Trap`,
   - `Rope_Trap`,
   - `Rush`,
   - `Double_Throw`,
   - `Heavy_Fists`,
   - `Force_Pull`.
8. Wire `MouseControler` target state to `SkillRules.GetTargets` and `SkillRules.Validate`.
9. Migrate remaining active skills by family.
10. Migrate stance commands.
11. Migrate passive triggers and status/trap hooks.
12. Remove active demo reliance on `CastManager` reflection.
13. Keep `CastManager` only as migration reference or compatibility shell until audited cleanup.
14. Update UI/right-click/info paths to read SO skill data, not XML.
15. Add per-skill automated and manual validation notes.

### Active Skill Mapping

| Skill id | Family | Target roles | Required effect data | Migration notes |
| --- | --- | --- | --- | --- |
| `Chope` | `Self` | actor self | area damage around caster | Preserve radius 1 and current enemy/other-unit filtering from legacy. |
| `Rush` | `Movement` | line/direction hex | actor move, optional damage/status | Preserve line behavior, no-hit movement, and legacy `+2` temporary stat spell. |
| `Double_Throw` | `UnitTarget` | enemy, enemy | two ordered ranged damage hits | Duplicate enemy target is legal. Preserve current 40 percent hit behavior implemented through legacy damage modifier. |
| `Axe_Rain` | `HexTarget` | area center hex | center damage, splash damage | Preserve radius and center/splash split from legacy. |
| `Slash` | `Movement` | movement destination, impact/direction | move actor, area damage | Keep movement-first target order for now. Mark UX debt only. |
| `Hate` | `UnitTarget` | enemy unit | mutual status link | Preserve `NI`, cooldown 2, duration 2, and mutual source/target status relation. |
| `Insult` | `UnitTarget` | auto all enemies | status/debuff | Preserve `NI`, cooldown 4, global enemy initiative/movement debuff behavior as current code applies it. |
| `Rage` | `Self` | actor self | self status/stat swap | Preserve `NI`, cooldown 3, duration 2, and current defense-to-attack conversion value from code. |
| `Spike_Trap` | `HexTarget` | empty walkable placement hex | place trap, trigger status/damage | New legality must reject occupied hexes and existing traps. Trigger behavior comes from `TosterHexUnit.SetHex`. |
| `Rope_Trap` | `HexTarget` | empty walkable placement hex | place trap, trigger status | New legality must reject occupied hexes and existing traps. Preserve current resistance/status payload. |
| `Tough_Skin` | `UnitTarget` | ally or self | status/buff | Preserve lizard-unit 25 resistance and other-race 15 resistance behavior. |
| `Defence_Ritual` | `UnitTarget` | auto all allies | status/buff | Preserve current team-wide defensive values and unit-type branch. |
| `Force_Pull` | `Movement` | ally unit, empty destination | teleport/move target | Preserve current allied target and radius 2 destination behavior. |
| `Stone_Stance` | `Self` | actor self | status/buff/counterattack modifier | Preserve cooldown 5, counterattack removal, and resistance behavior. |
| `Toxic_Fume` | `Movement` | movement destination | move actor, self root/status, enemy taunt | Preserve one selected movement destination and area around post-move caster. |
| `Shapeshift` | `Self` | actor self | stat swap / wait-like action | Preserve movement speed / initiative swap and current turn behavior. |
| `Long_Lick` | `Movement` | enemy unit, player-selected adjacent empty destination | teleport/move target, taunt | Approved PRD49ABC behavior replaces legacy first-empty-destination behavior. Player selects the destination. Keep enemy target, range, taunt, and effect values. |
| `Blind_by_light` | `UnitTarget` | enemy unit | blind status | Preserve cooldown 3 and duration 2. |
| `Stone_Throw` | `UnitTarget` | enemy unit target plus spawn destination | split stack, spawn unit, self/target damage | Approved PRD49ABC behavior replaces legacy empty-hex target variant. Target must be an enemy unit. Preserve split, spawn, self damage, target damage, cooldown, and presentation behavior. |
| `Fire_Ball` | `HexTarget` | area center hex | area ranged damage | Preserve radius and current damage modifier. |
| `Heavy_Fists` | `Movement` | movement destination, impact/direction | HP cost, move actor, cone/area damage | Preserve current area pattern, `20` HP floor behavior from code, and damage modifier. |

### Stance Mapping

| Skill id | Migration rule |
| --- | --- |
| `Range_Stance_Barb` | Model as repeatable stance command, no target, no cooldown, no turn cost. Preserve current slot id swap to `Melee_Stance_Barb`, range mode, damage modifier, and resistance modifier. |
| `Melee_Stance_Barb` | Model as stance state/partner command for compatibility. Preserve swap back to `Range_Stance_Barb`. |
| `Range_Stance_Lizard` | Same as Barbarian stance but with Lizard ids. |
| `Melee_Stance_Lizard` | Same as Barbarian stance but with Lizard ids. |

### Passive Mapping

| Skill id | Trigger | Migration rule |
| --- | --- | --- |
| `Cold_Blood` | always on / turn end | Preserve current passive status setup and damage-reduction / delayed damage behavior. |
| `Massochism` | damaged / retaliation timing | Preserve current passive timing and damage basis after code audit. |
| `Stone_Skin` | always on | Preserve current flat damage reduction behavior. |
| `Fire_Movement` | on move | Preserve fire trap placement on old hex and delayed reveal. |
| `Fire_Skin` | aura / turn hook | Preserve current aura/stat reduction timing after code audit. |
| `Terrifying_Presence` | turn start / proximity | Preserve counterattack-removal behavior in radius 1. |
| `Rotting` | turn tick | Preserve 15 percent max HP decay and minimum 1 HP rule. |
| `Unstoppable_Light` | on attack | Preserve armor/defense penetration behavior. |

Passive behavior must be extracted from `TosterHexUnit`, `SpellOverTime`, `HexClass`, and related hooks into the new skill/status/trap rule model. Keep the current trigger timing and values, but do not leave passive legality/effect truth hidden only in `TosterHexUnit`.

### Reference Audit Only

| Skill id | Rule |
| --- | --- |
| `Cleanse` | Legacy `CastManager` method exists, but no current skill asset/unit assignment was found. Do not migrate unless implementation finds a live demo reference. |
| `Brak_Weny` | Legacy placeholder/no-op method exists, but no current skill asset/unit assignment was found. Do not migrate as active gameplay. |

## 11. UI Compatibility

Do not change the visible skill UI contract:

- skill slots still come from `MouseControler.SelectedToster.skillstrings`;
- cooldown display still comes from `SelectedToster.cooldowns`;
- skill buttons keep the same order;
- right-click info keeps the same skill id;
- icons keep the existing `Resources/Sprites/Skill_Icons/{SkillName}` convention unless a separate approved UI task changes it;
- `SkillInfoPresentation`, `UnitRepresentation`, and `StackRepresentation` must read skill info from the SO-backed `DataMapper`/`SkillCatalog` path;
- text UI must remain TextMesh Pro where touched.

Target highlighting changes:

- `MouseControler` should ask `SkillRules.GetTargets(...)` for legal next target hexes;
- UI must not recalculate target legality independently;
- illegal target hexes must not be highlighted as selectable;
- forced illegal submits must be rejected by `SkillRules.Validate(...)`;
- multi-step skills must highlight only the legal next step based on previous selected targets.
- preserve the current highlight feel and density. Do not globally highlight every legal hex for broad skills such as traps if current UX uses a subtler hover/selection flow.

## 12. `CastManager` Compatibility

`CastManager` is migration reference, not the new authority.

Implementation rules:

- no new skill legality should be added to `CastManager`;
- no migrated active demo skill should require reflection `{SkillName}M` / `{SkillName}` for final legality or commit;
- legacy method bodies may be read to copy behavior;
- low-level helper behavior may be reused only if it is not a hidden skill-specific authority;
- after each migrated family passes tests, remove or bypass the matching reflection path so drift cannot continue;
- do not leave long-term commented legacy implementations.

`CastManager` may temporarily remain as a compatibility shell while the migration is incomplete, but PRD49ABC is not done until all current active demo skills route through the new API. Legacy skill legality and commit paths must be removed or made unreachable for migrated skills before completion.

## 13. `MouseControler` Compatibility

`MouseControler` remains the player-input surface, but it must stop owning skill rules.

Required changes:

- selected skill slot still maps to `SelectedToster.skillstrings[SelectedSpellid]`;
- initial skill click creates a `SkillUse` / partial target sequence;
- target hover and click asks `SkillRules` for legal target state;
- staged skills keep ordered target selection:
  - `Double_Throw`: enemy, enemy;
  - `Force_Pull`: ally, empty destination;
  - `Slash`: movement destination, direction/impact;
  - `Heavy_Fists`: movement destination, direction/impact;
  - `Long_Lick`: enemy, player-selected adjacent empty destination;
- commit validates the complete `SkillUse`;
- commit applies `SkillResult` through a runtime adapter;
- cooldown and turn usage use validated activation/cost rules, not `CastManager.cooldown` flags.

Do not change basic move, basic attack, wait, or defense behavior as part of this PRD except where the shared action lifecycle must receive a skill result.

`MouseControler` may keep the current interaction feel, staged selection flow, and visual style, but it must not keep a parallel legal-target calculation after migration. The source for legal targets is `SkillRules.GetTargets`; `MouseControler` only presents that result.

## 14. Preparation For PRD49D AI

PRD49ABC must expose queryable skill data and legal actions. It must not implement tactical scoring.

Required for PRD49D:

- AI can ask which skill ids are usable by the active actor;
- AI can ask legal target sequences for a skill;
- AI can request preview/result data for a legal candidate;
- AI receives stable rejection reasons for illegal candidates;
- passive skills are visible as metadata/triggers but are not active cast candidates;
- stance toggles are visible as repeatable no-target actions;
- migrated candidate data uses `BattleSnapshot`, `SkillDefinitionAsset`, and `SkillRules`, not `CastManager`.
- AI must be treated as another player. It must not receive an AI-only skill legality path, hidden target resolver, or special-case cooldown logic.

Do not modify `MostStupidAIEver` or Tactical AI scoring unless required to compile against the new API. Full AI behavior belongs to PRD49D.

## 15. Preparation For Server-Side Validation

No backend is implemented here.

The API must still be compatible with future server validation:

- `SkillUse` is an untrusted request;
- `SkillCast` is trusted normalized validator output;
- `SkillResult` is ordered and deterministic enough to compare/replay;
- validation must not depend on live Unity objects, scene selection state, or reflection;
- effect target sources must reference validated output;
- server-authoritative random/damage validation must be possible from seed/action context.

Extend `BattleSnapshot` only with fields needed for validation:

- stable actor id,
- active actor id,
- unit positions,
- team ownership,
- alive/actionable state,
- walkability,
- occupancy,
- trap state,
- cooldowns by skill id or slot with stable skill id pair,
- used skill ids this turn,
- moved/used/waited state,
- skill ownership,
- statuses,
- game seed / battle id / action index if deterministic damage is implemented in this slice.

If deterministic damage replaces direct `UnityEngine.Random`, preserve the current formulas, min/max ranges, and gameplay feel. Do not change damage values.

## 16. Regression Risks

High-risk areas:

- skill id string drift between unit assets, skill assets, icon paths, presentation catalog, and code;
- skill slot order changes;
- cooldowns shifting from legacy `CastManager.cooldown` values;
- `NI` / `AM` flag migration changing turn or movement behavior;
- staged targeting losing selection order;
- trap placement becoming more or less permissive than intended;
- passive timing changing at turn start/end or movement hooks;
- stance commands consuming turns or cooldowns by mistake;
- `Stone_Throw` intentionally changes target contract to enemy-unit-only; preserve every non-targeting value while removing the legacy empty-hex variant;
- `Long_Lick` intentionally changes destination contract to player-selected adjacent empty destination; preserve every non-targeting value while removing the legacy first-empty destination choice;
- UI still reading `skills.xml` after SO data migration;
- AI metadata provider still reading only old type/flags and missing target legality;
- serialized skill assets losing existing text/type/flags data during schema migration.

Any mismatch between legacy and new behavior must be treated as a bug unless the user explicitly approves the gameplay change.

## 17. Test Plan Per Skill

Before migration:

- record each active skill id from `SkillCatalog.asset`;
- record every unit assignment from `UnitCatalog.asset` / `Resources/0_Data/Units/*.asset`;
- inspect matching legacy `CastManager` methods and `TosterHexUnit` hooks;
- write expected target/cooldown/effect notes in the implementation completion file.

Automated EditMode test targets:

- activation/cooldown rules,
- `NI` / `AM` behavior,
- passive exclusion from active casts,
- stance repeatability,
- legal target generation,
- illegal submit rejection,
- ordered multi-target validation,
- effect result event order,
- trap trigger result events,
- deterministic preview/apply result shape where feasible.

Manual Unity Play Mode validation per skill:

| Skill id | Manual validation |
| --- | --- |
| `Chope` | Cast around enemies and allies. Confirm affected units, radius, damage, animation/result sequence, and turn completion. |
| `Rush` | Cast with enemy in line, no enemy in line, ally/blocker in line, and obstacle/edge. Confirm destination and hit behavior. |
| `Double_Throw` | Select two enemies and the same enemy twice. Confirm two ordered hits and cooldown/turn completion. |
| `Axe_Rain` | Cast on occupied and empty center. Confirm radius, center/splash damage split, projectiles, and cooldown. |
| `Slash` | Select movement destination then impact direction. Confirm move, affected area, damage scale, and target highlights. |
| `Hate` | Cast on enemy. Confirm mutual status, duration, `NI`, cooldown, and no turn-consuming behavior change. |
| `Insult` | Cast globally. Confirm all enemies affected, no allies affected, `NI`, cooldown, and UI state. |
| `Rage` | Self-cast. Confirm defense/attack conversion, duration, `NI`, cooldown, and skill still works before movement. |
| `Spike_Trap` | Place on empty hex, reject occupied hex, reject existing trap, trigger by entry, confirm slow/damage/remove behavior. |
| `Rope_Trap` | Place on empty hex, reject occupied hex, reject existing trap, trigger by entry, confirm status/remove behavior. |
| `Tough_Skin` | Cast on ally and self. Confirm lizard-unit 25 resistance and other-unit 15 resistance. |
| `Defence_Ritual` | Cast with multiple allies. Confirm all allies receive correct unit-type defensive values. |
| `Force_Pull` | Select ally then empty destination. Reject occupied destination. Confirm teleport timing and final hex. |
| `Stone_Stance` | Self-cast. Confirm resistance, counterattack removal, cooldown, and duration. |
| `Toxic_Fume` | Select movement destination. Confirm movement, self movement lock/counters, enemy taunt area, and cooldown. |
| `Shapeshift` | Self-cast. Confirm movement speed/initiative swap and current wait-like turn behavior. |
| `Long_Lick` | Select enemy at legal range, then select adjacent empty destination. Confirm old first-empty auto-destination path is gone. |
| `Blind_by_light` | Cast on enemy. Confirm blind status duration and cooldown. |
| `Stone_Throw` | Cast on enemy unit target. Confirm empty target is rejected, and split, spawn, self damage, target damage, cooldown, and presentation remain correct. |
| `Fire_Ball` | Cast on occupied and empty center. Confirm area radius, ranged damage modifier, projectiles, and cooldown. |
| `Heavy_Fists` | Select movement destination then impact direction. Confirm HP cost/floor, movement, area pattern, damage modifier, and cooldown/turn use. |
| stance commands | Toggle both Barbarian and Lizard range/melee commands. Confirm icon/skill id swap, no cooldown, no turn cost, and stat modifiers. |
| passives | Run turn/move/attack scenarios for every passive. Confirm trigger timing, status values, and no active button cast path. |

Unity compilation and Play Mode validation remain manual unless the user explicitly allows a Unity test command.

## 18. Definition Of Done

PRD49ABC implementation is done when:

- all current `Resources/0_Data/Skills/*.asset` skills compile and load;
- all active demo skills route through the new skill API;
- migrated skills preserve current gameplay behavior;
- `SkillDefinitionAsset` remains the main skill asset;
- no parallel skill-definition system exists;
- UI skill buttons, cooldown display, icons, and right-click info still work;
- `MouseControler` uses `SkillRules` for target legality;
- `CastManager` is no longer the authority for active demo skill legality or commit;
- migrated legacy `CastManager` reflection paths are removed or unreachable for migrated skills;
- future AI can query usable skills and legal targets without guessing;
- future server validation can validate `SkillUse` from snapshot/data without live scene objects;
- passives and stance commands are represented in the new model;
- `skills.xml` is no longer a runtime source for migrated skill text/rules/effects;
- no new skills were added;
- no cooldown, damage, range, status, target, SFX/VFX, or unit gameplay behavior changed without explicit approval;
- automated focused tests exist for pure skill rules and effect result shape where feasible;
- manual Unity validation checklist is completed for every migrated skill.

## 19. Implementation Instructions For Coding Agent

1. Start with a read-only audit:
   - list all skill assets,
   - list all unit skill assignments,
   - map every active skill id to legacy `CastManager` behavior,
   - identify passive hooks in `TosterHexUnit`, `SpellOverTime`, and `HexClass`.
2. Do not write asset files unless explicit user permission for Unity asset edits is present. If permission is missing, implement code and produce an asset migration checklist, then stop before `.asset` writes.
3. Add schema fields to `SkillDefinitionAsset` with backward-compatible defaults.
4. Keep existing getters and `DataMapper.SkillDefinition` compatibility until all UI callers are migrated.
5. Add the small API classes before touching `MouseControler`.
6. Add tests for pure rule methods before replacing player target flow.
7. Migrate representative hard skills first, then finish all active skills by family.
8. Keep effect execution ordered exactly as authored.
9. Do not make effects choose live scene targets after validation.
10. Preserve current low-level runtime functions only as apply helpers, not as rule authorities.
11. Replace UI target highlights with `SkillRules.GetTargets`.
12. Replace skill commit with validated `SkillCast` plus runtime apply.
13. Verify every migrated skill manually in Unity.
14. Remove or bypass obsolete Inspector/public fields only when the code path they configured is replaced.
15. Do not rename public or serialized fields unless the user approves.
16. Do not edit scenes, prefabs, materials, controllers, `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` unless the user explicitly approves.
17. Do not run `dotnet`, Unity builds, package restore, SDK install, or external build scripts.
18. In final completion notes, list:
   - changed files,
   - migrated skills,
   - remaining legacy references,
   - automated tests added,
   - manual Unity checks still required.

## Implementation - 2026-06-25

### What Changed

`SkillDefinitionAsset` now has new Inspector-backed rule groups: `activationRule`, `targetingRule`, `resolutionRule`, and ordered `effects`. These affect skill legality, targeting, normalized cast resolution, and preview/result event shape. Higher cooldown values delay reuse longer; target counts above `1` create staged selection; wider radius values include more surrounding hexes. Tuning hint: change values only after Play Mode validation because these fields now feed shared player/AI/server-facing rules.

Added `SkillContext`, `SkillUse`, `SkillCast`, `SkillTarget`, `SkillResult`, `SkillRules`, `SkillQuery`, and `SkillDefinitionMigrationDefaults`. `BattleSnapshot` now includes `GameSeed`, `BattleId`, and `NextActionIndex`.

Migrated all 33 `Resources/0_Data/Skills/*.asset` skill assets with activation, targeting, resolution, and effect data. `Long_Lick` and `Stone_Throw` descriptions were updated to match the approved PRD49ABC target contracts.

`MouseControler` now asks `SkillRules` for skill start legality, target highlights, and click validation before the legacy live body runs. `CastManager` permission helpers and Tactical AI skill metadata now read SO activation rules first. `UICanvas` cooldown-fill max now reads SO cooldown data first.

### Automatic Test

Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillRulesTests.cs`. It checks trap placement/rejection, duplicate `Double_Throw`, occupied `Force_Pull` destination rejection, `Stone_Throw` enemy targeting, passive exclusion, and stance repeatability. Run manually in Unity Test Runner: EditMode -> `SkillRulesTests`; expected result is all tests pass. Tests were not run automatically by command line.

### Unity Test

#### Unity Setup

Open Unity and let the updated `.cs` files and migrated skill assets import. Inspect several skill assets under `Assets/Resources/0_Data/Skills/` and confirm the new rule groups are visible. No scene or prefab wiring was intentionally changed.

#### Play Mode Test

Run the PRD manual checklist for all migrated skills. Prioritize `Spike_Trap`, `Rope_Trap`, `Double_Throw`, `Force_Pull`, `Long_Lick`, `Stone_Throw`, and `Heavy_Fists`. Confirm illegal highlighted/clicked targets are rejected, cooldowns and turn use match old behavior, and `Long_Lick` now requires enemy target plus selected adjacent empty destination.

### QA Verdict

Follow-up required. QA report: `_codex/tasks/QA/2026-06-25_1655_049ABC_QA_ArchitectureReview.md`.

Actionable findings: active live commits still call `CastManager.startSpell(...)` reflection after `SkillRules` validation, and passive trigger mutation still lives in legacy hooks. A focused highlight over-selection issue was fixed before the QA report by filtering highlighted hexes through `SkillRules.GetTargets`.

### Notes

This pass completes the SO schema, asset migration, query/validation API, UI legality boundary, and focused tests, but it does not fully close the PRD49ABC definition of done. Remaining legacy references are intentional blockers, not hidden completion: `CastManager` still owns live low-level skill mutation, and passive hooks remain in `TosterHexUnit`, `SpellOverTime`, and `HexClass`.

### Next Steps

Run Unity import/compile and EditMode `SkillRulesTests`. Then implement the QA follow-up: replace final skill commit with `SkillRules.Apply(...)` plus a live runtime adapter, and extract passive triggers into the new skill/status/trap result model.

## Closure - 2026-06-25

Project owner accepted the current implementation as the closing scope for ABC after Play Mode verification showed skills still work as before.

Closed scope:

- `SkillDefinitionAsset` schema exists and remains the main skill asset type.
- All 33 active skill assets under `Resources/0_Data/Skills/` have migrated activation, targeting, resolution, and effect data.
- `SkillRules` / `SkillQuery` API exists for shared skill legality, target query, validation, preview, and future AI/server callers.
- Player skill start and target click legality now pass through the new rule API.
- Skill UI cooldown display reads SO activation cooldown data first.
- `Long_Lick` and `Stone_Throw` target contracts match the approved PRD49ABC changes.
- Existing Play Mode skill behavior remains functional after migration.

Deferred out of ABC and into PRD49ED/PRD49E:

- replacing final live skill execution with `SkillRules.Apply(...)` / runtime adapter,
- removing active-skill dependence on `CastManager.startSpell(...)` reflection,
- extracting passive trigger mutation from `TosterHexUnit`, `SpellOverTime`, and `HexClass`.

Closure verdict:

- ABC is closed as the API/data/validation-boundary migration.
- The QA report remains valid as a scope handoff, not a blocker for closing ABC under the accepted split.
