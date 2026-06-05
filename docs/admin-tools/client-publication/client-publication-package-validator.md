# Validador de paquete de publicación staging

**Herramienta:** `ClientItemPublicationPipeline`  
**Modo:** `validate-publication-package`

## Uso

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode validate-publication-package \
  --package "Infrastructure/staging-client/publication-package-phase3c/12617" \
  --target-item-id 12617
```

Código de salida: `0` si el paquete es válido; `1` si hay blocking reasons.

## Comprobaciones

| Check | Descripción |
| --- | --- |
| Archivos | `data/common/Items.d2o`, `data/i18n/i18n_es.d2i`, `data/i18n/i18n_en.d2i` (o layout legacy plano Phase 3B) |
| ItemId | Presencia en índice D2O del paquete |
| TypeId | Existe en `ItemTypes.d2o` del cliente de referencia |
| i18n | `nameId` y `descriptionId` resueltos en ES y EN |
| IconId | PNG curado `by-icon/{iconId}.png` o heurística en `bitmap*.d2p` |
| AppearanceId | `0` → `NOT_APPLICABLE` |
| Checksums | SHA-256 de archivos generados; coherencia con manifiesto |
| Manifiesto | Actualiza `validation-report.json` / `.md` y reescribe manifiesto |

## Salidas

- `validation-report.json` — máquina
- `validation-report.md` — humano
- `checksums.sha256` — hashes por ruta relativa
- `publication-package-manifest.json` — enriquecido con `ValidationStatus`

## Integración API

`StagingPublicationPackageProbe` (Infrastructure) lee manifiesto y reporte en disco para el endpoint de manifiesto Admin — sin re-ejecutar Sunshine en la API.

Estados API (`stagingPackageStatus`):

- `NO_PACKAGE_GENERATED`
- `PACKAGE_AVAILABLE_IN_STAGING`
- `NEEDS_VALIDATION`
- `READY_FOR_CONTROLLED_PUBLISH`
