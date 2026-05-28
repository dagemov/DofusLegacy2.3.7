using Sunshine.MySql.Database.World.Maps.Merchants;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Maps.Pathfinding;

namespace Sunshine.WorldServer.Game.Actors.Merchants
{
    public class MerchantActor : RolePlayActor
    {
        public const short MerchantBagSkin = 237;

        public MerchantActor(WorldMapMerchantRecord record, ActorLook look)
        {
            Record = record;
            Look = look;

            if (Look != null)
            {
                Look.AddSubLook(new SubActorLook(0, SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_MERCHANT_BAG,
                    new ActorLook { BonesID = MerchantBagSkin }));
            }
        }

        public WorldMapMerchantRecord Record { get; }
        public ActorLook Look { get; }

        public override int Id => Record.CharacterId;

        public EntityDispositionInformations GetDisposition()
            => new EntityDispositionInformations(Record.CellId, (sbyte)Record.Direction);

        public override GameRolePlayActorInformations GetGameRolePlayActorInformations()
            => new GameRolePlayMerchantInformations(Id, Look.GetEntityLook(), GetDisposition(), Record.Name ?? string.Empty, 0);

        public override void StartMove(Path path)
        {
        }
    }
}
