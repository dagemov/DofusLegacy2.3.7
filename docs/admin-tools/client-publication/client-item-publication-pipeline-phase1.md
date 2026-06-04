# Phase 1 — Client Item Publication Pipeline (dry-run)

Date: `2026-06-04`  
Branch: `feature/client-item-publication-pipeline-phase1`

## Objetivo

Base read-only para publicar items custom al cliente: manifiesto que describe qué falta, qué archivos tocar y por qué no se puede automatizar aún.

## Entregables

| # | Entregable | Ubicación |
| --- | --- | --- |
| 1 | Auditoría writers | [client-d2o-d2i-write-capability-audit.md](./client-d2o-d2i-write-capability-audit.md) |
| 2 | Contrato manifiesto | [client-item-publication-manifest-contract.md](./client-item-publication-manifest-contract.md) |
| 3 | Servicio manifiesto | `ItemPublicationManifestService` |
| 4 | CLI dry-run | `Infrastructure/scripts/ClientItemPublicationPipeline/` |
| 5 | API | `GET .../publication-manifest` |
| 6 | Angular preview | `/admin/items/:id/publication-status` |

## Validación

### Build

```powershell
dotnet build "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj"
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj" /nr:false
cd Angular-tools/Admin/RollblackLegacy.Admin.Angular; npm run build
```

### CLI

```powershell
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- `
  --mode dry-run --item-id 12617 `
  --output "Infrastructure/temporal-artifacts/client-item-publication/12617"
```

Requiere conexión DB (appsettings `Development.local` o VPS example).

### API

```txt
GET /api/admin/v1/items/7754/publication-manifest
GET /api/admin/v1/items/12617/publication-manifest
```

## Criterio de cierre

- [x] Auditoría D2O/D2I documentada
- [x] Modelo manifiesto (Contracts + servicio)
- [x] CLI genera JSON + MD sin escribir cliente
- [x] API read-only
- [x] Angular preview sin botón publicar
- [x] Sin modificar cliente real / DB / VPS

## Phase 2 (no iniciar sin aprobación)

- Prototipo writer en **copia staging** de `Items.d2o` + i18n
- Validación diff + QA launcher
