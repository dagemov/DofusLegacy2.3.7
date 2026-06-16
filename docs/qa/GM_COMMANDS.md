# Comandos GM / QA — Sunshine WorldServer

**Uso:** prefijo `.` en chat in-game (ej. `.mapinfo`).  
**Rol mínimo QA:** `Moderator` (cuenta test con rol ≥ moderador).

---

## Logs VPS (NPC / oficios)

### Opción A — Script que guarda archivo en VPS

**Ruta en VPS:**

```txt
/opt/dofus-2.0.0-build/scripts/qa-npc-logs.sh
```

**Ejecutar en VPS:**

```bash
bash /opt/dofus-2.0.0-build/scripts/qa-npc-logs.sh
```

Imprime la ruta del archivo en `/opt/dofus-2.0.0-build/runtime-logs/qa-npc-YYYYMMDD_HHMMSS.log`.

**Desde Windows (PowerShell en el repo):**

```powershell
.\scripts\vps\qa-npc-logs.ps1
```

Recolecta las últimas 200 líneas filtradas y muestra las últimas 80.

### Opción B — SSH / PuTTY en vivo

**Con llave `.pem` (Windows):**

```powershell
ssh -i "C:\Users\Hombr\Downloads\keys\private_key_sebas.pem" root@174.138.35.107
```

**Logs en vivo:**

```bash
docker logs -f sunshine-server 2>&1 | grep -E '\[NpcReplyRaw\]|\[NpcReply\]|\[NpcAction\]|\[JobLearn\]|\[Harvest\]|\[JobXp\]'
```

**Host:** `174.138.35.107` | **Contenedor:** `sunshine-server`

---

## Comandos P1 QA (implementados)

| Comando | Rol | Estado | Descripción |
|---------|-----|--------|-------------|
| `.mapinfo` | Moderator | **NUEVO** | MapId, SubAreaId, CellId, Npcs, Monsters, Interactives |
| `.npcs` | Moderator | **NUEVO** | Lista NPCs del mapa con dialogId y repliesCount |
| `.jobs` | Moderator | **NUEVO** | Oficios actuales: jobId, nombre, level, xp |
| `.jobclear all` | Moderator | **NUEVO** | Borra todos los oficios del personaje (persiste DB) |
| `.jobclear 28` | Moderator | **NUEVO** | Borra un oficio concreto |
| `.npcdebug` | Moderator | **NUEVO** | Estado diálogo NPC activo |
| `.npcdebug on` / `.npcdebug off` | Moderator | **NUEVO** | Toggle logs verbose NPC por personaje |
| `.goto mapId cellId` | Moderator | **NUEVO** | Teleport QA (ej. `.goto 21759491 300`) |

### Ejemplo `.mapinfo`

```txt
MapId=21759491
SubAreaId=...
CellId=300
Npcs=[849:Contremaître Ikul:423]
Monsters=[...]
Interactives=[elementId:skillId:cellId,...]
```

### Ejemplo `.npcs`

```txt
npcId=849, actorId=..., name=Contremaître Ikul, cellId=423, dialogId=3596, dbReplies=11, repliesCount=...
```

### Flujo QA Ikul (P1)

```txt
.goto 21759491 300
.mapinfo
.npcs
.jobs
.jobclear all
```

Luego hablar con Ikul → click oficio → revisar logs VPS.

---

## Comandos existentes (referencia)

| Comando | Rol | Notas |
|---------|-----|-------|
| `.go mapId` | Moderator | Teleport sin celda (elige celda walkable) |
| `.oficio` | Player | Gestión oficios jugador (lista/aprender/especializar) |
| `.oficio lista` | Player | Catálogo de oficios |
| `.npc spawn\|unspawn id` | Moderator | Spawn/unspawn NPC en celda actual |
| `.monster spawn id [count] [groupId]` | Moderator | Spawn monstruo/grupo |
| `.monster unspawn id [groupId]` | Moderator | Quitar monstruo/grupo |
| `.interactives show` | Moderator | Resalta elementos interactivos en mapa |
| `.info` | Moderator | Cuentas conectadas |
| `.save` | Moderator | Guardar mundo |
| `.a mensaje` | Moderator | Anuncio servidor |
| `.debugmap` | Player | TP mapa debug fijo 2323 |

---

## Comandos NO existentes (P2/P3)

| Comando solicitado | Estado | Plan |
|--------------------|--------|------|
| `.spawnmob` | **No** | Alias propuesto → `.monster spawn` ya existe |
| `.mob` / `.mobs` | **No** | P2: listar grupos del mapa en chat |
| `.group` | **No** | P2 |
| `.spawngroup` | **Parcial** | `.monster spawn id count groupId` cubre parte |
| `.clearmobs` | **No** | P2: limpiar MonsterGroups del mapa |

### Propuesta P2 spawn QA

```txt
.mobs                          → listar groupId + monsters + cellId
.clearmobs                     → eliminar todos los grupos del mapa
.spawnmob monsterId cellId     → alias wrapper de .monster spawn
```

---

## Roles requeridos

| Rol enum | Valor típico | Comandos |
|----------|--------------|----------|
| Player | 0 | `.oficio`, `.help`, `.debugmap` |
| Moderator | 2+ | QA P1 + teleport + npc/monster |
| Administrator | 4+ | `.stop`, `.reload`, `.god`, etc. |

Si un comando responde `Unknown command`, verificar rol de la cuenta en DB (`accounts.Role`).

---

## Ikul — referencia rápida

| Campo | Valor |
|-------|-------|
| npcId | 849 |
| mapId | 21759491 |
| menu messageId | 3597 |
| replies LearnJob | 3184→28, 3185→2, 3186→41, 3187→36 |

Logs esperados tras click:

```txt
[NpcReplyRaw] clientReplyId=3184 ... source=DB ...
[NpcReply] actionType=8 actionArgs=28 handler=LearnJobReply
[NpcAction] type=LearnJob jobId=28
[JobLearn] saved=true notified=true
```
