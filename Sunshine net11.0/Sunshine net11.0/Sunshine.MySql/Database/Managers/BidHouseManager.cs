using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.BidsHouse;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;
using Sunshine.WorldServer.Game.BidsHouse;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class BidHouseManager : Singleton<BidHouseManager>
    {
        public Dictionary<int, BidHouse> BidsHouse = new Dictionary<int, BidHouse>();

        public Dictionary<int, List<BasePlayerItem>> BidsHouseItems = new Dictionary<int, List<BasePlayerItem>>();

        public Dictionary<BasePlayerItem, List<int>> BidsHousePrices = new Dictionary<BasePlayerItem, List<int>>();

        public IEnumerable<BidHouseRecord> GetAllBidsHouse()
        {
            return DatabaseManager.Connection.Query<BidHouseRecord>($"SELECT * FROM bids_house");
        }  
        
        public BidHouseRecord GetBidHouse(int mapId)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<BidHouseRecord>($"SELECT * FROM bids_house WHERE MapId = '{mapId}'");
        }

        public IEnumerable<BidHouseItemRecord> GetAllBidsHouseItems()
        {
            return DatabaseManager.Connection.Query<BidHouseItemRecord>($"SELECT * FROM bids_house_items");
        }

        public Dictionary<int, List<BasePlayerItem>> GetAllItems(IEnumerable<BidHouseItemRecord> items)
        {
            Dictionary<int, List<BasePlayerItem>> basePlayerItems = new Dictionary<int, List<BasePlayerItem>>();
            foreach (var itemRecord in items)
            {
                var effects = EffectManager.Instance.GetEffects(itemRecord.Effects);
                var item = ItemManager.Instance.CreatePlayerItem(itemRecord.Item, effects, itemRecord.Stack);
                item.Selled = itemRecord.Selled;
                item.Price = itemRecord.Price;
                if (basePlayerItems.ContainsKey(itemRecord.OwnerId))
                    basePlayerItems[itemRecord.OwnerId].Add(item);
                else
                    basePlayerItems.Add(itemRecord.OwnerId, new List<BasePlayerItem> { item });
            }
            return basePlayerItems;
        }

        public Tuple<int, BasePlayerItem> GetItem(int uid)
        {
            foreach (var owner in BidsHouseItems.Keys)
            {
                var item = BidsHouseItems[owner].FirstOrDefault(x => x.Id == uid && !x.Selled);
                if (item != null)
                    return new Tuple<int, BasePlayerItem>(owner, item);
            }
            return null;
        }

        public BasePlayerItem GetItem(int account, int guid, int quantity)
        {
            return BidsHouseItems[account].FirstOrDefault(x => x.Template.Id == guid && x.Stack == quantity);
        }

        public void AddItem(int account, BasePlayerItem item)
        {
            lock (item)
            {
                if (BidsHouseItems.ContainsKey(account))
                    BidsHouseItems[account].Add(item);
                else
                    BidsHouseItems.Add(account, new List<BasePlayerItem> { item });
            }
        }

        public void RemoveItem(int account, BasePlayerItem item)
        {
            lock (item)
            {
                BidsHouseItems[account].Remove(item);
            }
        }

        public void Sort(BasePlayerItem item, bool isRemove = false)
        {
            lock (item)
            {
                if (isRemove)
                    BidsHousePrices.Remove(item);

                if (!BidsHousePrices.ContainsKey(item))
                {
                    switch (item.Stack)
                    {
                        case 1:
                            BidsHousePrices.Add(item, new List<int> { item.Price, 0, 0 });
                            break;

                        case 10:
                            BidsHousePrices.Add(item, new List<int> { 0, item.Price, 0 });
                            break;

                        case 100:
                            BidsHousePrices.Add(item, new List<int> { 0, 0, item.Price });
                            break;
                    }
                }
                else
                {
                    var prices = BidsHousePrices[item];
                    switch (item.Stack)
                    {
                        case 1:
                            if (item.Price < prices[0])
                                prices[0] = item.Price;
                            break;

                        case 10:
                            if (item.Price < prices[1])
                                prices[1] = item.Price;
                            break;

                        case 100:
                            if (item.Price < prices[2])
                                prices[2] = item.Price;
                            break;
                    }
                    BidsHousePrices.Remove(item);
                    BidsHousePrices.Add(item, prices);
                }
            }
        }
       
        public void Refresh(BasePlayerItem item, int mapId)
        {
            var characters = CharacterManager.Instance.Characters.Values.Where(x => x.IsInWorld() && x.Map.Id == mapId);
            foreach(var character in characters.Where(x => x.IsInDialog() && x.Dialog is NpcBuyAction))
            {
                InventoryHandler.SendExchangeBidHouseInListRemovedMessage(character.Client, item);
                InventoryHandler.SendExchangeTypesExchangerDescriptionForUserMessage(character.Client, BidsHouse[mapId], (int)item.Type);           
                if(BidsHouseItems.Values.Any(x => x.Any(y => y.Template.Id == item.Template.Id)))
                    InventoryHandler.SendExchangeTypesItemsExchangerDescriptionForUserMessage(character.Client, item);
            }
        }
        
        public void Save()
        {
            DatabaseManager.Connection.Execute("DELETE FROM bids_house_items");
            foreach (var bidhouse in BidsHouseItems)
            {
                foreach (var item in bidhouse.Value)
                {
                    BidHouseItemRecord record = new BidHouseItemRecord
                    {
                        OwnerId = bidhouse.Key,
                        Item = item.Template.Id,
                        Price = item.Price,
                        Stack = item.Stack,
                        Effects = EffectManager.Instance.SetEffects(new BigEndianWriter(), item.Effects),
                        Selled = item.Selled
                    };
                    DatabaseManager.Connection.Insert(record);
                }
            }
        }
    }
}
