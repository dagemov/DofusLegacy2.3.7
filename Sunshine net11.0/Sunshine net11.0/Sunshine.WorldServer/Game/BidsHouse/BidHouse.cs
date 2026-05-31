using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.BidsHouse;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.BidsHouse
{
    public class BidHouse
    {
        public BidHouseRecord Record { get; set; }

        public IEnumerable<int> ItemsType { get; set; }

        public BidHouse(BidHouseRecord record)
        {
            Record = record;
            ItemsType = record.Types.Split(',').Select(x => int.Parse(x));
        }

        public IEnumerable<int> GetItems(ItemTypeEnum type)
        {
            List<int> itemsId = new List<int>();
            foreach(var items in BidHouseManager.Instance.BidsHouseItems.Values)
            {
                foreach (var item in items.Where(x => x.Type == type))
                    itemsId.Add(item.Template.Id);
            }
            return itemsId.Distinct();
        }

        public SellerBuyerDescriptor GetSellerBuyerDescriptor()
        {
            return new SellerBuyerDescriptor(new List<int> { 1, 10, 100 }, ItemsType, 2, Record.Level, Record.Level, -1, 476);
        }

        public IEnumerable<BidExchangerObjectInfo> GetBidExchangerObjectsInfo()
        {
            List<BidExchangerObjectInfo> exchangerObjects = new List<BidExchangerObjectInfo>();
            foreach(var items in BidHouseManager.Instance.BidsHouseItems.Values)
            {
                foreach (var item in items.Where(x => !x.Selled).Distinct())
                    exchangerObjects.Add(item.GetBidExchangerObjectInfo());
            }
            return exchangerObjects;
        }

        public void SellItem(Character owner, BasePlayerItem item, int quantity, int price)
        {
            owner.Inventory.RemoveItem(item, quantity);
            var newItem = ItemManager.Instance.CreatePlayerItem(item, quantity);
            newItem.Price = price;
            BidHouseManager.Instance.AddItem(owner.Account.Id, newItem);
            BidHouseManager.Instance.Sort(newItem);
            InventoryHandler.SendExchangeBidHouseItemAddOkMessage(owner.Client, newItem);
            BidHouseManager.Instance.Refresh(newItem, owner.Map.Id);
        }

        public void BuyItem(Character character, int objectUid, int quantity, int price)
        {
            if (character.Inventory.Kamas < price)
                return;

            var sells = BidHouseManager.Instance.GetItem(objectUid);
            if (sells.Item2 != null && !sells.Item2.Selled)
            {
                BidHouseManager.Instance.RemoveItem(sells.Item1, sells.Item2);
                character.Inventory.AddItem(sells.Item2, quantity);
                sells.Item2.Selled = true;
                character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 21, new object[] { quantity, sells.Item2.Template.Id });
                character.Inventory.SetKamas(-price);
                var seller = CharacterManager.Instance.GetCharacterByAccount(sells.Item1);
                if (seller != null && seller.IsInWorld())
                {
                    seller.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 65,
                    new object[] { sells.Item2.Price * quantity, sells.Item2.Id, sells.Item2.Template.Id, quantity });
                    seller.Inventory.SetKamas(sells.Item2.Price * quantity);
                    InventoryHandler.SendExchangeBidHouseItemRemoveOkMessage(seller.Client, sells.Item2);
                }
                else if (seller != null)
                    seller.BidHouseBag.AddSelledItem(sells.Item1, sells.Item2);
                BidHouseManager.Instance.Sort(sells.Item2, true);
                BidHouseManager.Instance.Refresh(sells.Item2, character.Map.Id);
            }
        }
    }
}
