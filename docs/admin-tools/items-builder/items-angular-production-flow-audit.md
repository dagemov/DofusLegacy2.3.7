# Auditoría — Flujo de producción Items/Sets desde Angular

Fecha: `2026-06-07`  
Macro: **Items/Sets Angular Production Flow** — Fase 1  
Rama activa al auditar: `feature/spell-builder-api-migration`  
Referencia probada: armas elementales `12623–12627` (pipeline + QA equip)

## Resumen ejecutivo

El Admin Angular **ya cubre** CRUD de items no-arma, diagnóstico de identidad cliente, estado de publicación **read-only**, effects editor y sets **solo lectura**.  
**No existe** flujo operador de un clic que lleve un ítem de Angular a: visible + equipable + publicado en cliente sin scripts manuales.

La lección de `12623` en Kuutar (nivel 158 < 180) demuestra que el estándar de producción debe incluir **nivel de QA**, no solo DB + D2O.

---

## Estado del repositorio al iniciar macro

| Check | Resultado |
| --- | --- |
| `git branch --show-current` | `feature/spell-builder-api-migration` |
| Worktree limpio | **NO** — cambios locales en Admin, Client2.3.7, OneLauncher, WorldServer, pipeline, docs previos |
| Acción | Fase 1 = **solo documentación**; no tocar código ajeno |

---

## 1. Angular Items Builder

### Rutas y páginas (`app.routes.ts`)

| Ruta | Componente | Estado |
| --- | --- | --- |
| `/admin/items` | `items-page` | ✅ Catálogo |
| `/admin/items/new` | `item-write-page` (create) | ✅ |
| `/admin/items/:id` | `item-detail-page` | ✅ |
| `/admin/items/:id/edit` | `item-write-page` (edit) | ✅ |
| `/admin/items/:id/duplicate` | `item-write-page` (duplicate) | ✅ |
| `/admin/items/:id/publication-status` | `item-publication-status-page` | ✅ Read-only |
| `/admin/items/icon-selector` | `item-icon-selector` | ✅ |
| `/admin/publication` | `publication-dashboard-page` | ✅ Backup/lane read-only |

### API consumida (`items.api.ts` / `items.facade.ts`)

| Endpoint | Uso Angular |
| --- | --- |
| `GET/POST/PUT /api/admin/v1/items` | CRUD |
| `GET .../items/{id}/publication-status` | Matriz publicación |
| `GET .../items/{id}/publication-manifest` | Dry-run manifest |
| `GET .../items/id-availability/{id}` | **API lista; UI no conectada** |
| `GET/PUT .../items/{id}/effects` | Editor effects (edit) |
| Client identity API | Diagnóstico i18n/D2O |

### Implementado vs faltante (Items)

| Capacidad | Estado | Evidencia |
| --- | --- | --- |
| Crear/editar item en `items` | ✅ | `ItemsAdminWriteService` → `items` |
| Crear/editar arma en `items_weapons` | ❌ | `UnsupportedWeaponTypeIds` bloquea typeIds arma |
| Persistir `description` | ❌ | Warning `DESCRIPTION_NOT_PERSISTED` en UI |
| Persistir `isVisible` | ❌ | Warning `IS_VISIBLE_NOT_PERSISTED` |
| Publicar cliente (un clic) | ❌ | UI: “Sin publicación automática al cliente real” |
| Estado publicación en español | ⚠️ Parcial | Mezcla badges EN (`PUBLISHED`, `CLIENT_KNOWN`) |
| Equip QA integrado | ❌ | No hay panel ni API |
| ID collision check en formulario | ❌ | Endpoint existe, formulario no lo usa |

---

## 2. Angular Sets Builder

| Ruta | Estado |
| --- | --- |
| `/admin/item-sets` | ✅ Lista read-only |
| `/admin/item-sets/:setId` | ✅ Detalle + bonuses decodificados |

| Capacidad | Estado |
| --- | --- |
| Crear/editar set | ❌ Sin API write ni UI |
| Publicación cliente del set | ❌ |
| Vincular item a set | ✅ Solo vía `setId` en item write (sets existentes) |

Controller: `ItemSetsAdminController.cs` — **solo GET**.

---

## 3. Backend Admin (API + Application)

### Items

| Servicio | Rol |
| --- | --- |
| `ItemsAdminReadService` | Detalle, QA summary, **publication status**, id-availability |
| `ItemsAdminWriteService` | Create/update/duplicate → tabla `items` |
| `ItemEffectsAdminService` | Effects hex round-trip |
| `ItemPublicationManifestService` | Manifest staging / blocking reasons |
| `ClientItemIdentityReadService` | Items.d2o + i18n audit |

### Publication status (`GetPublicationStatusAsync`)

Deriva:

- `visibilityState`: VISIBLE / VISIBLE_WITH_PATCH / INVISIBLE
- `publicationState`: PUBLISHED / NEEDS_CLIENT_PATCH / UNVERIFIED
- `publicationMode`: CLIENT_PUBLISHED / VISIBLE_CARRIER / SERVER_ONLY
- `dbSourceTable`: `items` vs `items_weapons` (por typeId)
- `clientD2oClass`: `Item` vs `Weapon`

**No evalúa:** equipabilidad, nivel QA, logs `[Equip]`, `data.meta` sincronizado.

### Sets

Solo `ItemSetsAdminReadService` + `ItemSetsAdminReadRepository` (`items_sets` + miembros).

---

## 4. Pipeline publicación cliente (probado 12623–12627)

Ubicación: `infrastructure/scripts/ClientItemPublicationPipeline/`

| Modo | Función |
| --- | --- |
| `stage-elemental-weapons-package` | Paquete batch i18n + manifest |
| `apply-elemental-weapons-client-patch` | Clon binario Weapon + i18n + **data.meta** |
| `d2o-inspect-ids` | Valida class/typeId/icon/criteria/apCost |
| `d2o-compare-weapons` | Diff vs template 9117/8575 |

Scripts operador (aún manuales): `Infrastructure/scripts/client/`

- `promote-elemental-weapons-client-patch.ps1`
- `deploy-elemental-weapons-parches-public.ps1`
- `rollback-elemental-weapons-client-patch.ps1`

### Hallazgos pipeline (caso real)

| Tema | Lección |
| --- | --- |
| Clase D2O | Armas = `Weapon`, no `Item`; `D2OWriter` unsafe |
| criteria | `ClearCriteria=true` en clones custom |
| `data/common/data.meta` | MD5 de `Items.d2o` obligatorio (mismo patrón que i18n) |
| `data/i18n/data.meta` | Obligatorio tras parche i18n |
| Launcher lane | Parches ZIP + `patch-validator.js` (fuera de Admin) |

Documentos de referencia:

- [custom-visible-item-publication.md](./custom-visible-item-publication.md)
- [elemental-weapons-client-publication.md](./elemental-weapons-client-publication.md)
- [elemental-weapons-equip-i18n-fix-20260607.md](./elemental-weapons-equip-i18n-fix-20260607.md)
- [server-item-criteria-audit-20260607.md](./server-item-criteria-audit-20260607.md)

---

## 5. Runtime servidor (DB + equip)

| Componente | Estado |
| --- | --- |
| `ItemManager` fusiona `items` + `items_weapons` | ✅ |
| Vendor NPC crea instancias de armas | ✅ |
| `CanEquip` | Nivel, posición, combate; **no criteria** |
| Logging `[Equip]` | ✅ `EquipAudit` desplegable en WorldServer (no expuesto en Admin) |

Evidencia equip (producción):

- `9117` en Thero (nivel 200) → `Position=1` ✅
- `12623` en Kuutar (nivel 158) → `Position=63` (inventario); fallo esperado por nivel

---

## 6. Matriz gap — objetivo vs actual

| Requisito producto | Angular | Backend API | Pipeline cliente | QA in-game |
| --- | --- | --- | --- | --- |
| Existe en DB | ✅ create/edit | ✅ | — | Manual SQL armas |
| Tabla correcta (`items` / `items_weapons`) | ⚠️ Solo `items` | ⚠️ Read detecta; write armas NO | SQL manual armas | — |
| ItemManager resuelve | — | ⚠️ Inferido por read | — | — |
| Visible en cliente | ⚠️ Diagnóstico | ✅ identity + status | Manual / scripts | Manual |
| Metadata cliente (D2O/i18n/meta) | ❌ | ❌ write | Scripts pipeline | `d2o-inspect-ids` manual |
| Equipable (nivel suficiente) | ❌ | ❌ | — | Manual |
| Logs `[Equip]` | ❌ | ❌ | — | `docker logs` manual |
| Sets producción completa | ❌ read-only | ❌ read-only | ❌ | ❌ |
| Un clic sin scripts | ❌ | ❌ | ❌ | ❌ |

---

## 7. Qué se puede cerrar rápido (por fase macro)

| Fase macro | Esfuerzo | Dependencia |
| --- | --- | --- |
| Fase 2 — Estándar documental | Bajo | Solo docs (este macro) |
| Fase 3 — UX estados ES | Medio | Angular; reutilizar `ItemPublicationStatusDto` |
| Fase 4 — Validator API | Medio | Application; invocar pipeline probes existentes |
| Fase 5 — One-click publish | Alto | Backend orchestration + pipeline + backup API |
| Fase 6 — Equip QA | Medio-alto | SSH/logs o endpoint diagnóstico; DB read |
| Fase 7 — Sets | Alto | Write API sets + D2O ItemSets + i18n |
| Fase 8 — QA final | Medio | Operador + checklist |

---

## 8. Qué requiere cada capa

### Angular

- Estados publicación 100% español (Fase 3)
- Botón publicar + resultado/rollback (Fase 5)
- Panel equip QA con diagnóstico legible (Fase 6)
- Formulario: id-availability, nivel mínimo recomendado, aviso arma → `items_weapons`
- Sets: create/edit/publication (Fase 7)

### Backend

- `ItemPublicationValidator` unificado (Fase 4)
- Orquestador publicación cliente (invocar `ClientItemPublicationPipeline` o servicio equivalente)
- Write path `items_weapons` + weapon metadata
- Equip diagnostics: leer `characters_items.Position`, opcional tail logs
- Persistencia workflow (borrador → publicado → validado)

### Cliente / publicación

- Reutilizar `D2oRawBinaryCodec`, `D2iMetaFile`, validadores existentes
- Automatizar `data/common/data.meta` + `data/i18n/data.meta`
- Integrar promote/deploy en API (hoy scripts PowerShell)
- Launcher lane: coordinación sin exponer detalle al operador

### QA in-game

- Personaje QA con nivel configurable
- Checklist: comprar/obtener → equip → position DB → logs
- Casos de referencia: Kuutar 158 (fallo esperado), Thero 200 (OK)

---

## 9. Riesgos y no mezclar

- **No mezclar** Spell Builder ni Combat (rama actual es spell-builder).
- **No abrir** parser global criteria en esta macro (ver auditoría separada).
- **No asumir** vendor/NPC sin fase explícita.
- Cambios locales sin commit previo deben **aislarse** antes de Fases 3–8.

---

## 10. Referencias de código

| Área | Path principal |
| --- | --- |
| Angular items | `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/` |
| Angular sets | `.../admin/item-sets/` |
| Items API | `RollblackLegacy.Admin.Api/Controllers/ItemsAdminController.cs` |
| Publication status | `ItemsAdminReadService.GetPublicationStatusAsync` |
| Items write | `ItemsAdminWriteService.cs` |
| Pipeline | `infrastructure/scripts/ClientItemPublicationPipeline/` |
| Equip server | `Sunshine.WorldServer/.../Inventory/Inventory.cs`, `EquipAudit.cs` |
| Handoff | `docs/handoffs/AGENT_HANDOFF.md` |
