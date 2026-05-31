using Dapper.Contrib.Extensions;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Characters
{
    [Table("characters")]
    public class CharacterRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public short Level { get; set; }
        public int Breed { get; set; }
        public bool Sex { get; set; }
        public string EntityLook { get; set; }
        public string CustomLook { get; set; }
        public int MapId { get; set; }
        public int? HouseId { get; set; }
        public int? PaddockId { get; set; }
        public short CellId { get; set; }
        public int Direction { get; set; }
        public long Experience { get; set; }
        public int Kamas { get; set; }
        public int StatsPoints { get; set; }
        public int SpellsPoints { get; set; }
        public string Zaaps { get; set; }
        public bool CanUseRename { get; set; }
        public bool CanUseRecolor { get; set; }
        public bool CanUseRelook { get; set; }
        public int? EquippedMount { get; set; }
        public bool IsRiding { get; set; }
    }
}
