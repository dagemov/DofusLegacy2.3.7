# Items Builder Client Publication Analysis

## Snapshot

- Date: `2026-06-03`
- Scope: analysis and planning only
- Target custom item: `12617` / `Dofus Tester`
- Current visible fallback: `7754` / `Dofus Ocre`
- Production writes in this phase: `NO`
- Client file edits in this phase: `NO`

## Exact diagnosis

`12617` is invisible because it exists only in the server database.

Confirmed runtime path:

1. `characters_items` stores the owned row.
2. `CharacterManager.GetCharacterItems(...)` hydrates the row into a `BasePlayerItem`.
3. `BasePlayerItem.GetObjectItem()` sends `Template.Id` as `objectGID`.
4. NPC shops do the same through `Npc.BuildNpcShopObjects()` and `ObjectItemToSellInNpcShop`.
5. The client resolves that `objectGID` against its own shipped item metadata.

So:

- `ItemId = 12617` existing in `sunshine.items` is **not** enough.
- `IconId = 23012` existing in DB is **not** enough.
- `AppearanceId = 0` being valid is **not** enough.
- selling `12617` through a vendor does **not** fix visibility.

The client must know template `12617` in its own metadata files before it can render the item name, tooltip, and inventory entry.

## Client-visible item chain

| Concern | Current client source | `7754` | `12617` | Result |
| --- | --- | --- | --- | --- |
| Template id exists | `Client2.3.7/data/common/Items.d2o` | present | missing | `7754` renders, `12617` does not |
| Localized name | `Client2.3.7/data/i18n/i18n_es.d2i`, `i18n_en.d2i` | present through existing client ids | missing for the new template | exact `Dofus Tester` identity cannot appear |
| Type resolution | `Client2.3.7/data/common/ItemTypes.d2o` | known (`TypeId = 23`) | can reuse existing type | no new type file needed if `TypeId = 23` stays |
| Icon lookup | `Client2.3.7/content/gfx/items/bitmap*.d2p` | known icon family (`23012`) | can reuse same icon | new icon pack is not required if `23012` is kept |
| Equipped / appearance look | `Client2.3.7/data/common/Appearances.d2o` and look assets | not needed for current Dofus fallback | still optional because `AppearanceId = 0` | not the blocker for inventory visibility |
| Vendor rendering | same client template lookup by `objectGID` | visible | invisible | vendor path inherits template visibility |

## Exact files a true `12617` publish would touch

### Required

- `Client2.3.7/data/common/Items.d2o`
- `Client2.3.7/data/i18n/i18n_es.d2i`
- `Client2.3.7/data/i18n/i18n_en.d2i`

### Conditional

- `Client2.3.7/content/gfx/items/bitmap0.d2p` or `bitmap1.d2p`
  - only if `12617` gets a brand-new icon instead of reusing `23012`
- `Client2.3.7/content/gfx/items/vector0.d2p` or `vector1.d2p`
  - only if the client path being used depends on vector item assets for this item family
- `Client2.3.7/data/common/Appearances.d2o`
  - only if future equipped-look work requires a new appearance id instead of `0`
- launcher patch metadata / delivery lane
  - `Client2.3.7/data/common/data.meta`
  - `Client2.3.7/data/i18n/data.meta`
  - `Client2.3.7/data/Launcher/VerInfo.rec`
  - remote `Uplauncher/patchfiles/*` payload or equivalent launcher package output

## What `7754` already proves

`7754` proves the client path is healthy because:

- the client already knows the template id
- the client already knows the localized name
- the client already knows the icon asset
- the vendor can display it immediately when the server sends `objectGID = 7754`

That is why `7754` is a valid visible fallback and `12617` is not.

## Minimum client data needed for `12617`

Confirmed minimum publication set:

1. a new item template entry in `Items.d2o` for id `12617`
2. a valid `nameId` and `descriptionId` for that template
3. matching ES and EN text entries in `i18n_es.d2i` and `i18n_en.d2i`
4. a valid `iconId`
5. launcher/patch metadata so clients actually receive the modified files

Likely no new type publication is required because:

- `TypeId = 23` already exists client-side

Likely no new icon publication is required if `23012` is intentionally reused.

Likely no appearance publication is required for current inventory visibility because:

- `AppearanceId = 0` is already acceptable for the existing Dofus workflow
- the blocker is template publication, not equipped look

## Current tooling gap

The repo already contains:

- `D2OReader`
- `D2OWriter`

The repo does **not** currently contain a documented or validated:

- `D2I` reader/writer workflow for custom item publication
- `D2P` pack writer workflow for item icons
- end-to-end `Items.d2o` item-template publication tool
- launcher patch regeneration script for custom item data updates

Important nuance:

- `D2OWriter` exists, but the shipped typed class coverage in `Sunshine.Protocol/Tools/D2o/Classes/` is minimal (`Breed.cs`)
- a future publish tool must add a typed item class or a safe generic mapping layer before it edits `Items.d2o`

## Safe technical plan for making `12617` visible

### Stage 1 - publication audit tooling

Build a read-only audit tool first that can answer:

- does `Items.d2o` contain template `12617`?
- does the chosen `nameId` resolve in ES and EN?
- does the chosen `descriptionId` resolve in ES and EN?
- does `iconId = 23012` already exist client-side?
- does the launcher patch lane know these changed files?

### Stage 2 - client metadata publication

Once audit tooling is in place:

1. backup current client metadata files
2. add template `12617` to `Items.d2o`
3. add ES text to `i18n_es.d2i`
4. add EN text to `i18n_en.d2i`
5. keep `IconId = 23012` if no new icon is required
6. regenerate the launcher patch metadata and publish the updated files
7. update one QA client through the launcher
8. validate vendor, inventory, tooltip, equip, relog

### Stage 3 - Admin visibility state

After the first successful publish, the Admin tool should distinguish:

- `SERVER_ONLY`
- `CLIENT_PUBLISHED`
- `CARRIER_TEMPLATE_FALLBACK`

That distinction should be stored and surfaced in the UI before future custom items are created.

## Risks

- Publishing `Items.d2o` without `i18n*.d2i` can produce a template that exists but has broken or blank identity.
- Publishing local files without the launcher patch lane means some clients stay stale and QA results become misleading.
- Changing `7754` now would break the current visible fallback that is already serving QA.
- Editing `Client2.3.7/` manually without a tracked publication checklist will reintroduce memory-based tribal knowledge.

## Recommended next step

Do **not** mutate `12617` in production yet.

Recommended next phase:

1. add read-only client publication audit docs/checklists now
2. implement a small non-destructive publication audit tool next
3. only after that, implement the first real `12617` client publish lane

That keeps the current `7754` fallback intact while we build the exact lane that makes future custom items truly visible.
