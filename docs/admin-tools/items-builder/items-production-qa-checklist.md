# Items Production QA Checklist

Date: `2026-06-03`

Use this checklist after Create/Edit and before calling an item production-ready.

## Runtime / Admin

- [ ] Create item works from Admin
- [ ] Edit item works from Admin
- [ ] Detail route loads without error
- [ ] Preview route resolves or the missing state is documented
- [ ] Warnings are understood, not ignored blindly
- [ ] `ItemId`, `IconId`, and `AppearanceId` are explicitly different when expected

## Publication / client visibility

- [ ] `Item Publication Status` reviewed
- [ ] `Client Known` is confirmed for the exact template being shipped
- [ ] if `Client Unknown`, the item is not claimed as visible yet
- [ ] required client patch files are identified
- [ ] launcher / delivery lane is accounted for

## Inventory / character

- [ ] give item path chosen (`command` vs `SQL`)
- [ ] item appears in inventory
- [ ] item tooltip resolves
- [ ] item name is correct
- [ ] item icon is correct
- [ ] stack / quantity shape is acceptable

## Equipment / gameplay-facing checks

- [ ] item can be equipped if it should be equipable
- [ ] slot behavior is correct
- [ ] effects visible in client are correct
- [ ] relog does not lose the item

## Exchange / economy

- [ ] vendor listing works if required
- [ ] vendor currency is correct
- [ ] item can be bought if vendor path is part of rollout
- [ ] trade / bank / drop behavior checked if in scope

## Safety

- [ ] no restart happened without backup discipline
- [ ] no production write happened without a reversible record
- [ ] no custom template was called visible only because it existed in DB
