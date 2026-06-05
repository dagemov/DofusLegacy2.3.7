# Items Builder Options Loading Fix

## Snapshot

- Date: `2026-06-02`
- Status: `DONE`

## Problem statement

Operators reported three related symptoms:

1. catalog requests looked successful at HTTP level but the UI still felt empty or broken
2. `Choose Type` appeared to load no options
3. `Item Set` appeared to load no options

## Root findings

### Catalog

The catalog endpoint itself was already returning data in the local working environment:

- `GET /api/admin/v1/items?page=1&pageSize=20`

The most likely operator-facing confusion came from:

- generic or technical error text
- parse-failure messaging that did not explain what actually happened
- screens that did not make empty or fallback states obvious

### Choose Type

`Choose Type` is not a DB query against Sunshine.

Actual source:

- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Items/AdminProtocolCatalog.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.Protocol/Enums/ItemTypeEnum.cs`

Route:

- `GET /api/admin/v1/items/types/options`

Meaning:

- if this combo is empty, the likely issue is not `items` table data
- the likely issue is API wiring, enum loading, or frontend mapping

### Item Set

`Item Set` is DB-backed.

Actual source:

- `sunshine.items_sets`

Route:

- `GET /api/admin/v1/item-sets/options`

Meaning:

- if the table has rows, the combo should populate
- if the table is empty, the UI should stay stable and explain that no sets are available

## Fixes applied in this phase

- safer Spanish `ProblemDetails` messages
- clearer Angular parse/network failure mapping
- clearer list-page and write-page labels
- write-page hint when the item-set options list is empty
- explicit documentation that type options come from protocol enums, not from DB rows

## Expected operator behavior now

### If the catalog is healthy

The operator sees:

- real catalog rows
- clear pagination
- real type labels
- no fake "request could not be complete" wording

### If the API fails

The operator sees:

- a Spanish human message
- `traceId`
- enough context to escalate without guessing

### If there are no item sets

The operator sees:

- `Sin set`
- a non-breaking hint that the environment returned no set options

## Validation checklist

- `GET /api/admin/v1/items?page=1&pageSize=20`
- `GET /api/admin/v1/items/types/options`
- `GET /api/admin/v1/item-sets/options`
- `/admin/items`
- `/admin/items/new`
- `/admin/items/39/edit`
