# Agent Handoff - Sprite Preview Pipeline (Macro 3)

Generated: `2026-06-03`

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-curated-import-phase3
```

## Estado Macro 3

```txt
Phase 1 DONE — audit scaffold
Phase 2 DONE — D2P extract-icon (Sunshine D2pFile)
Phase 3 DONE — by-icon/23012.png (Dofus Ocre 7754)
Phase 4 NEXT — curated import workflow / selector integration
```

## Ultimo trabajo (Phase 3)

Commit:

```txt
feat: import curated dofus ocre icon preview
```

Asset:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/23012.png
```

Comando:

```bash
dotnet run --project Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj -- \
  --mode extract-icon --icon-id 23012 \
  --output Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted \
  --approve-curated-copy
```

## Validacion

```txt
Pipeline audit 7754 → IconPreviewAvailable: yes, BY_ICON /23012.png
dotnet build (pipeline + Sunshine.sln) → OK
npm run build → OK
API/browser → pendiente confirmacion operador con stack levantado
```

URLs QA:

```txt
/admin/items/7754
/admin/items/7754/publication-status
```

## Prohibiciones

```txt
no mas imports masivos sin aprobacion
no commitear temporal-artifacts
no modificar Client2.3.7
```

## Siguiente agente

Phase 4: formalizar workflow de import + selector; no abrir Macro 4 (Spells) sin peticion.

Docs: `docs/admin-tools/sprite-preview/sprite-preview-curated-import-phase3.md`
