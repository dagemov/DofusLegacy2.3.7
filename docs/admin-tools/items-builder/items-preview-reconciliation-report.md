# Items Builder — Preview reconciliation report

**Fecha:** 2026-06-05  
**Rama:** `feature/items-preview-sets-polish-final`  
**Base:** `feature/item-preview-category-expansion-phase6d`

## Resumen

El resolver de preview solo consideraba `by-item` y `by-icon`. Tras Phase 6D existen **1916 PNG** bajo `by-category/`, pero el API seguía devolviendo `previewState = MISSING` para la mayoría de ítems cuyo `IconId` solo estaba catalogado por categoría.

Se añadió el fallback **`BY_CATEGORY`** (prioridad 3) usando `catalog-manifest.json`, índice en disco y `ItemPreviewCategoryTypeMap` por `typeId`.

## Inventario de assets (Angular)

| Ruta | PNG en repo |
| --- | ---: |
| `src/assets/item-previews/by-item/` | 0 |
| `src/assets/item-previews/by-icon/` | 13 |
| `src/assets/item-previews/by-category/` | **1916** |
| `catalog-manifest.json` → `totalPngInAngular` | 1916 |
| `catalog-manifest.json` → `totalCataloged` | 2529 |
| Armas excluidas del catálogo | 95 |

## Métricas de reconciliación

| Métrica | Valor | Notas |
| --- | ---: | --- |
| **MISSING estimado antes** (ítems con `IconId` en índice categoría, sin by-item/by-icon) | **~1903** | 1916 iconIds indexados − ~13 solapados en `by-icon` |
| **Resueltos por BY_CATEGORY** (mismo universo) | **~1903** | Pasan a `state = FOUND`, `previewSource = BY_CATEGORY` |
| **Aún MISSING** (causas estructurales) | variable | Ver tabla de causas; no requiere extracción nueva si el icono no está en catálogo |

> Auditoría sin consulta masiva a MySQL: cifras derivadas del índice de categoría (1916 `iconId`) y del resolver. Validación operativa: listado/detalle Admin y rutas QA del handoff.

## Prioridad de resolución (implementada)

1. `/assets/item-previews/by-item/{itemId}.png`
2. `/assets/item-previews/by-icon/{iconId}.png`
3. `/assets/item-previews/by-category/{category}/{iconId}.png` — manifest + escaneo + fallback por `typeId`
4. Placeholder / `MISSING` si existen directorios de preview pero ninguna ruta aplica

## Causa raíz

`FileSystemItemPreviewStateResolver` no leía `by-category/` ni `catalog-manifest.json`. Los PNG de Phase 6D nunca participaban en `Resolve()`.

## Cambios técnicos

- `ItemPreviewCategoryIndex` — índice `iconId → category` desde manifest y filesystem.
- `ItemPreviewCategoryTypeMap` — categoría por `TypeId` cuando el manifest no lista el icono pero el PNG existe en disco.
- `ItemPreviewStateDto.ByCategoryPath` — expuesto al Angular.
- `ItemsAdminReadService` / sets — `Resolve(itemId, iconId, typeId)` en lista y detalle.

## Ejemplos

| Caso | Antes | Después |
| --- | --- | --- |
| Amuleto `IconId` presente solo en `by-category/amuletos/12345.png` | `MISSING` | `FOUND` / `BY_CATEGORY` |
| Ítem con PNG en `by-icon/7754.png` (13 casos) | `FOUND` / `BY_ICON` | Sin cambio |
| Arma excluida del catálogo Phase 6D | `MISSING` | `MISSING` (esperado) |
| `IconId` no catalogado y sin by-item/by-icon | `MISSING` | `MISSING` |

## Validación

| Check | Resultado |
| --- | --- |
| `dotnet build` Application | OK |
| `dotnet build` Admin.Api | `FAILED_EXTERNAL_LOCK` si Visual Studio tiene el API en ejecución |
| `npm run build` | OK |

## Referencias

- [items-builder README](./README.md)
- Manifest: `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-category/catalog-manifest.json`
- Resolver: `RollblackLegacy.Admin.Infrastructure/Services/Items/FileSystemItemPreviewStateResolver.cs`
