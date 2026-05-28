using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Guilds
{
    [Table("taxcollectors_items")]
    public class TaxCollectorItemsRecord
    {
        public int Id { get; set; }
        public int Item { get; set; }
        public int Stack { get; set; }
        public string Effects { get; set; }
    }
}
