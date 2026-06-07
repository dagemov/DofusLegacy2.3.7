# Categorías de efectos — Sunshine

Taxonomía para corrección por capa. Rutas bajo `Sunshine.WorldServer/` (**game**).

## 1. Directos

Efectos que aplican resultado inmediato en `Apply()` sin buff de duración (salvo que el handler cree buff aparte).

| Subcategoría | Handlers | Archivos | Notas Fase 1 |
|--------------|----------|----------|--------------|
| **Daño** | `DirectDamage`, `DamagePercent`, `PunishmentDamage` | `Damages/DirectDamage.cs`, `DamagePercent.cs`, `PunishmentDamage.cs` | Castigo daño % diverge de Rollback |
| **Curas** | `Heal`, `RestoreHpPercent`, `SubHealPercent` | `Heals/Heal.cs`, `RestoreHpPercent.cs`, `SubHealPercent.cs` | — |
| **Robo** | `HpSteal` | `Damages/HpSteal.cs` | **Gap DOT:** ignora `Duration` (veneno/Cil) |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Damages/StealHpEffectHandler.cs
ruta/actual/Game/Effects/Spells/Damages/HpSteal.cs
LINEAS: 17-41 vs 19-27
Módulo: game
```

## 2. Estados

| Subcategoría | Handler | Archivo | `SpellStatesEnum` / notas |
|--------------|---------|---------|---------------------------|
| **Invisibilidad** | `Invisibility` | `States/Invisibility.cs` | — |
| **Pesado** | `AddState` → `StateBuff` | `States/AddState.cs`, `Fights/Buffs/Spells/StateBuff.cs` | Estado por ID hechizo |
| **Indesplazable** | `AddState` | idem | Misma vía que otros estados |
| **Dispel estado** | `RemoveState` | `States/RemoveState.cs` | Invulnerabilidad / skip turn |

## 3. Buffs

| Subcategoría | Handlers | Archivos |
|--------------|----------|----------|
| **Stats** | `StatsBoost`, `SubStatsBoost` | `States/StatsBoost.cs`, `Debuffs/SubStatsBoost.cs` |
| **AP** | `APBuff`, `APDebuff`, `APDebuffFix` | `Buffs/APBuff.cs`, `Debuffs/APDebuff.cs`, `APDebuffFix.cs` |
| **MP** | `MPBuff`, `MPDebuff`, `MPDebuffFix` | `Buffs/MPBuff.cs`, `Debuffs/MPDebuff.cs`, `MPDebuffFix.cs` |
| **Castigo (buff)** | `PunishmentBoost` → `PunishmentBuff` | `Buffs/PunishmentBoost.cs`, `Fights/Buffs/Spells/PunishmentBuff.cs` |
| **Sacrificio** | `Sacrifice` | `Buffs/Sacrifice.cs` |
| **Escudo** | `Shield`, `ShieldPercent`, `DamageReduction` | `Shield/*.cs`, `Armor/DamageReduction.cs` |

## 4. Triggers

Buffs que reaccionan a eventos de combate (`TriggerBuff`, `PunishmentBuff.OnDamaged`).

| Subcategoría | Mecanismo | Archivos |
|--------------|-----------|----------|
| **Al recibir daño** | `AfterDamaged` / `OnDamaged` | `PunishmentBuff.cs`, `FightActor.InflictDamage` |
| **Al comenzar turno** | `TriggerBuff` `OnTurnBegin` | `Fights/Buffs/Customs/TriggerBuff.cs` — **gap** en `HpSteal` |
| **Al terminar turno** | `LoseHpByUsingAP`, glifos fin turno | `Damages/LoseHpByUsingAP.cs`, `Fights/Triggers/Glyph.cs` |

## 5. Invocaciones

| Subcategoría | Handler | Archivo | Casos |
|--------------|---------|---------|-------|
| **Summon** | `Summon` | `Summon/Summon.cs` | Monstruos, bombas, slaves |
| **Doble** | `Double` | `Summon/Double.cs` | Clase Sadida / similar |
| **Árbol** | `Summon` (plantilla monstruo) | `Summon.cs` | Invocación por ID template |
| **Sacrificada** | `Sacrifice` buff + summon IA | `Buffs/Sacrifice.cs`, `SummonedMonster.cs` | — |
| **Hinchable / bomba** | `Summon` + `ActivateBomb` | `Summon.cs`, `ActivateBomb.cs` | `BombManager.cs` |

**Golosón / suicidas:** capa `SummonedMonster.Die()` + IA, no hechizo individual — ver [combat-fix-philosophy.md](../combat-fix-philosophy.md).

## 6. Mecánicas especiales

| Subcategoría | Componentes | Archivos |
|--------------|-------------|----------|
| **Bosses** | IA + scripts | `Fights/Mechanics/FrigostBossMechanics.cs`, `Actors/AI/MonsterAttackAI.cs` |
| **Invulnerabilidades** | Estados | `AddState.cs`, `StateBuff.cs`, `RemoveState.cs` |
| **Casillas especiales** | Glifos / trampas | `Marks/GlyphSpawn.cs`, `TrapSpawn.cs`, `Fights/Triggers/Glyph.cs` |
| **Muerte instantánea** | **Sin handler** | Gap `Effect_Kill` — Fase 1 |

## Matriz categoría → síntoma Fase 1

| Categoría | Síntomas Fase 1 relacionados |
|-----------|------------------------------|
| Robo / Triggers turno | Venenos, Cil |
| Castigo buff + daño | Sacrógrito |
| Invocaciones | Golosón, suicidas, bombas |
| Casillas + Kill | Casillas de muerte |
| Movimiento | Empujes especiales |
| Marcas mapa | Triggers de mapa |
| (transversal) | Secuencias combate |
