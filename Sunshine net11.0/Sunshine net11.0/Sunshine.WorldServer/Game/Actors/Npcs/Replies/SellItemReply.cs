using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(9)]
    public class BuyItemReply : Reply
    {
        public BuyItemReply()
        {
        }

        public override bool Execute()
        {
            var itemSplit = (Parameters[0] as string).Split(',');
            int item = int.Parse(itemSplit[0]);
            int quantity = int.Parse(itemSplit[1]);
            int price = int.Parse(itemSplit[2]);

            if (Client.Character.Inventory.Kamas < price)
                return false;

            Client.Character.Inventory.SetKamas(-price);

            BasePlayerItem itemAdd = ItemManager.Instance.CreatePlayerItem(item, quantity);

            if (itemAdd == null)
                return false;

            Client.Character.Inventory.AddItem(itemAdd);
            return true;
        }
    }
}
