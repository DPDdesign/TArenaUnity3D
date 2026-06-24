# ADR 013: Tactical AI Uses CastManager Skill Bridge Before Skill Rework

Status: accepted direction
Date: 2026-06-24
Project: TArenaUnity3D

## Context

TArena needs a first Tactical Battle AI that plays to win, uses skills, remains
100% legal, and can evaluate future actions from battle state.

The desired long-term architecture is a clean battle snapshot, action
validator, and skill prediction model that can simulate legal player, AI, and
future network actions without mutating live Unity objects.

However, current skill gameplay logic is still owned by legacy `CastManager`:

- `{SkillName}M` methods configure target mode and skill availability through
  legacy flags,
- `{SkillName}` methods execute gameplay effects,
- some skills start movement, damage, traps, projectiles, cooldowns, and
  presentation directly,
- current `SkillDefinitionAsset` / `SkillCatalog` metadata contains skill name,
  type, info, and flags, but not enough structured effect data to simulate every
  skill in a pure snapshot.

Extracting full skill targeting, validation, prediction, and execution data into
the skill catalog is a large future rework already aligned with
`_codex/tasks/034_PRD_FutureSkillLogicAndTargetingExtraction.md`.

## Decision

Tactical AI V1 will not require a full skill-system rework before it can use
skills.

For V1, Tactical AI may route skill legality and execution through the existing
legacy skill surfaces, including `CastManager`, as an adapter boundary.

The AI still must remain legal:

```text
AI intent
-> battle/action validation adapter
-> legacy CastManager / MouseControler / BattleActionLifecycle execution
```

The AI must not gain hidden stat bonuses, ignore cooldowns, cast illegal
targets, or bypass current turn/action rules.

Any cached or background AI planning must be treated as advisory. Before a skill
or action executes in the live battle scene, it must be revalidated against the
current live state and executed through the normal lifecycle path.

## Rationale

Requiring perfect pure-snapshot simulation for every current skill before AI can
use skills would block Tactical AI behind a broad skill architecture rewrite.

Using `CastManager` as a temporary bridge lets the project improve enemy
decision-making in small steps while preserving current gameplay behavior.

This keeps the near-term Tactical AI work focused on:

- team-level battle decision-making,
- legal action candidate generation,
- scoring and search budget,
- use of existing skill behavior,
- safe execution through `BattleActionLifecycle`.

It also keeps the long-term architecture honest: the bridge is not the final
skill model.

## Consequences

- Tactical AI V1 can use current skills without first migrating all skill logic.
- Skill prediction quality may be limited where the only authoritative behavior
  lives inside `CastManager`.
- Background planning and caching must avoid mutating live Unity scene objects.
- Any approximate prediction must be revalidated before execution.
- The bridge may produce less precise long-range skill search than a future
  pure snapshot model.
- A future skill rework should move validation-relevant targeting and
  prediction data into the existing skill catalog model, using the current
  `skillId` as the stable join key.

## Future Direction

Future skill architecture work should build on:

- `_codex/tasks/034_PRD_FutureSkillLogicAndTargetingExtraction.md`,
- `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`,
- `_codex/Context/BattleActionRules.md`.

That future work should gradually replace legacy `CastManager` coupling with:

```text
BattleSnapshot
-> BattleActionValidator
-> Skill prediction data on existing SkillDefinitionAsset / SkillCatalog
-> ValidatedAction
-> BattleActionLifecycle
-> skill execution adapter or migrated skill executor
```

The future rework should not create a second skill identity system. The existing
skill id remains the join key for UI, unit ownership, validation, prediction,
presentation, and execution.

## Boundaries

This ADR does not approve:

- rewriting `CastManager` now,
- adding a separate `SkillPredictionCatalog`,
- migrating all skill execution now,
- changing skill ids, unit skill ownership, cooldowns, targeting ranges, damage,
  movement, status values, initiative, or turn rules,
- editing Unity assets, prefabs, scenes, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.

## Verification Direction

Tactical AI implementation tasks should verify that:

- AI-selected skill actions execute through normal battle lifecycle paths,
- illegal skill casts are rejected the same way player illegal casts are,
- cooldowns, target rules, action timing, and turn completion remain unchanged,
- cached AI plans are discarded or recalculated when the live battle state no
  longer matches the planned state,
- no live Unity objects are mutated during background planning.
