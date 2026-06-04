# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 4 / Phase 3A — D2O Item class staging

| Campo | Valor |
| --- | --- |
| Rama | `feature/client-item-publication-d2o-item-class-phase3a` |
| Estado | **`DONE`** |
| Docs | [client-publication-phase3a-d2o-item-class.md](../admin-tools/client-publication/client-publication-phase3a-d2o-item-class.md) |

Resultados:

- Clases `[D2OClass]`: `Item`, `Weapon`, `EffectInstance`, `EffectInstanceInteger`, `EffectInstanceDice` en `Sunshine.Protocol/Tools/D2o/Classes/`.
- `D2OReader` lee `Items.d2o` (11067 índices).
- Round-trip staging: índice preservado; item `7754` intacto.
- Clone staging `7754` → `12617` (`typeId=23`, `iconId=23012`, `appearanceId=0`); `nameId`/`descriptionId` heredados — **i18n pendiente Phase 3B**.
- Staging: `Infrastructure/staging-client/d2o-phase3a/Items.d2o` (gitignored).
- Cliente real / VPS / DB: **sin cambios**.

CLI:

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-clone-item --source-item-id 7754 --target-item-id 12617 \
  --output "Infrastructure/staging-client/d2o-phase3a"
```

**Siguiente:** Macro 4 / **Phase 3B** — D2I writer research/prototype.

## Macro 4 — Phase 1–2 (referencia)

| Phase | Estado |
| --- | --- |
| 1 dry-run manifest | `DONE` |
| 2 writer audit | `DONE` |

## QA Dofus de los Hielos

| Campo | Valor |
| --- | --- |
| Estado | `BLOCKED_CLIENT_TEMPLATE_MISSING` |
| Doc | [dofus-hielos-production-qa.md](../admin-tools/items-builder/items-final/dofus-hielos-production-qa.md) |

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-item-publication-d2o-item-class-phase3a
```

## Builds

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false — PASS
dotnet build "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" — PASS (net11.0)
dotnet build "Infrastructure/scripts/ClientD2oWriterResearch/ClientD2oWriterResearch.csproj" — PASS
```

## Commit sugerido

```txt
feat: add d2o item class staging publisher prototype
```
