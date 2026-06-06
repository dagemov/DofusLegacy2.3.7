# Combat Docs

Documentación de combate del emulador Sunshine.

## Macro Combat Sanitization

| Documento | Contenido |
| --- | --- |
| [combat-system-audit.md](../combat-sanitization/combat-system-audit.md) | Auditoría comparativa Sunshine vs Rollback |
| [combat-turn-flow-comparison.md](../combat-sanitization/combat-turn-flow-comparison.md) | Flujo de turnos y diferencias ReadyChecker |
| [combat-telemetry-plan.md](../combat-sanitization/combat-telemetry-plan.md) | Plan de instrumentación |
| [combat-telemetry-phase2.md](../combat-sanitization/combat-telemetry-phase2.md) | Telemetría JSONL Phase 2 |
| [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md) | Gate antes de Phase 3 |
| [vps-combat-telemetry-operations.md](../combat-sanitization/vps-combat-telemetry-operations.md) | Operaciones telemetría VPS |
| [combat-vps-test-matrix.md](../combat-sanitization/combat-vps-test-matrix.md) | Matriz de pruebas VPS |

## Lab y scripts

```txt
infrastructure/artifacts/combat-health/
```

Scripts: `run-local-combat-lab.ps1`, `enable-vps-combat-telemetry.ps1`, `disable-vps-combat-telemetry.ps1`, `collect-vps-combat-logs.ps1`, `analyze-combat-telemetry.ps1`

## Fases

| Fase | Estado |
| --- | --- |
| 1 — Auditoría | **DONE** |
| 2 — Telemetría baseline | **DONE** (código + analyzer) |
| 2.5 — Gate logs reales | **ABIERTO** |
| 2.6 — VPS telemetry ops | **EN CURSO** (scripts on/off) |
| 3 — Turn Transition Fix | **BLOQUEADA** hasta evidencia |
