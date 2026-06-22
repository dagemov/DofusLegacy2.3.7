# 09 — Vertical Slice: validación del modelo con datos reales

> **Objetivo.** No construir el grafo completo. Tomar 5 entidades (2 spells, 1 item, 1 npc,
> 1 quest) y demostrar que **las relaciones diseñadas en `00`–`08` se reconstruyen usando los
> datos reales** del emulador (BD, código C#, logs del VPS, MCP-2).
>
> **Resultado.** ✅ El modelo es válido. La cadena epistémica completa
> `Spell → Effect → Code → Fight → Finding → Bug → Test → Deploy` se reconstruye con datos
> verificables, y el grafo **expone contradicciones reales** entre la definición estática y el
> comportamiento observado — que es exactamente el conocimiento verificable que L5 debe capturar.

Artefactos ejecutables: [`prototype/nodes.jsonl`](prototype/nodes.jsonl),
[`prototype/edges.jsonl`](prototype/edges.jsonl),
[`prototype/traverse.mjs`](prototype/traverse.mjs).
Reproducir: `cd grafo_emu/prototype && node traverse.mjs`.

---

## 1. Entidades seleccionadas

| Nodo | Identidad canónica | Por qué |
|------|--------------------|---------|
| Spell **189** "La Sacrifiée" | `spell:189` | Aparece en logs reales (12 CAST en fight 1) y tiene bug conocido (BUG-001) |
| Spell **196** "Vent Empoisonné" | `spell:196` | Aparece en logs reales (59 eventos) y tiene bug conocido (BUG-002, DOT a 0) |
| Item **12116** "Coiffe du Glourséleste" | `item:12116` | Item real con vendedor y precio economía-v2 |
| NPC **1053** "Vendeur de Dofus" | `npc:1053` | Vende el item 12116 (arista `SELLS` explícita) |
| Quest **3** "La discorde végétale" | `quest:3` | 5 pasos reales, reconstruibles por doble vía |

---

## 2. Cadena objetivo vs. cadena reconstruida (Spell 189)

El usuario pidió demostrar algo como `Spell(189) → … → Deploy(abc123)`. Esto es lo que el
grafo reconstruye **con la fuente real de cada salto**:

```
Spell(189) «La Sacrifiée»
  │  HAS_LEVEL            [BD  sunshine.sql:57312 → SpellLevelsIdsCSV]
  ▼
SpellLevel(941)
  │  PARSED_EFFECT ●○○    [BD  spells_levels.Effects hex → effects-parser.js]  ⚠️ disputed
  ▼
Effect(99) «Effect_DamageFire»        ←─ lo que dice el hex estático
  ✗  CONTRADICTS ●●●      [DERIVED  estático(99) ≠ observado(182)]
  ▼
Effect(182) «Effect_Summon»           ←─ lo que REALMENTE despacha (log)
  │  HANDLED_BY           [CODE  Game/Effects/Spells/Summon/Summon.cs]
  ▼
CSharpType(Summon)

Spell(189)
  │  CAST_HANDLED_BY      [CODE  [SpellCastHandler(SacrificeDoll)] SacrifierHandler.cs:13]
  ▼
CSharpType(SacrificeHandler)   ── Execute() → Caster.InflictDamage(Health.Total, Caster)
  │  EXPLAINS             [CODE+LOG  InflictDamage ↔ DAMAGE src=tgt=378 amount=1050]
  ▼
Spell(189)
  │  OBSERVED_IN          [LOG  12× CAST caster=378 spell=189]
  ▼
Fight(1)
  │  GENERATED            [LOG  logs/fights/1.log]
  ▼
LogSequence(189@fight1)   ── CAST → DISPATCH Effect_Summon dice=116-1 → SUMMON_CREATE monster=116
  │                          → DAMAGE src=378 tgt=378 amount=1050 → SUMMON_DIE
  │  MATCHES ●●●          [MCP2  signature-matcher SIG-001]
  ▼
BugSignature(SIG-001)  ── eventos=[CAST,SUMMON_CREATE,DAMAGE,SUMMON_DIE], spell=189
  │  SIGNALS
  ▼
Bug(BUG-001) «Sacrificada autodestruccion»   [MCP2  knowledge/schema.js:47]
  │  VALIDATED_BY
  ▼
TestCase(T1)  ── {detector: SUMMON_MUERTE_INSTANTANEA, spell: 189}  [MCP2  eval-battery.js:6]
  │  GUARDED_BY ○○○
  ▼
Deployment(pending)   ⌛ sin instancia real (deploy.sqlite vacío) — ver §6
```

**Todos los saltos se reconstruyeron con datos reales.** El único nodo sin instancia es
`Deployment` (no hay deploys registrados todavía), lo cual el grafo marca honestamente con
`confidence=0` / `status=candidate` en lugar de inventarlo.

---

## 3. El hallazgo que valida el diseño: estático ≠ observado

La rebanada no solo "conecta nodos". Expone **conocimiento verificable que ninguna fuente
individual contenía**:

### 3.1 Spell 189 — el hex miente, el log no

- La BD (`spells_levels.Effects`, parseada por `effects-parser.js`) dice efecto **99 = `Effect_DamageFire`** con dados 11-110.
- El log del VPS dice: `DISPATCH spell=189 effect=Effect_Summon dice=116-1` (**efecto 182**).
- → Arista `Effect(99) ✗CONTRADICTS Effect(182)` con `confidence=0.9`, fuente `DERIVED`.

Esto confirma el **Principio 2 (`00-vision.md`): los logs son evidencia primaria.** El parser
ingenuo del blob hex está desalineado con el formato real `EffectInstance` de Dofus 2.x; el
grafo lo deja registrado en vez de propagar el dato falso.

### 3.2 Spell 196 — el DOT que no hace daño

- Definición observada: `DISPATCH Effect_DamageNeutral dice=3-0` + `DISPATCH Effect_SubIntelligence dice=50-0`.
- Comportamiento observado: `BUFF_ADD kind=DOT spell=196` seguido de `BUFF_TICK kind=DOT spell=196 amount=0`.
- → Coincide con `SIG-002` (DOT con tick a cero) → `Finding(TICK_CERO@196)` → `BUG-002`.
- El hex estático parsea efecto **138 / dados 200**, que **contradice** el `Effect_DamageNeutral (100) / dados 3-0` observado.

### 3.3 El contrato derivado es incompleto

`deriveContract(effects)` para el spell 189 produce `[CAST, DISPATCH Summon, SUMMON_CREATE]`,
pero **omite el auto-daño**. El log lo añade, así que el grafo emite
`LogSequence(189) ✗VIOLATES Contract(189)`. El bug vive precisamente en ese hueco.

---

## 4. Las otras tres entidades (item, npc, quest)

### 4.1 Item 12116 — `npc:1053 -SELLS-> item:12116`

```
Npc(1053) «Vendeur de Dofus»
  │  SELLS ●●●  {price: 9_750_000}   [BD  npcs_items:35514 (NpcId=1053, Item=12116, Price=9750000)]
  ▼
Item(12116) «Coiffe du Glourséleste»  {typeId:16, level:195}
  │  HAS_TYPE ●●○                     [BD  items.TypeId=16]
  ▼
ItemType(16)  ⚠️ el NOMBRE del tipo no está en la BD del servidor → vive en D2O del cliente
```

Demuestra una arista **explícita** (`SELLS`, FK-like en `npcs_items`) y una **frontera real**:
los nombres de `ItemType` no existen en el servidor (hallazgo de `08-identity-resolution`).
El item **no aparece en ningún log** → confirma la observación de `05-preguntas-emergentes`:
el catálogo estático es una isla sin capa L5.

### 4.2 Quest 3 — `HAS_STEP` confirmado por doble vía

```
Quest(3) «La discorde végétale»  {stepsCSV: "2,3,4,5,32"}
  │  HAS_STEP ●●●   [BD  quests.StepsCSV  ∧  quests_steps.Quest=3]   ← coincidencia bidireccional
  ├─▶ QuestStep(2)  «L arroseur arrosant»
  ├─▶ QuestStep(3)  «L epyss s enlise»
  ├─▶ QuestStep(4)  «Mais quel herbier !»   {optimalLevel:25, xp:6500}
  ├─▶ QuestStep(5)  «L aphone et la flore»
  └─▶ QuestStep(32) «Qui sème le vhan…»      {xp:15000} ──REWARDS──▶ Item(288) ⚠️ ref-only
```

La relación `Quest→HAS_STEP` se reconstruye por **dos caminos independientes** (el CSV hacia
adelante en `quests` y el back-reference `quests_steps.Quest`), lo que eleva la confianza: es
una relación implícita pero **autoconfirmada**.

### 4.3 La relación implícita type-dependent: `Quest ↔ Npc`

No existe una columna `quests.startNpcId` (FK directa). Pero el vínculo **sí es reconstruible**:
los NPCs implicados viven en `quests_objectives.ParametersCSV`, y el significado de cada
parámetro depende del `Type` (tabla `quests_objectives_types`):

```
Quest(3) «La discorde végétale»
  ├─ HAS_STEP → QuestStep(3) ─INVOLVES_NPC ●●○─▶ Npc(449) «Erty Trapchet»
  │     [BD  quests_objectives Id=3, Type=3 «Ramener à #1 : x#3 #2», params='449,306,1' → #1=NPC]
  └─ HAS_STEP → QuestStep(5) ─INVOLVES_NPC ●●○─▶ Npc(488) «Trois-Fleurs»
        [BD  quests_objectives Id=26, Type=1 «Aller voir #1», params='488' → #1=NPC]
```

El reto **no** es que la relación falte, sino que es **dependiente del tipo**: el primer token
de `ParametersCSV` es un NPC solo para `Type` 1/3/9/12 («Aller voir», «Ramener à», «Retourner
voir», «Rapporter âme à»), mientras que para `Type 4` es un **mapa**, para `Type 7` un
**monstruo** y para `Type 8` un **ítem**. Misma posición, entidad distinta según contexto:
el caso canónico de `08-identity-resolution.md`. Por eso estas aristas se emiten con confianza
media (`●●○`) y método `csv+type-resolve`, no `●●●`.

Matiz: lo anterior es `INVOLVES_NPC` (visitar / entregar). El *quest giver* literal
(`STARTS`) requiere además interpretar la lógica de diálogos (`npcs_replies`), que el slice
**no** reconstruye todavía — esa parte sí valida el hallazgo #4 de
`01-inventario-conocimiento.md` ("relaciones que solo viven en lógica de diálogo").

---

## 5. Veredicto de validación

| Criterio (del encargo) | Resultado |
|------------------------|-----------|
| 1 spell con cadena completa | ✅ Spell 189 — 8 capas hasta TestCase, Deploy marcado pendiente |
| 1 spell adicional | ✅ Spell 196 — cadena DOT→BUG-002 con evidencia `amount=0` |
| 1 item | ✅ Item 12116 — `SELLS` explícito + frontera D2O |
| 1 npc | ✅ NPC 1053 — vendedor real con precio |
| 1 quest | ✅ Quest 3 — 5 pasos por doble vía |
| Reconstruir relaciones diseñadas | ✅ 42 aristas, **0 colgantes** (auditoría del reconstructor) |
| Usar datos reales | ✅ Cada nodo/arista cita su `provenance.ref` (línea de SQL, archivo C#, línea de log, módulo MCP-2) |

**El diseño está validado.** Además, el slice **descubre conocimiento nuevo** (3 contradicciones
estático↔observado) que ninguna fuente aislada expresaba — la prueba de que el grafo no es un
índice redundante sino un productor de conocimiento verificable.

---

## 6. Límites honestos de esta rebanada

1. **Deploy sin instancia.** `deploy.sqlite` está vacío localmente; la arista `GUARDED_BY → deploy:pending`
   es estructural (confianza 0). Poblarla requiere `registrarDeploy()` / `sync-build-gate`.
2. **Hub compartido.** `fight:1` es generador de `logseq:189` y `logseq:196`; al recorrer un seed
   se "filtra" hacia el otro a través del hub. Es comportamiento correcto de grafo (no un bug),
   pero conviene tenerlo presente al leer la traza.
3. **Parser hex ingenuo.** Las aristas `PARSED_EFFECT` tienen confianza baja a propósito; el
   formato real de `EffectInstance` necesita un decodificador fiel antes de subir su confianza.
4. **Item 288 / ItemType 16** quedan como `ref-only` (identidad sin resolver del lado servidor) —
   ejemplos vivos de la cuarentena descrita en `08-identity-resolution.md`.

---

## 7. Qué NO se hizo (y por qué)

Neo4j, Memgraph, GraphQL, MCP, embeddings, RAG, vector DB: **fuera de alcance**. La rebanada se
sostiene sobre dos archivos JSONL y un script de Node sin dependencias, fiel a la recomendación
arquitectónica de `07-roadmap.md` (SQLite/JSONL ahora, diferir el motor de grafos).

## 8. Siguiente paso sugerido

Con el modelo validado, la **Fase Seed Graph** (`07-roadmap.md`) puede empezar: generalizar los
extractores del slice (hoy manuales) a los ~22k spells, ~13k items y catálogo de quests,
escribiendo a `nodes.jsonl`/`edges.jsonl` masivos con las mismas reglas de procedencia y
confianza demostradas aquí.
