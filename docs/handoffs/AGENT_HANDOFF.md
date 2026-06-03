# Agent Handoff - Sprite Preview Pipeline (Macro 3)

Generated: `2026-06-03`

Leer este archivo antes de cualquier implementacion.

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-pipeline-phase1
```

Stack Admin: `Angular-tools/Admin/`

## Estado macros

```txt
Macro 2 COMPLETE — Client Identity (Phases 1–4)
Macro 3 IN_PROGRESS
  Phase 1 DONE / PARTIAL — source map + audit scaffold + casos 7754/39/12617
  Phase 2 NEXT — D2P extractor research or implementation (requiere scope explícito)
```

## Ultimo trabajo (Macro 3 Phase 1)

Commit esperado:

```txt
feat: add item sprite preview pipeline scaffold
```

Entregables:

```txt
Infrastructure/scripts/ItemSpritePreviewPipeline/ (--mode audit)
docs/admin-tools/sprite-preview/*
Infrastructure/temporal-artifacts/item-sprite-preview-audit/ (gitignored)
by-appearance/.gitkeep en Angular assets
```

Comando validado:

```bash
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode audit --items 7754,39,12617 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit"
```

## Resultados audit (resumen)

| ItemId | Icon preview curado | ClientKnown | Siguiente paso |
| --- | --- | --- | --- |
| 7754 | no (IconId 23012 en D2P, sin índice) | yes | Phase 2 D2P o curar by-icon/23012.png |
| 39 | sí (by-icon/1001.png) | yes | Mantener catálogo |
| 12617 | no | no | Client patch antes de preview |

AppearanceId 458: hipótesis no verificada; ver `items-client-appearance-mapping-audit.md`.

## Validacion ejecutada

```txt
dotnet build ItemSpritePreviewPipeline.csproj -> OK
dotnet build Sunshine.sln /nr:false -> OK
```

## Prohibiciones

```txt
no worktrees externos
no modificar Client2.3.7 write
no extracción masiva D2P
no commitear temporal-artifacts
no DB writes
no Macro 4+ (Spells/Maps) sin petición
```

## Siguiente agente

```txt
1. Confirmar commit feat Phase 1.
2. Phase 2 solo si el usuario aprueba scope (lector D2P o import puntual 1–3 PNG).
3. No tocar cliente ni gameplay.
```

Docs: `docs/admin-tools/sprite-preview/sprite-preview-pipeline-phase1.md`
