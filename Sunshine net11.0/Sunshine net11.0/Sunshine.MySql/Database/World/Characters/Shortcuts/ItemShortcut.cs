using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Shortcuts
{
    [Table("characters_shortcuts_items")]
    public class ItemShortcut : Shortcut
    {
        public int OwnerId { get; set; }
        public int ItemUid { get; set; }
        public int ItemGid { get; set; }
        public int Slot { get; set; }

        public override Protocol.Types.Shortcut GetNetworkShortcut()
        {
            return new Protocol.Types.ShortcutObjectItem(this.Slot, this.ItemUid, this.ItemGid);
        }
    }
}
