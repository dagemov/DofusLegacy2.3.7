# D2O round-trip report (staging)

Date: `2026-06-04`  
Branch: `feature/client-item-publication-d2o-item-class-phase3a`  
Staging: `Infrastructure/staging-client/d2o-phase3a/`

## Resultado

| Métrica | Valor |
| --- | --- |
| Índice antes | `11067` |
| Índice después (`Items.roundtrip.d2o`) | `11067` |
| Índice preservado | `yes` |
| Item `7754` legible tras round-trip | `yes` |
| `7754` typeId / iconId / appearanceId | `23` / `23012` / `0` |
| `7754` nameId / descriptionId | `40904` / `40905` (sin cambio) |

## Comandos

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode d2o-roundtrip --output "Infrastructure/staging-client/d2o-phase3a"
```

## Notas técnicas

- `D2OReader` abre `Items.d2o` con las nuevas clases `[D2OClass]`.
- `D2OWriter` reescribe el staging cargado en memoria (`StartWriting` → `EndWriting`).
- Correcciones Sunshine aplicadas en Phase 3A: lectura sin bloqueo de archivo en `OpenWrite`, `WriteFieldVector` escribe elementos de lista (no metadatos de vector), `StartWriting` cierra el stream antes de truncar.

## Clone PoC (misma sesión staging)

| Campo | Valor |
| --- | --- |
| Origen | `7754` (Dofus Ocre) |
| Destino | `12617` |
| `typeId` | `23` |
| `iconId` | `23012` |
| `appearanceId` | `0` |
| `nameId` / `descriptionId` | heredados (`40904` / `40905`) — **i18n pendiente Phase 3B** |
| Índice contiene `12617` | `yes` |
