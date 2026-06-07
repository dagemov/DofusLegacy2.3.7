# Agent Handoff - Admin Tools Migration

Generated: `2026-06-06`

## Macro 5 / Phase 7 - Angular Detail

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `d2e5cf1 feat: add angular spell catalog` |
| Estado | **`DONE`** |

### Entregables

- Reemplazo del placeholder `admin/spells/:spellId` por vista real read-only
- `SpellsApi` y `SpellsFacade` ampliados con `getSpell`, `getSpellLevels` y `getSpellLevelEffects`
- Contratos TypeScript de detalle, niveles y effects en `spells.models.ts`
- Selector visual de niveles con detalle activo read-only
- Carga diferida de `effects` y `criticalEffects` por nivel
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase7-angular-detail.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |

### Notas

- La pantalla usa `GET /api/admin/v1/spells/{spellId}` como contrato principal para cabecera y referencia.
- Para mostrar `castInDiagonal`, `needTakenCell` e `initialCooldown` se apoya tambien en `GET /api/admin/v1/spells/{spellId}/levels`.
- `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects` se consume solo bajo demanda al abrir un nivel.
- Toda la fase se mantiene read-only: sin editor de niveles, sin editor de effects y sin write de effects.
- Si la referencia no existe en el entorno, la pantalla mantiene un estado claro y no rompe la vista.

### Siguiente

- Macro 5 / Phase 8 - Spell Level Editor
- Alcance esperado: habilitar escritura de campos de nivel ya soportados por `PATCH`, manteniendo los effects fuera de la fase

## Macro 5 / Phase 6 - Angular Catalog

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `bb6c345 feat: add spell effects api` |
| Estado | **`DONE`** |

### Entregables

- Ruta Angular `admin/spells`
- Página `spells-page.component` con catálogo read-only
- `SpellsApi`, `SpellsFacade`, `spells.models.ts`, `spells.queries.ts`
- Integración del link `Spells` en el shell principal
- Ruta placeholder `admin/spells/:spellId` para preparar Phase 7 sin abrir detalle real
- Documentación `docs/admin-tools/spell-builder/spell-builder-phase6-angular-catalog.md`

### Validación

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |

### Notas

- El Angular Admin usa standalone components y `loadComponent`, sin módulos por feature.
- La configuración API sigue en `environment.adminApiBaseUrl = /api/admin/v1`.
- Phase 6 no tocó backend, base de datos, cliente ni publicación.
- La navegación a detalle quedó habilitada solo como placeholder; el detalle real sigue fuera de esta fase.
- Los filtros implementados son `search`, `spellId`, `breedId`, `typeId`, paginación y `pageSize`.

### Siguiente

- Macro 5 / Phase 7 - Angular Detail
- Alcance esperado: detalle real del spell consumiendo `GET /api/admin/v1/spells/{spellId}`, resumen de niveles y puente visual hacia Phase 4/5

## Macro 5 / Phase 5 - Spell Effects API

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `f81d492 feat: add spell levels api` |
| Estado | **`DONE`** |

### Entregables

- `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects`
- Contratos `SpellLevelEffectsDto`, `SpellEffectCollectionDto`, `SpellEffectRowDto`
- Read models dedicados para effects de spells
- Decoder backend para:
  - payload hex runtime Sunshine
  - payload binario legacy compatible
  - payload de referencia desde `spellsReferences`
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase5-effects-api.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |
| `npm run build` | `NOT_RUN` (Angular no fue tocado) |

### Notas

- La API separa `effects` y `criticalEffects`, y dentro de cada bucket separa `runtimeRows` y `referenceRows`.
- En schema actual se respeta la prioridad runtime: payload serializado primero y fallback binario despues.
- `ReferenceSpellCatalogReader` ahora conserva `EffectsPayload` y `CriticalEffectsPayload` cuando `Documents/spellsReferences` esta disponible.
- No se abrio write API de effects en esta fase.

### Siguiente

- Macro 5 / siguiente fase: decision controlada de write API de effects antes del editor Angular
- Alcance esperado: preservacion segura de payloads, estrategia frente a fallback binario y posible catalogo de opciones para UI

## Macro 5 / Phase 4 - Spell Levels API

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `9031339 feat: add spell detail api` |
| Estado | **`DONE`** |

### Entregables

- `GET /api/admin/v1/spells/{spellId}/levels`
- `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
- `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
- Contratos `SpellLevelDetailDto`, `SpellLevelUpdateRequest`, `SpellLevelUpdateResultDto`
- Read models y write draft/result para spell levels
- Estrategia dual de write:
  - `legacy-level-id-update`
  - `current-runtime-row-rewrite`
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase4-levels-api.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |
| `npm run build` | `NOT_RUN` (Angular no fue tocado) |

### Notas

- El schema actual de Sunshine sigue sin `Id` por nivel en `spells_levels`; Phase 4 escribe preservando el orden runtime consumido por `SpellsLoader` y `SpellManager`.
- En schema legacy la escritura sigue el orden de `SpellLevelsCSV` y actualiza por `spells_levels.Id`.
- Effects normales, critical effects y editor Angular siguen fuera de esta fase.

### Siguiente

- Macro 5 / Phase 5: Spell Effects API
- Alcance esperado: lectura detallada de rows de effects, separacion normal/critico y decision posterior del write API de effects

## Macro 5 / Phase 3 - Spell Detail API

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base | puntero preservado en `9031339` |
| Estado | **`DONE`** |

### Entregables

- Endpoint `GET /api/admin/v1/spells/{spellId}`
- Contratos `SpellDetailDto`, `SpellReferenceMetadataDto`, `SpellLevelSummaryDto`
- Read models de detalle y niveles read-only
- Repositorio/reader con compatibilidad para `spells` actual y `spells_templates` legacy
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase3-detail-api.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `FAILED_EXTERNAL_LOCK` |
| Causa registrada | DLLs del Admin API bloqueadas por Visual Studio (`RollblackLegacy.Admin.Api`) |
| Estado de compilacion del cambio | `COMPILED_BEFORE_COPY_LOCK` |

### Siguiente

- Macro 5 / Phase 4: Spell Levels API
- Alcance esperado: contratos de nivel mas ricos, orden/consistencia de niveles y lectura dedicada sin write API

## Macro 5 / Preparacion segura antes de Phase 4

| Campo | Valor |
| --- | --- |
| Rama activa | `feature/spell-builder-api-migration` |
| HEAD | `eb76a82 docs: prepare spell builder branch for phase4` |
| Worktree | `CLEAN` |
| Stash de resguardo | `stash@{Sat Jun 6 08:54:24 2026}: On feature/items-sets-production-acceptance-test: wip: preserve items sets work before spell builder phase4` |
| Estado | **`CONSUMED_BY_PHASE4`** |

### Notas

- Los cambios locales de Items/Sets y auxiliares quedaron preservados en stash antes del cambio de rama.
- No se toco codigo funcional de API, Angular, base de datos, cliente ni Items/Sets durante esta preparacion.
- Esta preparacion ya fue consumida por `Phase 4 - Spell Levels API`.

## Macro Items Final Plus - Preview + Sets + Stat icons

| Campo | Valor |
| --- | --- |
| Rama | `feature/items-preview-sets-polish-final` |
| Base | `feature/item-preview-category-expansion-phase6d` |
| Estado | **`DONE`** (browser QA pendiente operador) |

### Entregables

1. **Preview reconciliation** — `FileSystemItemPreviewStateResolver` + `ItemPreviewCategoryIndex` + fallback `BY_CATEGORY`; `typeId` en `Resolve()` para lista/detalle/sets.
2. **Sets read UI** — `GET /api/admin/v1/item-sets`, `GET /api/admin/v1/item-sets/{setId}`; Angular `/admin/item-sets`, `/admin/item-sets/:setId`; bonos por piezas con labels de `item-effects/options`.
3. **Stat icons** — `angular.json` publica `src/assets`; quick-picks con PNG reales y fallback emoji.

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build` Application | OK |
| `dotnet build` Admin.Api | `FAILED_EXTERNAL_LOCK` si VS ejecuta `RollblackLegacy.Admin.Api` |
| `npm run build` | OK (warning budget +1.13 kB) |
| Browser QA | `PENDING_OPERATOR` — ver rutas abajo |

### Browser QA (operador)

```txt
/admin/items/new
/admin/items/12616/edit
/admin/items/icon-selector
/admin/item-sets
/admin/item-sets/:setId
```

Validar: iconos stats visibles, sin imagenes rotas, previews BY_CATEGORY en lista/detalle/sets, bonos legibles por piezas.

### Commits esperados (esta sesion)

```txt
fix: reconcile item previews from category catalog
feat: add item set previews and bonuses
fix: load item stat icons correctly
docs: record items preview and sets polish
```

### Docs

- [items-preview-reconciliation-report.md](../admin-tools/items-builder/items-preview-reconciliation-report.md)
- [items-stat-icons-fix-report.md](../admin-tools/items-builder/items-stat-icons-fix-report.md)
- [sets-builder-preview-and-bonuses.md](../admin-tools/sets-builder/sets-builder-preview-and-bonuses.md)

### Siguiente

- PR desde `feature/items-preview-sets-polish-final` hacia base de migracion acordada
- Browser QA operador
- Spell Builder ya fue aislado en `feature/spell-builder-api-migration`; Phase 4 puede iniciarse desde esa rama cuando corresponda.

### Prohibiciones

- No tocar cliente real, VPS, publicacion, armas, scan 44k, worktrees externos, temporal-artifacts en git

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/spell-builder-api-migration
```

## Macro 4 / Phase 6D (referencia)

**`DONE`** — 1916 PNG `by-category/`, manifest `categoryStats`. Ver handoff previo en historial git de este archivo.
