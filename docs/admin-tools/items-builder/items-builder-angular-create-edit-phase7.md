# Items Builder Angular Create/Edit Phase 7

## Snapshot

- Date: `2026-06-02`
- Branch: `feature/items-builder-create-edit-phase7`
- Workspace: `Angular-tools/Admin/RollblackLegacy.Admin.Angular`
- Status: `DONE / LIVE`

## Routes

Implemented:

- `/admin/items/new`
- `/admin/items/:itemId/edit`
- `/admin/items/:itemId/duplicate`

Entry points visible from:

- `/admin/items`
- `/admin/items/:itemId`

## Main component

Phase 7 uses:

```txt
src/app/admin/items/item-write-page.component.ts
src/app/admin/items/item-write-page.component.html
src/app/admin/items/item-write-page.component.scss
```

The page handles three modes:

- `create`
- `edit`
- `duplicate`

## Reused feature pieces

Still reused instead of reinvented:

- `items.api.ts`
- `items.facade.ts`
- `items.models.ts`
- `api-problem-panel.component.ts`
- `item-preview-card.component.ts`
- `item-diagnostic-panel.component.ts`
- `item-icon-selector.component.ts`

Reason:

- keep the controller and service contract honest
- keep `traceId` handling identical
- keep preview and diagnostics behavior consistent with the read-only slice

## Frontend behavior

Implemented:

- load type options
- load item set options
- preload existing item row for edit and duplicate
- preview current form state by `IconId`
- show safe operator advisories before save
- show backend warnings after save
- show `ValidationProblemDetails.errors` by field
- show `traceId` when backend rejects the request
- redirect to detail after successful save
- preserve form values on failure
- provide `Cancel`

## ItemIconSelector integration

The write form now embeds the selector instead of forcing blind `IconId` entry.

Supported flow:

- operator clicks `Choose icon`
- embedded selector opens
- operator selects a PNG-backed icon
- form updates `IconId`
- preview refreshes immediately
- current logical preview path is shown

The selector also still supports the full route:

- `/admin/items/icon-selector`

## Important UX rules

1. `ItemId`, `IconId`, and `AppearanceId` stay visibly separate.
2. Preview is evaluated by `IconId` in the write form.
3. `description` and `isVisible` are labeled as deferred, not silently discarded.
4. Weapon types are not hidden from the dropdown, but the backend rejects them with a clear `422`.
5. Duplicate mode explains that a new `ItemId` and `DescriptionId` will be allocated.

## Browser validation outcome

Validated visually in the official Angular app:

- `/admin/items/new`
  - route loads from the official repo workspace
  - embedded icon selector opens
  - selecting `1001.png` updates `IconId` to `1001`
  - preview switches to `FOUND`
  - current preview path shows `/assets/item-previews/by-icon/1001.png`
- `/admin/items/39/edit`
  - live data preloads successfully
  - `ResolvedName` is populated
  - `TypeId=1` and `IconId=1001` stay visible
  - preview renders as `FOUND`
- `/admin/items/39/duplicate`
  - source data preloads successfully
  - duplicate advisory explains new identities
  - preview stays resolved by `IconId`

## Validation split

Browser validation focused on:

- route load
- live data preload
- icon selection behavior
- preview state rendering
- diagnostics rendering

Actual create, update, and duplicate mutations were validated through the live Admin API with immediate cleanup, not through browser submit automation, to keep the smoke test controlled and reversible.

## Deferred follow-up

Still for later:

- publish workflow
- manual PNG upload
- `Description` publish flow
- `IsVisible` persistence strategy
- weapon-specific workflow
