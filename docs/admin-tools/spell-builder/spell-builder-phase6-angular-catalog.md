# Macro 5 / Phase 6 - Angular Catalog

Fecha: `2026-06-06`
Rama activa verificada: `feature/spell-builder-api-migration`
Estado inicial del worktree: `CLEAN`
Base funcional: `bb6c345 feat: add spell effects api`
Estado: `DONE`

## Objetivo cubierto

Portar al Angular Admin actual la pantalla inicial de catálogo de Spell Builder legacy, consumiendo la API existente:

- `GET /api/admin/v1/spells`

Sin mezclar todavía:

- detalle real
- editor de niveles
- editor de effects
- write de effects

## Verificación previa ejecutada

Comandos ejecutados antes de implementar:

```powershell
git branch --show-current
git status --short
git log --oneline -5
```

Resultado:

- rama correcta: `feature/spell-builder-api-migration`
- worktree limpio
- historial alineado con Macro 5 hasta `Phase 5`

## Estructura Angular encontrada

Inspección real de `Angular-tools/Admin/RollblackLegacy.Admin.Angular`:

- routing central en `src/app/app.routes.ts`
  - usa `loadComponent`
  - no hay módulos Angular por feature
- shell principal en `src/app/app.html`
  - navegación superior con `routerLink`
  - layout Bootstrap simple, sin sidebar dedicado
- componentes standalone por página
  - convención `*.component.ts/html/scss`
- patrón de feature existente:
  - `admin/items/*`
  - `admin/item-sets/*`
  - `data-access` por feature
- convención HTTP:
  - `*.api.ts` para `HttpClient`
  - `*.facade.ts` como capa del feature
  - `*.models.ts` para contratos y helpers
  - `*.queries.ts` para query params y normalización
- manejo de loading/error/empty:
  - flags locales por página
  - `ApiProblemPanelComponent` como panel compartido de errores API
  - empty state inline dentro de tabla o card
- configuración API:
  - `src/environments/environment.ts`
  - `adminApiBaseUrl = '/api/admin/v1'`
  - `proxy.conf.json` apunta `/api` a `http://127.0.0.1:5248`

Conclusión:

- no existe infraestructura previa de spells en Angular
- sí existe un patrón claro reutilizable desde `Items` y `Item Sets`
- la implementación correcta para Phase 6 era una feature nueva `admin/spells`

## Referencia legacy aplicada

Se tomó como referencia de comportamiento visible el catálogo inicial de:

- `legacy-reference/Rollback.Web/Pages/Admin/Spells.razor`

Paridad portada en esta fase:

- cabecera de Spell Builder
- búsqueda libre
- filtro por `TypeId` como reemplazo de categoría
- listado ordenado y accionable
- estados de loading y vacío
- navegación hacia detalle futuro

Paridad diferida:

- split view catálogo + editor
- `Solo con efectos críticos`
- botón `Nuevo hechizo`
- auditoría visual avanzada
- niveles, states y effects

## Ruta Angular creada

- `/admin/spells`

Ruta mínima adicional para navegación futura:

- `/admin/spells/:spellId`

Importante:

- esta segunda ruta es solo placeholder operativo para no bloquear la navegación
- no implementa detalle real ni editor

## Servicio Angular creado

Feature nueva:

- `src/app/admin/spells/data-access/spells.api.ts`
- `src/app/admin/spells/data-access/spells.facade.ts`
- `src/app/admin/spells/data-access/spells.models.ts`
- `src/app/admin/spells/data-access/spells.queries.ts`

Contrato consumido:

- `GET /api/admin/v1/spells`

Parámetros usados:

- `search`
- `spellId`
- `breedId`
- `typeId`
- `page`
- `pageSize`

## Pantalla creada

Página principal:

- `src/app/admin/spells/spells-page.component.ts`
- `src/app/admin/spells/spells-page.component.html`
- `src/app/admin/spells/spells-page.component.scss`

Pantalla placeholder de detalle:

- `src/app/admin/spells/spell-detail-page.component.ts`
- `src/app/admin/spells/spell-detail-page.component.html`
- `src/app/admin/spells/spell-detail-page.component.scss`

## Integración en navegación

Archivos modificados:

- `src/app/app.routes.ts`
- `src/app/app.html`

Cambios:

- se agregó enlace `Spells` al shell superior
- se registró la ruta lazy `admin/spells`
- se registró la ruta lazy mínima `admin/spells/:spellId`

## Campos mostrados en catálogo

Por fila se muestran:

- `spellId`
- `name`
- `description` corta
- `typeLabel`
- `typeId`
- `breeds`
- `levelCount`
- `iconId`
- `runtimeAvailable`
- `referenceAvailable`

## Filtros implementados

- búsqueda libre por `search`
  - útil para nombre, `TypeLabel`, descripción y otros campos cubiertos por backend
- filtro exacto por `spellId`
- filtro exacto por `breedId`
- filtro exacto por `typeId`
- cambio de `pageSize`
- paginación siguiente/anterior

## Decisión sobre TypeLabel y raza

No se añadieron endpoints nuevos de opciones para dropdown.

Motivo:

- la fase pedía priorizar el patrón existente y no cambiar la API salvo necesidad estricta
- `GET /api/admin/v1/spells` ya soporta `search`, `breedId` y `typeId`
- con el backend actual, la solución segura era:
  - `search` para nombre y `TypeLabel`
  - `typeId` exacto como reemplazo real de categoría
  - `breedId` exacto cuando el operador lo necesite

## Archivos creados o modificados

### Creados

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.models.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.queries.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.api.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.facade.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spells-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spells-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spells-page.component.scss`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.scss`
- `docs/admin-tools/spell-builder/spell-builder-phase6-angular-catalog.md`

### Modificados

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/app.routes.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/app.html`
- `docs/handoffs/AGENT_HANDOFF.md`

## Campos diferidos a Phase 7/8/9

### Diferido a Phase 7

- detalle real del spell
- consumo de `GET /api/admin/v1/spells/{spellId}`
- contexto read-only de niveles
- sidebar o contexto de auditoría más rico

### Diferido a Phase 8

- editor de niveles
- states requeridos/prohibidos
- flujo de guardado runtime

### Diferido a Phase 9

- editor de effects
- write de effects
- decisiones de preservación de payload

## Limitaciones conocidas

- no existe preview/icon asset de spells en Angular actual; por eso se muestra `IconId`, no sprite real
- no hay endpoint de opciones para `TypeId` o `BreedId`; se usan filtros exactos por número
- la ruta `/admin/spells/:spellId` es placeholder operacional, no detalle real
- no se integró botón `Nuevo hechizo` para no mezclar create flow con catálogo
- no se portó `Solo con efectos críticos` porque el contrato de catálogo Phase 2 no expone ese filtro como endpoint Angular separado

## Validación ejecutada

### Angular

Comando:

```powershell
npm run build
```

Resultado:

- `OK`
- warning de budget inicial:
  - total `501.51 kB`
  - excede el warning de `500 kB` por `1.51 kB`

### Dotnet

Comando:

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"
```

Resultado:

- `OK`

Warnings observados:

- `NETSDK1057` por SDK preview
- `CA1416` en `FirewallManager.cs`
- `CS0169` en `D2pEntry.cs`

No hubo:

- `MSB3027`
- `MSB3021`
- bloqueo externo por DLLs

## Próxima fase recomendada

- `Phase 7 - Angular Detail`

Alcance sugerido:

- página real `/admin/spells/:spellId`
- consumo de `GET /api/admin/v1/spells/{spellId}`
- resumen read-only de niveles
- puente visual hacia los endpoints de Phase 4 y Phase 5
