# P1 Fase 9 — Validación manual

**Rama:** `feature/professions-p1-global-npc-actions-audit`  
**Fecha cierre:** 2026-06-16  
**Tester:** operador in-game  
**Resultado global:** **OK — P1 aceptado**

## Pre-requisitos

| # | Requisito | Estado |
|---|-----------|--------|
| 1 | PR #47 mergeado en `main` | **OK** |
| 2 | PR #48 rebased sin commits P0 duplicados | **OK** |
| 3 | SQL `patch_npc_849_ikul_jobs.sql` en staging | **OK** |
| 4 | Binario P1 desplegado en staging | **OK** |
| 5 | Fix CSV vs `npcs_replies` tipadas | **OK** |
| 6 | LearnJob cierra diálogo (terminal types) | **OK** |
| 7 | Logs `[NpcReplyRaw]`, `[JobSync]`, `[JobUi]`, `[JobLearn]` | **OK** |
| 8 | `JobHandler.SyncJobsOnLogin` + `NotifyJobLearned` | **OK** |

## Resultado validado in-game (2026-06-16)

| Criterio | Estado | Notas |
|----------|--------|-------|
| Panel Oficios `(J)` activo | **OK** | Deja de aparecer gris tras aprender |
| Oficios visibles en cliente | **OK** | Aparecen en panel |
| Recolección → XP oficio correcto | **OK** | Job correcto gana XP |
| Level-up visible | **OK** | Mensaje de subida de nivel |
| Persistencia al cambiar personaje/cuenta | **OK** | Oficios conservados |
| Aprender oficio vía Ikul/NPC | **OK** | Flujo Leñador validado end-to-end |
| Mensaje aprendizaje + sync packets | **OK** | Post-fix `NotifyJobLearned` |

## Decisión operativa — límite de oficios

**Aceptado temporalmente:** no restringir a 3 oficios base. Todos los oficios disponibles en servidor privado.

**Pendiente P2/P3:** rediseñar lógica visual/cliente del panel:
- 3 casillas normales + 3 especialidades, o
- modelo moderno (todos visibles como oficial), con
- cambio automático de oficio activo según actividad (campesino al trigo, sastre al craft, etc.)

No bloquea cierre P1.

## Checklist P1

| Caso | NPC | jobId / nota | Resultado esperado | Estado |
|---|---|---:|---|:---:|
| Ikul aprende oficio (Leñador validado) | 849 | 2 (reply 3185) | LearnJob + UI + persist | **OK** |
| Ikul menú multi-oficio (3184–3187) | 849 | 28/2/41/36 | Cada reply → jobId distinto (DB) | **OK** (SQL + logs) |
| Panel `(J)` activo tras aprender | — | — | No gris | **OK** |
| Recolección + XP + level-up | — | — | Oficio correcto progresa | **OK** |
| Persistencia relog / cambio cuenta | — | — | Oficios en DB + UI | **OK** |
| Hada Risette | 1223 | — | Clasificado P2 (sin patch) | **CLASIFICADO** |
| Serveuse / taberna | 889 | — | AutoClose / audit docs | **CLASIFICADO** |
| NPC misión regresión | 843 | quest 172 | type 5 QuestReply | **OK** (sin regresión reportada) |
| Mazmorra patrón | 888 | teleport | type 2 audit | **CLASIFICADO** |
| Hub mazmorra | 1248 | — | Navigate only | **CLASIFICADO** |

## Criterio aceptación P1 — cerrado

- [x] Ikul: flujo aprendizaje + UI validado in-game
- [x] Logs confirman resolución DB-first y job sync
- [x] Panel Oficios activo y persistente
- [x] Recolección / XP / level-up funcionan
- [x] Risette / taberna / mazmorra clasificados (P2)
- [x] PR #48 listo para merge

## Alcance entregado P1

- Auditoría global + diagnósticos `[NpcReplyRaw]` / `[JobSync]`
- Patch SQL Ikul (849) + restore
- Fix resolución replies DB-first vs CSV
- Comandos QA (`.mapinfo`, `.npcs`, `.jobs`, `.jobclear`, `.goto`)
- Script logs VPS `qa-npc-logs.sh`

## Fuera de alcance — P2/P3

- Parches Risette (1223), taberna (889/890), mazmorras (1248/1249)
- Handlers exchange / shop / key / party
- Reducir 128 NPCs sin replies
- UI panel 3+3 slots / oficio activo automático
- Restaurar límite estricto 3 oficios base
