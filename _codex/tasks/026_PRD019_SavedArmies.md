# [TARENA] PRD019: Saved Armies

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Saved Army Roster, Defence Selection, Attack History, Saved Army Preview
- Label: implemented-manual-test-pending
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: `_codex/tasks/025_PRD019_SummaryValue.md`

## HITL Gate

Status: completed on 2026-06-15.

Confirmed decisions:

- PRD26 `Saved Armies` is a new architecture and a new roster system.
- Existing `Custom Armies / Arena` creation stays separate and keeps its current
  flow for now.
- Existing `CanvasMainMenu -> Panel Zestawy` / `PanelArmii` / `Generator` is
  not rewritten in this task.
- V1 adds a temporary `Import from Arena` command so development can create
  saved armies without completing a full run.
- `Import from Arena` copies an Arena/custom army into the new saved-army
  format. It does not link to the source and does not keep source as gameplay
  state.
- After import, the saved army is a normal PRD26 saved army.
- Run-created saved armies and imported Arena armies are equivalent once they
  enter the PRD26 roster.
- V1 roster is in-memory. Durable SQLite persistence is a later DB task.

## Problem Statement

The player needs a dedicated `Saved Armies` roster where completed-run armies
can be stored, previewed, selected for defence, and later used by offence and
defence systems.

The project already has a working manual army creator in the main menu, but
that system is a separate custom/Arena army flow. It is useful for development,
but it must not become the storage model for PRD26 saved armies.

PRD26 needs a clean roster architecture that can eventually receive armies from
run completion, while still allowing developers to seed the roster from the
existing Arena/custom army creator during recovery work.

## Solution

Build a new Offline Mode saved-army roster with 8 physical slots, selected-army
preview, `Set Defence`, overwrite, and attack-history presentation.

For V1, all 8 slots are unlocked as a developer default. The model must still
support locked slots because future metaprogress will provide the unlocked slot
count.

Add a temporary `Import from Arena` flow:

1. The player/developer selects a PRD26 saved-army slot.
2. They click `Import from Arena`.
3. The UI shows existing custom/Arena armies from the current creator flow.
4. They choose one custom/Arena army.
5. If the PRD26 slot is empty, the roster saves a new saved army.
6. If the PRD26 slot is taken, overwrite requires confirmation.
7. The importer copies the selected custom/Arena army into the new PRD26
   saved-army snapshot format.
8. Later edits to the source custom/Arena army do not change the PRD26 saved
   army.

The importer exists only to avoid requiring a full run during development. It is
not the long-term source of saved armies.

## User Stories

1. As a player, I want to see my saved army slots, so that I know which armies I
   can use later.
2. As a player, I want empty slots to be visible, so that I understand where a
   future army can be saved.
3. As a player, I want taken slots to show army summary information, so that I
   can compare saved armies quickly.
4. As a player, I want to click a saved army, so that I can inspect its units
   before choosing it for defence.
5. As a player, I want to set one saved army as defence, so that it can defend
   me in future asynchronous battles.
6. As a player, I want to have no defence selected after certain actions, so
   that overwrite does not silently assign a new defence army.
7. As a player, I want only valid non-empty armies to be eligible for defence,
   so that empty or invalid armies cannot create meaningless battle results.
8. As a player, I want imported Arena armies to behave like normal saved
   armies, so that the development shortcut does not create confusing rules.
9. As a player, I want overwriting a slot to require confirmation, so that I do
   not replace a saved army accidentally.
10. As a player, I want overwrite to create a new saved army identity, so that
    old history does not appear to belong to the replacement army.
11. As a player, I want attack history to be visible for the selected saved
    army, so that I can review how that army performed.
12. As a player, I want attack history to show offence and defence results in
    one list, so that I can understand the full battle record for that army.
13. As a player, I want the roster to hide invalid/replaced armies, so that the
    main saved-army screen stays focused on active armies.
14. As a developer, I want to import a custom/Arena army into the PRD26 roster,
    so that I can test saved-army management before full run completion is
    wired in.
15. As a developer, I want the importer to copy into the new format, so that
    PRD26 does not depend on legacy mutable build data.
16. As a developer, I want a clean command boundary for roster operations, so
    that UI does not mutate stores directly.
17. As a developer, I want attack history in a separate store, so that roster
    state and battle-result logs can evolve independently.
18. As a developer, I want army value to be calculated from current unit
    definitions, so that balance changes update saved-army value.
19. As a developer, I want history entries to keep value-at-battle, so that old
    results remain understandable after balance changes.
20. As a future online developer, I want the Offline roster payloads to be
    backend-ready, so that Online Mode can later use an authoritative adapter.

## Implementation Decisions

- `Custom Armies / Arena` and `PRD26 Saved Armies` are separate systems.
- The existing custom/Arena army creator remains available and is not rewritten
  for PRD26.
- `Import from Arena` is a temporary development command that copies from the
  current custom/Arena army system into the new PRD26 saved-army format.
- The import result must not keep a live link to the custom/Arena army.
- The import result must not require a gameplay `sourceType`; imported and
  run-created armies are treated as the same kind of saved army after creation.
- Every save/import creates a new `savedArmyId`.
- Slots are storage locations. Armies are independent identities.
- Overwrite replaces the active army in the selected slot with a new
  `savedArmyId`.
- In V1, overwritten/old armies are hidden from the active roster. Future SQL
  should model this as `IsValid = false`.
- Overwrite of the current-defence slot clears defence. The replacement army
  does not inherit defence status.
- Roster defence rule is `at most one current defence`, not `exactly one`.
- A roster with no current defence is valid.
- `Set Defence` is allowed only for taken slots whose saved army is valid and
  has current calculated value greater than zero.
- V1 uses 8 unlocked slots as a development default.
- The model must still support locked slots for future metaprogress.
- Saved army snapshots do not include hero or leader data.
- V1 stack data is minimal: `unitId` and `amount`.
- Future stack fields such as level, skills, or upgrades may be added later, but
  PRD26 V1 must not pretend they exist.
- Saved armies do not store a permanent `armyValue` as their source of truth.
- Current army value is calculated dynamically from current unit definitions and
  stack amounts.
- A generic army-value function should be extracted or reused from existing
  generator/shop value logic. It should not be saved-army-specific.
- Attack history is stored separately from the roster and queried by
  `savedArmyId`.
- Attack history entries include result kind such as `Offence Win`,
  `Offence Loss`, `Defence Win`, and `Defence Loss`.
- Attack history entries store value-at-battle for attacker and defender.
- UI/presenter code must use service commands rather than mutating stores
  directly.
- Useful service commands include list roster, select slot, import from Arena,
  save to slot, overwrite slot with confirmation, set defence, read selected
  army preview, and read attack history.
- V1 Offline Mode is in-memory. No SQLite, PlayFab, Photon, PUN, or backend
  storage is added in this task.
- Future Online Mode must use a separate backend-authoritative adapter.

Minimal model:

```text
SavedArmyRoster
- slots[8]
- currentDefenceSavedArmyId nullable

SavedArmySlot
- slotId
- savedArmyId nullable
- state: Locked / Empty / Taken

SavedArmy
- savedArmyId
- snapshotId
- isValid
- stacks

SavedArmyStack
- unitId
- amount

AttackHistoryStore
- entries by savedArmyId

ArenaArmyImporter
- reads existing custom/Arena army
- copies it into SavedArmy format
- saves into selected PRD26 slot
```

## UI And Flow Requirements

Prepare the `Saved Armies` screen with:

- 8 physical saved-army slots.
- locked, empty, and taken slot states.
- all 8 slots unlocked by default in V1/dev.
- selected-army detail panel.
- stack list preview.
- dynamically calculated current army value.
- current defence marker when one exists.
- visible no-defence state when no army is selected as defence.
- attack history panel/list for the selected saved army.
- `Set Defence` command.
- `Import from Arena` command.
- overwrite confirmation when importing into a taken slot.
- `Back` command.
- no rank/progression panel.
- no opponent selection on this screen.

The separate future `Attack` screen remains out of scope. In that future flow,
the player chooses an opponent first, then chooses which saved army attacks.

Actual scene, prefab, and Unity asset edits require explicit permission.

## Backend / Service Methods

Define service-level operations for:

- selecting the Offline saved-army roster adapter for current implementation.
- listing saved army slots with locked/empty/taken state.
- listing existing custom/Arena armies for the import picker.
- importing a selected custom/Arena army into a selected saved-army slot.
- saving a run-created saved army candidate into a selected slot.
- requiring overwrite confirmation for taken slots.
- rejecting locked slots as save, overwrite, offence, or defence targets.
- preventing mutation of saved-army snapshots after creation.
- reading selected saved army details for preview.
- calculating current army value from unit definitions.
- setting one current defence army.
- clearing defence when the current-defence army is overwritten.
- listing attack history for a selected saved army.

Do not add manual post-save editing of PRD26 saved armies.

## Persistence Boundaries

V1 is in-memory and does not write the new PRD26 roster to disk.

Prepare boundaries for future SQLite persistence:

- saved army id.
- account/player id.
- slot id.
- immutable army snapshot data.
- current validity flag.
- invalidated/replaced metadata if needed.
- created-from-run id where applicable in future run flow.
- current defence reference.
- attack history records or references.
- created/updated metadata.
- slot state: locked, empty, taken.
- unlocked saved-army slot count.
- account unlock state that affects future runs and future slot availability.

Do not use legacy `buildN.d` files as the PRD26 saved-army store.

## Testing Decisions

Test external behavior through service-level seams rather than UI internals or
store implementation details.

Add or update tests for:

- listing 8 slots.
- V1 default with all 8 slots available.
- locked-slot behavior when a lower unlocked count is supplied by tests.
- import from Arena copies data into a new saved army.
- imported saved army does not change when source custom/Arena data changes.
- importing into an empty slot succeeds.
- importing into a taken slot requires overwrite confirmation.
- overwrite creates a new saved army identity.
- overwrite hides/replaces the old active army.
- overwrite of current-defence slot clears defence.
- `Set Defence` allows only valid taken armies with current value greater than
  zero.
- at most one saved army can be current defence.
- roster may have no current defence.
- current army value is recalculated from current unit definitions.
- attack history is queried by `savedArmyId`, not by slot.
- attack history keeps value-at-battle.

Prior art:

- Summary value service tests already cover save slots, locked slots, and
  overwrite confirmation.
- Battle result service tests already cover saved-army ids and result payloads.

## Out Of Scope

- Rewriting the existing custom/Arena army creator.
- Replacing `PanelArmii` or `Generator`.
- Migrating existing legacy `buildN.d` files into the new saved-army roster.
- Durable SQLite persistence.
- PlayFab, Photon, PUN, cloud sync, backend calls, or Online Mode storage.
- Full Attack screen.
- Opponent selection.
- Active PvP.
- Rank/progression UI on this screen.
- Manual delete/clear slot command.
- Hero/leader support.
- Post-save editing of PRD26 saved armies.
- Skill, upgrade, or stack-level progression beyond V1 `unitId + amount`.

## Acceptance Criteria

Done when:

- PRD26 has a new saved-army roster architecture independent from Custom
  Armies / Arena.
- the roster supports 8 physical slots.
- V1/dev defaults all 8 slots to unlocked.
- locked slot behavior still exists for future metaprogress.
- existing custom/Arena armies can be selected through `Import from Arena`.
- `Import from Arena` copies a custom/Arena army into a new PRD26 saved-army
  snapshot.
- imported saved armies are not linked to their custom/Arena source.
- imported and future run-created saved armies are equivalent after creation.
- a saved army from a run-created candidate can be stored through the same
  roster path when that source is available.
- clicking a saved army shows preview/details.
- current army value is calculated dynamically from current unit definitions.
- taken valid armies with value greater than zero can be marked as defence.
- empty, locked, invalid, or zero-value armies cannot be marked as defence.
- at most one saved army can be marked as current defence.
- no-current-defence is a valid roster state.
- overwrite requires confirmation.
- overwrite creates a new saved army identity.
- overwrite hides the old active army in V1.
- overwrite of current-defence slot clears defence.
- attack history can be represented and reviewed for the selected saved army.
- attack history is keyed by saved army id, not by slot id.
- attack history can show offence and defence result kinds in one list.
- attack history stores value-at-battle.
- frontend data exists for roster, detail preview, defence selection, import,
  overwrite confirmation, and attack history.
- UI presenters use service commands and do not mutate stores directly.
- Offline Mode can manage the saved-army roster without backend services.
- roster payloads are explicit enough for a future Online backend-authoritative
  adapter.
- rank/progression is not required on this screen.
- opponent selection and attack army selection are deferred to a separate
  future `Attack` screen/task requiring `/grill-me`.
- no backend SDK, durable DB write path, post-save army editing, or replacement
  of the current custom/Arena creator is introduced without explicit approval.

## Further Notes

The `Import from Arena` button is a development convenience. Its purpose is to
avoid requiring a completed run every time saved-army roster behavior needs to
be tested. It should not drive the long-term product model.

The current custom/Arena army system remains useful and separate. PRD26 should
copy from it, not merge with it.

## Implementation - 2026-06-15

Status: implemented, Unity manual verification pending.

### What Changed

- Added the PRD26 saved-army domain model, in-memory roster store, attack
  history store, offline service, and adapter under
  `Assets/Scripts/RunMetagame/026_SavedArmies`.
- Added dynamic value calculation using current unit definitions when available,
  with local fallbacks for offline/dev prototype use.
- Added a legacy Arena import boundary that copies existing Arena/custom build
  data into immutable PRD26 saved-army snapshots, then treats imported armies the
  same as future run-created saved armies.
- Added the Saved Armies screen controller and UGUI view scripts for slots,
  selected-army detail, stack preview, Arena import options, attack history,
  `Import from Arena`, `Set Defence`, and `Back`.
- Added an Editor prefab builder menu command:
  `TArena/Mockups/Rebuild PRD 19 026 Saved Armies Prefabs`.
- The generated prefab layout is intentionally specific to Saved Armies:
  8-slot roster grid, parchment-style selected-army snapshot, defence state,
  import panel, and attack-history panel.
- The screen intentionally excludes rank/progression, opponent selection, and a
  full Attack flow because they are out of scope for PRD26.

### Automatic Test

Added/updated EditMode service tests in
`Assets/Scripts/Tests/EditMode/SavedArmiesServiceTests.cs` for:

- eight-slot roster listing.
- locked-slot representation.
- Arena import copy semantics and new saved-army identity.
- overwrite confirmation and defence clearing on overwrite.
- valid-only `Set Defence` behavior.
- attack history lookup by `savedArmyId`, not by slot.

Local static validation performed:

- no stale/conflicting PRD26 type names or placeholder TODO/FIXME markers found
  in the 026 implementation scope.
- no duplicate C# type declarations found in the 026 implementation scope.
- brace-balance scan passed for the 026 implementation and tests.
- no orphan `.meta` files found in the 026 implementation folder.
- prefab builder serialized-field references were checked against the current
  026 view/controller classes.

### Unity Test

Not run in this Codex pass. Per project rules, Unity import, compilation, and
EditMode test execution are left for manual verification in the Unity Editor.

Manual setup:

1. Open Unity and let scripts import.
2. Run `TArena/Mockups/Rebuild PRD 19 026 Saved Armies Prefabs`.
3. Open or instantiate
   `Assets/Resources/UI/PRD_19/026_SavedArmies/PRD_19_026_SavedArmies.prefab`.
4. Verify slot selection, Arena import, overwrite confirmation, `Set Defence`,
   attack-history refresh, and `Back` status behavior.
5. Run `SavedArmiesServiceTests` in EditMode.

### QA Verdict

Manual code review passes for architecture alignment with PRD26:

- UI commands call controller/service methods instead of mutating stores
  directly.
- saved-army identity is separate from slot identity.
- overwrite creates a new identity and clears current defence when needed.
- attack history is separated from roster state.
- offline prototype has a clean adapter seam for future backend authority.
- durable SQLite/backend persistence remains intentionally deferred.

Remaining risk: Unity import could still reveal serialized field or prefab
layout issues because prefab generation was not executed inside Unity during
this pass.

### Next Steps

- Perform Unity import and EditMode test verification.
- If Unity reports serialization errors, fix only the affected 026 view/builder
  references.
- Future DB task should replace the in-memory store with durable saved-army
  persistence and account unlock state.
