# Macro 4 / Phase 5A — Controlled patch sandbox

**Estado:** `DONE`  
**Rama:** `feature/client-publication-controlled-patch-phase5`

## Objetivo

Aplicar el paquete validado `publication-package-phase3c/12617` sobre una **copia sandbox** del cliente, sin modificar `Client2.3.7` real.

## Estructura

```txt
Infrastructure/staging-client/client-patch-sandbox/12617/
  data/common/Items.d2o
  data/i18n/i18n_es.d2i
  data/i18n/i18n_en.d2i
  original-client-baseline.json
  sandbox-apply-manifest.json
  sandbox-validation-report.json
  sandbox-validation-report.md
```

La semilla inicial copia solo los 3 archivos desde `Client2.3.7` (lectura). El patch sobrescribe con el paquete Phase 3C.

## CLI

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode apply-package-to-sandbox \
  --package "Infrastructure/staging-client/publication-package-phase3c/12617" \
  --sandbox "Infrastructure/staging-client/client-patch-sandbox/12617"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode validate-sandbox-client \
  --sandbox "Infrastructure/staging-client/client-patch-sandbox/12617" \
  --target-item-id 12617
```

## Validación

- ItemId 12617 en sandbox `Items.d2o`
- `nameId` / `descriptionId` en i18n ES y EN
- IconId 23012
- Checksums de archivos sandbox
- **Client2.3.7 real intacto** (SHA-256 vs `original-client-baseline.json`)

## Siguiente

Publicación real al cliente sigue bloqueada; Phase 6+ requerirá backup operador + aprobación explícita.
