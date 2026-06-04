# Contrato — ItemPublicationManifest

API: `GET /api/admin/v1/items/{itemId}/publication-manifest`  
DTO: `RollblackLegacy.Admin.Contracts.Items.ItemPublicationManifestDto`

## Campos

| Campo | Tipo | Descripción |
| --- | --- | --- |
| `dbItemId` | int | Item en `sunshine.items` |
| `targetClientItemId` | int | Id objetivo en `Items.d2o` (Phase 1 = mismo que DB) |
| `nameEs` / `nameEn` | string? | Nombre resuelto (cliente o DB) |
| `descriptionId` | int | DescriptionId DB |
| `typeId` | int | TypeId DB |
| `typeName` | string? | Nombre tipo desde ItemTypes.d2o si disponible |
| `iconId` | int | IconId DB |
| `appearanceId` | int | AppearanceId DB |
| `effectsSummary` | string | Resumen legible de efectos runtime |
| `criteria` | string? | Criterio equipable/uso |
| `sourceTemplateItemId` | int? | Template cliente sugerido (ej. `7754` para Dofus) |
| `clientKnown` | bool | Template presente en `Items.d2o` |
| `primaryState` | string | Estado principal (ver abajo) |
| `states` | string[] | Todos los estados aplicables |
| `requiredClientActions` | string[] | Pasos operativos |
| `filesToPatch` | string[] | Rutas relativas bajo `Client2.3.7` |
| `risks` | string[] | Riesgos de publicación |
| `canPublishAutomatically` | bool | Phase 1 siempre `false` |
| `blockingReasons` | string[] | Motivos de bloqueo |
| `clientRootPath` | string? | Raíz cliente auditada |
| `stagingOutputPath` | string? | Carpeta temporal sugerida |
| `generatedAtUtc` | datetime | Marca de generación |

## Estados (`ItemPublicationManifestStates`)

| Constante | Significado |
| --- | --- |
| `READY_TO_STAGE` | Template ya conocido o listo conceptualmente para staging |
| `BLOCKED_CLIENT_WRITER_MISSING` | Falta pipeline D2O Item validado |
| `BLOCKED_I18N_WRITER_MISSING` | Falta writer D2I / textos cliente |
| `BLOCKED_UNKNOWN_TYPE` | `TypeId` ausente en `ItemTypes.d2o` |
| `BLOCKED_INVALID_ICON` | IconId inválido o sin preview/D2P verificado |
| `BLOCKED_MANUAL_REVIEW` | Revisión humana obligatoria |

## Casos de control esperados

| ItemId | clientKnown | primaryState (típico) |
| ---: | --- | --- |
| `7754` | true | `READY_TO_STAGE` |
| `12617` | false | `BLOCKED_CLIENT_WRITER_MISSING` |
| `12616` | según DB | dry-run operador |

## CLI output

Bajo `--output`:

```txt
publication-manifest.json
publication-manifest.md
```
