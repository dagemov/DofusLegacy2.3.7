using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Items
{
    [Table("runes_effect")]
    public class RuneEffectRecord
    {
        public int Id { get; set; }
        public double PEffect { get; set; }
    }
}
