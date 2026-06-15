# Fase 5 — Validación manual P0 (oficios Incarnam)

**Build:** `feature/professions-p0-incarnam-fix`  
**Fecha:** 2026-06-15  
**Tester:** _(pendiente — completar tras prueba manual)_

## Pre-requisitos de prueba

1. Aplicar SQL patch: `docs/professions-audit/sql/patch_incarnam_profession_replies.sql`
2. Reiniciar servidor Sunshine con build de esta rama
3. Usar personaje **sin** oficios pre-aprendidos (P0 desactiva `EnsureAutoLearnJobs`)
4. Opcional: limpiar `characters_jobs` del personaje test antes de probar

## Checklist manual (usuario)

| # | Paso | OK |
|---|---|:---:|
| 1 | Hablo con NPC de oficio | ☐ |
| 2 | El diálogo abre | ☐ |
| 3 | Puedo seleccionar “aprender oficio” (reply 3182 → 3189) | ☐ |
| 4 | La interfaz de oficio existe/aparece | ☐ |
| 5 | El oficio queda listado | ☐ |
| 6 | Puedo recolectar un recurso relacionado | ☐ |
| 7 | La recolección da XP | ☐ |
| 8 | El oficio sube XP/nivel | ☐ |
| 9 | Relogeo | ☐ |
| 10 | El oficio y XP siguen persistidos | ☐ |

## Tabla de resultados (completar tras prueba)

| Caso | NPC | jobId | Resultado | Evidencia log |
|---|---|---:|---|---|
| Aprender oficio | 863 | 28 | **PENDIENTE** | `[JobLearn] ... saved=true notified=true` |
| Ver interfaz oficio | 863 | 28 | **PENDIENTE** | packets 5655/5809/6016 |
| Recolectar recurso | recurso Incarnam (Paysan) | 28 | **PENDIENTE** | `[Harvest]` |
| Ganar XP | recurso Incarnam | 28 | **PENDIENTE** | `[JobXp]` |
| Relog persistencia | personaje test | 28 | **PENDIENTE** | `validate_character_profession_state.sql` |

### Repetir para otros NPCs Incarnam

| Caso | NPC | jobId | Resultado | Evidencia log |
|---|---|---:|---|---|
| Aprender Bûcheron | 881 | 2 | **PENDIENTE** | `[JobLearn]` |
| Aprender Chasseur | 882 | 41 | **PENDIENTE** | `[JobLearn]` |
| Aprender Pêcheur | 883 | 36 | **PENDIENTE** | `[JobLearn]` |

## Logs esperados (referencia)

```txt
[NpcDialog] charId=... npcId=863 mapId=21760002 dialogId=3695
[NpcReply] charId=... npcId=863 replyId=3182 actionType=1 args=
[NpcReply] charId=... npcId=863 replyId=3189 actionType=8 args=28
[NpcAction] type=LearnJob jobId=28 result=success
[JobLearn] charId=... jobId=28 alreadyKnown=false saved=true notified=true
[Harvest] charId=... skillId=45 resourceId=289 jobId=28 xp=...
[JobXp] charId=... jobId=28 oldXp=... newXp=... oldLevel=1 newLevel=1
```

## Diálogo roto (regresión)

| Caso | Resultado esperado | Evidencia |
|---|---|---|
| NPC sin replies (ej. antes del patch) | `replies=0 result=AutoClose` + cliente no bloqueado | `[NpcDialog]` warning |

## Validación DB post-prueba

Ejecutar `docs/professions-audit/sql/validate_character_profession_state.sql` con `@character_id` y `@job_id` del test.

## Notas para el tester

- Flujo diálogo: **Talk** → seleccionar reply **3182** (continuar) → seleccionar reply **3189** (aprender).
- Reply **3183** debe cerrar el diálogo sin bloquear.
- Si `alreadyKnown=true` en logs, el personaje ya tenía el oficio (limpiar `characters_jobs` o usar otro personaje).
