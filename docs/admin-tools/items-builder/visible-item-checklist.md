# Visible Item Checklist

Use this checklist before calling a custom item "visible in client".

## Template publication

- [ ] The server row exists in `sunshine.items`
- [ ] The client template id exists in `Client2.3.7/data/common/Items.d2o`
- [ ] The chosen `TypeId` is valid in the client
- [ ] The chosen `IconId` is valid in the client
- [ ] The chosen `AppearanceId` strategy is explicit (`0`, reused, or published)

## Identity publication

- [ ] The template has a valid `nameId`
- [ ] The template has a valid `descriptionId`
- [ ] `nameId` resolves in `i18n_es.d2i`
- [ ] `nameId` resolves in `i18n_en.d2i`
- [ ] `descriptionId` resolves in `i18n_es.d2i`
- [ ] `descriptionId` resolves in `i18n_en.d2i`

## Asset publication

- [ ] The icon already exists in `bitmap*.d2p`, or a new icon publish plan exists
- [ ] The preview PNG state in Admin does not contradict the client icon choice
- [ ] Any new manual preview used by Admin is documented separately from client publication

## Delivery publication

- [ ] The changed client files are included in the launcher patch lane
- [ ] `data.meta` and patch metadata are updated as required
- [ ] At least one QA workstation updated through the real launcher lane
- [ ] Client cache/update behavior was checked, not assumed

## Gameplay validation

- [ ] Vendor path shows the item if vendor testing is in scope
- [ ] Inventory renders the item
- [ ] Tooltip renders the item name
- [ ] Relog keeps the item visible
- [ ] Equip validation passed if the item is equipable

## Operational safety

- [ ] The current visible fallback remains intact until the new publish path passes
- [ ] Backup paths are recorded
- [ ] Rollback steps are written before the live publish

If any box above is false, the item is not yet a fully published client-visible item.
