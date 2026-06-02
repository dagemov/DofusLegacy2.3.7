# DofusLegacy Admin Target Architecture

## Objective

Define the target structure for the new admin ecosystem inside `DofusLegacy2.3.7` without disturbing the public website, launcher API, or emulator runtime.

## Current repo reality

Today the repo already contains:

- `Sunshine net11.0/` for emulator/runtime
- `OneLauncher/OneLauncher.Api` for launcher/public auth HTTP endpoints
- `RollblackLegacy.Auth` for `sunshine`-backed account logic
- `RollblackLegacy.Website`, `RollblackLegacy.Website.Application`, `RollblackLegacy.Website.Contracts`, `RollblackLegacy.Website.Domain`, `RollblackLegacy.Website.Infrastructure`
- `docs/`, `docker/`, `runtime/`, `scripts/`

Today the repo does not contain:

- any dedicated admin API
- any admin Angular workspace
- any admin contracts package
- any admin-focused application/domain/infrastructure layers

## Target project tree

Because the repo already uses root-level sibling projects, the admin stack should follow the same shape instead of introducing a new `src/` convention.

```txt
RollblackLegacy.Admin.sln
RollblackLegacy.Admin.Api/
RollblackLegacy.Admin.Application/
RollblackLegacy.Admin.Domain/
RollblackLegacy.Admin.Infrastructure/
RollblackLegacy.Admin.Contracts/
RollblackLegacy.Admin.Angular/
```

Optional future expansion:

```txt
RollblackLegacy.WorldBuilder.Angular/
```

This should stay separate until map tooling clearly outgrows normal admin CRUD.

## Layer responsibilities

### `RollblackLegacy.Admin.Api`

Responsibilities:

- expose internal admin HTTP endpoints
- translate HTTP concerns to application use cases
- enforce auth/authorization policy for admin operators
- return versioned DTOs from `RollblackLegacy.Admin.Contracts`

Rules:

- controllers stay thin
- no SQL
- no direct file patch logic
- no UI-specific transformation rules

### `RollblackLegacy.Admin.Application`

Responsibilities:

- use cases
- orchestration
- validation coordination
- audit metadata creation
- dry-run style workflows for risky modules

Rules:

- no Angular concepts
- no direct MySQL driver code
- no direct FFDec process management unless hidden behind infrastructure ports

### `RollblackLegacy.Admin.Domain`

Responsibilities:

- admin invariants
- domain rules
- semantic validation helpers
- pure value objects and policy rules

Rules:

- no framework dependencies when avoidable
- no DB queries
- no file system assumptions

### `RollblackLegacy.Admin.Infrastructure`

Responsibilities:

- MySQL access against `sunshine`
- curated client-asset file access
- backup helpers for risky client publish steps
- adapters for FFDec or similar external tools
- repository implementations

Rules:

- no UI rendering logic
- all risky writes must be explicit and testable
- file patching must require backup paths

### `RollblackLegacy.Admin.Contracts`

Responsibilities:

- request/response DTOs
- paged result DTOs
- audit/warning envelopes
- shared enums used at the API boundary

Rules:

- stable, explicit, versionable
- no runtime entities leaked from `Sunshine`

### `RollblackLegacy.Admin.Angular`

Responsibilities:

- tables
- filters
- forms
- previews
- operator-facing warnings
- problem rendering for `400/404/409/422`

Rules:

- no direct MySQL
- no admin business rules hidden only in the browser
- one feature per folder
- small typed API services

## Naming and tech recommendations

- Admin backend target: `net8.0`
  - reason: aligns with the current website/auth stack and avoids coupling the admin surface to the `net11.0` preview runtime
- Admin frontend target: Angular `21.x`
  - reason: already proven in the previous repo and current Angular patterns are fresh there
- API route prefix: `/api/admin/v1/*`
- Root solution: `RollblackLegacy.Admin.sln`

## Integration map

### Public website path

`RollblackLegacy.Website -> OneLauncher.Api -> RollblackLegacy.Auth -> sunshine`

This path already exists and should stay focused on public registration/login.

### Admin path

`RollblackLegacy.Admin.Angular -> RollblackLegacy.Admin.Api -> RollblackLegacy.Admin.Application -> RollblackLegacy.Admin.Domain/Infrastructure -> sunshine + curated client assets`

This path must stay separate from the public website even if both ultimately reuse `RollblackLegacy.Auth`.

## Auth strategy guidance

Recommended baseline:

- reuse `RollblackLegacy.Auth` as the identity and password layer
- do not reuse public website pages as the admin UI shell
- keep admin authorization explicit at the API boundary
- reserve room for future operator roles beyond a single `ADMIN` flag

## Data and asset boundaries

### DB boundary

- `sunshine` is the target DB
- browser never talks to DB directly
- infrastructure owns SQL
- risky write modules should support preflight validation or dry-run where it makes sense

### Client asset boundary

- manual PNG previews and client publish workflows stay in infrastructure
- all client file mutations require:
  - local curated asset paths
  - backup directory policy
  - explicit operator action

## Modules by architectural style

### CRUD-first admin modules

- items
- sets
- monster families
- monsters
- NPCs

These fit naturally into standard Angular + API feature slices.

### High-risk admin modules

- spells
- maps
- monster groups
- teleports

These require:

- stronger validation
- backup discipline
- traceability
- likely staged rollout

## Non-goals

- no Blazor Server final stack
- no direct copy of `MapTools` as a browser tool
- no Angular direct DB access
- no gameplay rebalance as part of tooling migration
- no secret storage in source control

## Phase 1 deliverables implied by this architecture

- create the six admin projects and solution
- define project references
- define admin auth boundary
- define environment/secret loading strategy
- create the first `Contracts` primitives:
  - paged results
  - problem payloads
  - audit envelope
  - lookup option DTOs

## Recommendation

Build the new admin stack as a sibling ecosystem inside the current repo:

- public website remains public
- launcher API remains launcher/public auth focused
- emulator runtime remains separate
- admin becomes its own bounded vertical with explicit layers
