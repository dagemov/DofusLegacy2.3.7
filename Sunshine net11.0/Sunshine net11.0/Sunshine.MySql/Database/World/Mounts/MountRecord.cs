using Dapper.Contrib.Extensions;
using System;

namespace Sunshine.MySql.Database.World.Mounts
{
    [Table("mounts")]
    public class MountRecord
    {
        public string Name { get; set; }
        public sbyte Sex { get; set; }
        public int TemplateId { get; set; }
        public long Experience { get; set; }
        public sbyte GivenExperience { get; set; }
        public int Stamina { get; set; }
        public int Maturity { get; set; }
        public int Energy { get; set; }
        public int Serenity { get; set; }
        public int Love { get; set; }
        public int ReproductionCount { get; set; }
        public string BehaviorsCSV { get; set; }
        public int? OwnerId { get; set; }
        public string OwnerName { get; set; }
        public int? PaddockId { get; set; }
        public sbyte IsInStable { get; set; }
        public DateTime? StoredSince { get; set; }
        [ExplicitKey]
        public int Id { get; set; }
    }
}
