# Phase 3 — Diff Sunshine vs Rollback (ReadyChecker / turn hand-off)

**Fecha:** 2026-06-06  
**Rama:** `feature/combat-readychecker-phase3`  
**Referencia:** `RollBlackServer\2.0.0\Rollback`

## Resumen

| Área | Sunshine (antes) | Rollback | Acción Phase 3 |
| --- | --- | --- | --- |
| Fin de turno | `EndTurn` → `GetFighterPlaying` → `StartTurn` inmediato | `EndTurn` → `ReadyChecker.Start` → `TryAdvanceTurn` → `NewTurn` | **Portar** |
| `GameFightTurnReadyMessage` | Solo telemetría | `SetReadyForNextTurn` → `Checker.ToggleReady` | **Portar** |
| `ReadyChecker` | Ausente | Presente con timeout 5 s | **Portar** |
| Transición concurrente | Sin lock | `_turnTransitionLock` + flags | **Portar** |
| IA / summons / spells | Sin cambios | — | **NO tocar** |

## Qué se porta

### `ReadyChecker.cs`

- Espera `GameFightTurnReadyMessage` de todos los `CharacterFighter` en combate.
- Timeout configurable (`CombatReadyCheckerTimeoutMs`, default **5000**).
- Callbacks `success` / `failure` → `TryAdvanceTurn`.
- Telemetría: `ReadyCheckerStarted`, `ReadyCheckerAck`, `ReadyCheckerTimeout`.

### `Fight.TryBeginTurnEnd` / `TryAdvanceTurn` / `AdvanceToNextTurn`

- Lock de transición para evitar doble `EndTurn` / doble `NextTurn`.
- `TryAdvanceTurn` centraliza el avance que antes vivía al final de `FightActor.EndTurn`.
- Conserva lógica Sunshine existente: slaves, Roublabot, `CheckFightEnd`.

### `FightActor.EndTurn`

- Tras `EndTurnCompleted`, inicia `ReadyChecker` (si enabled) en lugar de avanzar turno.
- `TryBeginTurnEnd` detiene timer de turno antes del hand-off.

### `CharacterFighter.SetReadyForNextTurn`

- Delega en `Fight.Checker.ToggleReady(this)`.

### `ContextHandler.HandleGameFightTurnReadyMessage`

- Si `isReady`, llama `SetReadyForNextTurn` (ya no es no-op).
- Mantiene log `GameFightTurnReadyMessageReceived`.

### Config

```txt
CombatReadyCheckerEnabled=true   # default ON
CombatReadyCheckerTimeoutMs=5000
```

### Telemetría nueva

```txt
ReadyCheckerStarted
ReadyCheckerAck
ReadyCheckerTimeout
ReadyCheckerAdvanceTurn
ReadyCheckerIgnored
```

## Qué NO se porta

| Componente Rollback | Motivo |
| --- | --- |
| `FightTimer` con generación / stale timer ignore | Sunshine usa `System.Timers.Timer`; fuera de alcance Phase 3 |
| `NewTurn` + `_sequences` / `Acknowledge` | Modelo de secuencias distinto en Sunshine |
| `SpotLaggers` con `Client.Dispose()` | Solo mensaje informativo en Sunshine (menor riesgo) |
| Refactor completo `FightActor.StartTurn` | No requerido para hand-off |
| Cambios IA (`Brain`, `AIFighter`) | Prohibido por gate |
| Spell handlers / summons / damage | Prohibido por gate |
| Timer generation en `StartAction` | Mejora futura; no bloqueante para ReadyChecker |

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Doble `NextTurn` | `_turnEndStarted` / `_turnAdvanceStarted` + `TryBeginTurnEnd` |
| Deadlock si cliente nunca manda ready | Timeout 5 s → `TryAdvanceTurn("ReadyCheckerTimeout")` |
| Peleas sin jugadores humanos | `waiters.Length == 0` → avance inmediato (patrón Rollback) |
| Regresión timer 35 s jugador | QA VPS: comparar baseline; timer solo arranca en `StartTurn` post-ready |
| Feature flag | `CombatReadyCheckerEnabled=false` restaura avance inmediato (rollback operativo) |

## Archivos tocados (Sunshine)

```txt
Sunshine.WorldServer/Game/Fights/ReadyChecker.cs                    (nuevo)
Sunshine.WorldServer/Game/Fights/CombatReadyCheckerSettings.cs      (nuevo)
Sunshine.WorldServer/Game/Fights/Fight.cs                           (TryAdvanceTurn)
Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs             (EndTurn)
Sunshine.WorldServer/Game/Actors/Fighters/CharacterFighter.cs      (SetReadyForNextTurn)
Sunshine.WorldServer/Handlers/Context/ContextHandler.cs             (handler + send)
Sunshine.WorldServer/Game/Fights/Telemetry/CombatTelemetry.cs      (eventos)
Sunshine.csproj                                                     (includes)
```

## Evidencia que motivó el port

Ver [combat-vps-telemetry-analysis-20260606.md](./combat-vps-telemetry-analysis-20260606.md):

- 109 `GameFightTurnReadyMessageReceived`, 0 `ReadyChecker*`
- 6 `TimerElapsed` ~35 s en `CharacterFighter`
- IA/invocaciones rápidas; stall en hand-off jugador
