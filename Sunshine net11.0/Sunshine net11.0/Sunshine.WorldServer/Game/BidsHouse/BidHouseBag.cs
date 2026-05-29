using Sunshine.MySql.Database.Auth.Accounts;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.BidsHouse
{
    public class BidHouseBag
    {
        private Character _owner;

        public BidHouseBag(Character owner)
        {
            _owner = owner;
        }

        public void LoadBag()
        {
            if (BidHouseManager.Instance.BidsHouseItems.ContainsKey(_owner.Account.Id))
            {
                var bidHouseItems = BidHouseManager.Instance.BidsHouseItems[_owner.Account.Id].Where(x => x.Selled).ToArray();
                for(int i = 0; i < bidHouseItems.Length; i++)
                {
                    BasePlayerItem item = bidHouseItems[i];
                    _owner.Inventory.SetKamas(item.Price * item.Stack);
                    _owner.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 65,
                           new object[] { item.Price * item.Stack, item.Id, item.Template.Id, item.Stack });
                    BidHouseManager.Instance.RemoveItem(_owner.Account.Id, item);
                }
            }
        }

        public IEnumerable<ObjectItemToSellInBid> GetObjectItemsToSell(BidHouse bidHouse)
        {
            List<ObjectItemToSellInBid> itemSells = new List<ObjectItemToSellInBid>();
            
            if (BidHouseManager.Instance.BidsHouseItems.ContainsKey(_owner.Account.Id))
            {
                var bidsHouseItems = BidHouseManager.Instance.BidsHouseItems[_owner.Account.Id];
                foreach (var item in bidsHouseItems)
                {
                    if (bidHouse.ItemsType.Contains((int)item.Type))
                        itemSells.Add(item.GetObjectItemToSell());
                }
            }
            return itemSells;
        }

        public void AddSelledItem(int account, BasePlayerItem item)
        {
            var newItem = ItemManager.Instance.CreatePlayerItem(item, item.Stack);
            newItem.Selled = true;
            newItem.Price = item.Price;
            if (BidHouseManager.Instance.BidsHouseItems[account].Any(x => x.Selled && x.Template.Id == item.Template.Id))
            {
                var sameItem = StackItem(account, newItem);
                if (sameItem != null)
                {
                    BidHouseManager.Instance.RemoveItem(account, sameItem);
                    newItem.Stack += sameItem.Stack;
                }
                BidHouseManager.Instance.AddItem(account, newItem);
            }
            else
                BidHouseManager.Instance.AddItem(account, newItem);
        }

        public void MoveItem(int objectGid, int quantity)
        {
            var item = BidHouseManager.Instance.GetItem(_owner.Account.Id, objectGid, quantity *= -1);
            if (item != null)
            {
                BidHouseManager.Instance.RemoveItem(_owner.Account.Id, item);
                var newItem = ItemManager.Instance.CreatePlayerItem(item, quantity);
                _owner.Inventory.AddItem(newItem, quantity);
                InventoryHandler.SendExchangeBidHouseItemRemoveOkMessage(_owner.Client, item);
                BidHouseManager.Instance.Sort(item, true);
                BidHouseManager.Instance.Refresh(item, _owner.Map.Id);
            }
        }

        private BasePlayerItem StackItem(int account, BasePlayerItem item)
        {
            var items = BidHouseManager.Instance.BidsHouseItems[account].Where(x => x.Selled);
            return items.FirstOrDefault(x => x.Template.Id == item.Template.Id && HaveSameEffects(x, item));
        }

        private bool HaveSameEffects(BasePlayerItem first, BasePlayerItem second)
        {
            if ((first?.RawObjectEffects?.Count ?? 0) > 0 || (second?.RawObjectEffects?.Count ?? 0) > 0)
                return ObjectEffectSerializer.Serialize(first?.RawObjectEffects) == ObjectEffectSerializer.Serialize(second?.RawObjectEffects);

            var effects = first?.Effects ?? new List<Effect>();
            var secondEffects = second?.Effects ?? new List<Effect>();

            if (effects.Count != secondEffects.Count)
                return false;

            if (effects.Count == 0 && secondEffects.Count == 0)
                return true;

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Value != secondEffects[i].Value)
                    return false;
            }

            return true;
        }
    }
}
