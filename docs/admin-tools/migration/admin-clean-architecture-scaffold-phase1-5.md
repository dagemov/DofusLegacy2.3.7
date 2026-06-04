# Admin Clean Architecture Scaffold Phase 1.5

> Status correction on `2026-06-02`: this document records exploratory scaffold work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/admin-clean-architecture-scaffold-phase1-5`
- Base branch: `origin/devp`
- Scope type: scaffold and architecture only

## Goal

Create the base Admin clean-architecture projects inside the current solution so the next phase can implement the first real Items Builder read-only endpoints without mixing concerns into public website or launcher code.

## Delivered structure

```txt
src/
  Admin/
    RollblackLegacy.Admin.Api/
    RollblackLegacy.Admin.Application/
    RollblackLegacy.Admin.Contracts/
    RollblackLegacy.Admin.Domain/
    RollblackLegacy.Admin.Infrastructure/
```

## Solution adaptation

Earlier planning assumed a possible standalone `RollblackLegacy.Admin.sln`.

Actual repo adaptation:

- the scaffold was added to the existing `Sunshine net11.0/Sunshine net11.0/Sunshine.sln`
- this keeps the current repo convention intact and avoids introducing a second competing solution too early

## Project references

Implemented dependency graph:

```txt
Admin.Api -> Admin.Application, Admin.Contracts, Admin.Infrastructure
Admin.Application -> Admin.Domain, Admin.Contracts
Admin.Infrastructure -> Admin.Application, Admin.Domain, Admin.Contracts
Admin.Domain -> no project dependencies
Admin.Contracts -> no project dependencies
```

Rules preserved:

- `Contracts` does not depend on Infrastructure
- `Domain` does not depend on Infrastructure
- `Api` does not open MySQL connections directly

## DI and configuration

Created:

- `AddAdminApplication()`
- `AddAdminInfrastructure(IConfiguration configuration)`

Configured safe connection-string handling through:

- `src/Admin/RollblackLegacy.Admin.Api/appsettings.json`
- `src/Admin/RollblackLegacy.Admin.Api/appsettings.Development.example.json`
- optional ignored local override:
  - `src/Admin/RollblackLegacy.Admin.Api/appsettings.Development.local.json`

Tracked example connection string:

```json
"ConnectionStrings": {
  "SunshineAdmin": "Server=127.0.0.1;Port=3306;Database=sunshine;User ID=sunshine;Password=change-me;Allow User Variables=true;TreatTinyAsBoolean=true"
}
```

## Health endpoints

Implemented:

- `GET /api/admin/v1/health`
- `GET /api/admin/v1/health/db`

Current behavior:

- `/health` always returns the basic service identity
- `/health/db` is safe to call and returns:
  - `ok` when the probe succeeds
  - `not_configured` when the placeholder password is still present
  - `error` when a real connection attempt fails

## What is intentionally out of scope

- Items read-only endpoints
- Angular admin
- create/edit item flows
- manual asset upload
- DB writes
- DB schema changes
- gameplay integration

## Validation

Expected validation for this phase:

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"
dotnet run --project src/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj
GET /api/admin/v1/health
```

## Next phase

With the scaffold in place, the next implementation phase is:

```txt
Items Builder Read-only API
```

Expected first endpoints:

```txt
GET /api/admin/v1/items
GET /api/admin/v1/items/{itemId}
GET /api/admin/v1/items/{itemId}/identity
GET /api/admin/v1/items/types/options
GET /api/admin/v1/item-sets/options
```

## Expected commit

- `chore: scaffold admin clean architecture projects`
