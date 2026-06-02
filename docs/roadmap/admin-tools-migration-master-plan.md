# Admin Tools Migration Master Plan

## Snapshot

- Date: `2026-06-02`
- Official repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`
- Official solution: `Sunshine.sln`
- Official documentation roots:
  - `docs/admin-tools/`
  - `docs/roadmap/`
  - `docs/website/`
  - `docs/infrastructure/`
  - `docs/combat/`

## Repository rules

1. Use `C:\Users\Hombr\source\repos\DofusLegacy2.3.7` as the single source of truth.
2. Do not start new phases in external worktrees or parallel repos unless explicitly approved.
3. Keep new code inside `src/`.
4. Keep Admin work inside the existing `Sunshine.sln`.
5. Treat exploratory docs imported from parallel branches as reference, not as accepted code baseline.

## Current execution checkpoint

- Phase 6 - Asset Pipeline: `DONE`
- Phase 6.5A - Item Client Asset Intelligence Audit: `DONE`
- Phase 7 - Item Create/Edit: `PAUSED`
- Phase 7A - Item Icon Selector: `NEXT`

## Why Phase 7 is paused

- the official repo and doc structure had diverged from the exploratory worktree history
- the repo needed one in-place roadmap and one in-place doc hierarchy first
- the next safer step is a narrow icon selector slice instead of resuming full write flow immediately

## Phase 7A - Item Icon Selector

Goal:

- define the icon-picking UI slice that lets operators search and choose previewable icons before full create/edit resumes

Must support:

- preview PNG
- `IconId`
- client-facing name when available
- search
- selection

Initial catalog:

- `/assets/item-previews/by-icon`

Out of scope:

- full item create/edit
- weapon audit
- `44k` record audit
- mass extractors
- `SWF` extraction
- `D2P` extraction
- upload
- publish
- gameplay changes

## Items Builder doc set

Primary folder:

- [docs/admin-tools/items-builder](../admin-tools/items-builder/README.md)

Key references:

- [Phase 1 audit](../admin-tools/items-builder/items-builder-migration-phase1.md)
- [Target contracts](../admin-tools/items-builder/items-builder-target-contracts.md)
- [Asset pipeline](../admin-tools/items-builder/items-builder-asset-pipeline.md)
- [Future client asset intelligence](../admin-tools/items-builder/items-builder-client-asset-intelligence-future.md)
- [Phase 6.5A audit](../admin-tools/items-builder/items-client-asset-audit-phase6-5a.md)
- [Phase 7A icon selector](../admin-tools/items-builder/items-builder-phase7a-item-icon-selector.md)

## Cross-cutting Admin migration docs

- [Admin migration docs index](../admin-tools/migration/README.md)
- [Risk register](../admin-tools/migration/admin-tools-migration-risk-register.md)
- [Blazor inventory](../admin-tools/migration/blazor-admin-inventory.md)
- [Angular inventory](../admin-tools/migration/angular-admin-inventory.md)
- [Target architecture](../admin-tools/migration/dofuslegacy-admin-target-architecture.md)
- [Team VPS and database workflow](../admin-tools/migration/team-vps-database-workflow.md)

## Immediate next branch

After this realignment, the next intended branch is:

`feature/items-builder-icon-selector-phase7a`
