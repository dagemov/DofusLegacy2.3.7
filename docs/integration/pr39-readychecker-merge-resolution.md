# PR #39 ReadyChecker merge resolution — preservando PR #32

**Fecha:** 2026-06-07  
**Rama:** `fix/merge-readychecker-with-combat-patch32`  
**Base:** `devp` (incluye [PR #32](https://github.com/dagemov/DofusLegacy2.3.7/pull/32) merged)  
**Fuente:** `origin/feature/combat-readychecker-phase3`

---

## Objetivo

Acoplar sin sustituir:

```txt
#32 = IA monstruos, castigos, spells/effects, summons, DOT/Sacrifice
#39 = ReadyChecker, TryAdvanceTurn, telemetría turn-flow, hand-off
```

---

## Archivos en conflicto (merge manual)

| Archivo | Categoría | Decisión |
| --- | --- | --- |
| `FightActor.cs` | A — Turn flow | **#39** hand-off vía `TryAdvanceTurn` cuando ReadyChecker off; **no** `StartTurn()` directo en `EndTurn` |
| `EffectDispatcher.cs` | C — Effects | **Combinar:** `FightCombatLogger` (#32) + `CombatTelemetry` (#39) |
| `Fight.cs` | A — Turn flow | **Patch manual:** restaurar `DiesAtTurnEnd` de #32 dentro de `AdvanceToNextTurn` |

## Auto-merge sin conflicto (verificado)

| Archivo | Notas |
| --- | --- |
| `MonsterAttackAI.cs` | Conserva delays `MonsterActionDelayMs` (#32) + telemetría opcional (#39) |
| `CharacterFighter.cs` | `SetReadyForNextTurn()` (#39) |
| `ContextHandler.cs` | `GameFightTurnReadyMessage` → `SetReadyForNextTurn()` + telemetría |
| `ReadyChecker.cs` | Nuevo (#39) |
| `CombatReadyCheckerSettings.cs` | Flags `CombatReadyCheckerEnabled` / `TimeoutMs` |
| `CombatTelemetry.cs` | Eventos `ReadyCheckerStarted/Ack/Timeout/AdvanceTurn` |
| `Sunshine.csproj` | Entradas ReadyChecker + resto #32 |

---

## Detalle por decisión

### `FightActor.EndTurn`

**Preservado de #32 (vía `Fight.AdvanceToNextTurn`, no duplicado en `EndTurn`):**

- `SlaveFighter.RestoreSummonerContext()`
- `MustAutoUnspawnAfterTurn` → die
- `SummonedMonster` Roublabot + `DiesAtTurnEnd`

**Integrado de #39:**

- `TryBeginTurnEnd` + telemetría `EndTurnRequested` / `EndTurnCompleted`
- `CombatReadyCheckerSettings.Enabled` → `Checker.Start(waiters)`
- Else → `TryAdvanceTurn("ReadyCheckerDisabled")` (reemplaza `FighterPlaying.StartTurn()` directo)

### `EffectDispatcher.Dispatch`

**Preservado #32:** `FightCombatLogger.LogEffectDispatch` (diagnostics patch)

**Integrado #39:** `CombatTelemetry.LogSpellEvent` para `EffectResolved` / `EffectFailed` con `Stopwatch`

### `Fight.AdvanceToNextTurn`

**Gap detectado:** rama #39 solo mataba Roublabot; #32 también `DiesAtTurnEnd`.

**Fix aplicado:**

```csharp
else if (endingFighter is SummonedMonster summonedMonster && summonedMonster.IsAlive)
{
    if (summonedMonster.Monster?.Record?.Id == SlaveFighter.RoublabotMonsterId)
        summonedMonster.Die(endingFighter);
    else if (summonedMonster.DiesAtTurnEnd)
        summonedMonster.Die(endingFighter);
    ...
}
```

---

## Verificaciones de compilación

| Check | Resultado |
| --- | --- |
| `dotnet build Sunshine.csproj` | **OK** (4 CA1416 warnings) |
| `ReadyChecker` compila | OK |
| `GameFightTurnReadyMessage` handler | OK |
| `PunishmentBuff` / `SacrificeBuff` / `Effect_185` | Presentes (sin revert) |
| `MonsterActionDelayMs` en `MonsterAttackAI` | Presente |

---

## Riesgos / QA pendiente

```txt
- No deploy VPS en esta pasada.
- Validar en VPS: ReadyChecker events + monster turn pass + punishment/summon behavior post-merge.
- Comparar smoke 47 combates baseline vs build mergeado antes de merge a devp.
- PR #39 debe retargetarse a esta rama o mergearse esta rama en lugar del head anterior.
```

---

## Qué NO se hizo

```txt
- No revertir IA/spells/castigos de #32
- No merge a main
- No force push
- No ours/theirs global
```
