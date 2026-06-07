# Análisis telemetría VPS — sesión 2026-06-06

**Fecha análisis:** 2026-06-06  
**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**Operador:** ~15 combates reales (sesión manual); logs capturan **21** segmentos `FightStarted`/`FightEnded`  
**Collect local:** `Infrastructure/temporal-artifacts/combat-logs/vps/20260606-144931/`  
**Reportes:** `Infrastructure/temporal-artifacts/combat-telemetry/report.{md,json,html}`

## Resumen ejecutivo

La sesión **reproduce el problema de hand-off de turno en el jugador humano**, no en la IA de monstruos ni en invocaciones aisladas. Los turnos de `SummonedMonster` y `MonsterFighter` son rápidos (media ~1,5 s). Los turnos de `CharacterFighter` concentran la latencia visible: media ~19 s, **7 turnos >30 s**, y **6 `TimerElapsed` a ~35 000 ms** exactos.

Las invocaciones **correlacionan** con el problema porque alargan la cola de turnos y aumentan la espera hasta el turno del jugador; no porque la IA de la invocación sea lenta.

**Decisión recomendada:** abrir **`feature/combat-readychecker-phase3`** — portar `ReadyChecker` / `TryAdvanceTurn` desde `RollBlackServer\2.0.0\Rollback`. Opcional: instrumentar 2–3 eventos `ReadyChecker*` durante el port para cerrar la ambigüedad de telemetría (0 eventos server-side hoy).

**Telemetría VPS:** desactivada tras collect (`disable-vps-combat-telemetry.ps1`, `CONFIRM_RESTART=1`). Logs preservados en VPS (`/app/logs/combat/`) y copia local.

---

## Artefactos analizados

| Archivo | Eventos | Tamaño aprox. |
| --- | ---: | --- |
| `combat-turn-flow-20260606-165525.jsonl` | 2315 turn events | ~576 KB |
| `spell-casts/spell-casts-20260606-165525.jsonl` | 2225 spell events | ~681 KB |

Ventana UTC: `2026-06-06T16:55:25Z` → `2026-06-06T18:47Z`

---

## Respuestas a las 10 preguntas del gate

### 1. ¿Cuántos combates reales fueron capturados?

**21** pares `FightStarted` / `FightEnded` en un único `fightId=1` (contador interno del servidor; cada par = un combate distinto). Coherente con la sesión del operador (~15 declarados; posible continuación de la sesión).

- 250 turnos reconstruidos por el analyzer
- 251 `TurnStarted` en JSONL bruto

### 2. ¿Existen eventos de invocación?

**Sí.** Evidencia directa:

| Señal | Cantidad |
| --- | ---: |
| Turnos `SummonedMonster` | 68 |
| Combates con al menos una invocación | 15 / 21 |
| `AiStarted`/`AiFinished` en invocaciones | presentes con `durationMs` 1,1–3,0 s |

No hay evento explícito `SummonCreated` en JSONL; las invocaciones se identifican por `actorType=SummonedMonster` y `actorId` negativo.

### 3. ¿Aumenta la latencia cuando aparecen invocaciones?

**Parcialmente — mecanismo indirecto.**

| Cohort | Turnos | Media | Máx | >30 s |
| --- | ---: | ---: | ---: | ---: |
| `SummonedMonster` | 68 | 1 670 ms | 3 627 ms | 0 |
| `MonsterFighter` | 114 | 1 472 ms | 3 036 ms | 0 |
| `CharacterFighter` (jugador) | 63 | 18 903 ms | 35 003 ms | 7 |

Por segmento de combate:

- **Con invocaciones:** media turno 5 471 ms, máx 35 003 ms, 3 turnos >30 s
- **Sin invocaciones:** media 8 594 ms, máx 35 002 ms, 4 turnos >30 s

Las invocaciones **no** ralentizan su propio turno; **sí** multiplican turnos en la ronda y coinciden con combates donde el jugador acumula esperas largas y timers de rescate.

### 4. ¿Cuánto tarda `AiFinished` → `PlayerTurnStarted`?

Mediciones directas sobre JSONL (`AiFinished` → siguiente `TurnStarted` con `CharacterFighter`):

| Métrica | Valor |
| --- | ---: |
| Muestras | 51 |
| Media | 92 195 ms *(sesgada por outliers / AFK)* |
| Máximo | 4 050 936 ms (~67 min) |
| >5 s | 8 |
| >30 s | 4 |

Top gaps (ms): `4050936`, `414149`, `106726`, `55264`, `25695`, `22281`, `20187`, `6293`

La IA termina en **1–3 s** (`AiFinished.durationMs`); la espera visible ocurre **después**, en la transición hacia el turno del jugador.

### 5. ¿El turno del jugador empieza antes de que terminen las animaciones enemigas?

**No concluyente al 100%** (no hay marca de fin de animación cliente), pero:

- `GameFightTurnReadyMessageReceived`: **109** eventos — el cliente **sí** envía ready.
- Aun así, **6 turnos de jugador** solo avanzan por `TimerElapsed` a ~35 s.
- Los peores turnos de jugador (`CharacterFighter`) alcanzan 26–35 s sin `EndTurn` explícito del jugador en el analyzer.

**Interpretación:** el servidor entrega turno al jugador, pero el **cierre / hand-off** no progresa por la vía normal; el rescate es el timer de 35 s. Compatible con ReadyChecker ausente o no conectado al flujo post-IA.

### 6. ¿Existe evidencia de ReadyChecker faltante?

**Sí — evidencia fuerte, con matiz de instrumentación.**

| Evento | Count |
| --- | ---: |
| `ReadyCheckerStart` / `Ack` / `Timeout` | **0 / 0 / 0** |
| `GameFightTurnReadyMessageReceived` | **109** |
| `TimerElapsed` en turno jugador | **6** |

El cliente manda ready; el servidor **no registra** ningún ciclo ReadyChecker. Patrón típico de handler ausente o bypassed en Sunshine vs Rollback.

### 7. ¿Aparecen timers cercanos a 35 000 ms?

**Sí — 6 casos exactos**, todos en `CharacterFighter`:

| TurnId | ActorId | TurnStart→TimerElapsed |
| --- | ---: | ---: |
| `2-373` | 373 | 35 000 ms |
| `3-373` | 373 | 35 000 ms |
| `4-373` | 373 | 35 000 ms |
| `3-359` | 359 | 34 999–35 002 ms |
| `4-359` | 359 | 35 001 ms |

`report.json`: `timerElapsedCount=6`, `turnsEndedByTimerCount=6`, causa `TIMER_FALLBACK`.

### 8. ¿Existen hechizos o efectos que nunca terminan correctamente?

**Casi no — aislado.**

| Métrica | Valor |
| --- | ---: |
| `SpellCastStarted` / `Resolved` | 521 / 521 |
| `SpellCastFailed` | 0 |
| `EffectFailed` | 1 |

Único fallo JSONL:

```txt
EffectFailed spellId=2934 effectIds=417 result=HandlerMissing actor=MonsterFighter Gorgouille
```

Logs servidor durante disable: `Cannot dispatch Effect_SubPushDamageReduction on spell 2934`, `Fighter 373 try to cast spell 30`. **Secundarios** respecto al stall de 35 s del jugador.

### 9. ¿Hay diferencias entre combates con y sin invocaciones?

| Dimensión | Con invocaciones (15) | Sin invocaciones (6) |
| --- | --- | --- |
| Turnos invocación | 68 (rápidos) | 0 |
| Turnos jugador lentos (>5 s) | mayoría en peleas largas con cola mixta | también presentes |
| `TimerElapsed` 35 s | en peleas con invocaciones activas | posible en peleas sin invoc |
| Observación operador | “empeora con invocaciones” | coherente con más actores en cola |

**Conclusión:** la diferencia no es velocidad de IA de invocación, sino **profundidad de la cola de turnos** y **stall del turno humano** al volver el control.

### 10. ¿Phase 3 o Phase 2B?

| Criterio | Evidencia | Peso |
| --- | --- | --- |
| Hand-off roto | 6× timer 35 s en jugador; ready cliente sin ReadyChecker server | **Alto** |
| IA lenta | `AiFinished` 1–3 s en monstruos/invocaciones | Descartado como causa primaria |
| Spells rotos | 1 effect aislado | Bajo |
| Telemetría ambigua | 0 eventos `ReadyChecker*` (¿no implementado o no logueado?) | Medio |

**Recomendación: Phase 3** (`feature/combat-readychecker-phase3`).

Phase 2B solo si se quiere una pasada mínima previa: añadir 3 eventos (`ReadyCheckerStarted`, `ReadyCheckerAck`, `ReadyCheckerTimeout`) y re-capturar 5 combates — **no bloqueante** dado el patrón timer+ready ya claro.

---

## Hallazgos técnicos adicionales

### `ENDTURN_NOT_CALLED` en analyzer vs JSONL bruto

El analyzer marca 250 turnos con `ENDTURN_NOT_CALLED`, pero el JSONL **sí** contiene `EndTurnRequested` / `EndTurnCompleted` (239/230) en rutas de IA. El analyzer busca un evento legacy `EndTurn`; los turnos de jugador que terminan por `TimerElapsed` no pasan por EndTurn manual. **No contradice** la hipótesis de hand-off roto en jugador.

### Cadena de eventos saludable (invocación — ejemplo)

```txt
AiStarted → AiFinished (1,6 s) → EndTurnRequested → EndTurnCompleted → NextTurnRequested → NextTurnStarted
```

La cadena falla en la transición **hacia** y **durante** el turno del `CharacterFighter`, no en invocaciones.

### Collect script

`collect-vps-combat-logs.ps1` falló en `scp` por orden de argumentos en Windows OpenSSH. Collect manual exitoso vía `docker cp` remoto + `scp -i key ... host:/tmp/...`. Corregir splatting de `$sshArgs` en el script (pendiente menor).

---

## Próximos pasos (sin tocar combate aún en esta sesión)

1. Crear rama `feature/combat-readychecker-phase3` desde la rama actual o `main` según convención del equipo.
2. Comparar `ReadyChecker` / `TryAdvanceTurn` en `RollBlackServer\2.0.0\Rollback` vs `Sunshine.WorldServer`.
3. Port mínimo + telemetría ReadyChecker (2B inline) + lab local antes de redeploy VPS.
4. **No** tocar IA de invocaciones ni spell math como primera intervención.

## Referencias

- [combat-real-telemetry-gate.md](./combat-real-telemetry-gate.md)
- [combat-vps-telemetry-deploy-gate.md](./combat-vps-telemetry-deploy-gate.md)
- [combat-turn-latency-analysis-report.md](../../Infrastructure/temporal-artifacts/combat-telemetry/combat-turn-latency-analysis-report.md) *(gitignored)*
- [combat-turn-transition-phase2-report.md](../../Infrastructure/temporal-artifacts/combat-telemetry/combat-turn-transition-phase2-report.md) *(gitignored)*
