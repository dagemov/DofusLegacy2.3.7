# Items Builder Create/Edit Phase 7

## Snapshot

- Date: `2026-06-02`
- Branch: `feature/items-builder-create-edit-phase7`
- Base branch: `feature/items-builder-icon-selector-phase7a`
- Workspace:
  - `Angular-tools/Admin/RollblackLegacy.Admin.Api`
  - `Angular-tools/Admin/RollblackLegacy.Admin.Angular`
- Status: `DONE / LIVE`
- Next phase: `Phase 8 - Publish / QA Workflow`

## Goal

Implement official create, edit, and duplicate workflows for non-weapon items inside the official repo while keeping the identity rule explicit:

```txt
ItemId != IconId != AppearanceId
```

Phase 7 scope:

- create item
- edit item
- duplicate item
- preview by `IconId` before save
- integrate `ItemIconSelector`
- surface `422`, `409`, `500`, and `traceId`

Still out of scope:

- PNG upload
- client publish
- full effects editor
- vendor or set editors
- weapon writes in `items_weapons`
- mass asset import

## Write schema audit

Validated against local `sunshine` on `2026-06-02`.

### Table identity

- Table: `sunshine.items`
- Primary key: `Id`
- Engine: `MyISAM`
- `AUTO_INCREMENT`: `NO`

### Relevant columns

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | `int(11)` | `NO` | Runtime item identity |
| `Weight` | `int(10) unsigned` | `NO` | Persisted directly |
| `Name` | `mediumtext` | `NO` | Phase 7 `ResolvedName` maps here |
| `TypeId` | `int(10) unsigned` | `NO` | Weapon `TypeId` values blocked |
| `DescriptionId` | `int(10) unsigned` | `NO` | Allocated internally, not free text |
| `IconId` | `int(11)` | `NO` | Inventory/admin preview identity |
| `Level` | `int(10) unsigned` | `NO` | Persisted directly |
| `Cursed` | `tinyint(4)` | `NO` | Safe default on create |
| `UseAnimationId` | `int(11)` | `NO` | Safe default on create |
| `Usable` | `tinyint(4)` | `NO` | Persisted directly |
| `Targetable` | `tinyint(4)` | `NO` | Persisted directly |
| `Price` | `float(16,2)` | `NO` | Persisted directly |
| `TwoHanded` | `tinyint(4)` | `NO` | Persisted directly |
| `Etheral` | `tinyint(4)` | `NO` | Persisted directly |
| `ItemSetId` | `int(11)` | `NO` | `-1` when no set is linked |
| `Criteria` | `mediumtext` | `YES` | Phase 7 writes normalized conditions |
| `HideEffects` | `tinyint(4)` | `NO` | Safe default on create |
| `AppearanceId` | `int(10) unsigned` | `NO` | Equipped/runtime appearance identity |
| `RecipeIdsCSV` | `mediumtext` | `NO` | Safe default on create |
| `FavoriteSubAreasCSV` | `mediumtext` | `NO` | Safe default on create |
| `BonusIsSecret` | `tinyint(4)` | `NO` | Safe default on create |
| `FavoriteSubAreasBonus` | `int(11)` | `NO` | Safe default on create |
| `Effects` | `longtext` | `YES` | Create defaults to `0000` |

### Related tables

- `sunshine.items_sets`
- `sunshine.items_weapons`

### Practical write consequences

- `Id` must be allocated manually as `MAX(Id) + 1`
- `DescriptionId` must be allocated manually as `MAX(DescriptionId) + 1`
- `Description` free text is accepted by the API contract but not persisted yet
- `IsVisible` is accepted by the API contract but `sunshine.items` has no direct column for it
- weapon creation remains blocked because weapon specifics live outside `items`

## Implemented backend

Endpoints:

- `POST /api/admin/v1/items`
- `PUT /api/admin/v1/items/{itemId}`
- `POST /api/admin/v1/items/{itemId}/duplicate`
- `GET /api/admin/v1/items/preview-state?iconId=...`

Implementation notes:

- controllers stay thin
- write orchestration lives in `ItemsAdminWriteService`
- SQL stays in `ItemsAdminWriteRepository`
- `ValidationProblemDetails.errors` are returned for field-level Angular rendering
- `traceId` is always included
- preview lookup for the write form is intentionally by `IconId`

### Placeholder configuration fix

Phase 7 also fixed a real scaffold bug in `AdminDatabaseOptions`:

- before: any password containing `change-me` was treated as placeholder
- now: only an exact placeholder password of `change-me` is treated as non-usable

That change was required so a legitimate local secret such as `change-me-app` could be used without lying that the DB was `not_configured`.

## Implemented Angular

Routes:

- `/admin/items/new`
- `/admin/items/:itemId/edit`
- `/admin/items/:itemId/duplicate`

UI outcomes:

- list page exposes `Create item` and `Edit`
- detail page exposes `Edit item` and `Duplicate item`
- write form keeps the identity rule visible
- preview updates from the current form `IconId`
- embedded `ItemIconSelector` can assign `IconId`
- `Cancel` returns to list or source detail
- successful save redirects to the detail route

## Validation performed

### Build validation

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`: `OK`
- `npm run build`: `OK`

### Live API validation

Validated against the real local DB on `2026-06-02`:

- `GET /api/admin/v1/health/db`: `ok`
- `POST /api/admin/v1/items`: real create succeeded
- `PUT /api/admin/v1/items/{newItemId}`: real update succeeded
- `POST /api/admin/v1/items/39/duplicate`: real duplicate succeeded
- `POST /api/admin/v1/items` invalid payload: controlled `422`

Validated ids:

- create produced temporary `ItemId 12616`, `DescriptionId 50090`
- update confirmed the same `ItemId 12616`
- duplicate from `ItemId 39` produced temporary `ItemId 12617`, `DescriptionId 50091`

Validated data outcomes:

- created detail loaded successfully
- edited detail reflected updated `ResolvedName`, `Level`, `Weight`, and `Price`
- duplicated detail loaded successfully
- `previewState` resolved as `FOUND` by `IconId=1001`
- invalid create returned `errors.resolvedName`, `errors.level`, `errors.weight`, `errors.price`, `errors.iconId`, `errors.appearanceId`, and `errors.typeId`

### Cleanup validation

Because `sunshine.items` is `MyISAM`, the validation run cleaned up the two temporary rows immediately after the smoke test.

Cleanup verified:

- temporary `ItemId 12616`: removed
- temporary `ItemId 12617`: removed
- both ids return `404` after cleanup

### Browser validation

Validated in the official Angular app:

- `/admin/items/new`
  - loads type and set options from live sources
  - embedded icon selector opens
  - selecting `1001.png` updates `IconId` to `1001`
  - preview switches to `FOUND`
- `/admin/items/39/edit`
  - preloads live item data
  - keeps `ItemId 39` fixed
  - shows preview resolved by `IconId=1001`
- `/admin/items/39/duplicate`
  - preloads the source item
  - explains that a new `ItemId` and `DescriptionId` will be allocated

## Remaining constraints

Phase 7 intentionally does not do:

- direct persistence of free-text `Description`
- direct persistence of `IsVisible`
- PNG upload
- publish to client assets or i18n
- weapon writes

## Expected commit

- `feat: add items builder create edit workflow`
