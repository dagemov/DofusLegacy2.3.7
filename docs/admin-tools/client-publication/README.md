# Client Item Publication Pipeline

Macro 4 — publicar items custom del Items Builder al cliente **sin** depender de Navicat ni reinicios a ciegas.

## Estado

| Phase | Alcance | Estado |
| --- | --- | --- |
| **1** | Diseño + manifiesto dry-run (CLI, API, Angular preview) | `DONE` |
| **2** | Writer research + PoC staging | `DONE` |
| **3A** | Clases D2O `Item` + round-trip/clone staging | `DONE` |
| **3B** | D2I writer staging prototype | `DONE` |
| **3C** | Paquete staging completo + validador | `DONE` |
| **4** | Backup/recovery pipeline + publish lane (sin publish real) | `DONE` |
| **5** | Patch controlado en copia backup + launcher | `NEXT` |

## Documentos

- [Phase 1 — pipeline dry-run](./client-item-publication-pipeline-phase1.md)
- [Contrato del manifiesto](./client-item-publication-manifest-contract.md)
- [Phase 3A — D2O Item class](./client-publication-phase3a-d2o-item-class.md)
- [Phase 3B — D2I writer](./client-publication-phase3b-d2i-writer.md)
- [Phase 3B — D2I research](./client-publication-phase3b-d2i-writer-research.md)
- [Formato D2I](./client-d2i-format-notes.md)
- [Append D2I report](./client-d2i-append-report.md)
- [Phase 3C — staging package](./client-publication-phase3c-staging-package.md)
- [Package validator](./client-publication-package-validator.md)
- [Package 12617 report](./client-publication-package-12617-report.md)
- [Phase 4 — backup/recovery](./client-publication-phase4-backup-recovery.md)
- [VPS operations guide](./vps-publication-operations-guide.md)

## Herramientas

Dry-run manifest:

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode dry-run --item-id 12617 \
  --output "Infrastructure/temporal-artifacts/client-item-publication/12617"
```

D2O staging (Phase 3A):

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-clone-item --source-item-id 7754 --target-item-id 12617 \
  --output "Infrastructure/staging-client/d2o-phase3a"
```

D2I staging (Phase 3B):

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2i-inspect --output "Infrastructure/staging-client/i18n-phase3b"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2i-roundtrip --output "Infrastructure/staging-client/i18n-phase3b"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2i-append-text --output "Infrastructure/staging-client/i18n-phase3b" \
  --es-name "Dofus de los Hielos" \
  --es-description "Dofus de los Hielos creado para pruebas controladas del pipeline de publicación." \
  --en-name "Ice Dofus" \
  --en-description "Ice Dofus created for controlled publication pipeline testing."
```

Paquete staging Phase 3C (layout `data/common` + `data/i18n`, 12617):

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode stage-item-publication \
  --output "Infrastructure/staging-client/publication-package-phase3c/12617" \
  --source-item-id 7754 --target-item-id 12617 \
  --es-name "Dofus de los Hielos" \
  --es-description "Dofus de los Hielos creado para pruebas controladas del pipeline de publicación." \
  --en-name "Ice Dofus" \
  --en-description "Ice Dofus created for controlled publication pipeline testing."

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode validate-publication-package \
  --package "Infrastructure/staging-client/publication-package-phase3c/12617"
```

## Reglas

- No modificar `Client2.3.7` original.
- Staging bajo `Infrastructure/staging-client/` (gitignored).
- Phase 4: backups locales + inventario VPS; restore execute solo sandbox/local.
- No escribir producción ni modificar `Client2.3.7` real.

## Phase 4 — backup & lane

```powershell
.\Infrastructure\scripts\PublicationBackup\backup-client.ps1
.\Infrastructure\scripts\PublicationBackup\update-publish-lane.ps1
```

API: `GET /api/admin/v1/publication/backup-status`  
UI: `/admin/publication`
