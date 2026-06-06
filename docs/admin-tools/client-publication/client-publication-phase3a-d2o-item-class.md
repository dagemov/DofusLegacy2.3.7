# Macro 4 / Phase 3A — D2O Item class + staging publisher PoC

Date: `2026-06-04`  
Branch: `feature/client-item-publication-d2o-item-class-phase3a`  
Estado: **`DONE`**

## Objetivo

Habilitar `D2OReader` / `D2OWriter` de Sunshine sobre `Items.d2o` en **staging** sin tocar el cliente real, VPS ni DB.

## Entregables

| # | Entregable | Estado |
| ---: | --- | --- |
| 1 | Esquema `Item` (+ efectos) documentado | [client-d2o-item-schema-report.md](./client-d2o-item-schema-report.md) |
| 2 | Clases C# `[D2OClass]` en Sunshine | `Sunshine.Protocol/Tools/D2o/Classes/*.cs` (+ entradas en `Sunshine.csproj`) |
| 3 | CLI modos `d2o-inspect-class`, `d2o-roundtrip`, `d2o-clone-item` | `Infrastructure/scripts/ClientItemPublicationPipeline` |
| 4 | Round-trip staging | [client-d2o-roundtrip-report.md](./client-d2o-roundtrip-report.md) |
| 5 | Clone `7754` → `12617` en staging | `12617` presente; identity D2O OK; i18n **pendiente** |
| 6 | Cliente real / VPS / DB | **Sin cambios** |

## Hallazgos Phase 2 resueltos

- `FindType("Item")` fallaba por ausencia de clases tipadas → **resuelto** con stubs alineados al D2O del cliente.
- `D2OWriter` tenía bugs de Windows (lock de archivo, vector write) → **parcheado** en `D2OWriter.cs` para PoC staging.

## Limitaciones conocidas (Phase 3B+)

| Tema | Estado |
| --- | --- |
| D2I writer (name/description) | **No existe** en Sunshine |
| Client identity `CLIENT_KNOWN` para `12617` | Requiere textos i18n + validación Admin (manifest) |
| Publicación launcher / cliente real | Fuera de alcance 3A |
| `EffectInstanceMinMax` / `EffectInstanceDuration` | No en `Items.d2o` de esta build |

## Siguiente

**Macro 4 / Phase 3B** — investigación/prototipo **D2I writer** (entradas `nameId` / `descriptionId` para items staging).

## Validación ejecutada

```bash
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false
dotnet build "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj"
dotnet build "Infrastructure/scripts/ClientD2oWriterResearch/ClientD2oWriterResearch.csproj"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-inspect-class --class Item --output "Infrastructure/staging-client/d2o-phase3a"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-roundtrip --output "Infrastructure/staging-client/d2o-phase3a"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-clone-item --source-item-id 7754 --target-item-id 12617 \
  --output "Infrastructure/staging-client/d2o-phase3a"
```
