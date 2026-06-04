# Items Builder Phase 8 - Publish + QA Workflow

## Status

- Phase: `8 / 8`
- Status: `DONE`
- Branch: `feature/items-builder-publish-qa-phase8`
- Scope: safe QA readiness + publication diagnostics workflow

## Goal

Close the Items Builder MVP with a safe operator workflow that answers:

- is this item ready for QA?
- what is blocking QA right now?
- what should the operator verify in game?
- what still prevents real client publish?

This phase does **not** publish client data, modify SWF/D2P assets, or add schema fields.

It does make the publication gap explicit, so operators can tell the difference between:

- item exists only in DB
- item is client-known
- item still needs a client patch

## Delivered API

- `GET /api/admin/v1/items/{itemId}/qa-summary`
- `GET /api/admin/v1/items/{itemId}/publication-status`

The endpoint is read-only and derived from the current item detail plus preview diagnostics.

Returned fields:

- `itemId`
- `resolvedName`
- `type`
- `level`
- `iconId`
- `appearanceId`
- `previewState`
- `warnings`
- `workflowState`
- `canQa`
- `canPublish`
- `blockingReasons`
- `recommendedChecks`

## Workflow model

There is no persisted workflow field today in `sunshine.items`.

Audited fields that do **not** exist in the current schema:

- `IsVisible`
- `IsPublished`
- `IsCustom`
- `IsTestOnly`
- `AdminStatus`
- `PublishedAt`
- `UpdatedAt`
- `CreatedAt`

Because of that, Phase 8 uses a **virtual derived workflow state** instead of a write model.

Current derived states:

- `READY_FOR_QA`
- `BLOCKED`

Future states remain documented for later persistence work:

- `DRAFT`
- `QA_PASSED`
- `PUBLISH_PENDING`
- `PUBLISHED`

## Angular panel

Item detail now includes a `QA / Publish Readiness` panel with:

- workflow status badge
- `Can QA`
- `Can publish`
- preview readiness
- identity readiness
- blocking reasons
- recommended manual checks
- `Copy QA checklist`
- disabled `Mark ready for QA`
- visible manual-copy fallback when clipboard APIs are limited

The panel is intentionally read-only in this phase.

Phase 8 also adds a dedicated route:

```txt
/admin/items/:itemId/publication-status
```

That screen shows:

- `Client Known`
- `Client Unknown`
- `Published`
- `Needs Client Patch`
- `Needs Asset`
- `Needs QA`

with live evidence from:

- runtime item detail
- QA blockers
- read-only audit of `Client2.3.7/data/common/Items.d2o`

## Blocking rules

QA is blocked when any of these conditions are true:

- `ResolvedName` missing
- unknown `TypeId`
- missing or invalid `IconId`
- preview not resolved as `FOUND` or `MANUAL`

Publish remains blocked by design even when QA is ready.

Current publish blockers:

- real client publish is disabled in Phase 8
- description publish is deferred because `sunshine.items` stores `DescriptionId`, not client i18n payload
- `IsVisible` persistence is deferred because there is no direct schema field
- equipped/runtime validation remains incomplete when `AppearanceId <= 0`

## QA command audit

Existing Sunshine command:

- file: `Sunshine.WorldServer/Commands/Administrator/ItemCommand.cs`
- handler: `item`
- role: `Moderator`
- syntax: `.item add <itemId> <quantity> [CharacterName]`

Risk:

- this mutates live inventory state for a target character
- use only in a controlled QA environment or with explicit cleanup discipline

## Result

Items Builder MVP is now functionally complete for:

- list
- detail
- diagnostics
- preview
- icon selector
- create
- edit
- duplicate
- QA readiness
- publication diagnostics

## Supporting documents added in Phase 8

- `items-client-visibility-matrix.md`
- `items-publish-decision-workflow.md`
- `items-admin-command-catalog.md`
- `items-vendor-publication-workflow.md`
- `items-production-qa-checklist.md`

Still deferred after Phase 8:

- real client publish
- client i18n export/import
- `IsVisible` persistence
- description publish payload
- PNG upload
- weapon-specific workflow
