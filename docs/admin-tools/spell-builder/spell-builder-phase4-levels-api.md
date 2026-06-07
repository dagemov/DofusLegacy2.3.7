# Spell Builder Phase 4 - Spell Levels API

Fecha: `2026-06-06`

## Estado inicial verificado

- Rama activa antes de implementar: `feature/spell-builder-api-migration`
- Worktree inicial: `CLEAN`
- HEAD inicial verificado: `eb76a82 docs: prepare spell builder branch for phase4`

## Objetivo cubierto

Phase 4 implementa lectura detallada de niveles y actualizacion segura de spell levels en Admin API sin tocar Angular, base de datos, cliente, Items/Sets, NPC, monstruos, glifos ni effects.

## Endpoints creados

- `GET /api/admin/v1/spells/{spellId}/levels`
- `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
- `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`

Se eligio `PATCH` en vez de `PUT` para mantener la escritura mas segura frente a diferencias entre el esquema actual de Sunshine y el esquema legacy.

## Archivos creados o modificados

### API

- `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`

### Contracts

- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelDetailDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelUpdateRequest.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelUpdateResultDto.cs`

### Application

- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminReadService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminReadRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminWriteService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Spells/ISpellsAdminWriteRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/DependencyInjection/AdminApplicationServiceCollectionExtensions.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellLevelDetailReadModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellLevelUpdateDraft.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Spells/AdminSpellLevelUpdateResultModel.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/SpellsAdminReadService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/SpellsAdminWriteService.cs`

### Infrastructure

- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/DependencyInjection/AdminInfrastructureServiceCollectionExtensions.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/ReferenceSpellCatalogReader.cs`

## Contratos de respuesta

### `GET /api/admin/v1/spells/{spellId}/levels`

Devuelve `SpellLevelDetailDto[]`.

### `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}`

Devuelve `SpellLevelDetailDto`.

### `SpellLevelDetailDto`

- `levelNumber`
- `runtimeLevelId`
- `referenceLevelId`
- `minPlayerLevel`
- `apCost`
- `minRange`
- `maxRange`
- `castInLine`
- `castInDiagonal`
- `castTestLos`
- `needFreeCell`
- `needTakenCell`
- `rangeCanBeBoosted`
- `criticalFailureEndsTurn`
- `criticalHitProbability`
- `criticalFailureProbability`
- `maxCastPerTurn`
- `maxCastPerTarget`
- `minCastInterval`
- `initialCooldown`
- `statesRequired[]`
- `statesForbidden[]`
- `hasEffects`
- `hasCriticalEffects`
- `runtimeAvailable`
- `referenceAvailable`

Los valores son merged read values:

- si existe runtime, se prioriza runtime
- si el schema activo no expone cierto dato pero la referencia sana si, se usa fallback de referencia solo para lectura

## Contrato de escritura

### `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`

Body: `SpellLevelUpdateRequest`

Campos del request:

- `apCost`
- `minRange`
- `maxRange`
- `castInLine`
- `castInDiagonal`
- `castTestLos`
- `criticalHitProbability`
- `criticalFailureProbability`
- `needFreeCell`
- `needTakenCell`
- `minCastInterval`
- `initialCooldown`
- `maxCastPerTurn`
- `maxCastPerTarget`

Respuesta: `SpellLevelUpdateResultDto`

- `spellId`
- `levelNumber`
- `writeStrategy`
- `level`
- `warnings[]`

## Campos editables soportados

### Soportados en ambos esquemas

- `AP Cost`
- `Min Range`
- `Max Range`
- `Cast In Line`
- `Line Of Sight` (`CastTestLos`)
- `Need Free Cell`
- `Critical Rate`
- `Critical Failure`
- `Cooldown` (`MinCastInterval`)
- `Max Cast Per Turn`
- `Max Cast Per Target`

### Soportados solo en el esquema actual de Sunshine

- `Cast In Diagonal`
- `Need Taken Cell`
- `Initial Cooldown`

## Campos no soportados y razon

- `Range` como campo separado
  - en Sunshine actual el dato real vive en la columna `Range` pero semantica y contractualmente se expone como `maxRange`; en legacy el equivalente ya es `MaxRange`
- `StatesRequired` y `StatesForbidden`
  - quedan read-only en Phase 4 para no mezclar esta fase con el subeditor de estados completo
- `Effects` y `CriticalEffects`
  - quedan fuera por regla explicita; se difieren a `Phase 5 - Spell Effects API`
- `RangeCanBeBoosted`
  - existe y se lee, pero no se habilito write todavia para no ampliar el alcance mas alla del set pedido para niveles
- `CriticalFailureEndsTurn`
  - existe y se lee, pero no se habilito write en esta fase por la misma razon de alcance
- `MinPlayerLevel`
  - existe y se lee, pero no se habilito write en esta fase
- `NeedFreeTrapCell`, `GlobalCooldown`, `MaxStack`, `HideEffects`, `Hidden`, `SpellBreed`
  - son datos runtime auxiliares o de comportamiento no pedidos para la fase y sin respaldo claro del editor legacy de niveles

## Estrategia exacta de `levelNumber`

### Schema legacy (`spells_templates` + `spells_levels.Id`)

- La secuencia canonica del nivel sale de `spells_templates.SpellLevelsCSV`
- `levelNumber = posicion 1-based dentro de SpellLevelsCSV`
- La escritura se resuelve de forma directa contra `spells_levels.Id`

### Schema actual Sunshine (`spells` + `spells_levels` sin `Id`)

- Sunshine no tiene `Id` por nivel en `spells_levels`
- `SpellsLoader` y `SpellManager` construyen niveles por orden nativo de filas de `spells_levels` para cada `SpellId`
- Phase 4 replica exactamente ese criterio:
  - `GET` lista las filas en el mismo orden runtime
  - `levelNumber = posicion 1-based dentro de ese orden`

## Escritura implementada o bloqueada

La escritura quedo **implementada** con dos estrategias distintas:

### Legacy

- `writeStrategy = legacy-level-id-update`
- update directo por `spells_levels.Id`

### Sunshine actual

- `writeStrategy = current-runtime-row-rewrite`
- como no existe `Id` por nivel, no se inventa ninguno
- el repositorio:
  1. hace `LOCK TABLES spells_levels WRITE`
  2. recarga las filas actuales del spell en el orden runtime
  3. reemplaza en memoria solo el nivel pedido
  4. borra las filas del spell
  5. reinsert a todas las filas preservando el orden
  6. intenta restaurar el estado original si el reinsert falla
  7. hace `UNLOCK TABLES`

Esto evita inventar columnas o ids y mantiene alineado el `levelNumber` con el runtime real de Sunshine.

## Validaciones aplicadas

Se aplicaron solo validaciones minimas de fase:

- `apCost >= 0`
- `minRange >= 0`
- `maxRange >= 0`
- `maxRange >= minRange`
- `criticalHitProbability >= 0`
- `criticalFailureProbability >= 0`
- `minCastInterval >= 0`
- `initialCooldown >= 0` cuando el request lo envia
- `maxCastPerTurn >= 0`
- `maxCastPerTarget >= 0`
- el request debe traer al menos un campo editable
- en schema legacy se rechaza editar:
  - `castInDiagonal`
  - `needTakenCell`
  - `initialCooldown`

No se endurecieron reglas adicionales para no romper compatibilidad con datos legacy existentes.

## Relacion con legacy

- La lectura detailed mantiene la base de `SpellAdminService.GetByIdAsync`, pero la separa en endpoints de levels.
- La numeracion legacy sigue `SpellLevelsCSV`.
- El set comun editable conserva los campos que el `SpellLevelEditModel` y `SpellLevelRecord` legacy realmente persistian.
- Los campos `CastInDiagonal`, `NeedTakenCell` e `InitialCooldown` se leen desde referencia/actual runtime, pero solo se habilitan en write cuando el schema activo es el actual de Sunshine.

## Limitaciones conocidas

- `Documents/spellsReferences` sigue siendo opcional; si no existe, la API funciona igual pero sin fallback/reference metadata.
- En Sunshine actual la escritura de niveles depende de un rewrite bloqueado del grupo completo de filas del spell porque no hay PK por nivel.
- Esa estrategia es apta para una operacion administrativa puntual, no para concurrencia alta.
- `GET /api/admin/v1/spells` y `GET /api/admin/v1/spells/{spellId}` no se redisenaron; solo se extendio la capa para Phase 4.
- No hay Angular editor en esta fase.

## Validacion ejecutada

Comando ejecutado:

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"
```

Resultado:

- `BUILD SUCCEEDED`
- warnings observados:
  - `NETSDK1057` por SDK preview
  - warnings existentes de Sunshine (`CA1416`, `CS0169`) fuera del scope Spell Builder
- no hubo `MSB3027` ni `MSB3021` en esta corrida
- no se ejecuto `npm run build` porque Angular no fue tocado

## Proxima fase recomendada

- `Phase 5 - Spell Effects API`
  - lectura detallada de rows de effects
  - separacion normal/critico
  - decision posterior de write de effects sin mezclarla con levels
