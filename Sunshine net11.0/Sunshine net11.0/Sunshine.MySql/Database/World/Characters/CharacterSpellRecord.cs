using Dapper.Contrib.Extensions;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Characters
{
    [Table("characters_spells")]
    public class CharacterSpellRecord
    {
        public int OwnerId { get; set; }
        public short Spell { get; set; }
        public sbyte Level { get; set; }
        
        public SpellItem GetSpellItem()
        {
            return new SpellItem(63, Spell, Level);
        }
    }
}
