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

## Legacy functional reference

Controlled Blazor snapshot (read-only, no deploy):

- `legacy-reference/Rollback.Web/`
- `legacy-reference/Rollback.Admin/` (Items/effects services used by the Web host)

Inventory and port subphases: [blazor-to-angular-port-plan.md](../admin-tools/items-builder/blazor-to-angular-port-plan.md).

**Execution subphase (reference import):** `A.8 / A.8` — **DONE** (QA item 12616, see `items-builder-a8-qa-item-12616.md`).  
**Stabilization gate (pre-7C):** **PASSED** — [items-builder-stabilization-gate-before-7c.md](../admin-tools/items-builder/items-builder-stabilization-gate-before-7c.md) (build lock, VPS `isRemote=true`, warnings classified).  
**Product subphase (Items Builder):** `7C / 8` — form UX polish **authorized**.

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

- Phase 1 - Items Builder Audit: `DONE`
- Phase 1.5 - Admin Clean Architecture Scaffold: `DONE`
- Phase 2 - Items Builder Read-only API: `DONE / PARTIAL VALIDATED`
- Phase 3 - Angular Items List/Detail: `DONE / PARTIAL VALIDATED`
- Phase 4 - Diagnostics + Preview UI: `DONE`
- Phase 5 - Live Data Workflow: `DONE`
- Phase 6 - Asset Pipeline + PNG Preview: `DONE`
- Phase 6.5A - Client Asset Intelligence Audit: `DONE`
- Phase 7 - Item Create/Edit: `PAUSED / PARTIAL`
- Phase 7A - Item Icon Selector Modal: `DONE`
- Phase 7B - Item Effects/Characteristics Editor (Blazor functional port): `DONE`
- Stabilization Gate (pre-7C: build lock, VPS DB, warnings): `DONE`
- Phase 7C - Item Form UX Polish: `NEXT`
- Phase 7D - Client Sprite Preview Extraction: `PENDING DOCUMENTATION`
- Phase 8 - Publish / QA Workflow: `PENDING`

## Corrective audit status (Phase 7)

Validated on `2026-06-02` in the official repo:

- Current write stack is functional but parity-incomplete versus legacy Blazor item editor.
- Effects/characteristics editing is available on `/admin/items/:id/edit` (Phase 7B); publish and advanced conditions remain deferred.
- Conditions are currently plain string and should remain operator-editable.
- Preview and icon selection are present but need hardening before advanced write scope resumes.
- Phase 7 remains paused until parity corrective slices (7A-7C) are completed.

## Corrective phase references

- [Rollback.Web functional inventory](../admin-tools/items-builder/rollback-web-functional-inventory.md)
- [Blazor to Angular port plan (subphases)](../admin-tools/items-builder/blazor-to-angular-port-plan.md)
- [Items functional port map](../admin-tools/items-builder/items-functional-port-map.md)
- [Blazor functional port map](../admin-tools/items-builder/blazor-functional-port-map.md)
- [Items functional port Phase 7B](../admin-tools/items-builder/items-functional-port-phase7b.md)
- [Blazor parity audit](../admin-tools/items-builder/items-builder-blazor-parity-audit.md)
- [Create/Edit gap analysis](../admin-tools/items-builder/items-builder-create-edit-gap-analysis.md)
- [Phase 7A icon selector plan](../admin-tools/items-builder/items-builder-icon-selector-plan.md)
- [Phase 7B effects editor plan](../admin-tools/items-builder/items-builder-effects-editor-plan.md)

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
- [Blazor parity audit](../admin-tools/items-builder/items-builder-blazor-parity-audit.md)
- [Create/Edit gap analysis](../admin-tools/items-builder/items-builder-create-edit-gap-analysis.md)
- [Icon selector plan](../admin-tools/items-builder/items-builder-icon-selector-plan.md)
- [Effects editor plan](../admin-tools/items-builder/items-builder-effects-editor-plan.md)
- [Phase 8 publish and QA workflow](../admin-tools/items-builder/items-builder-publish-qa-phase8.md)
- [Phase 8 QA checklist](../admin-tools/items-builder/items-builder-qa-checklist.md)
- [Future client publish workflow](../admin-tools/items-builder/items-builder-future-client-publish.md)
- [Post-Phase 8 stabilization](../admin-tools/items-builder/items-builder-vps-qa-stabilization.md)
- [Options loading fix](../admin-tools/items-builder/items-builder-options-loading-fix.md)
- [PNG import plan](../admin-tools/items-builder/items-builder-png-import-plan.md)
- [Dofus Tester visibility diagnosis](../admin-tools/items-builder/dofus-tester-visibility-diagnosis.md)
- [Dofus Tester vendor kamas plan](../admin-tools/items-builder/dofus-tester-vendor-kamas-plan.md)
- [VPS world restart flow](../infrastructure/vps-world-restart-flow.md)

## Cross-cutting Admin migration docs

- [Admin migration docs index](../admin-tools/migration/README.md)
- [Risk register](../admin-tools/migration/admin-tools-migration-risk-register.md)
- [Blazor inventory](../admin-tools/migration/blazor-admin-inventory.md)
- [Angular inventory](../admin-tools/migration/angular-admin-inventory.md)
- [Target architecture](../admin-tools/migration/dofuslegacy-admin-target-architecture.md)
- [Team VPS and database workflow](../admin-tools/migration/team-vps-database-workflow.md)

## Immediate next branch

The next intended branch should execute the corrective parity order:

1. Phase 7C form UX polish
2. Phase 7D client sprite preview extraction (documentation)
3. Phase 8 publish / QA workflow
