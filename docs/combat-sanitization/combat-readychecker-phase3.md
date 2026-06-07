# Phase 3 — ReadyChecker y hand-off de turno

**Rama:** `feature/combat-readychecker-phase3`  
**Estado código:** **IMPLEMENTADO** — build OK; QA VPS pendiente operador  
**Baseline:** [combat-vps-telemetry-analysis-20260606.md](./combat-vps-telemetry-analysis-20260606.md)

## Problema

Tras cada fin de turno, Sunshine avanzaba **inmediatamente** al siguiente actor. El cliente enviaba `GameFightTurnReadyMessage` (109× en baseline) pero el servidor **no esperaba** ese ACK antes de `StartTurn` del jugador.

Resultado: el timer de 35 s del jugador empezaba mientras el cliente aún procesaba animaciones / cola de turnos (peor con invocaciones).

## Solución

Flujo objetivo (patrón Rollback):

```txt
Actor termina turno (IA / jugador / invocación)
        ↓
EndTurn → mensajes TurnEnd + TurnReadyRequest
        ↓
ReadyChecker espera ACK de todos los CharacterFighter (o timeout 5 s)
        ↓
TryAdvanceTurn → siguiente actor → StartTurn
```

## Configuración

| Clave | Default | Descripción |
| --- | --- | --- |
| `CombatReadyCheckerEnabled` | `true` | Feature flag |
| `CombatReadyCheckerTimeoutMs` | `5000` | Timeout si cliente no responde |

Variables de entorno / `Config.xml` vía `GameConfig` (mismo patrón que `CombatTelemetryEnabled`).

## Telemetría

Eventos JSONL en `combat-turn-flow-*.jsonl`:

```txt
ReadyCheckerStarted
ReadyCheckerAck
ReadyCheckerTimeout
ReadyCheckerAdvanceTurn
ReadyCheckerIgnored
```

Campos extra: `nextActorId`, `nextActorName`, `reason`, `waiterIds`, `elapsedMs`.

## Validación local

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.csproj" /nr:false
```

Lab (opcional):

```powershell
$env:COMBAT_HEALTH_LAB = "1"
$env:FIGHT_TELEMETRY_ENABLED = "true"
.\infrastructure\artifacts\combat-health\run-local-combat-lab.ps1
```

## Criterio cierre código

- [x] `ReadyChecker` implementado
- [x] `GameFightTurnReadyMessage` alimenta `ToggleReady`
- [x] `EndTurn` ya no hace `StartTurn` inmediato (con flag ON)
- [x] Telemetría `ReadyChecker*`
- [x] Build OK
- [x] Sin cambios IA / spells / summons
- [ ] QA VPS post-deploy (operador)

## Referencias

- [combat-readychecker-phase3-diff.md](./combat-readychecker-phase3-diff.md)
- [combat-readychecker-phase3-qa-plan.md](./combat-readychecker-phase3-qa-plan.md)
- Rollback: `Rollback.World\Game\Fights\ReadyChecker.cs`, `Fight.cs` (`TryAdvanceTurn`)
