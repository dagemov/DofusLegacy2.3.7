# Items Publish Decision Workflow

Date: `2026-06-03`

## Goal

When an operator creates or edits an item, the tool must immediately answer:

```txt
Is this item already client-known?
Or does it need a client patch?
```

## Decision tree

### A. Existing client template

Choose this path when:

- `ItemId` already exists in `Client2.3.7/data/common/Items.d2o`
- the client already knows the template
- the operator is intentionally reusing a shipped identity

Expected result:

- item can be visible immediately
- vendor publication can work immediately
- inventory render can work immediately

Typical example:

- `7754 / Dofus Ocre`

### B. Custom server-only template

Choose this path when:

- `ItemId` exists only in `sunshine.items`
- `Items.d2o` does not know the template id

Expected result:

- the item can exist in DB
- the item can be granted or sold server-side
- the client still cannot render it until the template is published

Typical example:

- `12617 / Dofus Tester`

## Approved strategies

### Strategy 1 - Reuse an existing client template

Use this for immediate QA when:

- the operator only needs a visible control case
- exact client identity is not required yet

Trade-off:

- the visible name remains the shipped client name
- this is not a real custom publish

### Strategy 2 - Publish a new client template

Use this when:

- the visible identity matters
- the template must be its own item

Required client files:

- `Client2.3.7/data/common/Items.d2o`
- `Client2.3.7/data/i18n/i18n_es.d2i`
- `Client2.3.7/data/i18n/i18n_en.d2i`
- launcher patch lane / published payload

Conditional:

- icon asset packs if a new icon is required
- appearance metadata if a new equipped look is required

## Tooling rule for the Admin UI

The UI should classify every item as one of these modes:

- `CLIENT_PUBLISHED`
- `SERVER_ONLY`
- `CARRIER_TEMPLATE_FALLBACK`

And it should warn explicitly when:

- `ItemId` is unknown to the client
- `IconId` exists but template publication is still missing
- the operator is using a carrier template instead of a real publish

## Recommended operator flow

1. Create or edit the runtime row in Admin.
2. Open `Item Publication Status`.
3. If the item is `CLIENT_KNOWN`, continue with QA and vendor workflow.
4. If the item is `CLIENT_UNKNOWN`, stop calling it visible and move to the publication pipeline.
5. Only after publication should the item be declared deliverable.
