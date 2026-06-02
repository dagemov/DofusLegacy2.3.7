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
3. Keep new Admin code inside `Angular-tools/Admin/`.
4. Keep Admin work inside the existing `Sunshine.sln`.
5. Treat exploratory docs imported from parallel branches as reference, not as accepted code baseline.
6. Do not create `RollblackLegacy.Admin.Angular` outside the official repo.
7. If a tool does not exist in the official repo, it cannot be marked as implemented.

## Admin Angular canonical location

The Admin Angular workspace now lives in the official repo at:

`Angular-tools/Admin/RollblackLegacy.Admin.Angular`

The Admin API, Application, Contracts, Domain, and Infrastructure projects for this migration also live under:

`Angular-tools/Admin/`

Operational rules:

- no se permite crear Angular Admin fuera del repo oficial
- no se permite continuar fases funcionales desde worktrees externos
- los proyectos Admin de esta migracion deben vivir bajo `Angular-tools/Admin/`
- todo documento debe vivir bajo `docs/`
- si una herramienta no existe en el repo oficial, no se puede marcar como implementada

## Current execution checkpoint

- Phase 6 - Asset Pipeline: `DONE`
- Phase 6.5A - Item Client Asset Intelligence Audit: `DONE`
- Phase 7A - Item Icon Selector: `DONE`
- Phase 7 - Item Create/Edit: `DONE / LIVE`
- Phase 8 - Publish / QA Workflow: `DONE`
- Post-Phase 8 Stabilization / VPS Data / Assets / UX: `DONE`
- Items Builder MVP: `DONE / CLIENT PUBLISH PENDING`

## Phase 7 completion checkpoint

Validated on `2026-06-02` in the official repo:

- backend write endpoints live:
  - `POST /api/admin/v1/items`
  - `PUT /api/admin/v1/items/{itemId}`
  - `POST /api/admin/v1/items/{itemId}/duplicate`
- Angular routes live:
  - `/admin/items/new`
  - `/admin/items/:itemId/edit`
  - `/admin/items/:itemId/duplicate`
- `ItemIconSelector` integrated into the write form
- `ItemId != IconId != AppearanceId` stays explicit in API and UI
- live DB smoke test completed for create, update, and duplicate with immediate cleanup of temporary rows
- preview by `IconId` validated as `FOUND` for `IconId=1001`

## Phase 8 completion checkpoint

Validated on `2026-06-02` in the official repo:

- backend QA summary endpoint live:
  - `GET /api/admin/v1/items/{itemId}/qa-summary`
- item detail now exposes a `QA / Publish Readiness` panel
- derived `READY_FOR_QA` vs `BLOCKED` status is visible without changing DB schema
- manual QA checklist is visible and copyable with manual fallback
- Sunshine QA command audited and documented:
  - `.item add <itemId> <quantity> [CharacterName]`
- explicit publish blockers remain visible:
  - description publish deferred
  - `IsVisible` persistence deferred
  - real client publish disabled in this phase

## What remains after Phase 8

Items Builder is now at MVP completion for Admin-side CRUD and QA readiness.

Still deferred to future publish-focused work:

- real client publish
- client i18n export/import
- description payload management
- `IsVisible` persistence
- PNG upload workflow
- weapon-specific workflow

## Post-Phase 8 stabilization checkpoint

Validated on `2026-06-02` in the official repo:

- `GET /api/admin/v1/health/db` now exposes safe target fields for `host`, `port`, `user`, and `isRemote`
- current working path is confirmed as `Angular -> local Admin API -> local Sunshine MySQL`
- item-type options are confirmed to come from `Sunshine.Protocol/Enums/ItemTypeEnum.cs`, not from DB rows
- item-set options are confirmed to come from `sunshine.items_sets`
- shared Angular problem messaging is now more human and operator-facing in Spanish
- a first controlled PNG import wave added `1002.png` through `1012.png` from the approved `amuletos_png` source
- safe restart scripts were added for future World-only restarts, but live VPS validation is currently blocked by SSH `Permission denied (publickey)`

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
- [Phase 7 create/edit](../admin-tools/items-builder/items-builder-create-edit-phase7.md)
- [Phase 7 write contracts](../admin-tools/items-builder/items-builder-write-contracts-phase7.md)
- [Phase 7 Angular workflow](../admin-tools/items-builder/items-builder-angular-create-edit-phase7.md)
- [Phase 8 publish and QA workflow](../admin-tools/items-builder/items-builder-publish-qa-phase8.md)
- [Phase 8 QA checklist](../admin-tools/items-builder/items-builder-qa-checklist.md)
- [Future client publish workflow](../admin-tools/items-builder/items-builder-future-client-publish.md)
- [Post-Phase 8 stabilization](../admin-tools/items-builder/items-builder-vps-qa-stabilization.md)
- [Options loading fix](../admin-tools/items-builder/items-builder-options-loading-fix.md)
- [PNG import plan](../admin-tools/items-builder/items-builder-png-import-plan.md)
- [VPS world restart flow](../infrastructure/vps-world-restart-flow.md)

## Cross-cutting Admin migration docs

- [Admin migration docs index](../admin-tools/migration/README.md)
- [Risk register](../admin-tools/migration/admin-tools-migration-risk-register.md)
- [Blazor inventory](../admin-tools/migration/blazor-admin-inventory.md)
- [Angular inventory](../admin-tools/migration/angular-admin-inventory.md)
- [Target architecture](../admin-tools/migration/dofuslegacy-admin-target-architecture.md)
- [Team VPS and database workflow](../admin-tools/migration/team-vps-database-workflow.md)

## Immediate next branch

After Phase 8, the next intended branch should target future client publish prerequisites or another admin module, not a reimplementation of Items CRUD.
