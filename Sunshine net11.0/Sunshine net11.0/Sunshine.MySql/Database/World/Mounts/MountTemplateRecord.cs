using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Mounts
{
    [Table("mounts_templates")]
    public class MountTemplateRecord
    {
        [ExplicitKey]
        public int Id { get; set; }
        public uint NameId { get; set; }
        public string LookAsString { get; set; }
        public int ScrollId { get; set; }
        public int PodsBase { get; set; }
        public int PodsPerLevel { get; set; }
        public int EnergyBase { get; set; }
        public int EnergyPerLevel { get; set; }
        public int MaturityBase { get; set; }
        public int FecondationTime { get; set; }
        public sbyte LearnCoefficient { get; set; }
    }
}
