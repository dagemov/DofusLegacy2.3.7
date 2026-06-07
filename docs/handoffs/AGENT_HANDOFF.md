# Agent Handoff - Admin Tools Migration

Generated: `2026-06-07`

## Spell Builder Production Parity / Fase 2 - Effects Write Closure Spec

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base documental | `f729e8f docs: audit spell builder production parity risks` |
| Estado | **`DONE`** |
| Tipo de cambio | `solo documentacion` |

### Entregables

- Especificacion principal `docs/admin-tools/spell-builder/spell-builder-effects-write-closure-spec.md`
- Plan de pruebas `docs/admin-tools/spell-builder/spell-builder-effects-roundtrip-test-plan.md`
- Safety gates de editor `docs/admin-tools/spell-builder/spell-builder-effects-editor-safety-gates.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `LOCKED_BY_VS` |

### Notas

- La fase define la primera cobertura segura como `Dice-only`.
- `effects` y `criticalEffects` deben seguir separados en toda futura API de write.
- Glifos, trampas e invocaciones quedan bloqueados para la primera implementacion real.
- La fase propone `preview/validate` antes de cualquier `PATCH` real.
- No se aprobo migracion automatica entre `current-serialized-hex` y `legacy-binary`.
- `StatesRequired` y `StatesForbidden` quedan fuera del write de effects.
- Todo texto visible final de Angular Spell Builder debe quedar 100% en espanol.
- `npm run build` paso con el warning conocido de budget inicial excedido por `1.51 kB`.
- `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` no expone un error funcional de esta fase porque solo hubo documentacion; el fallo fue de copiado final por lock externo de Visual Studio.
- DLLs bloqueadas durante `dotnet build`:
  - `RollblackLegacy.Admin.Contracts.dll`
  - `RollblackLegacy.Admin.Infrastructure.dll`
  - `RollblackLegacy.Admin.Application.dll`
- Proceso reportado por MSBuild: `Microsoft Visual Studio (14588), RollblackLegacy.Admin.Api (62476)`.
- Durante el cierre reaparecieron cambios ajenos del pipeline de items; deben quedar fuera del commit de esta fase.

### Siguiente

- `Fase 3 - Backend Preview Contract for Dice-Only Spell Effects`

## Spell Builder Production Parity / Fase 1 - Risk Closure Audit

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `e65a5e4 docs: finalize spell builder qa` |
| Estado | **`DONE`** |
| Tipo de cambio | `solo documentacion` |

### Entregables

- Documento de auditoria principal `docs/admin-tools/spell-builder/spell-builder-production-risk-audit.md`
- Documento de estrategia de write `docs/admin-tools/spell-builder/spell-builder-effects-write-strategy.md`
- Documento de estrategia de publicacion cliente `docs/admin-tools/spell-builder/spell-builder-client-publication-strategy.md`
- Documento de mapa de effects especiales `docs/admin-tools/spell-builder/spell-builder-special-effects-map.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `LOCKED_BY_VS` |

### Notas

- La fase se ejecuto sin tocar Angular funcional, API funcional, base de datos, cliente ni publicacion real.
- Legacy si tenia flujo productivo de `guardar + publicar` para spells; el stack actual todavia no.
- El stack actual mantiene catalogo, detalle, edicion parcial de levels y auditoria read-only de `effects`.
- No existe write seguro de `effects` ni `criticalEffects` en API actual.
- No existe pipeline actual de publicacion cliente de spells equivalente al pipeline moderno de items.
- Se detectaron multiples textos visibles en ingles o spanglish en Angular Spell Builder; la UI final debe quedar 100% en espanol.
- `dotnet build` compilo los proyectos relevantes pero fallo al copiar DLLs del Admin API porque Visual Studio mantiene lock sobre:
  - `RollblackLegacy.Admin.Contracts.dll`
  - `RollblackLegacy.Admin.Application.dll`
  - `RollblackLegacy.Admin.Infrastructure.dll`
- Lock verificado sobre `RollblackLegacy.Admin.Api` por `Microsoft Visual Studio (14588), RollblackLegacy.Admin.Api (62476)`.
- Durante el cierre reaparecieron cambios ajenos del pipeline de items en el worktree; deben quedar fuera del commit de esta fase.

### Siguiente

- `Fase 2 - Effects Write Closure Spec`
- Abrir macro aparte si hace falta corregir motor de combate para handlers especiales

## Macro 5 / Phase 10 - QA Final

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `1535c29 feat: add spell effects read-only editor guard` |
| Estado | **`DONE`** |
| Decision QA | **`PARTIAL`** |

### Entregables

- Documento de cierre `docs/admin-tools/spell-builder/spell-builder-phase10-qa-final.md`
- Documento de limites `docs/admin-tools/spell-builder/spell-builder-known-limitations.md`
- Documento de siguientes pasos `docs/admin-tools/spell-builder/spell-builder-next-steps.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |

### Notas

- QA funcional ejecutado sobre API local y Angular local.
- Se verifico catalogo, detalle, levels y auditoria read-only de effects.
- El editor de levels se verifico en apertura y contrato expuesto, sin ejecutar `PATCH` real por regla de no tocar base de datos en esta fase.
- No se encontro ningun spell con `referenceAvailable = true` al escanear el entorno actual, por lo que solo se valido el fallback con referencia nula.
- La ruta directa `/admin/spells/:spellId` funciona; el click SPA desde catalogo no pudo confirmarse con el runtime automatizado del navegador integrado y queda como limite de QA, no como bug confirmado.
- La macro cierra util para operacion admin incremental, pero no como paridad total 1:1 con legacy.
- Angular mantiene el warning conocido de budget inicial excedido por `1.51 kB`.
- El build .NET mantiene warnings previos `NETSDK1057`, `CA1416` y `CS0169`, sin errores.

### Siguiente

- Definir estrategia segura de write para effects antes de reabrir paridad total
- Repetir QA cuando exista dataset con referencia sana disponible

## Macro 5 / Phase 9 - Spell Effects Editor Guard

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `35b3bfa feat: add spell level editor` |
| Estado | **`DONE`** |

### Entregables

- Guardia Angular explicita de `edicion bloqueada` para `effects` y `criticalEffects`
- Ajuste de UX para tratar la seccion como auditoria read-only y no como editor incompleto
- Tablas runtime/reference ampliadas con metadata util por fila
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase9-effects-editor.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |

### Notas

- Se confirmo primero el cierre de `Phase 8` en `35b3bfa feat: add spell level editor`.
- El stack actual solo expone `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects`.
- No existe `PATCH`, `PUT` ni `POST` seguro para escribir `effects` o `criticalEffects`.
- Legacy si editaba `BinaryEffects` y `BinaryCriticalEffects`, pero ese write path no esta portado.
- La fase cierra deliberadamente en read-only para no inventar un endpoint ni una reserializacion riesgosa.
- Angular mantiene el warning conocido de budget inicial excedido por `1.51 kB`.
- El build .NET mantiene warnings previos `NETSDK1057`, `CA1416` y `CS0169` sin errores nuevos del cambio.

### Siguiente

- Macro 5 / siguiente paso: definir estrategia segura de write de effects antes de abrir un editor real
- Alcance esperado: contrato write, preservacion de payload hex/binario y reglas sobre rows no soportadas

## Macro 5 / Phase 8 - Spell Level Editor

| Campo | Valor |
| --- | --- |
| Rama | `feature/spell-builder-api-migration` |
| Base funcional | `f2d0a96 feat: add angular spell detail` |
| Estado | **`DONE`** |

### Entregables

- Editor Angular de levels integrado en `admin/spells/:spellId`
- `PATCH` Angular para `spells/{spellId}/levels/{levelNumber}`
- Request/response TypeScript para update de nivel
- Validacion frontend minima alineada con backend
- Respeto del bloqueo legacy de `castInDiagonal`, `needTakenCell` e `initialCooldown`
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase8-level-editor.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |

### Notas

- Se confirmo en codigo de Phase 4 que el write real usa `PATCH`, no `PUT`.
- El editor manda solo campos modificados y deja `states`, `effects` y `criticalEffects` fuera del write.
- `Range` no existe como field separado en el contrato; el editor usa `minRange` y `maxRange`.
- Si el runtime activo cae en schema legacy, `castInDiagonal`, `needTakenCell` e `initialCooldown` quedan visibles pero read-only.
- Effects y critical effects siguen en lectura bajo demanda dentro del mismo detalle.

### Siguiente

- Macro 5 / Phase 9 - Spell Effects Editor
- Alcance esperado: write controlado de effects/criticalEffects y estrategia segura de preservacion de payload

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
- Pagina `spells-page.component` con catalogo read-only
- `SpellsApi`, `SpellsFacade`, `spells.models.ts`, `spells.queries.ts`
- Integracion del link `Spells` en el shell principal
- Ruta placeholder `admin/spells/:spellId` para preparar Phase 7 sin abrir detalle real
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase6-angular-catalog.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `npm run build` | `OK` |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `OK` |

### Notas

- El Angular Admin usa standalone components y `loadComponent`, sin modulos por feature.
- La configuracion API sigue en `environment.adminApiBaseUrl = /api/admin/v1`.
- Phase 6 no toco backend, base de datos, cliente ni publicacion.
- La navegacion a detalle quedo habilitada solo como placeholder; el detalle real sigue fuera de esta fase.
- Los filtros implementados son `search`, `spellId`, `breedId`, `typeId`, paginacion y `pageSize`.

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

1. **Preview reconciliation** - `FileSystemItemPreviewStateResolver` + `ItemPreviewCategoryIndex` + fallback `BY_CATEGORY`; `typeId` en `Resolve()` para lista/detalle/sets.
2. **Sets read UI** - `GET /api/admin/v1/item-sets`, `GET /api/admin/v1/item-sets/{setId}`; Angular `/admin/item-sets`, `/admin/item-sets/:setId`; bonos por piezas con labels de `item-effects/options`.
3. **Stat icons** - `angular.json` publica `src/assets`; quick-picks con PNG reales y fallback emoji.

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build` Application | OK |
| `dotnet build` Admin.Api | `FAILED_EXTERNAL_LOCK` si VS ejecuta `RollblackLegacy.Admin.Api` |
| `npm run build` | OK (warning budget +1.13 kB) |
| Browser QA | `PENDING_OPERATOR` - ver rutas abajo |

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

**`DONE`** - 1916 PNG `by-category/`, manifest `categoryStats`. Ver handoff previo en historial git de este archivo.
