# Auditoria de Capacidades del Motor de Spells

> Objetivo: **no** identificar bugs ni historial. Identificar **que mecanicas de Dofus pueden
> expresarse hoy unicamente con datos** (Spell + Effects + Targets + Buffs + Cooldowns + Areas +
> States) **sin escribir codigo nuevo**.
>
> Resultado: una **matriz de capacidad** del motor y la respuesta a:
> **¿que porcentaje de las mecanicas de Dofus ya son data-driven?**
>
> Alcance: `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer`. Metodo: lectura de los handlers
> de efecto y del pipeline de cast (read-only).

---

## Leyenda de clasificacion

| Nivel | Significado | Que implica para crear una mecanica nueva |
| --- | --- | --- |
| **1. Solo datos** | El handler generico ya existe y se parametriza por `Effect` (valor, dados, duracion, target, area) | Crear el hechizo en BD. **Cero codigo.** |
| **2. Requiere metadata adicional** | El handler existe pero parte del comportamiento vive en constantes/tablas en C# | Crear el hechizo + ampliar la metadata (mover constantes a BD). **Codigo minimo / esquema.** |
| **3. Requiere sistema nuevo** | El mecanismo no existe en el motor | Diseñar e implementar el subsistema. **Codigo significativo.** |
| **4. Requiere SpellCastHandler** | Necesita un script propio por `SpellId` (orquestacion no reducible a efectos) | Escribir una clase `[SpellCastHandler]`. **Codigo por hechizo.** |

Como se decide: si el comportamiento se obtiene poblando `spell.Effects` (cada uno con su
`EffectId`, `Value`, `DiceNum/DiceFace`, `Duration`, `Target`, `Zone`) y el handler ya esta
registrado en `EffectManager.SpellEffects`, entonces es **Solo datos**. Si ademas necesita
constantes hardcodeadas, una clase de cast, o un subsistema inexistente, sube de nivel.

---

## Matriz de capacidad del motor

| # | Mecanica | Clasificacion | Handler / sistema actual | Que falta (si aplica) |
| --- | --- | --- | --- | --- |
| 1 | **Daños** | 🟢 Solo datos | `DirectDamage` (10 EffectId: 5 escuelas + variantes), `DamagePercent` (5), `FixedDamage`, `HpSteal` (5) | Nada para daño estandar. Solo la carga de Colere (159) esta hardcodeada |
| 2 | **Curas** | 🟢 Solo datos | `Heal` (3), `SubHealPercent`, `RestoreHpPercent`; filtro aliado/enemigo automatico | Cura a enemigo intencional necesita whitelist (192) -> metadata |
| 3 | **Buffs** | 🟢 Solo datos | `StatsBoost` (38 EffectId) -> `StatsBuff`; `APBuff`, `MPBuff`, `SpellBoost`, `Shield` | Nada: cualquier buff de stat = efecto + valor + duracion |
| 4 | **Debuffs** | 🟢 Solo datos | `SubStatsBoost` (31 EffectId, incl. robos), `APDebuff`, `MPDebuff`, `Erosion`, `Debuff`, `Dispel` | Nada |
| 5 | **Estados** | 🟢 Solo datos | `AddState` / `RemoveState` (Effect value = stateId, duracion) | Añadir un estado = datos. Algunos estados tienen comportamiento de motor (Unmovable, Heavy) ya implementado |
| 6 | **Trampas** | 🟢 Solo datos | `TrapSpawn` (`Effect_Trap`); el trigger reusa `EffectDispatcher` con el spell de la trampa | Una trampa nueva = spell de trampa + efectos. Datos |
| 7 | **Venenos (DoT)** | 🟢 Solo datos | `DirectDamage` con `Duration > 0` crea `DamageOverTimeBuff` automaticamente | Nada: un veneno = efecto de daño con duracion |
| 8 | **Teletransporte** | 🟢 Solo datos | `Teleport` (`Effect_Teleport`) | Nada |
| 9 | **Intercambio de posicion** | 🟢 Solo datos | `SwitchPosition` (`Effect_SwitchPosition`) | Nada |
| 10 | **Invisibilidad** | 🟢 Solo datos | `Invisibility` (`Effect_Invisibility`) + `InvisibilityBuff`; `RevealsInvisible`; deteccion al castear en `CastSpell` | Nada para invisibilidad estandar |
| 11 | **Invocaciones** | 🟡 Requiere metadata | `Summon` (4 EffectId); crea invocacion desde id de monstruo (`DiceNum`) | Limite de invocaciones, liberar cupo al morir, y "que invocacion muere" (233) viven en codigo, no en BD |
| 12 | **Glifos** | 🟡 Requiere metadata | `GlyphSpawn` (`Effect_Glyph`, `Effect_Glyph_402`); trigger generico | Timing de glifos de fin de turno usa lista hardcodeada `SPELLS_GLYPH_END_TURN` (`Glyph.cs:46`) |
| 13 | **Transformaciones** | 🟡 Requiere metadata | `ChangeSkin` (`Effect_ChangeAppearance`) cambia apariencia | Mapeos apariencia->`bonesId`/`scale` hardcodeados en un `switch` (667, 729, 874, 969-971...). Transformacion completa (set de hechizos nuevo, estado Arbol) = sistema nuevo |
| 14 | **IA de invocaciones** | 🟠 Requiere sistema nuevo | Existe IA generica de `MonsterFighter`; no hay configuracion por hechizo/invocacion | No hay motor de comportamiento ("brain") parametrizable por datos para invocaciones |
| 15 | **Portales** | 🟠 Requiere sistema nuevo | No existe sistema de portales (Xelor). Solo `Teleport` puntual | Subsistema de portales (red de portales, traslado entre ellos, linea de tiro) |
| 16 | **Bombas** | 🔴 Requiere SpellCastHandler | `BombFighter` + `BombManager` + 6-9 casts custom Roublard (combo, muros, explosion diferida) | Orquestacion no reducible a efectos: cadenas de combo, muros, detonacion programada |

---

## Distribucion por clasificacion

| Clasificacion | Cantidad | Mecanicas |
| --- | --- | --- |
| 🟢 **1. Solo datos** | **10 / 16** | Daños, Curas, Buffs, Debuffs, Estados, Trampas, Venenos, Teletransporte, Intercambio de posicion, Invisibilidad |
| 🟡 **2. Requiere metadata adicional** | **3 / 16** | Invocaciones, Glifos, Transformaciones |
| 🟠 **3. Requiere sistema nuevo** | **2 / 16** | IA de invocaciones, Portales |
| 🔴 **4. Requiere SpellCastHandler** | **1 / 16** | Bombas |

---

## Respuesta: ¿que porcentaje de las mecanicas de Dofus ya son data-driven?

- **62,5% (10/16) son data-driven hoy** — se crean solo con datos, sin tocar codigo.
- **+18,75% (3/16) estan "a un paso"**: el handler ya existe; solo falta mover constantes/listas
  a la BD (metadata). Con eso, **~81% serian data-driven**.
- **18,75% (3/16) requieren trabajo de motor**: 2 sistemas nuevos (IA de invocaciones, Portales) y
  1 familia atada a `SpellCastHandler` (Bombas).

> **Veredicto:** el motor ya cubre con datos puros las mecanicas "de combate base"
> (daño, cura, buff, debuff, estado, veneno, movimiento, invisibilidad, trampa).
> Lo que NO es data-driven son las mecanicas **espaciales/persistentes y de comportamiento**
> (portales, IA de invocaciones, cadenas de bombas) y un grupo intermedio que solo necesita
> **datos extra** (limite/muerte de invocaciones, timing de glifos, tablas de transformacion).

---

## Observaciones de capacidad (no son bugs)

1. **El nucleo de combate es data-driven real.** Daño elemental, %, fijo, robo de vida, buffs y
   debuffs de cualquier caracteristica, estados y venenos (DoT por `Duration > 0`) se expresan solo
   con `Effects`. Es la zona madura del motor.
2. **El salto de 62,5% a ~81% es barato.** Las 3 mecanicas "metadata" comparten el mismo patron:
   el handler existe pero hay una tabla de constantes en C# (`ChangeSkin` switch, limite de
   invocaciones, `SPELLS_GLYPH_END_TURN`). Moverlas a columnas de BD las vuelve data-driven sin
   rediseñar nada.
3. **Lo que falta es "espacio y comportamiento", no "efectos".** Portales (persistencia espacial) e
   IA de invocaciones (decision por turno) son subsistemas que el modelo Effect->Handler no cubre
   por diseño; necesitarian su propia capa.
4. **Las bombas son el unico caso atado a `SpellCastHandler` por mecanica genuina** (combo, muros,
   explosion diferida): es codigo legitimo, no atajo.

---

## Como ampliar la cobertura data-driven (capacidad, no bugs)

| Para volver data-driven | Accion de capacidad |
| --- | --- |
| Invocaciones (limite/muerte) | Columnas en BD: limite por hechizo, "kill target" (caster/summon), liberar cupo automatico |
| Glifos de fin de turno | Campo de timing en el efecto/glifo en BD en vez de `SPELLS_GLYPH_END_TURN` |
| Transformaciones (apariencia) | Tabla BD apariencia->`bonesId`/`scale` en vez del `switch` de `ChangeSkin` |
| IA de invocaciones | Nuevo subsistema de comportamiento parametrizable (objetivo, prioridad, hechizos) |
| Portales | Nuevo subsistema espacial (red de portales + reglas de traslado) |
| Bombas | Mantener `SpellCastHandler` (mecanica genuina) o crear meta-efectos de bomba |

---

### Fuentes (read-only)

- Handlers de efecto: `Game/Effects/Spells/**` (`DirectDamage`, `Heal`, `StatsBoost`,
  `SubStatsBoost`, `Summon`, `AddState`, `TrapSpawn`, `GlyphSpawn`, `Teleport`, `SwitchPosition`,
  `Invisibility`, `ChangeSkin`).
- Venenos/DoT: `DirectDamage.cs` (rama `Duration > 0` -> `DamageOverTimeBuff`).
- Transformaciones: `Effects/Spells/States/ChangeSkin.cs` (switch de apariencias).
- Glifos: `Fights/Triggers/Glyph.cs` (`SPELLS_GLYPH_END_TURN`).
- Bombas: `Game/Spells/Casts/Rogue/**`, `Actors/Fighters/BombFighter.cs`, `Fights/Bombs/BombManager`.
- Portales / IA: ausencia de subsistema dedicado (sin clases de portal ni "brain" de IA).
