# Client Identity Audit API Contracts

## Endpoints

### `GET /api/admin/v1/client-identity/items/{itemId}`

Respuesta:

- `ClientItemIdentityCheckResultDto`

Uso:

- inspeccion detallada de un item puntual
- soporte para `publication-status`
- comparacion DB vs cliente sin tocar `Client2.3.7/`

### `GET /api/admin/v1/client-identity/items/check?ids=7754,12616,12617,39`

Respuesta:

- `IReadOnlyList<ClientItemIdentityCheckResultDto>`

Uso:

- batch read-only de ids controlados
- smoke tests reproducibles
- reportes documentales

## DTO principal

### `ClientItemIdentityCheckResultDto`

Campos clave:

- `ItemId`
- `DbName`
- `DbDescriptionId`
- `ClientDescriptionId`
- `ClientNameId`
- `ClientKnown`
- `Status`
- `DescriptionEs`
- `DescriptionEn`
- `ClientNameEs`
- `ClientNameEn`
- `DbTypeId`
- `ClientTypeId`
- `ClientTypeNameEs`
- `ClientTypeNameEn`
- `DbSetId`
- `ClientSetId`
- `ClientSetNameEs`
- `ClientSetNameEn`
- `DbIconId`
- `ClientIconId`
- `DbAppearanceId`
- `ClientAppearanceId`
- `Appearance`
- `IconPreviewFound`
- `PreviewPath`
- `ItemsD2oPath`
- `ItemTypesD2oPath`
- `ItemSetsD2oPath`
- `AppearancesD2oPath`
- `I18nEsPath`
- `I18nEnPath`

## Estado calculado

### `ClientItemIdentityStatusDto`

Campos:

- `PrimaryStatus`
- `ClientKnown`
- `NeedsClientPatch`
- `Statuses`
- `Warnings`
- `RecommendedAction`

Estados observados hasta ahora:

- `SAFE_EXISTING_TEMPLATE`
- `CLIENT_KNOWN`
- `CLIENT_UNKNOWN`
- `NEEDS_CLIENT_PATCH`
- `I18N_MISSING_ES`
- `I18N_MISSING_EN`
- `ICON_MISSING`
- `ICON_PREVIEW_FOUND`
- `ICON_PREVIEW_MISSING`
- `APPEARANCE_UNKNOWN`
- `CLIENT_DATA_UNAVAILABLE`

## Notas operativas

- `IconId` no publica un template cliente por si solo.
- `AppearanceId` no sustituye la existencia del template.
- `ClientKnown = false` implica que el cliente no conoce `ItemId` en `Items.d2o`.
- `NeedsClientPatch = true` significa que el item no debe declararse visible en cliente todavia.
