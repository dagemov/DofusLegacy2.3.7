# Client Identity Audit Tool - Phase 1

## Estado

- Macro: `2`
- Phase: `1`
- Status: `DONE`
- Branch: `feature/client-identity-audit-tool-phase1`

## Objetivo

Eliminar el cuello de botella:

```txt
El item existe en DB, pero el cliente no lo conoce.
```

## Alcance de la fase

Esta primera fase entrega:

- plan del algoritmo
- scaffold read-only
- lectura puntual de `DB + D2O + D2I`
- validacion de casos de control
- reporte reproducible en Markdown

Esta fase no entrega:

- UI
- writes
- patch cliente
- extraccion masiva
- auditoria de armas

## Inputs auditados

Cliente:

- `Client2.3.7/data/common/Items.d2o`
- `Client2.3.7/data/common/ItemTypes.d2o`
- `Client2.3.7/data/common/ItemSets.d2o`
- `Client2.3.7/data/common/Appearances.d2o`
- `Client2.3.7/data/i18n/i18n_es.d2i`
- `Client2.3.7/data/i18n/i18n_en.d2i`
- `Client2.3.7/content/gfx/items/bitmap0.d2p`
- `Client2.3.7/content/gfx/items/bitmap1.d2p`

Runtime:

- `sunshine.items`

## Algoritmo implementado

Para cada `ItemId` pedido:

1. leer el row de `sunshine.items`
2. buscar el mismo `ItemId` en `Items.d2o`
3. si existe en cliente, leer:
   - `nameId`
   - `descriptionId`
   - `typeId`
   - `iconId`
   - `appearanceId`
   - `itemSetId`
4. resolver `descriptionId` DB en:
   - `i18n_es.d2i`
   - `i18n_en.d2i`
5. resolver `nameId` cliente en:
   - `i18n_es.d2i`
   - `i18n_en.d2i`
6. resolver `typeId` cliente contra `ItemTypes.d2o`
7. resolver `itemSetId` cliente contra `ItemSets.d2o`
8. verificar `appearanceId` en `Appearances.d2o` solo si `AppearanceId > 0`
9. verificar preview curado en:
   - `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-item`
   - `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon`
10. calcular estados:
    - `CLIENT_KNOWN`
    - `CLIENT_UNKNOWN`
    - `I18N_MISSING_ES`
    - `I18N_MISSING_EN`
    - `ICON_MISSING`
    - `APPEARANCE_UNKNOWN`
    - `NEEDS_CLIENT_PATCH`
    - `SAFE_EXISTING_TEMPLATE`

## Scaffold entregado

Ruta:

```txt
Infrastructure/scripts/ClientIdentityAudit
```

Archivos:

- `ClientIdentityAudit.csproj`
- `Program.cs`

Capacidades actuales:

- carga configuracion `SunshineAdmin` desde los `appsettings` del Admin API
- lee `sunshine.items` en modo read-only
- lee `Items.d2o`, `ItemTypes.d2o`, `ItemSets.d2o` y `Appearances.d2o`
- lee `i18n_es.d2i` e `i18n_en.d2i`
- genera reporte Markdown

## Casos de control validados

- `7754` `Dofus Ocre`
- `12616` `ADMIN TEST`
- `12617` `Dofus Tester`
- `39` item con preview `IconId=1001`

## Hallazgos de Phase 1

- `7754` sigue siendo el control seguro `CLIENT_KNOWN`
- `12616` y `12617` son `CLIENT_UNKNOWN`
- `12617` no falla por `IconId`
- `12617` no falla por `AppearanceId`
- `12617` falla porque el cliente no conoce el template
- `39` confirma que el path de preview curado por `IconId=1001` funciona
- el cliente actual usa una variante de `D2I` con indice simple `id -> offset`

## Validacion ejecutada

```bash
dotnet build "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj"
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md"
```

## Siguiente paso recomendado

Macro 2 - Phase 2:

- exponer este audit como servicio reusable para Admin API
- agregar chequeos read-only mas precisos de `nameId` y `descriptionId`
- decidir si `AppearanceId` debe seguir en `Appearances.d2o` o en una auditoria separada
- seguir sin UI hasta cerrar la capa de servicio reusable
