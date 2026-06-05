# Fase 3 — Cambios de arquitectura

> Esqueleto — completar en commit #7.

## Pipeline post-fix (diagrama)

```mermaid
flowchart LR
  cast[CastSpell] --> dispatch[EffectDispatcher]
  dispatch --> handler[SpellEffectHandler]
  handler --> instant[Daño instantáneo]
  handler --> trigger[TriggerBuff]
  trigger --> turn[StartTurn TURN_BEGIN]
  turn --> dot[Tick DOT / robo HP]
  damage[InflictDamage] --> punish[PunishmentBuff OnDamaged]
  damage --> after[TriggerBuff AFTER_ATTACKED]
  marks[TriggerMarks] --> kill[Effect_Kill handler]
```

## Archivos modificados (por commit)

| Commit | Archivos |
|--------|----------|
| #1 | `Damages/HpSteal.cs`, `FightActor.cs` (dispatch triggers) |
| #2 | `Others/Kill.cs` |
| #3 | `PunishmentBuff.cs`, `Buffs/PunishmentBoost.cs` |
| #4 | `Summon/Summon.cs`, `SummonedMonster.cs`, `SummonedStaticMonster.cs`, `FightActor.cs` |
| #6 | `Fights/Diagnostics/FightCombatLogger.cs`, hooks en dispatcher/daño/red |

## Pendiente para `develop` (no este PR)

- Port `ActiveSequenceCount` / `ReadyChecker` desde Rollback
- Integración logger ↔ secuencias de combate
- Bosses Frigost — Ola 2 si no entra en ventana
