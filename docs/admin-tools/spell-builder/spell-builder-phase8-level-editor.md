# Macro 5 / Phase 8 - Spell Level Editor

Fecha: `2026-06-06`
Rama activa verificada: `feature/spell-builder-api-migration`
Estado inicial del worktree: `CLEAN`
Base funcional: `f2d0a96 feat: add angular spell detail`
Estado: `DONE`

## Objetivo cubierto

Agregar edicion Angular para Spell Levels dentro de `/admin/spells/:spellId`, usando la API de Phase 4 sin tocar effects, critical effects, base de datos, cliente ni publicacion.

La vista mantiene:

- modo lectura por defecto
- editor solo para el nivel seleccionado
- save/cancel con feedback
- recarga posterior del detalle y niveles

## Verificacion previa obligatoria

Comandos ejecutados antes de implementar:

```powershell
git branch --show-current
git status --short
git log --oneline -5
```

Resultado:

- rama correcta: `feature/spell-builder-api-migration`
- worktree limpio
- historial alineado con Macro 5 hasta `Phase 7`

## Confirmacion de Phase 4 en codigo y docs

Revision ejecutada sobre:

- `docs/admin-tools/spell-builder/spell-builder-phase4-levels-api.md`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelUpdateRequest.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellLevelUpdateResultDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/SpellsAdminWriteService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/SpellsAdminController.cs`

### Endpoint de escritura detectado

- `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`

No se usa `PUT`.

### Campos aceptados exactamente por backend

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

### Validaciones backend confirmadas

- debe enviarse al menos un campo editable
- `spellId > 0`
- `levelNumber > 0`
- `apCost >= 0`
- `minRange >= 0`
- `maxRange >= 0`
- `criticalHitProbability >= 0`
- `criticalFailureProbability >= 0`
- `minCastInterval >= 0`
- `initialCooldown >= 0`
- `maxCastPerTurn >= 0`
- `maxCastPerTarget >= 0`
- `maxRange >= minRange`

### Campos bloqueados o no soportados

Bloqueados por backend en esquema legacy:

- `castInDiagonal`
- `needTakenCell`
- `initialCooldown`

Fuera del contrato de escritura de Phase 4:

- `minPlayerLevel`
- `rangeCanBeBoosted`
- `criticalFailureEndsTurn`
- `statesRequired`
- `statesForbidden`
- `effects`
- `criticalEffects`

Importante:

- `Range` como campo separado no existe en el contrato write.
- En el port Angular se documenta y usa `maxRange` como la representacion operativa del rango maximo.

### Resolucion de `levelNumber`

- schema legacy:
  - posicion `1-based` dentro de `SpellLevelsCSV`
  - update directo por `spells_levels.Id`
- schema actual Sunshine:
  - posicion `1-based` segun el orden runtime de filas consumido por `SpellsLoader` y `SpellManager`
  - write strategy `current-runtime-row-rewrite`

Conclusion:

- Phase 4 si implementa escritura segura de levels
- era valido habilitar un editor Angular funcional en Phase 8

## Implementacion Angular

Ruta integrada:

- `/admin/spells/:spellId`

Archivos creados o modificados:

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.models.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.api.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.facade.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.scss`
- `docs/admin-tools/spell-builder/spell-builder-phase8-level-editor.md`
- `docs/handoffs/AGENT_HANDOFF.md`

## Servicio y modelos Angular agregados

### Nuevos contratos TypeScript

- `SpellLevelUpdateRequest`
- `SpellLevelUpdateResultDto`

### Nuevo metodo HTTP

- `updateSpellLevel(spellId, levelNumber, request)`

Implementacion:

- usa `PATCH`
- envia solo los campos realmente modificados
- no manda fields sin cambios
- respeta el bloqueo legacy de `castInDiagonal`, `needTakenCell` e `initialCooldown`

## Flujo UX implementado

### Modo lectura por defecto

La pantalla sigue entrando en lectura:

- selector de niveles
- resumen read-only del nivel activo
- states read-only
- effects y critical effects read-only bajo demanda

### Modo editar

Al pulsar `Editar nivel`:

- se abre formulario integrado sobre el nivel activo
- se colapsa el panel de effects para no mezclar fases
- se bloquea el cambio de nivel mientras haya edicion activa
- el formulario se precarga desde el nivel seleccionado

### Guardado

Al pulsar `Guardar nivel`:

- se valida localmente el formulario
- se construye request diff-only
- se llama `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
- si responde `OK`:
  - se muestra feedback de exito
  - se preservan warnings si existen
  - se actualiza el nivel en memoria
  - se relanza carga de detalle y niveles
  - se vuelve al modo lectura

### Cancelacion

Al pulsar `Cancelar`:

- se descartan cambios del formulario
- se restaura el valor actual del nivel
- se vuelve a modo lectura

### Si falla el guardado

- no se pierde el contenido editado
- el formulario queda abierto
- se muestran errores backend/locales
- el operador puede corregir y reintentar

## Campos editables implementados

- `apCost`
- `minRange`
- `maxRange`
- `castInLine`
- `castTestLos` presentado como `Line Of Sight`
- `criticalHitProbability`
- `criticalFailureProbability`
- `needFreeCell`
- `minCastInterval`
- `maxCastPerTurn`
- `maxCastPerTarget`

Campos editables solo cuando el backend no esta en schema legacy:

- `castInDiagonal`
- `needTakenCell`
- `initialCooldown`

## Campos bloqueados o read-only en UI

Se dejan read-only en esta fase:

- `minPlayerLevel`
- `rangeCanBeBoosted`
- `criticalFailureEndsTurn`
- `statesRequired`
- `statesForbidden`
- `effects`
- `criticalEffects`

Se muestran read-only con nota de bloqueo cuando el esquema es legacy:

- `castInDiagonal`
- `needTakenCell`
- `initialCooldown`

## Validaciones frontend aplicadas

- `apCost >= 0`
- `minRange >= 0`
- `maxRange >= 0`
- `criticalHitProbability >= 0`
- `criticalFailureProbability >= 0`
- `minCastInterval >= 0`
- `initialCooldown >= 0`
- `maxCastPerTurn >= 0`
- `maxCastPerTarget >= 0`
- `maxRange >= minRange`
- `Guardar` deshabilitado si no hay cambios

No se agrego una validacion frontend artificial para fields no soportados; se siguio el contrato real de backend.

## Limitaciones conocidas

- el editor se apoya en `runtimeLevelId` para detectar el bloqueo legacy de tres campos; esa semantica esta respaldada por Phase 4 y por el propio write service
- `Range` como campo separado no existe y por eso no se renderiza como input independiente
- states siguen totalmente read-only en Phase 8
- effects y critical effects siguen en lectura bajo demanda, sin write
- no se pudo ejecutar Browser QA desde tool dedicado porque en esta sesion no quedo expuesto un tool Browser callable; la validacion visual quedo limitada a build y estructura

## Validacion ejecutada

### Angular

Comando:

```powershell
npm run build
```

Resultado:

- `OK`
- warning de budget inicial:
  - total `501.51 kB`
  - excede el warning de `500 kB` por `1.51 kB`

### Dotnet

Comando:

```powershell
dotnet build "Sunshine net11.0\Sunshine net11.0\Sunshine.sln"
```

Resultado:

- `OK`

Warnings observados:

- `NETSDK1057` por SDK preview
- `CS0169` en `D2pEntry.cs`
- `CA1416` en `FirewallManager.cs`

## Proxima fase recomendada

- `Phase 9 - Spell Effects Editor`

Alcance sugerido:

- abrir write controlado para `effects` y `criticalEffects`
- definir estrategia de preservacion del payload runtime/binario
- mantener separacion clara entre editor de nivel y editor de effects
