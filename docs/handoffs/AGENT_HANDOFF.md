# Agent Handoff - Admin Tools Migration

Generated: `2026-06-06`

## Items + Sets — Production acceptance test

| Campo | Valor |
| --- | --- |
| Rama | **`feature/items-sets-production-acceptance-test`** |
| Estado | **`PARTIAL / OPERATOR_REQUIRED`** |
| Resultado QA in-game | **`NOT_RUN`** |
| Create item + effects (un solo flujo) | **`PASS`** (código) |
| Backup cliente local | **`OK`** — `pre_creation_new_items_20260605_0732` |
| Backup DB VPS | **`OK`** — `/root/backups/sunshine-focused-20260606-004715.sql` |
| Backup VPS inventario | **`OK`** — `backups/vps/20260606-004658/` |
| SSH | **`OK`** — `SSH/private_key_sebas.pem` (gitignored) |

### Documentación

- [items-sets-production-acceptance-test.md](../admin-tools/items-builder/items-final/items-sets-production-acceptance-test.md)
- [sets-production-acceptance-test.md](../admin-tools/sets-builder/sets-production-acceptance-test.md)
- [itemsets-client-publication-plan.md](../admin-tools/client-publication/itemsets-client-publication-plan.md)
- [items-final-production-backup.md](../admin-tools/items-builder/items-final-production-backup.md)

### Bloqueadores

| Bloqueo | Estado |
| --- | --- |
| `ItemSets.d2o` staging automático | **PARTIAL** — apply/validate opcional si archivo en paquete |
| Varita de la Flor | **`BLOCKED_WEAPON_PUBLICATION`** |
| `Items.d2o` local en repo | ausente — publish requiere cliente operador |
| QA in-game | **`OPERATOR_REQUIRED`** |

### Si PASS (operador)

```txt
Items Builder = COMPLETE
Sets Builder = COMPLETE
Publication Pipeline = COMPLETE (con nota ItemSets)
Siguiente = Combat Sanitization
```

**No** abrir Spell Builder hasta PASS documentado.

## Macro 5 / Git Sanity Before Phase 4

| Campo | Valor |
| --- | --- |
| Estado | **`DOC_ONLY_AUDITED`** |
| Rama activa verificada | `feature/sets-builder-crud-and-pagination` |
| Commit de HEAD | `8ced8f6` |
| Scope Spell Builder en tree actual | **`REVERTED_FROM_ITEMS_TREE`** |

### Diagnostico rapido

- La rama activa real es `feature/sets-builder-crud-and-pagination`.
- `feature/sets-builder-crud-and-pagination` y `feature/items-preview-sets-polish-final` apuntan al mismo commit `8ced8f6`.
- El worktree actual esta sucio con cambios de Items/Sets/Launcher/Client/config/scripts.
- Los commits Spell Builder confirmados son:
  - `ccfcb8a` -> Phase 1 docs
  - `e5f0964` -> Phase 2 catalog api
  - `9031339` -> Phase 3 detail api
- En la historia de Items ya existen los reverts:
  - `dd0f287` -> revert de `9031339`
  - `8ced8f6` -> revert de `e5f0964`
- Conclusion:
  - la rama de Items ya no conserva el contenido Spell en el tree actual
  - pero si conserva la historia de Spell Builder y sus reverts

### Siguiente recomendado

1. No iniciar Phase 4 todavia.
2. Resolver saneamiento Git con aprobacion humana.
3. Preservar rama dedicada de Spell Builder fuera de las ramas de Items.
4. Reabrir Spell Builder sobre base limpia acordada antes de continuar.

## Gate Final - Items Builder

| Campo | Valor |
| --- | --- |
| Rama base gate | `feature/items-preview-sets-polish-final` |
| Rama activa real al 2026-06-05 | `feature/sets-builder-crud-and-pagination` |
| PR target | `devp` |
| **Items Builder** | **`COMPLETE`** |
| **Spell Builder** | **`PENDIENTE DE SANEAMIENTO GIT`** |

### Validacion gate (2026-06-05)

| Check | Resultado |
| --- | --- |
| Lock API limpiado | OK (`Stop-Process RollblackLegacy.Admin.Api`, `dotnet build-server shutdown`) |
| `dotnet build` Admin.Api | OK |
| `npm run build` | OK (warning budget +1.13 kB) |
| `dotnet build` Sunshine.sln | OK (5 warnings, 0 errors) |
| Git hygiene | OK en gate Items; el worktree actual volvio a ensuciarse con cambios locales de Items/Sets |
| Spell Builder en rama PR | **Revertido** - commits `e5f0964` y `9031339` excluidos del tree de Items |

### Browser QA

| Estado | Notas |
| --- | --- |
| `PENDING_OPERATOR` | Rutas minimas documentadas; builds OK como precondicion |

Rutas:

```txt
/admin/items/new
/admin/items/12616/edit
/admin/items/icon-selector
/admin/item-sets
/admin/item-sets/:setId
/admin/publication
```

Confirmar: stats icons, preview BY_CATEGORY, sets con preview, bonos por piezas, sin errores de consola criticos.

### Entregables Items (rama)

- Preview reconciliation (`BY_CATEGORY`)
- Sets read UI + bonos por piezas
- Stat icons fix (`src/assets` en `angular.json`)
- Docs: preview reconciliation, stat icons, sets builder

### Merge flow

1. PR `feature/items-preview-sets-polish-final` -> `devp`
2. Tras aprobacion: merge a `devp`
3. Luego `devp` -> `main` (no borrar ramas hasta main estable)
4. Solo despues de eso, abrir rama dedicada `feature/spell-builder-*`

### Prohibiciones

- No publicar cliente real
- No tocar VPS
- No versionar `temporal-artifacts/`
- No iniciar Phase 4 en `feature/sets-builder-crud-and-pagination`

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/sets-builder-crud-and-pagination
```
