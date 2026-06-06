# D2O schema report — `Items.d2o`

Date: `2026-06-04`  
Source: `Client2.3.7/data/common/Items.d2o` (read-only)  
Index entries: `11067`  
Class definitions in file: `5` (`Item`, `Weapon`, `EffectInstance`, `EffectInstanceDice`, `EffectInstanceInteger`)

> `EffectInstanceMinMax` y `EffectInstanceDuration` **no** aparecen en la tabla de clases de este `Items.d2o` (cliente 2.3.7).

## Item

Package: `com.ankamagames.dofus.datacenter.items`  
ClassId: `4`

| # | Field | Type | Vector chain |
| ---: | --- | --- | --- |
| 1 | `id` | `Int` | `-` |
| 2 | `nameId` | `I18N` | `-` |
| 3 | `typeId` | `Int` | `-` |
| 4 | `descriptionId` | `I18N` | `-` |
| 5 | `iconId` | `Int` | `-` |
| 6 | `level` | `Int` | `-` |
| 7 | `weight` | `Int` | `-` |
| 8 | `cursed` | `Bool` | `-` |
| 9 | `useAnimationId` | `Int` | `-` |
| 10 | `usable` | `Bool` | `-` |
| 11 | `targetable` | `Bool` | `-` |
| 12 | `price` | `Int` | `-` |
| 13 | `twoHanded` | `Bool` | `-` |
| 14 | `etheral` | `Bool` | `-` |
| 15 | `itemSetId` | `Int` | `-` |
| 16 | `criteria` | `String` | `-` |
| 17 | `hideEffects` | `Bool` | `-` |
| 18 | `appearanceId` | `Int` | `-` |
| 19 | `recipeIds` | `List` | `UInt:Vector.<uint>` |
| 20 | `bonusIsSecret` | `Bool` | `-` |
| 21 | `possibleEffects` | `List` | `1:Vector.<EffectInstance>` |
| 22 | `favoriteSubAreas` | `List` | `UInt:Vector.<uint>` |
| 23 | `favoriteSubAreasBonus` | `Int` | `-` |

## Weapon

Package: `com.ankamagames.dofus.datacenter.items`  
ClassId: `5` (31 campos; orden según D2O, no herencia C#)

Campos adicionales respecto a `Item`: `range`, `criticalHitBonus`, `minRange`, `castTestLos`, `criticalFailureProbability`, `criticalHitProbability`, `apCost`, `castInLine`.

## EffectInstance

Package: `com.ankamagames.dofus.datacenter.effects`  
ClassId: `1`

| Field | Type |
| --- | --- |
| `effectId` | Int |
| `targetId` | Int |
| `duration` | Int |
| `random` | Int |
| `hidden` | Bool |
| `zoneSize` | Int |
| `zoneShape` | Int |

## EffectInstanceInteger

Package: `com.ankamagames.dofus.datacenter.effects.instances`  
ClassId: `2` — 8 campos (`effectId`, `duration`, `hidden`, `random`, `value`, `targetId`, `zoneShape`, `zoneSize`).

## EffectInstanceDice

Package: `com.ankamagames.dofus.datacenter.effects.instances`  
ClassId: `3` — 10 campos (`effectId`, `diceNum`, `duration`, `hidden`, `diceSide`, `value`, `random`, `targetId`, `zoneSize`, `zoneShape`).

## Clases C# generadas (Sunshine)

```txt
Sunshine.Protocol/Tools/D2o/Classes/Item.cs
Sunshine.Protocol/Tools/D2o/Classes/Weapon.cs
Sunshine.Protocol/Tools/D2o/Classes/EffectInstance.cs
Sunshine.Protocol/Tools/D2o/Classes/EffectInstanceInteger.cs
Sunshine.Protocol/Tools/D2o/Classes/EffectInstanceDice.cs
```
