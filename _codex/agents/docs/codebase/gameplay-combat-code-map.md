# TArenaUnity3D Gameplay And Combat Codebase Router

Status: active
Last updated: 2026-06-26

## Purpose

This is a compatibility router for gameplay/combat codebase context. Prefer the
smaller maps below instead of loading this whole area as one context.

## Focused Maps

- Menu, army selection, match startup, player input, turn actions, and Tactical
  AI entry points:
  `_codex/agents/docs/codebase/menu-flow-code-map.md`
- Combat API, action validation, battle readiness, and PRD046-052 battle-flow
  risks:
  `_codex/agents/docs/codebase/battle-action-code-map.md`
- Skills, effects, skill data, skill UI coupling, and pathfinding:
  `_codex/agents/docs/codebase/skills-effects-code-map.md`
