# Plan de prueba — Combat Telemetry Phase 2

**Objetivo:** Verificar que la telemetría observa combates sin alterar comportamiento.

## Precondiciones

- Rama `feature/combat-telemetry-phase2`
- Build OK: `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false`
- Lab scripts en `infrastructure/artifacts/combat-health/`

## Configuración lab

En el entorno del WorldServer (config o env):

```txt
COMBAT_HEALTH_LAB=1
CombatTelemetryEnabled=true
CombatTelemetryLogDirectory=Infrastructure/logs/combat
CombatTelemetryWriteTurnFlow=true
CombatTelemetryWriteSpellCasts=true
```

## Casos de prueba

### TP-1 — Telemetría deshabilitada por defecto

| Paso | Acción | Esperado |
| --- | --- | --- |
| 1 | Arrancar Sunshine sin flags de telemetría | Sin archivos nuevos en `logs/combat/` |
| 2 | Combate corto PvM | Comportamiento idéntico a pre-Phase 2 |

### TP-2 — Turn flow JSONL

| Paso | Acción | Esperado |
| --- | --- | --- |
| 1 | Activar telemetría + lab | `combat-turn-flow-*.jsonl` creado |
| 2 | Combate 2–3 turnos (jugador + monstruo) | Eventos: `FightStarted`, `TurnStarted`, `EndTurn*`, `NextTurn*`, `FightEnded` |
| 3 | Revisar timestamps | `EndTurnCompleted` antes de `NextTurnStarted` en el mismo ciclo |

### TP-3 — Spell cast JSONL

| Paso | Acción | Esperado |
| --- | --- | --- |
| 1 | Cast exitoso | `SpellCastStarted` → `EffectResolved`* → `SpellCastResolved` |
| 2 | Cast rechazado (sin PA, etc.) | `SpellCastFailed` con `result` del enum |
| 3 | Sin cambio de daño/AP vs baseline | Solo logs nuevos |

### TP-4 — GameFightTurnReadyMessage

| Paso | Acción | Esperado |
| --- | --- | --- |
| 1 | Durante turno jugador, cliente envía ready | Línea `GameFightTurnReadyMessageReceived` |
| 2 | Handler servidor | Sigue sin lógica funcional (no bloquea turno) |

### TP-5 — Analizador

| Paso | Acción | Esperado |
| --- | --- | --- |
| 1 | `collect-combat-logs.ps1` | Copia a `temporal-artifacts/combat-logs/local/` |
| 2 | `analyze-combat-telemetry.ps1` | Genera `report.md`, `report.json`, informes de latencia |
| 3 | JSON sintético (CI/local) | Analyzer exit code 0 |

## Criterios de cierre Phase 2

```txt
1. Telemetría configurable existe.
2. Turn flow events sin cambio de lógica.
3. Spell cast telemetry opcional existe.
4. GameFightTurnReadyMessage observado, no funcional.
5. Analyzer en repo oficial.
6. Scripts lab lo invocan.
7. Build OK.
8. Docs y handoff actualizados.
```

## Estado operador

| Área | Estado |
| --- | --- |
| Build + analyzer | **PASS** (automático) |
| Browser/Game QA | **PENDING_OPERATOR** |

Checklist operador:

- [ ] Levantar lab (`run-local-combat-lab.ps1`)
- [ ] Combate PvM simple con telemetría ON
- [ ] Confirmar `.jsonl` en `Infrastructure/logs/combat/`
- [ ] Ejecutar analyzer y revisar `report.md`
- [ ] Confirmar que no hay regresión perceptible en turnos
