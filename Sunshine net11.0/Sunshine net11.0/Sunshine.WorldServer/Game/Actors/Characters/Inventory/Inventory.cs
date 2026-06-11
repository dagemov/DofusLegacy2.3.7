using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.MySql.Database.World.Characters.Items;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.Protocol.Types;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Handlers.Basic;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Effects.Items;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Game.Items.Custom;

namespace Sunshine.WorldServer.Game.Actors.Characters.Inventory
{
    public class Inventory
    {
        public const int DefaultTokenTemplateId = 12124;

        private Character _character;
        private Dictionary<int, List<BasePlayerItem>> _items;
        private Dictionary<int, List<Effect>> _itemSetsEffects;
        public List<CharacterPresetRecord> Presets { get; set; }
        public BasePlayerItem TokenItem { get; private set; }

        public Inventory(Character character)
        {
            _character = character;
            _items = CharacterManager.Instance.GetCharacterItems(character.Id);
            _itemSetsEffects = new Dictionary<int, List<Effect>>();
            Presets = CharacterManager.Instance.GetCharacterPresets(character.Id);

            MigratePersistedTokenItemsToAccount();
            SyncTokensFromAccount(false);

            foreach (var item in GetEquipedItems())
            {
                ApplyItemEffects(item, true, false);
                ApplyItemSetEffects(item, false);
            }
        }

        public int Kamas
        {
            get { return _character.Record.Kamas; }
            set { _character.Record.Kamas = value; }
        }

        public bool IsFull(BasePlayerItem item, int stack)
        {
            return (GetWeight() + (item.Weight * stack)) >= GetWeightTotal();
        }

        public bool IsFull()
        {
            return GetWeight() >= GetWeightTotal();
        }

        public int GetWeight()
        {
            return GetItems().Where(x => !IsTokenItem(x)).Sum(x => x.Weight);
        }

        public int GetWeightTotal()
        {
            return 1000 + _character.Stats[StatsEnum.Strength].Total * 5 + _character.Jobs.GetJobsLevelTotal() * 5 + _character.Jobs.GetJobsCount(100) * 1000 + _character.Stats[StatsEnum.Weight].Total;
        }

        public IEnumerable<BasePlayerItem> GetItems()
        {
            List<BasePlayerItem> playerItems = new List<BasePlayerItem>();

            foreach (var items in _items.Values)
                items.ForEach(x => playerItems.Add(x));

            if (TokenItem != null)
                playerItems.Add(TokenItem);

            return playerItems;
        }

        public IEnumerable<BasePlayerItem> GetItems(CharacterInventoryPositionEnum position)
        {
            List<BasePlayerItem> playerItems = new List<BasePlayerItem>();

            foreach (var items in _items.Values)
            {
                foreach (var item in items.Where(x => x.Position == position))
                    playerItems.Add(item);
            }

            if (TokenItem != null && TokenItem.Position == position)
                playerItems.Add(TokenItem);

            return playerItems;
        }

        public IEnumerable<BasePlayerItem> GetEquipedItems()
        {
            List<BasePlayerItem> playerItems = new List<BasePlayerItem>();

            foreach (var items in _items.Values)
            {
                foreach (var item in items.Where(x => x.IsEquiped()))
                    playerItems.Add(item);
            }

            return playerItems;
        }

        public BasePlayerItem GetItemUid(int uid)
        {
            if (TokenItem != null && TokenItem.Id == uid)
                return TokenItem;

            foreach (var items in _items.Values)
            {
                var item = items.FirstOrDefault(x => x.Id == uid);
                if (item != null)
                    return item;
            }

            return null;
        }

        public BasePlayerItem GetItem(int guid)
        {
            if (IsTokenTemplate(guid))
                return TokenItem;

            if (_items.ContainsKey(guid))
                return _items[guid].FirstOrDefault();

            return null;
        }

        public BasePlayerItem GetItem(int guid, CharacterInventoryPositionEnum position)
        {
            if (IsTokenTemplate(guid))
                return TokenItem != null && TokenItem.Position == position ? TokenItem : null;

            if (_items.ContainsKey(guid))
                return _items[guid].FirstOrDefault(x => x.Position == position);

            return null;
        }

        public BasePlayerItem GetItem(int guid, CharacterInventoryPositionEnum position, List<Effect> effects)
        {
            if (IsTokenTemplate(guid))
                return TokenItem != null && TokenItem.Position == position ? TokenItem : null;

            if (_items.ContainsKey(guid))
            {
                return _items[guid].FirstOrDefault(x =>
                    x.Position == position &&
                    IsSameEffects(x.Effects, effects));
            }

            return null;
        }

        public BasePlayerItem GetItem(CharacterInventoryPositionEnum position)
        {
            foreach (var items in _items.Values)
            {
                var item = items.FirstOrDefault(x => x.Position == position);
                if (item != null)
                    return item;
            }

            return null;
        }

        public IEnumerable<BasePlayerItem> GetItemSetsEquiped(int itemSet)
        {
            return GetEquipedItems().Where(x => x.Template.ItemSetId == itemSet);
        }

        public void AddItem(BasePlayerItem item, int quantity = 1, bool isJetMax = false)
        {
            if (item == null)
                return;

            if (IsTokenTemplate(item.Template.Id))
            {
                AddTokens(quantity > 0 ? quantity : item.Stack);
                return;
            }

            BasePlayerItem sameItem = null;

            if (HasRawObjectEffects(item))
                quantity = 1;
            else
                sameItem = GetItemForStack(item);

            if (sameItem != null) // Stack
            {
                _items[item.Template.Id].Remove(sameItem);
                sameItem.Stack += quantity;
                _items[item.Template.Id].Add(sameItem);
                InventoryHandler.SendObjectModifiedMessage(_character.Client, sameItem);
            }
            else // Add
            {
                item.Stack = quantity;

                if (_items.ContainsKey(item.Template.Id))
                    _items[item.Template.Id].Add(item);
                else
                    _items.Add(item.Template.Id, new List<BasePlayerItem>() { item });

                InventoryHandler.SendObjectAddedMessage(_character.Client, item);
            }

            InventoryHandler.SendInventoryWeightMessage(_character.Client);
            _character.Shortcuts.SynchronizeItemShortcuts();
            SynchronizePresetObjects();
            if (_character?.Client != null)
                Handlers.Characters.Shorcuts.ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
        }

        public void RemoveItem(BasePlayerItem item, int quantity = 1)
        {
            if (item == null)
                return;

            if (IsTokenItem(item) || IsTokenTemplate(item.Template.Id))
            {
                RemoveTokens(quantity);
                return;
            }

            var sameItem = HasRawObjectEffects(item) ? item : GetItemForStack(item);

            if (sameItem != null)
            {
                _items[item.Template.Id].Remove(sameItem);

                if (sameItem.Stack - quantity <= 0)
                {
                    if (_items[item.Template.Id].Count == 0)
                        _items.Remove(item.Template.Id);

                    _character.Shortcuts.RemoveItemShortcutsByItemUid(sameItem.Id);
                    InventoryHandler.SendObjectDeletedMessage(_character.Client, sameItem);
                }
                else
                {
                    sameItem.Stack -= quantity;
                    _items[item.Template.Id].Add(sameItem);
                    InventoryHandler.SendObjectModifiedMessage(_character.Client, sameItem);
                }
            }
            else
            {
                if (_items.ContainsKey(item.Template.Id))
                {
                    _items[item.Template.Id].Remove(item);

                    if (_items[item.Template.Id].Count == 0)
                        _items.Remove(item.Template.Id);

                    _character.Shortcuts.RemoveItemShortcutsByItemUid(item.Id);
                    InventoryHandler.SendObjectDeletedMessage(_character.Client, item);
                }
                else
                {
                    _character.SendServerMessage($"Item {item.Template.Id} doesn't exist in your inventory !");
                }
            }

            InventoryHandler.SendInventoryWeightMessage(_character.Client);
            _character.Shortcuts.SynchronizeItemShortcuts();
            SynchronizePresetObjects();
            if (_character?.Client != null)
                Handlers.Characters.Shorcuts.ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
        }

        public void MoveItem(BasePlayerItem item, CharacterInventoryPositionEnum position)
        {
            if (!HasItem(item))
                return;

            if (position == item.Position)
                return;

            if (item is LivingObjectItem livingObject && position != CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
            {
                if (!TryEquipLivingObjectOnItem(livingObject, position))
                    _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 161);

                return;
            }

            if (position != CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
                UnEquipedDouble(item);

            if (IsEquipPosition(position))
            {
                if (!CanEquip(item, position))
                    return;

                EquipedItem(item, position);
                _character.UpdateLook(item);
            }
            else
            {
                bool refreshLookAfterMove = IsEquipment(item);

                var sameItem = HasRawObjectEffects(item)
                    ? null
                    : GetItemForStack(item.Template.Id, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED, item);

                if (sameItem != null) // Stack
                    UnEquipedItem(item, sameItem, position, true);
                else
                    UnEquipedItem(item, null, position);

                if (refreshLookAfterMove)
                    _character.UpdateLook(item, true);
            }

            if (_character.Quests.HasQuest(489) && _character.Map.Id == 35651584)
                _character.Quests.UpdateObjective(1044, 3502, true, true);

            InventoryHandler.SendInventoryWeightMessage(_character.Client);
            _character.Shortcuts.SynchronizeItemShortcuts();
            SynchronizePresetObjects();
        }

        public void SetKamas(int amount)
        {
            Kamas += amount;
            InventoryHandler.SendKamasUpdateMessage(_character.Client, Kamas);
        }

        public void UnEquipedDouble(BasePlayerItem item)
        {
            if (item.Type == ItemTypeEnum.DOFUS || item.Type == ItemTypeEnum.TROPHEE)
            {
                var itemDouble = GetItems().FirstOrDefault(x => x.IsEquiped() && x.Template.Id == item.Template.Id);
                if (itemDouble != null)
                {
                    MoveItem(itemDouble, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                    return;
                }
            }
            else
            {
                if (item.Type != ItemTypeEnum.ANNEAU)
                    return;

                var ring = GetItems().FirstOrDefault(x => x.IsEquiped() && x.Template.Id == item.Template.Id);
                if (ring != null)
                {
                    MoveItem(ring, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                    return;
                }
            }
        }

        public bool CanEquip(BasePlayerItem item, CharacterInventoryPositionEnum position)
        {
            if (!Game.Items.ItemCriteriaEvaluator.IsRespected(_character, item?.Template?.Criteria))
                return false;

            if (_character.Level < item.Level)
                return false;

            if (item.IsEquiped())
                return false;

            if (_character.IsInFight() && _character.Fight.State != FightStateEnum.Placement)
                return false;

            if (position == CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED)
                return true;

            if (!IsPossiblePosition(item, position))
                return false;

            bool isPetEquipment = position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS &&
                                  (item.Type == ItemTypeEnum.FAMILIER || item.Type == ItemTypeEnum.MONTILIER);
            if (isPetEquipment && _character.EquippedMount != null &&
                !Handlers.Mounts.MountHandler.TryDismountCurrentMount(_character.Client))
                return false;

            var itemExit = GetItem(position);
            if (itemExit != null)
                MoveItem(itemExit, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);

            var weapon = GetItem(CharacterInventoryPositionEnum.ACCESSORY_POSITION_WEAPON);
            if (item.Type == ItemTypeEnum.BOUCLIER && weapon != null && weapon.Template.TwoHanded)
            {
                BasicHandler.SendTextInformationMessage(_character.Client, TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 78);
                MoveItem(weapon, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                return true;
            }

            var shield = GetItem(CharacterInventoryPositionEnum.ACCESSORY_POSITION_SHIELD);
            if (shield != null && (item.Template is WeaponTemplate && item.Template.TwoHanded))
            {
                BasicHandler.SendTextInformationMessage(_character.Client, TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 79);
                MoveItem(shield, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                return true;
            }

            return true;
        }

        public bool HasItem(BasePlayerItem item)
        {
            if (item == null)
                return false;

            if (IsTokenItem(item))
                return TokenItem != null;

            if (_items.ContainsKey(item.Template.Id))
            {
                if (_items[item.Template.Id].FirstOrDefault(x => x == item) != null)
                    return true;

                return false;
            }
            else
                return false;
        }

        public bool HasItem(int item)
        {
            if (IsTokenTemplate(item))
                return TokenItem != null;

            return _items.ContainsKey(item);
        }

        public bool IsSameEffects(List<Effect> effects, List<Effect> secondEffects)
        {
            effects = effects ?? new List<Effect>();
            secondEffects = secondEffects ?? new List<Effect>();

            if (effects.Count != secondEffects.Count)
                return false;

            if (effects.Count == 0 && secondEffects.Count == 0)
                return true;

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Id != secondEffects[i].Id)
                    return false;

                if (effects[i].Value != secondEffects[i].Value)
                    return false;
            }

            return true;
        }

        public bool IsEquipPosition(CharacterInventoryPositionEnum position)
        {
            if (position >= CharacterInventoryPositionEnum.ACCESSORY_POSITION_AMULET
                && position <= CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT)
                return true;

            return false;
        }

        private ItemTypeEnum GetEquipmentType(BasePlayerItem item)
        {
            if (item is CommonLivingObject livingObject && item.Type == ItemTypeEnum.OBJET_VIVANT && livingObject.SupportedItemType > 0)
                return (ItemTypeEnum)livingObject.SupportedItemType;

            return item != null ? item.Type : 0;
        }

        private bool TryEquipLivingObjectOnItem(LivingObjectItem livingObject, CharacterInventoryPositionEnum position)
        {
            if (livingObject == null)
                return false;

            livingObject.InitializeFromCurrentState();

            var host = GetItem(position);
            if (host == null || ReferenceEquals(host, livingObject) || host.Template == null)
                return false;

            if (!livingObject.TryBindTo(host))
                return false;

            RemoveItem(livingObject, 1);
            host = FinalizeItemRuntime(host) ?? host;
            InventoryHandler.SendObjectModifiedMessage(_character.Client, host);
            _character.UpdateLook(!string.IsNullOrWhiteSpace(_character.CustomLook));
            InventoryHandler.SendInventoryWeightMessage(_character.Client);
            _character.Shortcuts.SynchronizeItemShortcuts();
            SynchronizePresetObjects();
            return true;
        }

        public bool IsPossiblePosition(BasePlayerItem item, CharacterInventoryPositionEnum position)
        {
            switch (GetEquipmentType(item))
            {
                case ItemTypeEnum.CHAPEAU:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_HAT;

                case ItemTypeEnum.CAPE:
                case ItemTypeEnum.SAC_A_DOS:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_CAPE;

                case ItemTypeEnum.BOTTES:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_BOOTS;

                case ItemTypeEnum.ANNEAU:
                    return position == CharacterInventoryPositionEnum.INVENTORY_POSITION_RING_LEFT ||
                           position == CharacterInventoryPositionEnum.INVENTORY_POSITION_RING_RIGHT;

                case ItemTypeEnum.AMULETTE:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_AMULET;

                case ItemTypeEnum.CEINTURE:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_BELT;

                case ItemTypeEnum.BOUCLIER:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_SHIELD;

                case ItemTypeEnum.DOFUS:
                    return position == CharacterInventoryPositionEnum.INVENTORY_POSITION_DOFUS_1 ||
                           position == CharacterInventoryPositionEnum.INVENTORY_POSITION_DOFUS_2 ||
                           position == CharacterInventoryPositionEnum.INVENTORY_POSITION_DOFUS_3 ||
                           position == CharacterInventoryPositionEnum.INVENTORY_POSITION_DOFUS_4 ||
                           position == CharacterInventoryPositionEnum.INVENTORY_POSITION_DOFUS_5 ||
                           position == CharacterInventoryPositionEnum.INVENTORY_POSITION_DOFUS_6;

                case ItemTypeEnum.ARC:
                case ItemTypeEnum.DAGUE:
                case ItemTypeEnum.MARTEAU:
                case ItemTypeEnum.BAGUETTE:
                case ItemTypeEnum.BATON:
                case ItemTypeEnum.EPEE:
                case ItemTypeEnum.HACHE:
                case ItemTypeEnum.FAUX:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_WEAPON;

                case ItemTypeEnum.FAMILIER:
                case ItemTypeEnum.MONTILIER:
                    return position == CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS;

                default:
                    return false;
            }
        }

        public bool IsEquipment(BasePlayerItem item)
        {
            var type = GetEquipmentType(item);
            return type >= ItemTypeEnum.AMULETTE && type <= ItemTypeEnum.BOTTES ||
                   type >= ItemTypeEnum.CHAPEAU && type <= ItemTypeEnum.HACHE ||
                   type == ItemTypeEnum.PIOCHE ||
                   type == ItemTypeEnum.DOFUS ||
                   type == ItemTypeEnum.BOUCLIER;
        }

        private void EquipedItem(BasePlayerItem item, CharacterInventoryPositionEnum position)
        {
            if (item.Stack > 1)
            {
                var newItem = item.Clone();
                newItem.Id = ItemManager.Instance.GenerateId();
                newItem.Stack = item.Stack - 1;

                _items[item.Template.Id].Remove(item);

                item.Stack = 1;
                InventoryHandler.SendObjectModifiedMessage(_character.Client, item);

                _items[item.Template.Id].Add(newItem);
                InventoryHandler.SendObjectAddedMessage(_character.Client, newItem);

                item.Position = position;
                _items[item.Template.Id].Add(item);
                InventoryHandler.SendObjectMovementMessage(_character.Client, item);
            }
            else
            {
                _items[item.Template.Id].Remove(item);
                item.Position = position;
                _items[item.Template.Id].Add(item);
                InventoryHandler.SendObjectMovementMessage(_character.Client, item);
            }

            ApplyItemEffects(item, true);
            ApplyItemSetEffects(item);
        }

        private void UnEquipedItem(BasePlayerItem item, BasePlayerItem sameItem, CharacterInventoryPositionEnum position, bool isStacked = false)
        {
            if (isStacked)
            {
                _items[item.Template.Id].Remove(item);
                InventoryHandler.SendObjectDeletedMessage(_character.Client, item);

                _items[item.Template.Id].Remove(sameItem);
                item.Position = position;
                sameItem.Stack += 1;
                _items[item.Template.Id].Add(sameItem);
                InventoryHandler.SendObjectModifiedMessage(_character.Client, sameItem);
            }
            else
            {
                _items[item.Template.Id].Remove(item);
                item.Position = position;
                _items[item.Template.Id].Add(item);
                InventoryHandler.SendObjectMovementMessage(_character.Client, item);
            }

            ApplyItemEffects(item, false);
            ApplyItemSetEffects(item);
        }

        private void ApplyItemEffects(BasePlayerItem item, bool apply, bool send = true)
        {
            if (item == null || item.Effects == null)
                return;

            foreach (var effect in item.Effects)
            {
                if (ItemEffectHandler.TryGetRelatedStat(effect.Id, out var stats))
                {
                    var value = (short)Effects.EffectNumericResolver.GetNumericValue(effect);
                    if (ItemEffectHandler.IsNegativeEffectForStats(effect.Id))
                        value = (short)-value;

                    if (apply)
                        _character.Stats[stats].Equiped += value;
                    else
                        _character.Stats[stats].Equiped -= value;
                }
                else if (!ItemEffectHandler.ShouldIgnoreEffectForStats(effect.Id))
                {
                    Logs.Logger.WriteError($"{effect.Id} doesn't exist !");
                }
            }

            if (send)
                _character.RefreshStats();
        }

        private void ApplyItemEffects(List<Effect> effects, bool apply, bool send = true)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (ItemEffectHandler.TryGetRelatedStat(effect.Id, out var stats))
                {
                    var value = (short)Effects.EffectNumericResolver.GetNumericValue(effect);
                    if (ItemEffectHandler.IsNegativeEffectForStats(effect.Id))
                        value = (short)-value;

                    if (apply)
                        _character.Stats[stats].Equiped += value;
                    else
                        _character.Stats[stats].Equiped -= value;
                }
                else if (!ItemEffectHandler.ShouldIgnoreEffectForStats(effect.Id))
                {
                    Logs.Logger.WriteError($"{effect.Id} doesn't exist !");
                }
            }

            if (send)
                _character.RefreshStats();
        }

        private void ApplyItemSetEffects(BasePlayerItem item, bool send = true)
        {
            List<Effect> effects = null;
            int effectsCount = 0;

            if (item.EffectSets == null || item.EffectSets.Count <= 0)
                return;

            if (_itemSetsEffects.ContainsKey(item.Template.ItemSetId))
            {
                effects = _itemSetsEffects[item.Template.ItemSetId];
                ApplyItemEffects(effects, false, send);
                _itemSetsEffects.Remove(item.Template.ItemSetId);
            }

            var itemSetsCount = GetItemSetsEquiped(item.Template.ItemSetId).Count();
            if (itemSetsCount <= 1)
                return;

            if (itemSetsCount >= (item.EffectSets.Count - 1))
                effectsCount = itemSetsCount - 2;
            else
                effectsCount = (itemSetsCount - (item.EffectSets.Count - 1)) + 2;

            effects = item.EffectSets[effectsCount <= 0 ? 0 : effectsCount];
            _itemSetsEffects.Add(item.Template.ItemSetId, effects);
            ApplyItemEffects(effects, true, send);
        }

        private bool HasRawObjectEffects(BasePlayerItem item)
        {
            return item != null &&
                   item.RawObjectEffects != null &&
                   item.RawObjectEffects.Count > 0;
        }

        private BasePlayerItem GetItemForStack(BasePlayerItem item)
        {
            if (item == null)
                return null;

            return GetItemForStack(item.Template.Id, item.Position, item);
        }

        private BasePlayerItem GetItemForStack(int templateId, CharacterInventoryPositionEnum position, BasePlayerItem compareItem)
        {
            if (!_items.ContainsKey(templateId))
                return null;

            foreach (var entry in _items[templateId])
            {
                if (entry.Position != position)
                    continue;

                if (HasRawObjectEffects(entry) || HasRawObjectEffects(compareItem))
                    continue;

                if (IsSameEffects(entry.Effects, compareItem.Effects))
                    return entry;
            }

            return null;
        }

        public void ReplaceItemReference(BasePlayerItem previous, BasePlayerItem current)
        {
            if (previous == null || current == null)
                return;

            foreach (var pair in _items.ToList())
            {
                int index = pair.Value.FindIndex(x => ReferenceEquals(x, previous) || x.Id == previous.Id);
                if (index < 0)
                    continue;

                pair.Value.RemoveAt(index);

                if (!_items.ContainsKey(current.Template.Id))
                    _items[current.Template.Id] = new List<BasePlayerItem>();

                _items[current.Template.Id].Add(current);
                _character.Shortcuts.RebindItemShortcuts(previous.Id, current);
                SynchronizePresetObjects();
                return;
            }
        }

        public BasePlayerItem FinalizeItemRuntime(BasePlayerItem item)
        {
            if (item == null)
                return null;

            var finalized = ItemManager.Instance.FinalizePlayerItem(item);
            if (!ReferenceEquals(finalized, item))
                ReplaceItemReference(item, finalized);

            _character.Shortcuts.SynchronizeItemShortcuts();
            return finalized;
        }

        public int GetTokens()
        {
            return Math.Max(0, _character?.Account?.Tokens ?? 0);
        }

        public void AddTokens(int amount, bool notifyClient = true)
        {
            if (amount <= 0)
                return;

            SetTokens(GetTokens() + amount, notifyClient);
        }

        public void RemoveTokens(int amount, bool notifyClient = true)
        {
            if (amount <= 0)
                return;

            SetTokens(GetTokens() - amount, notifyClient);
        }

        public void SetTokens(int amount, bool notifyClient = true)
        {
            if (_character?.Account == null)
                return;

            if (amount < 0)
                amount = 0;

            _character.Account.Tokens = amount;

            if (_character.Client?.Account != null)
            {
                _character.Client.Account.Tokens = amount;
                _character.Client.Account.NewTokens = _character.Account.NewTokens;
            }

            SyncTokensFromAccount(notifyClient);
            AccountManager.Instance.UpdateAccountTokens(_character.Account);
        }

        public int MergePendingTokens(bool notifyClient = true)
        {
            if (_character?.Account == null)
                return 0;

            var latestAccount = AccountManager.Instance.GetAccountById(_character.Account.Id);
            if (latestAccount != null)
            {
                _character.Account.Tokens = Math.Max(0, latestAccount.Tokens);
                _character.Account.NewTokens = Math.Max(0, latestAccount.NewTokens);

                if (_character.Client?.Account != null)
                {
                    _character.Client.Account.Tokens = _character.Account.Tokens;
                    _character.Client.Account.NewTokens = _character.Account.NewTokens;
                }
            }

            int received = Math.Max(0, _character.Account.NewTokens);
            if (received > 0)
            {
                _character.Account.Tokens = Math.Max(0, _character.Account.Tokens) + received;
                _character.Account.NewTokens = 0;

                if (_character.Client?.Account != null)
                {
                    _character.Client.Account.Tokens = _character.Account.Tokens;
                    _character.Client.Account.NewTokens = 0;
                }

                AccountManager.Instance.UpdateAccountTokens(_character.Account);
            }

            SyncTokensFromAccount(notifyClient);
            return received;
        }

        public void SyncTokensFromAccount(bool notifyClient = true)
        {
            int amount = Math.Max(0, _character?.Account?.Tokens ?? 0);
            var template = ItemManager.Instance.Items.ContainsKey(DefaultTokenTemplateId)
                ? ItemManager.Instance.Items[DefaultTokenTemplateId]
                : null;

            if (template == null)
            {
                TokenItem = null;
                return;
            }

            if (amount <= 0)
            {
                var removedToken = TokenItem;
                TokenItem = null;

                if (notifyClient && removedToken != null && _character?.Client != null)
                    InventoryHandler.SendObjectDeletedMessage(_character.Client, removedToken);

                return;
            }

            if (TokenItem == null)
            {
                TokenItem = ItemManager.Instance.CreateTypedPlayerItem(DefaultTokenTemplateId) ?? new BasePlayerItem(template);
                TokenItem.Id = 2000000000 - _character.Id;
                TokenItem.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
                TokenItem.Stack = amount;
                TokenItem.Effects = template.EffectsBase != null ? template.EffectsBase.Clone() : new List<Effect>();
                TokenItem.EffectSets = template.EffectSets;
                TokenItem.RawObjectEffects = new List<ObjectEffect>();
                TokenItem.EnsureRuntimeEffects();

                if (notifyClient && _character?.Client != null)
                    InventoryHandler.SendObjectAddedMessage(_character.Client, TokenItem);
            }
            else
            {
                TokenItem.Stack = amount;
                TokenItem.EnsureRuntimeEffects();
                if (notifyClient && _character?.Client != null)
                    InventoryHandler.SendObjectModifiedMessage(_character.Client, TokenItem);
            }
        }

        public void CreateTokenItem(int amount)
        {
            SetTokens(amount, false);
        }

        private void MigratePersistedTokenItemsToAccount()
        {
            if (!_items.ContainsKey(DefaultTokenTemplateId))
                return;

            var tokenEntries = _items[DefaultTokenTemplateId].Where(x => x != null).ToList();
            int total = tokenEntries.Sum(x => Math.Max(0, x.Stack));
            _items.Remove(DefaultTokenTemplateId);

            if (_character?.Account != null && total > 0)
            {
                _character.Account.Tokens += total;

                if (_character.Client?.Account != null)
                    _character.Client.Account.Tokens = _character.Account.Tokens;

                AccountManager.Instance.UpdateAccountTokens(_character.Account);
            }
        }

        private static bool IsTokenTemplate(int templateId)
        {
            return templateId == DefaultTokenTemplateId;
        }

        private bool IsTokenItem(BasePlayerItem item)
        {
            return item != null && TokenItem != null && (ReferenceEquals(item, TokenItem) || item.Id == TokenItem.Id) && IsTokenTemplate(item.Template.Id);
        }

        public void SynchronizePresetObjects(bool notifyClient = false)
        {
            if (Presets == null)
                return;

            foreach (var preset in Presets.ToList())
            {
                preset.EnsureDeserialized();
                bool changed = false;
                var normalized = new List<Protocol.Types.PresetItem>();

                foreach (var presetItem in (preset.Objects ?? new List<Protocol.Types.PresetItem>()).Where(x => x != null))
                {
                    if (IsMountPresetPosition(presetItem.position))
                    {
                        normalized.Add(presetItem);
                        continue;
                    }

                    var linked = GetItemUid(presetItem.objUid) ?? GetItems().FirstOrDefault(x => x.Template.Id == presetItem.objGid);
                    if (linked == null)
                    {
                        changed = true;
                        continue;
                    }

                    if (linked.Id != presetItem.objUid || linked.Template.Id != presetItem.objGid)
                        changed = true;

                    normalized.Add(new Protocol.Types.PresetItem(presetItem.position, linked.Template.Id, linked.Id));
                }

                if (!changed)
                    continue;

                preset.SetObjects(normalized);

                if (notifyClient && _character.Client != null)
                    InventoryHandler.SendInventoryPresetUpdateMessage(_character.Client, preset.GetNetworkPreset());
            }

            _character.Shortcuts.SynchronizePresetShortcuts(notifyClient);
        }

        public IEnumerable<CharacterPresetRecord> GetPresetRecords()
        {
            return Presets ?? new List<CharacterPresetRecord>();
        }

        public IEnumerable<Protocol.Types.Preset> GetPresets()
        {
            return (Presets ?? new List<CharacterPresetRecord>()).Select(x => x.GetNetworkPreset());
        }

        public CharacterPresetRecord GetPreset(int presetId)
        {
            return (Presets ?? new List<CharacterPresetRecord>()).FirstOrDefault(x => x.PresetId == presetId);
        }

        private static bool IsMountPresetPosition(byte position)
        {
            return position == (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT;
        }

        private static bool IsMountOrPetPresetPosition(byte position)
        {
            return position == (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT ||
                   position == (byte)CharacterInventoryPositionEnum.ACCESSORY_POSITION_PETS;
        }

        private void UnequipItemsMissingFromPreset(List<Protocol.Types.PresetItem> presetItems)
        {
            presetItems = presetItems ?? new List<Protocol.Types.PresetItem>();

            var expectedItemsByPosition = presetItems
                .Where(x => x != null && !IsMountOrPetPresetPosition(x.position))
                .GroupBy(x => (CharacterInventoryPositionEnum)x.position)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var equippedItem in GetEquipedItems().ToList())
            {
                Protocol.Types.PresetItem expectedItem;
                if (!expectedItemsByPosition.TryGetValue(equippedItem.Position, out expectedItem))
                {
                    MoveItem(equippedItem, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
                    continue;
                }

                if (equippedItem.Id != expectedItem.objUid && equippedItem.Template.Id != expectedItem.objGid)
                    MoveItem(equippedItem, CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED);
            }

            // Important: a preset without a mount entry must not unequip the current mount.
            // Otherwise using a regular equipment preset strips the mount and converts it back to a certificate.
            // We only switch/unequip mounts later when the preset explicitly targets another mount.
        }

        public PresetSaveResultEnum AddPreset(sbyte presetId, sbyte symbolId, bool saveEquipment)
        {
            if (presetId < 0)
                return PresetSaveResultEnum.PRESET_SAVE_ERR_UNKNOWN;

            var equipped = GetEquipedItems().Where(x => x.IsEquiped()).ToList();
            bool hasEquippedMount = _character.EquippedMount != null;

            var preset = GetPreset(presetId);
            var presetItems = equipped
                .Select(x => new Protocol.Types.PresetItem((byte)x.Position, x.Template.Id, x.Id))
                .ToList();

            if (saveEquipment && _character.EquippedMount != null)
            {
                presetItems.RemoveAll(x => IsMountOrPetPresetPosition(x.position));
                presetItems.Add(new Protocol.Types.PresetItem(
                    (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT,
                    _character.EquippedMount.Record.TemplateId,
                    _character.EquippedMount.Id));
            }

            if (preset == null)
            {
                preset = new CharacterPresetRecord
                {
                    OwnerId = _character.Id,
                    PresetId = presetId,
                    SymbolId = symbolId
                };
                Presets.Add(preset);
            }
            else
            {
                preset.SymbolId = symbolId;
            }

            preset.SetObjects(presetItems);
            SynchronizePresetObjects();
            _character.Shortcuts.EnsurePresetShortcut(presetId);
            return PresetSaveResultEnum.PRESET_SAVE_OK;
        }

        public PresetDeleteResultEnum RemovePreset(sbyte presetId)
        {
            var preset = GetPreset(presetId);
            if (preset == null)
                return PresetDeleteResultEnum.PRESET_DEL_ERR_BAD_PRESET_ID;

            Presets.Remove(preset);
            _character.Shortcuts.RemovePresetShortcut(presetId);
            SynchronizePresetObjects();
            return PresetDeleteResultEnum.PRESET_DEL_OK;
        }

        public PresetSaveUpdateErrorEnum RemovePresetItem(sbyte presetId, byte position)
        {
            var preset = GetPreset(presetId);
            if (preset == null)
                return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_BAD_PRESET_ID;

            preset.EnsureDeserialized();
            var item = IsMountPresetPosition(position)
                ? preset.Objects.FirstOrDefault(x => IsMountPresetPosition(x.position))
                : preset.Objects.FirstOrDefault(x => x.position == position);
            if (item == null)
                return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_BAD_POSITION;

            preset.Objects.Remove(item);
            preset.SetObjects(preset.Objects);
            SynchronizePresetObjects();
            return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_UNKNOWN;
        }

        public PresetSaveUpdateErrorEnum UpdatePresetItem(sbyte presetId, byte position, int objUid)
        {
            var preset = GetPreset(presetId);
            if (preset == null)
                return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_BAD_PRESET_ID;

            Protocol.Types.PresetItem presetItem;

            if (IsMountPresetPosition(position))
            {
                var mount = _character.EquippedMount;
                if (mount == null || mount.Id != objUid)
                    mount = MountManager.Instance.GetMount(objUid);

                bool ownsMount = mount != null && mount.Record != null &&
                                 (mount.Record.OwnerId == _character.Id || (_character.Account != null && mount.Record.OwnerId == _character.Account.Id));
                if (!ownsMount)
                    return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_BAD_OBJECT_ID;

                presetItem = new Protocol.Types.PresetItem(
                    (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT,
                    mount.Record.TemplateId,
                    mount.Id);
            }
            else
            {
                var item = GetItemUid(objUid);
                if (item == null)
                    return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_BAD_OBJECT_ID;

                presetItem = new Protocol.Types.PresetItem(position, item.Template.Id, item.Id);
            }

            preset.EnsureDeserialized();
            preset.Objects.RemoveAll(x => IsMountPresetPosition(position) ? IsMountPresetPosition(x.position) : x.position == position);
            preset.Objects.Add(presetItem);
            preset.SetObjects(preset.Objects);
            SynchronizePresetObjects();
            return PresetSaveUpdateErrorEnum.PRESET_UPDATE_ERR_UNKNOWN;
        }

        public void EquipPreset(sbyte presetId)
        {
            var preset = GetPreset(presetId);
            if (preset == null)
            {
                InventoryHandler.SendInventoryPresetUseResultMessage(_character.Client, presetId, PresetUseResultEnum.PRESET_USE_ERR_BAD_PRESET_ID, new byte[0]);
                return;
            }

            preset.EnsureDeserialized();
            var presetItems = (preset.Objects ?? new List<Protocol.Types.PresetItem>()).Where(x => x != null).ToList();
            var unlinked = new List<byte>();

            UnequipItemsMissingFromPreset(presetItems);

            foreach (var presetItem in presetItems.OrderBy(x => x.position))
            {
                if (IsMountPresetPosition(presetItem.position))
                {
                    var mount = MountManager.Instance.GetMount(presetItem.objUid);
                    bool ownsMount = mount != null && mount.Record != null &&
                                     (mount.Record.OwnerId == _character.Id || (_character.Account != null && mount.Record.OwnerId == _character.Account.Id));
                    if (!ownsMount)
                    {
                        if (_character.EquippedMount?.Id != presetItem.objUid)
                            unlinked.Add(presetItem.position);
                        continue;
                    }

                    if (_character.EquippedMount?.Id == mount.Id)
                        continue;

                    MountCertificateFactory.EnsureMountIsRideable(mount);
                    if (!_character.IsInFight() && mount.IsRideable)
                    {
                        if (_character.EquippedMount != null && _character.EquippedMount.Id != mount.Id)
                        {
                            if (!Handlers.Mounts.MountHandler.TryUnequipCurrentMountToInventory(_character.Client))
                            {
                                unlinked.Add(presetItem.position);
                                continue;
                            }
                        }

                        var mountCertificate = GetItems()
                            .FirstOrDefault(entry =>
                            {
                                if (entry == null || entry.Template == null || !MountManager.Instance.IsMountCertificateTemplate(entry.Template.Id))
                                    return false;

                                Protocol.Types.ObjectEffectMount effect;
                                return MountCertificateFactory.TryGetMountEffect(entry, out effect) &&
                                       effect != null &&
                                       effect.mountId == mount.Id;
                            });

                        if (mountCertificate != null)
                            RemoveItem(mountCertificate, System.Math.Max(1, mountCertificate.Stack));

                        Handlers.Mounts.MountHandler.UnequipPetIfNeeded(_character.Client);
                        mount.Record.IsInStable = 0;
                        mount.Record.PaddockId = null;
                        mount.Record.StoredSince = null;
                        _character.EquippedMount = mount;
                        _character.IsRiding = false;
                        MountManager.Instance.Save(mount);
                        _character.Client.Send(new Protocol.Messages.MountSetMessage(mount.GetClientData()));
                        _character.Client.Send(new Protocol.Messages.MountXpRatioMessage((sbyte)System.Math.Max(0, System.Math.Min(90, (int)mount.Record.GivenExperience))));
                        _character.Client.Send(new Protocol.Messages.MountRidingMessage(false));
                        _character.OnLookRefreshed();
                        InventoryHandler.SendInventoryContentMessage(_character.Client);
                    }
                    else
                    {
                        unlinked.Add(presetItem.position);
                    }

                    continue;
                }

                var item = GetItemUid(presetItem.objUid) ?? GetItems().FirstOrDefault(x => x.Template.Id == presetItem.objGid);
                if (item == null)
                {
                    unlinked.Add(presetItem.position);
                    continue;
                }

                var targetPosition = (CharacterInventoryPositionEnum)presetItem.position;
                if (item.Position == targetPosition)
                    continue;

                MoveItem(item, targetPosition);
            }

            SynchronizePresetObjects(true);
            _character.Shortcuts.SynchronizeItemShortcuts(true);
            InventoryHandler.SendInventoryPresetUseResultMessage(_character.Client, presetId, unlinked.Count > 0 ? PresetUseResultEnum.PRESET_USE_OK_PARTIAL : PresetUseResultEnum.PRESET_USE_OK, unlinked);
            InventoryHandler.SendInventoryContentMessage(_character.Client);
        }
    }
}
