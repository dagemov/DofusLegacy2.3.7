# Auditoría del sistema de combate — DofusLegacy2.3.7 vs RollBlackServer

**Fecha:** 2026-06-06  
**Rama:** `feature/combat-sanitization-phase1-audit`  
**Estado:** Phase 1 — solo auditoría, sin fixes.

## Objetivo

Inventariar y comparar la arquitectura de combate entre:

| Repo | Ruta | Rol |
| --- | --- | --- |
| **Actual** | `C:\Users\Hombr\source\repos\DofusLegacy2.3.7` | Emulador Sunshine WorldServer en producción/beta |
| **Referencia** | `C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback` | Emulador donde ya se corrigió el hand-off de turnos (~35s → ~5s) |

## Mapa de clases — emulador actual (Sunshine)

### Núcleo

| Clase | Ruta | Responsabilidad |
| --- | --- | --- |
| `Fight` | `Sunshine.WorldServer/Game/Fights/Fight.cs` | Máquina de estados, timeline, timers, secuencias |
| `FightManager` | `.../FightManager.cs` | Factory PvM, duelo, agresión, PvT |
| `TimeLine` | `.../TimeLine.cs` | Orden de turnos + número de ronda |
| `FightActor` | `.../Actors/Fighters/FightActor.cs` | `StartTurn`, `EndTurn`, cast, buffs, muerte |
| `CharacterFighter` | `.../CharacterFighter.cs` | Jugador humano |
| `AIFighter` | `.../AIFighter.cs` | Driver AI async (`PlayAIAsync`) |
| `MonsterFighter` | `.../MonsterFighter.cs` | Monstruo PvM |

### IA

| Clase | Ruta | Notas |
| --- | --- | --- |
| `MonsterAttackAI` | `.../Actors/AI/MonsterAttackAI.cs` | **IA principal** de monstruos (cast/move loop) |
| `AIDispatcher` / `AITypeHandler` | `.../Actors/AI/` | Legacy; solo tax collector en práctica |
| `AIManager` | `.../AIManager.cs` | Registro de handlers no usados en PvM normal |

### Efectos y hechizos

| Clase | Ruta |
| --- | --- |
| `EffectManager` | `.../Game/Effects/EffectManager.cs` |
| `EffectDispatcher` | `.../EffectDispatcher.cs` |
| `SpellCastManager` | `.../Game/Spells/Casts/SpellCastManager.cs` |
| `Summon` (handler) | `.../Effects/Spells/Summon/Summon.cs` |

### Handlers de red

| Handler | Ruta | Mensajes clave |
| --- | --- | --- |
| `ContextHandler` | `.../Handlers/Context/ContextHandler.cs` | Turn start/end, **turn ready (vacío)**, turn finish |
| `ActionsHandler` | `.../Handlers/Actions/ActionsHandler.cs` | Secuencias, acciones de combate |

### Cliente (solo assets en repo actual)

| Asset | Ruta |
| --- | --- |
| `Fight.swf` / UI XML | `Client2.3.7/ui/Ankama_Fight/` |
| `ReadyChecker`, `TimerElapsed` | **No en código servidor** — lógica cliente SWF |

## Mapa de clases — emulador antiguo (Rollback)

### Núcleo equivalente

| Clase | Ruta | Diferencia vs Sunshine |
| --- | --- | --- |
| `Fight` | `Rollback.World/Game/Fights/Fight.cs` | `NewTurn`, `TryBeginTurnEnd`, `TryAdvanceTurn` |
| `ReadyChecker` | `.../ReadyChecker.cs` | **Existe** — espera ACK cliente 5s |
| `FightTelemetry` | `.../FightTelemetry.cs` | **Existe** — `FIGHT-PERF` + `FIGHT-TURN` |
| `FightTimer` + generación | `Fight.cs` | Rechaza callbacks stale |
| `Brain` | `.../AI/Brain.cs` | IA síncrona inline en `StartTurn` |
| `SpellCast` | `.../SpellCast.cs` | Contenedor de cast + handlers |
| `FightHandler` | `.../Handlers/Fights/FightHandler.cs` | Handlers de combate dedicados |

No existe `MonsterAI` — la IA es `Brain` sobre `AIFighter`.

## Hallazgos críticos (actual)

### 1. Sin `ReadyChecker` en servidor

Sunshine **no implementa** espera post-`EndTurn`. Tras enviar `GameFightTurnReadyRequestMessage`, avanza de inmediato:

```394:447:Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs
        public void EndTurn()
        {
            // ...
            ContextHandler.SendGameFightTurnEndMessage(currentFight.Clients, this);
            ContextHandler.SendGameFightTurnReadyRequestMessage(currentFight.Clients, this);
            // ...
            currentFight.FighterPlaying = currentFight.GetFighterPlaying();
            // ...
            if (currentFight.FighterPlaying != null)
                currentFight.FighterPlaying.StartTurn();
        }
```

El handler de ACK está vacío:

```171:174:Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Handlers/Context/ContextHandler.cs
        public static void HandleGameFightTurnReadyMessage(WorldClient client, GameFightTurnReadyMessage message)
        {

        }
```

**Hipótesis documentada:** el turno del jugador puede iniciar (timer 35s incluido) mientras el cliente aún reproduce animaciones del monstruo anterior.

### 2. IA monstruo async con delays configurables

```112:152:Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/AIFighter.cs
        private static int TurnStartDelayMs => Math.Max(0, GameConfig.GetInt("MonsterTurnStartDelayMs", 100));
        private static int TurnEndDelayMs => Math.Max(0, GameConfig.GetInt("MonsterTurnEndDelayMs", 80));
        // ...
        public void PlayAI() { _ = PlayAIAsync(); }
```

`MonsterAttackAI` añade `MonsterActionDelayMs`, `MonsterMovementDelayMs`, etc. (hasta 20 acciones/turno). Esto añade latencia **intencional** server-side, distinta del bug de 35s del antiguo emu.

### 3. Sin telemetría de combate estructurada

Rollback tiene `FightTelemetry` + `CombatTelemetryAnalyzer`. Sunshine solo tiene logs de error dispersos (`Logger`) y `SpellHistory` por fighter. **No hay** `FIGHT-TURN` / `FIGHT-PERF`.

### 4. Sincronización completa en límites de turno

`StartTurn` y `EndTurn` envían `GameFightSynchronizeMessage` a todos los fighters. Comentario en código advierte que esto corta animaciones cliente.

## Fix ya aplicado en Rollback (referencia)

Documentado en `RollBlackServer/.../Docs/combat/combat-readychecker-fix.es.md`:

| Antes | Después |
| --- | --- |
| `ReadyChecker` timeout no avanzaba turno | `TryAdvanceTurn` seguro tras timeout/ACK |
| Timer 35s rescataba como `EndTurn("Timer")` | Timer dispuesto en primer `EndTurn` válido |
| Demora visible ~35s post-AI | ~5s (timeout ReadyChecker esperando humanos) |

Commits de referencia: `51868d84`, `eae901dc`, `c691e45d`, merge PR `feature/combat-readychecker-turn-advance-fix`.

## Clases candidatas a portar/adaptar (Phase 3+)

| Prioridad | Componente antiguo | Acción propuesta |
| --- | --- | --- |
| P0 | `ReadyChecker` + `TryAdvanceTurn` | Portar patrón a `Fight`/`FightActor` Sunshine |
| P0 | `FightTelemetry` | Migrar/adaptar eventos `FIGHT-TURN` |
| P1 | Timer generation / stale callback guard | Portar de `Fight.cs` Rollback |
| P1 | `FightSequence` ack tracking | Evaluar si Sunshine necesita capa similar |
| P2 | `Brain` vs `MonsterAttackAI` | **No tocar** hasta cerrar turn transition |
| P3 | Summon/glyph/trap handlers | Phase 5 — requiere spell cast telemetry |

## Problemas reportados vs evidencia

| Problema | Evidencia Phase 1 | Fase fix |
| --- | --- | --- |
| Monstruos esperan ~35s | Sunshine no tiene el bug exacto de Rollback; revisar timer `StartAction(35000)` como rescue | Phase 3 |
| Turno jugador durante animación enemiga | `HandleGameFightTurnReadyMessage` vacío + `StartTurn` inmediato | Phase 3 |
| Mensaje `{playerName}` | i18n cliente/servidor — pendiente localizar | Phase 3 QA |
| Invocaciones / bosses / venenos / glifos | Sin telemetría de cast; handlers existen pero no auditados | Phase 4–5 |

## Alcance explícitamente fuera de Phase 1

- Fixes de código en `Sunshine.WorldServer`
- Cambios en VPS
- Mezcla con Admin tools (items/spells)
- Copiar proyectos completos desde Rollback

## Referencias cruzadas

- [combat-turn-flow-comparison.md](./combat-turn-flow-comparison.md)
- [combat-telemetry-plan.md](./combat-telemetry-plan.md)
- [combat-health-lab-plan.md](./combat-health-lab-plan.md)
- Rollback: `Docs/combat/combat-runtime-audit.md`, `combat-external-ai-review-package.md`
