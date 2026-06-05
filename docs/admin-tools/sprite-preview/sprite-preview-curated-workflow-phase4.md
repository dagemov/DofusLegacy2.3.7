# Macro 3 / Phase 4 — Curated Import Workflow + Icon Selector Integration

Estado: `DONE / PARTIAL` (QA navegador/API con stack levantado pendiente operador)

## Objetivo

Formalizar el flujo de import curado puntual desde D2P y mejorar la UX del selector de iconos para que el operador entienda preview disponible, fuente y comandos.

## Tool — flags de copia curada

| Flag | Efecto |
| --- | --- |
| `--dry-run-curated-copy` | Muestra plan de copia; **no** escribe en `by-icon/` |
| `--approve-curated-copy` | Copia a `by-icon/{iconId}.png` tras validaciones |
| `--overwrite-curated` | Requerido si el PNG curado ya existe |

Validaciones en approve:

- `--icon-id` explícito y positivo
- Firma PNG válida desde D2P
- Destino resuelto dentro de `item-previews/by-icon/`
- Un solo icono por ejecución (no batch)

## Comandos

```bash
# Dry-run (ej. Dofus Ocre)
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted" \
  --dry-run-curated-copy

# Aprobar copia (si no existe)
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted" \
  --approve-curated-copy

# Reemplazar PNG curado existente
dotnet run ... --approve-curated-copy --overwrite-curated
```

Salida dry-run (consola + `curated-copy-dry-run-{iconId}.md` en `--output`):

```txt
iconId, source d2p, source entry, target path, will overwrite, png signature valid, target inside by-icon
```

## Selector Angular

Ruta: `/admin/items/icon-selector`

Mejoras:

- Etiquetas en español: Preview disponible / faltante
- Fuente: `CURATED_BY_ICON` (API)
- Items vinculados + nombres de muestra desde DB
- Banner con comandos dry-run / approve

El catálogo lista solo PNGs ya presentes en `by-icon/`; iconos sin curar se importan vía CLI, no aparecen en el grid.

## QA Dofus Ocre (7754)

Con API + Angular:

```txt
/admin/items/7754
/admin/items/7754/publication-status
/admin/items/icon-selector?iconId=23012
```

Esperado:

```txt
preview BY_ICON /assets/item-previews/by-icon/23012.png
client identity: SAFE_EXISTING_TEMPLATE, CLIENT_KNOWN
publication: PUBLISHED
```

## Phase 5 — NEXT (solo con aprobación)

Estrategia opcional de preview por `AppearanceId` (`by-appearance/`).

## Rama

```txt
feature/item-sprite-preview-curated-workflow-phase4
```
