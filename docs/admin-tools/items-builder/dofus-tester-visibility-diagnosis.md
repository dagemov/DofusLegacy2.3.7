# Dofus Tester Visibility Diagnosis

## Snapshot

- Date: `2026-06-03`
- Scope: production diagnosis for `Dofus Tester`
- Target template created on server: `items.Id = 12617`
- Target account: `sebcos1`

## Observed live DB state

Confirmed on the VPS database:

- `items.Id = 12617` exists as `Dofus Tester`
- `TypeId = 23`
- `IconId = 23012`
- `AppearanceId = 0`
- `characters_items` rows exist for:
  - `359` -> `Dagemov`
  - `361` -> `Megatron`
  - `362` -> `Test-Yopy`
  - `363` -> `Test`

Observed persisted inventory rows:

```txt
359 Dagemov   Item 12617 Stack 20 Position 63
361 Megatron Item 12617 Stack 20 Position 63
362 Test-Yopy Item 12617 Stack 20 Position 63
363 Test      Item 12617 Stack 20 Position 63
363 Test      Item 12617 Stack 23 Position 63
```

## Root cause

The primary blocker is not the DB insert itself. The primary blocker is client knowledge of the template id.

Server code path:

- `CharacterManager.GetCharacterItems(...)` loads persisted rows from `characters_items`
- `BasePlayerItem.GetObjectItem()` sends `Template.Id` to the client as `objectGID`
- the server does **not** send item name or icon metadata as an override payload

That means the client must already know the template id from its own item data files.

For the current rollout:

- `12617` exists only in the server database
- the client does not have a matching shipped template for `12617`
- `IconId = 23012` alone is not enough
- `AppearanceId = 0` alone is not enough

So the item can exist in `characters_items` and still remain invisible in the client UI.

## Secondary issue

The current grant patch inserted one stacked row per character:

- `Stack = 20`

and one extra duplicated row currently exists on `Test`:

- `Stack = 23`

Even if `12617` were client-known, stacked equipment-style rows are not the ideal long-term representation for a Dofus test item. They should be treated as suspicious data shape, not as the preferred final grant format.

## Why restart alone does not solve it

Restarting World can refresh runtime memory, but it does not teach the client a new template id.

So a restart can be required for other persistence reasons, but it cannot make a server-only template visible if the client does not know that template.

## Direct conclusion

`Dofus Tester` is invisible mainly because:

1. `Template.Id = 12617` is server-only
2. the client resolves items by template id, not by server-side custom name/icon fields
3. the current grant shape is also suboptimal because it uses stacked equipment rows

## Safe paths forward

### Path A - client-aware custom template

Keep `12617`, but ship a client patch that adds the matching template and i18n data.

Needed outside the current server-only scope:

- item definition in the client item data
- matching client text entry for the name/description ids
- publish workflow for the client

This is the only path that preserves the exact visible identity `Dofus Tester`.

### Path B - visible fallback without client patch

Use an existing client-known Dofus template id and grant custom instance effects on that known template.

Trade-off:

- the item becomes visible immediately
- the visible name remains the client-known template name
- this is an operational workaround, not a true new custom client item

Applied production fallback on `2026-06-03`:

- `TemplateId = 7754`
- client-visible name: `Dofus Ocre`
- `IconId = 23012`
- vendor target: `NpcId = 1053`

Why this template was chosen:

- zero owned rows in production at audit time
- not sold by `NpcId = 1053` before the patch
- same icon family as server-side `12617`

This preserves immediate client visibility without pretending that `12617` itself became visible.

### Path C - NPC vendor by kamas

The audited NPC shop system can sell by kamas when `Token = 0`.

However:

- adding `12617` to an NPC shop does **not** fix visibility
- the shop would still send `objectGID = 12617`
- the client would still not know that template

So the vendor route is operationally viable only for:

- an already client-known template id
- or after a future client patch that makes `12617` known

## Audited seller candidate

Audited live seller:

- `NpcId = 1053`
- `Name = Vendeur de Dofus`
- map `2323`
- `Token = 0`
- already sells by kamas

This NPC is a good future insertion point for visible Dofus testing, but not for a server-only template id.

It is now also the live insertion point for the visible fallback based on `TemplateId = 7754`.

## Operational recommendation

- Keep `12617` documented as a server-side custom template.
- Do not rely on it for client-visible QA until a client patch exists.
- If immediate visible QA is required before client patching, choose between:
  - a client-known template fallback
  - or a full client publish workflow

Current live choice:

- client-known template fallback using `7754`
