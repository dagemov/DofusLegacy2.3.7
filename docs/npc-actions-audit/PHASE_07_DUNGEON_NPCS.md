# P1 Fase 7 — NPCs de mazmorra (patrón)

**Fecha:** 2026-06-15

## Patrón identificado

| Componente | Esperado | Estado actual |
|---|---|---|
| Acción principal | **type 2 TeleportReply** (`map,cell,direction`) | Hub NPCs sin `npcs_replies` type 2 |
| Handler | `TeleportReply` → `Character.Teleport` + `LeaveDialogMessage` | Implementado |
| Llave / party / quest | No validado en Sunshine TeleportReply | **No implementado** — teleport directo |
| Diálogo secundario | type 1 navegación entre páginas | CSV only en 1248/1249 |

## NPC patrón funcional: 888 Avaulé Ganymède

| Campo | Valor |
|---|---|
| mapId | 21761540 |
| dialogId | 3831 |
| replyId | 3362 |
| actionType | 2 (Teleport) |
| args | `2323,328,3` |
| Handler | TeleportReply |
| Estado | **PARTIAL OK** — tiene fila en `npcs_replies` |

## Hub mazmorras: NPC 1248 Hugo Frais

| Campo | Valor |
|---|---|
| mapId | 54534173 |
| SubAreaId | 601 |
| dialogId | 7704 |
| replies CSV | 7846, 8104, 8105, 8161, 8191, 8746 |
| npcs_replies | **0 filas** |
| Estado | **FAIL** |

### Preguntas obligatorias (1248)

| # | Pregunta | Respuesta |
|---|---|---|
| 1 | ¿Acción es teleport? | **Sí** (esperado type 2) |
| 2 | ¿Consume llave? | No en handler actual |
| 3 | ¿Valida party? | No |
| 4 | ¿Valida quest? | No en datos actuales |
| 5 | ¿Diálogo secundario? | Sí — múltiples mensajes 7704+ |
| 6 | ¿Servidor tiene handler? | Sí (TeleportReply) pero **sin datos** |
| 7 | ¿Packet cliente? | NpcDialogQuestion → Teleport → map change |

## Tabla mazmorras auditadas

| NPC | Dungeon / rol | mapId | replyId | actionType | args | Handler | Estado |
|--:|---|---:|---:|---:|---|---|---|
| 888 | Patrón teleport | 21761540 | 3362 | 2 | 2323,328,3 | TeleportReply | PARTIAL |
| 1248 | Hub 1-50 | 54534173 | 8104+ | — (CSV=1) | — | — | **FAIL** |
| 1249 | Hub 50-100 | 54535193 | 7731+ | — (CSV=1) | — | — | **FAIL** |

## Referencia `dungeons` table

| Id | Map | Parameters (teleport) | Note |
|---:|---|---|---|
| 1 | 23857152 | 23858176,422,7 | Incarnam dungeon |

**Ejemplo teleport** para una reply de Incarnam dungeon:
```sql
-- Ilustrativo — requiere mapear replyId → dungeon Id
Type=2, ParametersCSV='23858176,422,7'
```

## Acción P1

- **Documentar** patrón (este archivo)
- **No implementar** todas las mazmorras
- **P2:** patch 1248 con 1 reply teleport verificada (ej. primera mazmorra Incarnam) tras mapeo replyId↔dungeon

## Admin custom texts (Rollback.Admin)

NPC 1248 tiene textos custom 49631="Entrar a Incarnam" en `NpcClientPublishService` — IDs cliente **no coinciden** con reply IDs DB (8104, etc.). El patch debe usar IDs de `npcs.DialogRepliesIdCSV`, no IDs admin.
