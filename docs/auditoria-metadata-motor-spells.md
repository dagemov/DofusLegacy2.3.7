# Auditoria de Metadata del Motor de Spells

> Tesis (confirmada por las auditorias previas): el emulador **ya no esta en la etapa de "hacer
> hechizos"**. El problema actual es **convertir conocimiento escondido en codigo en conocimiento
> expresado como datos**. Este informe inventaria, por mecanica, que datos existen, cuales faltan,
> que reglas siguen ocultas en C#, y como se moverian a la BD.
>
> Alcance: tablas `spells` / `spells_levels` (`Parche_src_emu/sushine.sql`) y handlers de
> `Sunshine.WorldServer`. Metodo read-only. No analiza bugs ni historial.

---

## 0. Baseline: que datos YA existen en la BD

Antes de decir "que falta", hay que reconocer cuanto ya esta modelado. La data por hechizo es rica.

### Tabla `spells` (cabecera del hechizo)
`Spell` (id), `Name`, `Description`, `TypeId`, `ScriptParams`, `ScriptParamsCritical`,
`ScriptId`, `ScriptIdCritical`, `IconId`, `SpellLevelsIdsCSV`, `UseParamCache`.

### Tabla `spells_levels` (un registro por nivel)
`SpellId`, `SpellBreed`, `ApCost`, `Range`, `MinRange`, `CastInLine`, `CastInDiagonal`,
`CastTestLos`, `RangeCanBeBoosted`, `CriticalHitProbability`, `CriticalFailureProbability`,
`CriticalFailureEndsTurn`, `StatesRequiredCSV`, `StatesForbiddenCSV`, `NeedFreeCell`,
`NeedFreeTrapCell`, `NeedTakenCell`, `MaxStack`, `MaxCastPerTurn`, `MaxCastPerTarget`,
`MinCastInterval`, `InitialCooldown`, `GlobalCooldown`, `MinPlayerLevel`, `HideEffects`,
`Hidden`, `Effects`, `CriticalEffects`.

### Binario `Effects` / `CriticalEffects` (lista de efectos)
Cada efecto codifica: **EffectId**, **DiceNum**, **DiceFace**, **Value**, **Delay**, **Duration**,
**Target** (mascara `SpellTargetType`), **ZoneShape**, **ZoneMinSize**, **ZoneSize**
(ver `EffectManager.GetEffects`).

**Conclusion del baseline:** la BD ya expresa de forma nativa: coste, rango (min/max), linea/
diagonal/LoS, criticos, estados requeridos/prohibidos, ocupacion de celda, **cooldowns**, limites
de lanzamiento, **area/zona**, **target mask**, y los **parametros de cada efecto** (dados, valor,
duracion, delay). Es decir: el "esqueleto" data-driven es solido. Lo que falta es **metadata
semantica de ciertas mecanicas** que hoy el handler resuelve con constantes.

---

## 1. Auditoria por mecanica

Formato: **Existe hoy** / **Falta** / **Regla escondida en codigo** (`archivo:linea`) / **Como
moverlo a BD**.

### 1.1 Daños
- **Existe:** EffectId por escuela (10 en `DirectDamage`), dados/valor, duracion (DoT), zona, target.
- **Falta:** marca de "daño de carga / segundo lanzamiento".
- **Escondido:** `DirectDamage.cs:41` `if (Spell.Id == 159)` (Colere de Iop usa `State_51` como
  "cargado").
- **A BD:** columna de efecto `requires_state` / `bonus_if_state` (id de estado + multiplicador),
  para que "duplica si el caster tiene estado X" sea dato, no `if` por SpellId.

### 1.2 Curas
- **Existe:** EffectId de cura, valor/dados, duracion (HoT via `HealOverTimeBuff`), target.
- **Falta:** flag "puede curar a enemigos" (excepcion intencional).
- **Escondido:** `Heal.cs:29` `switch (spell.Id)` con `case 192` (Ronce Apaisante cura enemigo).
- **A BD:** flag de efecto `allow_enemy_target` (bool) o usar la propia `Target` mask para permitir
  enemigos; eliminar la whitelist por SpellId.

### 1.3 Buffs
- **Existe (completo):** `StatsBoost` mapea 38 EffectId a caracteristicas; valor + duracion + target.
- **Falta:** nada relevante.
- **Escondido:** ninguno significativo.
- **A BD:** ya es data-driven. Un buff nuevo = efecto + valor + duracion.

### 1.4 Debuffs
- **Existe (completo):** `SubStatsBoost` (31 EffectId, incl. robos), AP/MP debuff, erosion, dispel.
- **Falta:** nada relevante.
- **Escondido:** ninguno significativo.
- **A BD:** ya es data-driven.

### 1.5 Estados
- **Existe:** `AddState`/`RemoveState` (Effect `Value` = stateId, `Duration`); `StatesRequiredCSV`
  / `StatesForbiddenCSV` en `spells_levels`.
- **Falta:** definicion data-driven del **comportamiento** de cada estado (que hace Unmovable,
  Heavy, etc. esta implementado en el motor, no descrito en datos).
- **Escondido:** la semantica de estados especiales vive en el motor; aceptable mientras el set de
  estados sea cerrado.
- **A BD:** tabla `states` (id, nombre, flags de comportamiento) si se quiere extender estados sin
  codigo.

### 1.6 Invocaciones
- **Existe:** `Summon` (4 EffectId); el monstruo a invocar va en el efecto (`DiceNum`); limite via
  `Effect_AddSummonLimit` (buff de stat).
- **Falta:** (a) "que invocacion muere" cuando un hechizo mata una invo; (b) reglas de cupo por
  hechizo y liberacion al morir expresadas como dato; (c) si la invo es estatica/movil.
- **Escondido:** `Kill.cs` `spellId == 233` (suicidio de la Sacrificada); `Summon.cs:114,129`
  `Spell.Id == RoublabotSpellId`; logica de cupo en `FightActor`.
- **A BD:** columnas de efecto `kill_target` (enum: caster | summon | target) y `summon_kind`
  (static/mobile); regla de cupo derivada de `Effect_AddSummonLimit` (ya existe) sin ramas por id.

### 1.7 Trampas
- **Existe:** `TrapSpawn` (`Effect_Trap`); el spell de la trampa va en `DiceNum/DiceFace`; el
  trigger reusa `EffectDispatcher`; `NeedFreeTrapCell` en BD.
- **Falta:** poco; el modelo de "trampa = spell + efectos" es solido.
- **Escondido:** menor.
- **A BD:** ya casi completo. Una trampa nueva = spell de trampa + efectos.

### 1.8 Glifos
- **Existe:** `GlyphSpawn` (`Effect_Glyph`, `Effect_Glyph_402`); spell del glifo en datos; trigger
  generico.
- **Falta:** **timing del glifo** (inicio vs fin de turno) como dato.
- **Escondido:** `Glyph.cs:46` `SPELLS_GLYPH_END_TURN.Contains(CastedSpell.Id)` (lista de ids).
- **A BD:** columna `trigger_timing` (TURN_BEGIN | TURN_END) en el efecto del glifo; eliminar la
  lista hardcodeada.

### 1.9 Venenos (DoT)
- **Existe (completo):** un veneno = efecto de daño con `Duration > 0`; `DirectDamage.cs` crea
  `DamageOverTimeBuff` automaticamente. La escuela y el valor son datos.
- **Falta:** nada.
- **Escondido:** ninguno.
- **A BD:** ya es data-driven.

### 1.10 Teletransporte
- **Existe (completo):** `Effect_Teleport` (`Teleport.cs`); celda objetivo via target.
- **Falta:** nada.
- **Escondido:** ninguno.
- **A BD:** ya es data-driven.

### 1.11 Intercambio de posicion
- **Existe (completo):** `Effect_SwitchPosition` (`SwitchPosition.cs`).
- **Falta:** nada.
- **Escondido:** ninguno.
- **A BD:** ya es data-driven.

### 1.12 IA de invocaciones
- **Existe:** IA generica de `MonsterFighter` (las invos heredan comportamiento de monstruo).
- **Falta:** **perfil de comportamiento por invocacion** como dato (objetivo preferente, hechizos a
  usar, prioridad, agresividad).
- **Escondido:** no hay subsistema de "brain" parametrizable; el comportamiento de invo depende de
  la IA generica de monstruo, no de la metadata del hechizo invocador.
- **A BD:** tabla `summon_ai_profile` (id de monstruo invocado -> politica) o referencia a un script
  de IA por dato. Requiere primero el subsistema (ver auditoria de capacidades).

### 1.13 Transformaciones
- **Existe:** `Effect_ChangeAppearance` (`ChangeSkin.cs`); el valor de apariencia va en el efecto.
- **Falta:** **tabla apariencia -> `bonesId`/`scale`/`skinId`**; definicion de transformacion
  "completa" (set de hechizos/stats alterno).
- **Escondido:** `ChangeSkin.cs:103-155` `switch` con constantes (667 Picole, 729 Momification
  Xelor, 874 Zatoishwan, 969-971, 1575/1576 Pleutre/Psycho, skinIds 1443/1444/1448/1449).
- **A BD:** tabla `appearance_map` (effectValue -> bonesId, scale, skinId, sex-variant); el handler
  solo lee la tabla. Transformacion completa = sistema nuevo.

### 1.14 Portales
- **Existe:** nada dedicado (solo `Teleport` puntual).
- **Falta:** **todo el modelo de portales** (entidades portal en el tablero, traslado entre ellos,
  linea de tiro a traves de portales).
- **Escondido:** no aplica (no existe).
- **A BD:** primero el subsistema; luego datos (radio, duracion, tipo de traslado).

### 1.15 Bombas
- **Existe:** `BombFighter`, `BombManager`, efectos `Effect_SummonBomb`/`Effect_ActivateBomb`/
  `Effect_AddComboDamage`; limite de bombas; estados.
- **Falta:** parametros de combo/muros/explosion diferida como dato (hoy en `SpellCastHandler`).
- **Escondido:** casts custom Roublard (`Rebours`, `DernierSouffle`, `Poudre`, `Botte`, `Kaboom`,
  `Entourloupe`, `Remission`, `RoublabotDetonation`); constantes de combo y deteccion de muros.
- **A BD:** meta-efectos de bomba (`bomb_combo`, `bomb_wall`, `bomb_delayed_explode`) con sus
  parametros; reduciria varios casts a datos, manteniendo los genuinos.

### 1.16 Invisibilidad
- **Existe (completo):** `Effect_Invisibility` (`Invisibility.cs`) + `InvisibilityBuff`;
  `Effect_RevealsInvisible`; deteccion al castear gestionada por el motor.
- **Falta:** nada relevante.
- **Escondido:** menor (reglas de deteccion en `CastSpell`, aceptables).
- **A BD:** ya es data-driven.

---

## 2. Reglas transversales escondidas en codigo (tablas de constantes)

Estas no pertenecen a una sola mecanica; son "conocimiento de contenido" viviendo en el motor:

| Constante / tabla en C# | Ubicacion | Deberia ser |
| --- | --- | --- |
| `BambooMilkSpellIds`, `PandawaAlcoholSpellIds`, ids de apariencia de alcohol | `FightActor.cs:60-79,1401,1434` | columnas/flags del hechizo o del estado en BD |
| `switch` de apariencias (667, 729, 874, 969-971, 1575/1576...) | `ChangeSkin.cs:103-155` | tabla `appearance_map` |
| `SPELLS_GLYPH_END_TURN` | `Glyph.cs:46` | columna `trigger_timing` |
| `GetAssociatedCaracteristics(Spell.Id)` | `DamageReduction.cs:18` | mapeo por EffectId, no por SpellId |
| `BonesID == 842`, `monster.Record.Id == 2877` (elegibilidad de carga) | `KarchamHandler.cs:27,33` | flags `carriable` / `bone_blacklist` en datos de monstruo |
| `RoublabotSpellId` / `RoublabotMonsterId` | `SlaveFighter` / `Summon.cs` | referencia por dato de invocacion, no constante |
| `State_51` "cargado" de Colere | `DirectDamage.cs` | `requires_state` / `bonus_if_state` en el efecto |
| Suicidio de invocacion (233) | `Kill.cs` | `kill_target` en el efecto |

---

## 3. Propuesta de migracion (de codigo a datos)

Ordenada por retorno (cuanto conocimiento externaliza por unidad de esfuerzo).

### Prioridad ALTA (datos puros, sin subsistema nuevo)
1. **`appearance_map`** (tabla): elimina el `switch` de `ChangeSkin`. Externaliza todas las
   transformaciones de apariencia. Esfuerzo: S-M.
2. **`trigger_timing`** (columna de efecto de glifo): elimina `SPELLS_GLYPH_END_TURN`. Esfuerzo: S.
3. **`kill_target`** (columna de efecto): externaliza el suicidio de invocacion (233) y similares.
   Esfuerzo: S.
4. **`requires_state` / `bonus_if_state`** (columnas de efecto): externaliza la carga de Colere
   (159) y patrones "bonus si estado". Esfuerzo: S-M.
5. **`allow_enemy_target`** (flag de efecto) o uso correcto de `Target` mask: elimina la whitelist
   de cura a enemigos (192). Esfuerzo: S.

### Prioridad MEDIA (datos de contenido en otras tablas)
6. **Flags de monstruo** `carriable` + `bone_blacklist`: externaliza `KarchamHandler` (842, 2877).
7. **Metadata de invocacion** `summon_kind` + cupo derivado de `Effect_AddSummonLimit`: quita ramas
   `RoublabotSpellId`.
8. **Flags de alcohol Pandawa** en estado/hechizo: quita las listas `*SpellIds` de `FightActor`.

### Prioridad BAJA (requieren subsistema antes que datos)
9. **`summon_ai_profile`**: requiere primero un motor de comportamiento de invocaciones.
10. **Modelo de portales**: requiere el subsistema espacial completo.
11. **Meta-efectos de bomba**: opcional; las bombas son mecanica genuina, la migracion es de
    conveniencia.

---

## 4. Cuanto conocimiento esta ya externalizado

Conteo sobre las 16 mecanicas auditadas:

| Estado de metadata | Mecanicas | % |
| --- | --- | --- |
| **Conocimiento totalmente en datos** | Daños*, Curas*, Buffs, Debuffs, Estados, Trampas, Venenos, Teletransporte, Intercambio, Invisibilidad | **~62%** |
| **Conocimiento casi en datos (faltan columnas)** | Invocaciones, Glifos, Transformaciones | **~19%** |
| **Conocimiento aun en codigo (falta sistema)** | IA de invocaciones, Portales, Bombas | **~19%** |

\* Daños y Curas estan en "datos" salvo una excepcion hardcodeada cada una (159, 192), de prioridad
ALTA y esfuerzo S.

**Lectura:** ~62% del conocimiento de mecanicas ya vive como datos. Con las 5 acciones de
**prioridad ALTA** (todas datos puros, esfuerzo S-M, sin subsistemas) se externaliza practicamente
todo el grupo "casi en datos", llevando el conocimiento-en-datos a **~81%**. El 19% restante
(IA de invocaciones, portales, y la conveniencia de bombas) depende de construir subsistemas, no de
mover constantes.

---

## 5. Conclusion

El motor ya tiene un **modelo de datos rico** (`spells_levels` cubre coste, rango, LoS, criticos,
estados req/prohibidos, cooldowns, limites, zona; el binario de `Effects` cubre id, dados, valor,
duracion, delay, target y area). El cuello no es la ausencia de columnas generales, sino un puñado
de **reglas semanticas concretas atrapadas en constantes de C#** (tablas de apariencia, timing de
glifos, target de kill, bonus por estado, flags de monstruo carriable, listas de alcohol Pandawa).

El trabajo de mayor retorno **no es refactorizar el motor**: es **una migracion de metadata** de
prioridad ALTA (5 columnas/tablas, esfuerzo S-M) que convierte conocimiento escondido en datos y,
de paso, elimina las ramas `spell.Id` que han causado regresiones. Eso confirma la tesis: el reto
ya no es "hacer hechizos", es **expresar como datos lo que hoy solo sabe el codigo**.

### Fuentes (read-only)
- Esquema: `Parche_src_emu/sushine.sql` (`spells` 57432, `spells_levels` 60043).
- Decodificacion de efectos: `Game/Effects/EffectManager.cs` (`GetEffects`).
- Reglas escondidas: `ChangeSkin.cs`, `Glyph.cs`, `Kill.cs`, `DirectDamage.cs`, `Heal.cs`,
  `Summon.cs`, `DamageReduction.cs`, `KarchamHandler.cs`, `FightActor.cs`.
