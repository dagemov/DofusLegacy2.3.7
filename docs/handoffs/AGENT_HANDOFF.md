# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 4 / Phase 5 — Controlled patch sandbox + item UX polish

| Campo | Valor |
| --- | --- |
| Rama | `feature/client-publication-controlled-patch-phase5` |
| Base | `feature/client-publication-controlled-patch-phase4` |
| Estado | **`DONE`** |
| Docs | [client-publication-phase5-controlled-sandbox.md](../admin-tools/client-publication/client-publication-phase5-controlled-sandbox.md), [items-creation-ux-polish-phase5.md](../admin-tools/items-builder/items-final/items-creation-ux-polish-phase5.md) |

### Parte A — Sandbox

- CLI: `--mode apply-package-to-sandbox`, `--mode validate-sandbox-client`
- Código: `Infrastructure/scripts/ClientItemPublicationPipeline/Package/ClientPatchSandboxPublisher.cs`
- Sandbox: `Infrastructure/staging-client/client-patch-sandbox/12617/`
- Paquete: `publication-package-phase3c/12617`
- Validación ejecutada: `VALID_SANDBOX_CLIENT`, item 12617, i18n ES/EN, IconId 23012, **Client2.3.7 real intacto**

### Parte B — UX items

- Flujo 5 secciones en `item-write-page` (Identidad → Visual → Características → Reglas → Publicación)
- Stats frecuentes + búsqueda humana: `item-effect-stat-quick-picks.ts`, `item-effects-editor`
- Modal save: `item-save-error-modal`
- Preset UX ejemplo: `dofus-hielos-ux` (112/176/124/115) — sin publish

### Parte C — Docs VPS

- [vps-publication-operations-guide.md](../admin-tools/client-publication/vps-publication-operations-guide.md) — sección bash backup/restart (`CONFIRM_BACKUP`, `CONFIRM_RESTART`)

### Gate pre-Phase 6 (2026-06-04)

| Target | Resultado |
| --- | --- |
| `RollblackLegacy.Admin.Api.csproj` | OK (API detenida antes del build) |
| `npm run build` (Admin Angular) | OK |
| `Sunshine.sln` | OK |
| Commits Phase 5 | 3 commits en rama `feature/client-publication-controlled-patch-phase5` |
| Browser QA | `PENDING_OPERATOR_BROWSER_QA` — `/admin/items/new`, `12616/edit`, `12617/publication-status`, `/admin/publication` |

## Macro 4 / Phase 6 — Controlled publish to real client

| Campo | Valor |
| --- | --- |
| Estado | **`NEXT`** (no iniciar hasta operador apruebe) |
| Alcance | Publicación controlada al cliente real con backup + confirmación explícita del operador |
| Prerrequisitos | Backup lane `READY`, sandbox Phase 5 validado, browser QA Phase 5 |

**No iniciar implementación Phase 6 en agente hasta nueva orden explícita.**

## Macro 4 / Phase 4 — referencia

Rama `feature/client-publication-controlled-patch-phase4` — backup/recovery, publish lane, `/admin/publication`. `DONE`.

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-publication-controlled-patch-phase5
```
