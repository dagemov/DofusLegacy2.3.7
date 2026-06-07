# Gate — telemetría real antes de Phase 3

**Actualizado:** 2026-06-06  
**Estado gate telemetría:** **CERRADO** — evidencia baseline capturada  
**Estado Phase 3 código:** **IMPLEMENTADO** en `feature/combat-readychecker-phase3`  
**Estado Phase 3 QA VPS:** **PARTIAL** — deploy OK 2026-06-07; smoke pendiente ([combat-readychecker-phase3-vps-qa.md](./combat-readychecker-phase3-vps-qa.md))

## Baseline (sesión 2026-06-06)

| Item | Valor |
| --- | --- |
| Combates capturados | 21 |
| Análisis | [combat-vps-telemetry-analysis-20260606.md](./combat-vps-telemetry-analysis-20260606.md) |
| Decisión | Hand-off jugador roto → Phase 3 ReadyChecker |

## Phase 3 implementación

| Item | Estado |
| --- | --- |
| `ReadyChecker` portado | **SÍ** — ver [combat-readychecker-phase3.md](./combat-readychecker-phase3.md) |
| `GameFightTurnReadyMessage` → `ToggleReady` | **SÍ** |
| `TryAdvanceTurn` / lock transición | **SÍ** |
| Telemetría `ReadyChecker*` | **SÍ** |
| Build local | **OK** |
| Deploy VPS | **NO** — [QA plan](./combat-readychecker-phase3-qa-plan.md) |

## Preguntas del gate (baseline — respondidas)

| # | Pregunta | Resultado baseline |
| --- | --- | --- |
| 1 | Fin IA | Rápido (1–3 s) |
| 2 | EndTurn | Jugador sin EndTurn manual → timer |
| 3 | NextTurn | Stall pre-jugador |
| 4 | Hand-off | Roto — ready cliente ignorado |
| 5 | TimerElapsed ~35s | 6 en jugador |
| 6 | GameFightTurnReadyMessage | 109 recibidos, 0 ReadyChecker server |
| 7 | Spells fallidos | 0 spells; 1 effect aislado |

## Cierre gate telemetría (checklist)

- [x] VPS + captura + análisis
- [x] Decisión Phase 3 documentada
- [x] Código Phase 3 en rama dedicada
- [ ] QA VPS post-fix
- [ ] Confirmación operador fluidez

## Siguiente paso

Deploy manual según [combat-readychecker-phase3-qa-plan.md](./combat-readychecker-phase3-qa-plan.md) cuando operador apruebe.
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
