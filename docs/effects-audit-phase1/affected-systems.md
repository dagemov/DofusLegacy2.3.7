# Sistemas afectados — Matriz Fase 1

Inventario de síntomas y rutas de edición futura (**Fase 2+**). Estado del análisis: **confirmado en diff de código** salvo donde se indica *inferido* (comportamiento en juego sin prueba en esta fase).

Convención de rutas:

- **Rollback:** `C:\Dofus\2.0.0_v1_old\2.0.0\Rollback\Rollback.World\...`
- **Actual:** `c:\Dofus\2.0.0\Sunshine net11.0\Sunshine net11.0\Sunshine.WorldServer\...`

---

## Matriz de sistemas afectados

| Sistema | Síntoma | Ruta del Archivo | Acción sugerida (retrocompatible) |
|---------|---------|------------------|-----------------------------------|
| Venenos / DOT raza | Veneno / robo de vida con `Duration > 0` no hace tick por turno; daño aplicado una sola vez al cast | **(game)** Rollback: `Game/Effects/Handlers/Spells/Damages/StealHpEffectHandler.cs` L17–41 · Actual: `Game/Effects/Spells/Damages/HpSteal.cs` L19–27 | Portar rama `if (Effect.Duration != 0) AddTriggerBuff(OnTurnBegin, …)` de Rollback a `HpSteal` (o buff DOT dedicado). Mantener robo 50% al caster. |
| Venenos / DOT raza | Glifos/trampas de veneno (IDs 71, 2068) color/trigger | **(game)** Rollback: `Handlers/Spells/Triggers/TrapEffectHandler.cs` L29–40 · Actual: `Fights/Triggers/Trap.cs`, `Marks/TrapSpawn.cs` | Verificar que el hechizo interno del glifo se dispare con misma cadencia (turno inicio/fin) que Rollback `NotifyTriggers`. |
| Venenos / DOT raza | *Inferido* Cliente muestra icono DOT pero barra de vida no baja por turno | **(multi)** `Client2.3.7/as2invoker/.../logic/game/fight/BuffManager.as` (secuencias buff) | Tras arreglar servidor, validar `FightTriggeredEffect` / duración en paquetes; no parchear cliente si el bug es solo servidor. |
| Invocaciones suicidas | Invocaciones tipo Bloqueadora/Loca no mueren o no ejecutan IA suicida | **(game)** Rollback: `Summons/SummonEffectHandler.cs` L17–32, `Fighters/SummonedMonster.cs` · Actual: `Effects/Spells/Summon/Summon.cs` L22–110, `Actors/Fighters/SummonedMonster.cs` L92+ (`Die`) | Alinear `Die()` / fin de turno con plantilla monstruo y flags `UseSummonSlot`; revisar si IA `Crazy`/`Rusher` aplica en invocaciones Sadida. |
| Invocaciones suicidas | Bomba en celda ocupada (explosión inmediata) | **(game)** Actual: `Summon/Summon.cs` L54–73, `Fights/Bombs/BombManager.cs` | Comparar con comportamiento Rollback si existe bomba; asegurar orden: daño → muerte → mensajes antes de buffs post-daño. |
| Castigos Sacrógrito | Bonus por daños recibidos no acumula o se desincroniza en UI | **(game)** Rollback: `Buffs/PunishmentEffectHandler.cs` L19–47 · Actual: `Buffs/Spells/PunishmentBuff.cs` L65–120 (`OnDamaged`), `FightActor.cs` L923–928 | Unificar en modelo Rollback (`AfterDamaged` + tope `DiceFace` por ronda) o documentar desvío; evitar doble vía con `PunishmentDamage.cs`. |
| Castigos Sacrógrito | Hechizo de daño “castigo” usa curva simplificada | **(game)** Rollback: `Damages/PunishmentDamageEffectHandler.cs` · Actual: `Damages/PunishmentDamage.cs` L9–41 | Separar efecto daño (275–279) de efecto buff castigo (`Effect_Punishment`); no mezclar fórmula % vida con buff reactivo. |
| Castigos Sacrógrito | Aplicación buff castigo al cast | **(game)** Actual: `Effects/Spells/Buffs/PunishmentBoost.cs` L11–16 | Mantener creación `PunishmentBuff` pero alinear `ResolveMaxBoost` / `ResolveBoostedStat` con metadatos hechizo Rollback. |
| IA Bosses | Boss no lanza fase 2 / script Frigost | **(game)** Rollback: `Game/Fights/AI/Brain.cs`, `AIFighter.cs` · Actual: `Game/Fights/Mechanics/FrigostBossMechanics.cs`, `Game/Actors/AI/MonsterAttackAI.cs` | Mapear scripts por `MonsterId` a handlers; portar decisiones críticas de `Brain` donde Frigost no cubra. |
| IA Bosses | Telemetría / turno IA bloqueado | **(game)** Rollback: `FightTelemetry.cs`, `Fight.cs` (AIStart/AIEnd) · Actual: `Game/Fights/Fight.cs` (sin `FightTelemetry`) | *Inferido:* introducir guardas de turno similares a `ReadyChecker` o logs mínimos antes de refactor IA grande. |
| Invulnerabilidades | Estado invulnerable no se aplica o no se quita | **(game)** Rollback: `Buffs/StateBuffEffectHandler.cs`, `DispelStateEffectHandler.cs` · Actual: `States/AddState.cs`, `States/RemoveState.cs`, `Buffs/Spells/StateBuff.cs` | Verificar `SpellStatesEnum`, duración, `Dispellable`; mensaje `FightTemporaryBoostStateEffect`. |
| Invulnerabilidades | Conflicto estado + daño (0 PV luego curación fantasma) | **(game)** Actual: `FightActor.cs` L917–921 (`TryKillIfNoHealth` antes de buffs castigo) | Conservar orden Rollback: muerte antes de buffs `AfterDamaged`; ya parcialmente implementado — validar otros buffs trigger. |
| Casillas de muerte | Glifo de muerte instantánea no mata | **(game)** Rollback: `Others/KillEffectHandler.cs` L13–14 · Actual: **sin handler** — solo `States/StatsBoost.cs` L114 (`case Effect_Kill` sin handler) | Crear `EffectHandler(Effect_Kill)` que llame `Kill(caster)` como Rollback; registrar en `EffectsLoader`. |
| Casillas de muerte | Activación glifo al pisar celda | **(game)** Rollback: `Fight.cs` L553–565 · Actual: `Fight.cs` L788–796, `Triggers/Glyph.cs` | Asegurar `TriggerTypeEnum.MOVE` y re-lanzado hechizo interno con `Effect_Kill` registrado. |
| Empujes especiales | Daño por colisión / empuje incorrecto | **(game)** Rollback: `Movements/PushEffectHandler.cs` L30–40 → `FightActor.Push` · Actual: `Moves/Push.cs` L19–80 | Extraer daño empuje a método compartido; respetar `PushDamageBonus` / reducción como Rollback `FightFormulas`. |
| Empujes especiales | Glifo disparado a mitad de empuje | **(game)** Actual: `Moves/Push.cs` L69–72 (`ShouldTriggerOnMove`) | Comparar con Rollback pathfinding; orden: movimiento parcial → triggers → resto empuje (retrocompatible con cliente). |
| Triggers de mapa | Trampa no se activa al pasar | **(game)** Rollback: `Triggers/TrapEffectHandler.cs`, `Triggers/Types/Trap.cs` · Actual: `Marks/TrapSpawn.cs`, `Fights/Triggers/Trap.cs` | Igualar visibilidad, `TriggerType`, y `NotifyTriggers` vs `MarkTrigger.Trigger`. |
| Triggers de mapa | Glifo fin/inicio de turno (silence, etc.) | **(game)** Rollback: `GlyphEffectHandler.cs` L24–25 · Actual: `Triggers/Glyph.cs` L16–47 (`SPELLS_GLYPH_END_TURN`) | Validar lista IDs 13, 2035 y `TriggerTypeEnum` TURN_BEGIN vs TURN_END. |
| Secuencias combate (*transversal*) | Turno colgado / acciones durante animación | **(game)** Rollback: `Fight.cs` L85–99, `ReadyChecker.cs` · Actual: `Fight.cs` (sin `ActiveSequenceCount`) | *Inferido Fase 2:* portar concepto `ActiveSequenceCount` o equivalente antes de masivas correcciones de hechizos. |

---

## Bloques `auditoria` por síntoma prioritario

### Venenos (prioridad alta)

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Damages/StealHpEffectHandler.cs
ruta/actual/Game/Effects/Spells/Damages/HpSteal.cs
LINEAS: 15-42 vs 19-27
Módulo: game
```

### Castigos Sacrógrito

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Buffs/PunishmentEffectHandler.cs
ruta/actual/Game/Fights/Buffs/Spells/PunishmentBuff.cs
LINEAS: 19-47 vs 83-120 (OnDamaged, aprox.)

auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Damages/PunishmentDamageEffectHandler.cs
ruta/actual/Game/Effects/Spells/Damages/PunishmentDamage.cs
LINEAS: archivo completo vs 9-41
Módulo: game
```

### Muerte instantánea (glifos)

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Others/KillEffectHandler.cs
ruta/actual/Game/Effects/Spells/States/StatsBoost.cs
LINEAS: 13-14 vs 114-115 (case sin handler)
Módulo: game
```

### Empujes

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Movements/PushEffectHandler.cs
ruta/actual/Game/Effects/Spells/Moves/Push.cs
LINEAS: 30-40 vs 19-80
Módulo: game
```

### Triggers de mapa

```text
auditoria:
ruta/rollback/Game/Fights/Fight.cs
ruta/actual/Game/Fights/Fight.cs
LINEAS: 518-565 vs 788-823
Módulo: game
```

---

## Inventario de síntomas (resumen)

| # | Sistema | Severidad estimada | Evidencia |
|---|---------|-------------------|-----------|
| 1 | Venenos / DOT | Alta | Diff `StealHp` vs `HpSteal` confirmado |
| 2 | Invocaciones suicidas | Media–Alta | `Summon.cs` + `SummonedMonster.Die` — revisar IA |
| 3 | Castigos Sacrógrito | Alta | Dos modelos (buff + damage) en Sunshine |
| 4 | IA Bosses | Media | Arquitecturas distintas |
| 5 | Invulnerabilidades | Media | Handlers existen; validar estados |
| 6 | Casillas de muerte | Alta | `KillEffectHandler` ausente en Sunshine |
| 7 | Empujes especiales | Media | Lógica inline en `Push.cs` |
| 8 | Triggers mapa | Media | APIs `NotifyTriggers` vs `ShouldTriggerOnMove` |
| 9 | Secuencias (*extra*) | Alta | `ActiveSequenceCount` solo Rollback |

---

## Archivos **game** candidatos a edición (índice)

| Área | Rollback | Sunshine (actual) |
|------|----------|-------------------|
| Dispatcher | `Game/Effects/EffectManager.cs` | `Sunshine.BaseServer/.../EffectsLoader.cs`, `EffectDispatcher.cs` |
| Daño / DOT | `Handlers/Spells/Damages/*` | `Effects/Spells/Damages/*` |
| Buffs combate | `Game/Fights/Buffs/Types/*` | `Game/Fights/Buffs/Spells/*`, `Customs/TriggerBuff.cs` |
| Combate core | `Game/Fights/Fight.cs`, `SpellCast.cs` | `Game/Fights/Fight.cs`, `Spells/Casts/SpellCastManager.cs` |
| Actores | `Game/Fights/FightActor.cs` | `Game/Actors/Fighters/FightActor.cs` |
| IA | `Game/Fights/AI/*` | `Game/Actors/AI/*`, `Fights/Mechanics/*` |

## Archivos **multi** (solo verificación)

| Área | Ruta |
|------|------|
| Secuencias fight cliente | `Client2.3.7/as2invoker/com/ankamagames/dofus/logic/game/fight/` |
| Mensajes red | `Client2.3.7/as2invoker/com/ankamagames/dofus/network/messages/game/actions/fight/` |

No editar **multi** en Fase 1 salvo que pruebas en cliente demuestren bug puramente visual tras corregir **game**.
