using Dapper.Contrib.Extensions;
using Sunshine.Protocol.Enums;
using System.Collections.Generic;

namespace Sunshine.MySql.Database.World.Items
{
    [Table("pets")]
    public class PetRecord
    {
        [ExplicitKey]
        public short PetId { get; set; }
        public byte MaxLifePoints { get; set; }
        public short GhostId { get; set; }
        public short? CertificateId { get; set; }
        public byte? MinMealHours { get; set; }
        public byte? MaxMealHours { get; set; }
        public short? BoostItemId { get; set; }

        [Write(false)]
        public Dictionary<EffectsEnum, PetFoodRecord> FoodInformations { get; set; } = new Dictionary<EffectsEnum, PetFoodRecord>();
    }
}
