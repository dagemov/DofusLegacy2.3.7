# Items Builder QA Checklist

Use this checklist after creating, editing, or duplicating an item in Admin.

## Manual checklist

1. Confirm the item was saved and note the `ItemId`.
2. Confirm `IconId` and keep it separate from `ItemId`.
3. Confirm `AppearanceId` and keep it separate from `IconId`.
4. Confirm preview state in Admin is `FOUND` or `MANUAL`.
5. Confirm type and level are correct.
6. Confirm the row exists in `sunshine.items`.
7. Confirm `ResolvedName`, `TypeId`, `IconId`, and `AppearanceId` match expectations.
8. Reload or restart the relevant server process if your environment requires it.
9. Give the item to a QA character using `.item add <itemId> <quantity> [CharacterName]`.
10. Confirm the inventory icon is correct.
11. Confirm the item name and tooltip are correct.
12. If equipable, confirm equipped appearance and slot behavior.
13. Confirm effects behave as expected.
14. Confirm the client does not error when receiving or equipping the item.
15. Record the QA outcome before requesting future publish work.

## Operator reminders

- `ItemId != IconId != AppearanceId`
- do not use Navicat alone as the identity source once Admin already exposes the item detail
- do not treat `READY_FOR_QA` as client publish approval
- do not attempt real client publish from the Admin UI in Phase 8
