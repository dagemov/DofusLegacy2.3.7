# Items Admin Command Catalog

Date: `2026-06-03`

## Goal

Prefer safe audited admin commands before manual SQL whenever live runtime validation is needed.

## Current audited commands

| Command | Syntax | Use | Notes |
| --- | --- | --- | --- |
| `.item add` | `.item add <itemId> <quantity> [CharacterName]` | give item | works for caller; named target handling is still risky/broken in some cases |
| `.item remove` | `.item remove <itemId> <quantity> [CharacterName]` | remove item | same targeting caveat as `.item add` |
| `.kamas` | `.kamas <amount>` / `.kamas add <amount>` / `.kamas remove <amount>` | local kamas mutation | does not support target character in the audited implementation |
| `.save` | `.save` | flush world state | prefer before restart when the operator is online |
| `.stop <seconds>` | `.stop <seconds>` | graceful stop | safer than hard container restart |

## Not yet confirmed as safe alternatives

- offline bulk item grants for every character in an account
- safe targeted give commands for disconnected characters
- vendor configuration commands from in-game GM console
- publication commands for client metadata

## Operational rule

### Use commands when:

- the item is already client-known
- the operator needs live inventory QA
- cleanup is acceptable

### Use SQL when:

- the workflow is batch-oriented
- account-wide or character-wide grant is required
- the command layer does not safely target the needed characters

## Warning

Commands do not solve client publication.

If the client does not know the `ItemId`, these commands can still produce:

- server-side inventory rows
- but no visible inventory item in the client
