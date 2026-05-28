using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Items
{
    [Table("items_sets")]
    public class ItemSetTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool BonusIsSecret { get; set; }
        public string ItemsCSV { get; set; }
        public string Effects { get; set; }
    }
}
