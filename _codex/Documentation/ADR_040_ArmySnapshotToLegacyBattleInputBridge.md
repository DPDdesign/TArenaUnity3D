# ADR 040: Army Snapshot To Legacy Battle Input Bridge

Status: accepted
Date: 2026-06-18
Related: PRD040

## Context

Run metagame state stores player and enemy armies as persisted army snapshots. The current tactical battle scene still boots through legacy `HexMap` / `TeamClass` input surfaces that historically use PlayerPrefs and local build-file style ids.

PRD040 needs battle launch from Run Map without rewriting the tactical battle scene.

## Decision

The run metagame source of truth is `army_snapshots`.

Battle launch uses a transitional bridge from prepared run battle snapshots into the current legacy battle input surface. `OfflineRunBattleLaunchAdapter` loads the prepared player snapshot and the node enemy snapshot from `army_snapshots`, writes temporary `PanelArmii.BuildG` files into reserved runtime build slots `9001` and `9002`, then points `PlayerPrefs` keys `YourArmy` and `EnemyArmy` at those slots before the tactical scene loads.

The legacy build files and PlayerPrefs keys are adapter output only. They must not be treated as authoritative run state.

`GameSceneManager` remains a UI/scene transition coordinator only. It must not choose encounters, generate enemy armies, or mutate run progression.

## Consequences

- Run Map can travel, prepare battle, and enter the tactical scene without waiting for a full battle-scene rewrite.
- Enemy and player army selection remains testable in run metagame services and DB stores.
- Future work can replace the legacy adapter with direct runtime snapshot consumption in the tactical battle scene.
- Any new battle launch code must avoid treating PlayerPrefs/build files as authoritative run state.
