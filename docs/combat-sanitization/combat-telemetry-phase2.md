# Combat Telemetry — Phase 2

**Rama:** `feature/combat-telemetry-phase2`  
**Base:** `feature/combat-sanitization-phase1-audit`  
**Fecha:** 2026-06-02  
**Estado:** Implementado — QA in-game `PENDING_OPERATOR`

## Objetivo

Agregar telemetría de combate en Sunshine **sin cambiar lógica funcional**. Phase 2 busca evidencia antes de portar `ReadyChecker` o tocar el avance de turnos.

## Alcance entregado

| Parte | Entregable | Estado |
| --- | --- | --- |
| 1 | `CombatTelemetry` en WorldServer | OK |
| 2 | Eventos turn flow JSONL | OK |
| 3 | Spell cast telemetry opcional | OK |
| 4 | `GameFightTurnReadyMessageReceived` (handler vacío) | OK |
| 5 | `CombatTelemetryAnalyzer` migrado + JSONL | OK |
| 6 | Scripts lab actualizados | OK |
| 7 | Documentación + handoff | OK |

## Prohibiciones respetadas

```txt
no fix ReadyChecker
no cambio lógica turnos / IA / spells / summons
no VPS / deploy
no mezcla Admin/Items
```

## Implementación Sunshine

- **Servicio:** `Sunshine.WorldServer/Game/Fights/Telemetry/CombatTelemetry.cs`
- **Instrumentación:** `Fight.cs`, `FightActor.cs`, `AIFighter.cs`, `MonsterAttackAI.cs`, `EffectDispatcher.cs`, `ContextHandler.cs`
- **Logs:** JSONL en directorio configurable (ver [combat-log-schema.md](./combat-log-schema.md))

### Hallazgo que Phase 2 debe confirmar con logs

Hipótesis Phase 1: el servidor puede llamar `StartTurn()` inmediatamente tras `EndTurn` mientras el cliente aún reproduce animaciones, porque `GameFightTurnReadyMessage` no bloquea el avance.

Phase 2 registra:

- secuencia `EndTurnRequested` → `EndTurnCompleted` → `NextTurnRequested` → `NextTurnStarted`
- llegadas de `GameFightTurnReadyMessageReceived` (actor/sesión)
- duraciones IA y casts

## Analizador y lab

```powershell
# Tras combate con telemetría activa
.\infrastructure\artifacts\combat-health\collect-combat-logs.ps1
.\infrastructure\artifacts\combat-health\analyze-combat-telemetry.ps1
```

Salida en `Infrastructure/temporal-artifacts/combat-telemetry/`.

El analizador acepta JSONL Sunshine y logs legacy Rollback (`[FIGHT-TURN]` / `[FIGHT-PERF]`).

## Validación en esta sesión

| Check | Resultado |
| --- | --- |
| `dotnet build Sunshine.sln` | OK |
| Analyzer con JSONL sintético | OK (`report.md`, `report.json`) |
| Combate real + cliente | `PENDING_OPERATOR` |

## Siguiente fase (no iniciar sin evidencia)

1. Reproducir stall ~35s en lab con DB snapshot.
2. Analizar `report.md` / `combat-turn-transition-phase2-report.md`.
3. Phase 3: port controlado de `ReadyChecker` / `TryAdvanceTurn` desde Rollback.

## Referencias

- [combat-log-schema.md](./combat-log-schema.md)
- [combat-phase2-test-plan.md](./combat-phase2-test-plan.md)
- Rollback referencia: `RollBlackServer/2.0.0/Rollback/Infrastructure/scripts/CombatTelemetryAnalyzer`
