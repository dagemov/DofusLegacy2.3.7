using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using Sunshine.Protocol.Messages;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Items;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Exchanges;
using Sunshine.WorldServer.Game.Actors.Merchants;
using Sunshine.WorldServer.Game.Items.Custom;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;
using Sunshine.WorldServer.Game.BidsHouse;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.Logs;

namespace Sunshine.WorldServer.Handlers.Characters.Inventory
    {
        public class InventoryHandler : WorldPacketHandler
        {
            private static readonly Dictionary<int, int> SpellScrollLearnSpellByItemId = new Dictionary<int, int>
        {
                { 718, 350 },
                { 719, 368 },
                { 720, 369 },
                { 721, 214 },
                { 724, 390 },
                { 725, 391 },
                { 726, 392 },
                { 727, 393 },
                { 728, 394 },
                { 729, 395 },
                { 730, 396 },
                { 731, 410 },
                { 6664, 412 },
                { 6966, 416 },
                { 8087, 397 },
                { 9200, 373 },
                { 9201, 367 },
                { 9916, 420 },
                { 10506, 418 },
                { 10507, 425 },
                { 10508, 422 },
                { 10509, 427 },
                { 10510, 423 },
                { 10511, 421 },
                { 10512, 426 },
                { 10513, 424 },
                { 10602, 414 },
                { 10603, 413 },
                { 10604, 366 },
                { 10605, 364 },
                { 12241, 430 },
        };

            private static readonly Dictionary<int, int> SpellScrollRequiredBreedByItemId = new Dictionary<int, int>
        {
                { 731, (int)BreedEnum.Iop },
                { 6664, (int)BreedEnum.Ecaflip },
                { 6966, (int)BreedEnum.Sram },
                { 9916, (int)BreedEnum.Osamodas },
                { 10506, (int)BreedEnum.Cra },
                { 10507, (int)BreedEnum.Enutrof },
                { 10508, (int)BreedEnum.Feca },
                { 10509, (int)BreedEnum.Eniripsa },
                { 10510, (int)BreedEnum.Pandawa },
                { 10511, (int)BreedEnum.Sacrieur },
                { 10512, (int)BreedEnum.Sadida },
                { 10513, (int)BreedEnum.Xelor },
                { 12241, (int)BreedEnum.Roublard },
        };

            private static readonly Dictionary<int, int> SpellScrollSpellPointsByItemId = new Dictionary<int, int>
        {
                { 684, 1 },
        };

            private static bool HandleFmTransferFromInventory(WorldClient client, IEnumerable<int> ids, bool transferAll)
            {
                if (!(client?.Character?.Trade is FMTrade fmTrade) || client.Character.Inventory == null)
                    return false;

                var items = client.Character.Inventory.GetItems()
                    .Where(x => x != null && fmTrade.CanMoveItem(x));

                if (!transferAll && ids != null)
                {
                    var idSet = ids.ToHashSet();
                    items = items.Where(x => idSet.Contains(x.Id));
                }

                foreach (var item in items.ToList())
                    fmTrade.MoveItem(client.Character, item.Id, Math.Max(1, item.Stack));

                return true;
            }

            private static bool HandleFmTransferToInventory(WorldClient client, IEnumerable<int> ids, bool transferAll)
            {
                if (!(client?.Character?.Trade is FMTrade fmTrade))
                    return false;

                if (!fmTrade.Inventories.TryGetValue(client.Character, out var entries) || entries == null)
                    return true;

                var selectedEntries = entries.Where(x => x != null && x.Item1 != null);

                if (!transferAll && ids != null)
                {
                    var idSet = ids.ToHashSet();
                    selectedEntries = selectedEntries.Where(x => idSet.Contains(x.Item1.Id));
                }

                foreach (var entry in selectedEntries.ToList())
                    fmTrade.MoveItem(client.Character, entry.Item1.Id, -entry.Item2);

                return true;
            }

            [WorldHandler(5608)]
            public static void HandleSpellUpgradeRequestMessage(WorldClient client, SpellUpgradeRequestMessage message)
            {
                client.Character.Spells.BoostSpell(message.spellId);
                client.Character.RefreshStats();
            }

            [WorldHandler(ObjectUseMessage.Id)]
            public static void HandleObjectUseMessage(WorldClient client, ObjectUseMessage message)
            {
                if (client == null || client.Character == null || client.Character.Inventory == null)
                    return;

                var item = client.Character.Inventory.GetItemUid(message.objectUID);
                if (item == null || item.Template == null)
                    return;

                if (TryUseBread(client, item))
                    return;

                if (MountManager.Instance.IsMountCertificateTemplate(item.Template.Id))
                {
                    MountCertificateFactory.TryNormalizeImportedCertificate(item, client.Character.Id);
                    var mount = MountCertificateFactory.ResolveMount(item, client.Character.Id);
                    if (mount == null)
                    {
                        client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));
                        return;
                    }

                    if (!Handlers.Mounts.MountHandler.EquipMountFromInventoryCertificate(client, item, mount))
                        client.Send(new MountEquipedErrorMessage((sbyte)MountEquipedErrorEnum.UNSET));

                    return;
                }

                if (PrismManager.Instance.IsPrismItem(item.Template.Id))
                {
                    string reason;
                    var prismItem = item as PrismItem ?? new PrismItem(item.Template);
                    prismItem.Id = item.Id;
                    prismItem.Position = item.Position;
                    prismItem.Stack = item.Stack;
                    prismItem.Effects = item.Effects;
                    prismItem.EffectSets = item.EffectSets;
                    prismItem.RawObjectEffects = item.RawObjectEffects;

                    if (prismItem.Use(client.Character, out reason))
                        client.Character.Inventory.RemoveItem(item, 1);
                    else if (!string.IsNullOrWhiteSpace(reason))
                        client.Character.SendServerMessage(reason);

                    return;
                }

                if (TryUseCharacterModificationItem(client, item))
                    return;

                if (TryUseMagicFragments(client, item))
                    return;

                if (TryUseSpellScroll(client, item))
                    return;

                if (TryUseCharacteristicScroll(client, item))
                    return;
            }

            private static readonly Dictionary<EffectsEnum, StatsEnum> CharacteristicScrollEffects = new Dictionary<EffectsEnum, StatsEnum>
            {
                { EffectsEnum.Effect_AddStrength, StatsEnum.Strength },
                { EffectsEnum.Effect_AddIntelligence, StatsEnum.Intelligence },
                { EffectsEnum.Effect_AddChance, StatsEnum.Chance },
                { EffectsEnum.Effect_AddAgility, StatsEnum.Agility },
                { EffectsEnum.Effect_AddVitality, StatsEnum.Vitality },
                { EffectsEnum.Effect_AddWisdom, StatsEnum.Wisdom },
                { EffectsEnum.Effect_AddPermanentStrength, StatsEnum.Strength },
                { EffectsEnum.Effect_AddPermanentIntelligence, StatsEnum.Intelligence },
                { EffectsEnum.Effect_AddPermanentChance, StatsEnum.Chance },
                { EffectsEnum.Effect_AddPermanentAgility, StatsEnum.Agility },
                { EffectsEnum.Effect_AddPermanentVitality, StatsEnum.Vitality },
                { EffectsEnum.Effect_AddPermanentWisdom, StatsEnum.Wisdom },
                { EffectsEnum.Effect_SubStrength, StatsEnum.Strength },
                { EffectsEnum.Effect_SubIntelligence, StatsEnum.Intelligence },
                { EffectsEnum.Effect_SubChance, StatsEnum.Chance },
                { EffectsEnum.Effect_SubAgility, StatsEnum.Agility },
                { EffectsEnum.Effect_SubVitality, StatsEnum.Vitality },
                { EffectsEnum.Effect_SubWisdom, StatsEnum.Wisdom },
            };

            private static readonly HashSet<EffectsEnum> NegativeScrollEffects = new HashSet<EffectsEnum>
            {
                EffectsEnum.Effect_SubStrength,
                EffectsEnum.Effect_SubIntelligence,
                EffectsEnum.Effect_SubChance,
                EffectsEnum.Effect_SubAgility,
                EffectsEnum.Effect_SubVitality,
                EffectsEnum.Effect_SubWisdom,
            };

            private static bool TryUseCharacteristicScroll(WorldClient client, BasePlayerItem item)
            {
                if (client?.Character == null || item?.Template == null)
                    return false;

                if (client.Character.IsInFight())
                {
                    client.Character.SendServerMessage("No puedes usar un pergamino en combate.", System.Drawing.Color.Red);
                    return true;
                }

                if (client.Character.Fighter != null && client.Character.Fighter.IsDead())
                {
                    client.Character.SendServerMessage("No puedes usar un pergamino mientras estás muerto.", System.Drawing.Color.Red);
                    return true;
                }

                if (item.Effects == null || item.Effects.Count == 0)
                    return false;

                foreach (var effect in item.Effects)
                {
                    if (effect == null)
                        continue;

                    if (!CharacteristicScrollEffects.TryGetValue(effect.Id, out var targetStat))
                        continue;

                    int value = effect.Value;
                    if (value == 0)
                        continue;

                    if (NegativeScrollEffects.Contains(effect.Id))
                        value = -value;

                    var statData = client.Character.Stats[targetStat];
                    if (statData == null)
                        continue;

                    statData.Base += value;
                    client.Character.Stats.Update();
                    client.Character.Inventory.RemoveItem(item, 1);

                    if (client.Character.Stats.Health != null)
                        Handlers.Characters.CharacterHandler.SendUpdateLifePointsMessage(client);

                    client.Character.RefreshStats();
                    CharacterManager.Instance.Save(client.Character);

                    var statName = targetStat.ToString();
                    if (value > 0)
                        client.Character.SendServerMessage($"+{value} {statName}", System.Drawing.Color.Green);
                    else
                        client.Character.SendServerMessage($"{value} {statName}", System.Drawing.Color.Red);

                    return true;
                }

                return false;
            }

            private static bool TryUseMagicFragments(WorldClient client, BasePlayerItem item)
            {
                if (client == null || client.Character == null || client.Character.Inventory == null || item == null)
                    return false;

                if (item.Template == null || item.Template.Id != 8378)
                    return false;

                var rawEffects = item.RawObjectEffects?.OfType<ObjectEffectDice>()
                    .Where(x => x != null && x.actionId == 10000 && x.diceNum > 0 && x.diceSide > 0)
                    .ToList();

                if (rawEffects == null || rawEffects.Count == 0)
                    return false;

                var added = false;
                foreach (var rawEffect in rawEffects)
                {
                    if (!ItemManager.Instance.Items.ContainsKey(rawEffect.diceNum))
                        continue;

                    var runeItem = ItemManager.Instance.CreatePlayerItem(rawEffect.diceNum, rawEffect.diceSide);
                    if (runeItem == null)
                        continue;

                    client.Character.Inventory.AddItem(runeItem, runeItem.Stack);
                    added = true;
                }

                if (!added)
                    return false;

                client.Character.Inventory.RemoveItem(item, 1);
                return true;
            }


            private static bool TryUseSpellScroll(WorldClient client, BasePlayerItem item)
            {
                if (client?.Character?.Inventory == null || item?.Template == null)
                    return false;

                if (client.Character.IsInFight())
                    return false;

                int spellPointsToAdd = GetItemEffectNumericValue(item, EffectsEnum.Effect_AddSpellPoints);
                int spellId = GetItemEffectNumericValue(item, EffectsEnum.Effect_LearnSpell);

                if (spellPointsToAdd <= 0 && spellId <= 0)
                    return false;

                if (spellPointsToAdd > 0)
                {
                    client.Character.SpellsPoints += spellPointsToAdd;
                    client.Character.RefreshStats();
                    client.Character.Inventory.RemoveItem(item, 1);
                    CharacterManager.Instance.Save(client.Character);
                    client.Character.SendServerMessage($"Tu gagnes {spellPointsToAdd} point(s) de sort.");
                    return true;
                }

                if (spellId <= 0)
                    return false;

                string reason;
                if (!CanLearnSpellFromScroll(client.Character, item, (short)spellId, out reason))
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                        client.Character.SendServerMessage(reason);
                    return true;
                }

                client.Character.Spells.LearnSpell((short)spellId);
                SendSpellListMessage(client, true);
                client.Character.RefreshStats();
                client.Character.Inventory.RemoveItem(item, 1);
                CharacterManager.Instance.Save(client.Character);
                client.Character.SendServerMessage($"Nouveau sort appris : {(short)spellId}.");
                return true;
            }

            private static bool CanLearnSpellFromScroll(Character character, BasePlayerItem item, short spellId, out string reason)
            {
                reason = null;

                if (character == null)
                {
                    reason = "Personnage introuvable.";
                    return false;
                }

                if (character.Spells != null && character.Spells.HasSpell(spellId))
                {
                    reason = "Tu connais déjà ce sort.";
                    return false;
                }

                if (!SpellManager.Instance.Spells.TryGetValue(spellId, out var spellLevels) || spellLevels == null || spellLevels.Count == 0)
                {
                    reason = "Ce parchemin ne correspond à aucun sort utilisable.";
                    return false;
                }

                int ownerBreed = GetRequiredBreedForSpellScroll(item, spellId);
                if (ownerBreed > 0 && ownerBreed != character.Breed)
                {
                    reason = "Ce parchemin de sort est réservé à une autre classe.";
                    return false;
                }

                return true;
            }

            private static int GetRequiredBreedForSpellScroll(BasePlayerItem item, short spellId)
            {
                if (TryGetRequiredBreedFromItemCriteria(item?.Template?.Criteria, out var requiredBreed) && requiredBreed > 0)
                    return requiredBreed;

                if (item?.Template != null && SpellScrollRequiredBreedByItemId.TryGetValue(item.Template.Id, out requiredBreed) && requiredBreed > 0)
                    return requiredBreed;

                return 0;
            }

            private static bool TryGetRequiredBreedFromItemCriteria(string criteria, out int breedId)
            {
                breedId = 0;

                if (string.IsNullOrWhiteSpace(criteria))
                    return false;

                criteria = criteria.Trim();
                if (criteria.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return false;

                const string marker = "PG=";
                var index = criteria.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    return false;

                index += marker.Length;
                var startIndex = index;

                while (index < criteria.Length && char.IsDigit(criteria[index]))
                    index++;

                if (index <= startIndex)
                    return false;

                return int.TryParse(criteria.Substring(startIndex, index - startIndex), out breedId) && breedId > 0;
            }

            private static int GetSpellOwnerBreed(short spellId)
            {
                if (SpellManager.Instance.Spells.TryGetValue(spellId, out var spellLevels) && spellLevels != null)
                {
                    var templateBreed = spellLevels
                        .Where(x => x?.Template != null)
                        .Select(x => x.Template.SpellBreed)
                        .FirstOrDefault(x => x > 0);

                    if (templateBreed > 0)
                        return templateBreed;
                }

                foreach (var entry in BreedManager.Instance.BreedSpells)
                {
                    if (entry.Value != null && entry.Value.Any(x => x != null && x.Spell == spellId))
                        return entry.Key;
                }

                return 0;
            }

            private static int GetItemEffectNumericValue(BasePlayerItem item, EffectsEnum effectId)
            {
                if (item == null)
                    return 0;

                if (item.Template != null)
                {
                    if (effectId == EffectsEnum.Effect_LearnSpell && SpellScrollLearnSpellByItemId.TryGetValue(item.Template.Id, out var learnedSpellId))
                        return learnedSpellId;

                    if (effectId == EffectsEnum.Effect_AddSpellPoints && SpellScrollSpellPointsByItemId.TryGetValue(item.Template.Id, out var spellPointAmount))
                        return spellPointAmount;
                }

                return Game.Effects.EffectNumericResolver.GetNumericValue(item, effectId);
            }

            private static bool TryUseCharacterModificationItem(WorldClient client, BasePlayerItem item)
            {
                if (client == null || client.Character == null || item == null || item.Template == null)
                    return false;

                switch (item.Template.Id)
                {
                    case 10860:
                        if (client.Character.Record.CanUseRename)
                        {
                            client.Character.SendServerMessage("Le changement de nom est déjà actif.");
                            return true;
                        }

                        client.Character.Record.CanUseRename = true;
                        client.Character.Inventory.RemoveItem(item, 1);
                        CharacterManager.Instance.Save(client.Character);
                        client.Character.SendServerMessage("Changement de nom activé. Reconnecte-toi et choisis le personnage.");
                        return true;

                    case 10861:
                        if (client.Character.Record.CanUseRecolor)
                        {
                            client.Character.SendServerMessage("Le changement de couleur est déjà actif.");
                            return true;
                        }

                        client.Character.Record.CanUseRecolor = true;
                        client.Character.Inventory.RemoveItem(item, 1);
                        CharacterManager.Instance.Save(client.Character);
                        client.Character.SendServerMessage("Changement de couleur activé. Reconnecte-toi et choisis le personnage.");
                        return true;

                    case 10862:
                        if (client.Character.Record.CanUseRelook)
                        {
                            client.Character.SendServerMessage("Le changement de sexe / apparence est déjà actif.");
                            return true;
                        }

                        client.Character.Record.CanUseRelook = true;
                        client.Character.Inventory.RemoveItem(item, 1);
                        CharacterManager.Instance.Save(client.Character);
                        client.Character.SendServerMessage("Changement de sexe / apparence activé. Reconnecte-toi et choisis le personnage.");
                        return true;
                }

                return false;
            }

            private static bool TryUseBread(WorldClient client, BasePlayerItem item)
            {
                if (client == null || client.Character == null || item == null)
                    return false;

                if (client.Character.Stats == null || client.Character.Stats.Health == null)
                    return false;

                if (!IsBreadConsumable(item))
                    return false;

                if (client.Character.IsInFight())
                {
                    var fight = client.Character.Fight;
                    if (fight == null || fight.State != FightStateEnum.Placement)
                        return true;
                }

                int heal = GetConsumableHealAmount(item);
                if (heal <= 0)
                    return true;

                int currentHp = client.Character.Stats.Health.Total;
                int maxHp = client.Character.Stats.Health.TotalMax;
                if (currentHp >= maxHp)
                    return true;

                int finalHeal = Math.Min(heal, maxHp - currentHp);
                if (finalHeal <= 0)
                    return true;

                client.Character.Stats.Health.Taken = Math.Max(0, client.Character.Stats.Health.Taken - finalHeal);
                client.Character.Stats.Update();
                client.Character.ResetRegenTimerAfterManualHeal();

                Handlers.Characters.CharacterHandler.SendUpdateLifePointsMessage(client);
                client.Character.RefreshStats();

                if (client.Character.IsInFight() && client.Character.Fight != null)
                    Handlers.Context.ContextHandler.SendGameFightSynchronizeMessage(
                        client.Character.Fight.Clients,
                        client.Character.Fighter);

                client.Character.Inventory.RemoveItem(item, 1);
                return true;
            }



            private static bool IsBreadConsumable(BasePlayerItem item)
            {
                if (item == null)
                    return false;

                if (item.Type == ItemTypeEnum.PAIN)
                    return true;

                if (item.Type != ItemTypeEnum.DIVERS)
                    return false;

                var name = item.Template?.Name?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                var normalized = name.ToLowerInvariant();
                return (normalized.Contains("pain")
                        || normalized.Contains("brioche")
                        || normalized.Contains("briochette")
                        || normalized.Contains("biscotte")
                        || normalized.Contains("fougasse")
                        || normalized.Contains("galette")
                        || normalized.Contains("miche")
                        || normalized.Contains("baguette"))
                    && GetConsumableHealAmount(item) > 0;
            }

            private static int GetConsumableHealAmount(BasePlayerItem item)
            {
                int heal = 0;

                if (item.RawObjectEffects != null)
                {
                    foreach (var effect in item.RawObjectEffects.Where(x => x != null))
                    {
                        var effectId = (EffectsEnum)effect.actionId;
                        if (IsHealEffect(effectId))
                            heal += GetRawObjectEffectHealValue(effect);
                    }
                }

                if (heal > 0)
                    return heal;

                if (item.Effects != null)
                {
                    foreach (var effect in item.Effects.Where(x => x != null))
                    {
                        if (IsHealEffect(effect.Id))
                            heal += GetSpellEffectHealValue(effect);
                    }
                }

                return heal;
            }

            private static int GetRawObjectEffectHealValue(ObjectEffect effect)
            {
                switch (effect)
                {
                    case ObjectEffectInteger integerEffect:
                        return Math.Max(0, (int)integerEffect.value);

                    case ObjectEffectMinMax minMaxEffect:
                        return Math.Max(0, Math.Max((int)minMaxEffect.min, minMaxEffect.max));

                    case ObjectEffectDice diceEffect:
                        return Math.Max(0, Math.Max((int)diceEffect.diceConst, Math.Max(diceEffect.diceNum, diceEffect.diceSide)));

                    default:
                        return 0;
                }
            }

            private static int GetSpellEffectHealValue(Effect effect)
            {
                if (effect == null)
                    return 0;

                if (effect.Value > 0)
                    return effect.Value;

                return Math.Max(0, Math.Max((int)effect.DiceNum, (int)effect.DiceFace));
            }

            private static bool IsHealEffect(EffectsEnum effectId)
            {
                return effectId == EffectsEnum.Effect_HealHP_81
                    || effectId == EffectsEnum.Effect_HealHP_108
                    || effectId == EffectsEnum.Effect_HealHP_143
                    || effectId == EffectsEnum.Effect_AddHealth;
            }


            [WorldHandler(LivingObjectChangeSkinRequestMessage.Id)]
            public static void HandleLivingObjectChangeSkinRequestMessage(WorldClient client, LivingObjectChangeSkinRequestMessage message)
            {
                if (client == null || client.Character == null || client.Character.Inventory == null || client.Character.IsInFight())
                    return;

                var item = client.Character.Inventory.GetItemUid(message.livingUID) as CommonLivingObject;
                if (item == null)
                    return;

                item.InitializeFromCurrentState();
                var oldAppearance = item.AppearanceId;
                item.SelectedLevel = (short)message.skinId;
                var newAppearance = item.AppearanceId;

                var finalized = client.Character.Inventory.FinalizeItemRuntime(item) ?? item;
                SendObjectModifiedMessage(client, finalized);
                RefreshLivingObjectLook(client.Character, finalized, oldAppearance, newAppearance);
                RefreshInventoryAndShortcuts(client);
            }

            [WorldHandler(LivingObjectFeedMessage.Id)]
            public static void HandleLivingObjectFeedMessage(WorldClient client, LivingObjectFeedMessage message)
            {
                if (client == null || client.Character == null || client.Character.Inventory == null || client.Character.IsInFight())
                    return;

                var item = client.Character.Inventory.GetItemUid(message.livingUID) as BoundLivingObjectItem;
                var food = client.Character.Inventory.GetItemUid(message.foodUID);
                if (item == null || food == null)
                    return;

                item.InitializeFromCurrentState();
                if (!item.TryFeed(food))
                    return;

                client.Character.Inventory.RemoveItem(food, 1);
                var finalized = client.Character.Inventory.FinalizeItemRuntime(item) ?? item;
                SendObjectModifiedMessage(client, finalized);
                RefreshInventoryAndShortcuts(client);
            }

            [WorldHandler(LivingObjectDissociateMessage.Id)]
            public static void HandleLivingObjectDissociateMessage(WorldClient client, LivingObjectDissociateMessage message)
            {
                if (client == null || client.Character == null || client.Character.Inventory == null || client.Character.IsInFight())
                    return;

                var item = client.Character.Inventory.GetItemUid(message.livingUID) as BoundLivingObjectItem;
                if (item == null)
                    return;

                item.InitializeFromCurrentState();
                var oldAppearance = item.AppearanceId;
                var livingObject = item.Dissociate();
                var newAppearance = item.AppearanceId;

                var finalized = client.Character.Inventory.FinalizeItemRuntime(item) ?? item;
                SendObjectModifiedMessage(client, finalized);
                if (livingObject != null)
                    client.Character.Inventory.AddItem(livingObject, 1);

                RefreshLivingObjectLook(client.Character, finalized, oldAppearance, newAppearance);
                RefreshInventoryAndShortcuts(client);
            }

            private static void RefreshInventoryAndShortcuts(WorldClient client)
            {
                if (client?.Character?.Inventory == null)
                    return;

                client.Character.Shortcuts.SynchronizeItemShortcuts(true);
                client.Character.Inventory.SynchronizePresetObjects(true);
                SendInventoryContentMessage(client);
                Handlers.Characters.Shorcuts.ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
                SendInventoryWeightMessage(client);
            }

            private static void RefreshLivingObjectLook(Character character, BasePlayerItem item, short oldAppearance, short newAppearance)
            {
                if (character == null || item == null || !item.IsEquiped())
                    return;

                character.UpdateLook(!string.IsNullOrWhiteSpace(character.CustomLook));
            }

            [WorldHandler(InventoryPresetSaveMessage.Id)]
            public static void HandleInventoryPresetSaveMessage(WorldClient client, InventoryPresetSaveMessage message)
            {
                var result = client.Character.Inventory.AddPreset(message.presetId, message.symbolId, message.saveEquipment);
                SendInventoryPresetSaveResultMessage(client, message.presetId, result);

                if (result == PresetSaveResultEnum.PRESET_SAVE_OK)
                {
                    var preset = client.Character.Inventory.GetPreset(message.presetId);
                    if (preset != null)
                        SendInventoryPresetUpdateMessage(client, preset.GetNetworkPreset());

                    Handlers.Characters.Shorcuts.ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
                }
            }

            [WorldHandler(InventoryPresetDeleteMessage.Id)]
            public static void HandleInventoryPresetDeleteMessage(WorldClient client, InventoryPresetDeleteMessage message)
            {
                var result = client.Character.Inventory.RemovePreset(message.presetId);
                SendInventoryPresetDeleteResultMessage(client, message.presetId, result);

                if (result == PresetDeleteResultEnum.PRESET_DEL_OK)
                    Handlers.Characters.Shorcuts.ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
            }

            [WorldHandler(InventoryPresetItemUpdateRequestMessage.Id)]
            public static void HandleInventoryPresetItemUpdateRequestMessage(WorldClient client, InventoryPresetItemUpdateRequestMessage message)
            {
                var result = message.objUid <= 0
                    ? client.Character.Inventory.RemovePresetItem(message.presetId, message.position)
                    : client.Character.Inventory.UpdatePresetItem(message.presetId, message.position, message.objUid);

                if (result != PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_UNKNOWN)
                {
                    SendInventoryPresetUpdateErrorMessage(client, result);
                    return;
                }

                var preset = client.Character.Inventory.GetPreset(message.presetId);
                if (preset != null)
                    SendInventoryPresetUpdateMessage(client, preset.GetNetworkPreset());
            }

            [WorldHandler(InventoryPresetUseMessage.Id)]
            public static void HandleInventoryPresetUse(WorldClient client, InventoryPresetUseMessage message)
            {
                client.Character.Inventory.EquipPreset(message.presetId);
            }

            [WorldHandler(3021)]
            public static void HandleObjectSetPositionMessage(WorldClient client, ObjectSetPositionMessage message)
            {
                var item = client.Character.Inventory.GetItemUid(message.objectUID);
                if (item == null)
                    return;

                client.Character.Inventory.MoveItem(item, (CharacterInventoryPositionEnum)message.position);
            }

            [WorldHandler(3022)]
            public static void HandleObjectDeleteMessage(WorldClient client, ObjectDeleteMessage message)
            {
                var item = client.Character.Inventory.GetItemUid(message.objectUID);
                client.Character.Inventory.RemoveItem(item, message.quantity);
            }

            [WorldHandler(5773)]
            public static void HandleExchangePlayerRequestMessage(WorldClient client, ExchangePlayerRequestMessage message)
            {
                ExchangeTypeEnum exchangeType = (ExchangeTypeEnum)message.exchangeType;
                if (exchangeType != ExchangeTypeEnum.PLAYER_TRADE)
                    InventoryHandler.SendExchangeErrorMessage(client, ExchangeErrorEnum.REQUEST_IMPOSSIBLE);
                else
                {
                    Character target = CharacterManager.Instance.GetCharacter(message.target);
                    if (target == null)
                        InventoryHandler.SendExchangeErrorMessage(client, ExchangeErrorEnum.BID_SEARCH_ERROR);
                    else
                    {
                        if (target.Map.Id != client.Character.Map.Id)
                            InventoryHandler.SendExchangeErrorMessage(client, ExchangeErrorEnum.REQUEST_CHARACTER_TOOL_TOO_FAR);
                        else
                        {
                            if (target.IsInTrade())
                                InventoryHandler.SendExchangeErrorMessage(client, ExchangeErrorEnum.REQUEST_CHARACTER_OCCUPIED);
                            else
                                client.Character.SetTradeRequest(exchangeType, target);
                        }
                    }
                }
            }

            [WorldHandler(5508)]
            public static void HandleExchangeAcceptMessage(WorldClient client, ExchangeAcceptMessage message)
            {
                if (client.Character.IsInTrade() && client.Character.Trade is PlayerTrade playerTrade && !playerTrade.IsOpened)
                    playerTrade.Open();
            }

            [WorldHandler(5520)]
            public static void HandleExchangeObjectMoveKamaMessage(WorldClient client, ExchangeObjectMoveKamaMessage message)
            {
                if (client.Character.IsInTrade())
                    client.Character.Trade.SetKamas(client.Character, message.quantity);
            }

            [WorldHandler(5518)]
            public static void HandleExchangeObjectMoveMessage(WorldClient client, ExchangeObjectMoveMessage message)
            {
                if (client.Character.IsInTrade())
                {
                    client.Character.Trade.MoveItem(client.Character, message.objectUID, message.quantity);
                    return;
                }
                else if (client.Character.IsInDialog() && client.Character.Dialog is NpcSellAction)
                    client.Character.BidHouseBag.MoveItem(message.objectUID, message.quantity);
            }

            [WorldHandler(ExchangeObjectTransfertAllFromInvMessage.Id)]
            public static void HandleExchangeObjectTransfertAllFromInvMessage(WorldClient client, ExchangeObjectTransfertAllFromInvMessage message)
            {
                HandleFmTransferFromInventory(client, Array.Empty<int>(), true);
            }

            [WorldHandler(ExchangeObjectTransfertListFromInvMessage.Id)]
            public static void HandleExchangeObjectTransfertListFromInvMessage(WorldClient client, ExchangeObjectTransfertListFromInvMessage message)
            {
                HandleFmTransferFromInventory(client, message.ids, false);
            }

            [WorldHandler(ExchangeObjectTransfertAllToInvMessage.Id)]
            public static void HandleExchangeObjectTransfertAllToInvMessage(WorldClient client, ExchangeObjectTransfertAllToInvMessage message)
            {
                HandleFmTransferToInventory(client, Array.Empty<int>(), true);
            }

            [WorldHandler(ExchangeObjectTransfertListToInvMessage.Id)]
            public static void HandleExchangeObjectTransfertListToInvMessage(WorldClient client, ExchangeObjectTransfertListToInvMessage message)
            {
                HandleFmTransferToInventory(client, message.ids, false);
            }

            [WorldHandler(ExchangeObjectUseInWorkshopMessage.Id)]
            public static void HandleExchangeObjectUseInWorkshopMessage(WorldClient client, ExchangeObjectUseInWorkshopMessage message)
            {
                if (client.Character.IsInTrade() && client.Character.Trade is FMTrade fmTrade)
                    fmTrade.UseItemInWorkshop(message.objectUID, message.quantity);
            }

            [WorldHandler(5511)]
            public static void HandleExchangeReadyMessage(WorldClient client, ExchangeReadyMessage message)
            {
                if (client.Character.IsInTrade())
                    client.Character.Trade.SetReadyStatus(client.Character, message.ready);
            }

            [WorldHandler(5774)]
            public static void HandleExchangeBuyMessage(WorldClient client, ExchangeBuyMessage message)
            {
                if (client.Character.IsInTrade() && client.Character.Trade is MerchantCustomerTrade merchantCustomerTrade)
                {
                    merchantCustomerTrade.BuyItem(message.objectToBuyId, message.quantity);
                    return;
                }

                if (client.Character.IsInDialog() && client.Character.Dialog is NpcBuySellAction)
                    (client.Character.Dialog as NpcBuySellAction).BuyItem(message.objectToBuyId, message.quantity);
            }

            [WorldHandler(ExchangeSellMessage.Id)]
            public static void HandleExchangeSellMessage(WorldClient client, ExchangeSellMessage message)
            {
                if (client.Character.IsInDialog() && client.Character.Dialog is NpcBuySellAction npcBuySellAction)
                    npcBuySellAction.SellItem(message.objectToSellId, message.quantity);
            }

            [WorldHandler(ExchangeShowVendorTaxMessage.Id)]
            public static void HandleExchangeShowVendorTaxMessage(WorldClient client, ExchangeShowVendorTaxMessage message)
            {
                const int objectValue = 0;
                var totalTax = MerchantManager.Instance.GetMerchantTax(client.Character.Id);
                if (totalTax <= 0)
                    totalTax = 1;

                client.Send(new ExchangeReplyTaxVendorMessage(objectValue, totalTax));
            }

            [WorldHandler(ExchangeRequestOnShopStockMessage.Id)]
            public static void HandleExchangeRequestOnShopStockMessage(WorldClient client, ExchangeRequestOnShopStockMessage message)
            {
                MerchantManager.Instance.OpenStock(client.Character);
            }

            [WorldHandler(ExchangeStartAsVendorMessage.Id)]
            public static void HandleExchangeStartAsVendorMessage(WorldClient client, ExchangeStartAsVendorMessage message)
            {
                if (MerchantManager.Instance.Activate(client.Character, out var reason))
                    return;

                if (!string.IsNullOrWhiteSpace(reason))
                    client.Character.SendServerMessage(reason);
            }

            [WorldHandler(ExchangeOnHumanVendorRequestMessage.Id)]
            public static void HandleExchangeOnHumanVendorRequestMessage(WorldClient client, ExchangeOnHumanVendorRequestMessage message)
            {
                var merchant = MerchantManager.Instance.GetMerchant(client.Character.Map, message.humanVendorId, (short)message.humanVendorCell);
                if (merchant == null)
                {
                    SendExchangeErrorMessage(client, ExchangeErrorEnum.BUY_ERROR);
                    return;
                }

                client.Character.SetTrade(ExchangeTypeEnum.DISCONNECTED_VENDOR, null, merchant);
                client.Character.Trade.Open();
            }

            [WorldHandler(5805)]
            public static void HandleExchangeBidHousePriceMessage(WorldClient client, ExchangeBidHousePriceMessage message)
            {
                client.Send(new ObjectsQuantityMessage(new List<ObjectItemQuantity>()));
                client.Send(new ExchangeBidPriceMessage(message.genId, client.Character.Level));
            }

            [WorldHandler(5803)]
            public static void HandleExchangeBidHouseTypeMessage(WorldClient client, ExchangeBidHouseTypeMessage message)
            {
                if (client.Character.IsInDialog() && client.Character.Dialog is NpcBuyAction)
                {
                    var bidHouse = BidHouseManager.Instance.BidsHouse[client.Character.Map.Id];
                    InventoryHandler.SendExchangeTypesExchangerDescriptionForUserMessage(client, bidHouse, message.type);
                }
            }

            [WorldHandler(5514)]
            public static void HandleExchangeObjectMovePricedMessage(WorldClient client, ExchangeObjectMovePricedMessage message)
            {
                if (client.Character.IsInTrade() && client.Character.Trade is MerchantStockTrade merchantStockTrade)
                {
                    merchantStockTrade.StorePricedItem(message.objectUID, message.quantity, message.price);
                    return;
                }

                if (message.quantity > 0 && message.price > 0)
                {
                    var item = client.Character.Inventory.GetItemUid(message.objectUID);
                    if (item != null)
                    {
                        item.EnsureRuntimeEffects();
                        if (!item.IsExchangeable())
                        {
                            SendExchangeErrorMessage(client, ExchangeErrorEnum.REQUEST_IMPOSSIBLE);
                            client.Character.SendServerMessage("Cet objet ne peut pas être mis en vente.");
                            return;
                        }

                        var bidHouse = BidHouseManager.Instance.BidsHouse[client.Character.Map.Id];
                        if (bidHouse.GetSellerBuyerDescriptor().types.Contains((int)item.Type))
                        {
                            int taxBidHouse = Math.Abs(message.price / 100);
                            if (client.Character.Inventory.Kamas > taxBidHouse)
                            {
                                client.Character.Inventory.SetKamas(-taxBidHouse);
                                bidHouse.SellItem(client.Character, item, message.quantity, message.price);
                            }
                            else
                                client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 65, new object[0]);
                        }
                        else
                            client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 64, new object[0]);
                    }
                }
            }

            [WorldHandler(ExchangeObjectModifyPricedMessage.Id)]
            public static void HandleExchangeObjectModifyPricedMessage(WorldClient client, ExchangeObjectModifyPricedMessage message)
            {
                if (client.Character.IsInTrade() && client.Character.Trade is MerchantStockTrade merchantStockTrade)
                {
                    merchantStockTrade.ModifyPricedItem(message.objectUID, message.quantity, message.price);
                }
            }

            [WorldHandler(5807)]
            public static void HandleExchangeBidHouseListMessage(WorldClient client, ExchangeBidHouseListMessage message)
            {
                if (client.Character.IsInDialog() && client.Character.Dialog is NpcBuyAction)
                {
                    var bidHouse = BidHouseManager.Instance.BidsHouse[client.Character.Map.Id];
                    InventoryHandler.SendExchangeTypesItemsExchangerDescriptionForUserMessage(client, bidHouse);
                }
            }

            [WorldHandler(5804)]
            public static void HandleExchangeBidHouseBuyMessage(WorldClient client, ExchangeBidHouseBuyMessage message)
            {
                if (client.Character.IsInDialog() && client.Character.Dialog is NpcBuyAction)
                {
                    var bidHouse = BidHouseManager.Instance.BidsHouse[client.Character.Map.Id];
                    bidHouse.BuyItem(client.Character, message.uid, message.qty, message.price);
                }
            }

            [WorldHandler(6002)]
            public static void HandleExchangeReplayMessage(WorldClient client, ExchangeReplayMessage message)
            {
                if (!client.Character.IsInTrade())
                    return;

                if (client.Character.Trade is CraftTrade)
                    (client.Character.Trade as CraftTrade).UpdateReplay(message.count);
                else if (client.Character.Trade is FMTrade)
                    (client.Character.Trade as FMTrade).UpdateReplay(message.count);
            }

            [WorldHandler(6001)]
            public static void HandleExchangeReplayStopMessage(WorldClient client, ExchangeReplayStopMessage message)
            {
                if (!client.Character.IsInTrade())
                    return;

                if (client.Character.Trade is CraftTrade)
                    (client.Character.Trade as CraftTrade).Stop();
                else if (client.Character.Trade is FMTrade)
                    (client.Character.Trade as FMTrade).Stop();
            }

            public static void SendExchangeErrorMessage(WorldClient client, ExchangeErrorEnum errorEnum)
            {
                client.Send(new ExchangeErrorMessage((sbyte)errorEnum));
            }

            public static void SendExchangeBuyOkMessage(WorldClient client)
            {
                client.Send(new ExchangeBuyOkMessage());
            }

            public static void SendExchangeSellOkMessage(WorldClient client)
            {
                client.Send(new ExchangeSellOkMessage());
            }

            public static void SendExchangeStartOkCraftWithInformationMessage(WorldClient client, sbyte nbCase, int skill)
            {
                client.Send(new ExchangeStartOkCraftWithInformationMessage(nbCase, skill));
            }

            public static void SendExchangeReplayCountModifiedMessage(WorldClient client, int count)
            {
                client.Send(new ExchangeReplayCountModifiedMessage(count));
            }

            public static void SendExchangeReplayStopMessage(WorldClient client)
            {
                client.Send(new ExchangeReplayStopMessage());
            }

            public static void SendExchangeItemAutoCraftRemainingMessage(WorldClient client, int count)
            {
                client.Send(new ExchangeItemAutoCraftRemainingMessage(count));
            }

            public static void SendExchangeCraftResultMagicWithObjectDescMessage(WorldClient client, BasePlayerItem item, CraftResultEnum result, MagicPoolStatusEnum magicPool)
            {
                client.Send(new ExchangeCraftResultMagicWithObjectDescMessage((sbyte)result, item.GetObjectItemNotInContainer(), (sbyte)magicPool));
            }

            public static void SendExchangeCraftResultWithObjectDescMessage(WorldClient client, BasePlayerItem item, CraftResultEnum result)
            {
                client.Send(new ExchangeCraftResultWithObjectDescMessage((sbyte)result, item.GetObjectItemNotInContainer()));
            }

            public static void SendExchangeItemAutoCraftStopedMessage(WorldClient client, ExchangeReplayStopReasonEnum reason)
            {
                client.Send(new ExchangeItemAutoCraftStopedMessage((sbyte)reason));
            }

            public static void SendExchangeRequestedTradeMessage(WorldClient client, ExchangeTypeEnum type, Character source, Character target)
            {
                client.Send(new ExchangeRequestedTradeMessage((sbyte)type, source.Id, target.Id));
            }

            public static void SendExchangeLeaveMessage(WorldClient client, bool apply)
            {
                client.Send(new ExchangeLeaveMessage(apply));
            }

            public static void SendExchangeStartedWithPodsMessage(WorldClient client, ExchangeTypeEnum type, Trader source, Trader target)
            {
                client.Send(new ExchangeStartedWithPodsMessage((sbyte)type, source.Id, source.Inventory.GetWeight(),
                    source.Inventory.GetWeightTotal(), target.Id, target.Inventory.GetWeight(), target.Inventory.GetWeightTotal()));
            }

            public static void SendExchangeStartOkNpcShopMessage(WorldClient client, Npc npc)
            {
                var sellerId = npc.Id;
                var isVirtualShop = npc?.Record != null && VirtualShopRegistry.Instance.TryGetShop(npc.Record.Id, out _);

                if (isVirtualShop)
                    sellerId = VirtualShopRegistry.Instance.ResolveVirtualSellerId(client.Character, npc);

                var items = npc.GetObjectItemToSellInNpcShops;
                var itemCount = items?.Count() ?? 0;
                var token = npc.ResolveShopToken();

                Logger.WriteInfo(
                    $"[ShopTrace] Send5761 charId={client.Character?.Id} sellerId={sellerId} virtual={isVirtualShop} " +
                    $"npcTemplate={npc?.Record?.Id} npcActor={npc?.Id} token={token} items={itemCount}");

                client.Send(new ExchangeStartOkNpcShopMessage(sellerId, token, items));
            }

            public static void SendExchangeStartOkNpcTradeMessage(WorldClient client, Npc npc)
            {
                client.Send(new ExchangeStartOkNpcTradeMessage(npc.Id));
            }

            public static void SendExchangeStartedWithStorageMessage(WorldClient client, ExchangeTypeEnum type, int maxStorage)
            {
                client.Send(new ExchangeStartedWithStorageMessage((sbyte)type, maxStorage));
            }

            public static void SendExchangeStartPaddockBuySell(WorldClient client, bool isSellDialog, int ownerId, int price)
            {
                if (client == null)
                    return;

                client.Send(new ExchangeStartedMessage((sbyte)ExchangeTypeEnum.REALESTATE_FARM));
                client.Send(new PaddockSellBuyDialogMessage(isSellDialog, ownerId, price < 0 ? 0 : price));
            }

            public static void SendStorageInventoryContentMessage(WorldClient client, IEnumerable<ObjectItem> objects, int kamas)
            {
                client.Send(new StorageInventoryContentMessage(objects, kamas));
            }

            public static void SendExchangeStartedBidBuyerMessage(WorldClient client)
            {
                if (client.Character.IsInDialog() && client.Character.Dialog is NpcBuyAction)
                {
                    var bidHouse = BidHouseManager.Instance.BidsHouse[client.Character.Map.Id];
                    client.Send(new ExchangeStartedBidBuyerMessage(bidHouse.GetSellerBuyerDescriptor()));
                }
            }

            public static void SendExchangeStartedBidSellerMessage(WorldClient client)
            {
                if (client.Character.IsInDialog() && client.Character.Dialog is NpcSellAction)
                {
                    var bidHouse = BidHouseManager.Instance.BidsHouse[client.Character.Map.Id];
                    client.Send(new ExchangeStartedBidSellerMessage(bidHouse.GetSellerBuyerDescriptor(), client.Character.BidHouseBag.GetObjectItemsToSell(bidHouse)));
                }
            }

            public static void SendExchangeBidHouseItemAddOkMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ExchangeBidHouseItemAddOkMessage(item.GetObjectItemToSell()));
            }

            public static void SendExchangeBidHouseItemRemoveOkMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ExchangeBidHouseItemRemoveOkMessage(item.Template.Id));
            }

            public static void SendExchangeBidHouseInListRemovedMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ExchangeBidHouseInListRemovedMessage(item.Id));
            }

            public static void SendExchangeBidHouseInListAddedMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ExchangeBidHouseInListAddedMessage(item.Id, item.Template.Id, 0, false, item.Effects.Select(x => x.GetObjectEffectInteger()), BidHouseManager.Instance.BidsHousePrices[item]));
            }

            public static void SendExchangeTypesItemsExchangerDescriptionForUserMessage(WorldClient client, BidHouse bidHouse)
            {
                client.Send(new ExchangeTypesItemsExchangerDescriptionForUserMessage(bidHouse.GetBidExchangerObjectsInfo()));
            }

            public static void SendExchangeTypesItemsExchangerDescriptionForUserMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ExchangeTypesItemsExchangerDescriptionForUserMessage(new List<BidExchangerObjectInfo> { item.GetBidExchangerObjectInfo() }));
            }

            public static void SendExchangeTypesExchangerDescriptionForUserMessage(WorldClient client, BidHouse bidHouse, int type)
            {
                client.Send(new ExchangeTypesExchangerDescriptionForUserMessage(bidHouse.GetItems((ItemTypeEnum)type)));
            }

            public static void SendExchangeKamaModifiedMessage(WorldClient client, bool remote, int quantity)
            {
                client.Send(new ExchangeKamaModifiedMessage(remote, quantity));
            }

            public static void SendExchangeObjectAddedMessage(WorldClient client, bool remote, BasePlayerItem item)
            {
                client.Send(new ExchangeObjectAddedMessage(remote, item.GetObjectItem()));
            }

            public static void SendExchangeObjectModifiedMessage(WorldClient client, bool remote, BasePlayerItem item)
            {
                client.Send(new ExchangeObjectModifiedMessage(remote, item.GetObjectItem()));
            }

            public static void SendExchangeObjectRemovedMessage(WorldClient client, bool remote, BasePlayerItem item)
            {
                client.Send(new ExchangeObjectRemovedMessage(remote, item.Id));
            }

            public static void SendStorageObjectRemoveMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new StorageObjectRemoveMessage(item.Id));
            }

            public static void SendStorageObjectsUpdateMessage(WorldClient client, IEnumerable<BasePlayerItem> items)
            {
                client.Send(new StorageObjectsUpdateMessage(items.Select(x => x.GetObjectItem())));
            }

            public static void SendStorageObjectUpdateMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new StorageObjectUpdateMessage(item.GetObjectItem()));
            }

            public static void SendStorageKamasUpdateMessage(WorldClient client, int kamasTotal)
            {
                client.Send(new StorageKamasUpdateMessage(kamasTotal));
            }

            public static void SendExchangeIsReadyMessage(WorldClient client, Trader trader, bool ready)
            {
                client.Send(new ExchangeIsReadyMessage(trader.Id, ready));
            }

            public static void SendKamasUpdateMessage(WorldClient client, int kamasTotal)
            {
                client.Send(new KamasUpdateMessage(kamasTotal));
            }

            public static void SendSpellUpgradeSuccessMessage(WorldClient client, SpellItem spellItem)
            {
                client.Send(new SpellUpgradeSuccessMessage(spellItem.spellId, spellItem.spellLevel));
            }
            public static void SendSpellUpgradeFailureMessage(WorldClient client)
            {
                client.Send(new SpellUpgradeFailureMessage());
            }

            public static void SendSpellListMessage(WorldClient client, bool spellPrevisualization)
            {
                client.Send(new SpellListMessage(spellPrevisualization,
                    from entry in client.Character.Spells.GetSpells()
                    select entry.GetSpellItem()));
            }

            public static void SendInventoryContentMessage(WorldClient client)
            {
                var objects = new List<ObjectItem>();

                if (client?.Character?.Inventory != null)
                {
                    var visibleItems = client.Character.Inventory.GetItems()
                        .Where(entry =>
                        {
                            if (entry == null || entry.Template == null)
                                return false;

                            if (!MountManager.Instance.IsMountCertificateTemplate(entry.Template.Id))
                                return true;

                            return MountCertificateFactory.IsActiveCertificateItem(entry, client.Character.Id);
                        })
                        .Select(entry => entry.GetObjectItem());

                    objects.AddRange(visibleItems);
                }

                if (client?.Character?.EquippedMount != null)
                {
                    var mount = client.Character.EquippedMount;
                    objects.RemoveAll(x => x != null &&
                        x.position == (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT);

                    try
                    {
                        short displayTemplateId = (short)(mount.Template != null && mount.Template.Id > 0
                            ? mount.Template.Id
                            : mount.Record.TemplateId);

                        objects.Add(new ObjectItem(
                            (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT,
                            displayTemplateId,
                            0,
                            false,
                            MountCertificateFactory.BuildEffects(mount),
                            mount.Id,
                            1));
                    }
                    catch
                    {
                    }
                }

                client.Send(new InventoryContentAndPresetMessage(
                    objects,
                    (int)client.Character.Inventory.Kamas,
                    client.Character.Inventory.GetPresets()));

            }

            public static void SendInventoryWeightMessage(WorldClient client)
            {
                client.Send(new InventoryWeightMessage(client.Character.Inventory.GetWeight(),
                                                       client.Character.Inventory.GetWeightTotal()));
            }

            public static void SendObjectAddedMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ObjectAddedMessage(item.GetObjectItem()));
            }

            public static void SendObjectsAddedMessage(WorldClient client, System.Collections.Generic.IEnumerable<BasePlayerItem> addeditems)
            {
                client.Send(new ObjectsAddedMessage(
                    from entry in addeditems
                    select entry.GetObjectItem()));
            }

            public static void SendObjectDeletedMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ObjectDeletedMessage(item.Id));
            }

            public static void SendObjectModifiedMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ObjectModifiedMessage(item.GetObjectItem()));
            }

            public static void SendObjectQuantityMessage(WorldClient client, BasePlayerItem item, int quantity)
            {
                client.Send(new ObjectQuantityMessage(item.Id, quantity));
            }

            public static void SendObjectDropMessage(WorldClient client, BasePlayerItem item, int quantity)
            {
                client.Send(new ObjectDropMessage(item.Id, quantity));
            }

            public static void SendObjectMovementMessage(WorldClient client, BasePlayerItem item)
            {
                client.Send(new ObjectMovementMessage(item.Id, (byte)item.Position));
            }

            public static void SendObjectErrorMessage(WorldClient client, ObjectErrorEnum error)
            {
                client.Send(new ObjectErrorMessage((sbyte)error));
            }

            public static void SendInventoryPresetUpdateMessage(WorldClient client, Protocol.Types.Preset preset)
            {
                client.Send(new InventoryPresetUpdateMessage(preset));
            }

            public static void SendInventoryPresetSaveResultMessage(WorldClient client, sbyte presetId, PresetSaveResultEnum result)
            {
                client.Send(new InventoryPresetSaveResultMessage(presetId, (sbyte)result));
            }

            public static void SendInventoryPresetDeleteResultMessage(WorldClient client, sbyte presetId, PresetDeleteResultEnum result)
            {
                client.Send(new InventoryPresetDeleteResultMessage(presetId, (sbyte)result));
            }

            public static void SendInventoryPresetUseResultMessage(WorldClient client, sbyte presetId, PresetUseResultEnum result, IEnumerable<byte> unlinkedPosition)
            {
                client.Send(new InventoryPresetUseResultMessage(presetId, (sbyte)result, unlinkedPosition));
            }

            public static void SendInventoryPresetUpdateErrorMessage(WorldClient client, PresetSaveUpdateErrorEnum result)
            {
                client.Send(new InventoryPresetItemUpdateErrorMessage((sbyte)result));
            }

            public static void SendStorageInventoryContentMessage(WorldClient client, TaxCollector taxCollector)
            {
                client.Send(new ExchangeStartedMessage((sbyte)ExchangeTypeEnum.TAXCOLLECTOR));
                client.Send(new StorageInventoryContentMessage(taxCollector.Inventory.GetItems().Select(x => x.GetObjectItem()), taxCollector.Inventory.GatheredKamas));
            }

            public static void SendStorageInventoryContentMessage(WorldClient client, House house)
            {
                client.Send(new ExchangeStartedWithStorageMessage((sbyte)ExchangeTypeEnum.STORAGE, House.MaxChestWeight));
                client.Send(new StorageInventoryContentMessage(
                    house.GetChestItems().Select(x => x.GetObjectItem()),
                    (int)house.ChestKamas));
            }
        }
    }

