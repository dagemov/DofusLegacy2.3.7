# Phase 7D.1 — Item Effects Catalog Audit

Date: `2026-06-03`  
Branch: `feature/items-final-effects-catalog-audit-7d1`  
Auditor: Admin migration agent (docs-only phase)

## Executive summary

Phase 7B shipped **encode/decode + save** for `sunshine.items.Effects` and a **minimal characteristics picker** (~26 effect IDs). Rollback.Web exposes **every `EffectId`** in a grouped dropdown with per-row **kind** editing (Integer, Dice, String, Duration, Date, Mount). Admin Angular today only supports **adding** curated stats and **editing integer values** on supported rows.

**Verdict:** Items Builder is **not** at 100% Blazor functional parity for effects. Macro Items Final phases 7D.2–7D.5 are required before Spells migration.

## Sources compared

| Source | Role | Location |
| --- | --- | --- |
| Legacy UI | Operator effect rows | `legacy-reference/Rollback.Web/Components/Admin/EffectListEditor.razor` |
| Legacy catalog | Labels, groups, suggested kind | `legacy-reference/Rollback.Admin/Services/GameEffectDisplayService.cs` |
| Legacy serialize | Binary blob read/write | `legacy-reference/Rollback.Admin/Services/GameEffectEditorService.cs` |
| Sunshine protocol names | Effect action id → enum name | `Sunshine.Protocol/Enums/EffectsEnum.cs` (81 values) |
| Admin catalog (current) | Options for “Agregar característica” | `LegacyBlazorEffectLabelRegistry` + `ItemEffectsCharacteristicCatalog` |
| Admin codec | Hex ObjectEffect | `SunshineItemEffectsCodec` |
| Admin API | Edit + options | `GET/PUT .../items/{id}/effects`, `GET item-effects/options` |
| Admin UI | Editor panel | `item-effects-editor.component.*` |

## Catalog size comparison

| Catalog | Count | Notes |
| --- | ---: | --- |
| Legacy `GameEffectDisplayService.GetOptions()` | **All `EffectId` enum values** | Built from `Enum.GetValues<EffectId>()`; ~115 explicit Spanish labels in `_labels`, remainder uses PascalCase heuristic |
| Admin `GET item-effects/options` | **26** | Only `LegacyBlazorEffectLabelRegistry.Characteristics` |
| `EffectsEnum.cs` (Sunshine.Protocol) | **81** | Used by `ItemEffectNameResolver` for display names on decoded rows, **not** exposed as add-options |
| Phase 7B doc table | **~12 listed** | Understates registry; registry actually has 26 IDs |

### Curated characteristics (Admin today)

From `LegacyBlazorEffectLabelRegistry.cs`:

| EffectId | Label | Group |
| ---: | --- | --- |
| 111 | + PA | Principales |
| 168 | - PA | Principales |
| 128, 19 | + PM | Principales |
| 61 | + Vitalidad | Stats |
| 153 | - Vitalidad | Stats |
| 54, 157 | Fuerza +/- | Stats |
| 62, 155 | Inteligencia +/- | Stats |
| 59, 152 | Suerte +/- | Stats |
| 55, 154 | Agilidad +/- | Stats |
| 60, 156 | Sabiduría +/- | Stats |
| 53 | + Alcance | Principales |
| 51 | + Golpes críticos | Combate |
| 118 | + Daños | Combate |
| 114 | + Invocaciones | Especiales |
| 93 | - Pods | Especiales |
| 210–214 | + % Resist (5 elements) | Resistencias |

**Not in curated list but common on gear:** initiative, prospection, heal bonus, lock/dodge, fixed resists, damage %, spell learn, etc. — all available in legacy dropdown via full enum.

## Functional gap matrix

| Feature | Legacy (`EffectListEditor`) | Admin Angular (Phase 7B) | Gap severity |
| --- | --- | --- | --- |
| Full effect dropdown (grouped) | Yes — all `GameEffectOption` | No — 26-item add list only | **Critical** |
| Change `EffectId` on existing row | Yes — `<select>` per row | No — label read-only | **Critical** |
| Serialization kind selector | Yes — `EffectEditorKind` | No — fixed per add; opaque for others | **High** |
| Integer value edit | Yes | Yes — `operatorMode === 'Integer'` only | Partial |
| Dice (`diceNum`, `diceSide`, const) | Yes | Decode only; UI readonly | **High** |
| MinMax | Yes | Decode only; UI readonly | **High** |
| Duration / Date | Yes | Partial decode; readonly | **Medium** |
| String / Mount kinds | Yes | Not in Sunshine codec scope | **Medium** (document exclude or phase 2 codec) |
| Row reorder | Subir / Bajar | Missing | **Medium** |
| Random weight field | Yes | Missing | **Low** (confirm item template need) |
| Add row default kind | `SuggestKind` + dice detection | Always Integer on add | **High** |
| Spanish label | `GameEffectDisplayService` | Curated subset; else protocol name | **High** for unlabeled IDs |
| Group label | Heuristic + label map | Curated + heuristic on decode only | **Medium** |
| Save payload | `BinaryEffects` via `EffectManager` | `Effects` hex via `SunshineItemEffectsCodec` | Different storage — **accepted**; round-trip must hold |
| Unsupported bytes | Engine-dependent | `preservedSuffixHex` | Parity OK if UI warns |

## API contract gap (`AdminEffectOptionDto`)

Current record (`RollblackLegacy.Admin.Contracts`):

```txt
EffectId, Label, ProtocolName, Group, DefaultSerializationTypeId, OperatorMode, IsCharacteristic, IsSupported
```

Legacy `GameEffectOption` also provides:

```txt
SortPriority, SuggestedKind (EffectEditorKind), GroupLabel
```

**7D.2 recommendation:** extend DTO (non-breaking additions):

| Field | Purpose |
| --- | --- |
| `SortPriority` | Match legacy ordering in dropdown |
| `SuggestedSerializationTypeId` | Default type when adding row |
| `AllowedSerializationTypeIds` | Populate kind `<select>` |
| `IsDiceDefault` | Mirror `EffectManager.IsDiceEffect` where ported |
| `CatalogScope` | `characteristic` \| `full` (optional filter) |

Keep `IsCharacteristic` for quick filters / presets (7D.4).

## UX gap (screens)

| Surface | Legacy | Angular |
| --- | --- | --- |
| Add button | “Agregar efecto” — opens full catalog row | “Agregar característica” — limited select |
| Row header | Effect select + move + delete | Static label + delete only |
| Values | Grid per kind | Single integer input or readonly preview |
| Phase label | n/a | Still shows “Phase 7B” — update in 7D.3 |

## Serialization alignment (no change required for 7D.1)

Phase 7B audit remains valid:

- Write path: `SunshineItemEffectsCodec` / ObjectEffect types `70`, `73`, `71`, `75`, `74`, `76`, `82`
- Read path preserves unsupported effect as opaque + suffix

**7D.3 must not** reintroduce client-side hex editing.

## Label port strategy (for 7D.2)

1. **Copy** `_labels` dictionary from `GameEffectDisplayService.cs` into Infrastructure (e.g. `LegacyGameEffectLabelRegistry`) as data, not a runtime dependency on `Rollback.World`.
2. **Generate** full option list: union of `EffectsEnum.cs` keys + legacy enum ids not in Sunshine file (if any discovered during 7D.2).
3. **Fallback label:** same PascalCase split as legacy `GetDisplayName` when no curated label.
4. **Group:** reuse `ResolveGroupLabel` heuristics from legacy service.
5. **Suggested type:** port `SuggestKind` / dice detection from legacy `EffectManager.IsDiceEffect` equivalent — if no World reference in Admin, duplicate minimal dice-effect id set from legacy or protocol docs.

## Items explicitly deferred (not 7D blockers)

| Item | Reason |
| --- | --- |
| `StringCriterion` visual builder | Separate from effects catalog; raw string already editable elsewhere |
| `APCost` / range weapon fields | Non-weapon scope unchanged |
| Client publish / SWF | Phase 8 / future lane |
| Spells builder | User prohibition |

## Risk register updates

See `R35` in [admin-tools-migration-risk-register.md](../../migration/admin-tools-migration-risk-register.md).

## Acceptance criteria for Phase 7D.1

- [x] Documented legacy vs Admin catalog counts
- [x] Gap matrix for API + UX + serialization kinds
- [x] Target contracts and file map for 7D.2–7D.5
- [x] Macro plan + roadmap/handoff pointers

## Next action (7D.2)

Implement `ItemEffectsCatalog` with full options endpoint; branch `feature/items-final-effects-catalog-api-7d2`; do not start Spells.
