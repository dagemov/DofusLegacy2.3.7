# Gate — telemetría real antes de Phase 3

**Rama:** `feature/combat-telemetry-phase2`  
**Fecha:** 2026-06-02  
**Estado gate:** **ABIERTO** — sin captura in-game real en esta sesión

## Objetivo del gate

Confirmar con logs reales si el hand-off de turnos está roto (turno humano arranca antes de que el cliente termine animaciones enemigas) **antes** de portar `ReadyChecker` / `TryAdvanceTurn`.

## Combates probados

| Escenario | Fuente | Resultado |
| --- | --- | --- |
| 1 jugador vs 1 monstruo | Lab local | **NO EJECUTADO** — requiere operador + cliente |
| 1 jugador vs 2 monstruos | Lab local | **NO EJECUTADO** |
| Monstruo con spell | Lab local | **NO EJECUTADO** |
| VPS beta (Opción B) | — | **NO APROBADO** en esta sesión |

### Por qué no hay logs reales

```txt
Infrastructure/logs/combat/          → vacío (sin combat-turn-flow-*.jsonl)
Infrastructure/temporal-artifacts/combat-logs/ → sin exportaciones locales/VPS
```

Bloqueadores verificados:

1. **OPERATOR_REQUIRED** — el agente no puede lanzar cliente Dofus ni completar combates PvM.
2. **DB lab** — requiere `sync-vps-db-snapshot.ps1` + `appsettings.Development.local.json` (gitignored) apuntando a `sunshine_lab`.
3. **VPS** — no autorizado explícitamente para esta captura.

## Evidencia disponible (no válida para Phase 3)

Única muestra existente: JSONL **sintético** de Phase 2 en `Infrastructure/temporal-artifacts/combat-telemetry/sample-input/`.

| Métrica | Sample sintético | Interpretación |
| --- | --- | --- |
| `AiFinished` → `EndTurnCompleted` | ~20 ms (monstruo ficticio) | No representa IA real |
| `EndTurnCompleted` → `NextTurnStarted` | ~50 ms | Muestra transición inmediata **diseñada** en el sample |
| `TimerElapsed` ~35000 ms | **NO** | `timerElapsedCount: 0` |
| `GameFightTurnReadyMessageReceived` | 1 evento en sample | Confirma que el **parser** funciona, no comportamiento real |
| ReadyChecker | 0 eventos | Esperado — Sunshine no lo implementa aún |

**Conclusión:** el sample valida pipeline analyzer; **no confirma ni descarta** la hipótesis de stall ~35s en producción/lab.

## Validación técnica (esta sesión)

| Check | Resultado | Notas |
| --- | --- | --- |
| `dotnet build Sunshine.csproj` | **OK** | Build del emulador |
| `dotnet build Sunshine.sln` | **BLOQUEADO** | `RollblackLegacy.Admin.Api` bloquea DLLs (VS/proceso activo) — no afecta combate |
| Analyzer sobre sample | **OK** | `report.md`, `report.json` generados |
| Analyzer sobre `Infrastructure/logs/combat` | **N/A** | Directorio sin archivos |

## Preguntas del gate — estado

| # | Pregunta | Estado |
| --- | --- | --- |
| 1 | ¿Cuándo termina IA? | **SIN DATOS** — instrumentado (`AiFinished` + `durationMs`) |
| 2 | ¿Cuándo se envía EndTurn? | **SIN DATOS** — instrumentado (`EndTurnRequested` / `EndTurnCompleted`) |
| 3 | ¿Cuándo empieza NextTurn? | **SIN DATOS** — instrumentado (`NextTurnRequested` / `NextTurnStarted`) |
| 4 | ¿Turno humano arranca con animaciones enemigas activas? | **HIPÓTESIS SIN CONFIRMAR** — requiere correlación timestamps cliente/servidor |
| 5 | ¿`TimerElapsed` ~35000 ms como rescate? | **SIN DATOS** |
| 6 | ¿`GameFightTurnReadyMessage` llega del cliente? | **SIN DATOS** en combate real |

## Hipótesis Phase 1

> Sunshine avanza turno inmediatamente tras `EndTurn` sin `ReadyChecker`; el cliente puede seguir reproduciendo animaciones.

| Veredicto | Motivo |
| --- | --- |
| **Ni confirmada ni descartada** | Auditoría de código sigue vigente; **falta correlación en logs reales** |

## Decisión

```txt
Phase 3 (ReadyChecker / TryAdvanceTurn): BLOQUEADA
Phase 2B (ampliar telemetría): PREPARADA si tras captura real la señal es ambigua
```

### Criterios para abrir Phase 3 (operador)

Tras Opción A local:

1. Al menos **2 combates PvM** con `combat-turn-flow-*.jsonl` en `Infrastructure/logs/combat/`.
2. `analyze-combat-telemetry.ps1` sin warning de `*sample*`.
3. En `report.json`:
   - `readyMessageReceivedCount > 0` en turnos de monstruo, **y**
   - `NextTurnStarted` del jugador aparece **antes** o **sin espera** tras `AiFinished` del monstruo, **o**
   - `timerElapsedCount > 0` con `turnsOver30s > 0` en sesiones con síntoma visible.

Si la captura real **no** muestra gap anómalo pero el jugador sigue reportando stall:

→ Abrir **Phase 2B**: telemetría de secuencias/ACK, fan-out de mensajes, marca `stale` en timer.

## Procedimiento operador — Opción A (recomendado)

```powershell
$env:COMBAT_HEALTH_LAB = "1"
$env:CombatTelemetryEnabled = "true"
$env:CombatTelemetryLogDirectory = "Infrastructure/logs/combat"
$env:CombatTelemetryWriteTurnFlow = "true"
$env:CombatTelemetryWriteSpellCasts = "true"

.\infrastructure\artifacts\combat-health\sync-vps-db-snapshot.ps1 -SshKey "SSH\private_key_sebas.pem"
# Configurar appsettings.Development.local.json → sunshine_lab
.\infrastructure\artifacts\combat-health\run-local-combat-lab.ps1

# Tras 3 escenarios PvM:
.\infrastructure\artifacts\combat-health\collect-combat-logs.ps1
.\infrastructure\artifacts\combat-health\analyze-combat-telemetry.ps1
```

Actualizar **este documento** con hallazgos y cambiar estado gate a **CERRADO**.

## Opción B — VPS beta

Solo con aprobación explícita. Reglas: pocos combates, telemetría off tras descarga, sin commit de logs.

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem"
.\infrastructure\artifacts\combat-health\analyze-combat-telemetry.ps1 -InputDirectory "Infrastructure\temporal-artifacts\combat-logs\vps\<timestamp>"
```

## Referencias

- [combat-telemetry-phase2.md](./combat-telemetry-phase2.md)
- [combat-log-schema.md](./combat-log-schema.md)
- [combat-phase2-test-plan.md](./combat-phase2-test-plan.md)
