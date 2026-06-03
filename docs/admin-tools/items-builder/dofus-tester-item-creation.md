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

Generated with the Admin item effects codec integer serialization (`TypeInteger = 70`):

```txt
000D0046006F0006004600800006004600750003004600B600030046007D01F4004600B000C80046008A01900046007000320046007C00C80046019A00280046019C0028004602F10032004602F00032
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

## Operational blocker

Live restart is currently blocked on this machine:

```txt
ssh -o BatchMode=yes root@174.138.35.107
Permission denied (publickey)
```

Also, the scripted default key path does not currently exist:

```txt
SSH/private_key_sebas.pem
```

## Safe apply order

1. Take a focused SQL backup of `items`, `accounts`, `worlds_characters`, `characters`, and `characters_items`.
2. Apply `patch_create_dofus_tester_item.sql`.
3. Apply `patch_make_sebcos1_admin.sql`.
4. Stop or restart only the World runtime in a controlled window.
5. While the target characters are offline, apply `patch_grant_dofus_tester_to_sebcos1.sql`.
6. Bring World back up.
7. Re-login with `sebcos1` and validate inventory.

## Current conclusion

The audited ids, reversible scripts, and operator docs are ready in-repo.

What is still blocked for a full live close:

- controlled World restart over SSH
- safe proof that target characters are offline during the inventory grant
- live client validation after restart
