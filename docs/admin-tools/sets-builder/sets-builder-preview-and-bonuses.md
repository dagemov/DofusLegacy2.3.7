# Sets Builder — Preview y bonos por piezas

**Fecha:** 2026-06-05  
**Rama:** `feature/items-preview-sets-polish-final`

## Alcance

Lectura Admin de sets de equipo con previews de miembros y bonos decodificados desde `items_sets.Effects` (formato WorldServer).

## Rutas Angular

| Ruta | Componente |
| --- | --- |
| `/admin/item-sets` | `item-sets-page` — listado |
| `/admin/item-sets/:setId` | `item-set-detail-page` — detalle |

Enlace desde listado de ítems: **Sets de equipo**.

## API

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/api/admin/v1/item-sets` | Lista: `setId`, nombre, cantidad de ítems, tiers de bonus |
| GET | `/api/admin/v1/item-sets/{setId}` | Detalle: miembros + tiers + efectos |
| GET | `/api/admin/v1/item-sets/options` | Opciones para formulario de ítem (sin cambios) |

## Detalle de set

### Miembros

Cada fila incluye:

- `itemId`, nombre, `typeId`, `typeName`, `iconId`
- `preview` — mismo `ItemPreviewStateDto` que Items (incluye `BY_CATEGORY`)
- Etiqueta legible de preview (`Preview disponible` / `Preview pendiente`)

### Bonos por piezas

Tiers decodificados con etiquetas humanas:

- 2 piezas, 3 piezas, 4 piezas, 5 piezas, set completo

Cada efecto expone:

- `effectId`
- `label` — desde `GET /api/admin/v1/item-effects/options` (no labels inventados)
- `protocolName`, `value`, `format`

Si un `effectId` no está en catálogo, fallback técnico `Effect {id}`.

## Backend

| Pieza | Ubicación |
| --- | --- |
| DTOs | `RollblackLegacy.Admin.Contracts/Items/ItemSetDtos.cs` |
| SQL | `ItemSetsAdminReadRepository` → tablas `items_sets` + ítems por `ItemSetId` |
| Codec | `ItemSetEffectsCodec` (Application) |
| Servicio | `ItemSetsAdminReadService` |
| Controller | `ItemSetsAdminController` |

## Validación

| Check | Resultado |
| --- | --- |
| `dotnet build` Application | OK |
| `npm run build` | OK (chunks lazy `item-sets-page`, `item-set-detail-page`) |
| Browser QA | Pendiente operador: `/admin/item-sets`, `/admin/item-sets/{id}` |

## Próximo (fuera de esta fase)

- CRUD de sets (edición de nombre, miembros, effects hex)
- Publicación de sets al cliente
