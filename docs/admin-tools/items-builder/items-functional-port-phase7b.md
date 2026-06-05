# Items Functional Port — Phase 7B / 8

Date: `2026-06-02`  
Status: `DONE` (Items slice; Spells deferred)  
Branch: `feature/items-builder-vps-qa-stabilization`  
Commit: `feat: port items builder logic from legacy blazor` (this pass)

## Objective

Use legacy Blazor Items Builder as **functional source**, reimplement rules in Admin API C#, keep Angular as modern UI.

## What was ported

### Backend (`Angular-tools/Admin/`)

| Capability | Blazor reference | Implementation |
| --- | --- | --- |
| List / search items | `ItemAdminService.GetPagedAsync` | `ItemsAdminReadService` |
| Create / edit template fields | `ItemAdminService.SaveAsync` | `ItemsAdminWriteService` |
| Duplicate | (not in Blazor) | `DuplicateAsync` — extra |
| Effects read/write | `GameEffectEditorService` | `SunshineItemEffectsCodec` + `ItemEffectsAdminService` |
| Spanish effect labels | `GameEffectDisplayService` | `LegacyBlazorEffectLabelRegistry` |
| Type options | `ItemType` enum | `AdminProtocolCatalog` |
| Set options | `ItemSetId` | `item-sets/options` |
| Conditions string | `StringCriterion` | `Criteria` on write form |
| Icon catalog | manual IconId + bitmap | `item-icons` + modal 7A |

### Angular (view only)

| Screen | Role |
| --- | --- |
| `/admin/items` | Catalog + filters |
| `/admin/items/new`, `/:id/edit`, `/:id/duplicate` | Write form |
| `/admin/items/:id/edit` | `ItemEffectsEditorComponent` |
| `/admin/items/icon-selector` | Icon modal route |

### Explicitly not ported

- `ItemClientPublishService` (SWF/i18n/client PNG publish)
- `ItemAppearanceResolverService` auto-patch
- Full identity audit panels
- Weapon templates
- Spells / Glyphs admin

## Architecture proof

```txt
LegacyBlazorEffectLabelRegistry  → Infrastructure
SunshineItemEffectsCodec         → Infrastructure
ItemEffectsAdminService          → Application
ItemEffectsAdminController       → Api
ItemEffectsEditorComponent       → Angular (DTOs only)
```

## Validation checklist (ItemId 12616)

| Step | Expected |
| --- | --- |
| Open `/admin/items/12616/edit` | Form + effects panel load |
| Add + PA, + PM, + Vitalidad | Rows in Principales/Stats |
| Guardar efectos | `PUT` 200, hex updated |
| Reload detail | Effects visible in detail card |
| Preview icon | `IconId` 1003 path resolves if PNG exists |
| Create new test item | Optional; uses empty `0000` effects |

## Builds

```bash
dotnet build Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj
npm run build  # RollblackLegacy.Admin.Angular
```

## Related docs

- [blazor-functional-port-map.md](./blazor-functional-port-map.md)
- [items-effects-port-map.md](./items-effects-port-map.md)
- [items-builder-effects-serialization-audit.md](./items-builder-effects-serialization-audit.md)

## Next after Items

1. Phase 7C — form UX polish (numeric display, conditions hints)
2. Phase 7D — client sprite preview extraction (docs)
3. Phase 8 — publish / QA workflow
4. **Spells** — only after Items sign-off
