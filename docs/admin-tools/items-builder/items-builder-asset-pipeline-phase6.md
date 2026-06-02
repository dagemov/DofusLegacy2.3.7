# Items Builder Asset Pipeline Phase 6

> Status correction on `2026-06-02`: this document records exploratory work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/items-builder-asset-pipeline-phase6`
- Base branch: `feature/items-builder-live-data-phase5`
- Scope type: curated preview pipeline only
- Suggested commit: `feat: add items builder preview asset pipeline`
- Current roadmap follow-up after repo realignment: `Phase 7A - Item Icon Selector`

## Goal

Make the existing preview paths actually useful by introducing the smallest possible curated PNG pipeline for Items.

This phase stays read-only:

- no item create/edit
- no upload UI
- no client publish flow
- no SWF extraction
- no DB writes

Phase 7 pause note:

- the prior create/edit exploration remains reference-only
- the official roadmap now pauses full `Create/Edit`
- the next accepted step is `Phase 7A - Item Icon Selector`

## What changed

### Official preview structure

The Angular workspace now owns the official curated preview roots:

```txt
src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-item/
src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/
src/Admin/RollblackLegacy.Admin.Angular/src/assets/manual-assets/items/
```

Tracked files introduced in this phase:

- `src/assets/item-previews/by-icon/1001.png`
- `.gitkeep` placeholders for empty `by-item` and `manual-assets/items`

### Backend resolution order

The API now resolves preview files from the official Angular asset roots in this order:

1. `manual-assets/items/{itemId}.png`
2. `item-previews/by-item/{itemId}.png`
3. `item-previews/by-icon/{iconId}.png`
4. placeholder

### Preview metadata exposed to Angular

`ItemPreviewStateDto` now carries:

- `state`
- `byItemPath`
- `byIconPath`
- `manualPath`
- `previewSource`
- `resolvedPath`
- `fallbackUsed`

The Angular detail card now shows:

- preview source
- resolved path
- fallback used

## Curated seed used in this phase

Validated exact seed:

- source: `DofusBeta-2.0/Dofus-2/client/app/content/gfx/items/bitmap/1001.png`
- target: `src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/1001.png`
- SHA256: `86B55A877AF4A8EABB82C48C11C8E44969CD7A76F67FDEA030E876C8037539D6`

Reason for choosing `by-icon` instead of `manual`:

- `itemId=39` has `IconId=1001`
- the old stack preview service explicitly resolved inventory PNGs by `IconId`
- the single seed also benefits the related `IconId=1001` family (`39`, `68`, `69`, `70`)
- it avoids pretending we have a manually curated item-specific override when we only need preview validation

## Validation

Build validation:

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`: `OK`
- `npm run build`: `OK`

HTTP validation:

- `GET /api/admin/v1/items/39`: `previewState.state = FOUND`
- `GET /api/admin/v1/items/39`: `previewSource = BY_ICON`
- `GET /api/admin/v1/items/39`: `resolvedPath = /assets/item-previews/by-icon/1001.png`
- `HEAD /assets/item-previews/by-icon/1001.png`: `200`
- `GET /api/admin/v1/items/74`: `previewState.state = MISSING`
- `HEAD /assets/item-previews/by-icon/1005.png`: `404`

Browser validation:

- `/admin/items/39`: real preview image renders
- `/admin/items/39`: UI shows `Preview source = By icon path`
- `/admin/items/39`: UI shows `Fallback used = IconId fallback`
- `/admin/items/74`: placeholder remains visible
- `/admin/items/74`: UI shows `MISSING` plus `MANUAL_ASSET_MISSING`
- `/admin/items?page=1&pageSize=20&typeId=1`: `39/68/69/70` show `FOUND`, while `74+` still show `MISSING`

## Repo hygiene

Added `.gitignore` protections for temporary outputs:

```txt
Infrastructure/temporal-artifacts/
**/generated-item-previews/
**/item-preview-dumps/
```

These rules do **not** ignore the final curated asset folders.

## Phase status

Status: `DONE`

Exact note:

`A minimal curated by-icon preview pipeline is now live. itemId=39 renders a real PNG through IconId 1001, while items without curated PNGs stay on explicit placeholder fallback.`

## Next step

Follow-up after Phase 6:

- Phase 7A icon selector is now complete
- the first post-Phase 8 controlled import wave added `1002.png` through `1012.png`
- see [items-builder-png-import-plan.md](./items-builder-png-import-plan.md) for the approved import rules and reports

Related future note:

- deeper client asset and multilingual name research is intentionally deferred to [items-builder-client-asset-intelligence-future.md](./items-builder-client-asset-intelligence-future.md)
- that future `Phase 6.5A` is now documented, and `Phase 7A` is the next official step
