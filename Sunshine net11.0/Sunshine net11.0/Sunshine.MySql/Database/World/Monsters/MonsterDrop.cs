using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Monsters
{
    [Table("monsters_drops")]
    public class MonsterDrop
    {
        public int MonsterId { get; set; }
        public int ItemId { get; set; }
        public int DropLimit { get; set; }
        public float DropRateForGrade1 { get; set; }
        public float DropRateForGrade2 { get; set; }
        public float DropRateForGrade3 { get; set; }
        public float DropRateForGrade4 { get; set; }
        public float DropRateForGrade5 { get; set; }
        public int ProspectingLock { get; set; }
    }
}
