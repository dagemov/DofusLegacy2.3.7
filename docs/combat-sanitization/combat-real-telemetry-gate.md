# Gate — telemetría real antes de Phase 3

**Actualizado:** 2026-06-06 (post-deploy VPS)  
**Estado:** **ABIERTO** — infra lista; falta combate in-game

## Deploy VPS (2026-06-06)

| Item | Valor |
| --- | --- |
| Deploy sunshine con telemetría | **SÍ** |
| `sunshine-server` running | **SÍ** |
| Telemetría enabled en `.env` + Config.xml | **SÍ** |
| JSONL generados | **NO** (sin combates aún) |

Ver detalle: [combat-vps-telemetry-deploy-gate.md](./combat-vps-telemetry-deploy-gate.md)

## Combates probados

| Escenario | Cantidad | JSONL |
| --- | ---: | --- |
| Smoke 1 PvM | 0 | — |
| Sesión 30–50 | 0 | — |

## Preguntas del gate

| # | Pregunta | Estado |
| --- | --- | --- |
| 1 | Fin IA | Sin datos |
| 2 | EndTurn | Sin datos |
| 3 | NextTurn | Sin datos |
| 4 | Hand-off vs animaciones | Hipótesis sin confirmar |
| 5 | TimerElapsed ~35s | Sin datos |
| 6 | GameFightTurnReadyMessage | Sin datos |

## Hipótesis Phase 1

**Ni confirmada ni descartada** — auditoría código vigente; VPS ahora puede emitir logs.

## Decisión

```txt
Phase 3 (ReadyChecker): BLOQUEADA
Próximo paso: operador — smoke + 30–50 combates en VPS beta
```

## Cierre gate (checklist operador)

- [ ] Smoke genera `combat-turn-flow-*.jsonl`
- [ ] `collect-vps-combat-logs.ps1 -RunAnalyzer` → `report.html`
- [ ] `disable-vps-combat-telemetry.ps1` ejecutado
- [ ] Hallazgos documentados aquí
- [ ] Decisión Phase 3 con evidencia
