# Client Item Publication Pipeline

Macro 4 — publicar items custom del Items Builder al cliente **sin** depender de Navicat ni reinicios a ciegas.

## Estado

| Phase | Alcance | Estado |
| --- | --- | --- |
| **1** | Diseño + manifiesto dry-run (CLI, API, Angular preview) | `DONE` |
| **2** | Writer research + PoC staging | `DONE` |
| 3 | Staging publisher (Item.cs o writer genérico) | `BLOCKED` — requiere aprobación explícita |
| 3 | Launcher patch lane + QA cliente | `PENDING` |

## Documentos

- [Phase 1 — pipeline dry-run](./client-item-publication-pipeline-phase1.md)
- [Contrato del manifiesto](./client-item-publication-manifest-contract.md)
- [Auditoría capacidad escritura D2O/D2I (Phase 1)](./client-d2o-d2i-write-capability-audit.md)
- [Auditoría writers Phase 2](./client-writer-capability-audit.md)
- [PoC D2O writer](./client-writer-proof-of-concept.md)
- [Phase 2 cierre](./client-publication-phase2.md)

## Herramientas

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode dry-run --item-id 12617 \
  --output "Infrastructure/temporal-artifacts/client-item-publication/12617"
```

API read-only:

```txt
GET /api/admin/v1/items/{itemId}/publication-manifest
```

UI:

```txt
/admin/items/{itemId}/publication-status
```

## Reglas Phase 1

- No modificar `Client2.3.7` original.
- No escribir DB ni archivos D2O/D2I.
- No reiniciar VPS.
