# 035 PRD Git Repository Snapshot Recovery

- Status: closed
- Type: repository cleanup / incident record
- Area: Git, GitHub, Unity project hygiene
- Owner: coding-agent
- Date: 2026-06-16

## Problem Statement

The TArenaUnity3D workspace had an unsafe Git state:

- The correct GitHub repository was `DPDdesign/TArenaUnity3D`.
- The local workspace had Git metadata at the workspace root and had also
  previously contained Git metadata inside the Unity project subfolder.
- The `Assets` branch contained the current local project work, but its history
  included files that should not be pushed, including large generated/media
  content.
- The desired GitHub branch was `master`, with the current project state applied
  without preserving the problematic `Assets` branch history.
- Unity generated/imported many local changes while Git cleanup was in progress,
  making it important not to accidentally commit generated or forbidden folders.

## Solution

Create a clean local `master` snapshot from the current `Assets` branch content,
without merging or preserving the `Assets` branch history.

The repository should keep only the intended project tree and explicitly exclude
Unity-generated and local-only folders such as `Library`, `Temp`, `Recordings`,
and `Logs`.

Git LFS should be used for binary Unity assets so GitHub does not receive large
binary blobs in normal Git history.

## User Stories

1. As the project owner, I want the current Unity project state available on the
   correct GitHub repository, so that future work starts from the latest project
   files.
2. As the project owner, I want `master` to receive the current state without the
   old `Assets` branch history, so that previously committed large or unwanted
   files do not block publishing.
3. As the project owner, I want generated Unity folders excluded, so that
   `Library`, `Temp`, `Recordings`, and `Logs` are not committed again.
4. As the project owner, I want the local repository layout cleaned up, so that
   GitHub Desktop and Git operate on the intended workspace repository.
5. As the project owner, I want a record of this cleanup, so that future agents
   understand why the repository history and branch flow changed.

## Implementation Decisions

- Treat the workspace root repository as the active repository.
- Keep the GitHub remote as `DPDdesign/TArenaUnity3D`.
- Use `master` as the branch that should receive the clean project snapshot.
- Do not merge `Assets` into `master`; instead, apply the current tree state as a
  new snapshot commit on top of `origin/master`.
- Keep Git history from `Assets` out of the push path.
- Use Git LFS for Unity binary assets.
- Keep `.gitattributes` macro definitions at the repository root, because Git
  attribute macro definitions are only valid there.
- Keep Unity project-specific ignore rules in the Unity project `.gitignore`.
- Exclude Unity-generated and local-only folders from the tracked snapshot.
- Restore `UnitCatalog.asset` after Unity import temporarily emptied its unit
  list.

## Verification

The clean local `master` snapshot was verified against `Assets` at the path
level after excluding intentionally ignored paths:

- `AssetsPathCount`: 88309
- `MasterPathCount`: 88309
- `MissingOnMaster`: 0
- `ExtraOnMaster`: 0

The current `master` tree was also checked for forbidden generated folders:

- `TArenaUnity3D/Library`: not present
- `TArenaUnity3D/Temp`: not present
- `TArenaUnity3D/Recordings`: not present
- `TArenaUnity3D/Logs`: not present

The local `master` branch was left one commit ahead of `origin/master` with the
snapshot commit:

- `54a672963 [TARENA] Apply current project snapshot`

## Testing Decisions

- No Unity build or automated Unity test run was performed during this cleanup.
- Repository correctness was verified with Git tree comparison and forbidden
  path checks.
- Unity was opened manually by the user after the cleanup; the project eventually
  opened after a long reimport.
- The unit catalog issue was handled by restoring the catalog asset from the
  clean snapshot.

## Out of Scope

- Publishing the branch to GitHub.
- Buying or configuring additional GitHub LFS quota if the push requires it.
- Deleting the old `Assets` branch locally or remotely.
- Committing Unity-generated changes that appeared after the project was opened.
- Auditing gameplay issues unrelated to the repository cleanup.

## Further Notes

After Unity reopened, the working tree contained additional local modifications
in some fonts, UI prefabs, solution/user settings files, and Unity settings.
Those changes were not included in the clean snapshot commit and should be
reviewed separately before any future commit.

## Closure - 2026-06-16

Closed as an archive record. The requested repository cleanup state was achieved
locally: current project files were represented on `master` as a clean snapshot,
without the old `Assets` branch history and without generated Unity folders.
