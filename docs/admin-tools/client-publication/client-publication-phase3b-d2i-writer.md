# Macro 4 / Phase 3B — D2I Writer staging prototype

Date: `2026-06-04`  
Branch: `feature/client-item-publication-d2i-writer-phase3b`  
Estado: **`DONE`**

## Objetivo

Prototipo read/write de `.d2i` en staging para desbloquear identidad de items custom (nombre/descripción) sin tocar `Client2.3.7` real.

## Entregables

| # | Entregable | Estado |
| ---: | --- | --- |
| 1 | Notas formato D2I | [client-d2i-format-notes.md](./client-d2i-format-notes.md) |
| 2 | `D2iFile` / `D2iTextWriter` + `D2iStagingPublisher` | `Infrastructure/scripts/ClientItemPublicationPipeline/D2i/` |
| 3 | CLI `d2i-inspect`, `d2i-roundtrip`, `d2i-append-text` | OK |
| 4 | Modo `stage-item-publication` | OK |
| 5 | Reporte append | [client-d2i-append-report.md](./client-d2i-append-report.md) |

## Validación ejecutada

```bash
dotnet build "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2i-inspect --output "Infrastructure/staging-client/i18n-phase3b"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2i-roundtrip --output "Infrastructure/staging-client/i18n-phase3b"
# ES/EN count 62710 -> 62710, textId 40904 preservado

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2i-append-text --output "Infrastructure/staging-client/i18n-phase3b" \
  --es-name "Dofus de los Hielos" \
  --es-description "Dofus de los Hielos creado para pruebas controladas del pipeline de publicación." \
  --en-name "Ice Dofus" \
  --en-description "Ice Dofus created for controlled publication pipeline testing."

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode stage-item-publication \
  --output "Infrastructure/staging-client/publication-phase3b/12617" \
  --source-item-id 7754 --target-item-id 12617 \
  --es-name "..." --en-name "..."
```

## Decisión de modelo i18n

- Un solo `nameId` y un solo `descriptionId` en `Items.d2o`.
- Textos distintos por idioma en archivos `i18n_es.d2i` / `i18n_en.d2i` bajo el **mismo** id.

## Siguiente

**Macro 4 / Phase 3C** — paquete de publicación completo (launcher patch, QA cliente, manifest Admin sin `BLOCKED_I18N_WRITER_MISSING`).
