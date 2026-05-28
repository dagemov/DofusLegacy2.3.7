using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Shortcuts
{
    [Table("characters_shortcuts_spells")]
    public class SpellShortcut : Shortcut
    {
        public int OwnerId { get; set; }
        public short Spell { get; set; }
        public int Slot { get; set; }

        public override Protocol.Types.Shortcut GetNetworkShortcut()
        {
            return new Protocol.Types.ShortcutSpell(this.Slot, (short)this.Spell);
        }
    }
}
