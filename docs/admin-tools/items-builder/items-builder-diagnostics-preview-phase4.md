# Items Builder Diagnostics + Preview UI Phase 4

> Status correction on `2026-06-02`: this document records exploratory work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/items-builder-diagnostics-preview-phase4`
- Base branch: `feature/items-builder-angular-list-detail-phase3`
- Scope type: Angular UI refinement only
- Suggested commit: `feat: add item diagnostics and preview ui`

## Goal

Make the read-only item detail page useful for operators by separating runtime data, client identity, preview/manual asset state, and diagnostics into focused UI cards.

This phase remains read-only:

- no item create/edit
- no PNG upload
- no publish flow
- no client or SWF changes
- no DB writes

## Implemented UI components

```txt
src/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/components/
  item-client-identity-card.component.ts
  item-client-identity-card.component.html
  item-client-identity-card.component.scss
  item-preview-card.component.ts
  item-preview-card.component.html
  item-preview-card.component.scss
  item-diagnostic-panel.component.ts
  item-diagnostic-panel.component.html
  item-diagnostic-panel.component.scss
  item-runtime-summary-card.component.ts
  item-runtime-summary-card.component.html
  item-runtime-summary-card.component.scss
  item-warning-badge.component.ts
  item-warning-badge.component.html
  item-warning-badge.component.scss
src/Admin/RollblackLegacy.Admin.Angular/src/app/shared/utils/
  copy-text.ts
```

## Detail page outcomes

The detail page now renders:

- runtime item summary card
- dedicated client identity card
- dedicated preview/manual asset card
- dedicated diagnostics panel
- read-only effects table
- controlled `ProblemDetails` panel with visible `traceId`

## Identity rule preserved

The UI keeps this rule explicit:

```txt
ItemId != IconId != AppearanceId
```

This is now reinforced both in the page banner and in the card layouts.

## Preview behavior

The preview card now shows:

- `FOUND`
- `MISSING`
- `MANUAL`
- `UNKNOWN`

It also surfaces:

- `ByItemPath`
- `ByIconPath`
- `ManualPath`

Behavior rules:

- if a usable preview path exists, the UI attempts to render it
- if the image cannot be reached from the current Angular host, a technical non-blocking placeholder message is shown
- no files are created, copied, or committed in this phase

## Diagnostics behavior

The diagnostics panel now renders these codes cleanly when returned:

- `MISSING_CLIENT_NAME`
- `MISSING_ICON`
- `ICON_ID_MISMATCH`
- `APPEARANCE_ID_MISMATCH`
- `MANUAL_ASSET_MISSING`
- `SET_LINK_MISSING`
- `UNKNOWN_TYPE`

Each entry shows:

- severity
- code
- field
- message

Empty state:

```txt
No identity warnings detected.
```

## Copy/debug helpers

Small copy buttons now exist for:

- `ItemId`
- `IconId`
- `AppearanceId`
- `ClientNameId`
- `ByItemPath`
- `ByIconPath`
- `ManualPath`

## Validation

Build validation:

- `npm run build`: `OK`

Local UI validation target:

- `/admin/items/1`

Validated in current local conditions:

- specialized cards render
- identity separation stays explicit
- preview state shell renders
- copy buttons are present
- controlled error panel with `traceId` remains visible when the Admin API cannot read Sunshine locally

## Phase status

Status: `DONE`

Exact note:

`UI slice implemented and validated in build/error-state mode; live item-backed validation still depends on a usable local SunshineAdmin secret`

## Next step

Phase 5 should implement read/write item create-edit flows only after live read validation is completed against a local secret configuration.
