# P1 Fase 4 — Contremaître Ikul (849) — Mapeo replies

**Mapa:** 21759491 | **Fecha:** 2026-06-15

## Problema

Solo existía **un** `LearnJob` (type 8):

```
MessageId=3598, ReplieId=3189, ParametersCSV=2 (Bûcheron)
```

El menú en mensaje **3597** tiene 4 opciones (3184-3187) pero todas eran **type 1** → avanzaban al mensaje **3598** → única opción learn = leñador.

## Tabla reply → oficio (corregida)

| replyId | Texto esperado (cliente) | actionType anterior | args anterior | actionType nuevo | args correcto | jobName |
|---:|---|---:|---|---:|---:|---|
| 3182 | Continuar / menú | 1 | — | 1 | — | — |
| 3183 | Cerrar | 0 | — | 0 | — | — |
| 3184 | Aprender Paysan | 1 → 8/2 | — / 2 | **8** | **28** | Paysan |
| 3185 | Aprender Bûcheron | 1 → 8/2 | — / 2 | **8** | **2** | Bûcheron |
| 3186 | Aprender Chasseur | 1 → 8/2 | — / 2 | **8** | **41** | Chasseur |
| 3187 | Aprender Pêcheur | 1 → 8/2 | — / 2 | **8** | **36** | Pêcheur |
| 3188 | Cerrar | 0 | — | 0 | — | — |
| 3189 | (legacy confirm) | 8 | 2 | **1** | — | nav only |

## Flujo post-patch

```
3596 → 3182 (nav) → 3597
3597 → 3184 (LearnJob 28) | 3185 (2) | 3186 (41) | 3187 (36) | 3188 (close)
```

Aprendizaje **directo** en 3597 — compatible con Sunshine que no ramifica por reply.

## Fix código adicional (P1, post-validación)

Además del patch SQL, el servidor necesitaba dos correcciones para que Ikul deje de comportarse como “todo leñador”:

1. **`Npc.BuildDialogsFromTemplateCsv`** — no registrar en CSV replies que ya existen tipadas en `npcs_replies` (`HasTypedReply`). El CSV del template ponía 3184–3187 como type 1 en mensaje 3596 y podía ganar en `TryGetReplyIndex` fallback.
2. **`NpcTalkAction`** — cerrar diálogo tras acciones terminales (type 8 LearnJob, 0 close, 2 teleport, 5 quest) en lugar de avanzar siempre al siguiente mensaje (3598 legacy).

Sin estos fixes, aun con SQL correcto el cliente podía seguir viendo el flujo antiguo.

## SQL

- Patch: `docs/npc-actions-audit/sql/patch_npc_849_ikul_jobs.sql`
- Restore: `docs/npc-actions-audit/sql/restore_npc_849_ikul_jobs.sql`

## Log esperado

```txt
[NpcReply] npcId=849 replyId=3184 actionType=8 actionArgs=28 handler=LearnJobReply result=Success
[NpcAction] type=LearnJob npcId=849 jobId=28 jobName=Paysan result=success
[JobLearn] charId=... jobId=28 saved=true notified=true
```
