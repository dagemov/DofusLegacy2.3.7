# Spell Builder Git Sanity Before Phase 4

Fecha: `2026-06-05`

## Rama actual

- Rama activa verificada: `feature/sets-builder-crud-and-pagination`
- `git rev-list --left-right --count feature/items-preview-sets-polish-final...feature/sets-builder-crud-and-pagination`
  - resultado: `0 0`
- Conclusion:
  - la rama activa no tiene commits propios adicionales respecto a `feature/items-preview-sets-polish-final`
  - la diferencia actual esta en el **worktree sucio**, no en la historia ya confirmada

## Estado del worktree

### Cambios sin commit

Se detectaron cambios no staged en:

- Angular de `item-sets`
- API/Application/Infrastructure de `item-sets`
- `Client2.3.7/`
- `OneLauncher/`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/WorldServerManager.cs`
- documentacion de items
- scripts VPS

### Untracked

Se detectaron untracked en:

- nuevos archivos Angular/API/Application/Infrastructure de `item-sets`
- `Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.sln`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/temporal-artifacts/`
- archivos locales de `Client2.3.7`, `OneLauncher` y `config/`

### Diagnostico del worktree

- El worktree **no esta limpio**.
- El trabajo pendiente actual pertenece a **Items/Sets** y no a Spell Builder.
- No es seguro cambiar de rama ni ejecutar saneamiento Git de Spell Builder sin tratar primero ese estado local.

## Ultimos commits

Comando auditado:

```powershell
git log --oneline --decorate --graph -12
```

Resumen relevante:

```txt
8ced8f6 (HEAD -> feature/sets-builder-crud-and-pagination, origin/feature/items-preview-sets-polish-final, feature/items-preview-sets-polish-final) Revert "feat: add spell catalog api"
dd0f287 Revert "feat: add spell detail api"
9031339 feat: add spell detail api
ac8521f docs: record items preview and sets polish
d258c07 fix: load item stat icons correctly
5c1f3f2 feat: add item set previews and bonuses
7391e7a fix: reconcile item previews from category catalog
e5f0964 (feature/item-preview-category-expansion-phase6d) feat: add spell catalog api
7e66b1b docs: record item preview category expansion
3124d21 feat: improve category gallery navigation
3966445 feat: expand item preview categories
ccfcb8a (feature/item-preview-massive-extraction-phase6c) docs: audit legacy spell builder parity
```

## Diagnostico

### 1. Que rama esta activa

- Activa: `feature/sets-builder-crud-and-pagination`

### 2. Si existen cambios sin commit

- Si.
- Son numerosos y corresponden a Items/Sets y cambios locales auxiliares.

### 3. Si los commits de Spell Builder estan encima de una rama de Items

- Si.
- `e5f0964` y `9031339` fueron creados sobre una linea de Items.
- Esto contamino la historia de la rama de Items aunque el tree actual ya fue limpiado por revert.

### 4. Si los commits de Items ya fueron revertidos o no

- **No**.
- Los commits de Items siguen vigentes.
- Lo que fue revertido fueron los commits de Spell Builder:
  - `dd0f287` revierte `9031339`
  - `8ced8f6` revierte `e5f0964`

### 5. Si existe una rama correcta de Spell Builder

- **No** existe hoy una rama dedicada `feature/spell-builder-*`.
- `feature/item-preview-category-expansion-phase6d` no sirve como rama correcta de Spell Builder porque:
  - es una rama de Items
  - solo contiene hasta Phase 2
  - no representa el scope completo `Phase 1 + Phase 2 + Phase 3`

### 6. Si Phase 1, Phase 2 y Phase 3 estan juntos o separados

- `Phase 1`:
  - `ccfcb8a`
  - commit limpio solo documental
- `Phase 2`:
  - `e5f0964`
  - commit limpio de Spell Builder catalog API
- `Phase 3`:
  - `9031339`
  - commit limpio de Spell Builder detail API

Conclusion:

- Los tres commits existen y son identificables.
- No estan hoy en una rama dedicada exclusiva de Spell Builder.
- `Phase 2` y `Phase 3` aparecen incrustadas en historia de ramas de Items.

### 7. Que commits pertenecen exclusivamente a Spell Builder

- `ccfcb8a` -> `docs: audit legacy spell builder parity`
- `e5f0964` -> `feat: add spell catalog api`
- `9031339` -> `feat: add spell detail api`

Evidencia:

- `git show --stat --summary` de esos commits solo toca:
  - `docs/admin-tools/spell-builder/*`
  - `Angular-tools/Admin/*/Spells/*`
  - `SpellsAdminController`
  - `docs/handoffs/AGENT_HANDOFF.md`

### 8. Que commits pertenecen a Items y no deben mezclarse

Commits confirmados de Items/Items+Sets:

- `3966445` -> `feat: expand item preview categories`
- `3124d21` -> `feat: improve category gallery navigation`
- `7e66b1b` -> `docs: record item preview category expansion`
- `7391e7a` -> `fix: reconcile item previews from category catalog`
- `5c1f3f2` -> `feat: add item set previews and bonuses`
- `d258c07` -> `fix: load item stat icons correctly`
- `ac8521f` -> `docs: record items preview and sets polish`

Commits de revert relacionados con Spell dentro de Items:

- `dd0f287` -> `Revert "feat: add spell detail api"`
- `8ced8f6` -> `Revert "feat: add spell catalog api"`

## Riesgo

### Riesgo principal

- Continuar Phase 4 desde la rama activa mezclaria trabajo Spell con una rama y un worktree de Items/Sets.

### Riesgos secundarios

- Cambiar de rama con el worktree actual podria arrastrar cambios locales de Items/Sets.
- Hacer cherry-pick, merge o reset sin limpiar y acordar el flujo podria romper trazabilidad.
- Usar `feature/item-preview-category-expansion-phase6d` como base canonica de Spell Builder dejaria la fase incompleta y con ancestry de Items.

## Plan recomendado paso a paso

### Recomendacion general

- **No ejecutar saneamiento Git todavia sin aprobacion humana.**
- El saneamiento correcto debe separar:
  1. preservacion del trabajo local actual de Items/Sets
  2. preservacion visible de los commits limpios de Spell Builder
  3. reapertura futura de Spell Builder sobre base acordada

### Plan recomendado

1. Congelar el trabajo actual de Items/Sets.
   - No tocar ni mezclar Spell Builder en la rama activa.
   - Resolver primero que hacer con el worktree sucio actual:
     - commit de Items/Sets en su rama correspondiente, o
     - stash explicito temporal, si el responsable lo aprueba

2. Crear una **rama puntero de rescate** para Spell Builder sin mover HEAD ni reescribir historia.
   - Objetivo:
     - preservar `Phase 1/2/3` como linea identificable de trabajo Spell
   - Forma sugerida:
     - rama nueva apuntando a `9031339`

3. Mantener intactas las ramas de Items.
   - No revertir los reverts.
   - No borrar `feature/items-preview-sets-polish-final`.
   - No borrar `feature/sets-builder-crud-and-pagination`.

4. Cuando Items esten listos y el equipo lo apruebe, abrir la rama real de continuidad Spell.
   - Recomendacion:
     - base limpia acordada, idealmente `devp` despues del merge de Items
   - A partir de alli:
     - cherry-pick controlado de `ccfcb8a`, `e5f0964` y `9031339`
     - o reaplicacion equivalente si el equipo prefiere no portar el handoff tal cual

5. Recien despues iniciar Phase 4.

## Comandos exactos sugeridos

### Solo despues de aprobacion humana

#### Opcion A - preservacion minima e inmediata del trabajo Spell

No cambia HEAD ni toca el worktree actual:

```powershell
git branch feature/spell-builder-api-migration 9031339
git branch --contains 9031339
```

Uso recomendado:

- preservar ya mismo una rama visible de Spell Builder
- dejar trazabilidad clara sin tocar la rama actual de Items

#### Opcion B - continuidad limpia posterior desde base acordada

Solo despues de limpiar el worktree y de acordar la base:

```powershell
git switch devp
git pull --ff-only
git switch -c feature/spell-builder-api-migration
git cherry-pick ccfcb8a
git cherry-pick e5f0964
git cherry-pick 9031339
```

Uso recomendado:

- reabrir Spell Builder en una rama correcta y limpia
- evitar que Phase 4 nazca sobre una rama de Items

### Si antes hay que despejar el worktree de Items/Sets

Esto requiere decision humana porque afecta trabajo en curso:

```powershell
git status
git stash push -u -m "items-sets-wip-before-spell-git-sanity"
```

o, preferiblemente, commit de Items/Sets en su propia rama si el responsable lo decide.

## Que NO se debe hacer

- No iniciar `Phase 4` en `feature/sets-builder-crud-and-pagination`.
- No usar `git reset --hard`.
- No hacer `git cherry-pick` inmediato sobre la rama activa de Items.
- No hacer `merge` de Spell Builder hacia la rama de Items.
- No revertir `dd0f287` ni `8ced8f6` dentro de la rama de Items.
- No borrar ramas.
- No forzar push ni reescribir historia remota sin aprobacion explicita.
- No asumir que `feature/item-preview-category-expansion-phase6d` es la rama final correcta de Spell Builder.

## Requiere aprobacion humana

- **Si.**

Motivos:

- hay worktree sucio de Items/Sets
- la rama activa no es Spell Builder
- el siguiente paso correcto afecta la estrategia de ramas y la trazabilidad de commits ya compartidos entre historias de Items y Spell

## Conclusion

- Spell Builder **no debe continuar todavia**.
- El contenido de Spell Builder esta recuperable y bien identificado en:
  - `ccfcb8a`
  - `e5f0964`
  - `9031339`
- La rama de Items ya no tiene el contenido Spell en el tree actual, pero si tiene la historia y los reverts.
- La accion mas segura antes de Phase 4 es:
  1. preservar una rama dedicada apuntando a `9031339`
  2. no tocar la rama activa de Items
  3. reabrir luego Spell Builder desde una base limpia aprobada
