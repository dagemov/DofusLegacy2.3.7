# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 4 / Phase 6C — Item preview massive extraction

| Campo | Valor |
| --- | --- |
| Rama | `feature/item-preview-massive-extraction-phase6c` |
| Base | `feature/item-skin-catalog-by-category-phase6b` |
| Estado | **`DONE`** (browser QA pendiente operador) |

### Entregables

- CLI: `item-preview-extract-by-category` → PNG reales desde D2P + `catalog.json` / `catalog.csv` / `gallery.html` (temporal)
- CLI: `item-preview-copy-to-angular` (`--approve-curated-copy`) → **500 PNG** en `src/assets/item-previews/by-category/`
- **Dofus completo:** 10/10 iconos en Angular
- Manifest: `docs/admin-tools/sprite-preview/item-preview-curated-copy-manifest-phase6c.{json,md}`
- API + selector: modo `by-category`, filtros categoría / ItemId / nombre ES-EN / IconId
- Armas: **0** copiadas; 95 TypeIds excluidos en catálogo
- Sin modificar `Client2.3.7`, DB, VPS; `temporal-artifacts` fuera de git

### Validación

| Check | Resultado |
| --- | --- |
| Pipeline build | OK |
| Extract 500 PNG temporal | OK |
| Copy 500 PNG Angular | OK |
| npm run build | OK |
| Browser QA | `PENDING_OPERATOR_BROWSER_QA` |

### Commits en rama

```txt
feat: extract item previews by category from d2p
feat: integrate item preview category gallery
docs: record item preview extraction phase6c
```

### Siguiente (operador / Phase 7+)

- QA browser: `/admin/items/icon-selector`, `/admin/items/new`, `/admin/items/12616/edit`
- Ampliar cupo o re-run por categoría (botas, mascotas, escudos, anillos, amuletos, cinturones, recursos)
- Macro 4 Phase 6A publish real sigue `READY_FOR_OPERATOR`

## Macro 4 / Phase 6B — Item skin catalog by category

**`DONE`** — dry-run 1925 entradas, galería HTML, mapa de categorías.

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-preview-massive-extraction-phase6c
```
