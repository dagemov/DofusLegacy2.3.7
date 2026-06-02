# Items Builder — Effects Serialization Audit (Phase 7B.0)

Date: `2026-06-02`  
Branch: `feature/items-builder-vps-qa-stabilization`

## Where effects are stored

| Layer | Field | Format |
| --- | --- | --- |
| Sunshine runtime | `sunshine.items.Effects` | Hex string (uppercase, no separators) |
| Legacy Blazor | `items_templates.BinaryEffects` | Binary blob via `EffectManager.SerializeEffects` |

Phase 7B writes **only** `sunshine.items.Effects`.

## Canonical Sunshine item format (ObjectEffect)

Aligned with `Sunshine.WorldServer/Game/Items/ObjectEffectSerializer.cs` and Dofus protocol types under `Sunshine.Protocol/Types/.../effects/`.

```txt
[int16 effectCount]
repeat count times:
  [int16 serializationTypeId]
  [int16 actionId / effectId]
  payload depends on serializationTypeId
```

### Supported serialization types (Phase 7B)

| TypeId | Protocol class | Payload |
| --- | --- | --- |
| `70` | `ObjectEffectInteger` | `int16 value` |
| `73` | `ObjectEffectDice` | `int16 diceNum`, `int16 diceSide`, `int16 diceConst` |
| `71` | `ObjectEffectCreature` | `int16 monsterFamilyId` |
| `75` | `ObjectEffectDuration` | `int16 days`, `int16 hours`, `int16 minutes` |
| `74` | legacy read alias | decoded like `75` (compat) |
| `76` | `ObjectEffect` base | no extra fields |
| `82` | `ObjectEffectMinMax` | `int16 min`, `int16 max` |

Empty payload: hex `0000` (count = 0).

### Legacy alternate format (read-only awareness)

`EffectManager.GetEffects(string)` in WorldServer uses a **different** per-effect layout (`uint` id + long tail). Item templates loaded from D2O may still use that path at runtime load, but **Admin read/write for `items.Effects` uses ObjectEffectSerializer shape** above (same as `AdminProtocolCatalog` decode cases).

Do not mix encoders.

## Parse / serialize implementation (Admin)

| Component | Role |
| --- | --- |
| `SunshineItemEffectsCodec` | Encode/decode ObjectEffect hex |
| `ItemEffectsCodecAdapter` | Application port |
| `AdminProtocolCatalog.DecodeItemEffects` | Detail view decode (delegates to codec) |
| `ItemEffectsAdminRepository` | `UPDATE items SET Effects = @Effects` |

Unsupported type mid-stream: current effect preserved as opaque hex; trailing bytes kept in `preservedSuffixHex`.

## Effect IDs for operator characteristics (catalog)

Curated in `ItemEffectsCharacteristicCatalog` (labels for UI):

| EffectId | Label | Group |
| --- | --- | --- |
| `111` | AP / PA | Combat stats |
| `128` | MP / PM | Combat stats |
| `61` | Vitality | Core stats |
| `60` | Wisdom | Core stats |
| `54` | Strength | Core stats |
| `62` | Intelligence | Core stats |
| `59` | Chance | Core stats |
| `55` | Agility | Core stats |
| `51` | Critical Hit | Combat stats |
| `53` | Range | Combat stats |
| `210-214` | Resist % (elements) | Resistances |

Names resolved from `EffectsEnum.cs` via `AdminProtocolCatalog`.

## What Blazor did

| Piece | Behavior |
| --- | --- |
| `GameEffectEditorService` | `EffectManager.DeserializeEffects` / `SerializeEffects` on **binary blob** |
| UI rows | Integer, Dice, String, Duration, Date, Mount kinds |
| AP/PM/stats | Effect action IDs inside serialized rows |

## What Angular must do (Phase 7B)

- Render/edit rows returned by `GET /api/admin/v1/items/{id}/effects/edit`
- Persist via `PUT /api/admin/v1/items/{id}/effects`
- Load presets from `GET /api/admin/v1/item-effects/options`
- **No** client-side hex serialization
- Confirm destructive removal of unsupported rows

## Minimum validations

| Layer | Rule |
| --- | --- |
| API | `effectId > 0`, non-negative numeric fields, weapon types blocked |
| API | Unsupported suffix preserved unless explicitly cleared |
| API | Only `items.Effects` column updated on PUT |
| UI | Single save in flight, field errors via ProblemDetails |

## QA item

```txt
ItemId 12616 — ADMIN TEST (Amulette, IconId 1003, AppearanceId 1004)
```

Validate AP/PM/Vitality add → save → reload detail effects list.
