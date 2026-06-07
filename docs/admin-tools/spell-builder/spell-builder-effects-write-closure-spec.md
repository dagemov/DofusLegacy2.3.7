# Spell Builder Production Parity - Fase 2 - Effects Write Closure Spec

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`
Estado: `SPEC_ONLY`

## Objetivo

Cerrar la especificación técnica previa a cualquier implementación real de escritura de `effects` y `criticalEffects`, definiendo:

- formato runtime exacto
- formato legacy fallback exacto
- criterio de editabilidad
- validaciones bloqueantes
- warnings permitidos
- estrategia de backup, diff y rollback
- contrato propuesto para futura API de write
- criterio exacto para habilitar la siguiente fase

## Fuentes revisadas

- `docs/handoffs/AGENT_HANDOFF.md`
- `docs/admin-tools/spell-builder/spell-builder-production-risk-audit.md`
- `docs/admin-tools/spell-builder/spell-builder-effects-write-strategy.md`
- `docs/admin-tools/spell-builder/spell-builder-special-effects-map.md`
- `docs/admin-tools/spell-builder/spell-builder-phase5-effects-api.md`
- `docs/admin-tools/spell-builder/spell-builder-phase9-effects-editor.md`
- `legacy-reference/Rollback.Web/Pages/Admin/Spells.razor`
- `legacy-reference/Rollback.Web/Components/Admin/EffectListEditor.razor`
- `legacy-reference/Rollback.Admin/Models/GameEffects/EffectEditorKind.cs`
- `legacy-reference/Rollback.Admin/Models/Spells/SpellLevelEditModel.cs`
- `legacy-reference/Rollback.Admin/Services/GameEffectEditorService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminSchemaService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellClientPublishService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellEffectCollectionDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellEffectRowDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelEffectsDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelUpdateRequest.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/SpellEffectsDecoder.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/World/Spells/SpellTemplate.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/SpellManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/EffectManager.cs`

## Hallazgos verificados

### 1. Formato exacto del runtime actual

`Sunshine.WorldServer.Game.Effects.EffectManager.GetEffects(string hexa)` espera un contenedor serializado hex con esta estructura:

1. `short` inicial con cantidad de filas.
2. Por cada fila:
   - `uint effectId`
   - `uint diceNum`
   - `uint diceFace`
   - `int value`
   - `int delay`
   - `int duration`
   - `int targetType`
   - `utf` descartado por runtime actual
   - `uint zoneMinSize`
   - `uint zoneSize`
   - `uint zoneShape`
   - `bool`
   - `int`
   - `int`
   - `int`
   - `bool`

Hecho verificado adicional:

- `SpellManager` prioriza `Effects` / `CriticalEffects` serializados en string hex.
- Solo cae a binario si el string no existe o viene vacio.

### 2. Formato exacto del fallback legacy binario

`EffectManager.GetEffects(byte[] buffer)` soporta este formato:

1. Repetir hasta consumir `BytesAvailable`.
2. Por fila:
   - `byte serializationId`
   - `int effectId`
   - `uint random`
   - `short duration`
   - `ushort targetType`
   - `byte zoneShape`
   - `byte zoneSize`
3. Payload variable segun `serializationId`:
   - `1` -> sin payload numerico adicional
   - `4` -> `short value`, `short diceNum`, `short diceFace`
   - `6` -> `short value`

Hecho verificado adicional:

- El decoder actual del Admin marca como warning cualquier `serializationId` fuera de `1`, `4`, `6`.
- En legacy, `GameEffectEditorService` hacia round-trip real sobre ese formato binario.

### 3. Legacy de spells no era un editor libre de todos los kinds

Hechos verificados:

- `GameEffectEditorService` general soportaba `Base`, `Integer`, `Dice`, `String`, `Duration`, `Date`, `Mount`.
- El editor legacy de spells declaraba `_spellEffectKinds = new[] { EffectEditorKind.Dice };`.
- `SpellAdminService.NormalizeSpellEffects(...)` forzaba `row.Kind = EffectEditorKind.Dice`.
- La UI legacy documentaba que "los efectos de hechizo se fuerzan a formato `Dice` porque el runtime los carga como `EffectDice[]`".

Conclusión verificada:

- La primera versión segura de write para spells no debe abrir soporte general a todos los `kinds` del editor global legacy.

### 4. Separación de buckets obligatoria

Hechos verificados:

- Legacy guardaba y publicaba `BinaryEffects` y `BinaryCriticalEffects` por separado.
- Runtime actual carga `Effects` y `CriticalEffects` por separado.
- La API actual ya expone `effects` y `criticalEffects` como buckets distintos.

Conclusión verificada:

- Cualquier write futuro debe preservar separación estricta entre efectos normales y críticos.

### 5. Casos especiales que no son CRUD plano

Hechos verificados:

- Glifos y trampas usan `Effect.DiceNum` y `Effect.DiceFace` como link a un spell persistente interno.
- Legacy ya tenía sincronización adicional para payload persistente.
- Invocaciones, estados y venenos dependen de handlers y buffs del runtime.

Conclusión verificada:

- No todos los payloads decodificables son automáticamente editables.

## Criterio exacto de editabilidad

Un bucket de `effects` o `criticalEffects` se considera `editable` solo si cumple todos estos puntos:

1. Existe runtime real para el nivel.
2. El bucket runtime decodifica completamente.
3. El decode no devuelve warnings bloqueantes.
4. El formato origen queda identificado de forma determinista como uno de:
   - `current-serialized-hex`
   - `legacy-binary`
5. Todas las filas del bucket pertenecen a la `matriz inicial soportada`.
6. El bucket no contiene links persistentes ambiguos de glifo o trampa.
7. El bucket no contiene filas clasificadas como `motor-only`.
8. El encode de prueba cumple `decode -> encode -> decode` sin pérdida semántica.

Si falla cualquiera de esos puntos:

- el bucket es `read-only`
- se permite lectura y auditoría
- no se habilita write ni preview confirmable

## Matriz inicial soportada

### Soportado para primera habilitación

- filas canónicas tipo `Dice`
- filas simples sin warnings de decode
- filas sin sincronización persistente
- filas sin dependencia de template monstruo externo
- filas sin semántica de trigger especial

### Bloqueado inicialmente

- `serializationId` desconocido
- filas `Base` sin reconstrucción inequívoca
- glifos
- trampas
- invocaciones
- bombas
- esclavos
- filas cuyo comportamiento depende de sync externo
- filas que lleguen con warning de decode
- buckets mezclados con payload no preservable

## Decode sin warnings críticos

Se consideran `warnings críticos` y por tanto bloqueantes:

- `serializationId` legacy no soportado
- payload truncado o ilegible
- contador negativo de effects
- discrepancia entre formato detectado y formato esperado
- row que no puede mapearse a un DTO write canónico
- row clasificada como especial sin regla de preservación

Se consideran `warnings no críticos`:

- metadata de referencia ausente
- bucket de referencia vacío
- diferencias de label o `group` respecto de referencia
- ausencia de publish-client compatibility para tooltip, si el runtime write sigue siendo seguro

## Encode sin pérdida

### Regla general

La codificación futura debe ser `backend-only` y debe regenerar el bucket completo, nunca una fila aislada.

### Regla por formato

- Si el origen es `current-serialized-hex`, el resultado debe volver a `Effects` o `CriticalEffects`.
- Si el origen es `legacy-binary`, el resultado debe volver a `BinaryEffects` o `BinaryCriticalEffects`.
- No se permite convertir automáticamente un formato al otro en la primera versión productiva.

### Regla de preservación

Si el nivel tiene dos fuentes pobladas y el runtime está priorizando una:

- solo se escribe la fuente realmente activa
- la fuente fallback se conserva intacta
- no se borra fallback en Fase 3 salvo especificación posterior aprobada

## Round-trip obligatorio

Todo bucket que pretenda guardarse debe pasar esta secuencia:

1. `decode(runtimePayloadOriginal)`
2. `encode(writeModelNormalizado)`
3. `decode(payloadReencodeado)`
4. comparar semánticamente:
   - cantidad de filas
   - orden de filas
   - `effectId`
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
   - bucket normal o crítico

Si cualquier comparación falla:

- el save debe bloquearse
- el write preview debe marcar `roundtrip_failed`

## Estrategia propuesta para futura API de write

### Principio

La primera entrega de write real debe separar tres pasos lógicos, aunque puedan terminar en dos endpoints:

1. `preview/validate`
2. `confirm write`
3. `audit result`

### Contrato propuesto

Endpoint propuesto, no implementado en esta fase:

`POST /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects/preview`

Propuesta de request:

```json
{
  "expectedSourceFormat": "current-serialized-hex",
  "expectedBucketVersion": "sha256-or-etag-placeholder",
  "normalEffects": [
    {
      "rowIndex": 0,
      "effectId": 100,
      "operatorMode": "Dice",
      "value": 0,
      "minValue": 1,
      "maxValue": 3,
      "delay": 0,
      "random": 0,
      "duration": 0,
      "targetType": 0,
      "zoneShape": 0,
      "zoneMinSize": 0,
      "zoneSize": 0
    }
  ],
  "criticalEffects": [],
  "safetyFlags": {
    "allowRuntimeOnlyWrite": true,
    "forbidFormatMigration": true,
    "forbidSpecialEffectsWrite": true
  },
  "clientPublicationIntent": "none"
}
```

Propuesta de response:

```json
{
  "canSave": false,
  "blockingErrors": [],
  "warnings": [],
  "sourceFormat": "current-serialized-hex",
  "roundtripPassed": true,
  "backupPlan": {
    "runtimeBucketBackupRequired": true,
    "clientPublicationBackupRequired": false
  },
  "diffPreview": {
    "normalEffectsChanged": true,
    "criticalEffectsChanged": false,
    "rowDiffs": []
  },
  "safetyClassification": {
    "normalEffects": "supported",
    "criticalEffects": "empty",
    "hasSpecialEffects": false
  }
}
```

Endpoint propuesto para write final, no implementado en esta fase:

`PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects`

Regla:

- el `PATCH` solo debe aceptarse si existe preview previo exitoso y sin drift

## DTOs propuestos

### `SpellLevelEffectsPreviewRequestDto`

- `ExpectedSourceFormat`
- `ExpectedBucketVersion`
- `NormalEffects`
- `CriticalEffects`
- `SafetyFlags`
- `ClientPublicationIntent`

### `SpellEffectWriteRowDto`

- `RowIndex`
- `EffectId`
- `OperatorMode`
- `Value`
- `MinValue`
- `MaxValue`
- `Delay`
- `Random`
- `Duration`
- `TargetType`
- `ZoneShape`
- `ZoneMinSize`
- `ZoneSize`

### `SpellLevelEffectsPreviewResultDto`

- `CanSave`
- `BlockingErrors`
- `Warnings`
- `SourceFormat`
- `RoundtripPassed`
- `BackupPlan`
- `DiffPreview`
- `SafetyClassification`

### `SpellLevelEffectsWriteResultDto`

- `Saved`
- `WriteAuditId`
- `NormalEffectsChanged`
- `CriticalEffectsChanged`
- `Warnings`
- `RuntimeBackupReference`

## Validaciones obligatorias

1. `spellId` y `levelNumber` deben existir en runtime.
2. El bucket runtime debe existir y ser decodificable.
3. El request debe conservar buckets separados.
4. No se permite cambiar bucket normal por crítico ni viceversa.
5. No se permite formato distinto al detectado.
6. No se permite guardar filas fuera de la matriz soportada.
7. El request debe respetar orden explícito de filas.
8. Debe pasar round-trip sin pérdida.
9. Debe pasar control de concurrencia por `expectedBucketVersion`.
10. Debe existir backup lógico del bucket runtime antes del write real.

## Errores que deben impedir guardar

- `runtime_level_not_found`
- `runtime_bucket_missing`
- `unsupported_source_format`
- `decode_warning_blocking`
- `unsupported_serialization_id`
- `unsupported_effect_kind`
- `special_effect_requires_manual_strategy`
- `glyph_or_trap_sync_not_supported`
- `summon_effect_not_supported`
- `roundtrip_failed`
- `bucket_version_conflict`
- `format_migration_forbidden`
- `normal_critical_bucket_mix_forbidden`

## Warnings que pueden mostrarse sin bloquear

- referencia sana no disponible
- tooltip cliente no garantiza publish equivalente
- bucket de referencia vacío
- labels o grupos difieren de referencia
- el nivel tiene fallback binario conservado, pero inactivo

## Estrategia de backup

Antes de cualquier write real de effects debe existir backup mínimo del runtime:

1. `spellId`
2. `levelNumber`
3. bucket activo original
4. bucket inactivo fallback si existe
5. timestamp UTC
6. usuario o actor admin
7. hash del payload original

En la primera implementación:

- este backup debe ser lógico y almacenarse como artefacto o auditoría backend
- no debe tocar cliente ni publicación todavía

## Estrategia de rollback

Rollback mínimo requerido antes de Fase 3:

- restaurar exactamente el bucket activo original
- restaurar también el fallback preservado si existía
- registrar quién revirtió y por qué
- bloquear rollback si el nivel cambió nuevamente y no hay confirmación explícita

## Estrategia de diff y preview

Angular debe mostrar diff semántico, no payload crudo:

- bucket: normal o crítico
- filas agregadas
- filas eliminadas
- filas modificadas
- cambios por campo
- clasificación de seguridad por fila
- si el write sigue siendo runtime-only o si requerirá publicación cliente futura

Angular no debe mostrar:

- hex
- blob binario
- `serializationId`
- raw bytes

## Estrategia por categoría especial

### Effects normales

- soportar primero solo subset `Dice`
- preview obligatorio
- write solo si bucket completo es soportado

### CriticalEffects

- mismas reglas que el bucket normal
- auditoría y diff totalmente separados

### Glifos y trampas

- bloqueados en Fase 3 inicial
- requieren diseño posterior de sync hacia spell persistente enlazado

### Venenos

- candidatos solo si la fila sigue siendo `Dice` canónica y sin sync externo
- si dependen de comportamiento ambiguo por duración, bloquear inicialmente

### Invocaciones

- bloqueadas en Fase 3 inicial
- requieren validación de template monstruo, grado y reglas especiales

### Estados y conditions

- `StatesRequired` y `StatesForbidden` quedan fuera del write de effects
- siguen como dominio separado de nivel
- no se introduce `conditions` nuevo en esta fase porque no hay campo productivo verificado equivalente

## Riesgos

### Riesgos bloqueantes

- ausencia de serializer backend aprobado
- ambigüedad de glifos y trampas
- mezcla de formatos con migración implícita
- filas especiales sin round-trip determinista

### Riesgos no bloqueantes

- referencia ausente
- tooltip cliente legacy no reconstruible para todos los efectos
- deuda de idioma en Angular

## Decisiones recomendadas

1. Fase 3 no debe abrir write general; debe empezar por subset seguro y preview.
2. La primera cobertura soportada debe ser `Dice-only` para spells.
3. `StatesRequired` y `StatesForbidden` no deben mezclarse con el write de effects.
4. No se debe convertir automáticamente between `current-serialized-hex` y `legacy-binary`.
5. Todo texto final visible de Angular debe migrarse a español antes de exponer editor real al usuario final.

## Qué NO implementar todavía

- endpoint real de write
- serializer real
- write de glifos o trampas
- write de invocaciones
- publish cliente de spells
- cambios de motor de combate

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, incluyendo labels, errores, warnings, confirmaciones, resumen de diff y pasos de rollback.

## Criterio exacto para pasar a Fase 3

La Fase 3 solo puede iniciar si estos puntos quedan aceptados:

1. Queda aprobado que la cobertura inicial es `Dice-only`.
2. Queda aprobado que glifos, trampas e invocaciones siguen bloqueados.
3. Queda aprobado que el flujo empezará por `preview/validate` antes de `PATCH`.
4. Queda aprobado que no habrá migración automática entre formatos.
5. Queda aprobado el backup lógico mínimo por bucket runtime.
6. Queda aprobado el plan de round-trip de `decode -> encode -> decode`.
7. Queda aprobado que la UI final de Spell Builder será 100% en español.

Si cualquiera de esos puntos queda abierto:

- Fase 3 debe detenerse
- solo puede continuarse con documentación o laboratorio no destructivo

## Próxima fase recomendada

`Fase 3 - Backend Preview Contract for Dice-Only Spell Effects`
