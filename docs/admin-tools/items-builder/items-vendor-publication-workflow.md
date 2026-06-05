# Items Vendor Publication Workflow

Date: `2026-06-03`

## Goal

Define when a created item is ready to be sold by NPC vendor without pretending that vendor stock alone makes the item visible.

## Vendor data path

Current audited live path:

- NPC template row
- `npcs_items`
- `Token = 0` means kamas

Audited seller:

- `NpcId = 1053`
- `Vendeur de Dofus`

## Required checks before vendor insertion

1. The runtime item exists in `sunshine.items`.
2. The Admin detail page resolves identity and preview coherently.
3. `Item Publication Status` does not say `CLIENT_UNKNOWN` for the exact template being sold.
4. The price and currency are explicit.
5. Existing vendor stock is preserved unless a cleanup is intentional and documented.

## Safe vendor outcomes

### Vendor-ready now

- template is client-known
- the client can render the `ItemId`
- selling the item will display correctly in shop and inventory

### Vendor-ready only after client patch

- runtime row exists
- but `Items.d2o` still does not know the template id

This is the current status of:

- `12617 / Dofus Tester`

### Carrier-template vendor fallback

- a client-known template is reused to make QA possible now
- this is operationally useful
- but it does not publish the original custom template

This is the current status of:

- `7754 / Dofus Ocre` carrying tester stats

## Rule

Do not say:

```txt
The item is published because the vendor sells it.
```

The correct statement is:

```txt
The vendor can sell the item only if the client already knows the template id being sold.
```
