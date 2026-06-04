# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## QA Dofus de los Hielos (producción controlada)

| Campo | Valor |
| --- | --- |
| Estado | `BLOCKED_CLIENT_TEMPLATE_MISSING` |
| Doc | [dofus-hielos-production-qa.md](../admin-tools/items-builder/items-final/dofus-hielos-production-qa.md) |
| DB QA | VPS `174.138.35.107` / `sunshine` / `isRemote=true` |
| Item creado | **No** — sin template cliente *Dofus de los Hielos* en `Items.d2o` |
| Backup / reinicio VPS | **No ejecutado** (sin cambios DB) |

Siguiente paso: aprobación explícita para client patch; luego repetir QA con template `ItemId` conocido.

## Macro 4 — Client Item Publication (Phase 1)

| Campo | Valor |
| --- | --- |
| Rama | `feature/client-item-publication-pipeline-phase1` |
| Estado | Phase 1 `DONE` (dry-run manifest) |
| Docs | [client-publication/README.md](../admin-tools/client-publication/README.md) |
| API | `GET /api/admin/v1/items/{id}/publication-manifest` |
| CLI | `Infrastructure/scripts/ClientItemPublicationPipeline` |

Phase 2 Writer Research: **DONE** — D2OWriter existe pero **no operativo** para `Items.d2o` (falta clase C# `Item`). D2I writer: **no existe**. Ver [client-writer-capability-audit.md](../admin-tools/client-publication/client-writer-capability-audit.md).

Phase 3 (staging publisher): **NO iniciar** sin aprobación — opción A: generar `Item.cs` / opción B: writer genérico Admin.

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-item-publication-pipeline-phase1
```

## Macro Items Final — DONE (API)

| Phase | Commit |
| --- | --- |
| 7D.1 | `44632b8` docs: audit final item effects catalog parity |
| 7D.2 | `10538e8` feat: add full item effects catalog api |
| 7D.3 | `5a2fe50` feat: add item effects editor parity ui |
| 7D.4 | `d00ecaa` feat: add item stat templates |
| 7D.5 | (pending) docs: record final items builder e2e qa |

## 7D.5 API smoke (12616)

```txt
PUT preset Dofus Tester QA → 13 effects PASS
GET reload → 13 effects persist
7754 detail → IconId 23012, client Dofus Ocre
7754 publication → PUBLISHED
12616 publication → NEEDS_CLIENT_PATCH
```

Browser: PENDING_OPERATOR — checklist en `items-builder-final-e2e-qa-phase7d5.md`

## PR

Abrir **un solo PR** con los 6 commits de la rama. Título sugerido: `Macro Items Final — Items Builder effects parity`

## Siguiente macro

```txt
Macro 4 Phase 2 (client writers staging) — NO sin aprobación explícita
Macro Spells Builder — sigue diferido tras Items Final PR
```

## Builds

```txt
dotnet build Admin.Api — PASS (sin API en ejecución)
npm run build — PASS
Sunshine.sln — PASS si Admin.Api detenido
```
