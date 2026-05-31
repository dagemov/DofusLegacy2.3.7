using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.BidsHouse
{
    [Table("bids_house")]
    public class BidHouseRecord
    {
        public int MapId { get; set; }
        public string Types { get; set; }
        public int Level { get; set; }
    }
}
