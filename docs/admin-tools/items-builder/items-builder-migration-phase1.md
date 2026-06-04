# Items Builder Migration Phase 1 Audit

## Snapshot

- Date: `2026-05-31`
- Branch: `feature/items-builder-migration-phase1`
- Scope type: documentation and audit only
- Follow-up contract phase: `feature/items-builder-readonly-contracts-phase2`
- Scaffold follow-up phase: `feature/admin-clean-architecture-scaffold-phase1-5`
- Current implementation follow-up: `feature/items-builder-create-edit-phase7`
- Source references:
  - legacy Blazor: `DofusBeta-2.0/Dofus-2/Rollback`
  - previous Angular/Admin API: `RollBlackServer/2.0.0/Rollback`
  - target repo: `DofusLegacy2.3.7`

## Goal

Freeze the technical audit for the first real functional migration slice: `Items Builder`.

This branch does not implement code yet. It documents:

- the current Sunshine item schema
- the portable parts of the legacy Blazor item editor
- the target API contracts and Angular feature shape
- the safe asset pipeline for manual PNG preview work

Current note:

- the later read-only API, Angular list/detail, preview pipeline, and Phase 7 write workflow all still depend on the audit conclusions frozen here

## Scope

In scope:

- audit current item-related tables in `sunshine`
- verify how item identity differs from client icon identity
- verify whether a current admin API or Angular admin already exists
- inventory the old Blazor item builder
- extract reusable contract patterns from the older Angular/Admin API repo
- define draft DTOs, endpoints, Angular structure, and asset rules

Out of scope:

- scaffolding `RollblackLegacy.Admin.*` projects
- writing to the production-like `sunshine` database
- copying the Blazor project
- mass-copying PNGs or client folders
- SWF/client publish automation
- gameplay edits

## Audit answers

### 1. Current item table in Sunshine

The current repo uses Sunshine runtime tables, not the older Rollback table names.

Confirmed local evidence:

- `Sunshine.MySql/Database/World/Items/ItemTemplate.cs` maps to `[Table("items")]`
- `Sunshine.MySql/Database/Managers/ItemManager.cs` loads:
  - `SELECT * FROM items`
  - `SELECT * FROM items_weapons`
  - `SELECT * FROM items_sets`
- `database/sunshine.sql` contains:
  - `CREATE TABLE items`
  - `CREATE TABLE items_sets`
  - `CREATE TABLE npcs_items`

Phase 1 conclusion:

- the target write model must adapt to `items`, not blindly reuse any legacy `items_templates` assumptions
- vendor links must account for `npcs_items`
- set lookups must account for `items_sets`

### 2. Item id versus image/icon identity

Item identity and client bitmap identity are not interchangeable.

Observed legacy behavior:

- Blazor search explicitly distinguishes `itemId`, `iconId`, `png`, and name
- `ItemEditModel` keeps both runtime and client-facing metadata:
  - `ClientIconId`
  - `ClientAppearanceId`
  - `ReferenceIconId`
  - `ManualAssetRelativePath`
- `ItemClientPublishService` resolves publish icon ids from multiple candidates and only accepts a real `.png` for client bitmap publishing

Phase 1 conclusion:

- future contracts must keep `ItemId`, `IconId`, `AppearanceId`, and manual preview asset metadata separate
- the Angular UI must never imply that `ItemId == IconId`

### 3. Existing admin API in the current repo

No dedicated admin API project exists yet inside `DofusLegacy2.3.7`.

Implication:

- the next implementation phase must scaffold a clean admin API instead of extending public website code directly

### 4. Existing Angular admin in the current repo

No Angular admin workspace exists yet inside `DofusLegacy2.3.7`.

Implication:

- the Angular feature plan in this phase is architectural guidance for the future `RollblackLegacy.Admin.Angular` workspace

### 5. Legacy Blazor logic that is actually reusable

High-value reusable concepts:

- item list filtering and lookup UX
- item detail/edit flow
- diagnostic and audit surfacing
- identity correction preview
- manual asset upload workflow
- preview resolution against client bitmaps
- publish orchestration concepts and warning rules

Do not port directly yet:

- Blazor UI components
- in-process data access patterns
- direct publish execution to local client files
- legacy admin support table creation without Sunshine approval

### 6. Curated versus non-curated PNG assets

Useful curated references:

- legacy admin manual asset folder under `Rollback.Web/wwwroot/admin-assets/items`
- local client bitmap folder used only as a validation/reference source:
  - `client/app/content/gfx/items/bitmap`

Non-curated or blocked sources:

- whole client bitmap dumps committed to Git
- `bin/`, `obj/`, `artifacts`, temp exports
- bulk folder copy from legacy repos

### 7. Preview storage without polluting the repo

Phase 1 recommendation:

- keep preview/manual uploads outside tracked folders by default
- if a webroot-backed preview path is needed later, it must be either:
  - ignored by `.gitignore`, or
  - configurable outside the repo root
- canonical persisted preview assets for future publish workflows should end as `.png`

## Current migration conclusion

`Items Builder` is still the correct first feature slice to audit, but implementation remains gated by the baseline admin architecture and auth alignment phases described in the master roadmap.

Phase 2 follow-up on `2026-06-01` confirmed that the repo still has no `RollblackLegacy.Admin.*` project set. That means the next safe move is:

1. freeze the read-only endpoint and DTO surface
2. avoid partial implementation inside unrelated projects
3. schedule a small admin-foundation scaffold before real API coding

Phase 1.5 follow-up on `2026-06-01` then delivered that scaffold in a dedicated branch, so the next execution step is no longer architecture discovery. It is implementation of the Items Builder read-only API on top of the new Admin project set.

Phase 2 implementation follow-up on `2026-06-01` then delivered the read-only API slice itself on `feature/items-builder-readonly-api-phase2`.

Current Phase 2 outcome:

1. read-only endpoints exist
2. DTOs exist in `RollblackLegacy.Admin.Contracts`
3. error handling returns controlled `400/404/500` problem details with `traceId`
4. local runtime validation is `PARTIAL` because `SunshineAdmin` still uses the placeholder on this machine

The practical outcome of this branch is:

1. freeze the Sunshine schema assumptions
2. freeze which Blazor concepts are portable
3. freeze a clean contract set
4. freeze a safe asset policy
5. reduce rework before API/Angular scaffolding begins

## Deliverables in this branch

- [items-builder-blazor-inventory.md](./items-builder-blazor-inventory.md)
- [items-builder-target-contracts.md](./items-builder-target-contracts.md)
- [items-builder-angular-plan.md](./items-builder-angular-plan.md)
- [items-builder-asset-pipeline.md](./items-builder-asset-pipeline.md)
- [items-builder-readonly-api-phase2.md](./items-builder-readonly-api-phase2.md)

## Checklist

- [x] confirm the current Sunshine item/set/vendor tables
- [x] confirm that no current admin API exists in the target repo
- [x] confirm that no Angular admin workspace exists in the target repo
- [x] inventory reusable Blazor item-builder logic
- [x] extract reusable API contract patterns from the previous Angular/Admin API repo
- [x] define target DTOs and endpoints
- [x] define Angular UX and feature boundaries
- [x] define manual asset and preview rules
- [x] confirm that Phase 2 remains contracts/docs because admin projects do not exist yet
- [x] scaffold `RollblackLegacy.Admin.*`
- [x] implement read endpoints
- [ ] implement write endpoints
- [ ] implement Angular UI

## Validation

- `git status --short`
- documentation only, no build required
- no database mutation
- no client/SWF mutation

## Expected commit

- `docs: audit items builder migration phase`

## Close criteria

This phase is closed when:

- the audit answers are explicit
- the legacy item editor is inventoried
- the target contracts are defined
- the Angular plan is defined
- the asset pipeline is documented
- the master roadmap and risk register reflect this new branch
- no code or asset mass-copy happened
