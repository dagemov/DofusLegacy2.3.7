# Items Client Asset Source Inventory

## Purpose

List the client-side sources that matter for future item identity and preview intelligence.

## Source inventory

| Source family | Verified location | Data type | Planned use | Status |
| --- | --- | --- | --- | --- |
| Legacy item template SWFs | `DofusBeta-2.0/Dofus-2/client/app/data/common/Items*.swf` | legacy structured data | reference-only audit | verified |
| Legacy item set SWF | `DofusBeta-2.0/Dofus-2/client/app/data/common/ItemSets0.swf` | legacy structured data | reference-only audit | verified |
| Legacy item type SWF | `DofusBeta-2.0/Dofus-2/client/app/data/common/ItemTypes0.swf` | legacy structured data | reference-only audit | verified |
| Legacy i18n SWFs | `DofusBeta-2.0/Dofus-2/client/app/data/i18n_es/i18n*.swf`, `.../i18n_en/i18n*.swf` | localized client text | bilingual name audit | verified |
| Legacy unpacked i18n AS files | `DofusBeta-2.0/Dofus-2/client/app/data/i18n_es/tmp/*.as`, `.../i18n_en/tmp/*.as` | decompiled text chunks | text-source inspection | verified |
| Legacy item bitmaps | `DofusBeta-2.0/Dofus-2/client/app/content/gfx/items/bitmap/*.png` | inventory PNG previews | curated preview seed source | verified |
| Current repo item templates | `Client2.3.7/data/common/Items.d2o` | structured data | future official extractor input | verified |
| Current repo item sets | `Client2.3.7/data/common/ItemSets.d2o` | structured data | future official extractor input | verified |
| Current repo item types | `Client2.3.7/data/common/ItemTypes.d2o` | structured data | future official extractor input | verified |
| Current repo i18n | `Client2.3.7/data/i18n/i18n_es.d2i`, `i18n_en.d2i` | localized client text | future bilingual identity projection | verified |
| Current repo item bitmap packs | `Client2.3.7/content/gfx/items/bitmap0.d2p`, `bitmap1.d2p` | packed preview assets | future extraction candidate, not now | verified |
| Current repo item vector packs | `Client2.3.7/content/gfx/items/vector0.d2p`, `vector1.d2p` | packed vector assets | future extraction candidate, not now | verified |
| Legacy Blazor manual previews | `Rollback.Web/wwwroot/admin-assets/items/*.png` | manual operator previews | reference for admin behavior only | verified |

## Decision

For near-term admin UX:

- use curated PNGs for preview
- use `IconId` as the preview lookup key
- keep `AppearanceId` separate for future equipped-look work
- keep `ClientNameId` as the anchor for multilingual identity

For future extraction:

- prefer `D2O + D2I + curated PNG cache`
- do not start with mass `SWF` or `D2P` extraction
