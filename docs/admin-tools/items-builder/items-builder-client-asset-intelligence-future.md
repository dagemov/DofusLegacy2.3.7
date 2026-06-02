# Items Builder Client Asset Intelligence Future

> Roadmap note on `2026-06-02`: the audit portion of this future lane is now captured in `Phase 6.5A`, and the official next build-facing step is `Phase 7A - Item Icon Selector`, not full `Create/Edit`.

## Snapshot

- Date: `2026-06-01`
- Planned label: `Future Phase 6.5`
- Current baseline branch when documented: `feature/items-builder-asset-pipeline-phase6`
- Scope type: future research and extraction plan only

## Purpose

Capture the next layer of Items preview and naming intelligence without delaying `Phase 7 / 8 - Item Create/Edit`.

This future phase exists to answer where item visuals and multilingual client names really come from, and how to expose that information to operators without requiring Navicat.

## What this future phase should solve

1. Identify the authoritative client asset sources for item icon previews.
2. Clarify the exact relationship between:
   - `ItemId`
   - `IconId`
   - `AppearanceId`
   - `ClientNameId`
3. Build a clean operator-facing identity projection that can expose:
   - `NameEs`
   - `NameEn`
   - `ClientNameId`
   - `SourceFile`
   - `Confidence`
4. Reduce the need for manual SQL lookup when an operator needs to understand what an item really is.

## Questions to answer later

### Real client asset source

Investigate:

- `Items*.swf`
- `i18n_*.swf`
- old client pack directories
- legacy PNG caches and export folders
- any `.as` or supporting metadata source that explains item icon resolution

Concrete questions:

- Does the inventory icon come from `Items*.swf`?
- Does equipped appearance come from another SWF or client source?
- Does `IconId=1001` map directly to the real PNG used by the client?
- Is `AppearanceId` only for equipped visuals, not inventory icon lookup?
- What is the exact difference between `IconId` and `AppearanceId` for admin operators?

### Extraction format

Preliminary decision:

- for Admin preview, PNG cache is sufficient
- do not mass-convert SWFs in the short term
- do not use PDF as an intermediate format
- if extraction is needed, it must be `SWF -> PNG`
- bulk outputs must go to temporary locations only
- only tiny curated PNG subsets may enter the repo

### Language intelligence

Preliminary decision:

- keep `ClientNameId` as a stable technical reference
- allow future projections for `NameEs` and `NameEn`
- prefer Spanish as the primary operator-facing name if the server remains Spanish-first
- do not hand-translate thousands of items manually

Future extractor goals:

- compare Spanish and English client sources
- emit `ClientNameId`, `NameEs`, `NameEn`, `SourceFile`, and `Confidence`
- preserve uncertainty explicitly when the client metadata is incomplete

## Suggested future outputs

- client asset source inventory
- icon-resolution decision table
- `IconId` to PNG cache strategy
- bilingual name projection strategy
- safe temp-output script plan under ignored directories
- future DTO extension proposal for multilingual client identity

## Suggested safe workspace rules

- temporary extraction outputs go under ignored folders such as `Infrastructure/temporal-artifacts/`
- no mass PNG commits
- no SWF binaries copied into tracked admin folders
- no generated PDFs
- no manual translation campaign for item names

## Explicit non-goals for this document

- no SWF extraction now
- no code changes now
- no asset mass-copy now
- no i18n parser now
- no delay to `Phase 7 / 8 - Item Create/Edit`

## Relationship to nearby phases

- Phase 6 proved that curated preview seeds work
- Phase 6.5A now documents where the client sources actually live
- Phase 7A should use the curated icon catalog before full write workflow resumes
- full Phase 7 create/edit remains paused until the official repo catches up in-place
