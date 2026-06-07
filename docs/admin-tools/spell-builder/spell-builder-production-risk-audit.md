# Spell Builder Production Parity - Fase 1 - Risk Closure Audit

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`
Estado de la macro previa: `Macro 5 = PARTIAL`

## Objetivo

Auditar que falta para pasar de la migracion parcial actual a una herramienta productiva real de usuario final, donde el flujo esperado sea:

1. Abrir Spell Builder.
2. Buscar hechizo.
3. Editar niveles y effects.
4. Validar.
5. Guardar.
6. Publicar al cliente si aplica.
7. Probar en juego.

## Fuentes revisadas

- `docs/handoffs/AGENT_HANDOFF.md`
- `legacy-reference/Rollback.Web/Pages/Admin/Spells.razor`
- `legacy-reference/Rollback.Web/Components/Admin/EffectListEditor.razor`
- `legacy-reference/Rollback.Admin/Services/GameEffectEditorService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminSchemaService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellClientPublishService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/SpellEffectsDecoder.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spells-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.ts`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/World/Spells/SpellTemplate.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/SpellManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.BaseServer/Loaders/World/Spells/SpellsLoader.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/EffectManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Marks/GlyphSpawn.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Marks/TrapSpawn.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Summon/Summon.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/States/AddState.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Damages/LoseHpByUsingAP.cs`
- `infrastructure/scripts/ClientItemPublicationPipeline/Program.cs`
- `infrastructure/scripts/PublicationBackup/backup-client.ps1`
- `Client2.3.7/config.xml`
- `Client2.3.7/data/Launcher/VerInfo.rec`

## Hallazgos

### 1. Legacy si era una herramienta productiva completa

- `Rollback.Web` exponia un editor completo de hechizos con catalogo, detalle, edicion por nivel, `Effects`, `CriticalEffects`, `StatesRequired` y `StatesForbidden`.
- `Rollback.Admin` hacia round-trip real de `BinaryEffects` y `BinaryCriticalEffects` mediante `GameEffectEditorService`.
- `SpellAdminSchemaService` aplicaba el modelo editable sobre `spells_levels`, serializando effects y regenerando `StatesRequiredCSV` / `StatesForbiddenCSV`.
- `SpellAdminService` no solo guardaba runtime: tambien disparaba sincronizacion adicional para zonas persistentes y luego publicacion cliente.
- `SpellClientPublishService` publicaba hacia cliente, con backup y warnings, modificando datos de spell presentation, spell levels y textos.

### 2. El stack actual solo cubre una parte del flujo

- API actual disponible:
  - `GET /api/admin/v1/spells`
  - `GET /api/admin/v1/spells/{spellId}`
  - `GET /api/admin/v1/spells/{spellId}/levels`
  - `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
  - `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects`
  - `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
- Angular actual disponible:
  - catalogo `/admin/spells`
  - detalle `/admin/spells/:spellId`
  - edicion de campos de nivel soportados por `PATCH`
  - auditoria read-only de `effects` y `criticalEffects`
- No existe endpoint seguro de escritura para `effects` ni `criticalEffects`.
- No existe flujo actual de publicacion cliente de spells.
- No existe validacion de producto equivalente a `guardar y publicar`.

### 3. El runtime actual mezcla dos formatos de effects

- `SpellTemplate` soporta:
  - `Effects`
  - `CriticalEffects`
  - `BinaryEffect`
  - `BinaryEffects`
  - `BinaryCriticalEffect`
  - `BinaryCriticalEffects`
- `SpellManager` prioriza `Effects` / `CriticalEffects` serializados como string hex y solo usa binario como fallback.
- `EffectManager` sabe deserializar:
  - contenedor serializado actual en hex
  - formato binario legacy con `serializationId`
- El decoder del Admin actual solo lee. No existe round-trip productivo equivalente.

### 4. Hay logica de effects que no pertenece solo al formulario

- Glifos y trampas en runtime no son una fila suelta: `GlyphSpawn` y `TrapSpawn` disparan un spell enlazado usando `Effect.DiceNum` y `Effect.DiceFace`.
- Invocaciones usan handlers dedicados con validaciones de template monstruo, grado, slots, bombas y esclavos.
- Estados se aplican via handlers dedicados y `StateBuff`.
- Venenos y buffs temporales dependen de handlers y buffs del motor, no solo del payload almacenado.
- Legacy ya tenia una capa especial de sincronizacion para zonas persistentes; no era un simple `UPDATE` de blobs.

### 5. El cliente actual tiene datos de spells, pero la automatizacion actual no los cubre

- En `Client2.3.7/data/Launcher/VerInfo.rec` aparece `data/common/SpellLevels.d2o`.
- Tambien aparecen archivos `data/i18n/i18n_es.d2i`, `data/i18n/i18n_en.d2i`, `content/gfx/spells/*.swf` y `content/scripts/spellFx/*.dx`.
- Los scripts actuales de publicacion y backup en repo estan orientados a `Items.d2o`, `ItemSets.d2o`, `ItemTypes.d2o` e i18n de items.
- No se encontro un pipeline actual equivalente para spells en `infrastructure/scripts`.

### 6. La UI Angular actual no cumple la regla de producto de idioma

Se detectaron textos visibles en ingles o spanglish que no deben llegar al usuario final:

- `Spell Builder / Catalogo`
- `SpellId`, `BreedId`, `TypeId`, `TypeLabel`
- `Listado de spells`
- `Cargando spells...`
- `No se encontraron spells...`
- `Runtime OK / No`
- `Spell Builder / Detail`
- `Volver a spells`
- `Selector y editor por nivel`
- `Warnings del save`
- `RuntimeLevelId`, `ReferenceLevelId`
- `Effects`
- `Critical Effects`
- `AP Cost`
- `Min Player Level`
- `Initial Cooldown`
- `Min Cast Interval`
- `Max Cast Per Turn`
- `Max Cast Per Target`
- `Critical Rate`
- `Critical Failure`
- `Line Of Sight`
- `Cast In Line`
- `Cast In Diagonal`
- `Need Free Cell`
- `Need Taken Cell`
- `Read-only`
- `Runtime rows`
- `Reference rows`
- `Preview`
- `Name`, `Description`, `NameId`, `DescriptionId`, `LevelCount`

## Que vive en DB/runtime y que vive en cliente

| Capa | Evidencia | Estado auditado |
| --- | --- | --- |
| DB/runtime | `spells_levels`, `breeds_spells`, campos `Effects`, `CriticalEffects`, binarios fallback, estados CSV | Confirmado |
| Runtime servidor | `SpellManager`, `SpellsLoader`, `EffectManager`, handlers de combate | Confirmado |
| Cliente datos | `SpellLevels.d2o`, `i18n_*.d2i`, recursos `gfx/spells`, `spellFx` | Confirmado a nivel estructural |
| Cliente publicacion automatizada actual | Scripts solo para items | Confirmado |
| Publicacion actual de spells | Pipeline propio actual | No encontrada |

## Que se puede editar seguro hoy

- Catalogo y consulta de spells.
- Detalle read-only.
- Campos de nivel soportados por `PATCH` actual.
- Lectura de `effects` y `criticalEffects` para auditoria.

## Que no se puede editar seguro hoy

- `effects`
- `criticalEffects`
- sincronizacion de glifos / trampas persistentes
- estados ligados a semantica de effect mas alla de los CSV simples
- publicacion cliente de spells

## Que romperia cliente o servidor si se edita mal

- Reescritura incorrecta de payloads de `effects` puede romper cast, tooltips o ejecucion del runtime.
- Editar glifos o trampas como si fueran filas planas puede desalinear el spell contenedor del spell persistente real.
- Editar invocaciones sin respetar `monsterId`, grado y logica del handler puede romper combate o lanzar errores de runtime.
- Publicar cliente sin identificar primero la fuente real de datos de spells puede dejar servidor y cliente desalineados.
- Mantener textos ingles/espanol mezclados en Angular rompe el criterio de producto para usuario final.

## Que falta para un flujo simple "guardar y publicar"

1. Estrategia segura de round-trip para `effects` y `criticalEffects`.
2. Matriz de casos especiales: glifos, trampas, venenos, invocaciones, estados, buffs.
3. Contrato backend de validacion previa y escritura controlada de effects.
4. Editor Angular real de effects con UX 100% en español.
5. Estrategia actualizada de publicacion cliente para spells.
6. Backups y validaciones previas equivalentes a la pipeline de items.
7. QA con juego real despues de guardar y publicar.

## Riesgos clasificados

### Riesgos cerrables solo con documentacion o UX

- La UI Angular no esta 100% en español.
- No hay explicacion clara para el usuario final sobre que campos son seguros y cuales siguen bloqueados.
- El flujo actual no comunica todavia un "guardar y publicar" como una sola operacion de producto.

### Riesgos que requieren backend

- Falta write seguro de `effects` y `criticalEffects`.
- Falta validacion previa de consistencia entre nivel, criticos y estados.
- Falta soporte explicito para sincronizacion de payload persistente de glifos / trampas.

### Riesgos que requieren Angular

- Falta editor productivo de effects.
- Falta UX de validacion previa, confirmacion y resumen de publicacion.
- Falta migracion total de textos visibles a español.

### Riesgos que requieren serializer o round-trip

- No existe todavia un serializer productivo probado para los dos formatos soportados por runtime.
- Los formatos serializado actual y binario legacy conviven y no deben perderse por accidente.
- Los payloads especiales no deben editarse como texto ni rearmarse manualmente.

### Riesgos que requieren cliente o publicacion

- No hay pipeline actual de spells equivalente a la de items.
- Los scripts de backup actuales no incluyen `SpellLevels.d2o`.
- No esta demostrado todavia cual es la ruta minima y segura para publicar datos de spells al cliente actual.

### Riesgos que pertenecen al motor de combate y deben salir a macro separada

- Bugs o ambiguedades de ejecucion en handlers de invocaciones, bombas, glifos, trampas, estados o venenos.
- Ajustes de semantica de efectos especiales que no se resuelven desde el formulario admin.
- Cualquier correccion de logica de combate al aplicar un effect, buff o trigger.

## Decisiones recomendadas

1. No abrir edicion de `effects` hasta cerrar una estrategia de round-trip validada por backend.
2. Separar claramente dos frentes:
   - Spell Builder productivo
   - macro aparte de motor de combate para semantica especial
3. Diseñar la publicacion cliente de spells desde el cliente actual y sus artefactos reales, no copiando ciegamente la publicacion SWF legacy.
4. Tratar la migracion de idioma como requisito de producto, no como polish opcional.
5. Mantener `effects` y `criticalEffects` como buckets separados en toda la cadena.

## Que NO implementar todavia

- Editor Angular real de `effects`
- Endpoint write de `effects`
- Reescritura manual de payloads hex o binarios
- Publicacion cliente real de spells
- Modificaciones del motor de combate dentro de esta macro

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, incluyendo titulos, botones, labels, placeholders, mensajes de error, mensajes de carga, mensajes de vacio, warnings, tooltips, estados de bloqueo y textos de publicacion o confirmacion.

## Proxima fase recomendada

`Fase 2 - Effects Write Closure Spec`

Alcance recomendado:

- definir contrato backend de write de effects
- fijar reglas de round-trip por formato
- separar casos que pertenecen a motor de combate
- preparar backlog de migracion completa de textos Angular a español
