# Macro 5 / Phase 7 - Angular Detail

Fecha: `2026-06-06`
Rama activa verificada: `feature/spell-builder-api-migration`
Estado inicial del worktree: `CLEAN`
Base funcional: `d2e5cf1 feat: add angular spell catalog`
Estado: `DONE`

## Objetivo cubierto

Reemplazar el placeholder de `/admin/spells/:spellId` por una vista Angular real de detalle para Spell Builder, manteniendo la fase en modo lectura:

- identidad principal del spell
- metadata de referencia opcional
- niveles read-only
- effects y critical effects por nivel en lectura bajo demanda

No se habilito:

- editor de niveles
- editor de effects
- write de effects
- cambios de backend fuera del consumo de endpoints ya existentes

## Verificacion previa ejecutada

Comandos ejecutados antes de implementar:

```powershell
git branch --show-current
git status --short
git log --oneline -5
```

Resultado:

- rama correcta: `feature/spell-builder-api-migration`
- worktree limpio
- historial alineado con Macro 5 hasta `Phase 6`

## Referencias leidas antes de tocar codigo

- `docs/handoffs/AGENT_HANDOFF.md`
- `docs/admin-tools/spell-builder/spell-builder-audit.md`
- `docs/admin-tools/spell-builder/spell-builder-gap-analysis.md`
- `docs/admin-tools/spell-builder/spell-builder-port-map.md`
- `docs/admin-tools/spell-builder/spell-builder-phase3-detail-api.md`
- `docs/admin-tools/spell-builder/spell-builder-phase4-levels-api.md`
- `docs/admin-tools/spell-builder/spell-builder-phase5-effects-api.md`
- `docs/admin-tools/spell-builder/spell-builder-phase6-angular-catalog.md`
- `legacy-reference/Rollback.Web/Pages/Admin/Spells.razor`

## Ruta Angular final

- `/admin/spells/:spellId`

La navegacion existente desde el catalogo Phase 6 hacia el detalle ya quedo operativa con vista real.

## Servicio y metodos Angular creados

Archivos actualizados:

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.api.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.facade.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.models.ts`

Metodos agregados:

- `getSpell(spellId)`
- `getSpellLevels(spellId)`
- `getSpellLevelEffects(spellId, levelNumber)`

## Contratos consumidos

### Principal

- `GET /api/admin/v1/spells/{spellId}`

Usado para:

- `spellId`
- `name`
- `description`
- `typeId`
- `typeLabel`
- `iconId`
- `breeds`
- `levelCount`
- `runtimeAvailable`
- `referenceAvailable`
- `reference`

### Complementario para flags de nivel

- `GET /api/admin/v1/spells/{spellId}/levels`

Usado para completar flags que no estan en el resumen corto de Phase 3:

- `castInDiagonal`
- `needTakenCell`
- `initialCooldown`

Tambien se reutiliza para:

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
- `statesRequired`
- `statesForbidden`
- `hasEffects`
- `hasCriticalEffects`
- `runtimeAvailable`
- `referenceAvailable`

### Bajo demanda para lectura de effects

- `GET /api/admin/v1/spells/{spellId}/levels/{levelNumber}/effects`

Usado para:

- `effects.runtimeRows`
- `effects.referenceRows`
- `effects.runtimeWarnings`
- `effects.referenceWarnings`
- `criticalEffects.runtimeRows`
- `criticalEffects.referenceRows`
- `criticalEffects.runtimeWarnings`
- `criticalEffects.referenceWarnings`

## Vista implementada

Archivos creados o modificados:

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.scss`

Comportamiento final:

- boton `Volver a spells`
- cabecera con identidad principal del spell
- nota explicita sobre `IconId` sin inventar sprite
- bloque de metadata runtime
- bloque de metadata de referencia opcional
- selector de niveles estilo pill para mantener la lectura por nivel cercana al legacy
- detalle read-only del nivel activo
- boton `Ver effects por nivel`
- carga diferida y cache local por nivel para effects normales y criticos
- estados loading, error y empty para:
  - detalle
  - niveles
  - effects por nivel

## Campos mostrados

### Cabecera principal

- `SpellId`
- `Name`
- `Description`
- `TypeId`
- `TypeLabel`
- `IconId`
- `Breeds`
- `LevelCount`
- `runtimeAvailable`
- `referenceAvailable`

### Metadata de referencia opcional

- `sourceDescription`
- `name`
- `description`
- `nameId`
- `descriptionId`
- `typeId`
- `typeLabel`
- `iconId`
- `breedIds`
- `levelCount`

### Nivel activo read-only

- `levelNumber`
- `runtimeLevelId`
- `referenceLevelId`
- `minPlayerLevel`
- `apCost`
- `minRange`
- `maxRange`
- `castInLine`
- `castInDiagonal`
- `castTestLos` presentado como `lineOfSight`
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
- `statesRequired`
- `statesForbidden`
- `hasEffects`
- `hasCriticalEffects`
- `runtimeAvailable`
- `referenceAvailable`

### Effects por nivel

Buckets mostrados:

- `effects`
- `criticalEffects`

Por bucket:

- disponibilidad runtime/referencia
- fuente runtime/referencia
- warnings runtime/referencia
- tabla de filas runtime
- tabla de filas referencia

## Que quedo read-only

- toda la cabecera del spell
- metadata de referencia
- selector y resumen de niveles
- estados requeridos/prohibidos
- effects normales
- critical effects

No se agregaron botones ni formularios de escritura.

## Que queda diferido a Phase 8 y Phase 9

### Diferido a Phase 8

- editor de niveles
- write de `PATCH /api/admin/v1/spells/{spellId}/levels/{levelNumber}`
- navegacion de guardado y feedback de escritura

### Diferido a Phase 9

- editor de effects
- write de effects
- reglas de preservacion de payload runtime/binario al editar rows

## Limitaciones conocidas

- no existe respaldo visual estable para renderizar sprite/icono real del spell; se muestra `IconId`
- el detalle usa `GET /spells/{spellId}` como contrato principal, pero necesita `GET /levels` para exponer `castInDiagonal`, `needTakenCell` e `initialCooldown`
- la referencia puede llegar nula si el entorno no tiene `spellsReferences`
- la carga de effects es bajo demanda por nivel para no convertir la pantalla en un fetch masivo
- no se intento portar auditoria rica ni referencias bloqueantes del legacy porque siguen fuera del alcance de esta fase

## Archivos creados o modificados en esta fase

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.api.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.facade.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/data-access/spells.models.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.scss`
- `docs/admin-tools/spell-builder/spell-builder-phase7-angular-detail.md`
- `docs/handoffs/AGENT_HANDOFF.md`

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

No hubo:

- `MSB3027`
- `MSB3021`
- bloqueo externo por DLLs

## Proxima fase recomendada

- `Phase 8 - Spell Level Editor`

Alcance sugerido:

- reutilizar la misma pantalla de detalle
- abrir escritura solo para fields de nivel ya soportados por `PATCH`
- mantener effects y critical effects todavia fuera del write hasta Phase 9
