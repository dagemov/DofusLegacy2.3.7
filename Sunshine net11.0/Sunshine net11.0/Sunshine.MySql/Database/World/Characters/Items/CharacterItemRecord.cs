using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Characters.Items
{
    [Table("characters_items")]
    public class CharacterItemRecord
    {
        public int OwnerId { get; set; }
        public int ItemUid { get; set; }
        public int Item { get; set; }
        public byte Position { get; set; }
        public int Stack { get; set; }
        public string Effects { get; set; }
    }
}
