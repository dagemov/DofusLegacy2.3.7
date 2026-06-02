# Items Builder Asset Pipeline

> Roadmap note on `2026-06-02`: `Phase 6` is `DONE`, `Phase 6.5A` is now documented, and the next official UI slice is `Phase 7A - Item Icon Selector`. Full `Create/Edit` remains paused in the official roadmap.

## Purpose

Define how item preview PNGs should be handled during the Angular + API migration without polluting the repo or confusing preview assets with client publish assets.

As of `2026-06-01`, this document also reflects the first live curated preview seed delivered in Phase 6.

## Asset categories

| Category | Source | Intended use | Git policy | Decision |
| --- | --- | --- | --- | --- |
| Curated preview assets | operator upload, hand-picked admin asset, or exact one-file icon seed | preview and identity support | keep tiny and intentional; tracked only when explicitly curated | allowed |
| Local client bitmaps | `client/app/content/gfx/items/bitmap/*.png` | read-only validation and preview reference | never commit from local client packs | allowed as local reference only |
| Legacy sample admin assets | `Rollback.Web/wwwroot/admin-assets/items` | reference for naming, quality, and expected operator behavior | do not mass-copy | reference only |
| Curated repo preview seeds | `src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/*` | official small tracked preview seeds | allowed only in tiny reviewed batches | allowed |
| Bulk client dumps or exports | local folders, generated exports, temp batches | none for Phase 1 | never commit | blocked |
| Build artifacts | `bin/`, `obj/`, `artifacts`, generated temp outputs | none | never commit | blocked |

## Key rules

1. No mass PNG commits.
2. No direct copy of whole asset folders from legacy repos.
3. Only tiny curated preview seeds may live in tracked source folders.
4. Client bitmaps are read-only validation inputs, not source-controlled assets.
5. Future client publish workflows must use a canonical `.png`.

## Upload format policy

Legacy behavior allowed:

- `.png`
- `.jpg`
- `.jpeg`
- `.webp`

Phase 1 recommendation:

- allow those formats only if the API clearly distinguishes preview staging from publish-ready output
- normalize the canonical stored preview asset to `.png` before any future publish workflow
- if normalization is not implemented yet, restrict manual upload to `.png` in the first live version

## Storage recommendation

Preferred order:

1. tracked curated seed roots for a tiny reviewed subset only
2. configurable local filesystem root outside the repo for future operator uploads
3. configurable folder under the future admin API webroot that is explicitly ignored by Git

Avoid:

- bulk checked-in preview folders
- storing uploads under current website public asset folders
- mixing manual preview storage with local client bitmap packs

## Metadata recommendation

The future admin API should store metadata separately from the binary asset itself:

```txt
entityType = item
entityId = {itemId}
assetKind = preview-png
relativePath
canonicalFormat
createdUtc
updatedUtc
source = manual-upload | migrated-reference
```

## Preview resolution order

Current read order:

1. explicit manual preview asset if present
2. resolved client bitmap by `ClientIconId`
3. reference bitmap by approved lookup rules
4. placeholder/no-preview state with a diagnostic warning

Phase 7 write-form note:

- pre-save preview is intentionally evaluated by `IconId`
- write forms must not trust a stale by-item preview when the operator is changing the icon
- by-item preview remains most useful after save or in read-only detail contexts

## Read-only path conventions

The current read-only API exposes deterministic preview candidates such as:

```txt
/assets/item-previews/by-item/{itemId}.png
/assets/item-previews/by-icon/{iconId}.png
/manual-assets/items/{itemId}.png
```

Current official workspace structure:

```txt
src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-item/
src/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/
src/Admin/RollblackLegacy.Admin.Angular/src/assets/manual-assets/items/
```

Read-only rule:

- the API returns preview state and candidate paths only
- the pipeline may carry a tiny curated seed set
- this phase still does not copy or generate bulk PNGs

Phase 6 live note:

- the Angular detail page now consumes those candidate paths as operator-facing debug values
- the UI now also shows `previewSource`, `resolvedPath`, and `fallbackUsed`
- `itemId=39` is validated through curated seed `by-icon/1001.png`
- when the current host cannot reach a preview or no curated file exists, the UI shows a non-blocking placeholder instead of pretending the asset exists
- the Phase 7 write form now reuses the same preview card, but drives it from the current `IconId` before save

## Preview state vocabulary

The API standardizes these preview states:

```txt
FOUND
MISSING
MANUAL
UNKNOWN
```

Current interpretation:

- `FOUND`: a deterministic preview path resolves successfully
- `MISSING`: a deterministic preview path was expected but the file is absent
- `MANUAL`: a manual asset exists and wins over derived preview paths
- `UNKNOWN`: the current repo has no confirmed persisted metadata or local file proof to resolve a preview

Additional Phase 6 metadata:

- `previewSource = MANUAL | BY_ITEM | BY_ICON | PLACEHOLDER`
- `resolvedPath = concrete logical path or null`
- `fallbackUsed = NONE | BY_ICON | PLACEHOLDER`

## Cleanup policy

- replacing a manual asset should deprecate the previous metadata entry
- cleanup of orphaned preview files should be explicit and scriptable
- no automatic deletion should happen from browser-only actions without backend ownership checks

## Validation checklist for future implementation

- confirm storage root is outside tracked files or ignored
- confirm uploaded format policy is enforced
- confirm preview URL cannot escape the intended asset root
- confirm no production-like client files are overwritten
- confirm publish workflows reject non-PNG canonical assets

## Phase 6 conclusion

The asset pipeline is now live as a safe preview pipeline, not a client publish pipeline. A single curated `by-icon` seed proves the path end to end, while the rest of the catalog still falls back cleanly until more assets are intentionally curated.

## Future Phase 6.5 note

The next research concern is no longer "can the preview render?" but "how do we explain and enrich client identity correctly?".

Deferred to the future:

- audit `Items*.swf`
- audit `i18n_*.swf`
- confirm exact `IconId` versus `AppearanceId` behavior
- compare Spanish and English client names
- design a clean `ClientNameId` + `NameEs` + `NameEn` projection

Reference:

- [items-builder-client-asset-intelligence-future.md](./items-builder-client-asset-intelligence-future.md)
- [items-client-asset-audit-phase6-5a.md](./items-client-asset-audit-phase6-5a.md)
- [items-builder-phase7a-item-icon-selector.md](./items-builder-phase7a-item-icon-selector.md)
