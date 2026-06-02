# Items Builder Angular Plan

> Roadmap note on `2026-06-02`: the next official UI step is `Phase 7A - Item Icon Selector`. Full `Create/Edit` remains paused until the repo realignment is complete in the official baseline.

## Snapshot

- Date: `2026-06-02`
- Branch: `feature/items-builder-create-edit-phase7`
- Workspace: `src/Admin/RollblackLegacy.Admin.Angular`
- Current scope: read-only list/detail plus writable create/edit/duplicate workflow, live Sunshine data integration, and curated preview asset reuse

## Current reality

`DofusLegacy2.3.7` now contains a real Angular admin workspace:

- Angular `21.x`
- standalone components
- SCSS
- Bootstrap
- local proxy to `/api/admin/v1/*`

This document now tracks the implemented Phase 3 and Phase 4 UI slices, not just future ideas.

## Implemented slices

```txt
src/app/admin/items/
  data-access/
    items.api.ts
    items.models.ts
    items.queries.ts
    items.facade.ts
  components/
    item-client-identity-card.component.ts
    item-diagnostic-panel.component.ts
    item-preview-card.component.ts
    item-runtime-summary-card.component.ts
    item-warning-badge.component.ts
  items-page.component.ts
  items-page.component.html
  items-page.component.scss
  item-detail-page.component.ts
  item-detail-page.component.html
  item-detail-page.component.scss
  item-write-page.component.ts
  item-write-page.component.html
  item-write-page.component.scss
src/app/shared/components/
  api-problem-panel.component.ts
src/app/shared/utils/
  copy-text.ts
```

Routes implemented:

- `/admin/items`
- `/admin/items/:itemId`
- `/admin/items/new`
- `/admin/items/:itemId/edit`
- `/admin/items/:itemId/duplicate`

## What the current UI does

### Item list page

Implemented:

- search by free text
- filters for `itemId`, `iconId`, `typeId`, `levelMin`, `levelMax`
- page and page-size query persistence
- columns for `ItemId`, `ResolvedName`, `Type`, `Level`, `Set`, `IconId`, `AppearanceId`, `PreviewState`, `WarningCount`
- operator-visible identity rule banner:
  - `ItemId != IconId != AppearanceId`
- controlled problem panel with `traceId`

### Item detail page

Implemented:

- runtime summary card with explicit identity separation
- client identity card with copy helpers
- preview/manual asset card with state badge, candidate paths, preview source, resolved path, fallback label, copy helpers, and safe placeholder rendering
- diagnostics panel with severity badges and readable empty state
- read-only effects table
- controlled problem panel with `traceId`

### Item write pages

Implemented:

- create form with explicit identity rule and deferred-field notes
- edit form preloaded from live item detail
- duplicate form preloaded from live item detail while keeping new-identity semantics explicit
- preview card reuse with `IconId`-driven preview lookup before save
- diagnostics panel reuse for advisories and backend warnings
- `ProblemDetails` and field-level validation rendering for `409/422/500`
- list/detail entry points into create/edit/duplicate routes

## Data and contract rules preserved

- consume the existing Phase 2 read endpoints plus the narrow Phase 7 write endpoints only
- keep Angular write DTOs aligned to the backend contracts
- keep `ItemId`, `IconId`, and `AppearanceId` visibly separate
- keep diagnostics operator-facing and readable before any future write flow
- keep preview handling `IconId`-driven during the write flow

## Error handling pattern

The Angular slice now reuses the intended previous-stack pattern:

- typed API service per resource
- normalized problem parser
- visible `HTTP status`
- visible `traceId`
- validation field lists when the backend returns them
- safe fallback shells when live item detail is unavailable
- safe advisory fallback even when mutating happy-path smoke tests are intentionally deferred

## Current live-data state

The tracked example config still keeps a placeholder `SunshineAdmin` password, but the live-data integration is now validated through an ignored local secret file and a local `sunshine-db` runtime.

Practical consequence:

- `GET /api/admin/v1/items`, `GET /api/admin/v1/items/{itemId}`, and `GET /api/admin/v1/item-sets/options` are now validated locally against real Sunshine rows
- `/admin/items` and `/admin/items/39` now render live data end to end
- `/admin/items/1` still serves as the clean not-found regression check with visible `traceId`
- preview cards can render real paths, and `itemId=39` now renders a curated `by-icon` seed through `1001.png`
- items without curated PNGs still stay on explicit placeholder fallback
- clipboard actions now fall back to visible manual-copy text when the browser clipboard path is unavailable
- Phase 7 write routes now preload real Sunshine data for `itemId=39`
- safe API validation confirms `422` payload errors and `traceId` without mutating the current DB

## Phase status

- Phase 3 / 8 - Items Builder Angular list/detail: `DONE (live list/detail validation completed locally against Sunshine data)`
- Phase 4 / 8 - Item Diagnostics + Preview UI: `DONE (identity, diagnostics, preview-state cards, and copy fallbacks validated on live detail data)`
- Live-data integration follow-up branch: `DONE (feature/items-builder-live-data-phase5)`
- Phase 6 / 8 - Asset Pipeline + PNG Preview: `DONE (one curated by-icon seed validated; placeholder fallback remains explicit)`
- Phase 7 / 8 - Item Create/Edit: `PARTIAL (implementation complete; destructive happy-path smoke test intentionally deferred)`

## Next Angular extensions

1. Run one approved create/edit/duplicate smoke test on a disposable or backed-up Sunshine dataset.
2. Add manual asset upload only after storage rules and ignored paths are finalized.
3. Add publish-flow UX only after preview staging and audit requirements are accepted.
4. Reuse the live-data/error-state/clipboard patterns for future spells and monsters slices.

## Out of scope in this branch

- create item
- edit item
- PNG upload
- publish flow
- spells/monsters/maps shared shell
- final design system work for the whole admin suite
