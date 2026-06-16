# 004_DOC-001 Gameplay Context Templates

- Status: closed
- Type: Documentation
- Area: Context templates
- Owner: Project Director

## Goal

Prepare TArenaUnity3D context templates so a future agent can run a guided
conversation such as "Uzupelnijmy GDD" / "Uzupełnijmy GDD" without importing
Retsot gameplay truth.

## Scope

Do:

- Analyze Retsot GDD, Identity, Gameplay Feel, and Current Mechanics only to
  understand document purpose and structure.
- Update TArena GDD, Identity, Current Mechanics, and context routing with
  document purpose, boundaries, and question prompts.
- Add a TArena feel-context template if the folder has no dedicated feel file.
- Keep all TArena files project-local and content-empty where user answers are
  still needed.

Do not:

- Copy Retsot design decisions, enemies, maps, mechanics, milestones, factions,
  or project state.
- Edit code, Unity assets, scenes, prefabs, materials, controllers,
  `.inputactions`, `.asmdef`, or `.asmref`.
- Convert this into a gameplay redesign task.

## Acceptance Criteria

Done when:

- TArena context files explain what GDD, Identity, Feel, and Current Mechanics
  are for.
- Future agents know what questions to ask when filling the GDD.
- `CONTEXT-MAP.md` routes GDD, identity, feel, and gameplay/current-mechanics
  work to the right TArena files.
- The task is archived after completion.

## Analysis - 2026-06-11

Retsot documents were used only as role examples:

- GDD: top-level design charter and production boundaries.
- Identity: core uniqueness, player-facing decision rules, and off-scope
  rejection rules.
- Feel: target player experience, feedback/readability rules, and pacing/payoff
  evaluation.
- Current Mechanics: factual inventory of what exists, why it matters, where it
  lives, and how it is verified.

No Retsot gameplay content was migrated into TArena.

## Implementation - 2026-06-11

Updated TArena context:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/Context/02_Current_State.md`

What changed:

- Added document purpose and boundaries for GDD, Identity, Feel, and Current
  Mechanics.
- Added guided question prompts for future "Uzupelnijmy GDD" /
  "Uzupełnijmy GDD" sessions.
- Added a dedicated TArena feel template.
- Routed GDD, identity, feel, gameplay, and current-mechanics questions through
  the TArena context map.
- Recorded that these are guided templates, not final gameplay truth.

## Closure - 2026-06-11

Closed as documentation/context setup. No code, Unity assets, prefabs, scenes,
materials, controllers, `.inputactions`, `.asmdef`, or `.asmref` were edited.

Remaining user-guided work:

- Run "Uzupelnijmy GDD" / "Uzupełnijmy GDD" inside TArena to provide actual
  TArena gameplay answers.
- Verify current mechanics from TArena code and Unity before marking anything
  as implemented.
