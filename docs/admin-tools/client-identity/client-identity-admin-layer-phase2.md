# Client Identity Audit Tool - Phase 2

## Estado

- Macro 2: `IN_PROGRESS`
- Phase 1: `DONE`
- Phase 2: `DONE`
- Scope de esta fase: `promover el scaffold read-only a capa reusable de Admin API`

## Objetivo

Eliminar la duplicacion entre:

- la tool offline `ClientIdentityAudit`
- los diagnosticos de `publication-status`
- futuras integraciones read-only del Admin API

La misma base read-only ahora responde:

- `GET /api/admin/v1/client-identity/items/{itemId}`
- `GET /api/admin/v1/client-identity/items/check?ids=7754,12616,12617,39`
- `GET /api/admin/v1/items/{itemId}/publication-status`

## Entregables

Backend reusable agregado en:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/ClientIdentityAdminController.cs
```

Tool CLI conservada y refactorizada:

```txt
infrastructure/scripts/ClientIdentityAudit/
```

## Algoritmo reusable

Para cada `ItemId`:

1. leer `sunshine.items` en modo read-only
2. buscar el mismo `ItemId` en `Items.d2o`
3. resolver `DescriptionId` DB en `i18n_es.d2i`
4. resolver `DescriptionId` DB en `i18n_en.d2i`
5. leer `nameId`, `typeId`, `itemSetId`, `iconId`, `appearanceId` del cliente si el template existe
6. resolver nombres ES/EN de tipo y set
7. verificar preview curado por `by-item`, `by-icon` o `manual-assets`
8. verificar `AppearanceId` contra `Appearances.d2o`
9. calcular estados:
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

## Validacion ejecutada

Builds:

```bash
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md"
```

Smoke test API:

```txt
GET /api/admin/v1/client-identity/items/7754
GET /api/admin/v1/client-identity/items/12617
GET /api/admin/v1/client-identity/items/check?ids=7754,12616,12617,39
GET /api/admin/v1/items/7754/publication-status
GET /api/admin/v1/items/12617/publication-status
```

## Casos de control

- `7754` -> `CLIENT_KNOWN / SAFE_EXISTING_TEMPLATE`
- `12616` -> `CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH / APPEARANCE_UNKNOWN`
- `12617` -> `CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH`
- `39` -> `CLIENT_KNOWN / SAFE_EXISTING_TEMPLATE / ICON_PREVIEW_FOUND`

## Decisiones

- no se toco `Client2.3.7/` en modo write
- no se escribio DB
- no se agrego UI en esta fase
- `publication-status` ya consume la nueva auditoria reusable
- la tool CLI dejo de parsear D2O/D2I por su cuenta y ahora actua como wrapper/report writer

## Siguiente paso recomendado

Macro 2 / Phase 3:

```txt
exponer esta auditoria en una pantalla Angular read-only
sin tocar cliente en write mode
sin abrir Macro 3 todavia
```
