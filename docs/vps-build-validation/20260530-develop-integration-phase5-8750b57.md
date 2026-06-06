# Validación VPS — integración Fase 5 (develop)

| Campo | Valor |
|-------|--------|
| Fecha | 2026-06-05 19:08 UTC |
| Rama | `develop` @ `8750b57` |
| VPS | `174.138.35.107` |
| Path test | `/opt/dofus-2.0.0-build` |
| Path prod | `/opt/dofus-2.0.0` (no modificado) |

## Higiene origin

| Acción | Estado |
|--------|--------|
| `origin/develop-build` eliminada | **OK** |
| VPS: checkout `develop` (antes `develop-build` @ `50e3093`) | **OK** |
| `git remote prune origin` en VPS | **OK** |

## Backup pre-test

| Artefacto | Ubicación |
|-----------|-----------|
| inspect JSON | `/opt/backups/sunshine-server-20260605-1907.json` |
| logs tail | `/opt/backups/sunshine-server-20260605-1907.log` |

## Procedimiento

1. `git fetch origin develop` → checkout `develop` @ `8750b57`
2. `docker stop sunshine-server`
3. `docker compose … build sunshine` → **OK**
4. `docker compose … up -d sunshine` → contenedor **Up**
5. Log: **162** efectos cargados (`[ World ] 162 Effects Loaded`)
6. Puertos **2450** / **5557** publicados en `sunshine-server`

## Resultado smoke (regression A + B)

| Criterio | Estado |
|----------|--------|
| Build Docker | **OK** |
| Runtime (WorldServer boot) | **OK** |
| EffectsLoader | 162 efectos |
| Puertos 2450 / 5557 | **OK** |
| Combate PvM/PvP/dungeons | **PENDING** (equipo in-game) |

## Notas

- Contenedor `sunshine-server` levantado desde path test (`dofus-2.0.0-build`).
- Tests C–E del [regression-checklist.md](../effects-integration-phase5/regression-checklist.md): completar en juego y actualizar `validation-results.md`.
- PR #19 (`feature/effects-integration-phase5` → `develop`) abierta; merge pendiente revisión.
