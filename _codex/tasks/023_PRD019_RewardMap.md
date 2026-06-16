# [TARENA] PRD019: Reward Map

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Reward Cards, Reward Catalog, Reward Resolver, Reward UI
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: `_codex/tasks/022_PRD019_RunBattle.md`

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm reward family boundaries, 12-18 first authored templates, legal target
rules, skill reward eligibility, recovery limits, and what happens when a
reward has no legal target.

Specific questions for `/grill-me`:

- What are the first 12-18 authored reward templates?
- Which exact reward operations are allowed in V1 for Mass, Quality, Width,
  Skill, Recovery, and Economy?
- Which units can be promoted, replaced, exchanged, healed, revived, or taught
  skills?
- What data belongs in `Gained` immediately after battle versus inside reward
  cards?

## What To Build

Create the Reward Map slice: after a won run battle, the player chooses 1 of 3
reward cards. The normal choice shape is Stabilize, Strengthen, and Pivot.

This task preserves the PRD rule: each reward card has one main verb, one clear
target, one plain preview, and one concrete operation.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only.
- Offline Mode generates, previews, validates, and applies reward cards locally
  through a deterministic reward catalog/generator/resolver adapter.
- The local reward resolver is authoritative only for Offline Mode.

Future online target:

- `Online Mode` must receive legal reward choices from a backend or submit a
  reward request that the backend validates before applying.
- Online reward selection must not trust client-authored reward cards,
  client-side legality, or client-side army mutation as final authority.
- Do not add backend calls, online account storage, PlayFab, PUN, Photon, or
  cloud sync in this task.

Shared seam:

- Shape reward choice and reward application as mode-neutral payloads:
  generated reward choice id, template ids, legal target, focused preview,
  selected reward id, before/after army snapshots, run gold changes, and result
  source.
- Offline and future Online should be separate adapters behind the same reward
  interface. UI presenters must not duplicate reward resolver rules.

## Clarification - 2026-06-14

Feedback from the Post-Battle Reward UI mockup:

- A compact `Battle Result` summary on the reward screen is good.
- `Gained` summary is good and should show immediate post-battle gains such as
  run gold or other confirmed simple loot.
- The 1-of-3 reward cards are the right direction: this is exactly the intended
  fast choice shape, but the exact reward catalog and operations require
  `/grill-me` before implementation.
- `Preview Army After Reward` is a strong requirement: clicking or focusing a
  reward card should preview the resulting army before the player confirms.
- A bottom/current army preview strip after selecting a card is good.
- Summary widgets for `RUN GOLD` and inventory are good.

Do not turn this into a full army editor. The player previews one proposed
reward operation, then confirms or chooses a different card.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode and reward authority/source,
- reward template id,
- reward family: Mass, Quality, Width, Skill, Recovery, Economy,
- reward intention: Stabilize, Strengthen, Pivot,
- rarity and stage metadata,
- legal target rules,
- preview data,
- battle result summary id/source,
- gained summary data such as run gold or other confirmed post-battle gains,
- generated reward choice id,
- selected reward id,
- focused/previewed reward id,
- army-after-reward preview snapshot,
- run gold balance after battle and after selected reward where applicable,
- inventory summary data where confirmed,
- reward application result.

Start with a small authored template catalog, not a broad arbitrary generator.

## 2. Backend Methods

Define methods for:

- selecting the Offline reward adapter for current implementation,
- selecting valid reward cards from authored templates,
- building a 1-of-3 reward choice,
- validating legal targets,
- building before/after previews,
- building a compact battle result summary for the reward screen,
- building `Gained` summary data from battle completion and confirmed simple
  gains,
- building army-after-reward preview snapshots when a card is focused/clicked,
- exposing run gold and inventory summary values for the reward screen,
- applying one selected reward operation,
- returning deterministic errors or fallback states when no legal target exists,
- proving preview-vs-actual consistency.

Reward application must be testable without Unity scene objects.

## 3. Frontend Methods

Define presenter/view-model methods for:

- three reward cards,
- compact battle result summary,
- gained summary,
- family/intention labels,
- verb and target text,
- before/after preview,
- legal target selection for small local choices such as skill target,
- focused card preview state,
- preview army after reward data,
- run gold summary,
- inventory summary where confirmed,
- selected card result,
- current army preview after reward.

Frontend methods must not duplicate reward resolver logic.

## 4. UI Setup

Prepare UI setup requirements for the reward choice screen:

- three large reward cards,
- compact `Battle Result` summary,
- `Gained` summary,
- Stabilize, Strengthen, Pivot readable structure,
- clear preview on each card,
- click/focus card behavior that previews `Army After Reward`,
- current army preview strip reflecting the focused reward card,
- `RUN GOLD` summary,
- inventory summary where confirmed,
- Select and Continue commands,
- disabled/error state for invalid card or missing legal target.

Actual Unity scene, prefab, Canvas, material, or asset edits require explicit
permission.

## Mockup Workflow

If a mockup is requested for this task, use
`_codex/skills/make-ui-mockup/SKILL.md`. The accepted output is a Unity UGUI
prefab/prototype with visible components, repeated prefab templates, `Script_*`
owners, and serialized field wiring. Browser/prototype-page mockups do not
satisfy this task's mockup requirement.

## Acceptance Criteria

Done when:

- a won run battle can produce a 1-of-3 reward choice,
- all six reward families are represented in the model/resolver boundary,
- reward cards show clear preview data without raw budget math,
- the screen can show compact battle result and gained summaries,
- focusing/clicking a reward card can preview the army after that reward before
  confirmation,
- run gold and inventory summary data can be displayed where confirmed,
- selected rewards mutate the current run army exactly as previewed,
- Offline Mode can generate and apply rewards without backend services,
- reward choice/application payloads are explicit enough for future Online
  backend validation,
- skill rewards only target legal stacks,
- recovery and exchange rewards respect confirmed limits,
- frontend data exists for the reward card screen,
- no full army editor, gameplay rebalance, or backend SDK is introduced.

## Implementation - 2026-06-15

### What Changed

- `RewardMapModels`, `RewardMapContracts`, `DefaultRewardMapTemplateCatalog`,
  `RewardMapService`, `OfflineRewardMapAdapter`: added Offline 1-of-3 reward
  choice, 12 authored templates, all six reward families, preview/apply
  resolver, legal-target errors, battle result summary, gained summary, army
  after reward preview, and RUN GOLD result payloads. No Inspector fields
  changed.
- `RewardMapScreenController`, reward-card/army-preview/result/command button
  view components, and `PRD19_023_RewardMapPrefabBuilder`: added a
  task-specific Unity UGUI prototype for Reward Map under
  `Assets/Resources/UI/PRD_19/023_RewardMap/` after Unity imports scripts.
  The controller renders `RewardMapChoiceViewData` through
  `OfflineRewardMapAdapter` and applies rewards through `RewardMapService`.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RewardMapServiceTests.cs`.
- Tests check: all six reward families in the authored catalog, 1-of-3
  Stabilize/Strengthen/Pivot view data, battle/gained summary, preview-vs-apply
  consistency, and no-legal-target skill handling.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `RewardMapServiceTests`, click Run. Expected result: 4 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Ran a local `py -3` brace-balance scan for RunMetagame/EditMode C# files.

### Unity Test

#### Unity Setup

- Let Unity import scripts under `Assets/Scripts/RunMetagame/023_RewardMap/`.
- Unity should run the 023 mockup builder automatically once after compile, or
  use `TArena > Mockups > Rebuild PRD 19 023 Reward Map Prefabs`.
- Open `Assets/Resources/UI/PRD_19/023_RewardMap/PRD_19_023_RewardMap.prefab`.

#### Play Mode Test

- Inspect reward cards for Stabilize, Strengthen, Pivot, battle result/gained
  summary, selected details, Select Reward, and Continue.
- Enter Play Mode with the prefab under a Canvas. Click reward cards to focus
  a reward and refresh the preview, click Select to apply through
  `OfflineRewardMapAdapter.Apply(...)`, and click Continue after a selection to
  advance the prototype flow state.

### QA Verdict

- Final QA verdict: Pass with manual Unity import pending.
- QA report: `_codex/tasks/QA/2026-06-15_0242_023_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: exact reward balance is provisional; future work
  should extract shared PRD19 army/unit models instead of reusing the temporary
  unit definition shape.

### Notes

- This task does not add a full army editor, gameplay rebalance, backend SDK,
  PlayFab, PUN, Photon, scenes, materials, controllers, `.inputactions`,
  `.asmdef`, or `.asmref`.
- Backend gaps: durable reward choices, backend-validated Online reward
  selection, inventory summary source: tutaj powinno byc z bazy danych.

### Next Steps

- Run `RewardMapServiceTests` in Unity Test Runner EditMode.
- Let Unity generate/open the Reward Map mockup prefab and inspect its hierarchy.

## Prototype Audit - 2026-06-15

- `RewardMapScreenController` is the 023 UI owner. It creates a sample
  post-battle payload, builds choices with `OfflineRewardMapAdapter.BuildChoice(...)`,
  renders `RewardMapChoiceViewData`, and applies the selected reward with
  `OfflineRewardMapAdapter.Apply(...)`.
- Reward card buttons focus real card view data, Select commits the focused
  reward and locks the choice, and Continue is blocked until a reward has been
  selected.
- `PRD19_023_RewardMapPrefabBuilder` is now the task-specific Unity Editor
  builder for the 023 prefab. It generates nested reward card, army preview
  unit, result/gained panel, and command button prefabs under
  `Assets/Resources/UI/PRD_19/023_RewardMap/Prefabs/`.
- The checked-in prefab YAML can remain stale until Unity imports the new 023
  scripts and generates `.meta` GUIDs. Do not hand-author script GUIDs; let
  Unity import, then run `TArena > Mockups > Rebuild PRD 19 023 Reward Map
  Prefabs`.
