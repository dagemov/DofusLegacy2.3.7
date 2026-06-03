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
- Phase 7C - Item Form UX Polish: `NEXT`
- Phase 7D - Client Sprite Preview Extraction: `PENDING DOCUMENTATION`
- Phase 8 - Publish / QA Workflow: `PENDING`

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
- reversible SQL scripts live in `infrastructure/sql/items/`
- current live blocker: controlled VPS restart is still blocked by `Permission denied (publickey)` from this workstation
- do not apply the inventory grant while target characters may still be online

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
- `items-functional-port-map.md`
- `blazor-functional-port-map.md`
- `items-functional-port-phase7b.md`
- `items-effects-port-map.md`
- `items-builder-publish-qa-phase8.md`
- `items-builder-qa-checklist.md`
- `items-builder-future-client-publish.md`
- `items-builder-vps-qa-stabilization.md`
- `items-builder-options-loading-fix.md`
- `items-builder-png-import-plan.md`
- `dofus-tester-item-creation.md`
