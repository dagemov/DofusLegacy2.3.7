# Spell Builder Phase 2 - Catalog API

Fecha: `2026-06-04`

## Objetivo cubierto

Phase 2 implementa la lectura del catalogo de spells para Admin API sin tocar Angular, base de datos, cliente ni publicacion.

## Endpoint creado

- `GET /api/admin/v1/spells`

## Contratos creados

- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellCatalogSearchRequest.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellCatalogItemDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellBreedSummaryDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellPagedResultDto.cs`

## Archivos creados o modificados

- `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminReadService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminReadRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellBreedReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellCatalogReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminPagedSpellsReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/SpellsAdminReadService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/DependencyInjection/AdminApplicationServiceCollectionExtensions.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/ReferenceSpellCatalogReader.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/DependencyInjection/AdminInfrastructureServiceCollectionExtensions.cs`
- `docs/admin-tools/spell-builder/spell-builder-phase2-catalog-api.md`

## Campos soportados por el endpoint

### Request

- `search`
  - busqueda libre sobre `SpellId`, nombre, descripcion, `TypeId`, `TypeLabel`, `IconId` y razas resueltas si existen
- `spellId`
  - filtro exacto por spell
- `breedId`
  - filtro exacto por raza/clase si existe relacion en `breeds_spells` o en la referencia legacy opcional
- `typeId`
  - filtro exacto por el discriminador real observado en legacy y en el esquema actual
- `page`
- `pageSize`

### Response por fila

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

## Campos no soportados todavia y razon

- `category`
  - no existe una categoria de spell separada en el legacy auditado; el dato real disponible es `TypeId/TypeLabel`
- `sprite`
  - no se encontro una referencia liviana y estable de sprite en el flujo legacy auditado ni en el esquema actual confirmado
- `client fallback`
  - Phase 2 no toca cliente; el catalogo no lee `Spells*.swf` ni `i18n*.swf`
- `detail`, niveles completos, estados, effects normales, effects criticos, auditoria completa y publicacion
  - fuera de alcance de Phase 2

## Relacion con legacy

- El endpoint porta el concepto de catalogo de `SpellAdminService.GetPagedAsync`.
- La busqueda mantiene el enfoque legacy de resolver un listado ligero sin cargar detalle completo del spell.
- El filtro real de clasificacion soportado por legacy es `TypeId`, no `category`.
- La relacion por raza/clase sigue `breeds_spells`, igual que el builder legacy.
- Cuando el entorno ofrece `spellsReferences`, el backend puede completar nombre, descripcion, `TypeLabel`, `IconId` y spells clasicos faltantes en runtime sin depender del cliente.

## Compatibilidad con el esquema real del repo

Durante Phase 2 se detecto una diferencia importante entre el legacy auditado y el esquema real disponible en este repo:

- legacy auditado: `spells_templates` + `spells_levels`
- dump actual del repo: `spells` + `spells_levels`

Por eso el repositorio de spells detecta en runtime cual cabecera existe:

- si existe `spells`, usa `Spell`, `Name`, `Description`, `TypeId`, `IconId`, `SpellLevelsIdsCSV`
- si existe `spells_templates`, usa el shape legacy `Id`, `TypeId`, `SpellLevelsCSV`

La misma deteccion se aplica a `breeds_spells`:

- shape legacy: `SpellId`, `BreedId`
- shape actual: `Spell`, `Breed`

Tambien se detecta de forma opcional `admin_entity_text_overrides`; si no existe, el endpoint no inventa overrides.

## Limitaciones conocidas

- En este entorno no se encontro `Documents/spellsReferences`, por lo que la metadata de referencia legacy es opcional y hoy puede no estar disponible en runtime.
- Si el esquema activo es `spells_templates` y tampoco existe `spellsReferences`, el catalogo solo puede devolver lo respaldado por runtime: `SpellId`, `TypeId`, conteo de niveles y razas si existen.
- Las etiquetas de raza resueltas humanamente solo estan cubiertas para las 12 razas clasicas. Si aparece un `breedId` fuera de ese rango, el endpoint devuelve el id y deja `label` nulo.
- `description` se devuelve tal cual este disponible. No se aplico truncado artificial porque el legacy auditado no define una longitud corta canonica.

## Validacion ejecutada

### Validacion obligatoria

Comando ejecutado:

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"
```

Resultado:

- fallo por lock externo en el copiado final del Admin API
- error relevante:
  - `MSB3027` / `MSB3021`
  - no se pudo copiar `RollblackLegacy.Admin.Application.dll`, `RollblackLegacy.Admin.Contracts.dll` y `RollblackLegacy.Admin.Infrastructure.dll`
  - destino bloqueado por `Microsoft Visual Studio (14588), RollblackLegacy.Admin.Api (70928)`

Causa probable:

- Visual Studio tenia cargado `RollblackLegacy.Admin.Api` y dejo bloqueados los DLLs de salida del proyecto API.

Conclusion de validacion obligatoria:

- el fallo no corresponde a un error de compilacion restante del cambio de Phase 2
- la ultima corrida ya compilo `RollblackLegacy.Admin.Contracts`, `RollblackLegacy.Admin.Application` y `RollblackLegacy.Admin.Infrastructure` antes de caer en el lock de copiado del API

### Validacion suplementaria no concluyente

Se intento aislar el build del proyecto API hacia `Infrastructure/temporal-artifacts/`, pero esa configuracion temporal de MSBuild produjo errores `CS0579` de atributos duplicados en proyectos referenciados. Ese intento no se tomo como validacion oficial del modulo.

## Proxima fase recomendada

- `Phase 3 - Spell Detail API`
  - detalle read-only del spell
  - runtime/reference context
  - niveles y estados solo lectura
  - base para el editor posterior
