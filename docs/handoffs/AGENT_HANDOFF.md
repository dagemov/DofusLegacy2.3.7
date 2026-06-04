# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 4 / Phase 3C — Staging publication package + validator

| Campo | Valor |
| --- | --- |
| Rama | `feature/client-item-publication-staging-package-phase3c` |
| Base | `feature/client-item-publication-d2i-writer-phase3b` |
| Estado | **`DONE`** |
| Docs | [client-publication-phase3c-staging-package.md](../admin-tools/client-publication/client-publication-phase3c-staging-package.md) |

Resultados:

- Layout paquete: `Infrastructure/staging-client/publication-package-phase3c/12617/data/{common,i18n}/`
- CLI `--mode validate-publication-package` + checksums SHA-256
- Manifiesto enriquecido (`PackageId`, `ValidationStatus`, `NextManualSteps`, …)
- API `GET .../publication-manifest` — `stagingPackageStatus` / `READY_FOR_CONTROLLED_PUBLISH`
- Angular `/admin/items/12617/publication-status` — bloque staging package
- Validación 12617: `READY_FOR_CONTROLLED_PUBLISH`, nameId `63079`, descriptionId `63080`
- Cliente real / VPS / DB: **sin cambios**

**Siguiente:** Macro 4 / **Phase 4** — patch controlado solo en copia backup del cliente + launcher lane.

## Macro 4 / Phase 3B — referencia

Commit `3ceb02f` — `D2iFile`, `stage-item-publication` (layout plano Phase 3B).

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-item-publication-staging-package-phase3c
```

## Commit sugerido

```txt
feat: add client publication staging package validator
```

## Comandos QA

```bash
dotnet build "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj"
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj" /nr:false
cd Angular-tools/Admin/RollblackLegacy.Admin.Angular && npm run build

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode stage-item-publication --item-id 12617 \
  --output "Infrastructure/staging-client/publication-package-phase3c/12617"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode validate-publication-package \
  --package "Infrastructure/staging-client/publication-package-phase3c/12617"
```
