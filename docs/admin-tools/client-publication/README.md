# Client Item Publication Pipeline

Macro 4 — publicar items custom del Items Builder al cliente **sin** depender de Navicat ni reinicios a ciegas.

## Estado

| Phase | Alcance | Estado |
| --- | --- | --- |
| **1** | Diseño + manifiesto dry-run (CLI, API, Angular preview) | `DONE` |
| **2** | Writer research + PoC staging | `DONE` |
| **3A** | Clases D2O `Item` + round-trip/clone staging | `DONE` |
| **3B** | D2I writer research/prototype | `NEXT` |
| 3 | Launcher patch lane + QA cliente | `PENDING` |

## Documentos

- [Phase 1 — pipeline dry-run](./client-item-publication-pipeline-phase1.md)
- [Contrato del manifiesto](./client-item-publication-manifest-contract.md)
- [Auditoría capacidad escritura D2O/D2I (Phase 1)](./client-d2o-d2i-write-capability-audit.md)
- [Auditoría writers Phase 2](./client-writer-capability-audit.md)
- [PoC D2O writer](./client-writer-proof-of-concept.md)
- [Phase 2 cierre](./client-publication-phase2.md)
- [Phase 3A — Item class + staging PoC](./client-publication-phase3a-d2o-item-class.md)
- [Esquema D2O Items](./client-d2o-item-schema-report.md)
- [Round-trip staging](./client-d2o-roundtrip-report.md)

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
  --mode d2o-inspect-class --class Item --output "Infrastructure/staging-client/d2o-phase3a"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-roundtrip --output "Infrastructure/staging-client/d2o-phase3a"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-clone-item --source-item-id 7754 --target-item-id 12617 \
  --output "Infrastructure/staging-client/d2o-phase3a"
```

API read-only:

```txt
GET /api/admin/v1/items/{itemId}/publication-manifest
```

UI:

```txt
/admin/items/{itemId}/publication-status
```

## Reglas

- No modificar `Client2.3.7` original.
- Staging bajo `Infrastructure/staging-client/` (gitignored).
- No escribir DB ni reiniciar VPS en Phase 1–3A.
