# Roadmap de Externalizacion de Metadata (Spells)

> Objetivo: diseñar como mover a la BD los **16 casos clase A** ("metadata movible") identificados
> en [docs/knowledge-map-spells.md](docs/knowledge-map-spells.md), para pasar de **~62% a ~81%**
> data-driven **sin modificar el pipeline de combate**.
>
> Restriccion dura: NO se toca el pipeline `FightActor.CastSpell` -> `EffectDispatcher.Dispatch`
> -> `EffectManager.SpellEffects[]` -> `handler.Apply()`. Cada caso se resuelve haciendo que un
> handler **lea un dato** en lugar de una constante. Este documento es **diseño, no implementacion**.

## Modelo de datos (donde se engancha la metadata)

- Hechizo a nivel de nivel: [SpellTemplate.cs](Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/World/Spells/SpellTemplate.cs) (`[Table("spells_levels")]`, Dapper.Contrib). Flags **por hechizo** = columna o tabla lateral por `SpellId`.
- Efecto: [Effect.cs](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Spells/Effect.cs). Sus campos vienen de un **blob binario** -> metadata **por efecto** = tabla lateral `(spell_id, effect_id)`.
- Monstruo: tabla `monsters` (`Parche_src_emu/sushine.sql:16321`).

Detalle de tipos y DDL en [docs/metadata-schema-proposal.md](docs/metadata-schema-proposal.md).
Priorizacion en [docs/metadata-priority-matrix.md](docs/metadata-priority-matrix.md).

Leyenda esfuerzo: **S** (<2 dias), **M** (2-5 dias), **L** (>1 semana).

---

## Fichas de los 16 casos clase A

### S1 — Carga de Colere de Iop (daño x2 si "cargado")
- **Comportamiento actual:** si `Spell.Id == 159` y el caster tiene `State_51`, el daño se duplica; si no, se aplica el estado para el proximo lanzamiento.
- **Donde vive hoy:** [DirectDamage.cs:41-52](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Damages/DirectDamage.cs) (rama `if (Spell.Id == 159)` + `State_51`).
- **Estructura ideal:** `effect_metadata.requires_state` + `effect_metadata.bonus_if_state` + `bonus_multiplier` (objeto `effect_metadata`).
- **Impacto:** elimina la rama `spell.Id == 159`; habilita "bonus si estado X" para cualquier hechizo de carga.
- **Riesgo de regresion:** **Bajo** (el comportamiento por defecto es "sin metadata = sin bonus"; solo 159 tiene fila).
- **Esfuerzo:** S.

### S2 — Cura a enemigo (whitelist 192)
- **Comportamiento actual:** las curas filtran a aliados salvo `spell.Id == 192` (Ronce Apaisante), que puede curar enemigos.
- **Donde vive hoy:** [Heal.cs:29](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Heals/Heal.cs) (`switch (spell.Id)` / `AllowsEnemyHealing`).
- **Estructura ideal:** `effect_metadata.allow_enemy_target` (bool) o usar correctamente la `Target` mask del efecto.
- **Impacto:** elimina la whitelist por SpellId; cualquier cura puede declararse "afecta enemigos" por dato.
- **Riesgo de regresion:** **Bajo** (default = solo aliados; 192 lleva flag true).
- **Esfuerzo:** S.

### S3 — Suicidio de invocacion (Sacrificada 233)
- **Comportamiento actual:** `Effect_Kill` con `Spell.Id == 233` mata solo a la muñeca (caster), no a los enemigos; los enemigos reciben el daño del otro efecto.
- **Donde vive hoy:** [Kill.cs:15-26](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Others/Kill.cs) (`IsSacrificialDollSuicide(233)`).
- **Estructura ideal:** `effect_metadata.kill_target` (enum: `affected` | `caster` | `summon`).
- **Impacto:** elimina la rama `spellId == 233`; cualquier hechizo puede declarar "kill se aplica al caster/invocacion".
- **Riesgo de regresion:** **Bajo-medio** (toca un efecto de muerte; mitigable: default `affected` = comportamiento actual generico).
- **Esfuerzo:** S.

### S4 — Lista de hechizos de trampa
- **Comportamiento actual:** una lista de SpellId (`PoisonedTrap, Trap, TrapofSilence, RepellingTrap, ParalyzingTrap, TrickyTrap, LethalTrap, MassTrap`) decide reglas de celda.
- **Donde vive hoy:** [Fight.cs:790](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Fights/Fight.cs).
- **Estructura ideal:** `spell_flags.is_trap` (bool).
- **Impacto:** una trampa nueva no requiere editar la condicion en `Fight.cs`.
- **Riesgo de regresion:** **Bajo** (marcar los 8 hechizos existentes con la flag = paridad exacta).
- **Esfuerzo:** S.

### S5 — Referencia a invocacion Roublabot (SpellId 430)
- **Comportamiento actual:** `RoublabotSpellId = 430` usado para aplicar estados de esclavo y mensajes.
- **Donde vive hoy:** [SlaveFighter.cs:22](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/SlaveFighter.cs); usado en [Summon.cs:114,129](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Summon/Summon.cs) y [FightActor.cs:761](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs).
- **Estructura ideal:** `summon_flags.category` (ej. `slave`) ligado al hechizo/monstruo, en vez de constante.
- **Impacto:** quita el acoplamiento a un id concreto; habilita mas invocaciones tipo esclavo por dato.
- **Riesgo de regresion:** **Medio** (varios puntos de uso; requiere coherencia entre spell y monster).
- **Esfuerzo:** M.

### S6 — IDs de explosion/muro de bomba por elemento
- **Comportamiento actual:** constantes de SpellId por elemento (fuego/aire/agua) para explosion, daño y muro.
- **Donde vive hoy:** [BombFighter.cs:31-43](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/BombFighter.cs).
- **Estructura ideal:** tabla `bomb_element_spells(element, explosion_spell, damage_spell, wall_spell)` o `spell_flags.bomb_element`.
- **Impacto:** define bombas por dato; reduce constantes en el actor.
- **Riesgo de regresion:** **Medio** (mecanica de bombas sensible; requiere paridad por elemento).
- **Esfuerzo:** M.

### S7 — Listas de hechizos Pandawa (Lait de Bambou / Alcohol)
- **Comportamiento actual:** arrays de SpellId clasifican hechizos de "leche de bambu" y "alcohol".
- **Donde vive hoy:** [FightActor.cs:65-79](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs).
- **Estructura ideal:** `spell_flags.pandawa_role` (enum `none|alcohol|bamboo_milk`).
- **Impacto:** elimina las listas estaticas; clasificacion por dato.
- **Riesgo de regresion:** **Medio** (interactua con apariencias y estados; ver S7/K8/T2 juntos).
- **Esfuerzo:** M.

### S12 — DamageReduction mapea caracteristicas por SpellId
- **Comportamiento actual:** `GetAssociatedCaracteristics(Spell.Id)` deriva las caracteristicas de reduccion segun el hechizo.
- **Donde vive hoy:** [DamageReduction.cs:18](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Armor/DamageReduction.cs).
- **Estructura ideal:** mapear por **EffectId** (ya hay 2 EffectId) en lugar de SpellId; sin tabla nueva.
- **Impacto:** vuelve el handler generico por efecto; quita dependencia de SpellId.
- **Riesgo de regresion:** **Bajo** (refactor de mapeo interno, comportamiento identico).
- **Esfuerzo:** S.

### S13 — Timing de glifo (inicio vs fin de turno)
- **Comportamiento actual:** una lista `SPELLS_GLYPH_END_TURN` decide si el glifo dispara al final del turno.
- **Donde vive hoy:** [Glyph.cs:16,46](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Fights/Triggers/Glyph.cs).
- **Estructura ideal:** `effect_metadata.trigger_timing` (enum `turn_begin|turn_end`).
- **Impacto:** elimina la lista; el timing es dato del glifo.
- **Riesgo de regresion:** **Bajo** (default `turn_begin`; los glifos de la lista llevan `turn_end`).
- **Esfuerzo:** S.

### M2 — Identidad de invocacion Roublabot (MonsterId 3120)
- **Comportamiento actual:** `RoublabotMonsterId = 3120` distingue la invocacion Roublabot para estados y reglas.
- **Donde vive hoy:** [SlaveFighter.cs:21,36](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/SlaveFighter.cs); [FightActor.cs:477,1182](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs); [Summon.cs:113,129](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Summon/Summon.cs).
- **Estructura ideal:** `monster_flags.summon_category` (`slave`/`roublabot`).
- **Impacto:** elimina la constante; reglas por categoria de monstruo.
- **Riesgo de regresion:** **Medio** (coordinar con S5).
- **Esfuerzo:** M.

### M3 — Elegibilidad de carga Pandawa (monstruo 2877, bones 842)
- **Comportamiento actual:** Karcham no puede cargar al monstruo `2877` ni a actores con `BonesID == 842`.
- **Donde vive hoy:** [KarchamHandler.cs:27,33](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Spells/Casts/Pandawa/KarchamHandler.cs).
- **Estructura ideal:** `monster_flags.carriable` (bool) + lista `bone_blacklist` (config) o flag por bones.
- **Impacto:** la elegibilidad de carga deja de tener constantes de contenido.
- **Riesgo de regresion:** **Bajo** (marcar 2877 como no-carriable = paridad).
- **Esfuerzo:** S.

### M4 — Loot Doplon por monstruo
- **Comportamiento actual:** un diccionario `DoplonByMonsterId` asigna el item doplon por id de monstruo.
- **Donde vive hoy:** [FightResults.cs:193,216](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Fights/Results/FightResults.cs).
- **Estructura ideal:** tabla de loot/recompensa por monstruo (`monster_flags.doplon_item_id` o tabla de drops dedicada).
- **Impacto:** loot configurable por dato.
- **Riesgo de regresion:** **Bajo** (tabla espejo del diccionario actual).
- **Esfuerzo:** S.

### M5 — Comportamiento de grupo por lider
- **Comportamiento actual:** ciertos lideres (`2785, 494, 2781`) activan comportamiento de grupo especial.
- **Donde vive hoy:** [MonsterGroup.cs:148](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Monsters/MonsterGroup.cs).
- **Estructura ideal:** `monster_flags.group_behavior` (enum/flag).
- **Impacto:** quita ids hardcodeados del agrupamiento.
- **Riesgo de regresion:** **Medio** (afecta spawning/dificultad de grupo).
- **Esfuerzo:** M.

### K1 — Tabla de apariencias (transformaciones)
- **Comportamiento actual:** un `switch` enorme mapea valores de apariencia (667 Picole, 729 Momificacion, 874, 969-971, 1575/1576...) a `bonesId`/`scale`/`skinId` (con variante por sexo).
- **Donde vive hoy:** [ChangeSkin.cs:13-29,103-155](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/States/ChangeSkin.cs).
- **Estructura ideal:** tabla `appearance_map(effect_value, bones_id, scale, skin_id_male, skin_id_female)`.
- **Impacto:** externaliza TODAS las transformaciones de apariencia; el handler solo lee la tabla.
- **Riesgo de regresion:** **Bajo-medio** (tabla espejo exacta del switch; verificar variante por sexo).
- **Esfuerzo:** S-M.

### K8 — Listas de apariencia/estado/stats del alcohol Pandawa
- **Comportamiento actual:** arrays `PandawaAlcoholAppearanceIds`, `PandawaAlcoholStates`, `PandawaAlcoholStatsEffectIds` describen el set de alcohol.
- **Donde vive hoy:** [FightActor.cs:88-104](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs).
- **Estructura ideal:** parte de `spell_flags.pandawa_role` + `appearance_map` (apariencias) + estados por dato.
- **Impacto:** completa la externalizacion Pandawa junto a S7/T2.
- **Riesgo de regresion:** **Medio** (set acoplado; migrar junto a S7).
- **Esfuerzo:** M.

### K5 — Clasificacion de efectos para stats (Ignored/Negative)
- **Comportamiento actual:** dos `HashSet<EffectsEnum>` clasifican efectos ignorados/negativos para el calculo de stats de item.
- **Donde vive hoy:** [ItemEffectHandler.cs:15,47](Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Items/ItemEffectHandler.cs).
- **Estructura ideal:** tabla `effect_meta(effect_id, is_ignored_for_stats, is_negative_for_stats)` (metadata por EffectId, no por hechizo).
- **Impacto:** clasificacion de efectos por dato; reutilizable.
- **Riesgo de regresion:** **Bajo** (tabla espejo de los HashSet).
- **Esfuerzo:** M (es de items, borde del foco de combate).

> Nota: **T1** y **T2** no son casos independientes: T1 se resuelve con S1 (`requires_state`/
> `bonus_if_state`) y T2 con S7/K8 (metadata Pandawa). **K2** y **K3** son partes de S13 y S6.

---

## Tabla resumen (16 casos)

| Caso | Objeto de esquema | Esfuerzo | Riesgo |
| --- | --- | --- | --- |
| S1 (+T1) | effect_metadata (requires_state/bonus_if_state) | S | Bajo |
| S2 | effect_metadata (allow_enemy_target) | S | Bajo |
| S3 | effect_metadata (kill_target) | S | Bajo-medio |
| S4 | spell_flags (is_trap) | S | Bajo |
| S5 | summon_flags | M | Medio |
| S6 (+K3) | spell_flags (bomb_element) / tabla bomba | M | Medio |
| S7 (+T2) | spell_flags (pandawa_role) | M | Medio |
| S12 | (refactor a EffectId, sin tabla) | S | Bajo |
| S13 (+K2) | effect_metadata (trigger_timing) | S | Bajo |
| M2 | monster_flags (summon_category) | M | Medio |
| M3 | monster_flags (carriable/bones) | S | Bajo |
| M4 | monster_flags (doplon) / tabla loot | S | Bajo |
| M5 | monster_flags (group_behavior) | M | Medio |
| K1 | appearance_map | S-M | Bajo-medio |
| K8 | spell_flags + appearance_map | M | Medio |
| K5 | effect_meta (clasificacion stats) | M | Bajo |

---

## Calculo 62% -> 81% data-driven

De [docs/auditoria-capacidades-motor-spells.md](docs/auditoria-capacidades-motor-spells.md): hoy
**10/16 mecanicas** son data-driven (62,5%). Las 3 mecanicas "a un paso" son **Invocaciones,
Glifos, Transformaciones**. Externalizarlas:

- **Transformaciones** -> requiere **K1** (appearance_map) [+ K8].
- **Glifos** -> requiere **S13** (trigger_timing).
- **Invocaciones** -> requiere **S3** (kill_target) + **S5/M2** (categoria de invocacion).

Con esos cambios, las 3 mecanicas suben a data-driven: **13/16 = 81,25%**. Los casos restantes
(S1, S2, S4, S6, S7, S12, M3, M4, M5, K5) refuerzan daño/curas/trampas/debuffs ya contados como
data-driven y, sobre todo, **eliminan ramas `spell.Id` causantes de regresiones** (S1/S2/S3), pero
no cambian el conteo de mecanicas. El salto 62% -> 81% lo entregan **K1, S13, S3 y S5/M2**.

> Conclusion: el objetivo (62% -> 81%) se logra con **4 piezas** (appearance_map, trigger_timing,
> kill_target, categoria de invocacion), todas de esfuerzo S/M y sin tocar el pipeline.
