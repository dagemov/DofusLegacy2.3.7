# Spell Builder Phase 3 - Detail API

Fecha: `2026-06-04`

## Objetivo cubierto

Phase 3 implementa la lectura de detalle de un spell para Admin API sin tocar Angular, base de datos, cliente ni publicacion.

## Endpoint creado

- `GET /api/admin/v1/spells/{spellId}`

## Contratos creados

- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellDetailDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellReferenceMetadataDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelSummaryDto.cs`

## Archivos creados o modificados

- `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminReadService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminReadRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellDetailReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellReferenceReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellLevelSummaryReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/SpellsAdminReadService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/ReferenceSpellCatalogReader.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
- `docs/admin-tools/spell-builder/spell-builder-phase3-detail-api.md`

## Contrato de respuesta

### Cabecera del spell

- `spellId`
- `name`
- `description`
- `typeId`
- `typeLabel`
- `iconId`
- `breeds[]`
  - `breedId`
  - `label`
- `levelCount`
- `runtimeAvailable`
- `referenceAvailable`

### Metadata de referencia opcional

- `reference.sourceDescription`
- `reference.name`
- `reference.description`
- `reference.nameId`
- `reference.descriptionId`
- `reference.typeId`
- `reference.typeLabel`
- `reference.iconId`
- `reference.breedIds[]`
- `reference.levelCount`

### Resumen de niveles read-only

Cada entrada de `levels[]` expone:

- `levelNumber`
- `runtimeLevelId`
- `referenceLevelId`
- `minPlayerLevel`
- `apCost`
- `minRange`
- `maxRange`
- `castInLine`
- `castTestLos`
- `needFreeCell`
- `rangeCanBeBoosted`
- `criticalFailureEndsTurn`
- `criticalHitProbability`
- `criticalFailureProbability`
- `maxCastPerTurn`
- `maxCastPerTarget`
- `minCastInterval`
- `statesRequired[]`
- `statesForbidden[]`
- `hasEffects`
- `hasCriticalEffects`
- `runtimeAvailable`
- `referenceAvailable`

## Campos soportados

- Identidad resuelta del spell usando override admin si existe y luego runtime/referencia segun disponibilidad real.
- Tipo real respaldado por `TypeId` y `TypeLabel`.
- Icono real respaldado por runtime o referencia.
- Razas/clases desde `breeds_spells` cuando existe runtime, con fallback a la referencia sana.
- Metadata de referencia opcional desde `Documents/spellsReferences`.
- Resumen read-only de niveles.
- Resumen minimo de effects normales y critical effects mediante presencia booleana por nivel.

## Campos diferidos a Phase 4 y Phase 5

### Diferido a Phase 4

- API especializada de niveles.
- Navegacion/operaciones de clonacion o edicion de niveles.
- Contratos de nivel orientados a edicion.

### Diferido a Phase 5

- Decode completo de `Effects` y `CriticalEffects`.
- Filas read-only por efecto con `EffectId`, zona, target, duracion y valores.
- Catalogo de efectos de spells.
- Cualquier editor o write API de effects.

## Campos no soportados y razon

- `sprite` o `spriteId`
  - no se encontro un campo legacy estable con ese nombre en el DTO auditado ni una equivalencia segura en el stack actual; `ScriptId` y `ScriptParams` no se mapearon como sprite para no mezclar conceptos
- `overrideName` y `overrideDescription`
  - el endpoint devuelve la identidad resuelta; los campos de override como editables siguen fuera de Phase 3
- auditoria completa runtime vs referencia
  - el port-map la considera parte del detalle final, pero no fue incluida aqui para no mezclar esta fase con la futura capa de auditoria
- decode profundo de effects
  - el detalle solo expone presencia separada de effects normales y criticos, no filas completas

## Relacion con legacy

- El endpoint porta la lectura base de `SpellAdminService.GetByIdAsync`.
- La metadata de referencia sigue el mismo origen conceptual:
  - `spells_templates.sql`
  - `spells_levels.sql`
  - `spells_types.sql`
  - `i18n_es.json`
- El resumen de niveles replica los campos visibles del `SpellLevelEditModel` auditado, pero sin habilitar edicion.
- La separacion entre `hasEffects` y `hasCriticalEffects` conserva la distincion legacy entre efectos normales y criticos sin adelantar el editor de effects.

## Compatibilidad con el esquema real del repo

Phase 3 mantiene compatibilidad con los dos shapes detectados en el repo:

- runtime actual:
  - `spells`
  - `spells_levels` sin `Id` por nivel
  - `breeds_spells` con columnas `Spell` y `Breed`
- runtime legacy:
  - `spells_templates`
  - `spells_levels` con `Id`, `BinaryEffects` y `BinaryCriticalEffects`
  - `breeds_spells` con columnas `SpellId` y `BreedId`

Para el esquema actual, el resumen de niveles sigue el orden natural de filas devuelto por `spells_levels` porque el runtime actual de Sunshine ya consume ese dataset sin `Id` explicito por nivel.

## Limitaciones conocidas

- En este entorno no se encontro `Documents/spellsReferences`, por lo que la metadata de referencia sigue siendo opcional y puede llegar nula.
- En el esquema actual `spells_levels` no existe `Id` por nivel. Por eso `runtimeLevelId` queda nulo cuando el detalle sale de `spells` + `spells_levels`.
- `hasEffects` y `hasCriticalEffects` representan presencia, no decode completo ni conteo canonico de rows.
- La auditoria rica, el fallback cliente y la publicacion no forman parte de esta fase.
- Las etiquetas de raza solo cubren las 12 razas clasicas; ids fuera de ese rango se devuelven con `label` nulo.

## Validacion ejecutada

### Validacion obligatoria

Comando ejecutado:

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"
```

Resultado:

- el cambio compilo en `RollblackLegacy.Admin.Contracts`, `RollblackLegacy.Admin.Application` y `RollblackLegacy.Admin.Infrastructure`
- el fallo final ocurrio por lock externo durante el copiado del Admin API
- errores relevantes:
  - `MSB3027`
  - `MSB3021`
  - no se pudo copiar `RollblackLegacy.Admin.Infrastructure.dll`, `RollblackLegacy.Admin.Application.dll` y `RollblackLegacy.Admin.Contracts.dll`
  - destino bloqueado por `Microsoft Visual Studio (14588), RollblackLegacy.Admin.Api (70928)`

Conclusion:

- el fallo no corresponde a un error de compilacion restante del cambio de Phase 3
- corresponde a bloqueo externo del output del Admin API en uso por Visual Studio

## Proxima fase recomendada

- `Phase 4 - Spell Levels API`
  - detalle de niveles con contrato mas especifico
  - navegacion y consistencia de niveles separadas del header
  - base de lectura previa al write API
