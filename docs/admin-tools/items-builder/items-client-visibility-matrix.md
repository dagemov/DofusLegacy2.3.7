# Items Client Visibility Matrix

Date: `2026-06-03`  
Branch: `feature/items-builder-vps-qa-stabilization`

## Goal

Make the publication rule explicit:

- `ItemId` decides whether the client can render the template.
- `IconId` helps inventory preview and identity, but does not publish a template.
- `AppearanceId` helps equipped look checks, but does not publish a template.

## Matrix

| ItemId | ResolvedName | DescriptionId | IconId | AppearanceId | Present in `Items.d2o` | Current client result | Matrix state | Notes |
| --- | --- | ---: | ---: | ---: | --- | --- | --- | --- |
| `7754` | `Dofus Ocre` | `40905` | `23012` | `0` | `YES` | visible | `VISIBLE` | control case; client-known template |
| `12616` | `ADMIN TEST` | `50090` | `1003` | `1004` | `NO` | not guaranteed visible today | `VISIBLE CON PARCHE` | runtime + preview are valid, but the template is still unknown to the client |
| `12617` | `Dofus Tester` | `50091` | `23012` | `0` | `NO` | invisible today | `VISIBLE CON PARCHE` | exact identity requires client publication |

## Evidence

### DB / Admin runtime

- `12616` is a real runtime row used for Create/Edit QA.
- `12617` is a real runtime row created as `Dofus Tester`.
- `7754` is the current visible fallback because the client already ships it.

### Client metadata

Confirmed by read-only audit of:

```txt
Client2.3.7/data/common/Items.d2o
```

Observed:

```txt
7754   -> present
12616  -> missing
12617  -> missing
```

## Interpretation

### `VISIBLE`

Use when:

- the template exists in `Items.d2o`
- the client can already resolve `objectGID = ItemId`

### `VISIBLE CON PARCHE`

Use when:

- the server-side item is coherent enough to ship
- but the client still lacks the template id
- a client patch would likely make the item visible without redesigning runtime identity

### `INVISIBLE`

Use when:

- the item is missing client publication
- and runtime identity is too incomplete to even prepare a clean patch

Examples:

- missing `IconId`
- missing operator-facing identity
- missing preview and no curated asset path

## Operational rule

Never call an item "visible" just because:

- it exists in `sunshine.items`
- it exists in `characters_items`
- an NPC vendor can reference it

The deciding question is:

```txt
Does Client2.3.7 know this ItemId as a published template?
```

If the answer is `no`, the item is not client-visible yet.
