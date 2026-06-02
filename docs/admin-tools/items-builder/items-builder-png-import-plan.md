# Items Builder PNG Import Plan

## Snapshot

- Date: `2026-06-02`
- Status: `FIRST CONTROLLED IMPORT DONE`

## Goal

Expand the curated preview catalog without importing weapons, without mass-copying client assets, and without turning the repo into a bitmap dump.

## Current destination

Official tracked preview root:

`Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/`

## Source audit summary

Approved first-wave categories from the legacy client pack:

- `amuletos_png`
- `sombreros`
- `capas`
- `botas`
- `Dofus`
- `mascotas`

Excluded in this phase:

- `weapons`
- `armas`
- `44k` records
- `anillos`
- `cinturones`

Why `anillos` and `cinturones` stay blocked:

- the old source mirrors filenames exactly between both folders
- that makes the category mapping low-confidence

## Import script

Script created:

- `scripts/item-preview-import/import-item-previews.ps1`

Safety rules in the script:

- only approved categories
- blocked categories throw immediately
- default run is report-only
- no overwrite unless explicitly requested
- report is written under `docs/admin-tools/items-builder/reports/`

## First controlled import

Imported batch:

- category: `amuletos_png`
- `IconId`: `1002` through `1012`

Kept existing seed:

- `1001.png`

Resulting tracked preview files:

- `1001.png`
- `1002.png`
- `1003.png`
- `1004.png`
- `1005.png`
- `1006.png`
- `1007.png`
- `1008.png`
- `1009.png`
- `1010.png`
- `1011.png`
- `1012.png`

Reports generated:

- [dry run report](./reports/item-preview-import-dry-run-phase-stabilization.md)
- [apply report](./reports/item-preview-import-apply-phase-stabilization.md)

## Suggested validation case

Reference case:

- `itemId=74`
- expected `IconId=1005`

Before this import:

- preview state should be `MISSING`

After this import:

- preview state should become `FOUND`

## Next safe expansion

Possible next waves after review:

1. extend `amuletos_png` beyond `1012`
2. curate a first `sombreros` subset
3. curate a first `capas` subset

Do not do next waves blindly:

- require report first
- keep batch size small
- document source path and reason

## Explicit non-goals

- no SWF extraction
- no D2P extraction
- no PDF conversion
- no manual upload UI in this phase
- no publish-to-client workflow in this phase
