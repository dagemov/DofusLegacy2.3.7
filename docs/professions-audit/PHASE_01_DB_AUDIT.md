# Fase 1 — Auditoría de datos DB (oficios / professions)

**Fecha:** 2026-06-15  
**Base auditada:** MariaDB `sunshine` (Docker `sunshine-db`, dump de referencia `RollBackShushine/sunshine.sql`)  
**Cliente objetivo:** Dofus 2.10

## Resumen ejecutivo

La base de datos contiene las tablas necesarias para oficios y recolección, pero **los datos de diálogo de NPCs están severamente incompletos**. Solo existen **23 filas** en `npcs_replies` para **1.210 NPCs** y **1.255 spawns**. Los cuatro maestros de oficio de Incarnam (campesino, leñador, cazador, pescador) tienen mensajes de diálogo en `npcs.DialogMessagesIdCSV` pero **cero respuestas** (ni CSV ni `npcs_replies`). Esto explica por qué los NPCs de Incarnam no funcionan aunque la recolección y XP de oficio sí operan.

---

## Tablas relevantes

| Tabla | Uso en Sunshine | Problema encontrado | Ejemplo ID | Acción recomendada |
|---|---|---|---|---|
| `npcs` | Plantilla NPC: nombre, look, `DialogMessagesIdCSV`, `DialogRepliesIdCSV`, `ActionsIdCSV` | **132 NPCs** con mensajes pero sin respuestas (CSV vacío). Incarnam oficios: 863, 881, 882, 883 tienen `DialogRepliesIdCSV` vacío | NPC **881** (Bûcheron d Incarnam) | Poblar `DialogRepliesIdCSV` y/o `npcs_replies` con respuestas tipo 0 (cerrar), 1 (navegar), 8 (aprender oficio) |
| `npcs_replies` | Acciones por respuesta: `Type`, `MessageId`, `ReplieId`, `ParametersCSV`, mapa | Solo **23 filas** totales. **1** reply tipo 8 (LearnJob). **4** tipo 0 (EndDialog). Tipos **-1, -2, -3** sin handler en servidor | Reply **3189** → NPC 849, mapa 21759491, job **2** (leñador) | Importar/generar replies para todos los NPCs de oficio; eliminar o mapear tipos negativos |
| `npcs_messages` | Parámetros dinámicos de mensajes por NPC | Pocos registros; Incarnam oficios tienen entradas pero no se usan sin replies | NpcId **881**, MessageId 14443 | Mantener; depende de replies para ser útil |
| `npcs_actions` | Tabla Rollback (`Type` varchar: Shop, Talk…) | **Sunshine no la lee** — solo 1 fila `Shop`. Código usa `npcs.ActionsIdCSV` | — | Migrar datos útiles a schema Sunshine o documentar como legacy |
| `worlds_npcs` | Spawns: `Npc`, `Map`, `Cell`, `Direction` | 0 spawns huérfanos (OK) | Spawn **881** en mapa **21759493** (subarea 443) | OK; verificar replies por mapa |
| `worlds_maps` | Mapas y `SubAreaId` | Incarnam = subareas **442–450** (no 451) | Mapa **21759493**, SubAreaId **443** | Usar subareas 442–450 para queries Incarnam |
| `jobs` | Catálogo oficial: Id, Name, Specialization, Icon, ToolIdsCSV | Sunshine **no consulta** esta tabla en runtime; usa IDs numéricos hardcodeados | Job **28** = Paysan, **2** = Bûcheron | Opcional: cargar nombres desde `jobs` en comandos/UI |
| `characters_jobs` | Persistencia: `OwnerId`, `Job`, `Experience` | **0 filas** en DB local (sin personajes con oficios guardados). Migración limpia huérfanos | — | OK estructuralmente; verificar save en logout |
| `experiences` | Curva XP (`JobExp` por nivel) | Presente y usada por `ExperienceManager` | Nivel 1 JobExp | OK |
| `interactives_skills` | Skills: `ParentJob`, `GatheredRessourceItem`, craft | Datos completos para oficios base | Skill **6** (Couper, job 2, item 303) | OK |
| `worlds_interactives` | Nodos recolectables en mapa | **934** nodos en subareas Incarnam (442–450) | — | OK — explica por qué harvest funciona |
| `jobs_harvest` | XP/tiempo/loot por recurso | Datos presentes | Item harvest | OK |
| `npcs_items` | Tiendas NPC | Vendedores de recursos en Astrub/Bonta/Brâkmar tienen dialog CSV pero **0 replies** | NPC **554** (Vendeur Bûcheron d Astrub) | Misma acción que Incarnam: poblar replies o usar solo Shop action |

---

## NPCs de oficio en Incarnam (subareas 442–450)

| NPC ID | Nombre | Mapa | SubArea | DialogMessages | DialogReplies CSV | npcs_replies |
|---:|---|---:|---:|---|---|---|
| 863 | Paysan d Incarnam | 21760002 | 444 | 13 mensajes | **vacío** | **0** |
| 881 | Bûcheron d Incarnam | 21759493 | 443 | 13 mensajes | **vacío** | **0** |
| 882 | Chasseur d Incarnam | 21758977 | 445 | 14 mensajes | **vacío** | **0** |
| 883 | Pêcheur d Incarnam | 21760005 | 442 | 14 mensajes | **vacío** | **0** |

**No existen** en Incarnam: minero, alquimista, panadero (se aprenden en Astrub u otras zonas).

### NPC con LearnJob configurado (único en toda la DB)

| NPC | Nombre | Mapa | Reply | Type | ParametersCSV |
|---:|---|---:|---:|---:|---|
| 849 | Contremaître Ikul | 21759491 | 3189 | **8** | **2** (Bûcheron) |

Este NPC sí tiene `DialogRepliesIdCSV` completo y filas en `npcs_replies` (tipos 0, 1, 8).

---

## Distribución de tipos en `npcs_replies`

| Type | Cantidad | Handler Sunshine | Significado |
|---:|---:|---|---|
| 1 | 9 | *(ninguno — navegación)* | Avanzar diálogo sin acción |
| 0 | 4 | `EndDialogReply` | Cerrar diálogo |
| 8 | 1 | `LearnJobReply` | Aprender oficio |
| 2 | 2 | `TeleportReply` | Teletransporte |
| 4 | 1 | `CinematicReply` | Cinemática |
| 5 | 1 | `QuestReply` | Iniciar quest |
| 6 | 2 | `UpdateObjectiveReply` | Actualizar objetivo |
| **-1** | 1 | **NO EXISTE** | NPC 843 — error en dispatch |
| **-2** | 1 | **NO EXISTE** | NPC 843 — error en dispatch |
| **-3** | 1 | **NO EXISTE** | NPC 843 — error en dispatch |

---

## IDs de oficios (referencia código + tabla `jobs`)

| Oficio | Job ID | NPC Incarnam | Aprendizaje en DB |
|---|---:|---|---|
| Campesino (Paysan) | **28** | 863 | Sin reply tipo 8 |
| Leñador (Bûcheron) | **2** | 881 | Sin reply; solo NPC 849 en otra zona |
| Minero | **24** | — | Sin NPC Incarnam |
| Pescador (Pêcheur) | **36** | 883 | Sin reply tipo 8 |
| Alquimista | **26** | — | Vendedor Astrub 557 |
| Cazador (Chasseur) | **41** | 882 | Sin reply tipo 8 |
| Panadero (Boulanger) | **25** | — | Vendedor Astrub 561 |

---

## Problemas de integridad detectados

### 1. Replies huérfanas / inexistentes
- **132 NPCs** con `DialogMessagesIdCSV` pero sin `DialogRepliesIdCSV` ni filas en `npcs_replies`.
- Los 4 maestros de Incarnam están en este grupo.

### 2. Dialog IDs sin replies asociadas
- Mensajes definidos (ej. 3695, 3737…) sin `ReplieId` enlazado → el cliente no recibe botones.

### 3. Actions con parámetros incorrectos / tipos inválidos
- NPC **843** (Struk toer Nhin): tipos -1, -2, -3 en `npcs_replies` → `ReplyDispatcher` loguea error y **bloquea** el diálogo.

### 4. Dual schema Rollback vs Sunshine
- `npcs_actions` (Rollback) existe en DB pero **no la usa** Sunshine runtime.
- Rollback admin espera `npcs_templates` / `npcs_spawns`; Sunshine usa `npcs` / `worlds_npcs`.

### 5. `runes_effect` vs `runes_effects`
- Entidad mapea `runes_effect`; `JobManager.GetAllRunesEffects()` consulta `runes_effects` — posible fallo en runas de FM (no bloquea harvest).

### 6. Dump de referencia idéntico
- El dump `RollBackShushine/sunshine.sql` también tiene solo **23** `npcs_replies` y los mismos Incarnam NPCs sin replies. El problema es **de datos**, no de migración reciente.

---

## Queries de validación (re-ejecutables)

```sql
-- NPCs de oficio Incarnam sin replies
SELECT n.Id, n.Name, wn.Map
FROM npcs n
JOIN worlds_npcs wn ON wn.Npc = n.Id
JOIN worlds_maps m ON m.Id = wn.Map
WHERE m.SubAreaId BETWEEN 442 AND 450
  AND n.Name LIKE '%Incarnam%'
  AND (n.DialogRepliesIdCSV IS NULL OR n.DialogRepliesIdCSV = '')
  AND NOT EXISTS (SELECT 1 FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map);

-- Replies LearnJob
SELECT r.*, n.Name FROM npcs_replies r JOIN npcs n ON n.Id = r.Npc WHERE r.Type = 8;

-- Tipos sin handler
SELECT * FROM npcs_replies WHERE Type < 0 OR Type > 11;

-- NPCs con mensajes pero sin ninguna reply
SELECT COUNT(*) FROM (
  SELECT n.Id, wn.Map FROM npcs n
  JOIN worlds_npcs wn ON wn.Npc = n.Id
  WHERE n.DialogMessagesIdCSV != '' AND n.DialogMessagesIdCSV IS NOT NULL
    AND (n.DialogRepliesIdCSV IS NULL OR n.DialogRepliesIdCSV = '')
    AND NOT EXISTS (SELECT 1 FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map)
) t;
```

---

## Acciones recomendadas (prioridad)

1. **P0 — Datos Incarnam:** Generar `npcs_replies` para NPCs 863, 881, 882, 883 con flujo: saludo → "quiero aprender" (type 8, jobId correcto) → confirmación → cerrar (type 0).
2. **P0 — Replies globales:** Auditar los 132 NPCs sin replies; importar desde datos oficiales Dofus 2.10 o herramienta admin.
3. **P1 — Tipos negativos:** Corregir NPC 843 (mapear a handlers de quest o reemplazar por tipos 0/1/5/6).
4. **P1 — Vendedores Astrub:** NPCs 550–563 tienen dialog CSV vacío de replies — si son solo tienda, usar `ActionsIdCSV` shop sin diálogo talk.
5. **P2 — Schema:** Unificar `npcs_actions` legacy o eliminar tabla si no se usa.
6. **P2 — `runes_effects`:** Alinear nombre de tabla con entidad.
