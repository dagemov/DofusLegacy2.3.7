using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(7)]
    public class AddItemReply : Reply
    {
        public AddItemReply()
        {
        }

        public override bool Execute()
        {
            var itemSplit = (Parameters[0] as string).Split(';');
            foreach (var stack in itemSplit)
            {
                var items = stack.Split(':');
                var itemStack = int.Parse(items[1]);

                BasePlayerItem item = ItemManager.Instance.CreatePlayerItem(int.Parse(items[0]), itemStack);

                if (item == null)
                    return false;

                Client.Character.Inventory.AddItem(item);
            }
            return true;
        }
    }
}
