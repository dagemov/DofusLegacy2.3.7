# Spell Builder Port Map

Fecha: `2026-06-04`

## Objetivo de este mapa

Documentar como se debe portar el Spell Builder legacy hacia el stack Angular actual sin escribir codigo en Phase 1 y sin perder paridad funcional critica.

## Premisas auditadas

- `legacy-reference` es un espejo valido del dominio spell auditado:
  - `40/40` archivos relevantes coinciden con la referencia externa `Rollback`
- El destino real es el stack:
  - `RollblackLegacy.Admin.Api`
  - `RollblackLegacy.Admin.Application`
  - `RollblackLegacy.Admin.Infrastructure`
  - `RollblackLegacy.Admin.Contracts`
  - `RollblackLegacy.Admin.Angular`
- No existe implementacion spell iniciada en el destino.

## Mapa de portabilidad UI

| Legacy source | Rol observado | Destino recomendado | Nota de port |
| --- | --- | --- | --- |
| `Rollback.Web/Pages/Admin/Spells.razor` | Pantalla completa de catalogo + editor + sidebar | `RollblackLegacy.Admin.Angular` pagina nueva `admin/spells` | No partirlo en demasiados microcomponentes en el primer port; mantener shell coherente |
| `EffectListEditor.razor` | Editor de rows de efectos | Componente Angular nuevo para spells | Reusar patrones UX del editor de items, no su DTO ni su codec |
| `ShortIdCollectionEditor.razor` | Editor de chips numericos | Componente Angular compartido o spell-specific | Ideal para `StatesRequired` y `StatesForbidden` |
| `AdminPager.razor` | Paginacion | Reusar patron de list pages Angular | Puede seguir la convencion de items |
| Sidebar de referencia/auditoria dentro de `Spells.razor` | Contexto de operador | Componentes Angular de soporte | Mantenerlo visible en la misma vista, no esconderlo en modal |

## Mapa de portabilidad backend

| Legacy source | Rol observado | Destino recomendado | Nota de port |
| --- | --- | --- | --- |
| `SpellAdminService.GetPagedAsync` | Catalogo filtrado y paginado | Application read service + API endpoint de listado | Debe seguir devolviendo audit/status/domain |
| `SpellAdminService.GetByIdAsync` | DTO completo de edicion | Application read service + API endpoint de detalle/edit | Debe incluir runtime, referencia, cliente y auditoria |
| `SpellAdminService.ExistsAsync` | Validacion de `Id` | Endpoint o validacion dedicada | Necesario para UX actual de alta/actualizacion por Id |
| `SpellAdminService.GetNextAvailableIdAsync` | Siguiente `Id` libre | Servicio write/read auxiliar | Necesario para crear spell nuevo sin asumir Id |
| `SpellAdminService.SaveRuntimeAsync` | Guardado runtime | Application write service | Debe quedar separado de la publicacion cliente para trazabilidad |
| `SpellAdminService.DeleteAsync` | Borrado con guardas | Application write service | Mantener bloqueo por referencias |
| `SpellPublishOrchestrator` | Orquestacion save + publish | Application orchestration service | Mismo concepto, version Angular/API |
| `SpellClientPublishService` | Publicacion SWF/i18n | Infrastructure service | No mezclar con repos SQL |
| `SpellAdminSchemaService` | Mapping nivel <-> record | Application/Internal mapping | Requerido para mantener shape por nivel |
| `GameEffectEditorService` | Blob <-> rows | Infrastructure/Application adapter especifico de spells | No reutilizar codec de items |
| `SpellEffectCatalogService` | Catalogo de efectos de spells | Application read service | Debe seguir siendo distinto al catalogo de items |
| `ReferenceSpellCatalogService` | Referencia sana | Infrastructure read-only service | Mantener aislado del runtime SQL |
| `ClassicSpellDomainService` | Dominio clasico | Application service | Necesario para auditoria y filtro de catalogo |

## Mapa de modelos

| Legacy model | Port recomendado |
| --- | --- |
| `SpellListItem` | Contract DTO de listado |
| `SpellEditModel` | Contract DTO de edicion completa |
| `SpellLevelEditModel` | Contract DTO de nivel editable |
| `SpellReferenceSummary` | Contract DTO de referencias bloqueantes |
| `SpellAuditSnapshot` | Contract DTO de auditoria |
| `SpellTypeOption` | Contract DTO simple de opciones |
| `GameEffectEditRow` | Contract DTO spell-effect-row |
| `GameEffectOption` | Contract DTO spell-effect-option |
| `ReferenceSpellIdentity` / `ReferenceSpellLevelSummary` | Modelos internos de infrastructure/read-only |

## Mapa de datos y persistencia

| Area | Legacy runtime | Destino Angular/API |
| --- | --- | --- |
| Cabecera spell | `spells_templates` | Repository de spells templates |
| Niveles | `spells_levels` | Repository de spell levels |
| Asignacion por raza | `breeds_spells` | Repository o subrepo de breed links |
| Referencias bloqueantes | `characters_spells`, `monsters_spells`, `npcs_replies` | Queries read-only en infrastructure |
| Overrides | `admin_entity_text_overrides` | Reusar patron existente de metadata/admin tables |
| Metadata cliente | `admin_entity_client_metadata` | Reusar patron existente de metadata/admin tables |
| Revision runtime | `admin_runtime_revisions` | Reusar mecanismo existente de revision |
| Sync persistente | `admin_spell_trigger_payload_sync` | Repository especifico de spells |

## Mapa de features que no deben copiarse 1:1 desde items

| Pieza de items actual | Motivo para no copiarla 1:1 |
| --- | --- |
| `ItemEffectsAdminService` | Trabaja con `items.Effects` en hex, no con blobs runtime de spells |
| `ItemEffectsAdminRepository` | Solo actualiza una columna plana `items.Effects` |
| `item-effects-editor.component` | No maneja niveles, criticos, estados ni payload avanzado de spells |
| `AdminEffectOptionDto` de items | Catalogo actual esta pensado para caracteristicas de items |

## Mapa de reutilizacion segura

| Pieza actual | Reutilizacion segura |
| --- | --- |
| Convencion de `Controllers` en `api/admin/v1/*` | Si |
| Separacion `Api -> Application -> Infrastructure -> Contracts -> Angular` | Si |
| Manejo de `ProblemDetails` y panel de errores | Si |
| Patron de facade/api/models Angular | Si |
| Patron de `app.routes.ts` con `new/edit/detail` | Si |
| Componentizacion UX de listas, botones y feedback | Si |

## Contratos recomendados para el futuro port

Estas rutas no existen hoy. Se listan como destino recomendado para conservar consistencia con el Admin Angular actual.

| Capacidad | Shape recomendada |
| --- | --- |
| Listado de spells | `GET /api/admin/v1/spells` |
| Detalle de edicion | `GET /api/admin/v1/spells/{spellId}/edit` |
| Opciones de tipo | `GET /api/admin/v1/spells/types` |
| Catalogo de efectos de spells | `GET /api/admin/v1/spell-effects/options` |
| Verificacion de Id | endpoint dedicado o integrado en el detalle, segun decision de implementacion |
| Crear spell | `POST /api/admin/v1/spells` |
| Actualizar spell | `PUT /api/admin/v1/spells/{spellId}` |
| Eliminar spell | `DELETE /api/admin/v1/spells/{spellId}` |

## Secuencia recomendada de migracion

### Stage 1 - Read side

- Portar listado, filtros y detalle read-only.
- Exponer auditoria, referencia sana y fallback cliente.
- No intentar guardar todavia.

### Stage 2 - Editor de runtime

- Portar cabecera editable.
- Portar editor por niveles.
- Portar estados requeridos/prohibidos.
- Portar efectos normales y criticos.

### Stage 3 - Persistencia runtime

- Portar `SaveRuntimeAsync`.
- Portar validaciones y bloqueos.
- Portar borrado con guardas de referencias.
- Portar touch de `admin_runtime_revisions`.

### Stage 4 - Publicacion cliente

- Portar `SpellClientPublishService`.
- Portar FFDec extraction/patch.
- Portar persistencia de `admin_entity_client_metadata`.

### Stage 5 - Casos especiales

- Portar sync persistente de glifos/trampas.
- Portar compatibilidad especial de `ScriptParams`/`ScriptId`.
- Revisar flujo de `special spells` en `Characters.razor` solo si se decide cubrirlo.

## Decisiones de port ya justificadas por la auditoria

| Decision | Motivo |
| --- | --- |
| Crear modulo spell nuevo en Angular | No existe base spell hoy |
| Mantener servicio de auditoria separado del CRUD | La comparacion runtime vs referencia es una feature central, no decorativa |
| Mantener publicacion cliente separada de save runtime | Reduce acoplamiento y preserva trazabilidad de fallos |
| No reutilizar codec de items | Formato de datos distinto |
| Mantener efectos normales y criticos como listas separadas | Asi funciona el runtime legacy y asi edita el operador |
| Mantener estados como subeditor dedicado | Son datos de primer orden del nivel, no metadata secundaria |

## Resultado esperado de una migracion correcta

- Un operador debe poder abrir `admin/spells`, buscar un spell, ver su auditoria, editar niveles, estados, efectos normales y criticos, guardar runtime y disparar la publicacion cliente sin depender del panel Blazor.
- Si alguna de esas piezas falta, el port queda parcial y no representa paridad real del Spell Builder legacy.
