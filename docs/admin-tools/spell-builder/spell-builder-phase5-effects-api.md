# Macro 5 / Phase 5 - Spell Effects API

Fecha: `2026-06-06`
Rama: `feature/spell-builder-api-migration`
Base funcional: `f81d492 feat: add spell levels api`
Estado: `DONE`

## Objetivo

Exponer lectura detallada de `effects` y `criticalEffects` por nivel de spell, con separacion explicita entre:

- runtime actual Sunshine
- runtime legacy compatible
- referencia sana de `spellsReferences` cuando exista

Esta fase no abre escritura de effects.

## Endpoint entregado

### `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects`

Devuelve:

- `effects`
  - `runtimeRows[]`
  - `referenceRows[]`
  - `runtimeWarnings[]`
  - `referenceWarnings[]`
  - `runtimeSource`
  - `referenceSource`
  - `runtimeAvailable`
  - `referenceAvailable`
- `criticalEffects`
  - misma estructura

## Contratos nuevos

- `SpellLevelEffectsDto`
- `SpellEffectCollectionDto`
- `SpellEffectRowDto`

## Campos por fila

Cada `SpellEffectRowDto` expone:

- `rowIndex`
- `effectId`
- `label`
- `protocolName`
- `group`
- `operatorMode`
- `value`
- `minValue`
- `maxValue`
- `delay`
- `random`
- `duration`
- `targetType`
- `zoneShape`
- `zoneMinSize`
- `zoneSize`
- `previewText`

## Estrategia de lectura

### Schema actual Sunshine

Orden de resolucion alineado con `SpellManager`:

1. `Effects` / `CriticalEffects` serializados en hex
2. fallback binario `BinaryEffect` / `BinaryEffects`
3. fallback binario `BinaryCriticalEffect` / `BinaryCriticalEffects`

Importante:

- si el payload serializado existe, incluso `0000`, tiene prioridad sobre el binario
- el orden de niveles sigue el orden runtime consumido por `SpellsLoader` y `SpellManager`

### Schema legacy

Se leen:

- `BinaryEffects`
- `BinaryCriticalEffects`

El orden de niveles sigue `SpellLevelsCSV` del header legacy.

### Referencia sana

`ReferenceSpellCatalogReader` ahora conserva:

- `EffectsPayload`
- `CriticalEffectsPayload`

desde `spells_levels.sql` de `Documents/spellsReferences`, cuando esa referencia esta disponible en el entorno.

## Decoder implementado

### Payload serializado actual

Se decodifica el formato hex que usa Sunshine runtime:

- contador `short`
- filas con `effectId`, `diceNum`, `diceFace`, `value`, `delay`, `duration`, `targetType`, `zone`, flags extra

Resultado:

- filas `Dice` o `Integer` segun contenido y heuristica legacy de labels
- warnings si el payload no puede leerse completo

### Payload binario legacy

Se soportan los `serializationId` observados en spell builder legacy:

- `1` -> `Base`
- `4` -> `Dice`
- `6` -> `Integer`

Si aparece otro `serializationId`, la API:

- devuelve las filas parseadas antes del corte
- agrega warning de compatibilidad

## Compatibilidad legacy portada

Para labels y agrupacion se reutiliza la metadata ya portada desde `GameEffectDisplayService` legacy:

- labels legibles en espanol
- grupos como `Principales`, `Stats`, `Combate`, `Resistencias`, `Especiales`

## Decisiones de alcance

### Incluido

- lectura detallada de effects normales
- lectura detallada de critical effects
- comparacion runtime vs referencia por nivel
- warnings de decode por bucket

### Diferido

- `PATCH` de effects
- catalogo dedicado de opciones para Angular
- normalizacion de rows editables tipo Blazor
- preservacion/reescritura controlada de payloads mixtos por effect row

## Motivo para no abrir write en esta fase

Todavia hay dos riesgos tecnicos que conviene separar:

1. el schema actual no tiene identidad por row de effect; cualquier write seria una reserializacion completa del payload
2. el runtime actual acepta tanto hex serializado como fallback binario, asi que la estrategia segura de write debe decidir como preservar o eliminar esos fallbacks sin cambiar comportamiento en produccion

Por eso esta fase deja la lectura lista y la decision de escritura explicitamente diferida.

## Archivos principales

- API: `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`
- Application:
  - `ISpellsAdminReadService`
  - `ISpellsAdminReadRepository`
  - `SpellsAdminReadService`
  - modelos `AdminSpellLevelEffectsReadModel`, `AdminSpellEffectCollectionReadModel`, `AdminSpellEffectRowReadModel`
- Contracts:
  - `SpellLevelEffectsDto`
  - `SpellEffectCollectionDto`
  - `SpellEffectRowDto`
- Infrastructure:
  - `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/SpellEffectsDecoder.cs`
  - `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
  - `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/ReferenceSpellCatalogReader.cs`

## Validacion

Comando ejecutado:

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"
```

Resultado:

- `OK`

No se ejecuto:

- `npm run build`

Motivo:

- Angular no fue tocado

## Siguiente paso recomendado

Antes de abrir escritura o Angular editor de effects:

1. decidir estrategia de write para payload hex y fallback binario
2. definir si Angular necesita un endpoint de opciones/catalogo de effects en la misma fase
3. documentar reglas de preservacion para rows no soportadas, si aparecen
