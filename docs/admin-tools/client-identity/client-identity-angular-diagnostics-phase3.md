# Macro 2 / Phase 3 — Angular Client Identity Diagnostics

Date: `2026-06-03`  
Branch: `feature/client-identity-angular-diagnostics-phase3`  
Status: **DONE**

## Objetivo

Integrar en Angular la auditoría read-only expuesta en Phase 2, sin tocar cliente, sin writes DB y sin publish.

## Entregables

| Área | Archivo / ruta | Notas |
| --- | --- | --- |
| Data access | `client-identity.api.ts`, `client-identity.models.ts`, `client-identity.status.ts` | Consume `/client-identity/items/*` |
| Facade | `items.facade.ts` | `getClientIdentityDiagnostic`, `checkClientIdentity` |
| UI | `client-identity-diagnostic-card.component.*` | Card principal |
| UI | `client-identity-warning-badge.component.ts` | Badge reutilizable por código de estado |
| UI | `client-identity-recommended-action.component.*` | Acción recomendada |
| UI | `client-identity-batch-check-panel.component.*` | Muestra QA `7754,12616,12617,39` |
| Integración | `item-detail-page` | Card + batch + traceId en errores |
| Integración | `item-publication-status-page` | Misma card (sin link circular) |

## Rutas operador

```txt
/admin/items/7754
/admin/items/12616
/admin/items/12617
/admin/items/7754/publication-status
/admin/items/12617/publication-status
```

## UX / estados visuales

| Código | Presentación |
| --- | --- |
| `SAFE_EXISTING_TEMPLATE`, `CLIENT_KNOWN`, `ICON_PREVIEW_FOUND` | Verde |
| `CLIENT_UNKNOWN`, `NEEDS_CLIENT_PATCH`, `APPEARANCE_UNKNOWN` | Amarillo / warning |
| `I18N_MISSING_ES`, `I18N_MISSING_EN` | Info |
| `CLIENT_DATA_UNAVAILABLE`, `ERROR` | Rojo |

Texto en español. Errores vía `app-api-problem-panel` con `traceId`.

## Validación (2026-06-03)

```txt
dotnet build Sunshine.sln /nr:false -> OK (4 CA1416 conocidos)
npm run build (Admin Angular) -> OK (budget +6.79 kB CAN_DEFER)
```

Browser QA: pendiente confirmación visual del operador en las rutas listadas.

## Casos esperados

| ItemId | Diagnóstico esperado |
| ---: | --- |
| 7754 | Verde — cliente conoce template |
| 12616 | Warning — `CLIENT_UNKNOWN`, patch, appearance unknown |
| 12617 | Warning — `CLIENT_UNKNOWN`, needs patch |
| 39 | Verde — cliente conoce + preview |

## Siguiente fase

**Macro 2 / Phase 4 — Batch/report diagnostics** (NEXT): reportes exportables y vistas batch ampliadas sin escaneo masivo.

## Prohibiciones respetadas

- No cliente write
- No DB write
- No publish workflow
- No 44k scan
- No Macro 3 (Sprite Preview)
