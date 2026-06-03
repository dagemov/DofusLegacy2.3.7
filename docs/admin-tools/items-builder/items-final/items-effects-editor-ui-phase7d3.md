# Phase 7D.3 — Item Effects Editor Parity UI

Date: `2026-06-03`  
Branch: `feature/items-final-effects-catalog-audit-7d1`  
Status: `DONE`

## Scope

Angular `ItemEffectsEditorComponent` parity with legacy `EffectListEditor.razor` (MVP). No API/codec changes.

## Delivered

| Capability | Status |
| --- | --- |
| Full catalog (`GET item-effects/options`, 507 entries) | Done |
| `AdminEffectOptionDto` fields: `format`, `sortPriority`, `operatorMode` | Done |
| Add effect: group filter + search + grouped select | Done |
| Per-row effect `<select>` (optgroup by `group`) | Done |
| Visible group + format badges | Done |
| Format editor: `Integer` / `Dice` on supported rows | Done |
| MinMax / Duration / Base / unsupported: readonly + warning | Done |
| Row reorder (↑ / ↓) | Done |
| `preservedSuffixHex` info banner | Done |
| Save via existing `PUT /items/{id}/effects` | Unchanged |

## Files

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-effects-editor.component.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.models.ts
```

## Validation

| Check | Result |
| --- | --- |
| `npm run build` | PASS (budget +589 B, pre-existing) |
| `dotnet build` Admin.Api | PASS when API process not locking output DLLs |
| API smoke `12616` | Operator: open `/admin/items/12616/edit` |

### Browser QA checklist (item 12616)

```txt
1. /admin/items/12616/edit — load effects editor
2. Confirm existing rows (API smoke 2026-06-03: item `12616` has `111` + PA only in current DB; add 128/61 via editor if needed)
3. Change one row EffectId via dropdown (e.g. add + Suerte)
4. Toggle format Integer ↔ Dice on a row; edit values
5. Reorder rows with ↑ ↓
6. Guardar efectos — expect success panel
7. Reload page — values persist
8. /admin/items/12616 — detail still lists effects
```

## Deferred to 7D.4+

- Stat templates / presets
- Modal picker for effects
- MinMax / Duration inline editing
- Format kinds beyond Integer/Dice (String, Mount, Date)

## Commits on branch

```txt
44632b8 docs: audit final item effects catalog parity
10538e8 feat: add full item effects catalog api
(pending) feat: add item effects editor parity ui
```
