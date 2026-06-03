# Macro 3 / Phase 1 — Sprite Preview Pipeline (plan + scaffold)

Estado: `DONE / PARTIAL`

## Objetivo

Documentar fuentes reales, levantar un extractor/auditor offline seguro y validar casos puntuales sin extracción masiva ni modificar `Client2.3.7`.

## Entregables

| # | Entregable | Estado |
| --- | --- | --- |
| 1 | [sprite-preview-source-map.md](./sprite-preview-source-map.md) | DONE |
| 2 | `Infrastructure/scripts/ItemSpritePreviewPipeline/` | DONE |
| 3 | [item-sprite-preview-phase1-report.md](./item-sprite-preview-phase1-report.md) | DONE (generado por tool) |
| 4 | Artefactos en `Infrastructure/temporal-artifacts/item-sprite-preview-audit/` | DONE (gitignored) |
| 5 | Carpeta destino `by-appearance/` (.gitkeep) | DONE |
| 6 | Angular sin cambios de runtime | DONE |

## Comandos

```bash
dotnet build "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj"

dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode audit \
  --items 7754,39,12617 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit"
```

Opcional: actualiza también el reporte en `docs/` (por defecto activado).

## Casos de control

| ItemId | Nombre | Preguntas Phase 1 |
| --- | --- | --- |
| 7754 | Dofus Ocre | Cliente conoce; **no** hay PNG curado; IconId 23012 está en D2P pero sin índice → Phase 2 o curado manual |
| 39 | Petite Amulette du Hibou | Cliente conoce; **sí** `by-icon/1001.png` → Admin ya muestra preview |
| 12617 | Dofus Tester | Cliente **no** conoce; preview imposible hasta client patch |
| 458 (appearance) | Sombrero Jalato (hipótesis) | Sin item DB de prueba; no afirmar mapping; ver `items-client-appearance-mapping-audit.md` |

## Respuestas esperadas (resumen)

### 7754

- ¿Icon preview? **No** en catálogo curado; packs D2P presentes.
- ¿Appearance preview? N/A (`AppearanceId` vacío).
- ¿Fuente? `bitmap0.d2p` / `bitmap1.d2p` (entrada no resuelta en Phase 1).
- ¿Automático? **No** en Phase 1.
- ¿Client patch? **No** (template conocido).

### 39

- ¿Icon preview? **Sí** — `by-icon/1001.png`.
- ¿Fuente? Angular curated PNG.
- ¿Automático? **Sí** (ya desplegado).
- ¿Client patch? **No**.

### 12617

- ¿Icon preview? **No**.
- ¿Client patch? **Sí** (`NEEDS_CLIENT_PATCH`).
- ¿Automático? **No** hasta publicar `Items.d2o`.

## Phase 2 — NEXT

- Investigación o implementación de lector/extractor **D2P** compatible Dofus 2.x.
- Extracción puntual de IconId → PNG temporal → copia curada (1–3 assets).
- Wiring opcional `by-appearance` cuando exista mapping verificado.

## Prohibiciones respetadas

- No extracción masiva.
- No modificar `Client2.3.7` ni D2O/D2I/D2P.
- No DB writes.
- No commitear `temporal-artifacts`.

## Rama

```txt
feature/item-sprite-preview-pipeline-phase1
```
