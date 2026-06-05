# Spell Builder Audit

Fecha: `2026-06-04`

## Scope

- Repo oficial auditado: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`
- Referencia legacy externa obligatoria:
  - `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\Rollback.Web`
  - `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\Rollback.Admin`
- Referencia legacy local auditada: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\legacy-reference`
- Stack Angular actual auditado:
  - Backend: `Angular-tools/Admin/RollblackLegacy.Admin.Api`
  - Frontend: `Angular-tools/Admin/RollblackLegacy.Admin.Angular`
- Solucion objetivo declarada: `Sunshine net11.0\Sunshine net11.0\Sunshine.sln`
- Fase 1 limitada a auditoria documental. Sin cambios de runtime, cliente, DB ni endpoints.

## Resumen ejecutivo

- El Spell Builder legacy existe como una pantalla Blazor completa en `Rollback.Web/Pages/Admin/Spells.razor`.
- La logica no es solo CRUD de `spells_templates` y `spells_levels`: tambien resuelve identidad desde referencia sana, audita drift runtime, sincroniza payloads persistentes de glifos/trampas y publica cambios al cliente (`SpellLevels*.swf`, `Spells*.swf`, `i18n*.swf`).
- `legacy-reference` es una copia funcional valida para este dominio:
  - se compararon `40` archivos de spells/game-effects relevantes
  - `40/40` coinciden byte a byte con la referencia externa `Rollback`
- En el stack Angular actual no existe Spell Builder:
  - busqueda literal `spell|spells` en `RollblackLegacy.Admin.Api`, `RollblackLegacy.Admin.Application` y `RollblackLegacy.Admin.Infrastructure`: `0` coincidencias
  - en Angular frontend solo aparece `spellcheck="false"` dentro de una plantilla de items; no hay rutas, componentes ni data-access de spells

## Pantallas legacy de spells

| Pantalla | Ruta | Rol | Estado |
| --- | --- | --- | --- |
| `Rollback.Web/Pages/Admin/Spells.razor` | `/admin/spells` | Catalogo, filtros, alta, edicion, borrado, auditoria y publicacion del spell | `PRIMARY_BUILDER` |
| `Rollback.Web/Pages/Admin/Characters.razor` | `/admin/characters` | Asignacion de `special spells` de staff (`Spell RollBack`, `Doom`, `Matanza`) | `RELATED_NOT_BUILDER` |

## Componentes Blazor usados por Spell Builder

| Archivo | Componente | Rol dentro del flujo |
| --- | --- | --- |
| `Rollback.Web/Pages/Admin/Spells.razor` | pagina principal | Lista, filtros, editor, sidebar de referencia, botones guardar/eliminar |
| `Rollback.Web/Components/Admin/EffectListEditor.razor` | editor reutilizable | Edita filas de efectos normales y criticos |
| `Rollback.Web/Components/Admin/ShortIdCollectionEditor.razor` | editor reutilizable | Edita `StatesRequired` y `StatesForbidden` |
| `Rollback.Web/Components/Admin/AdminPager.razor` | paginador | Navega el catalogo de spells |
| `Rollback.Web/Shared/AdminLayout.razor` | layout/nav | Expone el enlace `/admin/spells` |

## Inventario funcional de la pantalla legacy

| Area | Evidencia | Comportamiento auditado |
| --- | --- | --- |
| Catalogo y filtros | `Spells.razor` | Busca por id, nombre, tipo o icono; filtra por tipo y por spells con criticos |
| Lista lateral | `Spells.razor` | Muestra `Id`, `TypeLabel`, `LevelCount`, `DisplayIconId`, estado de auditoria y dominio |
| Alta | `CreateNewAsync` | Toma el siguiente `Id` libre de `spells_templates` y crea un nivel default |
| Carga de existente | `LoadSpellAsync` + `LoadTypedExistingSpellAsync` | Permite escribir un `Id`, verificar existencia y cargar el spell |
| Edicion de identidad admin | `OverrideName`, `OverrideDescription` | Permite forzar nombre/descripcion visibles sin tocar referencia sana |
| Tipo | `TypeId` | Editable mientras exista runtime |
| Asignacion por clase | `AssignedBreedIds` | Se edita por checkboxes y persiste en `breeds_spells` |
| Niveles | `Levels` | Navegacion por nivel, clon del ultimo nivel, remocion del ultimo nivel |
| Flags de nivel | `SpellLevelEditModel` | Edita costo PA, rango, cooldown, criticos y flags de lanzamiento |
| Estados | `StatesRequired`, `StatesForbidden` | Editor separado por chips numericos |
| Efectos normales | `activeLevel.Effects` | Persisten a `BinaryEffects` |
| Efectos criticos | `activeLevel.CriticalEffects` | Persisten a `BinaryCriticalEffects` |
| Auditoria | `Audit` | Compara runtime vs referencia sana y clasifica `aligned`, `runtime-drift`, `missing-runtime`, etc. |
| Referencias bloqueantes | `SpellReferenceSummary` | Cuenta personajes, razas, monstruos y NPCs que usan el spell |
| Borrado | `DeleteAsync` | Se bloquea si hay referencias activas |
| Guardado + publicacion | `SaveAsync` | Guarda runtime y luego publica al cliente |

## Servicios legacy usados

| Servicio | Capa | Rol |
| --- | --- | --- |
| `SpellAdminService` | `Rollback.Admin` | Lectura/escritura principal de runtime, auditoria, referencias, borrado, sincronizacion persistente |
| `SpellPublishOrchestrator` | `Rollback.Admin` | Orquesta `SaveRuntimeAsync` + `PublishAsync` |
| `SpellAdminSchemaService` | `Rollback.Admin` | Mapea `SpellLevelRecord` <-> `SpellLevelEditModel` |
| `SpellEffectCatalogService` | `Rollback.Admin` | Catalogo filtrado de efectos utiles para spells |
| `GameEffectEditorService` | `Rollback.Admin` | Serializa/deserializa blobs de efectos |
| `GameEffectDisplayService` | `Rollback.Admin` | Resuelve labels visibles de `EffectId` |
| `ReferenceSpellCatalogService` | `Rollback.Admin` | Carga referencia sana desde `spellsReferences` |
| `ClassicSpellDomainService` | `Rollback.Admin` | Define el dominio clasico validando runtime + razas + referencia |
| `AdminEntityTextOverrideService` | `Rollback.Admin` | Lee/escribe overrides de nombre y descripcion |
| `AdminEntityClientMetadataService` | `Rollback.Admin` | Guarda `NameId`, `DescriptionId` e `IconId` publicados al cliente |
| `AdminRuntimeRevisionService` | `Rollback.Admin` | Marca revision del dominio `Spells` tras commits runtime |
| `SpellClientPublishService` | `Rollback.Admin` | Publica niveles y presentacion al cliente |
| `FfdecSpellLevelScriptExtractor` | `Rollback.Admin` | Extrae scripts `SpellLevels*.swf` con FFDec |
| `ClientSpellMetadataService` | `Rollback.Admin` | Lee o reconstruye metadata cliente desde `Spells*.swf` o JSON generado |
| `ClientSpellLocalizationService` | `Rollback.Admin` | Resuelve nombre/descripcion/tipo/icono desde fallback cliente |
| `SpellClientPresentationCompatibilityService` | `Rollback.Admin` | Override de `ScriptParams`/`ScriptId` para spells legacy sensibles |
| `SpellTooltipFallbackService` | `Rollback.Admin` | Genera descripcion fallback a partir de efectos y duraciones |

## Modelos y DTOs legacy auditados

| Archivo | Modelo | Rol |
| --- | --- | --- |
| `Models/Spells/SpellEditModel.cs` | `SpellEditModel` | DTO principal del editor |
| `Models/Spells/SpellLevelEditModel.cs` | `SpellLevelEditModel` | DTO por nivel editable |
| `Models/Spells/SpellListItem.cs` | `SpellListItem` | DTO del catalogo |
| `Models/Spells/SpellReferenceSummary.cs` | `SpellReferenceSummary` | Conteos de referencias bloqueantes |
| `Models/Spells/SpellAuditSnapshot.cs` | `SpellAuditSnapshot` | Resultado de auditoria runtime vs referencia |
| `Models/Spells/SpellAuditStatus.cs` | `SpellAuditStatus` | Estado calculado del spell |
| `Models/Spells/SpellTypeOption.cs` | `SpellTypeOption` | Opcion de selector de tipo |
| `Models/Spells/ReferenceSpellIdentity.cs` | `ReferenceSpellIdentity` | Identidad cargada desde referencia sana |
| `Models/Spells/ReferenceSpellLevelSummary.cs` | `ReferenceSpellLevelSummary` | Resumen por nivel de la referencia |
| `Models/Spells/AdminClientSpellMetadata.cs` | `AdminClientSpellMetadata` | Metadata cliente persistida/reconstruida |
| `Models/Spells/AdminClientSpellText.cs` | `AdminClientSpellText` | Fallback cliente legible |
| `Models/GameEffects/GameEffectEditRow.cs` | `GameEffectEditRow` | Fila editable de efecto |
| `Models/GameEffects/GameEffectOption.cs` | `GameEffectOption` | Opcion del catalogo de efectos |
| `Models/GameEffects/EffectEditorKind.cs` | `EffectEditorKind` | Tipado del row editor |

## Tablas, archivos y accesos de datos auditados

### Runtime SQL

| Tabla | Uso en Spell Builder |
| --- | --- |
| `spells_templates` | Cabecera runtime del spell (`Id`, `TypeId`, `SpellLevelsCSV`) |
| `spells_levels` | Niveles runtime y blobs de efectos |
| `breeds_spells` | Asignacion del spell a razas clasicas |
| `characters_spells` | Conteo de referencias y nivel maximo usado |
| `monsters_spells` | Conteo de referencias y nivel maximo usado |
| `npcs_replies` | Conteo de NPCs con `Action = 'LearnSpell'` |
| `admin_entity_text_overrides` | Overrides manuales de nombre/descripcion |
| `admin_entity_client_metadata` | `NameId`, `DescriptionId`, `IconId` publicados |
| `admin_runtime_revisions` | Marca revision del dominio runtime |
| `admin_spell_trigger_payload_sync` | Estado persistido de sincronizacion de payloads de glifos/trampas |

### Referencia sana y cliente

| Fuente | Uso |
| --- | --- |
| `Documents/spellsReferences/spells_templates.sql` | Identidad y `SpellLevelsIdsCsv` de referencia |
| `Documents/spellsReferences/spells_levels.sql` | Flags y resumen por nivel de referencia |
| `Documents/spellsReferences/spells_types.sql` | Labels de tipo |
| `Documents/spellsReferences/i18n_es.json` | Nombre/descripcion de referencia |
| `client/app/data/common/Spells*.swf` | Metadata cliente y definicion de spells |
| `client/app/data/common/SpellLevels*.swf` | Publicacion de niveles cliente |
| `client/app/data/i18n/i18n*.swf` | Publicacion de textos cliente |
| `spell-client-map.generated.json` | Cache JSON reconstruido de metadata cliente |

## Campos editables

### Cabecera del spell

| Campo | Editable | Persistencia |
| --- | --- | --- |
| `Id` | Solo al crear | `spells_templates.Id` |
| `TypeId` | Si | `spells_templates.TypeId` |
| `AssignedBreedIds` | Si | `breeds_spells` |
| `OverrideName` | Si | `admin_entity_text_overrides.DisplayName` |
| `OverrideDescription` | Si | `admin_entity_text_overrides.Description` |

### Por nivel

| Campo | Editable | Persistencia |
| --- | --- | --- |
| `APCost` | Si | `spells_levels.APCost` |
| `MinPlayerLevel` | Si | `spells_levels.MinPlayerLevel` |
| `MinRange` | Si | `spells_levels.MinRange` |
| `MaxRange` | Si | `spells_levels.MaxRange` |
| `MaxCastPerTurn` | Si | `spells_levels.MaxCastPerTurn` |
| `MaxCastPerTarget` | Si | `spells_levels.MaxCastPerTarget` |
| `MinCastInterval` | Si | `spells_levels.MinCastInterval` |
| `CriticalHitProbability` | Si | `spells_levels.CriticalHitProbability` |
| `CriticalFailureProbability` | Si | `spells_levels.CriticalFailureProbability` |
| `CastInLine` | Si | `spells_levels.CastInLine` |
| `CastTestLOS` | Si | `spells_levels.CastTestLOS` |
| `NeedFreeCell` | Si | `spells_levels.NeedFreeCell` |
| `RangeCanBeBoosted` | Si | `spells_levels.RangeCanBeBoosted` |
| `CriticalFailureEndsTurn` | Si | `spells_levels.CriticalFailureEndsTurn` |
| `StatesRequired` | Si | `spells_levels.StatesRequiredCSV` |
| `StatesForbidden` | Si | `spells_levels.StatesForbiddenCSV` |
| `Effects` | Si | `spells_levels.BinaryEffects` |
| `CriticalEffects` | Si | `spells_levels.BinaryCriticalEffects` |

### Subcampos editables por fila de efecto

| Campo | Editable |
| --- | --- |
| `EffectId` | Si |
| `Random` | Si |
| `Value` | Si |
| `MinValue` | Si |
| `MaxValue` | Si |
| `Duration` | Si |
| `TargetType` | Si |
| `Shape` | Si |
| `ZoneSize` | Si |
| `TextValue` | Si cuando el kind lo permite |
| `DurationDays/Hours/Minutes` | Si cuando el kind lo permite |
| `DateValue` | Si cuando el kind lo permite |
| `MountId/Expiration/ModelId` | Si cuando el kind lo permite |

## Campos solo lectura

| Campo | Fuente |
| --- | --- |
| `Name` visible resuelto | override + referencia + fallback cliente |
| `Description` visible resuelta | override + referencia + fallback cliente |
| `ReferenceName` / `ReferenceDescription` | referencia sana |
| `ClientName` / `ClientDescription` | fallback cliente |
| `ReferenceNameId` / `ReferenceDescriptionId` | referencia sana |
| `ReferenceTypeId` / `ReferenceIconId` | referencia sana |
| `ClientIconId` | fallback cliente |
| `DisplayIconId` | identidad resuelta |
| `ReferenceLevelIdsCsv` | referencia sana |
| `RuntimeLevelIdsCsv` | runtime actual |
| `Audit.StatusLabel`, `Audit.DomainLabel`, `Audit.Summary`, diffs | auditoria |
| `SpellReferenceSummary` | conteos de referencias |
| `spells_levels.Id` mostrado por nivel | runtime existente o `nuevo` |
| Formato de efecto en spells | en practica solo `Dice` por coercion de servicio |

## Flujo de guardado legacy

1. La UI valida el `Id` digitado y, si es un spell nuevo, comprueba existencia previa en `spells_templates`.
2. La pagina llama `SpellPublishOrchestrator.SaveAndPublishAsync(_form)`.
3. `SpellAdminService.SaveRuntimeAsync` valida y normaliza:
   - `Id > 0`
   - `TypeId >= 0`
   - `RuntimeExists == true`
   - al menos un nivel
   - orden y numeracion de niveles
   - `StatesRequired` y `StatesForbidden`
   - todos los rows de spell se fuerzan a `EffectEditorKind.Dice`
4. `SaveRuntimeAsync` hace `upsert` de:
   - `spells_levels`
   - `spells_templates`
   - `breeds_spells`
   - `admin_entity_text_overrides`
5. Si hubo payloads persistentes ligados a glifos/trampas:
   - calcula un plan por nivel normal/critico
   - sincroniza payloads hacia el spell interno linkeado
   - guarda el estado en `admin_spell_trigger_payload_sync`
6. Tras commit runtime, toca `admin_runtime_revisions` para el dominio `Spells`.
7. `SpellClientPublishService.PublishAsync` publica:
   - niveles a `SpellLevels*.swf`
   - definicion de spell a `Spells*.swf`
   - textos a `i18n*.swf`
   - metadata final a `admin_entity_client_metadata`
8. La UI recarga lista, detalle y nivel seleccionado.

## Flujo de borrado legacy

1. La UI solo habilita borrar si no hay referencias bloqueantes y si existe runtime.
2. `SpellAdminService.DeleteAsync` vuelve a validar referencias reales.
3. Borra `spells_templates`.
4. Borra `spells_levels` asociados por `SpellLevelsCSV`.
5. Marca revision de runtime para `Spells`.

## Effects normales

- Existen por nivel en `SpellLevelEditModel.Effects`.
- Se serializan a `spells_levels.BinaryEffects`.
- UI usa `EffectListEditor` con:
  - alta
  - borrado
  - reorder
  - cambio de `EffectId`
  - parametros avanzados (`Duration`, `TargetType`, `Shape`, `ZoneSize`)
- Para spells, el servicio fuerza el kind final a `Dice`.

## Critical effects

- Existen por nivel en `SpellLevelEditModel.CriticalEffects`.
- Se serializan a `spells_levels.BinaryCriticalEffects`.
- La UI los edita con otra instancia de `EffectListEditor`.
- La pantalla avisa si hay efectos criticos con `CriticalHitProbability <= 0`.

## Buffs

- No se encontro una entidad, tabla, DTO ni seccion de UI separada llamada `Buff`.
- Lo que si existe:
  - `Duration` en cada `GameEffectEditRow`
  - catalogo de efectos con grupo `Boosts y debuffs`
  - generacion de tooltip fallback y publish cliente para `buffs simples`
- Conclusion auditada:
  - los buffs no son un submodulo aparte
  - quedan representados implicitamente como efectos con duracion

## Conditions

- No se encontro un campo `Conditions`, `StringCriterion`, `Criteria` ni editor de condiciones en el flujo de spells auditado.
- Conclusion auditada:
  - Spell Builder legacy no expone condiciones como feature separada

## Estados

- Existen dos colecciones por nivel:
  - `StatesRequired`
  - `StatesForbidden`
- Se editan con `ShortIdCollectionEditor`.
- Se persisten como CSV:
  - `spells_levels.StatesRequiredCSV`
  - `spells_levels.StatesForbiddenCSV`

## Glifos, trampas y payload persistente

- El editor no tiene una pantalla separada para glifos/trampas.
- La logica si existe dentro de `SpellAdminService`:
  - detecta `EffectGlyph`, `EffectGlyph402` y `EffectTrap`
  - interpreta `MinValue`/`MaxValue` como link al spell interno persistente
  - sincroniza los efectos extra al spell linkeado
- Esto es parte del guardado del spell y no un modulo independiente visible.

## Que ya existe en Angular

| Hallazgo | Estado |
| --- | --- |
| Rutas de admin para items/publication | Existe |
| Controladores API para items y efectos de items | Existe |
| Patron `Api -> Application -> Infrastructure -> Angular` | Existe |
| Feedback de errores HTTP y formularios | Existe |
| Editor Angular de efectos de items | Existe |
| Catalogo de labels de efectos portado desde Blazor para items | Existe |
| Rutas, componentes, contratos, servicios o repositorios de spells | No existe |

## Que falta en Angular

- Pantalla `/admin/spells`
- Catalogo, filtros y paginacion de spells
- Formulario de cabecera del spell
- Editor por niveles
- Editor de estados requeridos/prohibidos
- Editor de efectos normales
- Editor de efectos criticos
- Sidebar de auditoria/referencia/fallback cliente
- Acciones de crear, cargar por Id, guardar y borrar spells

## Que falta en API

- Controlador de spells
- Contratos DTO para catalogo, detalle, save, delete y auditoria
- Servicios de aplicacion de spells
- Repositorios de infraestructura para `spells_templates`, `spells_levels` y `breeds_spells`
- Port de serializacion/deserializacion de efectos de spells
- Port de auditoria de referencia sana
- Port de sincronizacion de payload persistente
- Port de publicacion cliente de spells

## Conclusiones de Phase 1

- La migracion correcta no es un CRUD simple.
- El minimo de paridad funcional real incluye:
  - runtime SQL
  - editor por niveles
  - estados
  - efectos normales y criticos
  - auditoria contra referencia sana
  - publicacion cliente
- `legacy-reference` puede usarse como espejo local confiable para el port.
- El stack Angular actual no tiene Spell Builder iniciado; solo ofrece patrones reutilizables tomados del flujo de items.
