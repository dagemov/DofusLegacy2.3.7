# Items Client Asset Audit Phase 6.5A

## Snapshot

- Date: `2026-06-02`
- Status: `DONE`
- Scope type: documentation and path audit only
- Official repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`

## Goal

Audit where item client identity data and preview assets actually live before full `Create/Edit` continues.

This phase does not:

- extract SWF
- extract D2P
- commit mass PNGs
- implement upload
- unblock full write workflow by itself

## Verified findings

### Legacy client sources

Verified under `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\client\app\`:

- `data/common/Items0.swf` through `Items10.swf`
- `data/common/ItemSets0.swf`
- `data/common/ItemTypes0.swf`
- `data/i18n_es/i18n*.swf`
- `data/i18n_en/i18n*.swf`
- `data/i18n_es/tmp/i18n*.as`
- `data/i18n_en/tmp/i18n*.as`
- `content/gfx/items/bitmap/*.png`

### Current client sources in the official repo

Verified under `Client2.3.7/`:

- `data/common/Items.d2o`
- `data/common/ItemSets.d2o`
- `data/common/ItemTypes.d2o`
- `data/i18n/i18n_es.d2i`
- `data/i18n/i18n_en.d2i`
- `content/gfx/items/bitmap0.d2p`
- `content/gfx/items/bitmap1.d2p`
- `content/gfx/items/vector0.d2p`
- `content/gfx/items/vector1.d2p`

### Legacy Blazor asset folder

Verified under `Rollback.Web/wwwroot/admin-assets/items`:

- the folder is not the canonical client source
- it contains `38` manually curated PNG files
- naming pattern is `{entityId}-{utcTimestamp}.png`

### Identity separation confirmed

The audit keeps these fields separate:

- `ItemId`: runtime or DB template identity
- `IconId`: inventory preview identity
- `AppearanceId`: equipped or look identity
- `ClientNameId`: multilingual client text anchor

## Practical conclusion

The future asset-intelligence lane should target structured client data first:

1. `d2o` for item templates and relations
2. `d2i` for multilingual names
3. curated PNG cache for admin previews
4. only later, if still needed, deeper binary extraction research

`Phase 7` stays paused while the repo structure is corrected, and the next focused UI step becomes `Phase 7A - Item Icon Selector`.
