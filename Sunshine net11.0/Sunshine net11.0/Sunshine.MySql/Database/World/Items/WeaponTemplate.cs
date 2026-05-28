using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Items
{
    [Table("items_weapons")]
    public class WeaponTemplate : ItemRecord
    {
        public byte ApCost { get; set; }
        public byte MinRange { get; set; }
        public byte Range { get; set; }
        public bool CastInLine { get; set; }
        public bool CastInDiagonal { get; set; }
        public bool CastTestLos { get; set; }
        public short CriticalHitProbability { get; set; }
        public short CriticalHitBonus { get; set; }
        public short CriticalFailureProbability { get; set; }
        public override int Id { get; }
        public override int Weight { get; }
        public string Name { get; set; }
        public override byte TypeId { get; }
        public int DescriptionId { get; set; }
        public int IconId { get; set; }
        public override short Level { get; }
        public bool Cursed { get; set; }
        public int UseAnimationId { get; set; }
        public override bool Usable { get; }
        public override bool Targetable { get; }
        public override double Price { get; }
        public override bool TwoHanded { get; }
        public bool Etheral { get; set; }
        public override short ItemSetId { get; }
        public override string Criteria { get; }
        public bool HideEffects { get; set; }
        public override short AppearanceId { get; }
        public override string RecipeIdsCSV { get; }
        public string FavoriteSubAreasCSV { get; set; }
        public bool BonusIsSecret { get; set; }
        public int FavoriteSubAreasBonus { get; set; }
        public string Effects { get; set; }
    }
}
