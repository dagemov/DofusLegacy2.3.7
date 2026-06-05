# Phase 7B — Effects / Characteristics Editor

Date: `2026-06-02`  
Status: `DONE`  
Branch: `feature/items-builder-vps-qa-stabilization`

## Delivered

### Backend

- `SunshineItemEffectsCodec` — ObjectEffect hex encode/decode
- `ItemEffectsCharacteristicCatalog` — curated AP/PM/stats/resists
- Endpoints:
  - `GET /api/admin/v1/items/{itemId}/effects/edit`
  - `PUT /api/admin/v1/items/{itemId}/effects`
  - `GET /api/admin/v1/item-effects/options`
- `ItemEffectsAdminService` + `ItemEffectsAdminRepository`

### Angular

- `ItemEffectsEditorComponent` on `/admin/items/:id/edit`
- Grouped table (Core / Combat / Resistances / Other)
- Add characteristic, edit integer value, remove row, save effects
- Unsupported rows preserved with confirm on delete

## Out of scope (unchanged)

- Weapons (`items_weapons`)
- Client / SWF / D2P
- Description / IsVisible publish
- Phase 7D sprite extraction

## Validation

```bash
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj"
npm run build  # under RollblackLegacy.Admin.Angular
```

Browser: `/admin/items/12616/edit` → add PA/Vitality → Guardar efectos → reload detail.

## Related

- [items-builder-effects-serialization-audit.md](./items-builder-effects-serialization-audit.md)
- [items-builder-effects-editor-plan.md](./items-builder-effects-editor-plan.md)
