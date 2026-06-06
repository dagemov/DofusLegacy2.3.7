# Gate — telemetría real antes de Phase 3

**Actualizado:** 2026-06-06 (combates en curso)  
**Estado:** **ABIERTO** — infra OK, login OK, **captura en progreso**

## Deploy VPS (2026-06-06)

| Item | Valor |
| --- | --- |
| Deploy sunshine con telemetría | **SÍ** |
| `sunshine-server` running | **SÍ** |
| Cliente conecta | **SÍ** (confirmado operador) |
| Telemetría enabled en `.env` + Config.xml | **SÍ** |
| Combates reales en curso | **SÍ** (operador) |
| JSONL recolectados localmente | **NO** — pendiente `collect` post-sesión |

Ver: [combat-vps-telemetry-deploy-gate.md](./combat-vps-telemetry-deploy-gate.md)

## Combates

| Escenario | Estado |
| --- | --- |
| Smoke / sesión operador | **EN CURSO** — no interrumpir |
| JSONL en VPS `/app/logs/combat/` | Esperado durante combates |
| Análisis local | **PENDIENTE** tras `collect-vps-combat-logs.ps1 -RunAnalyzer` |

## Preguntas del gate

| # | Pregunta | Estado |
| --- | --- | --- |
| 1 | Fin IA (`AiFinished`) | Pendiente análisis |
| 2 | EndTurn | Pendiente análisis |
| 3 | NextTurn | Pendiente análisis |
| 4 | Hand-off vs animaciones | Pendiente análisis |
| 5 | TimerElapsed ~35s | Pendiente análisis |
| 6 | GameFightTurnReadyMessage | Pendiente análisis |
| 7 | Spells / effects fallidos | Pendiente análisis |

## Hipótesis Phase 1

**Ni confirmada ni descartada** — requiere JSONL reales de la sesión actual.

## Decisión

```txt
Phase 3 (ReadyChecker): BLOQUEADA
Próximo paso: collect + analyzer cuando operador termine combates
```

### Ramas según evidencia (post-análisis)

| Resultado | Acción |
| --- | --- |
| Hand-off roto | `feature/combat-readychecker-phase3` |
| Logs ambiguos | `feature/combat-telemetry-phase2b` |
| Sin reproducción | Phase 3 bloqueada — documentar |

## Cierre gate (checklist)

- [x] VPS funcional + telemetría ON
- [x] Cliente conecta
- [ ] Operador termina combates
- [ ] `collect-vps-combat-logs.ps1 -RunAnalyzer` → `report.html`
- [ ] `combat-vps-telemetry-analysis-YYYYMMDD.md` creado
- [ ] `disable-vps-combat-telemetry.ps1` tras collect
- [ ] Decisión Phase 3 / 2B con evidencia
- [ ] Hallazgos documentados aquí
