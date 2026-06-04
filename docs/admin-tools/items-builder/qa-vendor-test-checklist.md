# QA Vendor Test Checklist

Use this checklist for visible-item QA through an NPC vendor.

## Before the test

- [ ] Confirm the target item is `CLIENT_PUBLISHED` or an approved `CARRIER_TEMPLATE_FALLBACK`
- [ ] Confirm the vendor NPC id is correct
- [ ] Confirm the item is not already duplicated in the vendor stock
- [ ] Confirm currency mode:
  - [ ] `Token = 0` for kamas
  - [ ] non-zero token only when intentional
- [ ] Confirm the target price is documented

## DB validation

- [ ] `npcs_items` contains the expected row
- [ ] The target item template exists in `items`
- [ ] The target template id is client-known for the current test goal
- [ ] Any fallback carrier identity is explicitly documented

## In-game validation

- [ ] Open the NPC shop
- [ ] Confirm the item appears in the shop list
- [ ] Confirm the displayed price is correct
- [ ] Buy one unit
- [ ] Confirm the bought item appears in inventory
- [ ] Confirm the icon renders
- [ ] Confirm the visible name is the expected one
- [ ] Relog and confirm the item still appears
- [ ] Equip if applicable and confirm the runtime stats

## After the test

- [ ] Record whether the test used a real published template or a carrier fallback
- [ ] Record whether the vendor route is safe for repetition
- [ ] Clean up any temporary QA-only data if required

## Reminder

Selling a template through an NPC does **not** make that template client-visible by itself.

The shop can only display what the client already knows how to render.
