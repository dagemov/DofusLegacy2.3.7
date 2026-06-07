# Combat Docs

Documentación de combate del emulador Sunshine.

## Macro Combat Sanitization (activo)

| Documento | Contenido |
| --- | --- |
| [combat-system-audit.md](../combat-sanitization/combat-system-audit.md) | Auditoría comparativa Sunshine vs Rollback |
| [combat-turn-flow-comparison.md](../combat-sanitization/combat-turn-flow-comparison.md) | Flujo de turnos y diferencias ReadyChecker |
| [combat-telemetry-plan.md](../combat-sanitization/combat-telemetry-plan.md) | Plan de instrumentación (Phase 1 diseño) |
| [combat-telemetry-phase2.md](../combat-sanitization/combat-telemetry-phase2.md) | **Phase 2 — telemetría JSONL implementada** |
| [combat-log-schema.md](../combat-sanitization/combat-log-schema.md) | Esquema JSONL turn flow + spell casts |
| [combat-phase2-test-plan.md](../combat-sanitization/combat-phase2-test-plan.md) | Plan de prueba Phase 2 |
| [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md) | **Gate Phase 3** — captura real obligatoria |
| [combat-health-lab-plan.md](../combat-sanitization/combat-health-lab-plan.md) | Lab local temporal |

## Lab operativo

```txt
infrastructure/artifacts/combat-health/
```

Scripts: `run-local-combat-lab.ps1`, `collect-combat-logs.ps1`, `analyze-combat-telemetry.ps1`, …

Analizador: `Infrastructure/scripts/CombatTelemetryAnalyzer/`

## Fases

| Fase | Estado |
| --- | --- |
| 1 — Auditoría | **DONE** |
| 2 — Fight Telemetry baseline | **DONE** (código + analyzer) |
| 2.5 — Gate telemetría real | **ABIERTO** — sin combates PvM capturados |
| 3 — Turn Transition Fix | **BLOQUEADA** hasta cerrar gate con logs reales |
| 4 — Spell cast deep dive | Parcial (telemetría spell en Phase 2) |
| 5 — Summons / Boss | Pendiente |
