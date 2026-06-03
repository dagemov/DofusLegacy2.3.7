# Agent Handoff - Sprite Preview Pipeline (Macro 3)

Generated: `2026-06-03`

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-curated-workflow-phase4
```

## Estado Macro 3

```txt
Phase 1–3 DONE
Phase 4 DONE / PARTIAL — dry-run + approve workflow, selector UX
Phase 5 NEXT — appearance preview (solo con aprobación)
```

## Ultimo trabajo (Phase 4)

Commit:

```txt
feat: formalize curated sprite preview import workflow
```

Cambios:

```txt
--dry-run-curated-copy / --approve-curated-copy / --overwrite-curated
CuratedIconCopyPlanner.cs
Icon selector: labels ES, fuente CURATED_BY_ICON, banner CLI
API Source: CURATED_BY_ICON
```

## Comando dry-run validado (23012)

```bash
dotnet run --project Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj -- \
  --mode extract-icon --icon-id 23012 \
  --output Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted \
  --dry-run-curated-copy
```

## QA pendiente (operador, stack levantado)

```txt
/admin/items/7754
/admin/items/7754/publication-status
/admin/items/icon-selector?iconId=23012
```

## Prohibiciones

```txt
no import masivo
no commitear temporal-artifacts
no modificar Client2.3.7
```

Docs: `docs/admin-tools/sprite-preview/sprite-preview-curated-workflow-phase4.md`
