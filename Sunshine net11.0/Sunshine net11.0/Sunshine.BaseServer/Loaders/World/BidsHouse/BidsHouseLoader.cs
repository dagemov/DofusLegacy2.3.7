using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.BidsHouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Loaders.World.BidsHouse
{
    public static class BidsHouseLoader
    {
        public static void Initialize()
        {
            var bidsHouse = BidHouseManager.Instance.GetAllBidsHouse();
            var bidsHouseItems = BidHouseManager.Instance.GetAllBidsHouseItems();

            BidHouseManager.Instance.BidsHouseItems = BidHouseManager.Instance.GetAllItems(bidsHouseItems);

            foreach (var items in BidHouseManager.Instance.BidsHouseItems)
            {
                foreach (var item in items.Value)
                    BidHouseManager.Instance.Sort(item, item.Selled);
            }

            foreach (var house in bidsHouse)
                BidHouseManager.Instance.BidsHouse.Add(house.MapId, new BidHouse(house));               
        }
    }
}
