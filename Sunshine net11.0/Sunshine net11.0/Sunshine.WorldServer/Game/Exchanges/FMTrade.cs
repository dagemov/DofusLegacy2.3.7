using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.Protocol.Types;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using System.Threading.Tasks;
using System.Threading;
using Sunshine.Protocol.Utils;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class FMTrade : Trade
    {
        public Trader Trader { get; set; }

        public Trader Target { get; set; }

        private int _skill { get; set; }

        private sbyte _job { get; set; }

        private sbyte _nbCase { get; set; }

        private bool _stop { get; set; }

        private Task _task { get; set; }

        private CancellationTokenSource _replayCancellation { get; set; }

        private int? _workshopObjectUid { get; set; }

        public FMTrade(Character trader, Character target = null)
        {
            Trader = new Trader(trader);
            Target = target != null ? new Trader(target) : null;
            Inventories = new Dictionary<RolePlayActor, List<Tuple<BasePlayerItem, int>>>();
            Replay = 1;
        }

        public override ExchangeTypeEnum Type => ExchangeTypeEnum.FORGEMAGIE;

        public override void Open(List<object> parameters = null)
        {
            _job = (sbyte)parameters[0];
            InventoryHandler.SendExchangeStartOkCraftWithInformationMessage(Trader.Client, _nbCase = (sbyte)parameters[1], _skill = (int)parameters[2]);
        }

        public override void Close(List<object> parameters = null)
        {
            Trader.Trade = null;
            Trader.Client.Character.Dialog = null;
            CancelReplay();
            _workshopObjectUid = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, true);
        }

        public override void Cancel(List<object> parameters = null)
        {
            Trader.Trade = null;
            Trader.Client.Character.Dialog = null;
            CancelReplay();
            _workshopObjectUid = null;
            InventoryHandler.SendExchangeLeaveMessage(Trader.Client, false);
        }

        public override void SetReadyStatus(Character trader, bool isReady)
        {
            GetTrader(trader).IsReady = isReady;
            if (Trader.IsReady)
                Apply();
        }

        public override void SetKamas(Character trader, int quantity)
        {
        }

        public bool CanMoveItem(BasePlayerItem item)
        {
            if (item == null || item.Template == null)
                return false;

            return IsRuneLikeItem(item) || IsEditableItem(item);
        }

        private bool IsEditableItem(BasePlayerItem item)
        {
            return item != null &&
                   item.Template != null &&
                   Trader != null &&
                   Trader.Inventory != null &&
                   Trader.Inventory.IsEquipment(item);
        }

        private bool IsRuneLikeItem(BasePlayerItem item)
        {
            if (item == null || item.Template == null)
                return false;

            var typeId = item.Template.TypeId;
            var itemType = (ItemTypeEnum)typeId;

            return item.Template.Id == 7508 ||
                   itemType == ItemTypeEnum.RUNE_DE_FORGEMAGIE ||
                   itemType == ItemTypeEnum.POTION_DE_FORGEMAGIE ||
                   itemType == ItemTypeEnum.ORBE_DE_FORGEMAGIE ||
                   typeId == 211 ||
                   typeId == 212;
        }

        public override void MoveItem(Character trader, int objectUid, int quantity)
        {
            if (trader == null || trader.Inventory == null)
                return;

            var originalItem = trader.Inventory.GetItemUid(objectUid);
            if (originalItem == null)
                return;

            if (quantity > 0 && !CanMoveItem(originalItem))
                return;

            if (quantity > 0 && IsEditableItem(originalItem))
                quantity = 1;

            if (Inventories.ContainsKey(trader))
            {
                var entries = Inventories[trader];
                var existingByUid = entries.FirstOrDefault(x => x.Item1.Id == objectUid);
                bool panelFullForNewEntry = quantity > 0 && existingByUid == null && entries.Count >= _nbCase;
                if (panelFullForNewEntry)
                    return;

                if (quantity > 0 && IsEditableItem(originalItem))
                {
                    var otherEditable = entries.FirstOrDefault(x => x.Item1 != null && x.Item1.Id != originalItem.Id && IsEditableItem(x.Item1));
                    if (otherEditable != null)
                        return;
                }
            }

            var item = originalItem.Clone();
            if (item == null)
                return;

            if (Inventories.ContainsKey(trader))
            {
                var itemStock = Inventories[trader].FirstOrDefault(x => x.Item1.Id == item.Id);
                if (itemStock != null)
                {
                    if (quantity < 0)
                    {
                        quantity *= -1;
                        if (itemStock.Item2 > quantity)
                        {
                            item.Stack = itemStock.Item2 - quantity;
                            InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, item);
                            Inventories[trader].Remove(itemStock);
                            Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                        }
                        else
                        {
                            InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, trader != Trader.Actor, item);
                            Inventories[trader].Remove(itemStock);
                        }
                    }
                    else if (originalItem.Stack - itemStock.Item2 >= quantity)
                    {
                        item.Stack = itemStock.Item2 + quantity;
                        InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, item);
                        Inventories[trader].Remove(itemStock);
                        Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                    }
                }
                else
                {
                    if (quantity < item.Stack)
                        item.Stack = quantity;

                    var sameTemplateItem = !IsEditableItem(item)
                        ? Inventories[trader].FirstOrDefault(x => x.Item1.Template.Id == item.Template.Id)
                        : null;

                    if (sameTemplateItem != null)
                    {
                        item.Stack = sameTemplateItem.Item2 + quantity;
                        InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, trader != Trader.Actor, item);
                        Inventories[trader].Remove(sameTemplateItem);
                        Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                    }
                    else
                    {
                        InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, trader != Trader.Actor, item);
                        Inventories[trader].Add(new Tuple<BasePlayerItem, int>(item, item.Stack));
                    }
                }
            }
            else
            {
                if (quantity < item.Stack)
                    item.Stack = quantity;

                InventoryHandler.SendExchangeObjectAddedMessage(Trader.Client, trader != Trader.Actor, item);
                Inventories.Add(trader, new List<Tuple<BasePlayerItem, int>>() { new Tuple<BasePlayerItem, int>(item, item.Stack) });
            }
        }

        public void UseItemInWorkshop(int objectUid, int quantity)
        {
            var workshopItem = Trader?.Inventory?.GetItemUid(objectUid);
            if (workshopItem == null)
                return;

            if (IsEditableItem(workshopItem))
            {
                MoveItem(Trader.Actor as Character, objectUid, 1);
                return;
            }

            if (!IsRuneLikeItem(workshopItem))
                return;

            _workshopObjectUid = objectUid;
            Replay = quantity > 0 ? Math.Max(1, quantity) : Math.Max(1, workshopItem.Stack);
            Apply();
        }

        public override void Apply()
        {
            if (_task != null && !_task.IsCompleted)
                return;

            _stop = false;
            bool autoReplay = Replay != 1;
            bool infiniteReplay = Replay < 0;
            int requestedReplay = infiniteReplay ? -1 : Math.Max(Replay, 1);

            _replayCancellation?.Cancel();
            _replayCancellation?.Dispose();
            _replayCancellation = new CancellationTokenSource();
            var replayCancellation = _replayCancellation;
            var cancellationToken = replayCancellation.Token;

            _task = Task.Run(async () =>
            {
                var stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_OK;
                bool applied = false;

                while (infiniteReplay || requestedReplay > 0)
                {
                    if (_stop || cancellationToken.IsCancellationRequested)
                    {
                        stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_USER;
                        break;
                    }

                    if (!TryApplyOnce(out stopReason))
                        break;

                    applied = true;

                    if (!infiniteReplay)
                    {
                        requestedReplay--;
                        Replay = requestedReplay;
                        InventoryHandler.SendExchangeReplayCountModifiedMessage(Trader.Client, Replay);

                        if (requestedReplay <= 0)
                            break;
                    }

                    try
                    {
                        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_USER;
                        break;
                    }
                }

                _stop = false;

                if (autoReplay)
                {
                    Replay = 1;
                    InventoryHandler.SendExchangeReplayCountModifiedMessage(Trader.Client, Replay);
                }

                if (!applied && stopReason == ExchangeReplayStopReasonEnum.STOPPED_REASON_OK)
                    stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_IMPOSSIBLE_CRAFT;

                if (stopReason != ExchangeReplayStopReasonEnum.STOPPED_REASON_OK)
                    InventoryHandler.SendExchangeItemAutoCraftStopedMessage(Trader.Client, stopReason);

                if (ReferenceEquals(_replayCancellation, replayCancellation))
                {
                    _replayCancellation.Dispose();
                    _replayCancellation = null;
                }
            });
        }

        private bool TryApplyOnce(out ExchangeReplayStopReasonEnum stopReason)
        {
            stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_IMPOSSIBLE_CRAFT;

            if (!Inventories.TryGetValue(Trader.Actor, out var entries) || entries == null || entries.Count == 0)
            {
                Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
                return false;
            }

            var itemEntry = entries.FirstOrDefault(x => x != null && IsEditableItem(x.Item1));
            if (itemEntry == null)
            {
                Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
                return false;
            }

            Tuple<BasePlayerItem, int> runeEntry = null;
            BasePlayerItem sourceRune = null;

            if (_workshopObjectUid.HasValue)
            {
                sourceRune = Trader.Inventory.GetItemUid(_workshopObjectUid.Value);
                if (sourceRune != null && !IsRuneLikeItem(sourceRune))
                    sourceRune = null;
            }

            if (sourceRune == null)
            {
                runeEntry = entries.FirstOrDefault(x => x != null && IsRuneLikeItem(x.Item1));
                if (runeEntry != null)
                    sourceRune = Trader.Inventory.GetItemUid(runeEntry.Item1.Id);
            }

            var sourceItem = Trader.Inventory.GetItemUid(itemEntry.Item1.Id);

            if (sourceItem == null)
            {
                Clear(itemEntry);
                Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
                stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_MISSING_RESSOURCE;
                return false;
            }

            if (sourceRune == null || sourceRune.Effects == null || sourceRune.Effects.Count == 0)
            {
                if (runeEntry != null)
                    Clear(runeEntry);

                Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
                stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_MISSING_RESSOURCE;
                _workshopObjectUid = null;
                return false;
            }

            if (!TryApplyRuneOnItem(sourceItem, sourceRune, out var resultPreview, out var craftResult, out var poolStatus))
            {
                Trader.Client.Send(new ExchangeCraftResultMessage((sbyte)CraftResultEnum.CRAFT_FAILED));
                stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_IMPOSSIBLE_CRAFT;
                return false;
            }

            Trader.Inventory.RemoveItem(sourceRune, 1);

            InventoryHandler.SendObjectModifiedMessage(Trader.Client, sourceItem);
            InventoryHandler.SendExchangeCraftResultMagicWithObjectDescMessage(Trader.Client, resultPreview, craftResult, poolStatus);

            if (runeEntry != null)
            {
                if (runeEntry.Item2 <= 1)
                    Clear(runeEntry);
                else
                    UpdateExchangeEntryQuantity(runeEntry, runeEntry.Item2 - 1);
            }

            var remainingRune = Trader.Inventory.GetItemUid(sourceRune.Id);
            if (_workshopObjectUid.HasValue && (remainingRune == null || remainingRune.Stack <= 0))
                _workshopObjectUid = null;

            stopReason = ExchangeReplayStopReasonEnum.STOPPED_REASON_OK;
            return true;
        }

        private static bool TryApplyRuneOnItem(
            BasePlayerItem sourceItem,
            BasePlayerItem sourceRune,
            out BasePlayerItem resultPreview,
            out CraftResultEnum craftResult,
            out MagicPoolStatusEnum poolStatus)
        {
            resultPreview = null;
            craftResult = CraftResultEnum.CRAFT_FAILED;
            poolStatus = MagicPoolStatusEnum.UNMODIFIED;

            var runeEffect = sourceRune?.Effects?.FirstOrDefault(x => x != null);
            if (runeEffect == null || sourceItem?.Template == null)
                return false;

            if (sourceItem.Effects == null)
                sourceItem.Effects = new List<Effect>();

            int runeValue = Math.Max(1, GetEffectValue(runeEffect));
            var currentEffect = FindCompatibleEffect(sourceItem, runeEffect.Id);
            int currentValue = currentEffect != null ? GetEffectValue(currentEffect) : 0;
            int baseMax = GetTemplateMaxEffectValue(sourceItem, runeEffect.Id);
            int targetValue = currentValue + runeValue;

            if (!CanApplyForgemagicLimit(runeEffect.Id, targetValue, baseMax))
                return false;

            RollForgemagicResult(
                sourceItem,
                runeEffect,
                currentValue,
                baseMax,
                out craftResult,
                out bool mustLoseStats);

            if (craftResult == CraftResultEnum.CRAFT_SUCCESS || craftResult == CraftResultEnum.CRAFT_NEUTRAL)
                ApplyEffectGain(sourceItem, runeEffect, runeValue);

            if (mustLoseStats)
            {
                double powerToLose = GetEffectUnitPower(runeEffect.Id) * runeValue;
                double lostPower = DegradeOtherEffects(sourceItem, runeEffect, powerToLose);

                if (lostPower > 0)
                    poolStatus = MagicPoolStatusEnum.DECREASED;
            }

            NormalizeItemEffects(sourceItem);
            resultPreview = ItemManager.Instance.CreatePlayerItem(sourceItem);
            return true;
        }

        private static void RollForgemagicResult(
            BasePlayerItem sourceItem,
            Effect runeEffect,
            int currentValue,
            int baseMax,
            out CraftResultEnum result,
            out bool mustLoseStats)
        {
            GetSuccessRates(sourceItem, runeEffect, currentValue, baseMax,
                out double criticalSuccess,
                out double neutralSuccess,
                out double criticalFailure);

            double roll = new AsyncRandom().NextDouble() * 100.0;

            if (roll <= criticalSuccess)
            {
                result = CraftResultEnum.CRAFT_SUCCESS;
                mustLoseStats = false;
                return;
            }

            if (roll <= criticalSuccess + neutralSuccess)
            {
                result = CraftResultEnum.CRAFT_NEUTRAL;
                mustLoseStats = true;
                return;
            }

            result = CraftResultEnum.CRAFT_FAILED;
            mustLoseStats = true;
        }

        private static void GetSuccessRates(
            BasePlayerItem sourceItem,
            Effect runeEffect,
            int currentValue,
            int baseMax,
            out double criticalSuccess,
            out double neutralSuccess,
            out double criticalFailure)
        {
            criticalSuccess = 0;
            neutralSuccess = 0;
            criticalFailure = 100;

            if (IsApEffect(runeEffect.Id))
            {
                criticalSuccess = 3.5;
                neutralSuccess = 0;
                criticalFailure = 96.5;
                return;
            }

            if (IsMpEffect(runeEffect.Id))
            {
                criticalSuccess = 5.5;
                neutralSuccess = 0;
                criticalFailure = 94.5;
                return;
            }

            if (IsPoEffect(runeEffect.Id) || IsSummonEffect(runeEffect.Id))
            {
                criticalSuccess = 4.0;
                neutralSuccess = 0;
                criticalFailure = 96.0;
                return;
            }

            bool exotic = baseMax <= 0;
            double itemStatus = Math.Min(100.0, GetItemPowerProgress(sourceItem));

            if (exotic)
            {
                double adjustedItemStatus = Math.Min(
                    100.0,
                    89.0 + Math.Sqrt(Math.Max(0.0, GetEffectPower(runeEffect))) + Math.Sqrt(Math.Max(0.0, itemStatus)));

                double effectStatus = 100.0;
                double effectSuccess = effectStatus >= 80.0
                    ? 15.0 * effectStatus / 100.0
                    : effectStatus / 4.0;

                double itemSuccess = adjustedItemStatus >= 50.0
                    ? 30.0 * adjustedItemStatus / 100.0
                    : adjustedItemStatus;

                neutralSuccess = 50.0;
                criticalSuccess = Math.Max(1.0, 90.0 - Math.Ceiling(effectSuccess + itemSuccess));

                if (criticalSuccess > 50.0)
                    neutralSuccess = 100.0 - criticalSuccess;
                else if (criticalSuccess < 35.0)
                    neutralSuccess = 25.0 + criticalSuccess;

                criticalFailure = Math.Max(0.0, 100.0 - (criticalSuccess + neutralSuccess));
                return;
            }

            double effectStatusProgress = baseMax > 0
                ? (currentValue * 100.0) / Math.Max(1, baseMax)
                : 0.0;

            if (currentValue > baseMax)
            {
                itemStatus = Math.Max(itemStatus, effectStatusProgress / 2.0);
                effectStatusProgress += GetEffectPower(runeEffect);
            }

            effectStatusProgress = Math.Min(100.0, Math.Max(0.0, effectStatusProgress));
            itemStatus = Math.Min(100.0, Math.Max(0.0, itemStatus));

            double normalEffectSuccess = effectStatusProgress >= 80.0
                ? 15.0 * effectStatusProgress / 100.0
                : effectStatusProgress / 4.0;

            double normalItemSuccess = itemStatus >= 50.0
                ? 30.0 * itemStatus / 100.0
                : itemStatus;

            neutralSuccess = 50.0;
            criticalSuccess = Math.Max(1.0, 90.0 - Math.Ceiling(normalEffectSuccess + normalItemSuccess));

            if (criticalSuccess > 50.0)
                neutralSuccess = 100.0 - criticalSuccess;
            else if (criticalSuccess < 35.0)
                neutralSuccess = 25.0 + criticalSuccess;

            criticalFailure = Math.Max(0.0, 100.0 - (criticalSuccess + neutralSuccess));
        }

        private static void ApplyEffectGain(BasePlayerItem sourceItem, Effect runeEffect, int runeValue)
        {
            var existingEffect = FindCompatibleEffect(sourceItem, runeEffect.Id);

            if (existingEffect == null)
            {
                var newEffect = runeEffect.Clone();
                newEffect.Value = runeValue;
                newEffect.DiceNum = (uint)runeValue;
                newEffect.DiceFace = (uint)Math.Max(1, runeValue);
                sourceItem.Effects.Add(newEffect);
                return;
            }

            int newValue = GetEffectValue(existingEffect) + runeValue;
            existingEffect.Value = newValue;
            existingEffect.DiceNum = (uint)newValue;
            existingEffect.DiceFace = (uint)Math.Max(1, newValue);
        }

        private static double DegradeOtherEffects(BasePlayerItem sourceItem, Effect runeEffect, double powerToLose)
        {
            if (sourceItem?.Effects == null || powerToLose <= 0)
                return 0;

            double lostPower = 0;
            int safety = 512;
            var random = new AsyncRandom();

            while (powerToLose > 0.001 && safety-- > 0)
            {
                var effectToDown = SelectEffectToDegrade(sourceItem, runeEffect, random);
                if (effectToDown == null)
                    break;

                double unitPower = GetEffectUnitPower(effectToDown.Id);
                if (unitPower <= 0)
                {
                    sourceItem.Effects.Remove(effectToDown);
                    continue;
                }

                int current = GetEffectValue(effectToDown);
                if (current <= 0)
                {
                    sourceItem.Effects.Remove(effectToDown);
                    continue;
                }

                current -= 1;

                if (current <= 0)
                {
                    sourceItem.Effects.Remove(effectToDown);
                }
                else
                {
                    effectToDown.Value = current;
                    effectToDown.DiceNum = (uint)current;
                    effectToDown.DiceFace = (uint)Math.Max(1, current);
                }

                powerToLose -= unitPower;
                lostPower += unitPower;
            }

            return lostPower;
        }

        private static Effect SelectEffectToDegrade(BasePlayerItem sourceItem, Effect runeEffect, AsyncRandom random)
        {
            var candidates = sourceItem.Effects
                .Where(x =>
                    x != null &&
                    GetEffectValue(x) > 0 &&
                    IsDegradableEffect(x.Id) &&
                    !AreCompatibleEffects(x.Id, runeEffect.Id))
                .ToList();

            if (candidates.Count == 0)
                return null;

            var exotic = candidates.Where(x => IsExoticEffect(sourceItem, x.Id)).ToList();
            if (exotic.Count > 0)
                return exotic[random.Next(0, exotic.Count)];

            var overMax = candidates.Where(x => IsOverHardCap(sourceItem, x.Id, GetEffectValue(x))).ToList();
            if (overMax.Count > 0)
                return overMax[random.Next(0, overMax.Count)];

            return candidates[random.Next(0, candidates.Count)];
        }

        private static bool CanApplyForgemagicLimit(EffectsEnum effectId, int targetValue, int baseMax)
        {
            return targetValue <= GetHardCap(effectId, baseMax);
        }

        private static int GetHardCap(EffectsEnum effectId, int baseMax)
        {
            int officialLineCap = GetOfficialLineCap(effectId);
            if (officialLineCap == int.MaxValue)
                return int.MaxValue;

            return Math.Max(Math.Max(0, baseMax), officialLineCap);
        }

        private static int GetOfficialLineCap(EffectsEnum effectId)
        {
            double unitWeight = GetEffectUnitPower(effectId);
            if (unitWeight <= 0)
                return int.MaxValue;

            // Règle Dofus 2.x classique : une ligne ne dépasse pas 101 de poids,
            // sauf si le jet natif max de l'objet dépasse déjà cette borne.
            // Exemples obtenus avec cette formule :
            // PA=1, PM=1, PO=1, Invoc=3, Carac=101, Vita=505, Sagesse=33,
            // Ré% = 16, Ré fixes = 50, Do = 5, Do élémentaires = 20, etc.
            return Math.Max(1, (int)Math.Floor(101d / unitWeight));
        }

        private static bool IsOverHardCap(BasePlayerItem item, EffectsEnum effectId, int currentValue)
        {
            int baseMax = GetTemplateMaxEffectValue(item, effectId);
            return currentValue > GetHardCap(effectId, baseMax);
        }

        private static bool IsExoticEffect(BasePlayerItem item, EffectsEnum effectId)
        {
            return GetTemplateMaxEffectValue(item, effectId) <= 0;
        }

        private static bool IsDegradableEffect(EffectsEnum effectId)
        {
            return GetEffectUnitPower(effectId) > 0;
        }

        private static double GetItemPowerProgress(BasePlayerItem item)
        {
            double currentPower = GetItemPower(item, true);
            double basePower = GetItemPower(item, false);

            if (basePower <= 0)
                return 0;

            return (currentPower / basePower) * 100.0;
        }

        private static double GetItemPower(BasePlayerItem item, bool useCurrentEffects)
        {
            IEnumerable<Effect> effects = useCurrentEffects
                ? item?.Effects
                : item?.Template?.EffectsBase;

            if (effects == null)
                return 0;

            double total = 0;

            foreach (var effect in effects.Where(x => x != null))
            {
                int value = useCurrentEffects ? GetEffectValue(effect) : GetEffectMaxValue(effect);
                if (value <= 0)
                    continue;

                total += GetEffectUnitPower(effect.Id) * value;
            }

            return total;
        }

        private static double GetEffectPower(Effect effect)
        {
            return GetEffectUnitPower(effect.Id) * Math.Max(1, GetEffectValue(effect));
        }

        private static double GetEffectUnitPower(EffectsEnum effectId)
        {
            var record = JobManager.Instance.GetRuneEffect(effectId);
            if (record != null && record.PEffect > 0)
                return record.PEffect;

            // Filet de sécurité si la table runes_effect ne contient pas encore la ligne.
            switch (effectId)
            {
                case EffectsEnum.Effect_AddVitality:
                    return 0.2d;

                case EffectsEnum.Effect_AddStrength:
                case EffectsEnum.Effect_AddIntelligence:
                case EffectsEnum.Effect_AddChance:
                case EffectsEnum.Effect_AddAgility:
                case EffectsEnum.Effect_AddInitiative:
                    return 1d;

                case EffectsEnum.Effect_AddWisdom:
                case EffectsEnum.Effect_AddProspecting:
                    return 3d;

                case EffectsEnum.Effect_AddDamageBonus:
                case EffectsEnum.Effect_AddDamageBonus_121:
                    return 20d;

                case EffectsEnum.Effect_AddEarthDamageBonus:
                case EffectsEnum.Effect_AddFireDamageBonus:
                case EffectsEnum.Effect_AddWaterDamageBonus:
                case EffectsEnum.Effect_AddAirDamageBonus:
                case EffectsEnum.Effect_AddNeutralDamageBonus:
                case EffectsEnum.Effect_AddPushDamageBonus:
                case EffectsEnum.Effect_AddCriticalDamageBonus:
                case EffectsEnum.Effect_AddDamageReflection:
                    return 5d;

                case EffectsEnum.Effect_AddEarthResistPercent:
                case EffectsEnum.Effect_AddWaterResistPercent:
                case EffectsEnum.Effect_AddAirResistPercent:
                case EffectsEnum.Effect_AddFireResistPercent:
                case EffectsEnum.Effect_AddNeutralResistPercent:
                case EffectsEnum.Effect_AddPvpEarthResistPercent:
                case EffectsEnum.Effect_AddPvpWaterResistPercent:
                case EffectsEnum.Effect_AddPvpAirResistPercent:
                case EffectsEnum.Effect_AddPvpFireResistPercent:
                case EffectsEnum.Effect_AddPvpNeutralResistPercent:
                    return 6d;

                case EffectsEnum.Effect_AddEarthElementReduction:
                case EffectsEnum.Effect_AddWaterElementReduction:
                case EffectsEnum.Effect_AddAirElementReduction:
                case EffectsEnum.Effect_AddFireElementReduction:
                case EffectsEnum.Effect_AddNeutralElementReduction:
                case EffectsEnum.Effect_AddPvpEarthElementReduction:
                case EffectsEnum.Effect_AddPvpWaterElementReduction:
                case EffectsEnum.Effect_AddPvpAirElementReduction:
                case EffectsEnum.Effect_AddPvpFireElementReduction:
                case EffectsEnum.Effect_AddPvpNeutralElementReduction:
                case EffectsEnum.Effect_AddPushDamageReduction:
                case EffectsEnum.Effect_AddCriticalDamageReduction:
                    return 2d;

                case EffectsEnum.Effect_AddMagicDamageReduction:
                case EffectsEnum.Effect_AddPhysicalDamageReduction:
                    return 15d;

                case EffectsEnum.Effect_AddHealBonus:
                case EffectsEnum.Effect_AddCriticalHit:
                    return 10d;

                case EffectsEnum.Effect_AddDodge:
                case EffectsEnum.Effect_AddLock:
                    return 4d;

                case EffectsEnum.Effect_Dodge:
                    return 7d;

                case EffectsEnum.Effect_AddAP_111:
                    return 100d;

                case EffectsEnum.Effect_AddMP:
                case EffectsEnum.Effect_AddMP_128:
                    return 90d;

                case EffectsEnum.Effect_AddRange:
                case EffectsEnum.Effect_AddRange_136:
                    return 51d;

                case EffectsEnum.Effect_AddSummonLimit:
                    return 30d;

                default:
                    return 0d;
            }
        }

        private static Effect FindCompatibleEffect(BasePlayerItem sourceItem, EffectsEnum effectId)
        {
            if (sourceItem?.Effects == null)
                return null;

            var compatibleIds = GetCompatibleEffectIds(effectId);
            return sourceItem.Effects.FirstOrDefault(x => x != null && compatibleIds.Contains(x.Id));
        }

        private static int GetTemplateMaxEffectValue(BasePlayerItem sourceItem, EffectsEnum effectId)
        {
            if (sourceItem?.Template?.EffectsBase == null)
                return 0;

            var compatibleIds = GetCompatibleEffectIds(effectId);

            return sourceItem.Template.EffectsBase
                .Where(x => x != null && compatibleIds.Contains(x.Id))
                .Select(GetEffectMaxValue)
                .DefaultIfEmpty(0)
                .Max();
        }

        private static int GetEffectMaxValue(Effect effect)
        {
            if (effect == null)
                return 0;

            return new[]
            {
                effect.Value,
                (int)effect.DiceNum,
                (int)effect.DiceFace
            }.Max();
        }

        private static void NormalizeItemEffects(BasePlayerItem sourceItem)
        {
            if (sourceItem.Effects == null)
                sourceItem.Effects = new List<Effect>();

            var normalized = new List<Effect>();

            foreach (var group in sourceItem.Effects
                         .Where(x => x != null && GetEffectValue(x) > 0)
                         .GroupBy(x => GetCompatibilityKey(x.Id)))
            {
                var keep = group.First().Clone();
                int totalValue = group.Sum(GetEffectValue);

                keep.Value = totalValue;
                keep.DiceNum = (uint)totalValue;
                keep.DiceFace = (uint)Math.Max(1, totalValue);

                normalized.Add(keep);
            }

            sourceItem.Effects = normalized;
            sourceItem.RawObjectEffects = normalized
                .Select(x => (ObjectEffect)x.GetObjectEffectInteger())
                .ToList();
        }

        private static string GetCompatibilityKey(EffectsEnum effectId)
        {
            if (IsMpEffect(effectId))
                return "MP";

            if (IsPoEffect(effectId))
                return "PO";

            return effectId.ToString();
        }

        private static bool AreCompatibleEffects(EffectsEnum left, EffectsEnum right)
        {
            return GetCompatibleEffectIds(left).Contains(right) || GetCompatibleEffectIds(right).Contains(left);
        }

        private static HashSet<EffectsEnum> GetCompatibleEffectIds(EffectsEnum effectId)
        {
            if (IsMpEffect(effectId))
            {
                return new HashSet<EffectsEnum>
                {
                    EffectsEnum.Effect_AddMP,
                    EffectsEnum.Effect_AddMP_128
                };
            }

            if (IsPoEffect(effectId))
            {
                return new HashSet<EffectsEnum>
                {
                    EffectsEnum.Effect_AddRange,
                    EffectsEnum.Effect_AddRange_136
                };
            }

            return new HashSet<EffectsEnum> { effectId };
        }

        private static bool IsApEffect(EffectsEnum effectId)
        {
            return effectId == EffectsEnum.Effect_AddAP_111;
        }

        private static bool IsMpEffect(EffectsEnum effectId)
        {
            return effectId == EffectsEnum.Effect_AddMP || effectId == EffectsEnum.Effect_AddMP_128;
        }

        private static bool IsPoEffect(EffectsEnum effectId)
        {
            return effectId == EffectsEnum.Effect_AddRange || effectId == EffectsEnum.Effect_AddRange_136;
        }

        private static bool IsSummonEffect(EffectsEnum effectId)
        {
            return effectId == EffectsEnum.Effect_AddSummonLimit;
        }

        private static bool IsElementMainStatEffect(EffectsEnum effectId)
        {
            return effectId == EffectsEnum.Effect_AddStrength ||
                   effectId == EffectsEnum.Effect_AddIntelligence ||
                   effectId == EffectsEnum.Effect_AddChance ||
                   effectId == EffectsEnum.Effect_AddAgility;
        }

        private static bool IsResistanceEffect(EffectsEnum effectId)
        {
            switch (effectId)
            {
                case EffectsEnum.Effect_AddEarthResistPercent:
                case EffectsEnum.Effect_AddWaterResistPercent:
                case EffectsEnum.Effect_AddAirResistPercent:
                case EffectsEnum.Effect_AddFireResistPercent:
                case EffectsEnum.Effect_AddNeutralResistPercent:
                case EffectsEnum.Effect_AddEarthElementReduction:
                case EffectsEnum.Effect_AddWaterElementReduction:
                case EffectsEnum.Effect_AddAirElementReduction:
                case EffectsEnum.Effect_AddFireElementReduction:
                case EffectsEnum.Effect_AddNeutralElementReduction:
                case EffectsEnum.Effect_AddPvpEarthResistPercent:
                case EffectsEnum.Effect_AddPvpWaterResistPercent:
                case EffectsEnum.Effect_AddPvpAirResistPercent:
                case EffectsEnum.Effect_AddPvpFireResistPercent:
                case EffectsEnum.Effect_AddPvpNeutralResistPercent:
                case EffectsEnum.Effect_AddPvpEarthElementReduction:
                case EffectsEnum.Effect_AddPvpWaterElementReduction:
                case EffectsEnum.Effect_AddPvpAirElementReduction:
                case EffectsEnum.Effect_AddPvpFireElementReduction:
                case EffectsEnum.Effect_AddPvpNeutralElementReduction:
                    return true;

                default:
                    return false;
            }
        }

        private static int GetEffectValue(Effect effect)
        {
            if (effect == null)
                return 0;

            if (effect.Value > 0)
                return effect.Value;

            if (effect.DiceNum > 0)
                return (int)effect.DiceNum;

            if (effect.DiceFace > 0)
                return (int)effect.DiceFace;

            return 0;
        }

        private void UpdateExchangeEntryQuantity(Tuple<BasePlayerItem, int> entry, int newQuantity)
        {
            if (entry == null || newQuantity <= 0)
            {
                if (entry != null)
                    Clear(entry);

                return;
            }

            var item = entry.Item1.Clone();
            item.Stack = newQuantity;
            InventoryHandler.SendExchangeObjectModifiedMessage(Trader.Client, false, item);
            Inventories[Trader.Actor].Remove(entry);
            Inventories[Trader.Actor].Add(new Tuple<BasePlayerItem, int>(item, newQuantity));
        }

        public void Clear(Tuple<BasePlayerItem, int> item)
        {
            if (item == null || !Inventories.ContainsKey(Trader.Actor))
                return;

            InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, false, item.Item1);
            Inventories[Trader.Actor].Remove(item);
        }

        public void Clear()
        {
            if (!Inventories.ContainsKey(Trader.Actor))
                return;

            foreach (var item in Inventories[Trader.Actor].ToList())
                InventoryHandler.SendExchangeObjectRemovedMessage(Trader.Client, false, item.Item1);

            Inventories[Trader.Actor].Clear();
        }

        public void Stop()
        {
            CancelReplay();
        }

        public void UpdateReplay(int count)
        {
            Replay = count;
            InventoryHandler.SendExchangeReplayCountModifiedMessage(Trader.Client, count);
        }

        private void CancelReplay()
        {
            _stop = true;

            if (_replayCancellation != null)
            {
                try
                {
                    _replayCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public Trader GetTrader(Character owner)
        {
            return Trader.Actor.Id == owner.Id ? Trader : Target;
        }
    }
}
