# Items Builder Phase 7A - Item Icon Selector

## Snapshot

- Date: `2026-06-02`
- Status: `NEXT`
- Scope type: design and planning before full Item `Create/Edit`

## Goal

Define the first focused UI slice that helps operators choose the correct item icon before full write workflows resume.

## Why this comes before full Phase 7

- the repo source-of-truth had to be realigned first
- operators need a reliable icon-picking flow before broader create/edit
- the current curated preview catalog already proves the `IconId -> PNG` path
- this step is smaller and safer than reopening full item writes immediately

## Component target

`ItemIconSelectorComponent`

## Minimum responsibilities

- render preview PNG when available
- show `IconId`
- show client-facing name when available
- support search
- support selection
- keep `ItemId != IconId != AppearanceId` explicit in the surrounding UX

## Initial catalog source

Use the existing preview catalog under:

```txt
/assets/item-previews/by-icon
```

Initial expectations:

- the component may start from a small curated manifest
- the first pass does not need the entire client catalog
- placeholders are acceptable for icons not yet curated

## Inputs and outputs

Inputs:

- current selected `IconId`
- optional search text
- optional client name lookup data

Outputs:

- selected `IconId`
- selected preview path
- selected client label metadata if available

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

## Suggested follow-up branch

`feature/items-builder-icon-selector-phase7a`

## Expected commit

`docs: define item icon selector phase`
