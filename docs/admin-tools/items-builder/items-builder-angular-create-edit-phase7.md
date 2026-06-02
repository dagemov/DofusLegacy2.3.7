# Items Builder Angular Create/Edit Phase 7

> Status correction on `2026-06-02`: `Phase 7` is now `PAUSED` in the official roadmap. This document is preserved as exploratory reference only and must not be treated as the accepted baseline in `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`.

## Snapshot

- Date: `2026-06-02`
- Branch: `feature/items-builder-create-edit-phase7`
- Workspace: `src/Admin/RollblackLegacy.Admin.Angular`
- Scope type: exploratory writable form workflow, currently paused in the official roadmap

## Routes

Implemented:

- `/admin/items/new`
- `/admin/items/:itemId/edit`
- `/admin/items/:itemId/duplicate`

Entry points now visible from:

- `/admin/items`
- `/admin/items/:itemId`

## Main component

Phase 7 adds:

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

Reason:

- keep the controller/service contract honest
- keep `traceId` handling identical
- keep preview/debug behavior consistent with read-only detail

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

## Important UX rules

1. `ItemId`, `IconId`, and `AppearanceId` stay visibly separate.
2. Preview is evaluated by `IconId` in the write form.
3. `description` and `isVisible` are labeled as deferred, not silently discarded.
4. Weapon types are not hidden from the dropdown, but the backend rejects them with a clear `422`.
5. Duplicate mode explains that a new `ItemId` and `DescriptionId` will be allocated.

## Browser validation outcome

Validated visually:

- list page shows `Create item`
- list rows show `Edit`
- detail page shows `Edit item` and `Duplicate item`
- edit route preloads `itemId=39`
- duplicate route preloads `itemId=39`
- preview for `IconId=1001` renders as `FOUND`

Known limitation during browser automation:

- the in-app browser runtime in this session could inspect routes reliably, but text entry automation was limited by its clipboard/input bridge
- because of that, live browser validation focused on route load, preloaded values, preview state, and advisory rendering
- actual payload rejection was validated safely through direct HTTP calls instead

## Deferred follow-up

Still for later:

- `Phase 7A - Item Icon Selector`
- successful destructive smoke test on a safe DB copy
- manual PNG upload
- description publish flow
- `IsVisible` persistence strategy
- weapon-specific workflow
