# ADR 004: Battle Action Lifecycle Owns Turn Safety

Status: accepted direction, pending implementation task
Date: 2026-06-13
Project: TArenaUnity3D

## Context

Current battle action flow lets skill logic, skill presentation, movement
coroutines, and turn selection progress independently.

The observed player-facing symptom is that one unit can still be moving or a
projectile/skill presentation can still be resolving while the next unit is
already selectable. This can feel dynamic, but it can also desync board state:
a moving unit can appear stuck between hexes or be visually in a different
state than the tactical model.

Important current code shape:

- `MouseControler` owns input mode and returns to turn detection through
  `CancelUpdateFunc()`.
- `TurnManager` selects the next active unit from tactical state.
- `CastManager` executes skill methods and often calls `SetFalse()` after
  starting presentation.
- `SkillPresentationManager` owns cast, projectile, impact, and reveal
  coroutines, but callers do not consistently wait on a shared action
  completion seam.
- `TosterHexUnit.SetHex(...)` updates logical occupancy before `TosterView`
  finishes smoothing the visual model to the final hex.

The goal is to preserve the good feel of snappy and sometimes overlapping
presentation, while first making the architecture safe.

## Decision

Future action-flow work should introduce a deep **Battle Action Lifecycle
Module** as the seam that owns tactical action progress and turn advancement.

This module should decide when an action is:

1. committed,
2. blocking tactical state,
3. safe for the next turn/action opportunity,
4. still playing non-blocking presentation tail, if allowed.

`TurnManager` should not directly advance into a new unit opportunity while an
action is still in its blocking phase. `MouseControler` and `CastManager`
should not independently decide that an action is over by calling `SetFalse()`
or `CancelUpdateFunc()` without passing through the lifecycle seam.

The desired first architectural rule:

```text
No new actionable unit selection until the current action lifecycle reaches a
model-safe completion point.
```

This does not require every projectile, SFX, trail, or lingering VFX to block
turn advancement forever. It requires one owner to define which parts are
blocking and which parts are allowed presentation tail.

Action validation is intentionally separate from this lifecycle decision. ADR
005 records that the first lifecycle migration should use existing legacy
validation as adapters, while a dedicated action validation seam belongs in a
future PRD.

## Rationale

The current design has low locality. Each caller must know too much about:

- when `Moved` should be set,
- when movement animation has truly reached a stable point,
- when skill results have become model-safe,
- when presentation is only cosmetic,
- when `TurnManager.AskWhosTurn()` may run again.

That makes the modules shallow: much of the real action-ordering interface is
implicit and spread across `MouseControler`, `CastManager`,
`SkillPresentationManager`, `HexMap`, `TosterHexUnit`, and `TosterView`.

Concentrating action lifecycle behind one seam should improve:

- **Locality**: sequencing bugs are investigated in one module first.
- **Leverage**: movement, attacks, skills, passives, and future feel tuning can
  share the same action completion rules.
- **Test surface**: the lifecycle interface becomes the regression seam for
  "next unit cannot act until previous action is model-safe".

## Follow-up Analysis Candidates

The following candidates are not accepted as implementation decisions yet.
They should be explored after the Battle Action Lifecycle seam is designed.

### 1. Movement Completion Module

Files likely involved:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterView.cs`

Problem:

Logical hex occupancy changes immediately, while visual movement completes
later through `TosterView.AnimationIsPlaying`. This can leave the tactical model
and the visible model out of phase.

Analysis goal:

Define one movement completion interface that can guarantee final logical hex,
final visual position, and safe movement cleanup before the action lifecycle
allows another blocking action.

Open questions:

- Should model occupancy update step-by-step during movement or only at final
  arrival?
- Does the lifecycle need a hard final snap to the target hex after smoothing?
- Which movement paths are normal moves, approach skills, pulls, teleports, and
  forced movement?

### 2. Skill Presentation Context Module

Files likely involved:

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/FrontendResultReveal.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`

Problem:

Skill bodies currently choose partial presentation helpers and often call
`SetFalse()` immediately after starting presentation. The caller has to know
which helper includes cast, projectile, impact, frontend reveal, target
reaction, and completion timing.

Analysis goal:

Define one skill presentation context interface that separates:

- blocking action presentation required for model-safe completion,
- frontend result reveal that must happen before the player can act again,
- non-blocking presentation tail that may continue after the next opportunity.

Open questions:

- Which skill presentation phases must block action lifecycle by default?
- Are projectile travel and impact reveal blocking together, or can projectile
  travel be visual tail for specific skills?
- How should deferred/passive effects join the same lifecycle without turning
  every passive VFX into a turn blocker?

## Boundaries

This ADR does not approve:

- building a full new action validation layer; see ADR 005,
- changing damage, cooldowns, targeting, movement, initiative, or turn rules,
- changing gameplay float values,
- renaming public or serialized fields,
- editing Unity assets, prefabs, scenes, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`,
- slowing the game feel as the default solution.

Implementation requires a separate small task with explicit scope, files, and
Unity-side validation steps.

## Verification Direction

The first implementation task should define a Play Mode scenario that proves:

- while unit A is in a blocking movement or skill phase, unit B cannot receive
  actionable controls,
- after the lifecycle reaches model-safe completion, the next valid unit can
  act,
- allowed non-blocking presentation tail can continue without changing board
  state or active unit ownership,
- interrupted or invalid action paths release the lifecycle cleanly.
