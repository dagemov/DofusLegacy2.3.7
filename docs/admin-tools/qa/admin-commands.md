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
| `.item add` | `.item add <itemId> <quantity> [CharacterName]` | Moderator | `.item add 12617 20` | Medium. Gives a live item instance to the caller. Named target support is currently broken for connected and offline targets. |
| `.item remove` | `.item remove <itemId> <quantity> [CharacterName]` | Moderator | `.item remove 12617 20 Dagemov` | Medium. Removes a live item instance from inventory. |
| `.go` | `.go <...>` | Moderator | `.go astrub` | Medium. Moves characters. Not needed for this rollout. |
| `.kamas` | `.kamas <amount>` or `.kamas add <amount>` or `.kamas remove <amount>` | Moderator | `.kamas 100000000` | Medium. Mutates only the caller economy state. No target character argument is implemented. |
| `.levelup` | `.levelup <amount> [CharacterName]` | Moderator | `.levelup 200 Dagemov` | Medium. Gameplay mutation. |
| `.spell add` | `.spell add <spellId> [CharacterName]` | Moderator | `.spell add 1901 Dagemov` | Medium. Gameplay mutation. |
| `.spell learnall` | `.spell learnall` | Administrator | `.spell learnall` | Medium. **[QA fix]** Bulk grant all SpellManager spells; one `SpellListMessage` (no lag). Not in fight. See [spell-learnall-qa-fix.md](./spell-learnall-qa-fix.md). |
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
.item add 12617 20
```

### What really happens with `[CharacterName]`?

The current implementation resolves the target with `CharacterManager.Instance.GetCharacter(name)`, which only returns a character currently in world memory.

Then the command applies this guard:

```txt
if ((target != null && target.IsInWorld()) || (target == null && name != null))
```

That condition is inverted for the connected-target case.

Operational result:

- caller-only usage works: `.item add 12617 20`
- offline target fails because offline characters are not resolved from DB
- connected target also falls into the error branch because `target != null && target.IsInWorld()` is treated as failure

So the named target path is not just "online-only"; it is effectively broken in the current code and should not be trusted for production grants.

### Can it replace SQL for all sebcos1 characters?

Not completely.

Why:

- it works reliably only for the caller
- no audited account-wide bulk give command was found
- the named target path is currently broken
- it would require logging in each target character one by one even after a future fix

## Recommendation for this rollout

- Use SQL to create the template.
- Use SQL to grant `20` per character while the account characters are offline or while World is stopped in a controlled window.
- Keep `.item add` as the manual fallback for spot QA after `sebcos1` is elevated to admin-capable role.

## Direct answer: kamas command

### Real syntax

Supported forms from code:

```txt
.kamas <amount>
.kamas add <amount>
.kamas remove <amount>
```

Examples:

```txt
.kamas 100000000
.kamas add 50000
.kamas remove 25000
```

### Why did `.kamas 100000000 Dagemov` fail?

Because the command does not implement a target argument.

When more than one parameter is supplied, it parses:

- `Parameters[0]` as the action
- `Parameters[1]` as the amount

So:

```txt
.kamas 100000000 Dagemov
```

is interpreted as:

- action = `100000000`
- amount = parse(`Dagemov`) -> invalid

### Current limitation

- no target character argument
- no offline grant path
- caller inventory only

### Safe operational recommendation

- use `.kamas <amount>` only on the currently connected operator character
- use controlled SQL only if account-wide or offline economy repair is ever required
