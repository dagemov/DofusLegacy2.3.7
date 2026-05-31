using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(3)]
    public class HasItemReply : Reply
    {
        public HasItemReply()
        {
        }

        public override bool Execute()
        {
            var itemSplit = (Parameters[0] as string).Split(';');
            foreach (var stack in itemSplit)
            {
                var items = stack.Split(':');
                var itemStack = int.Parse(items[1]);
                var item = Client.Character.Inventory.GetItem(int.Parse(items[0]));
                if (item == null || (item != null && item.Stack < itemStack))
                    return false;               
                Client.Character.Inventory.RemoveItem(item, itemStack);                 
            }
            return true;
        }
    }
}
