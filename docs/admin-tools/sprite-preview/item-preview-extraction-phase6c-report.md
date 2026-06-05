# Item preview extraction — Phase 6C report

Generado: `2026-06-04`  
Rama: `feature/item-preview-massive-extraction-phase6c`

## Resumen

| Métrica | Valor |
| --- | --- |
| Items catalogados (sin armas) | **1925** |
| Armas excluidas (TypeIds) | **95** |
| PNG extraídos (temporal, límite 500) | **500** |
| PNG copiados a Angular (aprobado) | **500** |
| Errores de extracción | **0** |
| Armas copiadas a Angular | **0** |

## Por categoría (extracción + copia)

| Categoría | PNG extraídos | PNG en manifest Angular |
| --- | ---: | ---: |
| dofus | 10 | 10 |
| sombreros | 282 | 282 |
| capas | 208 | 208 |
| botas | 0 | 0 |
| mascotas | 0 | 0 |
| escudos | 0 | 0 |
| anillos | 0 | 0 |
| amuletos | 0 | 0 |
| cinturones | 0 | 0 |
| recursos | 0 | 0 |

**Nota:** con `--limit 500` y prioridad `dofus → sombreros → capas`, el cupo se agotó en capas. Para más categorías, re-ejecutar extract/copy con límite mayor o por categoría.

## Criterios de cierre Phase 6C

| # | Criterio | Estado |
| --- | --- | --- |
| 1 | PNG reales desde D2P (`bitmap0.d2p`, `bitmap1.d2p`) | OK |
| 2 | JSON / CSV / HTML temporal | OK — `Infrastructure/temporal-artifacts/item-skin-catalog/export/` |
| 3 | Copia aprobada + manifest | OK — ver [manifest](./item-preview-curated-copy-manifest-phase6c.md) |
| 4 | Icon selector por categoría | OK — `/admin/items/icon-selector` |
| 5 | Dofus completo visible | OK — 10/10 iconos |
| 6 | Armas excluidas | OK — 0 en copia |
| 7 | `npm run build` | OK |
| 8 | Cliente real sin modificar | OK |
| 9 | `temporal-artifacts` sin commit | OK |
| 10 | Handoff actualizado | OK |

## CLI

```bash
dotnet run --project Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj -- \
  --mode item-preview-extract-by-category \
  --categories dofus,sombreros,capas,botas,mascotas,escudos,anillos,amuletos,cinturones,recursos \
  --limit 500 --exclude-types weapons \
  --output Infrastructure/temporal-artifacts/item-skin-catalog/export

dotnet run --project Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj -- \
  --mode item-preview-copy-to-angular \
  --source Infrastructure/temporal-artifacts/item-skin-catalog/export \
  --approve-curated-copy
```

## Artefactos temporales (no en git)

```txt
Infrastructure/temporal-artifacts/item-skin-catalog/export/catalog.json
Infrastructure/temporal-artifacts/item-skin-catalog/export/catalog.csv
Infrastructure/temporal-artifacts/item-skin-catalog/export/gallery.html
Infrastructure/temporal-artifacts/item-skin-catalog/export/png/by-category/{category}/{iconId}.png
```

## Artefactos Angular (commit)

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-category/
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-category/catalog-manifest.json
```

## Ejemplos visibles

- **Dofus:** `23012.png` (Dofus Ocre), `23001`–`23017` en `by-category/dofus/`
- **Sombreros / capas:** cientos de previews bajo `by-category/sombreros/` y `by-category/capas/`
- **Galería local:** abrir `Infrastructure/temporal-artifacts/item-skin-catalog/export/gallery.html`

## Browser QA (operador)

- `/admin/items/icon-selector` — modo «Catálogo por categoría», filtro dofus → 10 iconos
- `/admin/items/new` — selector embebido
- `/admin/items/12616/edit` — cambio de IconId con preview real

Estado: `PENDING_OPERATOR_BROWSER_QA`

## PyDofus (opcional)

Audit auxiliar documentado en [pydofus-compatibility-audit.md](./pydofus-compatibility-audit.md). No es dependencia del build.
