# Item Publication Pipeline

## Purpose

Define the reusable workflow for turning a server-side custom item into a client-visible published item.

This document exists because:

- `sunshine.items` is not the client source of truth
- `IconId` alone does not make a custom item visible
- the repo now has a proven fallback pattern (`carrier template`) and needs a separate real publish pattern

## Publication modes

| Mode | Meaning | Visible in client? | Safe use case |
| --- | --- | --- | --- |
| `SERVER_ONLY` | Row exists only in server DB | no | internal experiments, diagnostics, pre-publish drafts |
| `CARRIER_TEMPLATE_FALLBACK` | server uses a client-known template id to carry custom stats | yes, but under the carrier identity | immediate QA when exact identity can wait |
| `CLIENT_PUBLISHED` | server template id also exists in client metadata and patch lane | yes, with exact identity | real custom item delivery |

## Decision table

| Question | If yes | If no |
| --- | --- | --- |
| Does the exact visible identity matter now? | plan a real client publish | use a carrier template fallback |
| Does the custom item reuse an existing icon? | no D2P icon work needed | add icon publication work |
| Does the item reuse an existing type? | no `ItemTypes.d2o` work needed | publish the new type first |
| Does the item require a new equipped look? | plan `Appearances.d2o` work | keep appearance unchanged |

## Standard pipeline

### 1. Audit first

Confirm:

- server item row exists and is valid
- client template id already exists or does not exist
- target `IconId` exists or does not exist
- target `nameId` / `descriptionId` strategy is defined
- vendor/runtime scenario is known

### 2. Choose the publication mode

- If exact identity is required: `CLIENT_PUBLISHED`
- If immediate visible QA is required: `CARRIER_TEMPLATE_FALLBACK`
- If neither is ready: keep `SERVER_ONLY`

### 3. Backup before mutation

Before any real client publication:

- backup client metadata files
- backup launcher patch metadata
- backup the relevant DB rows if runtime changes are also planned

See:

- [visible-item-checklist.md](./visible-item-checklist.md)
- [qa-vendor-test-checklist.md](./qa-vendor-test-checklist.md)
- [vps-restart-safety-checklist.md](../../infrastructure/vps-restart-safety-checklist.md)

### 4. Author client metadata

Required for a new published template:

- add or update the template in `Client2.3.7/data/common/Items.d2o`
- add ES text in `Client2.3.7/data/i18n/i18n_es.d2i`
- add EN text in `Client2.3.7/data/i18n/i18n_en.d2i`

Conditional:

- update `bitmap*.d2p` if the icon is new
- update `Appearances.d2o` if a new equipped look is required
- update `ItemTypes.d2o` only if the type is new

### 5. Publish through the launcher lane

Do not stop at local file edits.

The client patch lane also needs:

- updated `data.meta` files
- updated patch manifest / version data
- updated files under the path served by `Uplauncher/patchfiles/`

Without that, the changed client files may exist on one workstation but not reach the actual QA clients.

### 6. Validate on a real client

Minimum validation:

- launcher updates the changed files
- vendor shows the item
- inventory shows the item
- tooltip name resolves
- icon resolves
- relog still shows the item

Optional but recommended:

- equip validation
- relaunch client
- second workstation validation

## Example: `12617` / `Dofus Tester`

Current state:

- server row exists
- client row does not exist
- `IconId = 23012` already exists client-side
- `TypeId = 23` already exists client-side

So the real publish path for `12617` is:

1. add `12617` to `Items.d2o`
2. add ES/EN text entries for its name and description
3. reuse icon `23012`
4. publish the changed client files through the launcher lane

Until that exists, `7754` remains the approved visible fallback.

## Tooling improvements required

The Items Builder/Admin Tool should eventually expose:

- `publicationState`
- `clientTemplateKnown`
- `clientNamePublished`
- `clientDescriptionPublished`
- `iconAssetPublished`
- `launcherPatchPublished`

Suggested warnings:

- `TEMPLATE_ID_UNKNOWN_TO_CLIENT`
- `ICON_ID_ALONE_IS_NOT_ENOUGH`
- `I18N_NAME_MISSING`
- `I18N_DESCRIPTION_MISSING`
- `LAUNCHER_PATCH_NOT_PUBLISHED`

## Current repo gap

Today the repo has:

- D2O read/write primitives
- preview PNG pipeline
- vendor/runtime QA workflow

Today the repo does **not** yet have:

- a blessed D2I editing tool
- a blessed D2P editing tool
- a scripted custom-item launcher publish step

That is why the next safe step should be an audit/publish tooling slice, not a blind live mutation.
