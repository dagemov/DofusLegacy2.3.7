# Items Builder Write Contracts Phase 7

## Purpose

Freeze the official write contract used by Phase 7 create, edit, and duplicate inside the official repo without pretending that every future publish concern already persists all the way into the client.

## Request payload

Phase 7 uses one shared logical payload shape across:

- `POST /api/admin/v1/items`
- `PUT /api/admin/v1/items/{itemId}`
- `POST /api/admin/v1/items/{itemId}/duplicate`

Logical payload:

```txt
resolvedName
description
typeId
level
weight
price
iconId
appearanceId
setId
conditions
isVisible
usable
targetable
twoHanded
etheral
```

## Identity rule

```txt
ItemId != IconId != AppearanceId
```

Interpretation:

- `ItemId`: database/runtime item template identity
- `IconId`: inventory/admin preview identity
- `AppearanceId`: equipped or look identity

The contract keeps them separate and the write UI must never relabel one as another.

## Persistence notes

Persisted directly in `sunshine.items` during Phase 7:

- `Name`
- `TypeId`
- `Level`
- `Weight`
- `Price`
- `IconId`
- `AppearanceId`
- `ItemSetId`
- `Criteria`
- `Usable`
- `Targetable`
- `TwoHanded`
- `Etheral`

Allocated or derived internally:

- `Id`
- `DescriptionId`

Accepted but not fully persisted yet:

- `description`
- `isVisible`

Phase 7 warning policy:

- `description` is accepted to keep the future contract stable, but `sunshine.items` only stores `DescriptionId`
- `isVisible` is accepted to keep UI flow stable, but `sunshine.items` has no direct column for it

## Validation rules

Server-side rules enforced:

- `resolvedName` required
- `typeId` must resolve to a known item type
- weapon `typeId` values rejected with `422`
- `level >= 1`
- `weight >= 0`
- `price >= 0`
- `iconId >= 0`
- `appearanceId >= 0`
- `setId > 0` when supplied
- supplied `setId` must exist in `items_sets`

## Error contract

Write failures use `ProblemDetails` or `ValidationProblemDetails`.

Phase 7 guarantees:

- `400` for invalid route/query shape
- `404` for missing source item on edit or duplicate
- `409` for generated-id conflicts
- `422` for write payload validation
- `500` for configuration or DB failures
- `traceId` always exposed

Field errors serialize for Angular through:

```txt
errors.resolvedName
errors.typeId
errors.level
errors.weight
errors.price
errors.iconId
errors.appearanceId
errors.setId
```

## Response payload

`ItemWriteResultDto`

```txt
itemId
operation
resolvedName
descriptionId
descriptionPersisted
isVisiblePersisted
detailPath
previewState
warnings[]
```

Important meanings:

- `itemId`: final saved identity
- `operation`: `create`, `update`, or `duplicate`
- `descriptionPersisted`: `false` in Phase 7
- `isVisiblePersisted`: `false` in Phase 7
- `detailPath`: operator handoff path after save

## Preview contract for write form

Phase 7 write form uses:

- `GET /api/admin/v1/items/preview-state?iconId=...`

Reason:

- pre-save preview must follow the chosen `IconId`
- it must not imply that an unchanged `ItemId` preview is still correct after an icon change

## Safe defaults

For brand-new rows, Phase 7 create uses controlled defaults for unsupported columns:

- `Cursed = false`
- `UseAnimationId = -1`
- `HideEffects = false`
- `RecipeIdsCSV = ""`
- `FavoriteSubAreasCSV = ""`
- `BonusIsSecret = false`
- `FavoriteSubAreasBonus = 0`
- `Effects = "0000"`

## Local configuration note

The development guard only treats an exact password of `change-me` as placeholder.

That means:

- `Password=change-me` -> blocked as `not_configured`
- `Password=change-me-app` -> treated as a usable local secret

This avoids false negatives in local DB validation while still protecting the example config committed to Git.

## Explicit non-goals

Not part of Phase 7:

- weapon row creation in `items_weapons`
- text publish to client i18n
- PNG upload
- client pack publish
- SWF or D2P extraction
