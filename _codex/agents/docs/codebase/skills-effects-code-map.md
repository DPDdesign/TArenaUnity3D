# TArenaUnity3D Skills, Effects, And Pathfinding Codebase Map

Status: active
Last updated: 2026-06-30

## Skills And Effects

Key files:

- `Assets/Scripts/SkillDefinitionAsset.cs`
- `Assets/Scripts/SkillCatalog.cs`
- `Assets/Scripts/DataMapper.cs`
- `Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `Assets/Scripts/Lesisz/Skills/SkillUse.cs`
- `Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `Assets/Scripts/Lesisz/Skills/SkillContext.cs`
- `Assets/Scripts/Lesisz/Skills/SkillTarget.cs`
- `Assets/Scripts/Lesisz/Skills/SkillDefinitionMigrationDefaults.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `Assets/Scripts/Lesisz/HexMap/CombatDamageService.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `Assets/Scripts/Lesisz/Skills/SkillsDefault.cs`
- `Assets/UICanvas.cs`
- `Assets/RightClickInfo.cs`
- `Assets/RightClickInfoSkill.cs`
- `Assets/SkillInfoPresentation.cs`
- `Assets/Resources/0_Data/SkillCatalog.asset`
- `Assets/Resources/0_Data/Skills/*.asset`
- `Assets/Resources/0_Data/UnitCatalog.asset`
- `Assets/Resources/0_Data/Units/*.asset`

Responsibilities:

- load unit skill assignment from the unit `ScriptableObject` catalog into
  `TosterHexUnit.skillstrings`,
- load skill definitions from `SkillCatalog` / `SkillDefinitionAsset`,
- display skill buttons and icons from `SelectedToster.skillstrings`,
- expose skill metadata/right-click info through `DataMapper`,
- validate skill start/target legality through `SkillRules`,
- convert validated `SkillCast` effects into deterministic
  `BattleActionResult` events through `BattleActionRules`,
- route combat-style skill damage through `CombatDamageService` using distinct
  skill roll purposes and no legacy damage fallback,
- skip empty target-based damage/status events instead of emitting empty target
  ids; actor-targeted effects still resolve to the caster,
- execute committed skill result events in live play through
  `TacticalAISkillRulesExecutor`,
- simulate committed skill result events for AI search through
  `TacticalAISearchScoring`,
- expose future AI/server-facing skill queries through `SkillQuery`,
- route selected skill slots through `MouseControler.SelectedSpellid`,
- stage and validate selected skill targets,
- keep legacy live skill mutation working until PRD49ED replaces it,
- apply temporary status/stat modifiers through `SpellOverTime`,
- load skill icons through `DataMapper` / Resources sprite paths.

Current structure:

- `UnitCatalog.asset` is the current source for unit tier and which skill
  strings each unit can legally have.
- `SkillCatalog.asset` and `Resources/0_Data/Skills/*.asset` are the current
  source for active skill definition data.
- `SkillDefinitionAsset.skillName` is the canonical skill id.
- PRD49ABC added `ActivationRuleData`, `TargetingRuleData`,
  `ResolutionRuleData`, ordered `SkillEffect[]`, and the shared `SkillRules`
  API/data model.
- PRD055B migrated combat-style skill damage events
  (`BasicAttackDamage`, `RangedBasicAttackDamage`) to `CombatDamageService`.
  Snapshot AI planning/simulation must inject a snapshot-backed combat catalog
  rather than relying on the default live `DataMapper` service.
- run, reward, shop, and future player progression state may lock or unlock
  those legal skills for a specific stack.
- `TosterHexUnit.InitateType(name)` loads those strings into
  `TosterHexUnit.skillstrings`.
- `UICanvas` uses the same skill strings to load icons from
  `Resources/Sprites/Skill_Icons/{SkillName}`.
- `MouseControler` selects a skill slot and should use `SkillRules` for skill
  start legality, target highlights, and clicked-target validation.
- Current live/default skill commit can still call `CastManager.startSpell(...)`
  reflection bodies for actual mutation until PRD49ED replaces execution.
- Some skills directly play Animator states such as `Skill1`, `Skill2`, or
  `"Skill" + (SelectedSpellid + 1)`.
- Some skills directly instantiate projectiles from `CastManager.Projectiles` or
  store a prefab on `TosterHexUnit.Projectile`.

Risks:

- `CastManager` is over 2000 lines and mixes targeting mode, skill definitions,
  animation/projectile setup, RPCs, and cooldown changes.
- skill names are string-based and must match the unit catalog, skill catalog,
  icon paths, presentation entries, and remaining compatibility execution paths.
- passive skills have SO metadata after PRD49ABC, but trigger mutation can still
  live in `TosterHexUnit`, `SpellOverTime`, `HexClass`, and related hooks.
- future skill VFX/SFX data on unit models can drift from unit catalog skill
  assignment unless both use the exact skill string as the join key.
- `CastManager.Projectiles[index]`, `Axe(...)`, and `FireBall(...)` are legacy
  projectile paths; planned skill VFX/SFX work should replace them with
  catalog-driven projectile VFX moved by code, without Rigidbody physics.
- `skills.xml` is not current skill truth. Treat it as legacy/migration history
  unless current code inspection proves a remaining runtime dependency.

## Pathfinding

Key files:

- `Assets/Scripts/Lesisz/PathFinding/HPath.cs`
- `Assets/Scripts/Lesisz/PathFinding/IPathTile.cs`
- `Assets/Scripts/Lesisz/PathFinding/IQPathUnit.cs`
- `Assets/Scripts/Lesisz/PathFinding/IQPathWorld.cs`
- `Assets/Scripts/Lesisz/PathFinding/IQPath_AStar.cs`
- `Assets/Scripts/Lesisz/PathFinding/PathfindingPriorityQueue.cs`
- `Assets/Scripts/Lesisz/PathFinding/Priority Queue/*`

Responsibilities:

- A* path resolution over `IPathTile`,
- movement-cost calculation delegated to units and hexes,
- priority queue support.

Risks:

- generic priority queue code may be third-party or copied utility code,
- gameplay rules for blocking and movement cost live mostly in `TosterHexUnit`
  and `HexClass`, not in a separate rules service.
