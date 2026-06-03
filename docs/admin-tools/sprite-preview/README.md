# Sprite Preview Pipeline (Macro 3)

Pipeline offline y catálogo curado para previews de ítems en Angular Admin.

## Estado

| Fase | Estado |
| --- | --- |
| Macro 3 / Phase 1 | `DONE / PARTIAL` — source map, scaffold audit, casos 7754/39/12617 |
| Macro 3 / Phase 2 | `NEXT` — investigación o implementación lector D2P |

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
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode audit \
  --items 7754,39,12617 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit"
```

Salidas temporales (gitignored): `Infrastructure/temporal-artifacts/`

## Destinos Angular (curado manual, no masivo)

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-item/
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-appearance/
```

## Documentos

- [Phase 1 plan](./sprite-preview-pipeline-phase1.md)
- [Source map](./sprite-preview-source-map.md)
- [Phase 1 audit report](./item-sprite-preview-phase1-report.md)

## Reglas

- read-only sobre cliente (`Client2.3.7`)
- sin extracción masiva en Phase 1
- sin writes DB
- sin commitear `temporal-artifacts`
