# Items Builder Angular Phase 3

> Status correction on `2026-06-02`: this document records exploratory work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/items-builder-angular-list-detail-phase3`
- Base branch: `feature/items-builder-readonly-api-phase2`
- Scope type: implemented Angular read-only list/detail over the existing Items admin API
- Backend dependency commit: `588f322 feat: add items builder readonly admin api`

## Goal

Create the first functional Angular slice for Items Builder without reopening backend work.

This phase is intentionally read-only:

- list
- search
- filter
- detail
- client identity
- preview state
- read-only diagnostics

This phase does not implement create, edit, upload, publish, or write flows.

## Delivered workspace and files

Created workspace:

- `src/Admin/RollblackLegacy.Admin.Angular`

Implemented feature files:

```txt
src/app/admin/items/
  data-access/
    items.api.ts
    items.models.ts
    items.queries.ts
    items.facade.ts
  items-page.component.ts
  items-page.component.html
  items-page.component.scss
  item-detail-page.component.ts
  item-detail-page.component.html
  item-detail-page.component.scss
src/app/shared/components/
  api-problem-panel.component.ts
```

Supporting files:

- `proxy.conf.json`
- `src/environments/environment.ts`
- root route shell and Bootstrap styling

## Implemented routes

- `/admin/items`
- `/admin/items/:itemId`

## What was validated

### Build

- `npm run build`: `OK`

### Local runtime wiring

- Angular dev server proxy to `http://127.0.0.1:5248`: `OK`
- `GET /api/admin/v1/items/types/options` loads through the Angular UI
- `GET /api/admin/v1/items` surfaces controlled `500` problem details when `SunshineAdmin` stays on placeholder config
- `GET /api/admin/v1/items/1` and `GET /api/admin/v1/items/1/identity` surface controlled `500` problem details with visible `traceId`

### UI behavior

List page now shows:

- search and numeric filters
- separate `ItemId`, `IconId`, and `AppearanceId`
- type options from the real endpoint
- operator-visible `traceId` panel for backend failures

Detail page now shows:

- runtime shell
- identity-rule banner
- controlled `ProblemDetails` panel with `traceId`

## Identity rule preserved

```txt
ItemId != IconId != AppearanceId
```

The Angular list and detail views keep those values visually separate.

## Phase status

`IN_PROGRESS`

Reason:

- the Angular slice is implemented and builds
- browser validation confirms routing, filters, proxy, and error handling
- local live item-data validation remains blocked by the placeholder `SunshineAdmin` secret

This is not a backend gap anymore. It is a local secret/config gap.

## Immediate next step

Provide a usable local `appsettings.Development.local.json` for `RollblackLegacy.Admin.Api`, then re-run:

- `/admin/items`
- `/admin/items/{existingItemId}`

Once that is done, Phase 3 can move from implemented UI to real-data validation and then continue toward write flows.

## Expected commit

- `feat: add items builder angular list and detail`
