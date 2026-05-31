using System;

namespace Sunshine.MySql.Database.World.Teleports
{
    public class CustomTeleportDestinationRecord
    {
        public int Id { get; set; }

        public int TeleportMapId { get; set; }

        public int TeleportCellId { get; set; }

        public string DestinationName { get; set; }

        public string DestinationDescription { get; set; }

        public int KamasCost { get; set; }

        public int RequiredItemId { get; set; }
    }
}
