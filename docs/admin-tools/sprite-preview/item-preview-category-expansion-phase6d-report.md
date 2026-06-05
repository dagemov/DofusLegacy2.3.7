# Item preview category expansion — Phase 6D report

Generado: `2026-06-05`  
Rama: `feature/item-preview-category-expansion-phase6d`

## Resumen

| Métrica | Valor |
| --- | --- |
| Items catalogados (sin armas, con consumibles) | **2529** |
| Armas excluidas | **95** |
| PNG nuevos extraídos (run 6D) | **1416** |
| PNG ignorados (ya en Angular) | **0** |
| PNG copiados a Angular (run 6D) | **1416** |
| **Total PNG en Angular** | **1916** |
| Errores de extracción | **0** |
| Armas copiadas | **0** |

## Por categoría (Angular)

| Categoría | PNG |
| --- | ---: |
| dofus | 10 |
| sombreros | 282 |
| capas | 208 |
| botas | 199 |
| amuletos | 203 |
| anillos | 233 |
| escudos | 58 |
| mascotas | 78 |
| cinturones | 208 |
| recursos | 237 |
| consumibles | 200 |
| trofeos | 0 |

**Trofeos:** TypeId `151` mapeado; en este cliente no hubo entradas con preview disponible en el run.

## Categorías omitidas en extracción (ya pobladas)

```txt
dofus, sombreros, capas
```

## CLI

```bash
dotnet run --project Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj -- \
  --mode item-preview-expand-categories \
  --categories botas,amuletos,anillos,escudos,mascotas,cinturones,trofeos,recursos,consumibles \
  --limit 1500 \
  --skip-categories dofus,sombreros,capas \
  --approve-curated-copy
```

## Manifest (`catalog-manifest.json`)

- `categoryStats`: conteo, `lastExtractionUtc`, `previewSource: client-bitmap-d2p`
- `totalPngInAngular`: 1916
- Fase: `phase6d`

## Angular UX

- `/admin/items/icon-selector`: chips con contador por categoría (ej. `Botas (199)`)
- Búsqueda avanzada AND: `ItemId` + `IconId` + `nameEs` + `nameEn` simultáneos
- API: `GET /api/admin/v1/item-icons/category-stats`

## Criterio 1000+ previews

**Cumplido:** 1916 PNG visibles en `by-category/`.

## Browser QA

`PENDING_OPERATOR_BROWSER_QA` — validar selector, filtros combinados y categorías nuevas.
