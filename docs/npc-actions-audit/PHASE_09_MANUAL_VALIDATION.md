# P1 Fase 9 — Validación manual

**Rama:** `feature/professions-p1-global-npc-actions-audit` (rebased sobre `main` post-#47)  
**Fecha:** 2026-06-16  
**Tester:** _(pendiente — operador in-game)_

## Pre-requisitos

| # | Requisito | Estado |
|---|-----------|--------|
| 1 | PR #47 mergeado en `main` | **OK** (2026-06-16) |
| 2 | PR #48 rebased sin commits P0 duplicados | **OK** (8 commits P1 sobre `main`) |
| 3 | SQL `patch_npc_849_ikul_jobs.sql` en staging | **OK** (VPS `sunshine-db`) |
| 4 | Binario P1 desplegado en staging | **OK** (`Sunshine.dll` + restart `sunshine-server`) |
| 5 | Fix código: CSV no pisa `npcs_replies` tipadas | **OK** (`Npc.cs` `HasTypedReply`) |
| 6 | Fix código: LearnJob cierra diálogo (no avanza a 3598) | **OK** (`NpcTalkAction` terminal types) |
| 7 | Logs activos: `[NpcReply]`, `[NpcAction]`, `[JobLearn]`, `[JobSync]`, `[JobUi]` | **OK** |

## Causa raíz del “click no hace nada” (fix `784a621+`)

1. **CSV registraba 3184–3187 como type 1 en mensaje 3596** — `HasTypedReply` solo miraba mismo `messageId`, no detectaba DB type 8 en 3597.
2. **Resolución por índice/fallback** — `IndexOf(replyId)` podía devolver la entrada CSV (type 1) antes que la DB (type 8).
3. **`LearnJobReply` fallaba en silencio** — `already_known` / `max_jobs` → `Dispatch` false → `return` sin cerrar diálogo → “no hace nada”.
4. **SQL staging OK** — verificado 2026-06-16: type 8 en 3597, sin duplicados, sin legacy nav en 3184–3187.

### Fix aplicado

- `[NpcReplyRaw]` en packet 5616 + resolución en `ChangeMessage`
- `TryResolveReply`: DB exact → DB typed por replyId → CSV → Fallback (prefiere type != 1)
- `ShouldSkipCsvReply`: omite CSV si DB tiene reply tipada (cualquier messageId)
- Fallo LearnJob cierra diálogo (no silent return)

### SQL staging (2026-06-16)

```txt
3184 @ 3597 type 8 args 28
3185 @ 3597 type 8 args 2
3186 @ 3597 type 8 args 41
3187 @ 3597 type 8 args 36
Duplicados: 0
Legacy nav 3184-3187: 0 rows
Map spawn: 21759491
```

## Checklist

| Caso | NPC | jobId / nota | Resultado esperado | Estado |
|---|---|---:|---|:---:|
| Ikul aprende campesino | 849 | 28 (reply 3184) | LearnJob + UI + persist + cierre diálogo | **PENDIENTE** |
| Ikul aprende leñador | 849 | 2 (reply 3185) | LearnJob + UI + persist + cierre diálogo | **PENDIENTE** |
| Ikul aprende cazador | 849 | 41 (reply 3186) | LearnJob + UI + persist + cierre diálogo | **PENDIENTE** |
| Ikul aprende pescador | 849 | 36 (reply 3187) | LearnJob + UI + persist + cierre diálogo | **PENDIENTE** |
| Ikul ya no fuerza leñador en todas | 849 | — | reply 3184≠3185 jobId en logs | **PENDIENTE** |
| Hada Risette responde | 1223 | — | Logs Navigate; clasificar fallo (P2) | **PENDIENTE** |
| Serveuse no bloquea | 889 | — | AutoClose limpio | **PENDIENTE** |
| NPC misión sin regresión | 843 | quest 172 | type 5 QuestReply OK | **PENDIENTE** |
| Mazmorra patrón | 888 | teleport | type 2 success log | **PENDIENTE** |
| Hub mazmorra clasificado | 1248 | — | Logs muestran Navigate only | **PENDIENTE** |
| Unhandled no bloquea | cualquier | — | `result=Unhandled` + LeaveDialog | **PENDIENTE** |

## Bug UI/mensaje (fix 2026-06-16)

| Bug | Causa | Fix |
|-----|-------|-----|
| Sin mensaje "aprendiste oficio" | `LearnJobReply` no enviaba `TextInformationMessage` | `JobHandler.NotifyJobLearned` → msg 112 + chat |
| Panel `(J)` gris | Login no enviaba job packets (`5655/5809/5652/6016`) | `JobHandler.SyncJobsOnLogin` en selección personaje |
| XP sin sync parcial | Solo `JobExperienceUpdate` en level-up | `NotifyJobExperienceChanged` en cada harvest XP |

### Logs esperados tras aprender Leñador (3185)

```txt
[JobSync] phase=learn charId=... jobIds=2 packets=5655+5809+5652+5654+6016+Text112
[JobUi] charId=... jobId=2 panelExpected=true messageSent=true
[JobLearn] ... saved=true notified=true infoMessage=true
```

### Logs esperados en login con oficio

```txt
[JobSync] phase=login charId=... jobsCount=1 jobIds=2 packets=5655+5809+5652+6016x1
```

### Ikul — por cada reply 3184–3187

```txt
[NpcReply] npcId=849 replyId=3184 actionType=8 actionArgs=28 handler=LearnJobReply result=Success
[NpcAction] type=LearnJob npcId=849 jobId=28 jobName=Paysan result=success
[JobLearn] charId=... jobId=28 saved=true notified=true
```

Repetir con jobId 2 / 41 / 36 para replies 3185 / 3186 / 3187.

### Risette / taberna / mazmorra (solo clasificación, sin patch)

```txt
[NpcReply] npcId=1223 replyId=... actionType=1 ... result=Navigate
[NpcReply] npcId=889 ... result=AutoClose
[NpcReply] npcId=1248 replyId=... actionType=1 ... result=Navigate
```

### Quest regresión (843)

```txt
[NpcAction] type=Quest npcId=843 replyId=3120 questId=172 result=started
```

## Comandos operador — revisar logs staging

```bash
ssh root@174.138.35.107
docker logs sunshine-server 2>&1 | grep -E '\[NpcReply\]|\[NpcAction\]|\[JobLearn\]' | tail -50
```

## Regresiones a vigilar

- Quest NPC 843 reply 3120 → `[NpcAction] type=Quest ... result=started`
- Diálogos sin replies → `replies=0 result=AutoClose` sin congelar cliente
- Negative types → `type=QuestBranch result=SkippedDispatch`

## Criterio aceptación P1

- [ ] Ikul: 4 oficios distintos validados in-game
- [ ] Logs confirman actionType/args/handler/result
- [ ] Misiones sin regresión
- [ ] Risette/taberna/mazmorra: evidencia de logs capturada (clasificación P2)
- [ ] PR #48 merge **solo después** de completar checklist

## Alcance explícito — NO es P1

- Parches SQL para Risette (1223), taberna (889/890), mazmorras (1248/1249)
- Handlers nuevos (exchange, shop, key, party)
- Reducir los 128 NPCs sin replies
