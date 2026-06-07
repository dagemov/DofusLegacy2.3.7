# Plan de telemetría de combate

**Fecha:** 2026-06-06  
**Estado:** Phase 1 diseño — implementación en Sunshine Phase 2+.

## Objetivo

Reproducir en DofusLegacy2.3.7 la capacidad de diagnóstico que permitió cerrar el bug de turnos en Rollback, **sin** instrumentar a ciegas ni tocar VPS primero.

## Referencia — Rollback existente

| Componente | Ruta antigua |
| --- | --- |
| Emisor | `Rollback.World/Game/Fights/FightTelemetry.cs` |
| Config | `FightConfig.cs` — `FightTelemetryEnabled`, `FightTelemetryFileEnabled`, `FightTelemetryLogDirectory` |
| Analizador | `Infrastructure/scripts/CombatTelemetryAnalyzer/Program.cs` |
| Logs | `Infrastructure/logs/combat/*.log` |
| Docs | `Docs/combat/combat-telemetry-phase1.md` |

### Formatos de línea

**FIGHT-PERF** — timing de método:

```txt
[FIGHT-PERF] phase=FightActor.CastSpell fightId=42 elapsedMs=12 ...
```

**FIGHT-TURN** — ciclo de turno:

```txt
[FIGHT-TURN] fightId=42 event=EndTurnCompleted source=AI round=3 fighterId=107 ...
```

El analizador clasifica causas de stall: `TIMER_FALLBACK`, `READYCHECKER_WAIT`, `ENDTURN_NOT_CALLED`, `PENDING_SEQUENCE`, `AI_SLOW`, etc.

## Eventos obligatorios — Sunshine (nuevo)

Migrar/adaptar con nombres alineados al analizador:

| Evento | Cuándo | Campos mínimos |
| --- | --- | --- |
| `FightStarted` | `StartFight` completo | fightId, fightType, mapId |
| `TurnStarted` | Inicio `StartTurn` | fightId, round, fighterId, fighterType, monsterId? |
| `TurnOwner` | Fighter playing asignado | fightId, fighterId |
| `AiStarted` | Entrada `PlayAI`/`PlayAIAsync` | fightId, fighterId, monsterId |
| `AiActionSelected` | Cast/move elegido | fightId, actionType, spellId?, cell? |
| `SpellCastStarted` | Pre-validación cast | fightId, spellId, spellLevel, casterId |
| `SpellCastResolved` | Post-handlers | fightId, spellId, success, durationMs |
| `AnimationWaitStarted` | *(cliente o proxy)* | fightId, sequenceId? |
| `AnimationWaitEnded` | ACK secuencia | fightId, sequenceId?, elapsedMs |
| `EndTurnRequested` | Entrada `EndTurn` | fightId, source (AI/Player/Timer) |
| `ReadyCheckerTimeout` | Tras portar ReadyChecker | fightId, laggers[], elapsedMs |
| `NextTurnRequested` | Pre-avance timeline | fightId, source |
| `NextTurnStarted` | Nuevo `StartTurn` | fightId, fighterId |
| `TimerElapsed` | Callback timer | fightId, timerType, generation, stale? |
| `FightEnded` | `EndFight` | fightId, rounds, durationMs |

### Log dedicado — spell casts (Phase 4)

Directorio:

```txt
Infrastructure/logs/combat/spell-casts/
```

Archivo por sesión:

```txt
spell-casts-YYYYMMDD-HHMMSS.log
```

Campos por línea:

```txt
fightId turnId casterId casterName spellId spellLevel targetIds effectIds result durationMs
```

## Ubicación de logs

### Lab local

```txt
Infrastructure/logs/combat/               # telemetría general
Infrastructure/logs/combat/spell-casts/ # casts (Phase 4)
Infrastructure/temporal-artifacts/combat-logs/local/  # exports del lab
```

### VPS (solo tras deploy beta acordado)

```txt
/opt/dofus-2.0.0/Infrastructure/logs/combat/   # o ruta acordada en container
```

Recuperación:

```powershell
Infrastructure/artifacts/combat-health/collect-vps-combat-logs.ps1
```

Destino local (gitignored):

```txt
Infrastructure/temporal-artifacts/combat-logs/vps/YYYYMMDD-HHMMSS/
```

## Implementación propuesta en Sunshine

### Fase 2 — scaffolding

1. Crear `Sunshine.WorldServer/Game/Fights/FightTelemetry.cs` (adaptado de Rollback)
2. Flags en config (`appsettings` / `GameConfig`):
   - `FightTelemetryEnabled`
   - `FightTelemetryFileEnabled`
   - `FightTelemetrySlowThresholdMs` (default 50)
   - `FightTelemetryLogDirectory` → `Infrastructure/logs/combat`
3. Copiar `CombatTelemetryAnalyzer` a `infrastructure/scripts/CombatTelemetryAnalyzer/`
4. Ajustar `PreferredPhaseOrder` para fases Sunshine (`MonsterAttackAI.PlayAsync`, etc.)

### Fase 3 — instrumentar turn transition

Puntos de inyección prioritarios:

- `FightActor.StartTurn` / `EndTurn`
- `Fight.ActionElapsed`
- `ContextHandler.HandleGameFightTurnReadyMessage` (cuando se implemente)
- `AIFighter.PlayAIAsync` (inicio/fin)

### Fase 4 — spell cast telemetry

- `FightActor.CastSpell` inicio/fin
- `EffectDispatcher` por handler
- Archivo `spell-casts-*.log` separado

## Analizador — uso

### Fuente antigua (mientras no esté migrado)

```powershell
dotnet run --project "C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback\Infrastructure\scripts\CombatTelemetryAnalyzer" `
  -- --input "Infrastructure/logs/combat" `
  --output "docs/combat-sanitization/reports/telemetry-analysis.md"
```

### Fuente objetivo (repo oficial)

```powershell
Infrastructure/artifacts/combat-health/analyze-combat-telemetry.ps1 `
  -InputDirectory "Infrastructure/logs/combat" `
  -OutputDirectory "docs/combat-sanitization/reports"
```

Genera:

1. `combat-telemetry-analysis-report.md`
2. `combat-turn-latency-analysis-report.md`
3. `combat-turn-transition-phase2-report.md`

## Reglas

| Permitido | Prohibido |
| --- | --- |
| Logs locales intensivos | Commitear `.log` pesados |
| Copiar analizador | Commitear dumps DB |
| Activar en VPS beta con recolección | Activar en prod sin backup |
| Reportes markdown en `docs/combat-sanitization/reports/` | Secrets en scripts |

## Validación por fase

| Fase | Gate |
| --- | --- |
| 2 | Script `analyze-combat-telemetry.ps1` corre contra log de prueba |
| 3 | Informe muestra gap EndTurn→NextTurn medible |
| 4 | `spell-casts-*.log` parseable |
| 5 | Summon events correlacionables con cast log |

## Debate local vs VPS

**Decisión:** híbrido documentado en [combat-health-lab-plan.md](./combat-health-lab-plan.md):

1. Diagnóstico/fix en lab local
2. PR al repo oficial
3. Deploy VPS beta
4. Prueba con amigos
5. `collect-vps-combat-logs.ps1` + analizador
