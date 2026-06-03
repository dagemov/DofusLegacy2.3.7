# Admin Commands

## Snapshot

- Date: `2026-06-02`
- Source tree: `Sunshine.WorldServer.Commands`
- Scope: commands relevant to controlled item / QA / operations work

## Role enum

From `Sunshine.Protocol.Enums.RoleEnum`:

- `1` -> `Player`
- `2` -> `Moderator`
- `3` -> `GameMaster_Padawan`
- `4` -> `GameMaster`
- `5` -> `Administrator`

## Audited commands

| Command | Syntax | Role | Example | Risk |
| --- | --- | --- | --- | --- |
| `.help` | `.help` | Player | `.help` | Low. Lists available commands for the current role. |
| `.info` | `.info` | Moderator | `.info` | Low. Only reports connected account count. |
| `.save` | `.save` | Moderator | `.save` | Medium. Forces a world save. Safe but operationally relevant. |
| `.item add` | `.item add <itemId> <quantity> [CharacterName]` | Moderator | `.item add 12617 20 Dagemov` | Medium. Gives a live item instance. Target character must be connected. |
| `.item remove` | `.item remove <itemId> <quantity> [CharacterName]` | Moderator | `.item remove 12617 20 Dagemov` | Medium. Removes a live item instance from inventory. |
| `.go` | `.go <...>` | Moderator | `.go astrub` | Medium. Moves characters. Not needed for this rollout. |
| `.kamas` | `.kamas <amount> [CharacterName]` | Moderator | `.kamas 100000 Dagemov` | Medium. Mutates economy state. |
| `.levelup` | `.levelup <amount> [CharacterName]` | Moderator | `.levelup 200 Dagemov` | Medium. Gameplay mutation. |
| `.spell add` | `.spell add <spellId> [CharacterName]` | Moderator | `.spell add 1901 Dagemov` | Medium. Gameplay mutation. |
| `.look` | `.look ...` | Moderator | `.look help` | Medium. Visual mutation. |
| `.npc` | `.npc ...` | Moderator | `.npc help` | High. World mutation. |
| `.monster` | `.monster ...` | Moderator | `.monster help` | High. World mutation. |
| `.mount equip` | `.mount equip [CharacterName]` | Moderator | `.mount equip Dagemov` | Medium. Inventory / mount mutation. |
| `.reload interactives` | `.reload interactives` | Administrator | `.reload interactives` | High. Server-wide reload for interactives only. Not useful for item templates. |
| `.stop` | `.stop <seconds>` or `.stop cancel` | Administrator | `.stop 60` | Very high. Schedules a full world stop. Use only in controlled windows. |
| `.god` | `.god` | Administrator | `.god` | High. Character state mutation. |
| `.bank` | `.bank` | Administrator | `.bank` | Medium. Opens or manipulates bank flow. |
| `.a` | `.a <message>` | Moderator | `.a Maintenance in 2 minutes.` | Low. Announcement only. |

## Direct answer: item grant command

### Does an item grant command exist?

Yes.

### Which command is it?

```txt
.item add <itemId> <quantity> [CharacterName]
```

### Can it give `Dofus Tester`?

Yes, once the template exists in `items` and the caller has `Moderator` or higher.

Example:

```txt
.item add 12617 20 Dagemov
```

### Can it replace SQL for all sebcos1 characters?

Not completely.

Why:

- it works for the caller or for a named target that is currently connected
- no audited account-wide bulk give command was found
- it would require logging in each target character one by one

## Recommendation for this rollout

- Use SQL to create the template.
- Use SQL to grant `20` per character while the account characters are offline.
- Keep `.item add` as the manual fallback for spot QA after `sebcos1` is elevated to admin-capable role.
