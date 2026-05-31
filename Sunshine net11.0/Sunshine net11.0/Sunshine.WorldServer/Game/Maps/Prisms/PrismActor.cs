using Sunshine.MySql.Database.World.Maps.Prisms;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using System.Collections.Generic;
using System.Drawing;

namespace Sunshine.WorldServer.Game.Maps.Prisms
{
    public class PrismActor : RolePlayActor
    {
        public PrismActor(int id, WorldMapPrismRecord record)
        {
            Id = id;
            Record = record;
            Look = BuildLook(record.AlignmentSide);
        }

        public override int Id { get; }

        public WorldMapPrismRecord Record { get; private set; }

        public ActorLook Look { get; private set; }

        public EntityDispositionInformations GetDisposition()
        {
            return new EntityDispositionInformations(Record.CellId, (sbyte)DirectionsEnum.DIRECTION_SOUTH);
        }

        public override GameRolePlayActorInformations GetGameRolePlayActorInformations()
        {
            return new GameRolePlayPrismInformations(
                Id,
                Look.GetEntityLook(),
                GetDisposition(),
                new ActorAlignmentInformations(Record.AlignmentSide, 1, 2, 0, 3));
        }

        public override void StartMove(Path path)
        {
        }

        private static ActorLook BuildLook(sbyte alignmentSide)
        {
            short bonesId;
            switch ((AlignmentSideEnum)alignmentSide)
            {
                case AlignmentSideEnum.ALIGNMENT_ANGEL:
                    bonesId = 828; // Bonta = ailes ange
                    break;

                case AlignmentSideEnum.ALIGNMENT_EVIL:
                    bonesId = 827; // Brâkmar = ailes démon
                    break;

                default:
                    bonesId = 828;
                    break;
            }

            return new ActorLook(bonesId, new short[0], new Dictionary<int, Color>(), new short[0], new SubActorLook[0]);
        }
    }
}
