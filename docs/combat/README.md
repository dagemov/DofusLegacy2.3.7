# Combat Docs

Documentación de combate del emulador Sunshine.

## Phase 3 — ReadyChecker (activa)

| Documento | Contenido |
| --- | --- |
| [combat-readychecker-phase3.md](../combat-sanitization/combat-readychecker-phase3.md) | Implementación hand-off turno |
| [combat-readychecker-phase3-diff.md](../combat-sanitization/combat-readychecker-phase3-diff.md) | Diff Sunshine vs Rollback |
| [combat-readychecker-phase3-qa-plan.md](../combat-sanitization/combat-readychecker-phase3-qa-plan.md) | QA VPS post-fix |
| [combat-vps-telemetry-analysis-20260606.md](../combat-sanitization/combat-vps-telemetry-analysis-20260606.md) | Baseline pre-fix |

## Gates y operaciones

| Documento | Contenido |
| --- | --- |
| [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md) | Gate telemetría + estado Phase 3 |
| [combat-vps-telemetry-deploy-gate.md](../combat-sanitization/combat-vps-telemetry-deploy-gate.md) | Deploy gate VPS |
| [vps-combat-telemetry-operations.md](../combat-sanitization/vps-combat-telemetry-operations.md) | Operaciones telemetría VPS |
| [combat-vps-test-matrix.md](../combat-sanitization/combat-vps-test-matrix.md) | Matriz combates |

## Scripts

```txt
infrastructure/artifacts/combat-health/
```

| Script | Uso |
| --- | --- |
| `enable-vps-combat-telemetry.ps1` | Activar telemetría VPS |
| `disable-vps-combat-telemetry.ps1` | Desactivar |
| `collect-vps-combat-logs.ps1` | Descargar JSONL + `-RunAnalyzer` |
| `analyze-combat-telemetry.ps1` | Reportes locales |
| `run-local-combat-lab.ps1` | Lab local |

## Config Phase 3

```txt
CombatReadyCheckerEnabled=true
CombatReadyCheckerTimeoutMs=5000
```

## Fases

| Fase | Estado |
| --- | --- |
| Telemetría + analyzer | **DONE** |
| Gate baseline VPS | **CERRADO** |
| Phase 3 ReadyChecker código | **DONE** (rama `feature/combat-readychecker-phase3`) |
| Phase 3 QA VPS | **PENDIENTE** |
