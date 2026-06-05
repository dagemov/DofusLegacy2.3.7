# Mapeo EffectId → Handler

Inventario Sunshine: atributos `[EffectHandler(EffectsEnum.…)]` en `Game/Effects/Spells/**`, registro en `EffectsLoader.Initialize()`.

**Conteo runtime VPS:** 161 efectos cargados (log `develop-build` 2026-06-05).

## Fuentes

| Recurso | Ruta |
|---------|------|
| Enum IDs | `Sunshine.Protocol/Enums/EffectsEnum.cs` |
| Handlers | `Sunshine.WorldServer/Game/Effects/Spells/**/*.cs` |
| Loader | `Sunshine.BaseServer/Loaders/World/Effects/EffectsLoader.cs` |

## Tabla resumida por categoría Fase 2

| Categoría | EffectsEnum (muestra) | Handler | Archivo |
|-----------|----------------------|---------|---------|
| Daño directo | `Effect_DamageAir` … `Effect_DamageNeutral` | `DirectDamage` | `Damages/DirectDamage.cs` |
| Daño % | `Effect_DamagePercentAir` … | `DamagePercent` | `Damages/DamagePercent.cs` |
| Robo HP | `Effect_StealHPNeutral`, `Effect_StealHPAir`, … | `HpSteal` | `Damages/HpSteal.cs` |
| Curas | `Effect_HealHP_81`, `Effect_HealHP_108`, `Effect_HealHP_143` | `Heal` | `Heals/Heal.cs` |
| Curas % | `Effect_RestoreHPPercent` | `RestoreHpPercent` | `Heals/RestoreHpPercent.cs` |
| Estado | `Effect_AddState` | `AddState` | `States/AddState.cs` |
| Invisibilidad | `Effect_Invisibility` | `Invisibility` | `States/Invisibility.cs` |
| Quitar estado | `Effect_RemoveState`, `Effect_952` | `RemoveState` | `States/RemoveState.cs` |
| Stats buff | `Effect_AddStrength`, `Effect_AddAgility`, … | `StatsBoost` | `States/StatsBoost.cs` |
| Stats debuff | `Effect_SubStrength`, `Effect_SubVitality`, … | `SubStatsBoost` | `Debuffs/SubStatsBoost.cs` |
| AP buff | `Effect_AddAP_111`, `Effect_RegainAP` | `APBuff` | `Buffs/APBuff.cs` |
| MP buff | `Effect_AddMP`, `Effect_AddMP_128` | `MPBuff` | `Buffs/MPBuff.cs` |
| Castigo buff | `Effect_Punishment` | `PunishmentBoost` | `Buffs/PunishmentBoost.cs` |
| Castigo daño | `Effect_Punishment_Damage`, `Effect_275`–`279` | `PunishmentDamage` | `Damages/PunishmentDamage.cs` |
| Sacrificio | `Effect_Sacrifice` | `Sacrifice` | `Buffs/Sacrifice.cs` |
| Invocación | `Effect_Summon`, `Effect_SummonBomb`, `Effect_SummonSlave` | `Summon` | `Summon/Summon.cs` |
| Doble | `Effect_Double` | `Double` | `Summon/Double.cs` |
| Bomba activar | `Effect_ActivateBomb` | `ActivateBomb` | `Summon/ActivateBomb.cs` |
| Glifo | `Effect_Glyph`, `Effect_Glyph_402` | `GlyphSpawn` | `Marks/GlyphSpawn.cs` |
| Trampa | `Effect_Trap` | `TrapSpawn` | `Marks/TrapSpawn.cs` |
| Empuje | `Effect_Push`, `Effect_Push_1103` | `Push` | `Moves/Push.cs` |
| Teleport | `Effect_Teleport` | `Teleport` | `Moves/Teleport.cs` |
| Escudo | `Effect_AddShieldPercent`, `Effect_1038` | `ShieldPercent`, `Shield` | `Shield/*.cs` |

## Gaps vs Rollback (confirmado Fase 1)

| EffectsEnum / efecto | Sunshine | Rollback homólogo | Gap |
|----------------------|----------|-------------------|-----|
| `Effect_Kill` | Solo `case` en `StatsBoost.cs` L114 | `KillEffectHandler.cs` | **Sin handler registrado** |
| DOT robo HP (`Duration > 0`) | `HpSteal` instantáneo | `StealHpEffectHandler` + `TriggerBuff` | **Comportamiento** |
| Castigo acumulativo | `PunishmentBuff.OnDamaged` | `PunishmentEffectHandler` + trigger | **Modelo distinto** |
| Secuencias combate | Ausente | `ActiveSequenceCount`, `ReadyChecker` | **Infra ausente** |

## Handlers Rollback sin homólogo Sunshine (muestra)

| Rollback handler | Notas |
|------------------|-------|
| `KillEffectHandler` | Muerte glifos |
| `RevealInvisibleEffectHandler` | — |
| `NothingEffectHandler` | — |
| `DodgeEffectHandler` | Parcial vía stats |
| `DamageInterceptEffectHandler` | — |
| `HealOrMultiplyEffectHandler` | — |

Ver tabla completa en [rollback-vs-current-diff.md](../effects-audit-phase1/rollback-vs-current-diff.md).

## Registro duplicado

`EffectsLoader` omite silenciosamente si `SpellEffects` ya contiene la clave (`ContainsKey` → `continue`). Al añadir handlers nuevos, verificar que no exista factory previa para el mismo `EffectsEnum`.

```text
auditoria:
ruta/actual/Sunshine.BaseServer/Loaders/World/Effects/EffectsLoader.cs
LINEAS: 35-43
Módulo: game
```
