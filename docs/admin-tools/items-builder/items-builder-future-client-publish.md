# Future Client Publish Workflow

## Status

- Status: `FUTURE`
- Implemented in Phase 8: `NO`

## Purpose

Document the future client publish lane without implementing it yet.

## Future publish flow

1. Confirm the item passed Admin QA.
2. Confirm `ItemId`, `IconId`, `AppearanceId`, and `ClientNameId` mapping.
3. Confirm whether the item needs new client i18n data or only server-side row changes.
4. Export or update the required client metadata source.
5. Validate `IconId` against curated preview assets.
6. Validate equipped/runtime identity for `AppearanceId`.
7. Copy only curated preview assets required for the release.
8. Package the target client build.
9. Test the item in the target client.
10. Keep a rollback package for metadata and assets.

## Explicit non-goals in current implementation

Phase 8 does **not** do any of the following:

- SWF extraction
- D2P extraction
- mass asset import
- real client publish
- automatic i18n export
- automatic description publish
- automatic `IsVisible` persistence

## Why publish stays deferred

The current `sunshine.items` row is enough to drive Admin CRUD and QA readiness, but it is not enough to guarantee a safe client publish workflow.

Main gaps:

- description text is represented as `DescriptionId`, not as a managed client-language payload
- no persisted workflow field exists for publish state
- no upload or approval flow exists for curated item preview assets
- no packaged client metadata updater is in place yet

## Prerequisites for a future publish phase

- explicit workflow persistence design
- i18n extraction/update plan
- asset promotion rules
- rollback plan
- QA sign-off criteria
