# Admin Tools Migration Master Plan

## Snapshot

- Date: `2026-06-03`
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
- `legacy-reference/Rollback.Admin/`

Inventory and port subphases:

- [blazor-to-angular-port-plan.md](../admin-tools/items-builder/blazor-to-angular-port-plan.md)

Execution checkpoints:

- `A.8 / A.8` reference import: `DONE`
- pre-7C stabilization gate: `PASSED`
- `Phase 8 / 8` publish and client visibility workflow: `DONE`

## Repository rules

1. Use `C:\Users\Hombr\source\repos\DofusLegacy2.3.7` as the single source of truth.
2. Do not start new phases in external worktrees or parallel repos unless explicitly approved.
3. Keep new Admin code inside `Angular-tools/Admin/`.
4. Keep Admin work inside the existing `Sunshine.sln`.
5. Treat exploratory docs imported from parallel branches as reference, not as accepted code baseline.
6. Do not create `RollblackLegacy.Admin.Angular` outside the official repo.
7. If a tool does not exist in the official repo, it cannot be marked as implemented.
8. No item can be called client-visible unless the template exists in client metadata and the launcher delivery lane is accounted for.
9. Never continue implementing if `docs/handoffs/AGENT_HANDOFF.md` was not produced or is clearly outdated.
10. The next agent must read the latest handoff before starting work.
11. If the current agent is getting close to the last `15%` of paid token budget or similar operational limit, stop implementation and update the handoff first.

## Admin Angular canonical location

The Admin Angular workspace lives in:

`Angular-tools/Admin/RollblackLegacy.Admin.Angular`

The Admin API, Application, Contracts, Domain, and Infrastructure projects also live under:

`Angular-tools/Admin/`

Operational rules:

- no se permite crear Angular Admin fuera del repo oficial
- no se permite continuar fases funcionales desde worktrees externos
- los proyectos Admin de esta migracion deben vivir bajo `Angular-tools/Admin/`
- todo documento debe vivir bajo `docs/`
- si una herramienta no existe en el repo oficial, no se puede marcar como implementada

## Current execution checkpoint

Official operating order:

```txt
Macro 1 - Items Builder
Phase 1 DONE
Phase 1.5 DONE
Phase 2 DONE
Phase 3 DONE
Phase 4 DONE
Phase 5 DONE
Phase 6 DONE
Phase 6.5A DONE
Phase 7A DONE
Phase 7B DONE
Phase 7C DONE
Phase 7D DONE (documentary)
Phase 8 DONE

Macro 2 - Client Identity Audit Tool: COMPLETE (Phase 1–4 DONE)
Macro 3 - Sprite Preview Pipeline: COMPLETE (Phases 1–7 DONE)
Macro Items Final - Items Builder effects catalog parity: DONE (7D.1–7D.5; browser QA PARTIAL)
Macro 4 - Spells Builder: DEFERRED (until Macro Items Final closes)
Macro 5 - Glyph Builder: DEFERRED
Macro 6 - Maps Builder: DEFERRED
```

Phase status summary:

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
- Phase 7B - Item Effects/Characteristics Editor (codec + save): `DONE`
- Phase 7B - Full Rollback.Web effects catalog parity: `NOT DONE` (Macro Items Final)
- Phase 7C - Item Form UX Polish: `DONE`
- Phase 7D - Client Sprite Preview Extraction: `DONE (DOCUMENTARY)`
- Phase 8 - Publish / QA Workflow: `DONE`
- Phase 8 adds `Item Publication Status` plus the visibility matrix for `7754`, `12616`, and `12617`.
- Macro 2 - Client Identity Audit Tool / Phase 1: `DONE`
- Macro 2 now has a read-only scaffold under `Infrastructure/scripts/ClientIdentityAudit`.
- Macro 2 / Phase 2 now exposes the same audit through the Admin API and reuses it for `publication-status`.
- Macro 2 stabilization gate before Phase 3: `PASSED`
- Macro 2 / Phase 3 - Angular Client Identity Diagnostics: `DONE`
- Macro 2 / Phase 4 - Batch/report diagnostics: `DONE`
- Macro 3 / Phase 1 - Sprite preview source map + audit scaffold: `DONE / PARTIAL`
- Macro 3 / Phase 2 - D2P extractor research + minimal proof: `DONE`
- Macro 3 / Phase 3 - Curated icon import / Angular integration: `DONE`
- Macro 3 / Phase 4 - Curated import workflow / selector integration: `DONE / PARTIAL`
- Macro 3 / Phase 5 - Appearance identity audit + preview feasibility: `DONE`
- Macro 3 / Phase 6 - Curated appearance preview diagnostics (`by-appearance/`): `DONE / PARTIAL`
- Macro 3 / Phase 7 - Final QA + macro closure: `DONE`
- EntityLook renderer (Sprite Preview): `DEFERRED / NOT REQUIRED FOR ITEMS BUILDER MVP`
- Future lane - Client Publication Pipeline for custom items: `ANALYSIS COMPLETE`

## Corrective audit status

Validated on `2026-06-03` in the official repo:

- Current write stack is functional but parity-incomplete versus legacy Blazor item editor.
- Effects save/edit is available on `/admin/items/:id/edit`; **full effect catalog parity is pending** (Macro Items Final 7D.2–7D.5).
- Conditions remain plain operator-editable string.
- Preview, icon selection, and publication diagnostics are present.
- Publish and client patch workflow remain separate from write runtime flow.

## Corrective phase references

- [Rollback.Web functional inventory](../admin-tools/items-builder/rollback-web-functional-inventory.md)
- [Blazor to Angular port plan](../admin-tools/items-builder/blazor-to-angular-port-plan.md)
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
- [Phase 7D sprite preview extraction plan](../admin-tools/items-builder/items-client-sprite-preview-extraction-plan.md)
- [Phase 7D appearance mapping audit](../admin-tools/items-builder/items-client-appearance-mapping-audit.md)
- [Phase 7D JPEXS / FFDec notes](../admin-tools/items-builder/items-client-jpexs-ffdec-notes.md)
- [Phase 7 create/edit](../admin-tools/items-builder/items-builder-create-edit-phase7.md)
- [Phase 7 write contracts](../admin-tools/items-builder/items-builder-write-contracts-phase7.md)
- [Phase 7 Angular workflow](../admin-tools/items-builder/items-builder-angular-create-edit-phase7.md)
- [Phase 8 publish and QA workflow](../admin-tools/items-builder/items-builder-publish-qa-phase8.md)
- [Phase 8 QA checklist](../admin-tools/items-builder/items-builder-qa-checklist.md)
- [Item client visibility matrix](../admin-tools/items-builder/items-client-visibility-matrix.md)
- [Item publish decision workflow](../admin-tools/items-builder/items-publish-decision-workflow.md)
- [Items admin command catalog](../admin-tools/items-builder/items-admin-command-catalog.md)
- [Items vendor publication workflow](../admin-tools/items-builder/items-vendor-publication-workflow.md)
- [Items production QA checklist](../admin-tools/items-builder/items-production-qa-checklist.md)
- [Future client publish workflow](../admin-tools/items-builder/items-builder-future-client-publish.md)
- [Client publication analysis](../admin-tools/items-builder/items-builder-client-publication-analysis.md)
- [Item publication pipeline](../admin-tools/items-builder/item-publication-pipeline.md)
- [Macro 4 Client publication README](../admin-tools/client-publication/README.md)
- [Macro 4 Phase 1 dry-run manifest](../admin-tools/client-publication/client-item-publication-pipeline-phase1.md)
- [Macro 4 Phase 3A D2O Item class staging](../admin-tools/client-publication/client-publication-phase3a-d2o-item-class.md)
- [Macro 4 D2O schema report](../admin-tools/client-publication/client-d2o-item-schema-report.md)
- [Macro 4 D2O round-trip staging](../admin-tools/client-publication/client-d2o-roundtrip-report.md)
- [Macro 4 Phase 3B D2I writer](../admin-tools/client-publication/client-publication-phase3b-d2i-writer.md)
- [Macro 4 D2I format notes](../admin-tools/client-publication/client-d2i-format-notes.md)
- [Visible item checklist](../admin-tools/items-builder/visible-item-checklist.md)
- [QA vendor test checklist](../admin-tools/items-builder/qa-vendor-test-checklist.md)
- [VPS restart safety checklist](../infrastructure/vps-restart-safety-checklist.md)
- [Post-Phase 8 stabilization](../admin-tools/items-builder/items-builder-vps-qa-stabilization.md)

## Cross-cutting Admin migration docs

- [Admin migration docs index](../admin-tools/migration/README.md)
- [Risk register](../admin-tools/migration/admin-tools-migration-risk-register.md)
- [Blazor inventory](../admin-tools/migration/blazor-admin-inventory.md)
- [Angular inventory](../admin-tools/migration/angular-admin-inventory.md)
- [Target architecture](../admin-tools/migration/dofuslegacy-admin-target-architecture.md)
- [Team VPS and database workflow](../admin-tools/migration/team-vps-database-workflow.md)

## Client Identity doc set

- [Client Identity README](../admin-tools/client-identity/README.md)
- [Client Identity Phase 1](../admin-tools/client-identity/client-identity-audit-tool-phase1.md)
- [Client Identity Phase 2](../admin-tools/client-identity/client-identity-admin-layer-phase2.md)
- [Client Identity API contracts](../admin-tools/client-identity/client-identity-api-contracts.md)
- [Client Identity stabilization gate](../admin-tools/client-identity/client-identity-stabilization-gate-before-phase3.md)
- [Client Identity source map](../admin-tools/client-identity/client-identity-source-map.md)
- [Client Identity item check report](../admin-tools/client-identity/client-identity-item-check-report.md)
- [Client Identity Phase 3 Angular](../admin-tools/client-identity/client-identity-angular-diagnostics-phase3.md)
- [Client Identity Phase 4 batch/report](../admin-tools/client-identity/client-identity-batch-report-phase4.md)
- [Client Identity batch report sample](../admin-tools/client-identity/client-identity-batch-report-sample.md)

## Sprite Preview doc set

- [Sprite Preview README](../admin-tools/sprite-preview/README.md)
- [Sprite Preview Phase 1](../admin-tools/sprite-preview/sprite-preview-pipeline-phase1.md)
- [Sprite Preview source map](../admin-tools/sprite-preview/sprite-preview-source-map.md)
- [Sprite Preview Phase 1 report](../admin-tools/sprite-preview/item-sprite-preview-phase1-report.md)
- [Sprite Preview Phase 2 D2P](../admin-tools/sprite-preview/sprite-preview-d2p-extractor-phase2.md)
- [Sprite Preview D2P format notes](../admin-tools/sprite-preview/sprite-preview-d2p-format-notes.md)
- [Sprite Preview Phase 3 curated import](../admin-tools/sprite-preview/sprite-preview-curated-import-phase3.md)
- [Sprite Preview Phase 4 curated workflow](../admin-tools/sprite-preview/sprite-preview-curated-workflow-phase4.md)
- [Sprite Preview Phase 5 appearance audit](../admin-tools/sprite-preview/appearance-identity-audit-phase5.md)
- [Sprite Preview Phase 5 feasibility](../admin-tools/sprite-preview/appearance-preview-feasibility-study.md)
- [Sprite Preview EntityLook map](../admin-tools/sprite-preview/entitylook-relationship-map.md)
- [Sprite Preview Phase 6 appearance curated](../admin-tools/sprite-preview/appearance-preview-curated-workflow-phase6.md)
- [Sprite Preview Phase 7 final QA](../admin-tools/sprite-preview/sprite-preview-final-qa-phase7.md)

## Macro Items Final (effects catalog parity)

Goal: **100% functional parity** with `Rollback.Web` item effects editing before Spells.

| Phase | Status | Doc |
| --- | --- | --- |
| 7D.1 Item Effects Catalog Audit | `DONE` | [items-effects-catalog-audit-phase7d1.md](../admin-tools/items-builder/items-final/items-effects-catalog-audit-phase7d1.md) |
| 7D.2 Item Effects Catalog API | `PENDING` | [items-final-macro-plan.md](../admin-tools/items-builder/items-final/items-final-macro-plan.md) |
| 7D.3 Item Effects Editor UX | `DONE` | [items-effects-editor-ui-phase7d3.md](../admin-tools/items-builder/items-final/items-effects-editor-ui-phase7d3.md) |
| 7D.4 Templates y presets | `PENDING` | same |
| 7D.5 QA end-to-end | `DONE` / browser `PARTIAL` | [items-builder-final-e2e-qa-phase7d5.md](../admin-tools/items-builder/items-final/items-builder-final-e2e-qa-phase7d5.md) |

Branch (7D.1): `feature/items-final-effects-catalog-audit-7d1`

## Immediate next branch

The next intended branch order is:

1. Abrir **PR único** Macro Items Final desde `feature/items-final-effects-catalog-audit-7d1`
2. Browser QA pendiente: ver [items-builder-final-e2e-qa-phase7d5.md](../admin-tools/items-builder/items-final/items-builder-final-e2e-qa-phase7d5.md)
3. **Macro 4 Phase 6B** — item skin catalog by category + stat icons (`DONE`)
4. **Macro 4 Phase 6C** — item preview extract by category + Angular gallery (`DONE` — 500 PNG, dofus 10/10)
5. **Macro 4 Phase 6D** — category expansion 1916 PNG (`DONE`)
5b. **Macro Items Final Plus** — preview BY_CATEGORY + sets read UI + stat icons (`DONE`, browser QA pending)
6. **Macro 4 Phase 6A** — controlled publish to real client (`READY_FOR_OPERATOR`)
7. **Macro 4 Phase 5** — sandbox + UX (`DONE`)
8. **Macro 4 Spells** — solo tras merge PR + aprobación explícita

Macro 4 Phase 3A: **`DONE`** — D2O Item classes + clone staging.  
Macro 4 Phase 3B (`feature/client-item-publication-d2i-writer-phase3b`): **`DONE`** — `D2iFile` writer, append textos ES/EN.  
Macro 4 Phase 3C (`feature/client-item-publication-staging-package-phase3c`): **`DONE`** — paquete `publication-package-phase3c/12617/`, validador CLI, API/Angular staging status.  
Macro 4 Phase 4 (`feature/client-publication-controlled-patch-phase4`): **`DONE`** — backup/recovery scripts, publish lane, `GET publication/backup-status`, `/admin/publication`.  
Macro 4 Phase 5 (`feature/client-publication-controlled-patch-phase5`): **`DONE`** — sandbox patch, UX stats, VPS bash guide.  
Macro 4 Phase 6 (`feature/client-publication-controlled-publish-phase6`): **`READY_FOR_OPERATOR`** — `apply-package-to-real-client`, `validate-real-client`.  
Macro 4 Phase 6B (`feature/item-skin-catalog-by-category-phase6b`): **`DONE`** — catálogo por categoría, export curado dofus dry-run, galería HTML, PyDofus audit, iconos stats.

Macro 4 Phase 6C (`feature/item-preview-massive-extraction-phase6c`): **`DONE`** — extracción D2P masiva (500 PNG), copia Angular `by-category/`, selector con catálogo por categoría, manifest Phase 6C. Browser QA pendiente operador.

Macro 4 Phase 6D (`feature/item-preview-category-expansion-phase6d`): **`DONE`** — expansión incremental (+1416 PNG), **1916** total en Angular, manifest `categoryStats`, selector con contadores y búsqueda AND.

Macro Items Final Plus (`feature/items-preview-sets-polish-final`): **`DONE`** (browser QA pending) — resolver preview `BY_CATEGORY`, UI `/admin/item-sets` + bonos por piezas, fix assets `src/assets/icons`, docs en [items-preview-reconciliation-report.md](../admin-tools/items-builder/items-preview-reconciliation-report.md), [items-stat-icons-fix-report.md](../admin-tools/items-builder/items-stat-icons-fix-report.md), [sets-builder-preview-and-bonuses.md](../admin-tools/sets-builder/sets-builder-preview-and-bonuses.md).

Macro 3 is **COMPLETE** (Phases 1–7). EntityLook renderer remains deferred and is not required for the Items Builder MVP.

## Macro Combat Sanitization (`feature/combat-sanitization-phase1-audit`)

| Fase | Estado | Entregable |
| --- | --- | --- |
| Phase 1 — Auditoría comparativa | **`DONE`** | `docs/combat-sanitization/*.md` |
| Phase 2 — Combat Health Lab | **`SCAFFOLDING`** | `Infrastructure/artifacts/combat-health/` scripts |
| Phase 3 — Turn Transition Fix | **PENDING** | Requiere logs + evidencia |
| Phase 4 — Spell Cast Telemetry | **PENDING** | `spell-casts-*.log` |
| Phase 5 — Summons / Boss | **PENDING** | Tras Phase 4 |

Referencia corregida: `RollBlackServer/2.0.0/Rollback` (`ReadyChecker`, `FightTelemetry`, `CombatTelemetryAnalyzer`).

**No mezclar** con Admin items/spells. Flujo: lab local → PR → VPS beta → logs → analizador.

## Mandatory handoff

Canonical handoff path:

`docs/handoffs/AGENT_HANDOFF.md`

Minimum required contents:

- repo
- branch
- current phase
- last commit
- files touched
- validation
- pending work
- prohibitions
- exact next action
