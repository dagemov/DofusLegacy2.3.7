# Macro 4 / Phase 3C — Paquete staging de publicación

**Estado:** `DONE`  
**Rama:** `feature/client-item-publication-staging-package-phase3c`

## Objetivo

Generar un paquete autoconsistente bajo `Infrastructure/staging-client/` (gitignored) con layout de cliente real, manifiesto enriquecido, checksums y reportes de validación — **sin** tocar `Client2.3.7` original, DB ni VPS.

## Estructura del paquete

```txt
Infrastructure/staging-client/publication-package-phase3c/{itemId}/
  data/common/Items.d2o
  data/i18n/i18n_es.d2i
  data/i18n/i18n_en.d2i
  publication-package-manifest.json
  publication-package-manifest.md
  validation-report.json
  validation-report.md
  checksums.sha256
```

## Caso de control (12617)

| Campo | Valor |
| --- | --- |
| ItemId | 12617 |
| Template | 7754 (Dofus Ocre) |
| TypeId | 23 |
| IconId | 23012 |
| AppearanceId | 0 |
| ES name | Dofus de los Hielos |
| EN name | Ice Dofus |

Modelo i18n: un `nameId` y un `descriptionId` en `Items.d2o`; el mismo entero en `i18n_es.d2i` e `i18n_en.d2i` con textos distintos.

## Generación

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode stage-item-publication \
  --item-id 12617 \
  --output "Infrastructure/staging-client/publication-package-phase3c/12617" \
  --source-item-id 7754 --target-item-id 12617 \
  --es-name "Dofus de los Hielos" \
  --es-description "Dofus de los Hielos creado para pruebas controladas del pipeline de publicación." \
  --en-name "Ice Dofus" \
  --en-description "Ice Dofus created for controlled publication pipeline testing."
```

## Manifiesto (campos Phase 3C)

- `PackageId`, `CreatedAt`, `SourceTemplateItemId`, `TargetItemId`
- `GeneratedFiles`, `Checksums`
- `ValidationStatus`, `BlockingReasons`, `Warnings`, `NextManualSteps`
- `IsProductionPackage` — siempre `false` en staging

Estados de validación: `VALID_STAGING_PACKAGE`, `INVALID_STAGING_PACKAGE`, `READY_FOR_CONTROLLED_PUBLISH`, `BLOCKED_VALIDATION`.

## Admin API / Angular

- `GET /api/admin/v1/items/{itemId}/publication-manifest` expone `stagingPackageStatus`, `stagingValidationStatus`, `nextManualSteps`.
- `/admin/items/{itemId}/publication-status` muestra el bloque **Staging package** en el preview del manifiesto.

No hay botón **Publicar** en Phase 3C.

## Siguiente

**Macro 4 / Phase 4** — patch controlado solo en copia backup del cliente + lane launcher.
