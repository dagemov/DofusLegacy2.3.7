# QA producción controlada — Dofus de los Hielos

Date: `2026-06-04`  
Branch: `feature/items-final-effects-catalog-audit-7d1`  
Repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`  
**Estado final:** `BLOCKED_CLIENT_TEMPLATE_MISSING`

## Resumen ejecutivo

No se creó ni editó ningún item en VPS. El cliente **2.3.7** del repo **no** publica un template de item llamado *Dofus de los Hielos* / *Dofus des Glaces* en `Items.d2o` ni como entrada corta en `i18n_es.d2i`. El nombre solo aparece en textos de lore (capítulos de gesta), no como `nameId` de item equipable.

Crear un `ItemId` nuevo o renombrar un Dofus conocido sin patch de cliente produciría un item **invisible** o con nombre incorrecto — prohibido por el runbook de esta QA.

## Criterios de cierre (checklist)

| # | Criterio | Resultado |
| --- | --- | --- |
| 1 | Angular `/admin/items/new` muestra apartado características/effects | **PARCIAL** — sección *Effects* visible; editor activo solo tras guardar (ver Fase 2) |
| 2 | Admin API → VPS/DockerLegacy | **PASS** |
| 3 | Template cliente conocido para Dofus de los Hielos | **FAIL** → bloqueo |
| 4 | Stats requeridos aplicados | **NO EJECUTADO** (bloqueado) |
| 5 | `PublicationStatus` CLIENT_KNOWN / PUBLISHED | **NO EJECUTADO** |
| 6 | Backup antes de reinicio | **NO EJECUTADO** (sin cambios DB) |
| 7 | Reinicio VPS controlado | **NO EJECUTADO** |
| 8 | Servidor/login intacto | **N/A** (sin reinicio) |
| 9 | Documentación | **PASS** (este documento) |

## Fase 1 — Entorno DB (Admin API)

Comando:

```powershell
cd "C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Angular-tools\Admin\RollblackLegacy.Admin.Api"
dotnet run
# Base URL usada en esta sesión: http://127.0.0.1:5250
```

`GET /api/admin/v1/health/db` (2026-06-04):

| Campo | Valor |
| --- | --- |
| status | ok |
| host | `174.138.35.107` |
| port | `3306` |
| database | `sunshine` |
| user | `sunshine_remote` |
| isRemote | `true` |

**Conclusión:** apunta a VPS/DockerLegacy esperado, no a MySQL local.

## Fase 2 — Angular Items Builder

| Comando / URL | Notas |
| --- | --- |
| `npm run build` en `RollblackLegacy.Admin.Angular` | **PASS** (budget +598 B preexistente) |
| `http://localhost:4200/admin/items/new` | **PENDING_OPERATOR** — no se levantó `npm start` en esta sesión; validación por código + build |

Comportamiento en `/admin/items/new` (fuente `item-write-page.component.html`):

- **Icono:** selector modal + ruta `/admin/items/icon-selector` — presente.
- **Preview:** panel por `IconId` — presente.
- **Effects / características:** bloque `editor-layout__effects` con título *Editor de efectos*; mensaje *Guarda primero el item…* — el componente `app-item-effects-editor` solo se monta en modo **edit** con `sourceItemId` (paridad Blazor 7D.3).

Para validar presets y editor completo, el operador debe abrir un item existente (p. ej. `7754`) o crear+guardar y volver a editar.

## Fase 3 — Template cliente / DB

### Búsqueda VPS (`sunshine.items`)

| Consulta API | Resultado |
| --- | --- |
| `search=Hielos` | 0 filas |
| `search=Glaces` / `glaces` | 7 filas (mascotas/objetos, **ningún** `typeId=23`) |
| `search=Dofus` | 20 filas; Dofus `typeId=23` en VPS: ver tabla abajo |

**Dofus en VPS (typeId=23):**

| ItemId | Nombre (client-identity ES) | clientKnown |
| ---: | --- | --- |
| 694 | Dofus Púrpura | true |
| 737 | Dofus Esmeralda | true |
| 739 | Dofus Turquesa | true |
| 972 | Dofus Zanahowia | true |
| 6980 | Dofus Vulbis | true |
| 7112 | Dofus Salpicado | true |
| 7113 | Dofawa | true |
| 7754 | Dofus Ocre | true |
| 8072 | Dofus Kalipto | true |
| 10907 | [WIP]Dokille | true |
| 12617 | (sin template cliente) | **false** |

Ninguna fila coincide con *Dofus de los Hielos* / *Dofus des Glaces*.

### Cliente local (`Client2.3.7`)

| Fuente | Hallazgo |
| --- | --- |
| `data/common/Items.d2o` | **10** items `typeId=23`; lista idéntica a la tabla VPS salvo `12617` (solo DB) |
| `data/i18n/i18n_es.d2i` | Texto *Dofus de los Hielos* en **lore HTML** (gesta Astrub), no como nombre corto de item |
| `i18n_es` exact match | `Dofus des Glaces`, `Dofus de los Hielos`, `Ice Dofus` → **NOT FOUND** |
| Escáner temporal `Infrastructure/temporal-artifacts/DofusD2oScan` | 0 Dofus con aguja glace/hielo/ice en nombre ES/EN |

**Respuesta template (objetivo del runbook):**

```txt
ItemId:           (no existe para Dofus de los Hielos)
DescriptionId:    —
NameEs:           —
NameEn:           —
TypeId:           23 (Dofus) — no hay fila/template
IconId:           —
AppearanceId:     —
ClientKnown:      false (no hay template)
PublicationStatus: N/A — no crear item
```

### Diagnóstico

Para visibilidad en cliente hace falta un **client patch** futuro (aprobar explícitamente):

1. Entrada en `Items.d2o` con `typeId=23` y `nameId`/`descriptionId` en i18n.
2. Alineación opcional de fila en `sunshine.items` con el mismo `ItemId`.
3. Referencia: [items-builder-client-publication-analysis.md](../items-builder-client-publication-analysis.md) (caso `12617`).

**No usar** IDs retail de otras versiones (p. ej. `7453` en sunshine = *Runa Re Aire*).

## Fase 4–5 — Crear item / stats (no ejecutado)

Bloqueado por Fase 3.

### EffectIds para cuando exista template editable

Validados en VPS vía `GET /api/admin/v1/item-effects/options` (catálogo 7D.2):

| Stat pedido | EffectId | Label API |
| --- | ---: | --- |
| +40 daños | `112` | + Danos |
| +80 prospección | `176` | + Prospeccion |
| +50 sabiduría | `124` | + Sabiduria |
| +10 golpes críticos | `115` | + Golpes criticos |

**Nota:** el id `51` en API aparece como *Lanzar objetivo*; para críticos usar **`115`** (catálogo vivo), no inventar ids.

## Fase 6 — Backup

**NO EJECUTADO** — sin mutación en tablas `items` ni relacionadas.

Cuando haya patch aprobado, usar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\backup-before-restart.ps1
# o con confirmación:
powershell -ExecutionPolicy Bypass -File .\scripts\vps\backup-before-restart.ps1 -ConfirmBackup
```

Documentar ruta del `.sql` generado en esta sección antes de cualquier reinicio.

## Fase 7 — Reinicio VPS

**NO EJECUTADO** — sin cambios que requieran recarga de world/auth.

Referencias:

- [vps-controlled-restart.md](../../../infrastructure/vps-controlled-restart.md)
- [vps-restart-safety-checklist.md](../../../infrastructure/vps-restart-safety-checklist.md)

Reglas respetadas: sin `docker compose down -v`, sin borrar volúmenes, sin tocar cliente.

## Fase 8 — Validación final en juego

**NO APLICA** hasta existir template cliente + fila DB alineada.

Checklist operador (post-patch futuro):

- [ ] Dofus de los Hielos visible en inventario/tooltip
- [ ] Icono correcto
- [ ] Stats: +40 / +80 / +50 / +10 críticos
- [ ] Login y world arriba tras reinicio controlado

## Alternativa descartada

Reutilizar `7754` (Dofus Ocre) u otro Dofus conocido solo para probar el editor **no** cumple el objetivo de nombre *Dofus de los Hielos* visible. Ver [dofus-tester-vendor-kamas-plan.md](../dofus-tester-vendor-kamas-plan.md).

## Pendientes

1. Aprobación de producto para **client patch** (Items.d2o + i18n + opcional fila sunshine).
2. Operador: `npm start` + smoke visual `/admin/items/new` y `/admin/items/7754/edit` (presets 7D.4).
3. Tras patch: repetir esta QA (crear/editar template conocido, backup, reinicio, validación in-game).

## Evidencia técnica

- Admin API health: `http://127.0.0.1:5250/api/admin/v1/health/db`
- Escáner D2O (gitignored): `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/temporal-artifacts/DofusD2oScan/`
