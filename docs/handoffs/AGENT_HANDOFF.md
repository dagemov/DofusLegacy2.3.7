# Agent Handoff - Sprite Preview Pipeline (Macro 3)

Generated: `2026-06-03`

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-appearance-curated-phase6
```

## Estado Macro 3

```txt
Phase 1–5 DONE
Phase 6 DONE / PARTIAL — appearance preview diagnostics + Angular UX
Phase 7 OPTIONAL / DEFERRED — EntityLook renderer research
```

## Ultimo trabajo (Phase 6)

Commit:

```txt
feat: add curated appearance preview diagnostics
```

Cambios:

```txt
ItemAppearancePreviewStateDto + FileSystemItemAppearancePreviewStateResolver
GET /api/admin/v1/items/appearance-preview-state
ItemDetailDto / PublicationStatus / QaSummary incluyen appearancePreviewState
ItemAppearancePreviewCardComponent en detail / edit / publication-status
by-appearance/.gitkeep confirmado
```

Estados: `NOT_APPLICABLE`, `CURATED_BY_APPEARANCE`, `MISSING`, `UNKNOWN`

## QA browser (operador, API levantada)

```txt
/admin/items/7754 → AppearanceId 0, NOT_APPLICABLE
/admin/items/7754/publication-status
/admin/items/12616 → AppearanceId 1004, AppearanceKnown=false, UNKNOWN
/admin/items/12616/publication-status
```

## Builds validados

```txt
dotnet build Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj
npm run build (Angular Admin)
```

## Prohibiciones respetadas

```txt
no EntityLook renderer
no Tiphon
no extraccion masiva
no tocar cliente/DB/gameplay
```

## Siguiente (opcional)

```txt
ItemAppearanceSelectorComponent (fase futura)
Macro 3 Phase 7 EntityLook research — solo con aprobacion
```

Docs: `docs/admin-tools/sprite-preview/appearance-preview-curated-workflow-phase6.md`
