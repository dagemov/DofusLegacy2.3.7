# Fase 3 — Comparación con emulador de referencia

**Fecha:** 2026-06-15  
**Referencias consultadas:**
- Dump DB: `C:\Users\Hombr\Downloads\RollBackShushine\sunshine.sql`
- Fork Sunshine: `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\`
- Admin legacy: `legacy-reference/Rollback.Admin/`
- Servidor actual: `Sunshine net11.0/` (repo)

**Nota:** `Rollback.World` (game server original) **no está vendoreado** en el repositorio ni accesible con código fuente en las rutas documentadas. La comparación se basa en el fork Sunshine funcional, el dump compartido y el admin Rollback.

---

## Resumen ejecutivo

El servidor actual y el fork `RollBackShushine` son **idénticos** en la lógica de oficios y diálogos NPC. El dump de referencia tiene los **mismos datos incompletos** (23 `npcs_replies`, Incarnam sin replies). El problema no es una regresión de código sino **datos DB faltantes** + **gaps de notificación cliente** presentes desde el fork original.

---

## Tabla comparativa

| Comportamiento | Emulador antiguo / fork RollBackShushine | Servidor actual (Sunshine net11.0) | Diferencia | Fix |
|---|---|---|---|---|
| NPC reply type aprender oficio | **8** (`LearnJobReply`) | **8** (`LearnJobReply`) | Ninguna | — |
| NPC reply type cerrar diálogo | **0** (`EndDialogReply`) | **0** (`EndDialogReply`) | Ninguna | — |
| NPC action Talk al clickear | `ACTION_TALK = 3` | `ACTION_TALK = 3` | Ninguna | — |
| Packet hablar NPC | 5898 `NpcGenericActionRequestMessage` | 5898 | Ninguna | — |
| Packet seleccionar reply | 5616 `NpcDialogReplyMessage` | 5616 | Ninguna | — |
| Packet cerrar diálogo | 5501 → 5502 | 5501 → 5502 | Ninguna | — |
| Notificación cliente al aprender (NPC) | **No envía** job packets | **No envía** job packets | Ninguna — bug heredado | Llamar `JobHandler.SendVisibleJobDataMessage` + `JobListedUpdateMessage` |
| Notificación cliente (`.oficio`) | Envía 3 mensajes job | Envía 3 mensajes job | Ninguna | — |
| `JobListedUpdateMessage` (6016) | Definido, **nunca enviado** | Definido, **nunca enviado** | Ninguna | Implementar en LearnJob y login |
| Persistencia `CharacterJob` | `characters_jobs` en logout (`SaveJobs`) | Igual | Ninguna | Opcional: save inmediato |
| Auto-learn en login | `EnsureAutoLearnJobs` (33 jobs) | `EnsureAutoLearnJobs` (33 jobs) | Ninguna | Desactivar en prod |
| Datos Incarnam NPC replies | `DialogRepliesIdCSV` **vacío** en dump | Igual en DB live | Ninguna — dato faltante desde origen | Poblar `npcs_replies` / CSV |
| Total `npcs_replies` en dump | **23 filas** | **23 filas** | Ninguna | Importar datos oficiales |
| Único LearnJob en DB | NPC 849, reply 3189, job 2 | Igual | Ninguna | Añadir replies Incarnam 863/881/882/883 |
| Schema `npcs_replies` Rollback admin | Columna `Action` varchar (`LearnSpell`…) | Columna `Type` int (0–11) | **Schema distinto** — mismo nombre tabla | No usar admin Rollback SQL directo en Sunshine |
| Tabla `npcs_actions` | Usada por Rollback admin | Existe en DB, **ignorada** por Sunshine | Sunshine usa `npcs.ActionsIdCSV` | Migrar o deprecar |
| Tabla `jobs` | Presente en dump | Presente, **no leída** por runtime | IDs hardcodeados en `JobCommand` | Opcional: leer de `jobs` |
| Harvest / recolección | `SkillHarvest` + `jobs_harvest` | Igual | Ninguna | — |
| XP oficio al recolectar | Solo packet en level-up | Igual | Bug heredado | Enviar `JobExperienceUpdateMessage` siempre |
| Diálogo sin replies | Cierra silencioso (sin LeaveDialog) | Igual | Bug heredado | Enviar `LeaveDialogMessage` o no abrir |
| Tipos reply -1/-2/-3 | Sin handler en fork | Sin handler | Ninguna | Handler quest o fix DB |
| Cliente Dofus 2.10 protocol | Messages IDs 5616–5618, 5501–5502, 6016, 5654–5655 | Compatible | Ninguna en IDs | Completar uso de 6016 |

---

## Detalle: NPC actions en Rollback admin vs Sunshine

### Rollback admin (`legacy-reference/Rollback.Admin`)
- Tablas: `npcs_templates`, `npcs_spawns`, `npcs_actions`
- `npcs_actions.Action` = string: `Shop`, `Talk`, `LearnSpell`, etc.
- `NpcClientPublishService` publica diálogos al cliente Flash desde `MessagesCSV` / `RepliesCSV`
- **No contiene** lógica de gameplay LearnJob

### Sunshine runtime
- Tablas: `npcs`, `worlds_npcs`, `npcs_replies`
- Acciones de diálogo: `npcs_replies.Type` (int) + `ParametersCSV`
- Acciones de interacción inicial: `npcs.ActionsIdCSV` → `NpcActionTypeEnum`
- Reply handlers registrados por reflection `[ReplyHandler(n)]`

---

## Detalle: Persistencia CharacterJob

Ambas versiones (fork y actual):

```csharp
// CharacterManager.SaveJobs
DELETE FROM characters_jobs WHERE OwnerId = @OwnerId
INSERT por cada job en JobsCollection.GetJobs()
```

Trigger: `Character.LogOut()` → `CharacterManager.Save()`.

**Ninguna versión** persiste inmediatamente tras `LearnJobReply.Execute()`.

---

## Detalle: Packets enviados al cliente (oficios)

| Evento | Fork RollBackShushine | Actual | Packet IDs |
|---|---|---|---|
| Aprender por NPC | Diálogo continúa, sin job UI | Igual | — |
| Aprender por `.oficio` | JobDescription 5655, JobExperienceMulti 5809, JobCrafter 5652 | Igual | 5655, 5809, 5652 |
| Level-up harvest | JobLevelUp, JobExperienceUpdate | Igual | — |
| Lista job add/remove | No implementado | No implementado | 6016 sin usar |

---

## Conclusión

No hay divergencia significativa entre el fork funcional y el servidor actual. Los problemas reportados (NPCs Incarnam, aprender oficio, cerrar diálogo) provienen de:

1. **Datos DB incompletos** heredados del dump original (no regresión).
2. **Gaps de implementación heredados** en `LearnJobReply` (sin sync cliente).
3. **`EnsureAutoLearnJobs`** que enmascara el flujo real durante pruebas.
4. **Bug de diálogo vacío** presente en ambas versiones.

### Plan de fix unificado

| # | Acción | Tipo |
|---|---|---|
| 1 | Poblar `npcs_replies` para NPCs Incarnam 863, 881, 882, 883 | Datos DB |
| 2 | `LearnJobReply` → sync cliente + `JobListedUpdateMessage` | Código |
| 3 | Fix diálogo sin replies → `LeaveDialogMessage` | Código |
| 4 | Desactivar `EnsureAutoLearnJobs` (o flag config) | Código |
| 5 | Logging Fase 4 para validar en staging | Código |
| 6 | Import masivo replies desde datos oficiales 2.10 | Datos DB |
