using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class ItemCrusherTrade : Trade
    {
        private const int MagicFragmentTemplateId = 8378;
        private const short MagicFragmentEffectId = 10000;
        private const int DefaultSkillId = 181;
        private const sbyte DefaultJobId = 1;
        private const sbyte DefaultMaxSlots = 10;

        private static readonly object SyncRoot = new object();
        private static Dictionary<EffectsEnum, List<RuneOutput>> _cachedRuneOutputs;

        public Trader Trader { get; }

        private int _skill;
        private sbyte _job;
        private sbyte _nbCase;

        public ItemCrusherTrade(Character trader)
        {
            Trader = new Trader(trader);
            Inventories = new Dictionary<RolePlayActor, List<Tuple<BasePlayerItem, int>>>();
            Replay = 1;
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.CRAFT;

        public override void Open(List<object> parameters = null)
        {
            _job = parameters != null && parameters.Count > 0 ? Convert.ToSByte(parameters[0]) : DefaultJobId;
            _nbCase = parameters != null && parameters.Count > 1 ? Convert.ToSByte(parameters[1]) : DefaultMaxSlots;
            _skill = parameters != null && parameters.Count > 2 ? Convert.ToInt32(parameters[2]) : DefaultSkillId;

            InventoryHandler.SendExchangeStartOkCraftWithInformationMessage(Trader.Client, _nbCase, _skill);
        }

        public override void Close(List<object> parameters = null)
        {
            Trader.Trade = null;
            Trader.Client.Character.Dialog = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            Trader.Trade = null;
            Trader.Client.Character.Dialog = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
            if (!ReferenceEquals(trader, Trader.Actor))
                return;

            if (isReady)
                Apply();
        }

        public override void SetKamas(Character trader, int quantity)
        {
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (!ReferenceEquals(trader, Trader.Actor))
                return;

            if (quantity == 0)
                return;

            if (Inventories.ContainsKey(trader) && quantity > 0 && Inventories[trader].Count >= _nbCase)
                return;

            var sourceItem = trader.Inventory.GetItemUid(objectUid);
            if (sourceItem == null || sourceItem.Template == null)
                return;

            if (!CanCrusherAccept(sourceItem))
                return;

            var item = sourceItem.Clone();

            if (!Inventories.ContainsKey(trader))
                Inventories[trader] = new List<Tuple<BasePlayerItem, int>>();

            var stock = Inventories[trader].FirstOrDefault(x => x.Item1.Id == item.Id);
            if (stock != null)
            {
                if (quantity < 0)
                {
                    var removed = Math.Abs(quantity);
                    if (stock.Item2 > removed)
                    {
                        item.Stack = stock.Item2 - removed;
                        InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, false, item);
                        Inventories[trader].Remove(stock);
                        Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                    }
                    else
                    {
                        InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, false, item);
                        Inventories[trader].Remove(stock);
                    }
                }
                else if (sourceItem.Stack - stock.Item2 >= quantity)
                {
                    item.Stack = stock.Item2 + quantity;
                    InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, false, item);
                    Inventories[trader].Remove(stock);
                    Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                }

                return;
            }

            if (quantity < sourceItem.Stack)
                item.Stack = quantity;

            InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, false, item);
            Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
        }

        public override void Apply()
        {
            if (!Inventories.TryGetValue(Trader.Actor, out var items) || items.Count == 0)
                return;

            var fragmentEffects = new Dictionary<int, int>();

            foreach (var entry in items.ToList())
            {
                var playerItem = Trader.Inventory.GetItemUid(entry.Item1.Id);
                if (playerItem == null)
                    continue;

                for (var i = 0; i < entry.Item2; i++)
                    CrushSingleItem(playerItem, fragmentEffects);

                Trader.Inventory.RemoveItem(playerItem, entry.Item2);
                InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, false, entry.Item1);
            }

            items.Clear();

            if (fragmentEffects.Count > 0)
            {
                var fragment = BuildMagicFragment(fragmentEffects);
                if (fragment != null)
                {
                    Trader.Inventory.AddItem(fragment, 1);
                    InventoryHandler.SendExchangeCraftResultWithObjectDescMessage(Trader.Client, fragment, CraftResultEnum.CRAFT_SUCCESS);
                }
                else
                {
                    Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
                }
            }
            else
            {
                Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
            }

            Close();
        }

        private static bool CanCrusherAccept(BasePlayerItem item)
        {
            if (item == null || item.Template == null)
                return false;

            if (item.Template.Id == MagicFragmentTemplateId)
                return false;

            var itemType = (ItemTypeEnum)item.Template.TypeId;
            if (itemType == ItemTypeEnum.RUNE_DE_FORGEMAGIE || itemType == ItemTypeEnum.POTION_DE_FORGEMAGIE)
                return false;

            return item.Effects != null && item.Effects.Any(x => x != null && Math.Max(x.Value, (int)Math.Max(x.DiceFace, x.DiceNum)) > 0);
        }

        private void CrushSingleItem(BasePlayerItem item, Dictionary<int, int> fragmentEffects)
        {
            if (item?.Effects == null || item.Template == null)
                return;

            var random = new AsyncRandom();
            foreach (var effect in item.Effects)
            {
                if (effect == null)
                    continue;

                var runeEffect = JobManager.Instance.GetRuneEffect(effect.Id);
                if (runeEffect == null || runeEffect.PEffect <= 0)
                    continue;

                if (!TryGetRuneOutput(effect.Id, out var output))
                    continue;

                var statValue = effect.Value > 0 ? effect.Value : (int)Math.Max(effect.DiceFace, effect.DiceNum);
                if (statValue <= 0)
                    continue;

                var prop = 15.0 * statValue * Math.Min(2.0 / 3.0, 1.5 * item.Template.Level * item.Template.Level / Math.Pow(Math.Max(1.0, runeEffect.PEffect), 1.25));
                prop *= 0.9 + (random.NextDouble() * 0.2);

                var rawAmount = (int)Math.Floor(prop);
                var runeCount = output.Amount <= 0 ? Math.Max(1, rawAmount) : Math.Max(1, (int)Math.Floor(rawAmount / (double)output.Amount));
                runeCount = ApplyRuneYieldNerf(output.TemplateId, runeCount);

                if (fragmentEffects.ContainsKey(output.TemplateId))
                    fragmentEffects[output.TemplateId] += runeCount;
                else
                    fragmentEffects[output.TemplateId] = runeCount;
            }
        }

        private static int ApplyRuneYieldNerf(int templateId, int amount)
        {
            if (amount <= 0)
                return 0;

            if (templateId == 1557 || templateId == 1558)
                return Math.Max(1, amount / 3);

            return amount;
        }

        private static BasePlayerItem BuildMagicFragment(Dictionary<int, int> fragmentEffects)
        {
            var rawEffects = fragmentEffects
                .Where(x => x.Key > 0 && x.Value > 0 && x.Key <= short.MaxValue && x.Value <= short.MaxValue)
                .OrderBy(x => x.Key)
                .Select(x => (ObjectEffect)new ObjectEffectDice(MagicFragmentEffectId, (short)x.Key, (short)x.Value, 0))
                .ToList();

            if (rawEffects.Count == 0)
                return null;

            return ItemManager.Instance.CreatePlayerItem(MagicFragmentTemplateId, new List<Effect>(), rawEffects, 1);
        }

        private static bool TryGetRuneOutput(EffectsEnum effectId, out RuneOutput output)
        {
            EnsureRuneOutputs();
            output = null;

            if (_cachedRuneOutputs == null)
                return false;

            if (!_cachedRuneOutputs.TryGetValue(effectId, out var outputs) || outputs == null || outputs.Count == 0)
                return false;

            output = outputs.OrderBy(x => x.Amount <= 0 ? 1 : x.Amount).FirstOrDefault();
            return output != null;
        }

        private static void EnsureRuneOutputs()
        {
            if (_cachedRuneOutputs != null)
                return;

            lock (SyncRoot)
            {
                if (_cachedRuneOutputs != null)
                    return;

                var outputs = new Dictionary<EffectsEnum, List<RuneOutput>>();
                foreach (var template in ItemManager.Instance.Items.Values.Where(x => x != null && (ItemTypeEnum)x.TypeId == ItemTypeEnum.RUNE_DE_FORGEMAGIE))
                {
                    if (template.EffectsBase == null)
                        continue;

                    foreach (var effect in template.EffectsBase.Where(x => x != null))
                    {
                        var amount = effect.Value > 0 ? effect.Value : (int)Math.Max(effect.DiceFace, effect.DiceNum);
                        if (amount <= 0)
                            amount = 1;

                        if (!outputs.TryGetValue(effect.Id, out var list))
                        {
                            list = new List<RuneOutput>();
                            outputs[effect.Id] = list;
                        }

                        if (!list.Any(x => x.TemplateId == template.Id))
                            list.Add(new RuneOutput(template.Id, amount));
                    }
                }

                _cachedRuneOutputs = outputs;
            }
        }

        private sealed class RuneOutput
        {
            public int TemplateId { get; }
            public int Amount { get; }

            public RuneOutput(int templateId, int amount)
            {
                TemplateId = templateId;
                Amount = amount;
            }
        }
    }
}
