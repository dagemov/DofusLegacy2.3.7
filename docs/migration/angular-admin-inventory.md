# Previous Angular / Admin API Inventory

## Scope

Source analyzed:

- `RollBlackServer/2.0.0/Rollback/Rollback.Admin.Angular`
- `RollBlackServer/2.0.0/Rollback/Rollback.Admin.Api`
- `RollBlackServer/2.0.0/Rollback/Rollback.Admin.Application`
- `RollBlackServer/2.0.0/Rollback/Rollback.Admin.Contracts`
- `RollBlackServer/2.0.0/Rollback/MapTools`
- supporting docs under `RollBlackServer/2.0.0/Rollback/Docs`

This repo is the best reference for contracts, feature slicing, and admin architecture patterns.

## Structural findings

- The previous stack already introduced the intended separation:
  - `Rollback.Admin.Api`
  - `Rollback.Admin.Application`
  - `Rollback.Admin.Contracts`
  - Angular standalone frontend
- Angular is based on `@angular/* 21.x`.
- The frontend already has:
  - typed HTTP services
  - standalone feature components
  - proxy config
  - reusable API problem handling
- The backend already defined:
  - `/api/admin/v1/*`
  - read contracts
  - write contracts
  - audit/result envelopes
  - `409`, `422`, and trace metadata patterns

## Module inventory

| Module | API | Angular component | Service | DTO / docs | Estado | Decision |
| --- | --- | --- | --- | --- | --- | --- |
| Angular shell, proxy, and shared error pattern | environment + route shell around `/api/admin/v1/*` | `app.routes.ts`, root app shell, shared problem components | `AdminLookupService`, `toAdminApiProblem(...)` helpers | `admin-api.models.ts`, `angular-admin-roadmap.md`, `angular-admin-feature-pattern.md` | stable foundation | `PORT_DIRECTLY_IF_COMPATIBLE` |
| Monster Families feature | `GET/POST/PUT /monster-families` | `monster-family-list-page.component.*`, `monster-family-editor-page.component.*` | `AdminMonsterFamiliesApi` | `monster-family.models.ts` + roadmap docs | usable read/write slice | `PORT_DIRECTLY_IF_COMPATIBLE` |
| Monsters feature | `GET /monsters`, `GET /monsters/{id}` | `monster-list-page.component.*`, `monster-detail-page.component.*` | `AdminMonstersApi` | `monster.models.ts`, `monster-builder-roadmap.md` | read-only implemented | `PORT_DIRECTLY_IF_COMPATIBLE` |
| Maps / world builder MVP | `GET /maps`, `GET /maps/{id}`, map child endpoints | `map-list-page.component.*`, `map-detail-page.component.*`, `map-cell-grid.component.*` | `AdminMapsApi` | `map.models.ts`, `map-point.util.ts`, `maps-world-builder-mvp.md` | read-only MVP | `ADAPT_TO_SUNSHINE_DB` |
| Read-only Admin API catalog | `AdminReadControllers.cs` exposes monsters, monster families, items, NPCs, maps, dungeons, spells | none directly | `IAdminReadCatalogService` | `AdminDtos.cs`, `AdminReadContracts.cs`, phase 3 docs | implemented in legacy repo | `ADAPT_TO_SUNSHINE_DB` |
| Write-enabled Admin API catalog | `AdminWriteControllers.cs` and `AdminWriteControllers.Phase4Modules.cs` expose create/update flows for monster families, monsters, items, NPCs, maps, dungeons, spells | Angular only partially connected | `IAdminWriteCatalogService` | `AdminWriteDtos.cs`, `AdminWriteContracts.cs`, phase 4 and phase 5 docs | implemented, but tied to old repo/runtime | `ADAPT_TO_SUNSHINE_DB` |
| Audit, validation, and mutation envelope pattern | structured `400/404/409/422` responses with audit metadata | `ApiProblemPanelComponent`, `ValidationFieldErrorsComponent` consume the pattern | `AdminWriteControllerBase` + Angular helpers | `AdminPatchResultDto`, validation/audit docs | mature pattern | `PORT_DIRECTLY_IF_COMPATIBLE` |
| Items/NPCs/Dungeons/Spells backend backlog | backend-only contracts and controller slices exist; Angular pages were not committed in the same maturity level as monsters/maps | not yet implemented as full Angular features | resource-specific future services implied by contracts | admin API docs and roadmaps | backend-defined, frontend incomplete | `REWRITE_CLEAN` |
| MapTools console utility | direct DB and client-file console workflow | none | `MapWorkerManager`, `InteractiveWorkerManager`, `ClientManager` | `MapTools/Program.cs` | direct DB/client utility | `REFERENCE_ONLY` |
| World-builder roadmap library | docs-only | none | none | `Docs/world-builder/*` | rich planning reference | `REFERENCE_ONLY` |
| Launcher visual reference library | docs-only | none | none | `Docs/launcher/*`, `Docs/ui/*` | visual guidance only | `REFERENCE_ONLY` |
| Process / backup / gitflow rules | docs-only | none | none | `Docs/process/general-db-backup-and-gitflow-rules.md` | process reference | `REFERENCE_ONLY` |

## What is especially reusable

- Angular feature folder pattern:
  - one feature per folder
  - one small service per resource
  - models in `core/models`
  - shared API problem components
- API conventions:
  - `/api/admin/v1/*`
  - typed query objects
  - paged result DTOs
  - patch result DTOs with audit metadata
- Operator-facing error contract:
  - `409 Conflict`
  - `422` semantic validation
  - `traceId`
  - field-level errors

## What must be adapted, not copied blindly

- SQL and repository implementations tied to the old DB shape
- any assumptions about `rollback_world_clean`
- MapTools direct file/DB writes
- phase naming and branch naming from the previous repo

## Notable gap versus the new repo

- The previous Angular frontend already had a real admin shell.
- The current `DofusLegacy2.3.7` repo does not yet have that shell.
- The previous backend had explicit admin projects.
- The current repo still needs those projects created under the `RollblackLegacy` naming.

## Migration recommendation

Start the new admin stack by reusing:

- Angular component/service structure
- error handling
- DTO/result envelopes
- route and proxy conventions
- clean-architecture boundaries

Do not reuse directly:

- old DB-specific implementations
- direct console file patching tools
- repo-specific assumptions about legacy schemas
