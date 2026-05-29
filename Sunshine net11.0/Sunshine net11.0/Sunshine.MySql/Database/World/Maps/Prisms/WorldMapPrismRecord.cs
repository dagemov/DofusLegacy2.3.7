using Dapper.Contrib.Extensions;
using System;

namespace Sunshine.MySql.Database.World.Maps.Prisms
{
    [Table("world_maps_prism")]
    public class WorldMapPrismRecord
    {
        [Key]
        public int Id { get; set; }

        public int SubAreaId { get; set; }

        public int MapId { get; set; }

        public short CellId { get; set; }

        public short WorldX { get; set; }

        public short WorldY { get; set; }

        public sbyte AlignmentSide { get; set; }

        public DateTime PlacementDate { get; set; }

        public bool IsInFight { get; set; }

        public bool IsFightable { get; set; }

        public DateTime? Defeated { get; set; }

        public DateTime? LastFight { get; set; }

        [Write(false)]
        public bool WasDefeated
        {
            get { return Defeated.HasValue; }
        }
    }
}
