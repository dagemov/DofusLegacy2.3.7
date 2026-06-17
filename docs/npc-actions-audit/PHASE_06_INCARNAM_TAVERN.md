# P1 Fase 6 — Taberna Incarnam

**Mapa principal:** `23592962` (SubAreaId 448)  
**Fecha:** 2026-06-15

## NPCs en taberna

| NPC | Nombre | mapId | dialogId (1º) | replies CSV | db_replies | actionTypes | Estado | Causa probable |
|--:|---|---:|---:|---:|---:|---|---|
| 1223 | Fée Risette | 23592962 | 5452 | 8 | 0 | CSV=1 only | **FAIL** | Sin `npcs_replies` tipadas; quest 661 vinculada pero sin QuestReply en replies |
| 872 | Grobid Le Vétéran Kerubim | 23592962 | 3731 | 12 | 0 | CSV=1 only | **PARTIAL** | Solo navegación; puede funcionar parcialmente |
| 889 | Serveuse de Grobid | 23592962 | 3848 | **0** | 0 | — | **FAIL** | Sin replies → AutoClose (P0) |
| 890 | Habitué de la taverne | 23592962 | 3849 | **0** | 0 | — | **FAIL** | Sin replies → AutoClose |
| 879 | Eek Mite | 23592962 | 3791 | 5 | 0 | CSV=1 only | **PARTIAL** | Nav only |
| 854 | Capitaine des Kerubims | 23592962 | 3629 | 3 | 0 | CSV=1 only | **PARTIAL** | Nav only; posible quest/teleport sin tipo |

## Fée Risette (1223) — análisis

| Campo | Valor |
|---|---|
| HasQuest | 0 (no usa branching -1/-2/-3) |
| Quest vinculada en DB | Quest 661 "Quatrième postage" (step 1223) |
| Replies CSV | 7261, 7262, 7263, 7264, 7327, 7328, 7490, 7491 |
| npcs_replies | **ninguna** |

**Síntoma reportado:** diálogo/opciones no funcionan.

**Hipótesis (requiere logs P1):**
1. Replies avanzan por mensajes pero ninguna ejecuta quest/item/teleport
2. Alguna reply esperaba type 5 (Quest) o type 7 (AddItem) no configurada
3. Quest 661 requiere estado previo del personaje

**Acción P1:** Clasificar con logs `[NpcReply] ... handler=Navigate` — **sin patch SQL** hasta evidencia de actionType esperado por reply.

## Serveuse / Habitué (889/890)

- Tienen `DialogMessagesIdCSV` pero **DialogRepliesIdCSV vacío**
- P0 envía `AutoClose` — jugador ve diálogo abrir y cerrar
- **Fix recomendado P2:** importar replies oficiales Dofus 2.10 o mínimo reply type 0 (cerrar)

## Grobid (872)

- 12 replies en CSV, 0 en `npcs_replies`
- Debería mostrar opciones y navegar (type 1)
- Si falla: verificar si replies esperan type 2/5/10 sin configurar

## Patch aplicado en P1

**Ninguno** para taberna — solo clasificación. Evidencia insuficiente para actionTypes correctos.

## Logs esperados al probar

```txt
[NpcDialog] npcId=1223 dialogId=5452
[NpcReply] npcId=1223 replyId=7261 actionType=1 handler=Navigate result=Navigate
[NpcDialog] npcId=889 dialogId=3848 replies=0 result=AutoClose
```
