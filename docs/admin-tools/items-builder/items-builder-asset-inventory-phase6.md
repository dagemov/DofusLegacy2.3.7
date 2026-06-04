# Items Builder Asset Inventory Phase 6

> Status correction on `2026-06-02`: this document records exploratory work from a parallel branch/worktree. It is kept here as reference history only until the same work is replayed inside the official repo baseline.

## Snapshot

- Date: `2026-06-01`
- Branch: `feature/items-builder-asset-pipeline-phase6`
- Scope type: preview PNG inventory only

## Inventory

| SourcePath | FileCount | NamingPattern | LooksCurated | LooksGenerated | Decision |
| --- | ---: | --- | --- | --- | --- |
| `DofusBeta-2.0/Dofus-2/Rollback/Rollback.Web/wwwroot/admin-assets/items` | 38 | `{entityId}-{utcTimestamp}.png` | Yes | No | `CURATED_ASSET` |
| `DofusBeta-2.0/Dofus-2/Rollback/Rollback.Web/wwwroot/assest-img` | 4 | numeric decorative sprite names | No | No | `REFERENCE_ONLY` |
| `DofusBeta-2.0/Dofus-2/Rollback/Rollback.Web/wwwroot/assest-img/fondos` | 4 | themed portal/background art | No | No | `IGNORE` |
| `DofusBeta-2.0/Dofus-2/client/app/content/gfx/items/bitmap` | large local bitmap pack | `{iconId}.png` | Yes | Exported client pack | `CURATED_ASSET` for exact one-file seeds only |

## Interpretation

### `Rollback.Web/wwwroot/admin-assets/items`

This is the strongest evidence for the old manual-preview flow:

- operator-managed PNGs
- item-like naming using the admin entity id
- timestamped filenames from upload time
- good reference for future manual asset storage rules

This folder was **not** mass-copied into the new repo.

### `Rollback.Web/wwwroot/assest-img`

These PNGs are portal and decorative sprites:

- login/register/home ornaments
- hero art and decorative creatures
- not item preview assets

They are useful as visual references only and should not enter the Items preview pipeline.

### Adjacent client bitmap pack

The old Blazor preview service also resolved real item previews from a local client bitmap directory:

```txt
client/app/content/gfx/items/bitmap/{iconId}.png
```

That pack is not a manual upload folder, but it is the correct exact-match source for `IconId`-based preview validation.

For this phase, one exact seed was selected from that pack:

- source file: `DofusBeta-2.0/Dofus-2/client/app/content/gfx/items/bitmap/1001.png`
- target file: `src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/1001.png`
- purpose: validate `itemId=39` and the shared `IconId=1001` family without copying a bulk client dump

## Decisions

- `CURATED_ASSET`
  - exact one-file seeds taken intentionally and documented
  - no more than `1-3` PNGs should be introduced per focused validation phase
- `GENERATED_ARTIFACT`
  - reserved for future temp exports, dumps, or generated icon batches
  - none were imported in this phase
- `REFERENCE_ONLY`
  - useful for design/behavior context but not for the live preview pipeline
- `IGNORE`
  - irrelevant to item previews or risky to copy

## Phase 6 conclusion

The asset inventory confirms that the old stack had two distinct preview sources:

1. manual admin uploads under `wwwroot/admin-assets/items`
2. exact `IconId` previews from a local client bitmap pack

The new repo should keep those roles separate:

- manual overrides under `manual-assets/items`
- exact bitmap seeds under `item-previews/by-icon`
- no mass copy from either source
