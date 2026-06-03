# Macro 3 / Phase 2 — D2P Extractor Research + Minimal Proof

Estado: `DONE`

## Objetivo

Demostrar lectura/extracción puntual de iconos desde `bitmap*.d2p` del cliente 2.3.7, reutilizando el lector D2P ya presente en Sunshine.

## Resultado

| Criterio | Estado |
| --- | --- |
| D2P auditado (tamaño, entradas, links) | OK |
| Lector reutilizable identificado | `Sunshine.Protocol.Tools.D2p.D2pFile` |
| Modo `d2p-audit` | OK |
| Modo `extract-icon` | OK |
| IconId 23012 (Dofus Ocre) | Extraído desde `bitmap0.d2p` → `23012.png` |
| IconId 1001 (control) | Extraído desde `bitmap0.d2p` → `1001.png` |
| Sin modificar cliente | OK |
| Artefactos en temporal-artifacts | OK (gitignored) |

## Lector reutilizado

Proyecto wrapper (solo lectura, sin copiar lógica):

```txt
infrastructure/shared/Sunshine.Protocol.D2pReadOnly/
```

Enlaza fuentes desde `Sunshine net11.0/.../Sunshine.Protocol/Tools/D2p` + IO big-endian.

## Comandos

```bash
dotnet build "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj"

dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode d2p-audit \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit"

dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted"
```

Copia opcional al catálogo Angular (máx. 1 PNG, explícito):

```bash
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted" \
  --approve-curated-copy
```

Destino: `Angular-tools/Admin/.../src/assets/item-previews/by-icon/23012.png`

## Casos de control

| Caso | Resultado Phase 2 |
| --- | --- |
| 7754 / IconId 23012 | Extraíble desde D2P; antes solo faltaba PNG curado en Admin |
| 39 / IconId 1001 | Ya curado en Angular; también presente en `bitmap0.d2p` |
| 12617 | Sigue requiriendo client patch antes de preview |

## Artefactos generados (no commitear)

```txt
Infrastructure/temporal-artifacts/item-sprite-preview-audit/d2p-audit-report.md
Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted/23012.png
Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted/extract-icon-23012.md
```

## Phase 3 — DONE

`23012.png` importado al catálogo curado. Ver [sprite-preview-curated-import-phase3.md](./sprite-preview-curated-import-phase3.md).

## Phase 4 — DONE

Workflow dry-run/approve documentado. Ver [sprite-preview-curated-workflow-phase4.md](./sprite-preview-curated-workflow-phase4.md).

## Phase 5 — NEXT (solo con aprobación)

- Estrategia opcional `by-appearance/`.

## Documentación relacionada

- [D2P format notes](./sprite-preview-d2p-format-notes.md)
- [Source map](./sprite-preview-source-map.md)
- [Phase 1](./sprite-preview-pipeline-phase1.md)

## Rama

```txt
feature/item-sprite-preview-d2p-extractor-phase2
```
