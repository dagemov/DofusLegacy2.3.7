# Dofus Tester Item Creation

## Snapshot

- Date: `2026-06-02`
- Repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`
- Branch: `feature/items-builder-vps-qa-stabilization`
- DB target during audit: `VPS_DB`
- Account target: `sebcos1`

## Goal

Create a real persistent item template named `Dofus Tester`, elevate `sebcos1` to admin, and prepare a controlled grant of `20` units per character.

## Audited source of truth

### Tables involved

- `items`
- `accounts`
- `worlds_characters`
- `characters`
- `characters_items`

### Account audit

- `AccountId`: `265`
- `Username`: `sebcos1`
- `Current role`: `1`
- `Role enum source`: `Sunshine.Protocol.Enums.RoleEnum`
- `Administrator role value`: `5`

### Character audit

Characters currently linked to `sebcos1` in `worlds_characters`:

- `359` -> `Dagemov`
- `361` -> `Megatron`
- `362` -> `Test-Yopy`
- `363` -> `Test`

### Item audit

- `items` is the template table used by the server item manager.
- `characters_items` is the persisted character inventory table.
- `characters_items.Position = 63` means `INVENTORY_POSITION_NOT_EQUIPED`.

### Chosen Dofus skin

Chosen source template:

- `Dofus Ocre`
- Existing template id: `7754`
- Existing type: `23` (`DOFUS`)
- Existing `IconId`: `23012`
- Existing `AppearanceId`: `0`

Chosen target values for `Dofus Tester`:

- `ItemId`: `12617`
- `DescriptionId`: `50091`
- `TypeId`: `23`
- `IconId`: `23012`
- `AppearanceId`: `0`
- `Name`: `Dofus Tester`

## Audited effect ids

The server does not expose a separate modern `Power` stat. For this core, the correct equivalent is `DamageBonusPercent`, the same family used by `Dofus Pourpre`.

| Requested stat | Effect id | Source |
| --- | ---: | --- |
| +6 AP | `111` | `EffectsEnum.Effect_AddAP_111` |
| +6 MP | `128` | `EffectsEnum.Effect_AddMP_128` |
| +3 Range | `117` | `EffectsEnum.Effect_AddRange` |
| +3 Summons | `182` | `EffectsEnum.Effect_AddSummonLimit` |
| +500 Vitality | `125` | `EffectsEnum.Effect_AddVitality` |
| +200 Prospecting | `176` | `EffectsEnum.Effect_AddProspecting` |
| +400 DamageBonusPercent | `138` | `EffectsEnum.Effect_IncreaseDamage_138` |
| +50 Damage | `112` | `EffectsEnum.Effect_AddDamageBonus` |
| +200 Wisdom | `124` | `EffectsEnum.Effect_AddWisdom` |
| +40 AP reduction | `410` | `EffectsEnum.Effect_APAttack` |
| +40 MP reduction | `412` | `EffectsEnum.Effect_412` / MP attack |
| +50 Lock | `753` | `EffectsEnum.Effect_AddLock` |
| +50 Dodge | `752` | `EffectsEnum.Effect_AddDodge` |

## Generated effects payload

Generated with the runtime `EffectManager.SetEffects`-compatible layout using fixed bonuses in `diceNum` and `value = 0`:

```txt
000D0000006F00000006000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000008000000006000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000007500000003000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000000000000000000000000000000000000000B600000003000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000007D000001F4000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000000000000000000000000000000000000000B0000000C8000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000008A00000190000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000007000000032000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000007C000000C8000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000019A00000028000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000000000000000000000000000000000000000019C00000028000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000000000000000000000000000000000000002F100000032000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000000000000000000000000000000000000002F000000032000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000000000000000000000000000000000
```

## Command audit

Relevant in-game command:

- Command: `.item add <itemId> <quantity> [CharacterName]`
- Role required: `Moderator` or higher
- Source: `Sunshine.WorldServer.Commands.Administrator.ItemCommand`

Important limitation:

- the command only works for the caller or for a target character that is currently connected
- no audited bulk account-wide give command was found

That makes SQL grant scripting the only realistic way to give `20` items to every character on `sebcos1` without logging each one in manually.

## Live deployment result

Applied on `2026-06-03` from this workstation with a local non-tracked key copy sourced from:

```txt
C:\Users\Hombr\Downloads\keys\private_key_sebas.pem
```

Verified live outcomes:

- `sebcos1` role updated from `1` to `5`
- `Dofus Tester` template created in `items`
- `20` units granted to each audited character:
  - `359` -> `Dagemov`
  - `361` -> `Megatron`
  - `362` -> `Test-Yopy`
  - `363` -> `Test`
- `sunshine-server` restarted in a controlled way without touching volumes or MySQL service
- public ports recovered after restart:
  - `2450/tcp` -> `True`
  - `5557/tcp` -> `True`

Important production finding:

- a pre-existing test row `items.Id = 12616` (`ADMIN TEST`) had been persisted with the compact Admin effects format and broke World restart
- that row was repaired to the runtime format before the final `Dofus Tester` deployment
- this proves the Admin compact codec and the runtime item-template codec must not be mixed for live template rows

## Safe apply order

1. Take a focused SQL backup of `items`, `accounts`, `worlds_characters`, `characters`, and `characters_items`.
2. Apply `patch_create_dofus_tester_item.sql`.
3. Apply `patch_make_sebcos1_admin.sql`.
4. Stop or restart only the World runtime in a controlled window.
5. While the target characters are offline, apply `patch_grant_dofus_tester_to_sebcos1.sql`.
6. Bring World back up.
7. Re-login with `sebcos1` and validate inventory.

## Current conclusion

The live server-side rollout is complete:

- `Dofus Tester` exists as a real template row
- `sebcos1` is admin
- audited `sebcos1` characters have persisted inventory rows
- controlled World restart completed successfully

However, the production diagnosis on `2026-06-03` changed the final interpretation:

- `items.Id = 12617` is a server-only custom template id
- the client resolves inventory entries by `Template.Id`
- `IconId` and `AppearanceId` alone do not make a server-only template visible
- the current grant shape also uses stacked equipment rows, which is not the preferred final representation

So the rollout is **not** yet equivalent to a client-visible shipped item.

See:

- `dofus-tester-visibility-diagnosis.md`

Still pending for total closure:

- client patch for `12617`, or
- explicit fallback to a client-known visible template id for QA

## Visible fallback executed

Because `12617` remains server-only from the client point of view, the immediate QA path was switched to a visible fallback:

- visible template used: `7754` (`Dofus Ocre`)
- vendor used: `NpcId = 1053` (`Vendeur de Dofus`)
- currency: kamas (`Token = 0`)
- vendor price: `500000`

Tester stats applied to the visible fallback template:

- `+10 AP`
- `+10 PM`
- `+3 Range`
- `+3 Summons`
- `+150 Prospecting`
- `+500 DamageBonusPercent`
- `+50 Damage`

This fallback is client-visible immediately after World reload, but the visible name remains `Dofus Ocre`.
