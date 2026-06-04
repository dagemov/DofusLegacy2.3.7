# Items Builder Read-only API Phase 2

> Status correction on `2026-06-02`: this document records exploratory work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/items-builder-readonly-api-phase2`
- Base branch: `feature/admin-clean-architecture-scaffold-phase1-5`
- Scope type: implemented read-only API with completed local runtime validation
- Scaffold prerequisite commit: `ce23364 chore: scaffold admin clean architecture projects`
- Contract source branch: `feature/items-builder-readonly-contracts-phase2`

## Goal

Implement the minimum read-only API surface required for the future Angular Items Builder.

This phase is intentionally limited to:

- item list and search
- item detail
- item client identity
- item type options
- item set options
- preview/manual asset state
- read-only diagnostics

This phase does not implement create, edit, upload, publish, or write flows.

## Implementation basis

Phase 1.5 now provides the required Admin clean-architecture projects:

- `RollblackLegacy.Admin.Api`
- `RollblackLegacy.Admin.Application`
- `RollblackLegacy.Admin.Contracts`
- `RollblackLegacy.Admin.Infrastructure`
- `RollblackLegacy.Admin.Domain`

Phase 2 implementation rules applied here:

- keep controllers thin
- keep MySQL access inside Infrastructure
- keep `ItemId`, `IconId`, and `AppearanceId` separate
- keep read-only diagnostics explicit
- return controlled problem details for invalid or unavailable requests

## Schema basis

This phase continues to use repo-local Sunshine schema and runtime classes as the source of truth:

- `database/sunshine.sql`
- `Sunshine.MySql/Database/World/Items/ItemTemplate.cs`
- `Sunshine.MySql/Database/World/Items/ItemSetTemplate.cs`
- `Sunshine.MySql/Database/Managers/ItemManager.cs`
- `Sunshine.Protocol/Enums/ItemTypeEnum.cs`
- `Sunshine.Protocol/Enums/EffectsEnum.cs`

Confirmed runtime sources:

- main item table: `items`
- set table: `items_sets`
- vendor link table: `npcs_items`

No confirmed Sunshine-native admin table exists yet for:

- client identity metadata
- manual asset metadata

Practical implication:

- identity stays derived from current runtime rows
- preview/manual asset state must support `UNKNOWN` and `MISSING`

## Critical identity rule

```txt
ItemId != IconId != AppearanceId
```

The read-only API exposes these as separate values and does not imply that one can be derived from another.

## Implemented contract surface

### ItemSearchRequest

```txt
search
itemId
iconId
typeId
levelMin
levelMax
page
pageSize
```

### ItemPagedResultDto

```txt
page
pageSize
totalCount
items[]
```

### ItemListItemDto

```txt
itemId
resolvedName
typeId
typeName
level
setId
setName
iconId
appearanceId
previewState
warningCount
```

### ItemDetailDto

```txt
itemId
resolvedName
description
descriptionId
typeId
typeName
level
weight
price
usable
targetable
twoHanded
etheral
criteria
iconId
appearanceId
set
clientIdentity
previewState
warnings[]
effects[]
```

Implementation note:

- `description` is currently `null` because the current Sunshine row exposes `DescriptionId` but not resolved client description text

### ItemClientIdentityDto

```txt
itemId
clientNameId
clientName
iconId
appearanceId
source
confidence
```

Current implementation note:

- `clientNameId` is currently `null`
- `source` resolves to `sunshine.items`
- `confidence` is currently `0.50` because identity is inferred from the runtime row only

### ItemPreviewStateDto

```txt
state
byItemPath
byIconPath
manualPath
```

Allowed `state` values:

```txt
FOUND
MISSING
MANUAL
UNKNOWN
```

### ItemWarningDto

```txt
code
severity
message
field
```

### ItemEffectDto

```txt
effectId
diceNum
diceSide
value
description
```

### ItemSetLinkDto

```txt
setId
setName
state
```

## Implemented endpoints

### `GET /api/admin/v1/items`

Purpose:

- paged list and search

Supported query keys:

```txt
search
itemId
iconId
typeId
levelMin
levelMax
page
pageSize
```

Response:

- `ItemPagedResultDto<ItemListItemDto>`

### `GET /api/admin/v1/items/{itemId}`

Purpose:

- full read-only detail for the future Angular detail page

Response:

- `ItemDetailDto`

### `GET /api/admin/v1/items/{itemId}/identity`

Purpose:

- explicit client identity projection

Response:

- `ItemClientIdentityDto`

### `GET /api/admin/v1/items/types/options`

Purpose:

- type lookup for filters and detail display

Primary source:

- `Sunshine.Protocol/Enums/ItemTypeEnum.cs`, parsed from the repo source tree

### `GET /api/admin/v1/item-sets/options`

Purpose:

- set lookup for filters and detail display

Primary source:

- `items_sets`

## Identity diagnostics

Implemented read-only warning codes:

```txt
MISSING_CLIENT_NAME
MISSING_ICON
ICON_ID_MISMATCH
APPEARANCE_ID_MISMATCH
MANUAL_ASSET_MISSING
SET_LINK_MISSING
UNKNOWN_TYPE
```

Current behavior notes:

- `ICON_ID_MISMATCH` and `APPEARANCE_ID_MISMATCH` stay dormant unless a future identity source disagrees with the runtime row
- `MANUAL_ASSET_MISSING` is emitted when preview directories exist but no preview/manual file resolves

## Preview/manual asset resolution

Logical paths frozen in this phase:

```txt
/assets/item-previews/by-item/{itemId}.png
/assets/item-previews/by-icon/{iconId}.png
/manual-assets/items/{itemId}.png
```

Resolution order:

1. manual item asset path
2. item-bound preview path
3. icon-bound preview path
4. `MISSING` if preview directories exist but the file is absent
5. `UNKNOWN` if no preview directories exist yet

The API returns state and logical paths only. It does not create, import, or publish PNG assets in this phase.

## Validation

Build validation:

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`: `OK`

HTTP validation:

- `GET /api/admin/v1/health`: `200 OK`
- `GET /api/admin/v1/health/db`: `200 OK`
- `GET /api/admin/v1/items/types/options`: `200 OK`
- `GET /api/admin/v1/item-sets/options`: `200 OK`
- `GET /api/admin/v1/items?page=1&pageSize=20`: `200 OK`
- `GET /api/admin/v1/items/39`: `200 OK`
- `GET /api/admin/v1/items/39/identity`: `200 OK`
- `GET /api/admin/v1/items/1`: controlled `404` with `traceId`
- `GET /api/admin/v1/items?page=0&pageSize=20`: controlled `400` validation response with `traceId`
- `GET /api/admin/v1/items/0`: controlled `400` validation response with `traceId`

Phase status on this machine: `DONE`

Reason:

- the read-only API is implemented
- local end-to-end data retrieval is now validated through an ignored `appsettings.Development.local.json`
- tracked config intentionally remains placeholder-only
- the local validation DB came from the repo-local Sunshine dump and local MariaDB runtime

## Current Angular consumer status

The read-only API is now consumed by the Angular admin slices delivered in Phase 3, Phase 4, and the live-data integration follow-up:

- items list page
- items detail page
- runtime summary card
- client identity card
- warning panel
- preview/manual asset visualization
- type/set filter wiring
- operator copy/debug helpers for identity and path values
- live `404` not-found handling with visible `traceId`
- Phase 7 create/edit/duplicate forms for:
  - type options
  - item set options
  - preview-state lookup by `IconId`
  - shared `ProblemDetails` parsing with field-level error rendering

The remaining limitation is no longer DB access. It is preview/manual asset availability: the API can expose logical paths and states, but live image rendering still depends on actual files being curated under those roots.

## Expected commit

- `feat: add items builder readonly admin api`
