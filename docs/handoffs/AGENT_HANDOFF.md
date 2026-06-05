# Agent Handoff - Admin Tools Migration

Generated: `2026-06-05`

## Sets Builder CRUD + pagination

| Campo | Valor |
| --- | --- |
| Rama | `feature/sets-builder-crud-and-pagination` |
| Base | `feature/items-preview-sets-polish-final` |
| **Sets Builder** | **`DONE`** (browser QA pending) |
| **Items Builder** | **`COMPLETE`** |
| **Spell Builder** | **`NEXT only after merge`** |

### Entregables

1. Fix loading infinito en `/admin/item-sets` — paginación API + `NgZone`/`ChangeDetectorRef`.
2. `GET /api/admin/v1/item-sets` paginado con filtros nombre/nivel/partes.
3. CRUD `POST/PUT/DELETE /api/admin/v1/item-sets`.
4. Angular: `/admin/item-sets`, `/new`, `/:setId`, `/:setId/edit` + bonus editor por piezas.

### Validación

| Check | Resultado |
| --- | --- |
| `dotnet build` Admin.Api | OK |
| `npm run build` | OK (budget warning +1.36 kB) |
| Browser QA | `PENDING_OPERATOR` |

### Browser QA

```txt
/admin/item-sets
/admin/item-sets/new
/admin/item-sets/:setId
/admin/item-sets/:setId/edit
```

### Docs

- [sets-builder-crud-pagination.md](../admin-tools/sets-builder/sets-builder-crud-pagination.md)
- [sets-builder-bonus-editor.md](../admin-tools/sets-builder/sets-builder-bonus-editor.md)

### Siguiente

- Merge `feature/sets-builder-crud-and-pagination` tras QA
- Spell Builder en rama dedicada tras cierre Items

### Prohibiciones

- No Spell Builder, cliente real, VPS, publicación, armas, temporal-artifacts en git

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/sets-builder-crud-and-pagination
```
