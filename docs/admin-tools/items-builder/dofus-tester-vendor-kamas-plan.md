# Dofus Tester Vendor Kamas Plan

## Snapshot

- Date: `2026-06-03`
- Scope: audited NPC shop fallback for `Dofus Tester`
- Production candidate NPC: `NpcId = 1053`
- NPC name: `Vendeur de Dofus`

## Audited NPC shop model

Server code and live DB confirm:

- NPC shops are backed by `npcs_items`
- each row defines:
  - `NpcId`
  - `Item`
  - `Price`
  - `Token`
- if `Token = 0`, purchase is made with kamas
- if `Token > 0`, purchase is made with the token item id

Relevant server flow:

- `Npc.ResolveShopToken()`
- `NpcBuySellAction.BuyItem(...)`
- `ExchangeStartOkNpcShopMessage`

## Audited production seller

Live seller candidate:

- `NpcId = 1053`
- `Name = Vendeur de Dofus`
- `Map = 2323`
- `Cell = 386`
- current shop item count: `1950`
- current purchase currency: kamas (`Token = 0`)

So this seller is already compatible with kamas-only testing.

## Why adding `12617` is not enough

Even if we run:

```sql
INSERT INTO npcs_items (NpcId, Item, Price, Token)
VALUES (1053, 12617, 500000, 0);
```

the client will still receive `objectGID = 12617`.

That does not solve the current visibility problem, because:

- `12617` is a server-only template id
- the client does not know that template
- the NPC shop cannot override the client item metadata

So a kamas NPC shop is **not** a valid visibility fix for `Dofus Tester` by itself.

## What the vendor path can do safely

### Safe now

- sell an already client-known template id by kamas
- expose a Dofus skin that the client already understands

### Not safe now

- claim that a server-only template id becomes visible just because it is sold by an NPC
- repurpose a live production Dofus template globally without a product decision

## Practical options

### Option A - keep vendor path blocked for `12617`

Use the vendor only after a client patch makes `12617` known.

Best when:

- the exact visible identity `Dofus Tester` matters
- a client publish workflow is already planned

### Option B - vendor sells a client-known Dofus template

Example candidate:

- `7754` -> `Dofus Ocre`

Possible SQL:

```sql
INSERT INTO npcs_items (NpcId, Item, Price, Token)
SELECT 1053, 7754, 500000, 0
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM npcs_items WHERE NpcId = 1053 AND Item = 7754
);
```

Trade-off:

- the item is visible immediately
- the visible name stays `Dofus Ocre`
- the sold stats remain the template stats unless the server template itself is changed

This is useful only for standard-template QA, not for a custom-stat `Dofus Tester`.

### Option C - direct grant on a client-known template id

If immediate visible QA is needed without a client patch, the safer workaround is not the NPC shop.

The safer workaround is:

- grant or create item instances using a client-known template id
- keep the custom stats at the instance level
- accept that the visible client name remains the known template name

## Current recommendation

- Do not apply a `12617` NPC shop insert as a production fix.
- Keep `Vendeur de Dofus (1053)` as the future kamas seller insertion point.
- If immediate visibility is required before client patching, choose a client-known template fallback explicitly and document the identity trade-off.

## Applied live fallback on `2026-06-03`

Chosen template:

- `7754` -> `Dofus Ocre`

Applied changes:

- updated `items.Id = 7754` base effects to the tester stat payload
- inserted `NpcId = 1053`, `Item = 7754`, `Price = 500000`, `Token = 0`
- restarted only `sunshine-server` after focused backup and zero active auth/world connections at audit time

Why `7754`:

- visible to the client
- same icon family as `12617`
- zero owned rows at audit time
- not already sold by vendor `1053`

Operational trade-off:

- the visible client item is `Dofus Ocre`, not `Dofus Tester`
- future creations of template `7754` will use tester effects until the restore patch is applied

Restore path:

- `infrastructure/sql/items/restore_enable_visible_dofus_tester_vendor.sql`
