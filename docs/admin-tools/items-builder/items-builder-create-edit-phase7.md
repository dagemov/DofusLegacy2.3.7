# Items Builder Create/Edit Phase 7

> Status correction on `2026-06-02`: `Phase 7` is now `PAUSED` in the official roadmap. This document is preserved as exploratory reference only and must not be treated as the accepted baseline in `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`.

## Snapshot

- Date: `2026-06-02`
- Branch: `feature/items-builder-create-edit-phase7`
- Base branch: `feature/items-builder-asset-pipeline-phase6`
- Scope type: exploratory write-slice reference, currently paused in the official roadmap
- Previous branch baseline: `9e32ac6 feat: add items builder preview asset pipeline`

## Goal

Describe the previously explored writable Items Builder slice so it can be revisited later after the official repo is realigned.

Phase 7 scope:

- create item
- edit item
- duplicate item
- keep `ItemId`, `IconId`, and `AppearanceId` visibly and contractually separate
- preview by `IconId` before save
- surface `409`, `422`, `500`, and `traceId`

Still out of scope:

- PNG upload
- SWF or client publish
- effects editor
- weapon writes
- mass import

## Sunshine write constraints confirmed

The current `sunshine` schema forced a narrow write slice:

- `items.Id` is **not** `AUTO_INCREMENT`
- `items.DescriptionId` is allocated from the same table, not a separate text table
- `items` uses `MyISAM`
- `items_weapons` is separate from `items`
- weapon `TypeId` values must be rejected in Phase 7
- `Description` and `IsVisible` are contract fields now, but not directly persisted by `sunshine.items`

Practical result:

- create and duplicate allocate `MAX(Id) + 1`
- create and duplicate allocate `MAX(DescriptionId) + 1`
- edit keeps the same `ItemId`
- duplicate creates a new `ItemId` and a new `DescriptionId`
- brand-new rows use safe defaults for unsupported runtime columns

## Implemented backend

Endpoints now implemented:

- `POST /api/admin/v1/items`
- `PUT /api/admin/v1/items/{itemId}`
- `POST /api/admin/v1/items/{itemId}/duplicate`
- `GET /api/admin/v1/items/preview-state?iconId=...`

Implementation notes:

- controllers stay thin
- write logic lives in `ItemsAdminWriteService`
- SQL stays in `ItemsAdminWriteRepository`
- `422` now returns field errors plus `traceId`
- `409` is reserved for generated-id conflicts
- preview lookup for the write form is intentionally by `IconId`

## Implemented Angular

Routes now implemented:

- `/admin/items/new`
- `/admin/items/:itemId/edit`
- `/admin/items/:itemId/duplicate`

UI outcomes:

- list page now exposes `Create item` and `Edit` entry points
- detail page now exposes `Edit item` and `Duplicate item`
- write form keeps the identity rule visible
- edit and duplicate preload the current runtime row
- preview card is reused and resolves from the current form `IconId`
- diagnostics panel is reused for operator-facing advisories and backend warnings
- `ProblemDetails` panel shows `traceId` and field-level errors

## Validation performed

Build validation:

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`: `OK`
- `npm run build`: `OK`

Safe API validation:

- `POST /api/admin/v1/items` with invalid payload: controlled `422`
- `PUT /api/admin/v1/items/39` with invalid weapon type: controlled `422`
- `POST /api/admin/v1/items/39/duplicate` with invalid weapon type: controlled `422`
- all three now return:
  - `traceId`
  - `errors.resolvedName`
  - `errors.typeId`

Browser validation:

- `/admin/items`: create and edit entry points visible
- `/admin/items/new`: create shell, advisory warnings, and preview card visible
- `/admin/items/39/edit`: form preloaded from live data, preview resolved by `IconId=1001`
- `/admin/items/39/duplicate`: duplicate shell preloaded and explicit new-identity advisory visible

## Validation intentionally not performed

No successful mutating write was executed against the current database during this phase.

Reason:

- the current environment can reach a live-like Sunshine runtime
- `items` is `MyISAM`
- no rollback-safe delete path exists yet for a smoke-test row
- leaving junk rows behind for validation was not acceptable

Status consequence:

- implementation: `DONE`
- destructive happy-path smoke test: `DEFERRED`
- overall phase sign-off: `PARTIAL / safe validation only`

## Remaining follow-up

Before this phase can resume officially:

1. complete `Phase 7A - Item Icon Selector`
2. replay accepted write contracts inside the official repo baseline
3. run one approved happy-path create test on a disposable or backed-up Sunshine dataset
4. confirm operator workflow around deferred `Description` and `IsVisible`

## Expected commit

- `feat: add items builder create edit workflow`
