# Fase 3 — Análisis de causa raíz

Evidencia contrastada con Rollback (`2.0.0_v1_old/Rollback/Rollback.World`).

## Resumen por capa

| Capa | Síntoma | Causa raíz (Sunshine) | Fix | Evidencia |
|------|---------|----------------------|-----|-----------|
| DOT / robo HP | Veneno/Cil sin tick | `HpSteal.Apply()` ignoraba `Duration`; sin dispatch `TURN_BEGIN` en `StartTurn` | `TriggerBuff` + `TriggerFightBuffs` | confirmado en diff |
| Muerte instantánea | Glifos/trampa no matan | Sin handler `[EffectHandler(Effect_Kill)]` | `Others/Kill.cs` + `FightActor.Kill` | confirmado en diff |
| Castigos | Bonus sin tope/ronda | `PunishmentBuff` sin cap `DiceFace`/ronda; stat por `SpellIdEnum` | Per-round cap + stat desde `Effect.DiceNum` | confirmado en diff |
| Invocaciones | Suicidas / estáticas | Falta `SummonedStaticMonster`; sin `Die` genérico fin de turno | Flags plantilla `CanPlay` / `UseSummonSlot` | confirmado en diff |
| Secuencias | Turnos colgados | Sin `ActiveSequenceCount`/`ReadyChecker` | Logger en build; port documentado | inferido |
| Bosses Frigost | Fase boss | Hooks parciales en `FrigostBossMechanics` | Ola 2 | inferido |

## 1. DOT / robo HP

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Damages/StealHpEffectHandler.cs
ruta/actual/Game/Effects/Spells/Damages/HpSteal.cs
LINEAS: Rollback 17-41 / Sunshine 19-75 (post-fix)
Módulo: game
Evidencia: confirmado en diff
```

Rollback: si `Duration != 0` → `AddTriggerBuff(OnTurnBegin)`. Sunshine pre-fix aplicaba daño instantáneo siempre.

## 2. Effect_Kill

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Others/KillEffectHandler.cs
ruta/actual/Game/Effects/Spells/Others/Kill.cs
LINEAS: Rollback 1-15 / Sunshine 1-18
Módulo: game
Evidencia: confirmado en diff
```

Pre-fix: `Effect_Kill` solo aparecía en `StatsBoost.cs` sin handler registrado.

## 3. Castigos (Effect_Punishment)

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Buffs/PunishmentEffectHandler.cs
ruta/actual/Game/Fights/Buffs/Spells/PunishmentBuff.cs
LINEAS: Rollback 19-47 / Sunshine OnDamaged (post-fix)
Módulo: game
Evidencia: confirmado en diff
```

Rollback acumula bonus por daño con tope **por ronda** (`DiceFace`). Sunshine usaba `DiceFace` como tope total y resolvía stat por ID de hechizo Sacrógrito.

## 4. Invocaciones

```text
auditoria:
ruta/rollback/Game/Fights/Fighters/SummonedStaticMonster.cs
ruta/actual/Game/Actors/Fighters/SummonedStaticMonster.cs
LINEAS: Rollback 7-14 / Sunshine 1-18
Módulo: game
Evidencia: confirmado en diff
```

- `CanPlay=false` → sin turno IA (`SummonedStaticMonster`)
- `UseSummonSlot=false` + `CanPlay=true` → `Die()` al fin del turno del summon (suicidas genéricas)

## 5. Secuencias / telemetría

```text
auditoria:
ruta/rollback/Game/Fights/FightTelemetry.cs
ruta/actual/Game/Fights/Diagnostics/FightCombatLogger.cs
Módulo: game
Evidencia: confirmado en diff (subset diagnóstico; sin port secuencias)
```

Port completo de `ActiveSequenceCount`/`ReadyChecker` queda documentado en [architecture-changes.md](./architecture-changes.md) para PR futuro.
