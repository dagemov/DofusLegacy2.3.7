# Macro 3 / Phase 6 — Appearance Preview Curated Workflow

## Estado

| Campo | Valor |
| --- | --- |
| Fase | Macro 3 / Phase 6 |
| Estado | `DONE / PARTIAL` |
| Commit feature | `feat: add curated appearance preview diagnostics` |
| Renderer EntityLook | **No implementado** (Phase 7 opcional) |

## Objetivo

Exponer en Admin el estado del preview de equipamiento por `AppearanceId`, separado del preview de inventario por `IconId`, usando solo PNG curados en:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-appearance/{appearanceId}.png
```

## Entregables código

### Backend

| Pieza | Descripción |
| --- | --- |
| `ItemAppearancePreviewStateDto` | `appearanceId`, `appearanceKnown`, `state`, rutas, `previewSource` |
| `IItemAppearancePreviewStateResolver` | Resolución filesystem read-only |
| `GET /api/admin/v1/items/appearance-preview-state` | Lookup puntual (formulario) |
| DTOs enriquecidos | `ItemDetailDto`, `ItemPublicationStatusDto`, `ItemQaSummaryDto` incluyen `appearancePreviewState` |

Estados:

| Estado | Significado |
| --- | --- |
| `NOT_APPLICABLE` | `AppearanceId <= 0` |
| `CURATED_BY_APPEARANCE` | Existe `by-appearance/{id}.png` |
| `MISSING` | Cliente puede conocer la apariencia, falta PNG curado |
| `UNKNOWN` | `AppearanceKnown = false` o sin validación D2O |

### Angular

| Pieza | Ubicación |
| --- | --- |
| `ItemAppearancePreviewCardComponent` | Detalle, edición, publication-status |
| API `getAppearancePreviewState` | Refresh en vivo al cambiar `AppearanceId` en formulario |

### Assets

```txt
src/assets/item-previews/by-appearance/.gitkeep
```

Sin extracción masiva ni PNG obligatorio en esta fase.

## Workflow operador (curado manual)

1. Equipar el ítem en cliente o capturar referencia visual del skin.
2. Exportar/guardar un PNG representativo (una captura por `AppearanceId`).
3. Copiar a:

   ```txt
   src/assets/item-previews/by-appearance/{appearanceId}.png
   ```

4. Verificar en Admin:
   - `/admin/items/{itemId}` → tarjeta **Appearance Preview**
   - Estado `CURATED_BY_APPEARANCE` y ruta resuelta.

5. Si `AppearanceKnown = No`: corregir `AppearanceId` en DB o ampliar `Appearances.d2o` del pack cliente (fuera de Admin).

**Regla:** `AppearanceId` no es `IconId`. El icono de inventario sigue en `by-icon/`.

## Casos QA esperados

| Item | AppearanceId | AppearanceKnown | Preview state |
| --- | --- | --- | --- |
| `7754` | `0` | n/a | `NOT_APPLICABLE` |
| `12616` | `1004` | `false` | `UNKNOWN` (+ warning APPEARANCE_UNKNOWN) |

Rutas:

```txt
/admin/items/7754
/admin/items/7754/publication-status
/admin/items/12616
/admin/items/12616/publication-status
```

## Fase futura — selector (no implementada)

Plan documental únicamente:

```txt
ItemAppearanceSelectorComponent
```

- Modal/grid de apariencias conocidas por tipo de ítem
- Fuente: catálogo DB + índices `Appearances.d2o` + previews curados
- Macro posterior; no bloquea Phase 6

## Limitaciones

- No valida que el skin exista en packs `gfx/sprites`
- No compone `EntityLook` (bone + colores + subentidades)
- `appearanceKnown` en formulario edit usa el snapshot del ítem cargado hasta recargar identidad cliente

## Referencias

- [appearance-identity-audit-phase5.md](./appearance-identity-audit-phase5.md)
- [appearance-preview-feasibility-study.md](./appearance-preview-feasibility-study.md)
- [sprite-preview-curated-workflow-phase4.md](./sprite-preview-curated-workflow-phase4.md)
