# 08 Current Mechanics

Status: template
Project: TArenaUnity3D
Last updated: 2026-06-11

This file records gameplay mechanics and presentation paths that actually exist
in TArenaUnity3D right now.

It is a source of truth for current behavior, not a design wishlist.

## Purpose

Use this file to answer:

- What mechanics are already implemented?
- What is verified in the local Unity project?
- What does each mechanic do for the player?
- Which files, scenes, XML data, Resources paths, or Inspector setup are
  involved?
- What is legacy, fragile, deprecated, or planned for removal?
- What manual Unity check proves the mechanic still works?

This file is especially important during legacy recovery because agents must
not break current local gameplay while cutting backend, multiplayer, plugin, or
presentation dependencies.

## Relationship To Other Context Files

- `01_Game_Design_Document.md` describes intended design.
- `19_Identity.md` describes what must remain true for the game to feel like
  itself.
- `GameplayFeelDoctrine.md` describes how mechanics should be communicated to
  the player.
- `10_Skill_Design_Rules.md` contains current skill-id and presentation rules.
- `02_Current_State.md` contains production state and recovery constraints.

If this file conflicts with verified C# runtime behavior, the current code wins
for technical questions. Update this file after verification.

## Current Mechanics Entry Template

Use this structure for each mechanic:

```text
## [Mechanic Name]

Status:
- Implemented / Partially implemented / Legacy / Broken / Unverified / Planned.

Player-facing purpose:
- What the player sees or does.

Current function:
- How it works at a high level.

Known files/data/scenes:
- Local TArena paths only.

Manual verification:
- What the user can check in Unity.

Risks:
- Coupling, missing setup, string drift, backend dependency, scene dependency,
  asset dependency, unclear ownership, or behavior that is easy to break.

Do not infer:
- What future agents must not assume from this mechanic.
```

## Recommended Sections When Filled

### Player Or Unit Control

Record movement, selection, active unit, turn ownership, camera interaction, and
input behavior that really exists.

### Combat

Record attacks, damage, death, targeting, hit reactions, combat animations, and
combat SFX/VFX paths.

### Skills

Record skill ownership, skill ids, targeting, execution, cooldown/resource
rules, UI icon/info lookup, presentation hooks, and known string-drift risks.

### Map, Level, And Hex/Grid Behavior

Record map generation, hex state, pathfinding, traps, highlights, objectives,
terrain, and end conditions if they exist.

### AI

Record AI behavior only after local verification or code-map analysis.

### UI And Menus

Record build selection, army setup, profile/shop/menu flows, battle UI, skill
UI, and any local persistence used by UI.

### Persistence And Data

Record XML, Resources, local files, `PlayerPrefs`, ScriptableObjects, backend
stubs, and any data source that changes runtime behavior.

### Presentation

Record Animator state names, model setup, SFX managers, VFX hooks, background
music, and setup requirements.

### Removed, Broken, Or Legacy Paths

Record systems that still exist in code but should not be treated as desired
future architecture.

## Guided Fill Questions

When filling current mechanics, ask:

- What mechanic has the user personally seen working in Unity?
- What file or scene owns it?
- Is it local-only, backend-dependent, network-dependent, or mixed?
- What data file or Inspector setup does it depend on?
- What is the player-facing purpose?
- What manual test confirms it?
- What should not be changed while documenting it?
- Is it future gameplay, legacy behavior to preserve, or technical debt to cut?

Ask for evidence before marking something as implemented. If evidence is weak,
use `Unverified`.

## What Belongs Here

- Current TArena gameplay and presentation behavior.
- Local code/data paths needed to understand that behavior.
- Manual Unity verification notes.
- Known risks and legacy dependencies.
- Small, factual notes that prevent rediscovery.

## What Must Not Be Copied Here

- Do not import design decisions, current state, tasks, milestones, enemy lists,
  skills, map designs, or gameplay truth from another project unless the user
  explicitly asks for a comparison or migration note.
- Do not reference another project's local files as default context.
- Do not list Retsot mechanics as TArena mechanics.

## Notes

Template prepared from document-role analysis only. Fill this file from TArena
code inspection, user confirmation, and Unity verification.
