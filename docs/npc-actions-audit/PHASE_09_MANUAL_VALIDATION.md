# P1 Fase 9 — Validación manual

**Rama:** `feature/professions-p1-global-npc-actions-audit`  
**Fecha:** 2026-06-15  
**Tester:** _(pendiente)_

## Pre-requisitos

1. Build de rama P1
2. SQL: `patch_npc_849_ikul_jobs.sql` aplicado
3. P0 patch Incarnam aún aplicado (863/881/882/883)
4. Logs activos: `[NpcReply]`, `[NpcAction]`, `[JobLearn]`

## Checklist

| Caso | NPC | jobId / nota | Resultado esperado | Estado |
|---|---|---:|---|:---:|
| Ikul aprende campesino | 849 | 28 | LearnJob + UI + persist | **PENDIENTE** |
| Ikul aprende leñador | 849 | 2 | LearnJob + UI + persist | **PENDIENTE** |
| Ikul aprende cazador | 849 | 41 | LearnJob + UI + persist | **PENDIENTE** |
| Ikul aprende pescador | 849 | 36 | LearnJob + UI + persist | **PENDIENTE** |
| Ikul ya no fuerza leñador en todas | 849 | — | reply 3184≠3185 jobId | **PENDIENTE** |
| Hada Risette responde | 1223 | — | Logs Navigate; clasificar fallo | **PENDIENTE** |
| Serveuse no bloquea | 889 | — | AutoClose limpio | **PENDIENTE** |
| NPC misión sin regresión | 843 | quest 172 | type 5 QuestReply OK | **PENDIENTE** |
| Mazmorra patrón | 888 | teleport | type 2 success log | **PENDIENTE** |
| Hub mazmorra clasificado | 1248 | — | Logs muestran Navigate only | **PENDIENTE** |
| Unhandled no bloquea | cualquier | — | `result=Unhandled` + LeaveDialog | **PENDIENTE** |

## Evidencia a capturar

Por cada prueba Ikul:

```txt
[NpcReply] npcId=849 replyId=3184 actionType=8 actionArgs=28 handler=LearnJobReply result=Success
[NpcAction] type=LearnJob npcId=849 jobId=28 jobName=Paysan result=success
```

## Regresiones a vigilar

- Quest NPC 843 reply 3120 → `[NpcAction] type=Quest ... result=started`
- Diálogos sin replies → `replies=0 result=AutoClose` sin congelar cliente
- Negative types → `type=QuestBranch result=SkippedDispatch`

## Criterio aceptación P1

Ver checklist en plan maestro — completar columna Estado tras prueba manual.
