# Esquema de logs de combate (JSONL)

**Schema:** `combat-telemetry-phase2-jsonl-1`  
**Estado:** Phase 2 baseline — observación sin cambio funcional.

## Ubicación

| Entorno | Ruta por defecto |
| --- | --- |
| Sunshine (runtime) | `{BaseDirectory}/logs/combat/` |
| Lab local (recomendado) | `Infrastructure/logs/combat/` vía `CombatTelemetryLogDirectory` |
| Spell casts | `{LogDirectory}/spell-casts/spell-casts-YYYYMMDD-HHMMSS.jsonl` |
| Turn flow | `{LogDirectory}/combat-turn-flow-YYYYMMDD-HHMMSS.jsonl` |

Los directorios de logs y `Infrastructure/temporal-artifacts/` están gitignored.

## Formato

Un evento por línea (**JSONL**). Campos comunes:

| Campo | Tipo | Descripción |
| --- | --- | --- |
| `schemaVersion` | string | Siempre `combat-telemetry-phase2-jsonl-1` |
| `timestampUtc` | string ISO-8601 | Momento UTC del evento |
| `event` | string | Nombre del evento (ver tablas abajo) |
| `fightId` | int | Id del combate |
| `turnId` | string | `{round}-{actorId}` |
| `actorId` | int? | Actor involucrado |
| `actorName` | string? | Nombre legible |
| `actorType` | string? | Tipo CLR (`CharacterFighter`, `MonsterFighter`, …) |
| `durationMs` | long? | Duración cuando aplica |
| `threadId` | int | Hilo administrado que emitió el evento |
| `detail` | string? | Contexto libre (`key=value` separado por `;`) |

## Turn flow (`combat-turn-flow-*.jsonl`)

| Evento | Cuándo |
| --- | --- |
| `FightStarted` | Combate entra en estado Fighting |
| `TurnStarted` | `FightActor.StartTurn` |
| `TurnOwner` | Fighter playing asignado |
| `TurnTimerStarted` | Timer de fin de turno arrancado |
| `AiStarted` | Entrada IA (`AIFighter.Play`) |
| `AiActionSelected` | IA eligió acción (p. ej. cast) |
| `AiFinished` | IA terminó ciclo |
| `EndTurnRequested` | Entrada `EndTurn` |
| `EndTurnCompleted` | Salida `EndTurn` |
| `NextTurnRequested` | Pre-avance de timeline |
| `NextTurnStarted` | Nuevo turno en timeline |
| `TimerElapsed` | Callback del timer (`detail=timer=EndTurn`) |
| `FightEnded` | Combate finalizado |
| `GameFightTurnReadyMessageReceived` | Cliente envió `GameFightTurnReadyMessage` (solo observación) |

## Spell casts (`spell-casts-*.jsonl`)

| Evento | Cuándo |
| --- | --- |
| `SpellCastStarted` | Tras validación OK, antes de secuencia |
| `SpellCastResolved` | Cast completado sin abortar combate |
| `SpellCastFailed` | `CanCastSpell` rechazó el cast |
| `EffectResolved` | Handler de efecto encontrado y ejecutado |
| `EffectFailed` | Handler ausente o excepción en dispatch |

Campos adicionales:

| Campo | Tipo |
| --- | --- |
| `spellId` | int |
| `spellLevel` | short |
| `targetIds` | string CSV (celdas/ids) |
| `effectIds` | string CSV |
| `result` | string (`OK`, `HandlerMissing`, código de cast, …) |
| `error` | string? |

## Configuración Sunshine

Claves en `GameConfig` / variables de entorno:

| Clave | Env alternativa | Default producción | Default lab (`COMBAT_HEALTH_LAB=1`) |
| --- | --- | --- | --- |
| `CombatTelemetryEnabled` | `FIGHT_TELEMETRY_ENABLED` | `false` | `true` |
| `CombatTelemetryLogDirectory` | `FIGHT_TELEMETRY_LOG_DIRECTORY` | `{BaseDirectory}/logs/combat` | mismo |
| `CombatTelemetryWriteTurnFlow` | — | `true` si enabled | `true` |
| `CombatTelemetryWriteSpellCasts` | — | `true` si enabled | `true` |

## Analizador

Entrada: directorio con `*.jsonl` y/o legacy `*.log` (`[FIGHT-TURN]`, `[FIGHT-PERF]`).

Salida:

```txt
Infrastructure/temporal-artifacts/combat-telemetry/report.md
Infrastructure/temporal-artifacts/combat-telemetry/report.json
```

Proyecto: `Infrastructure/scripts/CombatTelemetryAnalyzer/`.
