# Sprite Preview Pipeline (Macro 3)

Pipeline offline y catálogo curado para previews de ítems en Angular Admin.

## Estado

| Fase | Estado |
| --- | --- |
| Macro 3 / Phase 1 | `DONE / PARTIAL` — source map, scaffold audit, casos 7754/39/12617 |
| Macro 3 / Phase 2 | `DONE` — lector D2P reutilizado + `d2p-audit` / `extract-icon` |
| Macro 3 / Phase 3 | `DONE` — `by-icon/23012.png` (Dofus Ocre / 7754) |
| Macro 3 / Phase 4 | `DONE / PARTIAL` — workflow dry-run/approve + selector UX |
| Macro 3 / Phase 5 | `NEXT` — appearance preview strategy (solo con aprobación) |

## Objetivo

Conectar identidad cliente (Macro 2) con assets visuales utilizables en Admin:

- icon previews (`IconId` → `by-icon`)
- appearance previews (`AppearanceId` → `by-appearance`)
- sprite/look equipado (futuro)

## Herramienta offline

```txt
Infrastructure/scripts/ItemSpritePreviewPipeline/
```

```bash
# Phase 1 — identidad + previews curados
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode audit --items 7754,39,12617 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit"

# Phase 2 — auditoría D2P
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode d2p-audit \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit"

# Phase 4 — dry-run copia curada
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted" \
  --dry-run-curated-copy

# Phase 3/4 — aprobar copia al catálogo
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted" \
  --approve-curated-copy --overwrite-curated
```

Asset curado Phase 3: `src/assets/item-previews/by-icon/23012.png`

Salidas temporales (gitignored): `Infrastructure/temporal-artifacts/`

## Destinos Angular (curado manual, no masivo)

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-item/
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-appearance/
```

## Documentos

- [Phase 1 plan](./sprite-preview-pipeline-phase1.md)
- [Phase 2 D2P extractor](./sprite-preview-d2p-extractor-phase2.md)
- [Phase 3 curated import](./sprite-preview-curated-import-phase3.md)
- [Phase 4 curated workflow](./sprite-preview-curated-workflow-phase4.md)
- [D2P format notes](./sprite-preview-d2p-format-notes.md)
- [Source map](./sprite-preview-source-map.md)
- [Phase 1 audit report](./item-sprite-preview-phase1-report.md)

## Reglas

- read-only sobre cliente (`Client2.3.7`)
- sin extracción masiva en Phase 1
- sin writes DB
- sin commitear `temporal-artifacts`
