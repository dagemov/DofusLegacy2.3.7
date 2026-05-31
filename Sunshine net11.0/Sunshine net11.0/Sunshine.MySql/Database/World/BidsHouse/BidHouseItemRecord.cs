using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.BidsHouse
{
    [Table("bids_house_items")]
    public class BidHouseItemRecord
    {
        public int OwnerId { get; set; }
        public int Item { get; set; }
        public int Price { get; set; }
        public int Stack { get; set; }
        public string Effects { get; set; }
        public bool Selled { get; set; }
    }
}
