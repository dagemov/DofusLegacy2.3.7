# Items Effects Port Map — Blazor → Admin API

Date: `2026-06-02`

## Serialization decision

| Layer | Blazor legacy | Sunshine Admin (official) |
| --- | --- | --- |
| Column | `items_templates.BinaryEffects` | `sunshine.items.Effects` |
| Encoder | `EffectManager.SerializeEffects` | `ObjectEffectSerializer` / `SunshineItemEffectsCodec` |
| Wire in API | N/A (Blazor server) | Hex string |

**Rule:** Port **UX and effect IDs** from Blazor; port **bytes** from Sunshine audit.

---

## Class mapping

| Blazor | Admin (C#) | Angular |
| --- | --- | --- |
| `GameEffectEditorService.Deserialize` | `IItemEffectsCodec.Decode` | — |
| `GameEffectEditorService.Serialize` | `IItemEffectsCodec.Encode` | — |
| `GameEffectDisplayService.GetOptions` | `LegacyBlazorEffectLabelRegistry` + `ItemEffectsCharacteristicCatalog` | dropdown agregar |
| `GameEffectDisplayService.GetDisplayName` | `IItemEffectNameResolver` + registry label | fila label |
| `GameEffectDisplayService.ResolveGroupLabel` | `LegacyBlazorEffectLabelRegistry.ResolveGroup` | grupos UI |
| `EffectListEditor.razor` | — | `ItemEffectsEditorComponent` |
| `GameEffectEditRow` | `ItemEffectEditDto` / `ItemEffectEditRowRequest` | form rows |

---

## Effect row shape

| Blazor `GameEffectEditRow` | Admin `ItemEffectEditDto` | Sunshine typeId |
| --- | --- | --- |
| `EffectId` | `effectId` | `actionId` in blob |
| `Kind=Integer` | `operatorMode=Integer` | `70` |
| `Value` | `value` | int16 payload |
| `Kind=Dice` | `diceNum`, `diceSide`, `value` | `73` |
| `Kind` Duration/Date/Mount | preserved opaque | not editable 7B |

---

## Characteristic effect IDs (ported labels)

Grupos Blazor: **Principales**, **Stats**, **Resistencias**, **Combate**, **Especiales**.

| EffectId | Label (ES) | Grupo | Blazor `EffectId` |
| --- | --- | --- | --- |
| 111 | + PA | Principales | `Effect_AddAP111` |
| 168 | - PA | Principales | `Effect_SubAP` |
| 128 / 19 | + PM | Principales | `Effect_AddMP128` / `Effect_AddMP` |
| 61 | + Vitalidad | Stats | `Effect_AddVitality` |
| 54–62 | +/- stats | Stats | Strength, Int, Chance, Agi, Wisdom |
| 210–214 | + % Resistencia * | Resistencias | `Effect_Add*ResistPercent` |
| 51, 118 | Crítico / Daños | Combate | — |
| 93 | - Pods | Especiales | `Effect_DecreaseWeight` |

Registry: `LegacyBlazorEffectLabelRegistry.cs`

---

## API endpoints (Angular consumes)

| Endpoint | Blazor equivalent |
| --- | --- |
| `GET /items/{id}/effects/edit` | `LoadItemAsync` + deserialize effects |
| `PUT /items/{id}/effects` | `SaveAsync` effects section only |
| `GET /item-effects/options` | `GameEffectDisplayService.GetOptions` |

---

## Unsupported preservation

Blazor abortaba tipos raros en UI. Admin Phase 7B:

1. Decode stops at unknown `typeId`
2. Store `preservedEffectHex` + `preservedSuffixHex`
3. PUT re-appends suffix unless operator clears via `removedUnsupportedRowIds`

---

## Angular responsibilities (view only)

```txt
- Render groups (Principales/Stats/...)
- Add row from options API
- Edit integer value
- Confirm delete unsupported
- PUT DTO payload
- Show traceId on 422
```

No hex math in TypeScript.

---

## QA

Item `12616` — add +PA, +PM, +Vitalidad → save → reload `GET /items/12616` effects list.
