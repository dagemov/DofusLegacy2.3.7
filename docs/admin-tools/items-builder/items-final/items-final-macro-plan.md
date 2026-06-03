# Macro Items Final — Master Plan

Date: `2026-06-03`  
Branch (7D.1): `feature/items-final-effects-catalog-audit-7d1`  
Official repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`

## Why this macro exists

Macros 1–3 and Phase 7B delivered a **working** Items Builder (CRUD, icon selector, sprite preview diagnostics, publication status, compact effects save). Operators still cannot match **Rollback.Web** item effect editing because:

1. **Add-effect catalog** is a short characteristics list (~26 IDs), not the full `GameEffectDisplayService` option set.
2. **Per-row effect picker** and **serialization kind** controls from `EffectListEditor.razor` are missing in Angular.
3. **Dice / MinMax / Duration / Creature** rows decode but are mostly read-only in the UI.
4. Roadmap marked Phase 7B `DONE` while Blazor parity audit still lists effects as `PORT_TO_ANGULAR` / `REIMPLEMENT_CLEAN`.

This macro **reopens Items Builder** only for effects/catalog parity. It does **not** replace Macro 3 (sprite preview) or start Spells.

## Success criteria (100% functional parity)

An operator on `/admin/items/:id/edit` can:

| Capability | Legacy reference | Target Admin |
| --- | --- | --- |
| Pick any item-relevant `EffectId` from grouped dropdown | `EffectListEditor` + `GameEffectDisplayService.GetOptions()` | `GET item-effects/options` returns full catalog |
| Change effect type on an existing row | `UpdateEffectId` | Same in Angular |
| Choose row format (Integer, Dice, …) | `UpdateEffectKind` + `EffectEditorKind` | Map to `serializationTypeId` with validation |
| Edit dice/min-max/duration fields | Conditional grids in `EffectListEditor` | Editable when supported by codec |
| Reorder rows | Subir / Bajar | Move up/down |
| Save round-trip | `GameEffectEditorService` + `BinaryEffects` | `SunshineItemEffectsCodec` + `items.Effects` hex |
| Spanish labels + groups | `GameEffectDisplayService` | Port labels; fallback to `EffectsEnum` name |
| Preserve unsupported tail bytes | implicit in legacy engine | Keep `preservedSuffixHex` behavior |

Out of scope for this macro (unchanged):

- Weapon item templates
- Spells / glyph / maps builders
- Client SWF publish automation
- EntityLook in-browser renderer

## Phase breakdown

### Phase 7D.1 — Item Effects Catalog Audit (`DONE`)

Deliverable: [items-effects-catalog-audit-phase7d1.md](./items-effects-catalog-audit-phase7d1.md)

- Inventory legacy vs Admin vs `EffectsEnum.cs`
- Gap table and API/UX contract targets for 7D.2–7D.3

### Phase 7D.2 — Item Effects Catalog API (`PENDING`)

**Branch suggestion:** `feature/items-final-effects-catalog-api-7d2`

| Task | Notes |
| --- | --- |
| Replace `IItemEffectsCharacteristicCatalog` with `IItemEffectsCatalog` (or extend) | Full options, not only `LegacyBlazorEffectLabelRegistry.Characteristics` |
| Source of truth | Merge `GameEffectDisplayService` label map (port to Infrastructure) + `AdminProtocolCatalog` / `EffectsEnum.cs` for names |
| Enrich `AdminEffectOptionDto` | Add `SortPriority`, `SuggestedSerializationTypeId`, `AllowedSerializationTypeIds`, `IsDiceCapable` |
| Endpoint | Keep `GET /api/admin/v1/item-effects/options`; optional `?scope=characteristics\|full` during transition |
| Tests | Contract test: option count ≥ legacy characteristic labels; every curated ID resolves |

### Phase 7D.3 — Item Effects Editor UX (`PENDING`)

**Branch suggestion:** `feature/items-final-effects-editor-ux-7d3`

| Task | Notes |
| --- | --- |
| Rename / split UI | “Agregar efecto” (full catalog) vs quick-add presets (7D.4) |
| Per-row `<select>` grouped by `group` | Mirror Blazor `optgroup` |
| Kind selector | Maps `OperatorMode` / `serializationTypeId` |
| Field grids | Integer, Dice, MinMax, Duration per row |
| Row reorder | Up/down buttons |
| Unsupported rows | Read-only + warning; no silent drop on save |

### Phase 7D.4 — Templates y presets (`PENDING`)

**Branch suggestion:** `feature/items-final-effects-templates-7d4`

- Preset bundles: “Stats PA/PM”, “Full resist %”, “Prospecting / pods”, etc.
- Optional `POST` apply-template or client-side only initially
- Must not overwrite `preservedSuffixHex` without confirmation

### Phase 7D.5 — QA end-to-end (`PENDING`)

**Branch suggestion:** `feature/items-final-effects-qa-7d5`

- Item `12616` (ADMIN TEST): add/remove AP, vitality, resist; save; reload; compare detail effects
- Item with existing dice effects: edit dice fields round-trip
- API smoke script under `Infrastructure/temporal-artifacts/` (gitignored)
- Update [items-builder-qa-checklist.md](../items-builder-qa-checklist.md)
- Operator browser checklist in handoff

## Code map (current → target)

| Layer | Current | Target |
| --- | --- | --- |
| Labels | `LegacyBlazorEffectLabelRegistry` (26 rows) | Full port of `GameEffectDisplayService._labels` + group heuristics |
| Names | `ItemEffectNameResolver` → `EffectsEnum.cs` (81 entries) | Unchanged as fallback label |
| Catalog service | `ItemEffectsCharacteristicCatalog` | `ItemEffectsCatalog` implementing full `GetOptions()` |
| API | `ItemEffectsAdminService.GetOptionsAsync` → catalog | Same surface, richer DTO |
| Angular | `ItemEffectsEditorComponent` add-characteristic only | Full `EffectListEditor` parity |

## Dependencies

- `SunshineItemEffectsCodec` — keep as single write encoder (Phase 7B)
- `legacy-reference/Rollback.Admin/Services/GameEffectDisplayService.cs` — label/group/suggest-kind source
- `legacy-reference/Rollback.Web/Components/Admin/EffectListEditor.razor` — UX reference

## Roadmap interaction

- Macro 1 Items Builder: remains `DONE` for phases 1–8 **except** effects catalog marked `PARITY_PENDING` until 7D.5 passes
- Macro 3 Sprite Preview: `COMPLETE` (no change)
- Macro 4 Spells: `DEFERRED` until Items Final closes

## Commits (expected pattern)

```txt
docs: audit item effects catalog vs Rollback.Web (7D.1)
feat: expose full item effects catalog API (7D.2)
feat: item effects editor UX parity with Blazor (7D.3)
feat: item effects templates and presets (7D.4)
docs: items final effects e2e qa (7D.5)
```
