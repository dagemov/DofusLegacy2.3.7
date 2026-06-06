# Matriz de prueba — combates VPS con telemetría

**Objetivo:** 30–50 combates variados para alimentar el gate Phase 3.

## Configuración

- Telemetría ON (`enable-vps-combat-telemetry.ps1`)
- Mínimo 2 jugadores si es posible (aliado)
- Registrar hora inicio/fin de sesión

## Matriz

| # | Escenario | Cantidad sugerida | Qué observar en logs |
| --- | --- | ---: | --- |
| 1 | 1 jugador vs 1 monstruo simple | 8 | `AiFinished` → `NextTurnStarted` gap |
| 2 | 1 jugador vs 2+ monstruos | 8 | turnos encadenados |
| 3 | Monstruo con spell | 6 | `SpellCast*` en spell-casts jsonl |
| 4 | Monstruo invocador (si hay mapa) | 4 | duración turno IA |
| 5 | Boss / grupo elite | 4 | `TimerElapsed` ~35s |
| 6 | 1v1 con aliado | 4 | `TurnOwner` alternancia |
| 7 | Zona distinta / mapa nuevo | 6 | sin errores de handler |

**Total:** 40 combates (rango 30–50 OK)

## Señales gate Phase 3

| Señal | Campo en `report.json` |
| --- | --- |
| Ready message cliente | `readyMessageReceivedCount > 0` |
| Timer rescate | `timerElapsedCount`, `turnsOver30s` |
| Hand-off rápido post-IA | latencia en `combat-turn-latency-analysis-report.md` |

## Post-sesión

```powershell
disable-vps-combat-telemetry.ps1
collect-vps-combat-logs.ps1 -RunAnalyzer
```

Actualizar [combat-real-telemetry-gate.md](./combat-real-telemetry-gate.md) con hallazgos.
