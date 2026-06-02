# Items Builder Phase 7A - Item Icon Selector

## Snapshot

- Date: `2026-06-02`
- Status: `DONE`
- Scope type: implementation slice before full Item `Create/Edit`

## Goal

Deliver the first focused UI slice that helps operators choose the correct item icon before full write workflows resume.

## Why this comes before full Phase 7

- the repo source-of-truth had to be realigned first
- operators need a reliable icon-picking flow before broader create/edit
- the current curated preview catalog already proves the `IconId -> PNG` path
- this step is smaller and safer than reopening full item writes immediately

## Canonical locations

- Angular workspace: `Angular-tools/Admin/RollblackLegacy.Admin.Angular`
- Admin API: `Angular-tools/Admin/RollblackLegacy.Admin.Api`
- Admin contracts/application/infrastructure/domain: `Angular-tools/Admin/`

## Implemented slice

Backend:

- `GET /api/admin/v1/item-icons`
- query params: `search`, `iconId`, `page`, `pageSize`
- source: curated preview PNG catalog under `/assets/item-previews/by-icon`

Frontend:

- route: `/admin/items/icon-selector`
- component: `ItemIconSelectorComponent`
- entry points:
  - `/admin/items`
  - `/admin/items/:itemId`

## Responsibilities

- render preview PNG when available
- show `IconId`
- support search text and direct `iconId` filter
- support visual selection without touching DB
- emit `{ iconId, previewPath }` for future Phase 7 reuse
- keep `ItemId != IconId != AppearanceId` explicit in the surrounding UX

## Current catalog source

Use the curated preview catalog under:

```txt
/assets/item-previews/by-icon
```

Current expectations:

- the component starts from a small curated manifest discovered from PNG filenames
- the first pass does not need the entire client catalog
- placeholders are acceptable for icons not yet curated
- `LinkedItemCount` and `SampleItemNames` remain honest fallback data for now

## Output contract

```ts
{
  iconId: number;
  previewPath: string | null;
}
```

This output is intentionally small so Phase 7 can reuse the selector inside `Create`, `Edit`, and `Duplicate` without reimplementing icon lookup logic in Angular.

## Validation status

- `1001.png` is discoverable in the current curated catalog
- the selector is read-only and does not touch DB
- API errors keep `traceId` visible through the shared problem panel
- missing previews still show a placeholder instead of faking data

## Out of scope

- full `Create/Edit`
- `44k` record audit
- weapon write rules
- mass asset extraction
- `D2P` extraction
- `SWF` extraction
- upload
- publish
- gameplay changes

## Next phase handoff

After this slice, the next functional target remains:

`Phase 7 - Item Create/Edit`

That phase should consume the selector result instead of asking operators to type `IconId` manually.
