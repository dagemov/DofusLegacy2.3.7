# Admin Tools Migration Master Plan

## Snapshot

- Date: `2026-05-31`
- Working repo: `DofusLegacy2.3.7`
- Source tools:
  - legacy Blazor: `DofusBeta-2.0/Dofus-2/Rollback/Rollback.Web`
  - previous Angular/Admin API: `RollBlackServer/2.0.0/Rollback`
- Stable branches to keep: `main`, `Yaco`
- Planning branch: `feature/admin-tools-migration-master-plan`
- Documentation root adapted to the real repo shape: `docs/` instead of `Docs/`

## Git baseline

Remote Git state was re-validated on `2026-05-31`.

- `origin/main` already contains `origin/Yaco`.
- `Yaco` reached `main` indirectly through merged PR [#8](https://github.com/dagemov/DofusLegacy2.3.7/pull/8) (`Yaco -> devp`) and merged PR [#9](https://github.com/dagemov/DofusLegacy2.3.7/pull/9) (`devp -> main`).
- There is no remaining remote diff for a direct `Yaco -> main` promotion PR today.
- Result: `main` is the active promoted baseline, and `Yaco` remains the named stable branch that must not be deleted.

Because the local `Yaco` checkout had unrelated uncommitted files, this planning work was prepared in a clean worktree based on `origin/main`, which already includes `Yaco`.

## Strategic decisions

1. `DofusLegacy2.3.7` stays as the only destination repo.
2. Legacy Blazor is a business-logic reference, not a target stack.
3. Previous Angular/Admin API is the strongest source for contracts, feature slices, and architecture patterns.
4. No code or asset mass-copy happens in this branch.
5. Admin tooling must stay isolated from gameplay/runtime mutation paths except through explicit application services.
6. Browser UI must never talk directly to MySQL.
7. Secrets stay local-only and outside Git.

## What migrates, what rewrites, what stays reference-only

- Migrate to Angular:
  - items builder UX
  - sets editor UX
  - spells editor UX
  - monster builder UX
  - map/world builder UX
- Reuse logic selectively:
  - item identity diagnostics
  - manual PNG pipeline
  - client publish orchestration concepts
  - spell level/effect schema mapping
  - glyph and trap synchronization rules
  - audit and admin metadata concepts
- Reuse patterns/contracts selectively:
  - Angular feature folder pattern
  - typed API services
  - `409/422/traceId` handling
  - `AdminPatchResultDto` style audit payloads
  - `/api/admin/v1/*` resource design
- Keep as reference only:
  - Blazor pages and layouts
  - MapTools console
  - launcher WPF/Electron binaries
  - exploratory docs that describe old roadmaps but not the new repo reality
- Ignore:
  - `bin/`, `obj/`, `artifacts`, temp exports, raw dumps, and uncurated asset bundles

## Current repo reality

The current repo already has these useful building blocks:

- `Sunshine net11.0/` for emulator/runtime
- `OneLauncher/OneLauncher.Api` for launcher-facing HTTP APIs and auth endpoints
- `RollblackLegacy.Auth` for account/auth logic against `sunshine`
- `RollblackLegacy.Website*` projects for public website flow and registration/login UI
- `docs/`, `docker/`, `scripts/`, and validated VPS/database workflow docs

What the repo does not yet have:

- `RollblackLegacy.Admin.Api`
- `RollblackLegacy.Admin.Application`
- `RollblackLegacy.Admin.Domain`
- `RollblackLegacy.Admin.Infrastructure`
- `RollblackLegacy.Admin.Contracts`
- `RollblackLegacy.Admin.Angular`

See [dofuslegacy-admin-target-architecture.md](./dofuslegacy-admin-target-architecture.md) for the proposed shape.

## Legacy analysis references

- [blazor-admin-inventory.md](./blazor-admin-inventory.md)
- [angular-admin-inventory.md](./angular-admin-inventory.md)
- [team-vps-database-workflow.md](./team-vps-database-workflow.md)
- [admin-tools-migration-risk-register.md](./admin-tools-migration-risk-register.md)

## Recommended first migration target

After the baseline architecture and account/website alignment are in place, the first high-value module should be `Items Builder`.

Why `Items` first:

- It has the richest validated legacy logic in the Blazor tool.
- It gives immediate admin value without touching gameplay loops.
- It exercises the reusable PNG/manual asset pipeline early.
- It unlocks later work for sets, vendors, and visual data hygiene.
- It is lower risk than spells and maps.

`Spells` should come after items because spell runtime/client coherence and glyph-trap sync are materially riskier.

## Phase estimates

| Phase | Status | Hours min | Hours max | Risk | Dependencies | Expected result |
| --- | --- | ---: | ---: | --- | --- | --- |
| Phase 1 - Baseline admin architecture | `PENDING` | 6 | 10 | Medium | repo structure approval | clean admin solution and project skeleton |
| Phase 2 - Account/website integration | `PENDING` | 6 | 10 | Medium | Phase 1 | website/admin auth path aligned to `sunshine` |
| Phase 3 - Items Builder migration | `PENDING` | 12 | 20 | Medium | Phases 1-2 | Angular + API item CRUD with PNG preview |
| Phase 4 - Spells Builder migration | `PENDING` | 15 | 25 | High | Phases 1-3 | spell editor with levels/effects/glyph rules |
| Phase 5 - Monster Builder migration | `PENDING` | 12 | 20 | Medium | Phases 1-2 | Angular monster catalog and family workflows |
| Phase 6 - Map Builder migration | `PENDING` | 15 | 30 | High | Phases 1, 5 | map/group tooling with guarded writes |
| Phase 7 - Launcher visual/tooling reference | `PENDING` | 6 | 12 | Low | design freeze | admin visual guidance from launcher identity |
| Phase 8 - VPS/team workflow | `PENDING` | 4 | 8 | Medium | all previous phases | repeatable backup/deploy/secret workflow |

## Phase 1 - Baseline admin architecture

Status: `PENDING`

Goal:
- Create the clean admin solution and project boundaries inside `DofusLegacy2.3.7`.

Scope:
- define project names
- create solution membership
- establish references between API/Application/Domain/Infrastructure/Contracts
- decide auth boundary for internal admin requests
- decide Angular workspace location

Out of scope:
- feature implementation
- DB writes
- client asset publishing
- gameplay changes

Inputs:
- current repo layout
- previous Angular clean-architecture plan
- this master plan

Outputs:
- agreed project tree
- solution file
- baseline docs and ADR-style decisions

Checklist:
- [ ] create `RollblackLegacy.Admin.sln`
- [ ] create `RollblackLegacy.Admin.Api`
- [ ] create `RollblackLegacy.Admin.Application`
- [ ] create `RollblackLegacy.Admin.Domain`
- [ ] create `RollblackLegacy.Admin.Infrastructure`
- [ ] create `RollblackLegacy.Admin.Contracts`
- [ ] create `RollblackLegacy.Admin.Angular`
- [ ] define internal auth strategy
- [ ] define environment and secret loading rules

Validation:
- `git status --short`
- solution restores cleanly if skeleton projects are added
- no production DB mutation

Branch:
- `feature/admin-tools-migration-master-plan`

Expected commits:
- `docs: define dofuslegacy admin target architecture`
- later `chore: scaffold rollblacklegacy admin solution`

Close criteria:
- every admin layer exists in-repo
- references are explicit
- Angular location is locked
- no SQL is exposed to the browser

## Phase 2 - Account/website integration

Status: `PENDING`

Goal:
- Align website, launcher API, and future admin auth against the validated `sunshine` connection.

Scope:
- reuse `RollblackLegacy.Auth`
- confirm operator/admin identity flow
- document environment settings for website/admin/api
- define how admin sessions differ from public website sessions

Out of scope:
- full RBAC implementation
- public launcher redesign
- gameplay account changes

Inputs:
- `RollblackLegacy.Auth`
- `OneLauncher.Api`
- `RollblackLegacy.Website*`
- VPS/database workflow doc

Outputs:
- admin auth integration plan
- documented connection policy
- minimal operator identity model

Checklist:
- [ ] map current website auth flow
- [ ] map current launcher auth flow
- [ ] decide whether admin API uses cookie, token, or internal reverse proxy auth
- [ ] define admin role source of truth
- [ ] document local secret storage rules
- [ ] confirm connection strings and placeholder strategy

Validation:
- docs reviewed
- no password committed
- no live DB schema changes

Branch:
- `feature/admin-tools-migration-master-plan`

Expected commits:
- `docs: document team vps database workflow`

Close criteria:
- operator login path is clear
- public website and admin paths are not conflated
- secret handling is documented

## Phase 3 - Items Builder migration

Status: `PENDING`

Goal:
- Port the validated item builder into Angular + Admin API.

Scope:
- item create/edit
- item search and lookup
- type and set relations
- manual PNG upload
- client preview and icon resolution
- controlled client publish workflow
- DB validation and audit metadata

Out of scope:
- mass importers
- automatic asset scraping from uncurated packs
- direct client binary patching without backup

Inputs:
- Blazor item services and preview/upload pipeline
- previous Angular feature pattern
- target admin architecture

Outputs:
- item list/detail/editor pages
- item API endpoints
- upload/preview contract
- audit and warning surfaces

Checklist:
- [ ] implement item list API
- [ ] implement item detail API
- [ ] implement item create/update API
- [ ] implement item effects handling
- [ ] implement set lookup handling
- [ ] implement manual PNG upload pipeline
- [ ] implement preview component
- [ ] implement diagnostic/warning panel
- [ ] verify DB writes against `sunshine`

Validation:
- manual CRUD smoke test
- preview resolves valid PNGs
- warnings surface missing icon/text issues
- `git status --short`

Branch:
- dedicated `feature/...` branch from the admin baseline branch or `main`

Expected commits:
- `feat: scaffold admin items api`
- `feat: add angular items builder`

Close criteria:
- an operator can create/edit an item without raw SQL
- PNG preview/upload works with curated assets
- audit warnings are visible

## Phase 4 - Spells Builder migration

Status: `PENDING`

Goal:
- Port spell editing into Angular + API with runtime safety.

Scope:
- spell identity edit
- spell levels
- effects and critical effects
- class links and references
- glyph/trap synchronization rules
- controlled client publish orchestration
- QA checklist for in-game verification

Out of scope:
- broad gameplay rebalance
- combat engine rewrites
- silent runtime patching with no rollback

Inputs:
- Blazor spell editor
- spell schema service
- spell publish orchestrator
- legacy gameplay/spell docs

Outputs:
- spell editor UX
- spell API endpoints
- spell validation rules
- client publish/backups workflow

Checklist:
- [ ] implement spell list/detail/editor API
- [ ] implement level editor contracts
- [ ] implement effect serialization/deserialization
- [ ] port glyph/trap ambiguity rules
- [ ] design dry-run or guarded publish mode
- [ ] define in-game QA checklist
- [ ] add client backup policy for `Spells*.swf`, `SpellLevels*.swf`, `i18n*.swf`

Validation:
- spell save works on staging/local DB
- warnings appear for ambiguous glyph/trap links
- manual in-game QA passes for at least one migrated spell

Branch:
- dedicated feature branch after Items

Expected commits:
- `feat: add admin spell contracts`
- `feat: add angular spell builder`

Close criteria:
- spells can be edited without manual SQL
- level/effect payload is consistent
- publish path has backups and QA notes

## Phase 5 - Monster Builder migration

Status: `PENDING`

Goal:
- Bring over the previous Angular monster admin pattern and adapt it to `sunshine`.

Scope:
- monster families
- monster list/detail
- create/edit monster
- grade editing
- spell assignments
- family lookups
- visual preview strategy

Out of scope:
- map canvas editor
- dungeon orchestration
- uncontrolled spawn rewrites

Inputs:
- previous Angular monster families and monsters modules
- legacy monster builder roadmap
- current DB normalization rules

Outputs:
- Angular monster family pages
- Angular monster pages
- matching API endpoints and contracts

Checklist:
- [ ] port monster families Angular slice
- [ ] port monsters Angular list/detail
- [ ] add monster create/edit form
- [ ] add family option lookup
- [ ] decide look/preview rendering strategy
- [ ] validate grades and spell assignments

Validation:
- feature builds cleanly
- `409/422/traceId` handling matches the feature pattern
- monster writes do not require direct DB tooling

Branch:
- dedicated feature branch

Expected commits:
- `feat: add monster families admin slice`
- `feat: add monster builder admin slice`

Close criteria:
- monster/family workflows are available in Angular
- core validations are enforced server-side

## Phase 6 - Map Builder migration

Status: `PENDING`

Goal:
- Reintroduce map and group tooling with safer write boundaries.

Scope:
- map list/detail audit
- cell/grid read model
- monster group assignments
- NPC spawns
- teleports
- later guarded write operations

Out of scope:
- direct reuse of `MapTools` console as production UI
- bulk direct client file rewrites

Inputs:
- previous Angular maps module
- MapTools reference behavior
- world builder docs

Outputs:
- read-first map audit module
- write contracts for selected safe operations
- backup/rollback rules for high-risk changes

Checklist:
- [ ] port maps read-only pages first
- [ ] align DB schema to `sunshine`
- [ ] expose monster-group and NPC-spawn APIs
- [ ] define guarded mutation set
- [ ] define dry-run for risky writes
- [ ] document rollback for map/group changes

Validation:
- read-only map audit works before writes are enabled
- risky writes require backups and validation
- no direct client DLM patching from browser UI

Branch:
- dedicated feature branch

Expected commits:
- `feat: add admin maps audit slice`
- `feat: add guarded map group mutations`

Close criteria:
- map audit is usable
- write scope is intentionally limited and documented

## Phase 7 - Launcher visual/tooling reference

Status: `PENDING`

Goal:
- Use launcher visual language as the admin design reference, without copying launcher binaries or desktop implementation details.

Scope:
- color, spacing, framing, identity references
- admin design tokens
- shared branding guardrails

Out of scope:
- replacing the real launcher
- copying WPF/Electron code into Angular

Inputs:
- launcher roadmap docs
- current website branding assets

Outputs:
- admin design notes
- reusable token decisions

Checklist:
- [ ] extract safe visual references
- [ ] define admin token palette
- [ ] define typography/layout guardrails
- [ ] document what is visual reference only

Validation:
- design notes reviewed
- no binaries or generated launcher outputs copied into admin

Branch:
- dedicated docs/design branch or feature branch

Expected commits:
- `docs: define launcher visual reference for admin`

Close criteria:
- admin UI direction is consistent
- launcher code remains separate

## Phase 8 - VPS/team workflow

Status: `PENDING`

Goal:
- Standardize how the team touches DB, SSH, deploys, and backups during the admin migration.

Scope:
- Navicat connection policy
- SSH access notes
- backup-before-write workflow
- branch/commit hygiene
- secret handling
- deploy touchpoints

Out of scope:
- changing provider infrastructure
- rotating production secrets inside Git

Inputs:
- validated VPS/DB connection details
- existing deploy docs

Outputs:
- team workflow doc
- backup checklist
- secret handling checklist

Checklist:
- [ ] document Navicat connection with placeholder password only
- [ ] document SSH target and safety rules
- [ ] define backup checkpoints per risky phase
- [ ] define production-write approval checkpoints
- [ ] define rollback ownership

Validation:
- docs reviewed
- no secrets committed
- workflow matches actual VPS and DB endpoints

Branch:
- `feature/admin-tools-migration-master-plan`

Expected commits:
- `docs: document team vps database workflow`

Close criteria:
- every risky phase has an explicit backup rule
- DB and SSH usage are documented without exposing credentials

## Planning deliverables created in this branch

- `docs/migration/admin-tools-migration-master-plan.md`
- `docs/migration/admin-tools-migration-master-plan.html`
- `docs/migration/blazor-admin-inventory.md`
- `docs/migration/angular-admin-inventory.md`
- `docs/migration/dofuslegacy-admin-target-architecture.md`
- `docs/migration/team-vps-database-workflow.md`
- `docs/migration/admin-tools-migration-risk-register.md`

## Close criteria for this documentary phase

This planning phase is considered closed when:

- `Yaco` is confirmed as already promoted into `main`
- `feature/admin-tools-migration-master-plan` exists
- Blazor legacy inventory exists
- previous Angular inventory exists
- target architecture exists
- master roadmap exists in Markdown and HTML
- VPS/DB workflow is documented without password leakage
- the first recommended migration module is explicit
- no legacy code or asset mass-copy was performed
