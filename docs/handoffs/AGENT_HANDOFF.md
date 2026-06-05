# Agent Handoff - Admin Tools Migration

Generated: `2026-06-05`

## Gate final — Items Builder

| Campo | Valor |
| --- | --- |
| Rama | `feature/items-preview-sets-polish-final` |
| PR target | `devp` |
| **Items Builder** | **`COMPLETE`** |
| **Spell Builder** | **`NEXT, not started`** |

### Validación gate (2026-06-05)

| Check | Resultado |
| --- | --- |
| Lock API limpiado | OK (`Stop-Process RollblackLegacy.Admin.Api`, `dotnet build-server shutdown`) |
| `dotnet build` Admin.Api | OK |
| `npm run build` | OK (warning budget +1.13 kB) |
| `dotnet build` Sunshine.sln | OK (5 warnings, 0 errors) |
| Git hygiene | OK — sin commit de `Client2.3.7/`, `OneLauncher/`, `config/`, `temporal-artifacts/` |
| Spell Builder en rama PR | **Revertido** — commits `e5f0964` y `9031339` excluidos del scope Items |

### Browser QA

| Estado | Notas |
| --- | --- |
| `PENDING_OPERATOR` | Rutas mínimas documentadas; builds OK como precondición |

Rutas:

```txt
/admin/items/new
/admin/items/12616/edit
/admin/items/icon-selector
/admin/item-sets
/admin/item-sets/:setId
/admin/publication
```

Confirmar: stats icons, preview BY_CATEGORY, sets con preview, bonos por piezas, sin errores consola críticos.

### Entregables Items (rama)

- Preview reconciliation (`BY_CATEGORY`)
- Sets read UI + bonos por piezas
- Stat icons fix (`src/assets` en `angular.json`)
- Docs: preview reconciliation, stat icons, sets builder

### Merge flow

1. PR `feature/items-preview-sets-polish-final` → `devp` (creado en gate)
2. Tras aprobación: merge a `devp`
3. Luego `devp` → `main` (no borrar ramas hasta main estable)

### Siguiente

- Abrir **Spell Builder** en rama dedicada **después** de merge Items a `devp`/`main`
- Cherry-pick o re-aplicar trabajo Spell (`e5f0964`, `9031339`) en rama `feature/spell-builder-*` separada

### Prohibiciones

- No publicar cliente real, no VPS, no temporal-artifacts en git

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/items-preview-sets-polish-final
```
