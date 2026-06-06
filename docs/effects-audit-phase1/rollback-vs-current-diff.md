# Rollback vs Sunshine — diff técnico

Comparativa de código fuente entre:

- **Referencia:** `C:\Dofus\2.0.0_v1_old\2.0.0\Rollback\Rollback.World\`
- **Actual:** `c:\Dofus\2.0.0\Sunshine net11.0\Sunshine net11.0\Sunshine.WorldServer\`

Módulo de todas las rutas: **game**.

---

## 1. `SpellEffectHandler`

| Aspecto | Rollback | Sunshine |
|---------|----------|----------|
| Archivo | `Game/Effects/Handlers/Spells/SpellEffectHandler.cs` | `Game/Effects/Spells/SpellEffectHandler.cs` |
| Líneas | 1–59 | 1–97 |
| Patrón | Abstracto; `Apply()` sellado → `InternalApply(fighter)` | `Prepare(List<object>)` + `Apply()` abstracto |
| Contexto | `SpellCast`, `Zone`, `List<FightActor> Target` | 15 parámetros en lista (`Id`, dados, `Duration`, `Delay`, actores, `trapCell`, …) |
| Reflejo hechizo | En `Apply()` base (líneas 47–51) | No en clase base |
| Invisibilidad al cast | `EffectManager.EffectDispellInvisibility` en `Apply()` | No equivalente en base |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/SpellEffectHandler.cs
ruta/actual/Game/Effects/Spells/SpellEffectHandler.cs
LINEAS: 1-59 vs 15-98
Módulo: game
```

---

## 2. `FightEffects`

**No aplicable** — ningún emulador define `FightEffects`. Ver efectos diferidos en [effect-engine-overview.md](./effect-engine-overview.md).

---

## 3. Buffs

| Rollback | Sunshine | Notas |
|----------|----------|-------|
| `Game/Fights/Buffs/Buff.cs` | `Game/Fights/Buffs/Buff.cs` | APIs distintas (`AbstractFightDispellableEffect`) |
| `Buffs/Types/TriggerBuff.cs` (1–58) | `Buffs/Customs/TriggerBuff.cs` | Sunshine usa delegate `TriggerBuffCallback` |
| `Buffs/Types/StateBuff.cs` | `Buffs/Spells/StateBuff.cs` | |
| `Buffs/Types/StatBuff.cs` | `Buffs/Spells/StatsBuff.cs` | Sunshine también tiene handler `States/StatsBoost.cs` |
| `PunishmentEffectHandler` (buff trigger) | `PunishmentBoost.cs` + `PunishmentBuff.cs` | Modelos distintos |

```text
auditoria:
ruta/rollback/Game/Fights/Buffs/Types/TriggerBuff.cs
ruta/actual/Game/Fights/Buffs/Customs/TriggerBuff.cs
LINEAS: 1-58 vs 1-80 (aprox.)
Módulo: game
```

---

## 4. States (invulnerabilidad / estados de hechizo)

| Rollback | Sunshine |
|----------|----------|
| `Buffs/StateBuffEffectHandler.cs` | `Effects/Spells/States/AddState.cs` |
| `Buffs/DispelStateEffectHandler.cs` | `Effects/Spells/States/RemoveState.cs` |
| — | `States/Invisibility.cs`, `ChangeSkin.cs` |

Rollback centraliza estado en handlers bajo `Handlers/Spells/Buffs/`. Sunshine mezcla handler + `StateBuff` en `Fights/Buffs/Spells/`.

---

## 5. Summons (invocaciones)

| Rollback | Sunshine |
|----------|----------|
| `Handlers/Spells/Summons/SummonEffectHandler.cs` | `Effects/Spells/Summon/Summon.cs` |
| `Summons/DoubleEffectHandler.cs` | `Summon/Double.cs` |
| — | `Summon/ActivateBomb.cs` |
| `Game/Fights/Fighters/SummonedMonster.cs` | `Game/Actors/Fighters/SummonedMonster.cs` |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Summons/SummonEffectHandler.cs
ruta/actual/Game/Effects/Spells/Summon/Summon.cs
LINEAS: 1-34 vs 1-210 (aprox.)
Módulo: game
```

Sunshine: `SummonedMonster` hereda `AIFighter`; explosión bomba y mensajes de error al jugador.

---

## 6. Triggers (mapa)

| Rollback | Sunshine |
|----------|----------|
| `Handlers/Spells/Triggers/GlyphEffectHandler.cs` | `Effects/Spells/Marks/GlyphSpawn.cs` |
| `Handlers/Spells/Triggers/TrapEffectHandler.cs` | `Effects/Spells/Marks/TrapSpawn.cs` |
| `Fight.cs` NotifyTriggers 556–565 | `Fight.AddTrigger` 811–823, `ShouldTriggerOnMove` 788–796 |
| `Triggers/Types/Glyph.cs` | `Fights/Triggers/Glyph.cs` |

Rollback colorea glifos por ID de hechizo interno (veneno 71/2068 en trap handler). Sunshine usa `SpellManager` para resolver hechizo embebido en `GlyphSpawn`.

---

## 7. Delayed effects (duración / delay)

| Comportamiento | Rollback | Sunshine |
|----------------|----------|----------|
| Roibo HP multi-turno | `StealHpEffectHandler` 17–41 | `HpSteal.cs` 19–27 (ignora `Duration`) |
| Castigo acumulativo | `PunishmentEffectHandler` + `TriggerBuff` | `PunishmentBuff.OnDamaged` en `FightActor` 923–928 |
| Fin de turno / PA | `LoseHPByUsingAPEffectHandler` | `LoseHpByUsingAP.cs` |
| Glifos con duración | `DecrementCastedGlyphs` en `Fight.cs` 540–546 | Duración en `Glyph` constructor |

---

## 8. Effect dispatchers

| Rollback | Sunshine |
|----------|----------|
| `Game/Effects/EffectManager.cs` (523 líneas) — registra y ejecuta | `EffectManager.cs` (227) — datos + `GetAffectedActors` |
| `GenerateSpellEffectHandler(SpellCast)` | `EffectsLoader.Initialize()` en `Sunshine.BaseServer/Loaders/World/Effects/EffectsLoader.cs` |
| — | `EffectDispatcher.Dispatch()` |

```text
auditoria:
ruta/rollback/Game/Effects/EffectManager.cs
ruta/actual/.../Sunshine.BaseServer/Loaders/World/Effects/EffectsLoader.cs
LINEAS: 224-262 vs 13-47
Módulo: game
```

---

## Mapeo 1:1 de handlers (principal)

| Rollback (`Handlers/Spells/`) | Sunshine (`Effects/Spells/`) | Estado |
|-----------------------------|------------------------------|--------|
| `SpellEffectHandler.cs` | `SpellEffectHandler.cs` | Paridad estructural distinta |
| `Damages/DamageEffectHandler.cs` | `Damages/Damage.cs`, `DirectDamage.cs` | Parcial |
| `Damages/StealHpEffectHandler.cs` | `Damages/HpSteal.cs` | **Divergente** (DOT) |
| `Damages/PunishmentDamageEffectHandler.cs` | `Damages/PunishmentDamage.cs` | **Divergente** |
| `Buffs/PunishmentEffectHandler.cs` | `Buffs/PunishmentBoost.cs` | **Divergente** |
| `Movements/PushEffectHandler.cs` | `Moves/Push.cs` | **Divergente** (inline pushback) |
| `Movements/TeleportEffectHandler.cs` | `Moves/Teleport.cs` | Similar |
| `Movements/RepealsToEffectHandler.cs` | `Moves/RepelsTo.cs`, `AttractTo.cs`, `Pull.cs` | Parcial |
| `Triggers/GlyphEffectHandler.cs` | `Marks/GlyphSpawn.cs` | Similar |
| `Triggers/TrapEffectHandler.cs` | `Marks/TrapSpawn.cs` | Similar |
| `Summons/SummonEffectHandler.cs` | `Summon/Summon.cs` | Extendido |
| `Summons/DoubleEffectHandler.cs` | `Summon/Double.cs` | Similar |
| `Buffs/StateBuffEffectHandler.cs` | `States/AddState.cs` | Similar |
| `Buffs/DispelStateEffectHandler.cs` | `States/RemoveState.cs` | Similar |
| `Buffs/InvisibilityEffectHandler.cs` | `States/Invisibility.cs` | Similar |
| `Others/KillEffectHandler.cs` | — | **Falta en Sunshine** |
| `Others/RevealInvisibleEffectHandler.cs` | — | **Falta** |
| `Others/NothingEffectHandler.cs` | — | **Falta** |
| `Others/CarryEffectHandler.cs` | `Moves/Carrier.cs` (parcial) | Parcial |
| `Buffs/DodgeEffectHandler.cs` | — | **Falta** |
| `Buffs/DamageInterceptEffectHandler.cs` | — | **Falta** |
| `Buffs/ReflectSpellEffectHandler.cs` | `Armor/DamageReflect.cs` | Parcial |
| `Buffs/StatStealBuffHandler.cs` | — | **Falta** |
| `Damages/HealOrMultiplyEffectHandler.cs` | — | **Falta** |
| `Damages/GiveHPPercentEffectHandler.cs` | `Heals/RestoreHpPercent.cs` | Revisar paridad |
| `Buffs/RemoveSpellEffectsEffectHandler.cs` | `Debuffs/DispelMagicEffects.cs` | Parcial |
| `Movements/ExchangePositionsEffectHandler.cs` | `Moves/SwitchPosition.cs` | Similar |

---

## Handlers solo en Sunshine (muestra)

| Archivo | Efectos / notas |
|---------|-----------------|
| `Others/RogueSpecialEffects.cs` | Rogue 2.x |
| `Others/Roulette.cs` | Ecaflip |
| `Others/SpiritualLeash.cs` | |
| `Shield/Shield.cs`, `ShieldPercent.cs` | |
| `Debuffs/SubStatsBoost.cs` | Múltiples `[EffectHandler]` |
| `Debuffs/Erosion.cs` | |
| `Moves/ForcedMovementHelper.cs` | |
| `Summon/ActivateBomb.cs` | Bombas |

---

## Subsistemas sin paridad en Rollback

| Sunshine | Rollback |
|----------|----------|
| `Game/Spells/Casts/**` (`SpellCastHandler` por hechizo) | No hay carpeta equivalente |
| `Game/Fights/Bombs/BombManager.cs` | — |
| `Game/Fights/Mechanics/FrigostBossMechanics.cs` | — |
| `Game/Effects/EffectDispatcher.cs` | Dispatch vía `SpellCast` únicamente |

---

## Conteo resumido

| Métrica | Rollback | Sunshine |
|---------|----------|----------|
| Archivos en `Handlers/Spells` / `Effects/Spells` | 38 | 55 |
| Atributos `[Identifier]` / `[EffectHandler]` | ~38 / ~76 | |
| `Fight.cs` | 789 | 1055 |
| Telemetría combate | Sí (`FightTelemetry`, `ReadyChecker`) | No detectada |

---

## Cliente (multi) — referencia Fase 2

No es foco de diff servidor, pero mensajes de combate viven en:

- `Client2.3.7/as2invoker/com/ankamagames/dofus/logic/game/fight/`
- `network/messages/game/actions/fight/*`

Desincronización posible si Sunshine no respeta orden de secuencias que el cliente espera (Rollback mitiga con `ActiveSequenceCount`).
