using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Basic;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.Logs;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Actions
{
    public class NpcBuySellAction : NpcAction
    {
        private readonly Character _character;
        private readonly Npc _npc;

        public NpcBuySellAction(Npc npc, Character character)
        {
            _npc = npc;
            _character = character;
        }

        public override void Execute()
        {
            _character.Dialog = this;
            Logger.WriteInfo($"[ShopTrace] NpcBuySellAction.Execute charId={_character.Id} npcTemplate={_npc?.Record?.Id} npcActor={_npc?.Id} items={_npc?.GetObjectItemToSellInNpcShops?.Count() ?? 0} dialogBefore={_character.Dialog?.GetType().Name ?? "null"}");
            InventoryHandler.SendExchangeStartOkNpcShopMessage(_character.Client, _npc);
        }

        public void BuyItem(int objectGid, int quantity)
        {
            if (quantity <= 0 || _npc == null || !_npc.Shops.ContainsKey(objectGid))
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                return;
            }

            if (!ItemManager.Instance.Items.ContainsKey(objectGid))
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                return;
            }

            var shopEntry = _npc.Shops[objectGid];
            var template = ItemManager.Instance.Items[objectGid];

            int unitPrice = shopEntry.GetPrice((int)template.Price);
            long totalPrice = (long)unitPrice * quantity;

            if (unitPrice <= 0 || totalPrice <= 0 || totalPrice > int.MaxValue)
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                return;
            }

            if (!CanBuy(totalPrice))
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                return;
            }

            if (!ItemCriteriaEvaluator.IsRespected(_character, template.Criteria))
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                return;
            }

            var newItem = ItemManager.Instance.CreatePlayerItem(objectGid, quantity);
            if (newItem == null)
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                return;
            }

            // Sécurité : toujours dans l'inventaire normal
            newItem.Position = CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED;
            newItem.Stack = quantity;

            // Débit d'abord
            var shopToken = _npc.ResolveShopToken();
            if (shopToken != 0)
            {
                var tokenItem = _character.Inventory.GetItem(shopToken);
                if (tokenItem == null || tokenItem.Stack < totalPrice)
                {
                    InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                    return;
                }

                _character.Inventory.RemoveItem(tokenItem, (int)totalPrice);
            }
            else
            {
                if (_character.Inventory.Kamas < totalPrice)
                {
                    InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.BUY_ERROR);
                    return;
                }

                _character.Inventory.SetKamas(-(int)totalPrice);
            }

            // Ajout après paiement
            _character.Inventory.AddItem(newItem, quantity);

            BasicHandler.SendTextInformationMessage(
                _character.Client,
                TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE,
                21,
                new object[] { quantity, objectGid });

            BasicHandler.SendTextInformationMessage(
                _character.Client,
                TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE,
                46,
                new object[] { totalPrice });

            // Refresh complet pour éviter le mauvais onglet côté client
            InventoryHandler.SendInventoryContentMessage(_character.Client);
            InventoryHandler.SendInventoryWeightMessage(_character.Client);
            InventoryHandler.SendExchangeBuyOkMessage(_character.Client);
        }

        public void SellItem(int objectUid, int quantity)
        {
            if (quantity <= 0 || _npc == null)
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.SELL_ERROR);
                return;
            }

            if (_npc.ResolveShopToken() != 0)
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.SELL_ERROR);
                return;
            }

            var item = _character.Inventory.GetItemUid(objectUid);
            if (item == null || item.Template == null || item.Stack < quantity)
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.SELL_ERROR);
                return;
            }

            item.EnsureRuntimeEffects();
            if (!item.IsExchangeable())
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.SELL_ERROR);
                return;
            }

            int unitPrice = Math.Max(1, (int)Math.Floor(item.Template.Price));
            long totalPrice = (long)unitPrice * quantity;
            if (totalPrice <= 0 || totalPrice > int.MaxValue)
            {
                InventoryHandler.SendExchangeErrorMessage(_character.Client, ExchangeErrorEnum.SELL_ERROR);
                return;
            }

            _character.Inventory.RemoveItem(item, quantity);
            _character.Inventory.SetKamas((int)totalPrice);

            InventoryHandler.SendInventoryContentMessage(_character.Client);
            InventoryHandler.SendInventoryWeightMessage(_character.Client);
            InventoryHandler.SendExchangeSellOkMessage(_character.Client);
        }

        private bool CanBuy(long totalPrice)
        {
            if (totalPrice <= 0 || totalPrice > int.MaxValue)
                return false;

            var shopToken = _npc.ResolveShopToken();
            if (shopToken != 0)
            {
                var tokenItem = _character.Inventory.GetItem(shopToken);
                return tokenItem != null && tokenItem.Stack >= totalPrice;
            }

            return _character.Inventory.Kamas >= totalPrice;
        }
    }
}