# P1 Fase 5 — Action types negativos (-1 / -2 / -3)

**Fecha:** 2026-06-15  
**Referencias:** `ContextRoleplayHandler` (quest dialog), Stump `NpcReplyRecord.Type` (string discriminators), DB NPC 843

## Conclusión

Los tipos **-1, -2, -3** en Sunshine **no son acciones de reply ejecutables**. Son **marcadores de rama de diálogo de quest** usados por `SendNpcDialogQuestionMessage` cuando `npc.HasQuest = true`:

| type | Uso en código Sunshine | Cuándo se aplica |
|---:|---|---|
| **-1** | Rama "objetivo no iniciado / en progreso" | `objective == null` o objetivo no terminado |
| **-2** | Rama "objetivo intermedio" | Objetivo en progreso (segunda rama quest) |
| **-3** | Rama "quest completada" | `quest.isValided == true` |

Código: `ContextRoleplayHandler.cs` líneas ~260-340 — busca `GetNpcTypes.Where(x => x == -1/-3)` para elegir mensaje alternativo.

## Tabla

| actionType | NPCs afectados | Significado probable | Handler actual | Fix recomendado |
|---:|---|---|---|---|
| **-1** | 843 (Struk toer Nhin) | Quest: objetivo pendiente / no iniciado | `QuestBranchMarker` (P1 dispatcher) — **no ReplyDispatcher** | Mantener en `npcs_replies` solo como marcador; **no** despachar al clic. P1: skip dispatch + log |
| **-2** | 843 | Quest: rama intermedia | Igual que -1 | Igual |
| **-3** | 843 | Quest: quest validada / diálogo post-quest | Usado al **abrir** diálogo, no al clicar reply | Igual |

## Problema con NPC 843

Filas en `npcs_replies`:

```
Type=-1, MessageId=3634, ReplieId=NULL
Type=-2, MessageId=3548, ReplieId=NULL
Type=-3, MessageId=3549, ReplieId=NULL
```

- `ReplieId` NULL → no son botones clicables (correcto)
- Están en `GetNpcTypes` para branching al abrir diálogo
- **Antes de P1:** si algún índice coincidía, `ReplyDispatcher` logueaba error `Cannot dispatch the npc type -1`
- **Después de P1:** `ReplyDispatcher` detecta `typeId < 0`, loguea `QuestBranch`, retorna `true` sin bloquear

## Stump vs Sunshine

| Aspecto | Stump (Emu 2.10 M-Heroe) | Sunshine |
|---|---|---|
| Reply type | String discriminator (`LearnJob`, `Teleport`, etc.) | Int `[ReplyHandler(n)]` |
| Quest branches | Criteria + separate reply classes | Int negativos en `GetNpcTypes` + lógica en handler |
| Negative types en DB | No usa int -1/-2/-3 en misma tabla | Sí, en `npcs_replies.Type` |

## Fix P1 implementado

- `ReplyDispatcher`: tipos `< 0` → skip dispatch, log `type=QuestBranch`, no cierra diálogo
- No crear handlers -1/-2/-3 separados (sería duplicar lógica quest existente)

## Pendiente P2

- Validar si -1/-2/-3 deben moverse fuera de `npcs_replies` a tabla/columna dedicada
- Auditar otros quest NPCs con mismos marcadores
