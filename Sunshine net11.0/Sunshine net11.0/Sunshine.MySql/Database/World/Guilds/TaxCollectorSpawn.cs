using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Guilds
{
    [Table("worlds_taxcollectors")]
    public class TaxCollectorSpawn
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int Guild { get; set; }
        public int Map { get; set; }
        public short Cell { get; set; }
        public sbyte Direction { get; set; }
        public short FirstName { get; set; }
        public short LastName { get; set; }
        public string CallerName { get; set; }
        public DateTime Date { get; set; }
        public double GatheredExperience { get; set; }
        public int GatheredKamas { get; set; }
        public int AttacksCount { get; set; }
    }
}
