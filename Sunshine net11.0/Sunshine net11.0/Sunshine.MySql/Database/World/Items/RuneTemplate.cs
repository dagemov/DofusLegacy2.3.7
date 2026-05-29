using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Items
{
    [Table("runes")]
    public class RuneTemplate
    {
        public int Id { get; set; }
        public double Pwr { get; set; }
        public double PEffect { get; set; }
        public int Over { get; set; }
    } 
}
