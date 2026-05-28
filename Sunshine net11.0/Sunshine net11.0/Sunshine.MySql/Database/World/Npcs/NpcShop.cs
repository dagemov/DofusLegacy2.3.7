using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Npcs
{
    [Table("npcs_items")]
    public class NpcShop
    {
        public int NpcId { get; set; }
        public int Item { get; set; }
        public int Price { get; set; }
        public int Token { get; set; }

        public int GetPrice(int defaultPrice)
        {
            return Price > 0 ? Price : defaultPrice;
        }
    }
}
