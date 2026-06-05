# Spell Builder Gap Analysis

Fecha: `2026-06-04`

## Baseline auditado

- Legacy builder presente en:
  - `Rollback.Web/Pages/Admin/Spells.razor`
  - `Rollback.Admin/*`
- Mirror local `legacy-reference`:
  - `40/40` archivos auditados de spells/game-effects coinciden con la referencia externa
- Angular/API actual:
  - `RollblackLegacy.Admin.Api`, `RollblackLegacy.Admin.Application`, `RollblackLegacy.Admin.Infrastructure`: `0` coincidencias para `spell|spells`
  - `RollblackLegacy.Admin.Angular/src/app`: `1` coincidencia, solo `spellcheck="false"` dentro de `admin/items`

## Lo que ya existe y puede reutilizarse

| Pieza actual | Ubicacion | Valor para la migracion | Limite real |
| --- | --- | --- | --- |
| Ruteo Angular de admin | `src/app/app.routes.ts` | Patron de rutas `list/detail/create/edit/duplicate` ya probado | No hay ruta de spells |
| DI por capas | `Application/DependencyInjection`, `Infrastructure/DependencyInjection` | Estructura lista para agregar un modulo nuevo | Solo registra items/publication/client-identity |
| Controladores REST de admin | `Api/Controllers` | Convencion de versionado y errores ya establecida | No hay controlador de spells |
| `ApiProblemPanelComponent` y facade/api models | Angular items | Feedback y flujo HTTP ya resueltos | Modelos actuales no sirven para spells |
| `item-effects-editor.component` | Angular items | Patron UX de listas reordenables, save in-flight y warnings | Codec y shape de datos incompatibles con spells |
| `LegacyBlazorEffectLabelRegistry` | Infra items | Ejemplo real de como portar labels desde Blazor | Solo cubre catalogo de items, no de spells |

## Brecha funcional por area

| Area | Legacy | Angular frontend hoy | API hoy | Brecha |
| --- | --- | --- | --- | --- |
| Ruta de entrada | `/admin/spells` | No existe | n/a | `FULL_GAP` |
| Menu / navegacion | enlace en `AdminLayout` | No existe | n/a | `FULL_GAP` |
| Catalogo de spells | existe | No existe | No existe | `FULL_GAP` |
| Filtros de busqueda/tipo/solo criticos | existe | No existe | No existe | `FULL_GAP` |
| Paginacion | existe | No existe | No existe | `FULL_GAP` |
| Carga por Id | existe | No existe | No existe | `FULL_GAP` |
| Verificacion de `Id` libre/existente | existe | No existe | No existe | `FULL_GAP` |
| Alta de spell nuevo | existe | No existe | No existe | `FULL_GAP` |
| Borrado con bloqueo por referencias | existe | No existe | No existe | `FULL_GAP` |
| Tipo (`TypeId`) | existe | No existe | No existe | `FULL_GAP` |
| Asignacion por razas | existe | No existe | No existe | `FULL_GAP` |
| Nombre/descripcion override | existe | No existe | No existe | `FULL_GAP` |
| Sidebar de referencia sana | existe | No existe | No existe | `FULL_GAP` |
| Sidebar de fallback cliente | existe | No existe | No existe | `FULL_GAP` |
| Auditoria runtime vs referencia | existe | No existe | No existe | `FULL_GAP` |
| Navegacion por niveles | existe | No existe | No existe | `FULL_GAP` |
| Crear/clonar/quitar nivel | existe | No existe | No existe | `FULL_GAP` |
| Estados requeridos/prohibidos | existe | No existe | No existe | `FULL_GAP` |
| Efectos normales | existe | No existe | No existe | `FULL_GAP` |
| Efectos criticos | existe | No existe | No existe | `FULL_GAP` |
| Catalogo de efectos especifico de spells | existe | No existe | No existe | `FULL_GAP` |
| Normalizacion forzada a `Dice` | existe | No existe | No existe | `FULL_GAP` |
| Guardado runtime | existe | No existe | No existe | `FULL_GAP` |
| Touch de revision runtime | existe | No existe | No existe | `FULL_GAP` |
| Sincronizacion glifo/trampa | existe | No existe | No existe | `FULL_GAP` |
| Publicacion cliente SWF/i18n | existe | No existe | No existe | `FULL_GAP` |
| Metadata cliente persistida | existe | No existe | No existe | `FULL_GAP` |

## Brecha de modelos

### Legacy requiere

| Modelo | Estado en Angular actual |
| --- | --- |
| `SpellEditModel` | No existe |
| `SpellLevelEditModel` | No existe |
| `SpellListItem` | No existe |
| `SpellReferenceSummary` | No existe |
| `SpellAuditSnapshot` | No existe |
| `ReferenceSpellIdentity` | No existe |
| `ReferenceSpellLevelSummary` | No existe |
| `GameEffectEditRow` para spells | No existe |
| `GameEffectOption` para spells | No existe |

### Angular ya tiene, pero no cubre spells

| Modelo actual | Por que no alcanza |
| --- | --- |
| `ItemEffectEditDto` | No tiene `Duration`, `TargetType`, `Shape`, `ZoneSize`, `TextValue`, `DateValue`, `Mount*`, ni separacion normal/critica |
| `ItemEffectsEditDto` | Solo maneja una lista plana de efectos y un hex string |
| `AdminEffectOptionDto` de items | Catalogo pensado para items/caracteristicas, no para el catalogo filtrado de spells |

## Brecha de serializacion

| Capa | Items Angular actual | Spell Builder legacy | Gap |
| --- | --- | --- | --- |
| Forma de almacenamiento | `items.Effects` como hex `ObjectEffect` | `spells_levels.BinaryEffects` y `BinaryCriticalEffects` como blob runtime | `INCOMPATIBLE_FORMAT` |
| Fila editable | `serializationTypeId`, `effectId`, `dice/value/min/max` | `GameEffectEditRow` con payload extendido | `MODEL_GAP` |
| Multiple listas | una sola lista | normales + criticos por nivel | `STRUCTURE_GAP` |
| Estados | no aplica | CSV requeridos/prohibidos por nivel | `FEATURE_GAP` |

## Brecha de persistencia y datos

| Dato runtime | Items Angular actual | Spells legacy |
| --- | --- | --- |
| Tabla cabecera | `items` | `spells_templates` |
| Tabla detalle | no aplica | `spells_levels` |
| Asignacion de clase | no aplica | `breeds_spells` |
| Referencias bloqueantes | no aplica | `characters_spells`, `monsters_spells`, `npcs_replies` |
| Overrides de texto | parcialmente comparable | `admin_entity_text_overrides` |
| Metadata cliente | existe para items | `admin_entity_client_metadata` para spells |
| Estado sync persistente | no aplica | `admin_spell_trigger_payload_sync` |

## Brecha de auditoria y referencia sana

| Capacidad | Legacy | Angular actual |
| --- | --- | --- |
| Cargar `spellsReferences` desde `Documents` | Si | No |
| Clasificar dominio clasico vs moderno vs soporte | Si | No |
| Comparar `TypeId` runtime vs referencia | Si | No |
| Comparar `SpellLevelsCSV` runtime vs referencia | Si | No |
| Comparar flags y estados por nivel | Si | No |
| Exponer resumen y diffs al operador | Si | No |

## Brecha de publicacion cliente

| Capacidad | Legacy | Angular actual |
| --- | --- | --- |
| Publicar `SpellLevels*.swf` | Si | No |
| Publicar `Spells*.swf` | Si | No |
| Publicar `i18n*.swf` | Si | No |
| Resolver/crear `NameId` y `DescriptionId` | Si | No |
| Mantener `spell-client-map.generated.json` | Si | No |
| Aplicar compatibilidad especial de `ScriptParams`/`ScriptId` | Si | No |
| Requiere FFDec | Si | No portado |

## Brecha de UI respecto al editor Angular de items

| Tema | Items Angular | Necesidad de spells | Conclusion |
| --- | --- | --- | --- |
| Editor de efectos | Existe | Si, pero por nivel y dual (normal/critico) | Reutilizable solo como patron UX |
| Quick picks/presets | Existe | Podria servir mas adelante | No cubre el alcance legacy actual |
| Save feedback | Existe | Si | Reutilizable |
| Errores HTTP | Existe | Si | Reutilizable |
| Side diagnostics | Existe para items | Si, pero con auditoria distinta | Rehacer para spells |
| Create/edit shell | Existe para items | Si | Reutilizable solo en arquitectura |

## Faltantes concretos en Angular frontend

- Ruta `admin/spells`
- Pantalla catalogo + editor en split view o equivalente
- Estado local por spell y por nivel
- Componente de estados requeridos/prohibidos
- Componente de efectos normales
- Componente de efectos criticos
- Visualizacion de auditoria y referencias
- UX de validacion de `Id`
- UX de crear/cargar/eliminar spell

## Faltantes concretos en API/Application/Infrastructure

- `SpellsAdminController`
- Contratos de spells
- Servicios `read`, `write`, `publish` y `audit`
- Repositorios para tablas runtime
- Port del catalogo de efectos de spells
- Port de `GameEffectEditorService` o adaptador equivalente para spells
- Port de referencia sana (`spellsReferences`)
- Port de sync glifo/trampa
- Port de publicacion cliente con FFDec

## Riesgos de una migracion incompleta

| Riesgo | Motivo |
| --- | --- |
| Falso CRUD | Si solo se porta `spells_templates`, faltan `spells_levels`, estados, criticos y sync de payload persistente |
| Corrupcion de efectos | El formato de items Angular no coincide con el blob runtime de spells |
| Drift silencioso | Sin auditoria contra referencia sana, el operador pierde contexto de paridad |
| Publicacion parcial | Guardar SQL sin publicar SWF/i18n deja cliente y runtime desalineados |
| Pseudo-paridad de UI | Reusar `item-effects-editor` 1:1 ocultaria datos obligatorios para spells |

## Conclusion de brecha

- La brecha es total en spells, tanto en frontend como en API.
- Lo existente en Angular sirve como patron de capas y UX, no como implementacion directa.
- El port correcto debe tratar a Spell Builder como un modulo nuevo, no como una extension menor del flujo de items.
