# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 5 / Phase 3 - Spell Detail API

| Campo | Valor |
| --- | --- |
| Rama | `feature/item-preview-category-expansion-phase6d` |
| Base | `feature/item-preview-massive-extraction-phase6c` |
| Estado | **`DONE`** |

### Entregables

- Endpoint `GET /api/admin/v1/spells/{spellId}`
- Contratos `SpellDetailDto`, `SpellReferenceMetadataDto`, `SpellLevelSummaryDto`
- Read models de detalle y niveles read-only
- Repositorio/reader con compatibilidad para `spells` actual y `spells_templates` legacy
- Documentacion `docs/admin-tools/spell-builder/spell-builder-phase3-detail-api.md`

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"` | `FAILED_EXTERNAL_LOCK` |
| Causa registrada | DLLs del Admin API bloqueadas por Visual Studio (`RollblackLegacy.Admin.Api`) |
| Estado de compilacion del cambio | `COMPILED_BEFORE_COPY_LOCK` |

### Siguiente

- Macro 5 / Phase 4: Spell Levels API
- Alcance esperado: contratos de nivel mas ricos, orden/consistencia de niveles y lectura dedicada sin write API

## Macro Items Final Plus - Preview + Sets + Stat icons

| Campo | Valor |
| --- | --- |
| Rama | `feature/items-preview-sets-polish-final` |
| Base | `feature/item-preview-category-expansion-phase6d` |
| Estado | **`DONE`** (browser QA pendiente operador) |

### Entregables

1. **Preview reconciliation** — `FileSystemItemPreviewStateResolver` + `ItemPreviewCategoryIndex` + fallback `BY_CATEGORY`; `typeId` en `Resolve()` para lista/detalle/sets.
2. **Sets read UI** — `GET /api/admin/v1/item-sets`, `GET /api/admin/v1/item-sets/{setId}`; Angular `/admin/item-sets`, `/admin/item-sets/:setId`; bonos por piezas con labels de `item-effects/options`.
3. **Stat icons** — `angular.json` publica `src/assets`; quick-picks con PNG reales y fallback emoji.

### Validacion

| Check | Resultado |
| --- | --- |
| `dotnet build` Application | OK |
| `dotnet build` Admin.Api | `FAILED_EXTERNAL_LOCK` si VS ejecuta `RollblackLegacy.Admin.Api` |
| `npm run build` | OK (warning budget +1.13 kB) |
| Browser QA | `PENDING_OPERATOR` — ver rutas abajo |

### Browser QA (operador)

```txt
/admin/items/new
/admin/items/12616/edit
/admin/items/icon-selector
/admin/item-sets
/admin/item-sets/:setId
```

Validar: iconos stats visibles, sin imagenes rotas, previews BY_CATEGORY en lista/detalle/sets, bonos legibles por piezas.

### Commits esperados (esta sesion)

```txt
fix: reconcile item previews from category catalog
feat: add item set previews and bonuses
fix: load item stat icons correctly
docs: record items preview and sets polish
```

### Docs

- [items-preview-reconciliation-report.md](../admin-tools/items-builder/items-preview-reconciliation-report.md)
- [items-stat-icons-fix-report.md](../admin-tools/items-builder/items-stat-icons-fix-report.md)
- [sets-builder-preview-and-bonuses.md](../admin-tools/sets-builder/sets-builder-preview-and-bonuses.md)

### Siguiente

- PR desde `feature/items-preview-sets-polish-final` hacia base de migracion acordada
- Browser QA operador
- **Spell Builder** solo tras merge + aprobacion explicita

### Prohibiciones

- No tocar cliente real, VPS, publicacion, armas, scan 44k, worktrees externos, temporal-artifacts en git

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/items-preview-sets-polish-final
```

## Macro 4 / Phase 6D (referencia)

**`DONE`** — 1916 PNG `by-category/`, manifest `categoryStats`. Ver handoff previo en historial git de este archivo.
