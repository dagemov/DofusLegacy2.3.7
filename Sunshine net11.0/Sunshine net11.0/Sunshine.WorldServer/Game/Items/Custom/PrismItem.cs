using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;

namespace Sunshine.WorldServer.Game.Items.Custom
{
    public sealed class PrismItem : BasePlayerItem
    {
        public PrismItem(int id) : base(id)
        {
        }

        public PrismItem(ItemRecord item) : base(item)
        {
        }

        public bool Use(Character owner, out string reason)
        {
            return PrismManager.Instance.TryPlacePrism(owner, owner.Cell != null ? owner.Cell.Id : (short)0, out reason);
        }
    }
}
