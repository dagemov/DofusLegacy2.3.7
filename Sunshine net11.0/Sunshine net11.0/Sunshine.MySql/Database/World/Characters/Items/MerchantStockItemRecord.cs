using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Characters.Items
{
    [Table("characters_items_merchant")]
    public class MerchantStockItemRecord
    {
        [ExplicitKey]
        public int ItemUid { get; set; }
        public int OwnerId { get; set; }
        public int TemplateId { get; set; }
        public int Stack { get; set; }
        public int Position { get; set; }
        public int Price { get; set; }
        public string Effects { get; set; }
    }
}
