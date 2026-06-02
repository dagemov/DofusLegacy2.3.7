# Items Builder Target Contracts

## Purpose

These contracts adapt the strongest ideas from the previous Angular/Admin API repo to the current Sunshine schema and the new Admin clean-architecture boundaries.

This document now reflects the implemented read-only API baseline in `feature/items-builder-readonly-api-phase2`.

Current execution note:

- the contracts below now exist in `RollblackLegacy.Admin.Contracts`
- the read-only API is implemented in the Admin layers
- local runtime validation is still `PARTIAL` on this machine because `SunshineAdmin` remains configured with the tracked placeholder

## Contract principles

1. Keep runtime item data separate from client metadata and preview diagnostics.
2. Treat `ItemId`, `IconId`, and `AppearanceId` as separate fields.
3. Keep browser-to-server contracts typed and explicit.
4. Use consistent `400/404/500` problem details with `traceId`.
5. Keep manual asset handling explicit and never hide file-system side effects behind a generic save call.

Critical rule:

```txt
ItemId != IconId != AppearanceId
```

## Sunshine-aligned entities

| Concern | Current target table/source | Notes |
| --- | --- | --- |
| Item runtime row | `items` | primary item read/write target |
| Weapon extension | `items_weapons` | later write slices only if required |
| Item sets | `items_sets` | lookup and relation target |
| Vendor links | `npcs_items` | future vendor assignment feature |
| Client metadata | no confirmed Sunshine-native admin table | derived or `UNKNOWN` in read-only phase |
| Manual preview asset metadata | no confirmed Sunshine-native admin table | path ownership remains a later decision |

## Implemented read endpoints

| Endpoint | Purpose | Current state |
| --- | --- | --- |
| `GET /api/admin/v1/items` | paged search by `search`, `itemId`, `iconId`, `typeId`, and level range | implemented |
| `GET /api/admin/v1/items/{itemId}` | full item detail with runtime fields, client identity snapshot, diagnostics, and set link | implemented |
| `GET /api/admin/v1/items/{itemId}/identity` | explicit client identity projection | implemented |
| `GET /api/admin/v1/items/types/options` | lookup for type dropdowns and filters | implemented |
| `GET /api/admin/v1/item-sets/options` | lookup for set selector/autocomplete | implemented |

## Planned write endpoints

| Endpoint | Purpose | Phase intent |
| --- | --- | --- |
| `POST /api/admin/v1/items` | create a new runtime item | later slice |
| `PUT /api/admin/v1/items/{itemId}` | update runtime item fields | later slice |
| `PUT /api/admin/v1/items/{itemId}/effects` | replace or normalize serialized effects payload | later slice |
| `PUT /api/admin/v1/items/{itemId}/vendors` | update vendor assignments backed by `npcs_items` | later slice |
| `POST /api/admin/v1/items/{itemId}/manual-asset` | upload or replace a manual preview asset | later slice |
| `DELETE /api/admin/v1/items/{itemId}/manual-asset` | clear manual preview asset metadata | later slice |
| `POST /api/admin/v1/items/{itemId}/identity-correction` | preview or apply identity corrections in a guarded workflow | later slice |
| `POST /api/admin/v1/items/{itemId}/publish-client-support` | guarded future publish workflow | defer until infrastructure and backup policy exist |

## DTO summary

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

## Error envelope expectation

The Angular client can now rely on a consistent failure envelope such as:

```json
{
  "title": "The request is invalid.",
  "status": 400,
  "traceId": "00-...-...",
  "errors": {
    "page": [
      "The field Page must be between 1 and 2147483647."
    ]
  }
}
```

Controlled runtime states currently validated:

- `400` for invalid query or route values
- `404` for item not found once a usable DB connection is present
- `500` with `traceId` when `SunshineAdmin` is not configured

## Mapping guidance from legacy contracts

- Keep distinct list/detail/write DTOs.
- Keep vendor links as an explicit future sub-resource.
- Keep diagnostics and preview information in read models, not generic write responses.
- Do not collapse client metadata into runtime DTOs without labels.
- Do not blur `ItemId`, `IconId`, and `AppearanceId` in UI or API responses.

## Current execution note

The first implementation slice is now the read-only API and diagnostics layer.

What remains intentionally out of scope:

- write endpoints
- PNG upload
- client publish
- Angular UI
- DB migrations

The next execution step is Phase 3: Angular list/detail consumption of this API.
