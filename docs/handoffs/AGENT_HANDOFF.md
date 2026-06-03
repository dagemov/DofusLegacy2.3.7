# Agent Handoff - Sprite Preview Pipeline (Macro 3)

Generated: `2026-06-03`

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-d2p-extractor-phase2
```

## Estado Macro 3

```txt
Phase 1 DONE / PARTIAL — audit scaffold + source map
Phase 2 DONE — D2P audit + extract-icon (Sunshine D2pFile reutilizado)
Phase 3 NEXT — curated icon import / Angular integration
```

## Ultimo trabajo (Phase 2)

Commit esperado:

```txt
feat: add d2p icon extraction audit mode
```

Entregables:

```txt
infrastructure/shared/Sunshine.Protocol.D2pReadOnly/  (linked Sunshine D2P reader)
ItemSpritePreviewPipeline: --mode d2p-audit, --mode extract-icon
docs/admin-tools/sprite-preview/sprite-preview-d2p-extractor-phase2.md
docs/admin-tools/sprite-preview/sprite-preview-d2p-format-notes.md
```

## Prueba mínima validada

```txt
IconId 23012 → bitmap0.d2p / 23012.png (3881 bytes, PNG OK)
IconId 1001  → bitmap0.d2p / 1001.png (control)
```

Salida temporal (gitignored):

```txt
Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted/23012.png
```

Copia a Angular **no** incluida por defecto. Usar `--approve-curated-copy` para `by-icon/23012.png`.

## Comandos

```bash
dotnet run --project Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj -- \
  --mode d2p-audit --output Infrastructure/temporal-artifacts/item-sprite-preview-audit

dotnet run --project Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj -- \
  --mode extract-icon --icon-id 23012 \
  --output Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted
```

## Prohibiciones

```txt
no modificar Client2.3.7
no extracción masiva / no commitear temporal-artifacts
no DB writes
```

## Siguiente agente

Phase 3: promover `23012.png` al catálogo si operador aprueba; opcional API hint “preview desde D2P”.

Docs: `docs/admin-tools/sprite-preview/sprite-preview-d2p-extractor-phase2.md`
