# Items Builder Live Data Phase 5

> Status correction on `2026-06-02`: this document records exploratory work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/items-builder-live-data-phase5`
- Base branch: `feature/items-builder-diagnostics-preview-phase4`
- Scope type: live-data integration only over the existing read-only API and Angular detail/list slices
- Suggested commit: `feat: connect items builder angular to live admin data`

## Goal

Stop relying on placeholder-only detail shells for the Angular Items Builder and validate the existing read-only UI against real local Sunshine data.

This phase stays read-only:

- no item create/edit
- no PNG upload
- no publish flow
- no SWF/client changes
- no DB writes

## Backend audit findings

The current Admin API already had the right read surface. The practical gaps were integration and local validation, not missing resource design.

Confirmed endpoints:

- `GET /api/admin/v1/items`
- `GET /api/admin/v1/items/{itemId}`
- `GET /api/admin/v1/items/{itemId}/identity`
- `GET /api/admin/v1/items/types/options`
- `GET /api/admin/v1/item-sets/options`

Important findings:

- `GET /api/admin/v1/items/{itemId}` is the authoritative detail endpoint
- diagnostics already come embedded as `warnings`
- preview/manual asset state already comes embedded as `previewState`
- `GET /api/admin/v1/items/{itemId}/identity` remains a useful explicit projection, but the detail payload already contains `clientIdentity`
- no separate diagnostics endpoint was needed
- no separate preview/icon/appearance endpoint was needed

## Frontend vs backend alignment

What Angular expected from Phase 4:

- runtime detail
- client identity
- preview/manual asset state
- diagnostics
- readable error state with `traceId`

What the backend already returned:

- `ItemDetailDto` with runtime fields
- nested `clientIdentity`
- nested `previewState`
- nested `warnings`
- nested `effects`

Adjustments made in this phase:

- the detail page now trusts the detail payload as the primary source for `clientIdentity`
- item-set options failure is now non-blocking for detail rendering
- live preview/manual routes can be proxied when files exist
- clipboard actions degrade to a visible manual-copy fallback when browser clipboard support fails

## Implementation delivered

### Backend

- allow local development opt-in for the placeholder-looking `SunshineAdmin` connection string through ignored local config
- expose `/assets/*` and `/manual-assets/*` as static-file roots for future preview/manual assets
- fix item-set option materialization so live `GET /api/admin/v1/item-sets/options` works against Sunshine rows

### Angular

- consume `detail.clientIdentity` directly from the detail payload
- keep detail rendering alive even if item-set options cannot load
- proxy asset routes through the Angular dev server
- keep component boundaries from Phase 4 intact
- add clipboard capability detection plus manual fallback labels and visible copy values

## Local live-data basis

Validated local sources:

- repo-local dump: `database/sunshine.sql`
- local MariaDB container: `sunshine-db`
- ignored local Admin API settings file: `src/Admin/RollblackLegacy.Admin.Api/appsettings.Development.local.json`

Observed local runtime facts:

- `items` table returned `6652` rows in the local container
- `itemId=39` exists and was used as the main live validation target
- `itemId=1` does not exist in this local dump and correctly returns `404`

## Live validation

Backend validation:

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`: `OK`
- `GET /api/admin/v1/health`: `200 OK`
- `GET /api/admin/v1/health/db`: `200 OK`
- `GET /api/admin/v1/items?page=1&pageSize=3`: `200 OK`
- `GET /api/admin/v1/items/39`: `200 OK`
- `GET /api/admin/v1/items/39/identity`: `200 OK`
- `GET /api/admin/v1/items/types/options`: `200 OK`
- `GET /api/admin/v1/item-sets/options`: `200 OK`
- `GET /api/admin/v1/items/1`: controlled `404` with `traceId`

Angular validation:

- `npm run build`: `OK`
- `/admin/items`: live list validated against Sunshine rows
- `/admin/items/39`: live runtime summary, client identity, diagnostics, and effects validated
- `/admin/items/1`: controlled error-state validated with visible `traceId`

Clipboard validation:

- copy helper remains available when supported
- when the browser clipboard path fails, the button switches to `Manual`
- the UI exposes `Manual copy value: ...` so the operator is never blocked silently

## What is now live

- item list rows
- item detail runtime summary
- client identity card
- diagnostics/warnings from the detail payload
- item type options
- item set options
- controlled `404/400/500` problem states with `traceId`

## What still stays fallback-only

- preview image rendering still depends on real files existing under the preview/manual asset roots
- in the current local state, preview remains path-based and often `UNKNOWN`
- clipboard success still depends on the browser context; manual fallback remains required

## Phase status

Status: `DONE`

Exact note:

`Live Sunshine-backed read validation is now complete locally for list/detail/identity/diagnostics. Preview remains intentionally path-based until curated item preview or manual asset files exist.`

## Next step

The next functional step is still item create/edit, but only after write contracts and asset ownership rules are approved.
