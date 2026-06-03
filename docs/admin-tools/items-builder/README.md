# Items Builder Docs

This folder consolidates the official Items Builder roadmap, audits, contracts, and implementation notes into the official repo.

Important status note:

- older phase documents may still mention exploratory history
- the accepted baseline is the code and docs now present in `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`
- the official current roadmap state is defined by [admin-tools-migration-master-plan.md](../../roadmap/admin-tools-migration-master-plan.md)

Legacy functional reference (in official repo):

- `legacy-reference/Rollback.Web/` — Blazor UI snapshot (no build)
- `legacy-reference/Rollback.Admin/` — companion services/models for Items
- See [rollback-web-functional-inventory.md](./rollback-web-functional-inventory.md) and [blazor-to-angular-port-plan.md](./blazor-to-angular-port-plan.md)

Current checkpoint:

- Phase 6 - Asset Pipeline: `DONE`
- Phase 6.5A - Item Client Asset Intelligence Audit: `DONE`
- Phase 7 - Item Create/Edit: `PAUSED / PARTIAL` (effects editor shipped; publish/conditions deferred)
- Phase 7A - Item Icon Selector Modal: `DONE`
- Phase 7B - Item Effects/Characteristics Editor + Blazor functional port: `DONE`
- Phase A.8 - QA manual item 12616: `PASSED` (see `items-builder-a8-qa-item-12616.md`)
- Stabilization Gate (build lock, VPS DB, warnings): `PASSED` (see `items-builder-stabilization-gate-before-7c.md`)
- Phase 7C - Item Form UX Polish: `NEXT` (authorized after gate)
- Phase 7D - Client Sprite Preview Extraction: `DOCUMENTED / IMPLEMENTATION PENDING`
- Phase 8 - Publish / QA Workflow: `PENDING`
- Future lane - Client Publication Pipeline: `ANALYSIS COMPLETE / IMPLEMENTATION PENDING`

Operational DB target note:

- the Admin API can point to `LOCAL_DB` or `VPS_DB` through `Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.Development.local.json`
- the switching workflow is documented in [vps-database-connection.md](../../infrastructure/vps-database-connection.md)

Admin Angular canonical location:

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular`
- Admin backend projects for this migration also live under `Angular-tools/Admin/`

Repository rules:

- no se permite crear Angular Admin fuera del repo oficial
- no se permite continuar fases funcionales desde worktrees externos
- los proyectos Admin de esta migracion deben vivir bajo `Angular-tools/Admin/`
- todo documento debe vivir bajo `docs/`
- si una herramienta no existe en el repo oficial, no se puede marcar como implementada

Live Dofus Tester rollout note:

- the audited `Dofus Tester` template/grant flow is documented in `dofus-tester-item-creation.md`
- the production visibility root cause is documented in `dofus-tester-visibility-diagnosis.md`
- reversible SQL scripts live in `infrastructure/sql/items/`
- live rollout was executed on `2026-06-03`
- `sebcos1` is now `Administrator`
- `Dofus Tester` now exists as template `12617`
- persisted inventory rows were granted to the audited `sebcos1` characters
- `12617` is not yet a client-visible shipped item because the client does not know that template id
- adding `12617` to a kamas NPC shop would not solve visibility by itself
- do not apply future inventory grants while target characters may still be online

Client publication distinction:

- `SERVER_ONLY` means the DB row exists but the client does not know the template id
- `CARRIER_TEMPLATE_FALLBACK` means visibility is borrowed from a known client template such as `7754`
- `CLIENT_PUBLISHED` means the template id, i18n, and launcher patch lane were all updated
- no item should be called "visible" just because it exists in `sunshine.items`

Document index:

- `items-builder-migration-phase1.md`
- `items-builder-blazor-inventory.md`
- `items-builder-target-contracts.md`
- `items-builder-angular-plan.md`
- `items-builder-asset-pipeline.md`
- `items-builder-readonly-api-phase2.md`
- `items-builder-angular-phase3.md`
- `items-builder-diagnostics-preview-phase4.md`
- `items-builder-live-data-phase5.md`
- `items-builder-asset-inventory-phase6.md`
- `items-builder-asset-pipeline-phase6.md`
- `items-builder-client-asset-intelligence-future.md`
- `items-client-asset-audit-phase6-5a.md`
- `items-client-asset-source-inventory.md`
- `items-client-i18n-audit.md`
- `items-builder-phase7a-item-icon-selector.md`
- `items-client-sprite-preview-extraction-plan.md`
- `items-client-appearance-mapping-audit.md`
- `items-client-jpexs-ffdec-notes.md`
- `items-builder-create-edit-phase7.md`
- `items-builder-write-contracts-phase7.md`
- `items-builder-angular-create-edit-phase7.md`
- `items-builder-blazor-parity-audit.md`
- `items-builder-create-edit-gap-analysis.md`
- `items-builder-icon-selector-plan.md`
- `items-builder-effects-editor-plan.md`
- `items-builder-effects-serialization-audit.md`
- `items-builder-effects-editor-phase7b.md`
- `rollback-web-functional-inventory.md`
- `blazor-to-angular-port-plan.md`
- `items-builder-a8-qa-item-12616.md`
- `items-builder-stabilization-gate-before-7c.md`
- `items-functional-port-map.md`
- `blazor-functional-port-map.md`
- `items-functional-port-phase7b.md`
- `items-effects-port-map.md`
- `items-builder-publish-qa-phase8.md`
- `items-builder-qa-checklist.md`
- `items-builder-future-client-publish.md`
- `items-builder-client-publication-analysis.md`
- `item-publication-pipeline.md`
- `visible-item-checklist.md`
- `qa-vendor-test-checklist.md`
- `items-builder-vps-qa-stabilization.md`
- `items-builder-options-loading-fix.md`
- `items-builder-png-import-plan.md`
- `dofus-tester-item-creation.md`
- `dofus-tester-visibility-diagnosis.md`
- `dofus-tester-vendor-kamas-plan.md`
