# Sets Builder — CRUD y paginación

**Fecha:** 2026-06-05  
**Rama:** `feature/sets-builder-crud-and-pagination`

## Problema resuelto

`/admin/item-sets` quedaba en loading infinito por:

1. Respuesta no paginada (lista completa + decode de Effects por fila).
2. Cambios de estado fuera de `NgZone` (patrón ya usado en `items-page`).

## API de listado

```http
GET /api/admin/v1/item-sets?page=&pageSize=&search=&minLevel=&maxLevel=&minParts=&maxParts=
```

Respuesta `ItemPagedResultDto<ItemSetListItemDto>`:

| Campo | Descripción |
| --- | --- |
| `setId`, `name` | Identidad del set |
| `level` | `MIN(items.Level)` del set |
| `itemCount` | Miembros con `ItemSetId` |
| `bonusTierCount` | Tiers decodificados de `Effects` |
| `previewItemIcons[]` | Hasta 4 rutas resueltas (`BY_CATEGORY` / `BY_ICON`) |

## CRUD write API

| Método | Ruta |
| --- | --- |
| POST | `/api/admin/v1/item-sets` |
| PUT | `/api/admin/v1/item-sets/{setId}` |
| DELETE | `/api/admin/v1/item-sets/{setId}` |

Payload create/update:

```json
{
  "name": "Set ejemplo",
  "level": 150,
  "itemIds": [12616, 7754],
  "bonusTiers": [
    {
      "pieceCount": 2,
      "effects": [{ "effectId": 125, "value": 50, "format": "Integer" }]
    }
  ]
}
```

Validaciones: nombre obligatorio, `itemIds` sin duplicados y existentes, `pieceCount >= 2`, `effectId` en catálogo.

Persistencia: `items_sets.Effects` (hex) + sincronía `items.ItemSetId`.

## Angular

| Ruta | Pantalla |
| --- | --- |
| `/admin/item-sets` | Lista paginada + filtros |
| `/admin/item-sets/new` | Crear |
| `/admin/item-sets/:setId` | Detalle |
| `/admin/item-sets/:setId/edit` | Editar |

## Validación

| Check | Resultado |
| --- | --- |
| `dotnet build` Admin.Api | OK |
| `npm run build` | OK |
