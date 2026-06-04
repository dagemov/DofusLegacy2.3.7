# Sprite Preview Pipeline (Macro 3)

Pipeline offline y catálogo curado para previews de ítems en Angular Admin.

## Estado — Macro 3 COMPLETE

| Fase | Estado |
| --- | --- |
| Macro 3 / Phase 1 | `DONE` — source map, scaffold audit |
| Macro 3 / Phase 2 | `DONE` — lector D2P + `extract-icon` puntual |
| Macro 3 / Phase 3 | `DONE` — `by-icon/23012.png` (Dofus Ocre) |
| Macro 3 / Phase 4 | `DONE / PARTIAL` — workflow dry-run/approve + selector UX |
| Macro 3 / Phase 5 | `DONE` — appearance identity audit |
| Macro 3 / Phase 6 | `DONE / PARTIAL` — appearance preview diagnostics |
| Macro 3 / Phase 7 | `DONE` — final QA + macro closure |

**EntityLook renderer:** `DEFERRED` — no requerido para Items Builder MVP.

## Macro 4 / Phase 6 — Item skin catalog (plan)

- [Item skin catalog plan](./item-skin-catalog-plan-phase6.md)
- [Category map](./item-skin-category-map.md)
- Dry-run CLI: `item-skin-catalog-dry-run` (sin copia masiva PNG)
- Carpetas planificadas: `src/assets/item-previews/by-category/*`

## Cuatro superficies en Admin (post Phase 7)

| Superficie | Identidad | Asset |
| --- | --- | --- |
| Icon Preview | `IconId` | `by-icon/{iconId}.png` |
| Appearance Preview | `AppearanceId` | `by-appearance/{appearanceId}.png` (curado manual) |
| Client Identity | `ItemId` en `Items.d2o` | — |
| Publication Status | visibilidad + patch | — |

```txt
ItemId != IconId != AppearanceId
```

## Objetivo cumplido

Conectar identidad cliente (Macro 2) con assets visuales utilizables en Admin sin extracción masiva ni renderer 3D.

## Herramienta offline

```txt
Infrastructure/scripts/ItemSpritePreviewPipeline/
```

```bash
dotnet run --project Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj -- \
  --mode audit --items 7754,12616,39 \
  --output Infrastructure/temporal-artifacts/item-sprite-preview-audit

dotnet run --project Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj -- \
  --mode extract-icon --icon-id 23012 \
  --output Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted \
  --dry-run-curated-copy
```

## Destinos Angular (curado manual)

```txt
src/assets/item-previews/by-icon/
src/assets/item-previews/by-item/
src/assets/item-previews/by-appearance/
```

## Documentos

- [Phase 7 final QA](./sprite-preview-final-qa-phase7.md)
- [Phase 6 appearance curated](./appearance-preview-curated-workflow-phase6.md)
- [Phase 5 appearance audit](./appearance-identity-audit-phase5.md)
- [EntityLook map](./entitylook-relationship-map.md)
- [Phase 1 plan](./sprite-preview-pipeline-phase1.md)
- [Phase 2 D2P](./sprite-preview-d2p-extractor-phase2.md)
- [Phase 3 curated import](./sprite-preview-curated-import-phase3.md)
- [Phase 4 workflow](./sprite-preview-curated-workflow-phase4.md)
- [D2P format notes](./sprite-preview-d2p-format-notes.md)
- [Source map](./sprite-preview-source-map.md)

## Reglas (respetadas en Macro 3)

- read-only sobre cliente (`Client2.3.7`)
- sin extracción masiva
- sin writes DB
- sin renderer EntityLook / Tiphon en Admin
