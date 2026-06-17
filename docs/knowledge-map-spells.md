# Knowledge Map — Conocimiento atrapado en codigo (Spells & Combate)

> Objetivo: inventariar **todo comportamiento de combate/hechizos** que aun depende de
> `SpellId`, `MonsterId`, `StateId`, **constantes hardcodeadas** o **listas estaticas**, y
> clasificar cada caso para saber **que conocimiento sigue atrapado en codigo y cuanto costaria
> externalizarlo**.
>
> Metodo: barrido read-only de `Sunshine.WorldServer` + esquema de `sushine.sql`. No analiza bugs
> ni historial.

---

## Clasificacion y esfuerzo

| Clase | Significado | Accion |
| --- | --- | --- |
| **A — Metadata movible a BD** | Es conocimiento de contenido; el motor ya tiene el mecanismo, solo falta el dato | Añadir columna/tabla y leerla |
| **B — Regla de motor legitima** | Es logica de reglas del juego o de plataforma; vivir en codigo es correcto | Dejar en codigo (a lo sumo, ordenar) |
| **C — Sistema nuevo requerido** | El comportamiento no tiene mecanismo generico; externalizarlo exige un subsistema | Diseñar el subsistema y luego su esquema |

Esfuerzo: **S** (<2 dias), **M** (2-5 dias), **L** (>1 semana).

> Nota de alcance: se excluyen constantes de **infraestructura** no ligadas a hechizos (limites de
> banco/casa, tamaño de mapa, IDs de skill de casa, XP de montura, tablas de teleport, plantillas de
> item). Son clase B por definicion y no son "conocimiento de hechizos". Se listan al final de forma
> agregada.

---

## 1. Dependencias de `SpellId`

| # | Comportamiento | Ubicacion | Clase | Esfuerzo | Como externalizar |
| --- | --- | --- | --- | --- | --- |
| S1 | Carga de Colere (daño x2 si "cargado") atado a `Spell.Id == 159` | `Effects/Spells/Damages/DirectDamage.cs:41` | **A** | S | columna efecto `requires_state`/`bonus_if_state` |
| S2 | Cura a enemigo permitida solo para `192` (whitelist) | `Effects/Spells/Heals/Heal.cs:29` | **A** | S | flag efecto `allow_enemy_target` o usar `Target` mask |
| S3 | Suicidio de invocacion atado a `spellId == 233` | `Effects/Spells/Others/Kill.cs` | **A** | S | columna efecto `kill_target` (caster/summon/target) |
| S4 | Lista de spells de trampa (no consumir celda) `PoisonedTrap, Trap, TrapofSilence...` | `Fights/Fight.cs:790` | **A** | S | flag `is_trap` en el hechizo/efecto |
| S5 | `RoublabotSpellId = 430` (referencia a invocacion del Roublard) | `Actors/Fighters/SlaveFighter.cs:22`; usado en `Summon.cs:114,129`, `FightActor.cs:761` | **A/B** | M | referencia por dato de invocacion en vez de constante |
| S6 | IDs de explosion/muro de bomba por elemento (`2822/2845/2830`, `2823/2827/2831`, `2825/2829/2833`) | `Actors/Fighters/BombFighter.cs:31-43` | **A** | M | tabla `bomb_element -> {explosion, damage, wall} spellId` |
| S7 | Listas Pandawa: `BambooMilkSpellIds`, `PandawaAlcoholSpellIds` | `Actors/Fighters/FightActor.cs:65-79` | **A** | M | flags de hechizo/estado en BD |
| S8 | Arma / combate cuerpo a cuerpo `spell.Id == 0` | `FightActor.cs:745,893`; `Fight.cs:782` | **B** | — | regla de plataforma (arma sin hechizo); dejar |
| S9 | Dispel por id de hechizo (`Spell.Id == Value`) | `Effects/Spells/Debuffs/DebuffEffects.cs:23` | **B** | — | el id ES el dato (referencia legitima) |
| S10 | Doble buff si ya activo (`x.Spell.Id == Spell.Id`) | `Spells/Casts/Cra/PunitiveHandler.cs:26` | **B** | — | comparacion de identidad legitima |
| S11 | 22 registros `[SpellCastHandler(spellId)]` (cast custom) | `Spells/Casts/**` | **B (8) / A (4) / A-con-feature (10)** | M-L | ver auditoria de madurez (Q6): 4 borrables, 10 reducibles, 8 legitimos |
| S12 | `DamageReduction` mapea caracteristicas por `Spell.Id` | `Effects/Spells/Armor/DamageReduction.cs:18` | **A** | S | mapear por `EffectId`, no por `SpellId` |
| S13 | Timing de glifo por lista de spell ids | `Fights/Triggers/Glyph.cs:16,46` | **A** | S | columna `trigger_timing` (TURN_BEGIN/END) |

---

## 2. Dependencias de `MonsterId`

| # | Comportamiento | Ubicacion | Clase | Esfuerzo | Como externalizar |
| --- | --- | --- | --- | --- | --- |
| M1 | Mecanicas de bosses Frigost (12 monstruos: Royalmouth, Ben le Ripate, Hamrack, Obsidiantre, Tengu/Yokai/Yomi Givrefoux, Korriandre, Fuji, Kolosso, Glourseleste, Mansot) — invulnerabilidad, estados temporizados, invocaciones forzadas, ventanas de vulnerabilidad | `Fights/Mechanics/FrigostBossMechanics.cs:27-38` + todo el archivo | **C** | L | Subsistema de "encounter scripting" data-driven (ventanas, triggers por turno, estados); hoy es un modulo hardcodeado |
| M2 | `RoublabotMonsterId = 3120` (identidad de la invocacion Roublabot) | `Actors/Fighters/SlaveFighter.cs:21,36`; `FightActor.cs:477,1182`; `Summon.cs:113,129` | **A/B** | M | flag de monstruo `is_roublabot`/categoria de invocacion |
| M3 | Elegibilidad de carga Pandawa: `monster.Record.Id == 2877`, `BonesID == 842` | `Spells/Casts/Pandawa/KarchamHandler.cs:27,33` | **A** | S | flags de monstruo `carriable` + `bone_blacklist` |
| M4 | Loot Doplon por monstruo (`DoplonByMonsterId`) | `Fights/Results/FightResults.cs:193,216` | **A** | S | tabla de loot por monstruo (drop table) |
| M5 | Comportamiento de grupo por lider (`Leader.Record.Id == 2785/494/2781`) | `Actors/Monsters/MonsterGroup.cs:148` | **A/B** | M | flag de monstruo `group_behavior` |
| M6 | Caso especial Fuji (`Record.Id == 2970`) en quitar invulnerabilidad | `Fights/Fight.cs:386` | **C** | — | parte de M1 (encounter scripting) |
| M7 | Dopeul: id de monstruo de combate Dopeul (gestion de cooldown/recompensa) | `Actors/Npcs/Replies/DopeulReply.cs`; `Fights/Types/FightPvM.cs:59` | **B** | — | flujo de NPC/PvM; el id es dato de configuracion del NPC |

---

## 3. Dependencias de `StateId`

| # | Comportamiento | Ubicacion | Clase | Esfuerzo | Como externalizar |
| --- | --- | --- | --- | --- | --- |
| T1 | Estado "cargado" de Colere (`State_51`) usado como flag ad-hoc | `Effects/Spells/Damages/DirectDamage.cs:45-52` | **A** | S | parte de S1 (`requires_state`/`bonus_if_state`) |
| T2 | Estados de alcohol Pandawa (`Drunk, State_44, Barmy`) como lista | `FightActor.cs:99-101,1479` | **A** | S | parte de S7 (flags en estado/hechizo) |
| T3 | Estados de carga/transporte (`Carrying`/`Carried`) y su limpieza | `FightActor.cs:1286-1320,1562` | **B** | — | mecanica de motor (carry); dejar |
| T4 | Bloqueos por estado (`Gravity`, `Rooted`, `Heavy`, `Unmovable`) en movimiento/empuje | `FightActor.cs:1184`; `KarchamHandler.cs:30`; `RoublabotSpellHelper.cs:79` | **B** | — | semantica de estado del motor; dejar |
| T5 | `Invulnerable` / `Unhealable` previenen daño/cura | `FightActor.cs:1023,1151`; `Fight.cs:411-417` | **B** | — | regla de motor; dejar |
| T6 | `Kaboom` para objetivo de muro de bomba | `Fights/Triggers/BombWall.cs:252` | **B** | — | mecanica de bombas; dejar |
| T7 | `Weakened` afecta combate | `CharacterFighter.cs:123` | **B** | — | regla de motor; dejar |
| T8 | `StatesRequired` / `StatesForbidden` en validacion de cast | `FightActor.cs:913-916`; `AIFighter.cs:101-104`; `MonsterAttackAI.cs:571-574` | **B (ya data-driven)** | — | YA viene de BD (`StatesRequiredCSV`/`StatesForbiddenCSV`); modelo correcto |

> Lectura clave de los estados: la **mayoria** de las dependencias de StateId son **reglas de motor
> legitimas (B)** — cada estado tiene una semantica que debe vivir en codigo. Solo T1 y T2 son
> conocimiento de contenido atrapado (A). T8 demuestra que el motor YA sabe leer estados desde datos.

---

## 4. Constantes / listas estaticas (combate y hechizos)

| # | Constante / lista | Ubicacion | Clase | Esfuerzo | Como externalizar |
| --- | --- | --- | --- | --- | --- |
| K1 | Tabla de apariencias (667, 729, 874, 969-971, 1575/1576, skinIds 1443/1444/1448/1449) | `Effects/Spells/States/ChangeSkin.cs:13-29,103-155` | **A** | S-M | tabla `appearance_map(effectValue -> bonesId, scale, skinId, sexVariant)` |
| K2 | `SPELLS_GLYPH_END_TURN` | `Fights/Triggers/Glyph.cs:16` | **A** | S | parte de S13 (`trigger_timing`) |
| K3 | Constantes de spell de bomba (explosion/muro/daño por elemento) | `BombFighter.cs:31-43` | **A** | M | parte de S6 |
| K4 | Bosses Frigost (12 const ids + `InvulnerableBossIds` + ventanas) | `FrigostBossMechanics.cs:27-52` | **C** | L | parte de M1 (encounter scripting) |
| K5 | `IgnoredEffectsForStats` / `NegativeEffectsForStats` (clasificacion de efectos) | `Effects/Items/ItemEffectHandler.cs:15,47` | **A/B** | M | podria derivarse de metadata del efecto; util tener tabla `effect_meta` |
| K6 | Eficiencia de zona (`EFFECTSHAPE_DEFAULT_EFFICIENCY`, decremento por celda) | `Maps/Shapes/Zone.cs:9-10` | **B** | — | regla de motor (atenuacion por area); dejar |
| K7 | Limites de juego (`APLimit 12`, `MPLimit 6`, `RangeLimit 6`) | `Actors/Stats/StatsFields.cs:22-26` | **B** | — | reglas del juego; dejar (o config global) |
| K8 | `PandawaAlcoholAppearanceIds`, `PandawaAlcoholStates`, `PandawaAlcoholStatsEffectIds` | `FightActor.cs:88-104` | **A** | M | parte de S7 (metadata Pandawa) |

---

## 5. Resumen: cuanto conocimiento esta atrapado y cuanto cuesta soltarlo

### Conteo por clase (casos de combate/hechizos inventariados)

| Clase | Casos | Interpretacion |
| --- | --- | --- |
| **A — Metadata movible a BD** | **~16** | Conocimiento de contenido atrapado en codigo. Externalizable sin rediseño |
| **B — Regla de motor legitima** | **~11** | Correcto que viva en codigo (semantica de estados, arma id 0, dispel por id, limites) |
| **C — Sistema nuevo requerido** | **~2 frentes** | Encounter scripting de bosses (M1/M6/K4) y, fuera de este doc, IA de invocaciones y portales |

### Coste de externalizar el grupo A (alto retorno, sin subsistemas)

Casi todos los casos A se agrupan en **6 cambios de metadata** reutilizables:

| Cambio de metadata | Resuelve | Esfuerzo |
| --- | --- | --- |
| 1. `appearance_map` (tabla) | K1 (transformaciones) | S-M |
| 2. `trigger_timing` (columna efecto) | S13, K2 (glifos) | S |
| 3. `kill_target` (columna efecto) | S3 (suicidio invo) | S |
| 4. `requires_state`/`bonus_if_state` (columnas efecto) | S1, T1 (carga Colere) | S-M |
| 5. `allow_enemy_target` (flag efecto) | S2 (cura enemigo) | S |
| 6. Flags de contenido (`is_trap`, `carriable`/`bone_blacklist`, flags Pandawa, categoria de invocacion, bomba por elemento, loot por monstruo, mapear `DamageReduction` por EffectId) | S4, S5, S6, S7, S12, M2, M3, M4, M5, K3, K8, T2 | M (sumado) |

Coste total del grupo A: **bajo-medio** (todo son columnas/tablas + lectura; no toca el pipeline).
Beneficio: ademas de externalizar conocimiento, **elimina las ramas `spell.Id`** que han causado
regresiones (S1, S2, S3).

### Coste del grupo C (requiere subsistema)

- **Encounter scripting de bosses (M1/M6/K4):** esfuerzo **L**. `FrigostBossMechanics` ya tiene una
  estructura semi-generica (ventanas de vulnerabilidad, estados temporizados); convertirla en un
  motor data-driven de encuentros es un proyecto en si. Pragmaticamente, parte de las mecanicas de
  boss unicas pueden quedarse en codigo (B) y solo externalizar los parametros repetitivos
  (invulnerabilidad inicial, estados de apertura, ventanas por turnos).
- **IA de invocaciones y portales:** fuera del foco de este barrido (ver auditoria de capacidades);
  ambos son clase C.

---

## 6. Conclusion

El conocimiento atrapado en codigo **no es masivo ni esta disperso al azar**: se concentra en
**~16 casos de metadata (clase A)** que se resuelven con **6 cambios de datos reutilizables** de
esfuerzo bajo-medio, y en **1 frente de sistema (clase C)**: el scripting de bosses Frigost.

La mayoria de las dependencias de `StateId` son **reglas de motor legitimas (B)** — no son deuda.
El verdadero "conocimiento escondido" son: las tablas de apariencia, el timing de glifos, el target
de kill, el bonus por estado, la cura a enemigos, y un conjunto de flags de contenido
(trampa/carga/invocacion/bomba/loot). Todo eso es **dato disfrazado de codigo**.

> Veredicto: externalizar el grupo A es **barato y de alto impacto** (corta regresiones y sube la
> madurez data-driven). El grupo C (encounter scripting) es el unico que justifica diseñar un
> subsistema, y puede abordarse despues, de forma incremental.

### Fuentes (read-only)
- SpellId: `DirectDamage.cs`, `Heal.cs`, `Kill.cs`, `Fight.cs`, `SlaveFighter.cs`, `BombFighter.cs`,
  `FightActor.cs`, `DebuffEffects.cs`, `PunitiveHandler.cs`, `DamageReduction.cs`, `Glyph.cs`,
  `Spells/Casts/**`.
- MonsterId: `FrigostBossMechanics.cs`, `SlaveFighter.cs`, `KarchamHandler.cs`, `FightResults.cs`,
  `MonsterGroup.cs`, `Fight.cs`, `DopeulReply.cs`.
- StateId: `FightActor.cs`, `Summon.cs`, `BombWall.cs`, `CharacterFighter.cs`, `AIFighter.cs`,
  `MonsterAttackAI.cs`, `Rogue/*Helper.cs`.
- Constantes/listas: `ChangeSkin.cs`, `Glyph.cs`, `BombFighter.cs`, `FrigostBossMechanics.cs`,
  `ItemEffectHandler.cs`, `Zone.cs`, `StatsFields.cs`.
- Esquema BD (baseline de datos existentes): `Parche_src_emu/sushine.sql` (`spells_levels`).
