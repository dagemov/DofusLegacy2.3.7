# Items Client I18N Audit

## Goal

Document the bilingual name sources required for future item operator identity.

## Verified language sources

Legacy client:

- `DofusBeta-2.0/Dofus-2/client/app/data/i18n_es/i18n*.swf`
- `DofusBeta-2.0/Dofus-2/client/app/data/i18n_en/i18n*.swf`
- `DofusBeta-2.0/Dofus-2/client/app/data/i18n_es/tmp/i18n*.as`
- `DofusBeta-2.0/Dofus-2/client/app/data/i18n_en/tmp/i18n*.as`

Current official repo client:

- `Client2.3.7/data/i18n/i18n_es.d2i`
- `Client2.3.7/data/i18n/i18n_en.d2i`

## Language decisions

- Spanish remains the default operator-facing language
- `ClientNameId` remains mandatory in contracts
- future identity projection should support `NameEs`, `NameEn`, `ClientNameId`, `SourceFile`, and `Confidence`
- manual translation of thousands of items is explicitly out of scope

## Future extractor rule

If name extraction is built later, it should:

1. resolve the same `ClientNameId` in ES and EN
2. preserve missing-language cases explicitly
3. keep source-file provenance
4. never overwrite operator data with guessed text silently
