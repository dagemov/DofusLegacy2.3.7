using Sunshine.MySql.Database.World.Maps.Prisms;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps;
using System.Collections.Generic;
using System.Drawing;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public sealed class PrismFighter : AIFighter
    {
        private readonly WorldMapPrismRecord m_record;

        public PrismFighter(WorldMapPrismRecord record, Fight fight)
            : base(fight.GetNextContextualId(), BuildLook(record), 200, BuildStats(), fight)
        {
            m_record = record;
            Position = new ObjectPosition(fight.Map, record.CellId, DirectionsEnum.DIRECTION_SOUTH);
        }

        public WorldMapPrismRecord Record => m_record;

        public string DisplayName => (AlignmentSideEnum)m_record.AlignmentSide == AlignmentSideEnum.ALIGNMENT_EVIL
            ? "Prisme Brâkmarien"
            : "Prisme Bontarien";

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            return new GameFightFighterNamedInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                GetGameFightMinimalStats(client),
                DisplayName);
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            return new GameFightFighterNamedInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                GetGameFightMinimalStatsPreparation(client),
                DisplayName);
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberInformations(Id);
        }

        private static StatsFields BuildStats()
        {
            var stats = new StatsFields(new object());
            stats[StatsEnum.AP].Base = 6;
            stats[StatsEnum.MP].Base = 5;
            stats[StatsEnum.Health].Base = 8000;
            stats[StatsEnum.Initiative].Base = 0;
            stats[StatsEnum.EarthResistPercent].Base = 25;
            stats[StatsEnum.WaterResistPercent].Base = 25;
            stats[StatsEnum.FireResistPercent].Base = 25;
            stats[StatsEnum.NeutralResistPercent].Base = 25;
            stats[StatsEnum.AirResistPercent].Base = 25;
            stats[StatsEnum.DodgeAPProbability].Base = 30;
            stats[StatsEnum.DodgeMPProbability].Base = 30;
            stats[StatsEnum.APAttack].Base = 20;
            stats[StatsEnum.MPAttack].Base = 20;
            stats[StatsEnum.DamageBonus].Base = 40;
            return stats;
        }

        private static ActorLook BuildLook(WorldMapPrismRecord record)
        {
            short bonesId;
            switch ((AlignmentSideEnum)record.AlignmentSide)
            {
                case AlignmentSideEnum.ALIGNMENT_ANGEL:
                    bonesId = 828;
                    break;

                case AlignmentSideEnum.ALIGNMENT_EVIL:
                    bonesId = 827;
                    break;

                default:
                    bonesId = 828;
                    break;
            }

            return new ActorLook(bonesId, new short[0], new Dictionary<int, Color>(), new short[0], new SubActorLook[0]);
        }
    }
}
