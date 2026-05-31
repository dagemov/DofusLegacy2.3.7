using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Maps.Zaapis
{
    [Table("worlds_zaapis")]
    public class ZaapiRecord
    {
        public int Id { get; set; }
        public short IsBonta { get; set; }
    }
}
