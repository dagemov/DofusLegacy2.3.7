# Combat Docs

Documentación de combate del emulador Sunshine.

## Disponible en esta rama

| Documento | Contenido |
| --- | --- |
| [combat-health-lab-plan.md](../combat-sanitization/combat-health-lab-plan.md) | Lab local temporal |
| [vps-combat-telemetry-operations.md](../combat-sanitization/vps-combat-telemetry-operations.md) | **Operaciones telemetría VPS** |
| [combat-vps-test-matrix.md](../combat-sanitization/combat-vps-test-matrix.md) | Matriz 30–50 combates |

Docs Phase 1/2/gate viven en `feature/combat-telemetry-phase2` (merge pendiente a `devp`).

## Scripts

```txt
infrastructure/artifacts/combat-health/
```

| Script | Uso |
| --- | --- |
| `enable-vps-combat-telemetry.ps1` | Activar telemetría VPS (`-DryRun`) |
| `disable-vps-combat-telemetry.ps1` | Desactivar |
| `collect-vps-combat-logs.ps1` | Descargar JSONL + `-RunAnalyzer` |
| `analyze-combat-telemetry.ps1` | `report.md` / `report.json` / `report.html` |
| `run-local-combat-lab.ps1` | Lab local |

## Fases

| Fase | Estado |
| --- | --- |
| Telemetría código + analyzer | **En rama sprint** (cherry-pick) |
| VPS on/off scripts | **DONE** |
| Gate logs reales / Phase 3 ReadyChecker | **BLOQUEADA** |
