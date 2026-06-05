# Fase 3 — Análisis de causa raíz

> Esqueleto — completar en commit #7 tras validación VPS.

## Resumen por capa

| Capa | Síntoma | Causa raíz (Sunshine) | Referencia Rollback |
|------|---------|----------------------|---------------------|
| DOT / robo HP | Veneno/Cil sin tick | `HpSteal` ignora `Duration`; sin dispatch `TURN_BEGIN` | `StealHpEffectHandler.cs` |
| Muerte instantánea | Glifos/trampas no matan | Sin handler `Effect_Kill` | `KillEffectHandler.cs` |
| Castigos | Sacrógrito no acumula bien | Tope por ronda (`DiceFace`) ausente | `PunishmentEffectHandler.cs` |
| Invocaciones | Suicidas no mueren | `CanPlay` / `UseSummonSlot` / fin de turno | `SummonedStaticMonster.cs` |
| Secuencias | Turnos colgados | Sin `ActiveSequenceCount`/`ReadyChecker` | `FightTelemetry.cs` *(solo logger en build)* |
| Bosses Frigost | Fase 2 boss | Hooks parciales | `Brain.cs` *(Ola 2 si aplica)* |

## Bloques auditoria (plantilla)

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Damages/StealHpEffectHandler.cs
ruta/actual/Game/Effects/Spells/Damages/HpSteal.cs
LINEAS: TBD
Módulo: game
Evidencia: confirmado en diff | inferido
```

*(Completar por capa en commit #7.)*
