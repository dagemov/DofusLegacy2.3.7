using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Monsters
{
    [Table("monsters_grades")]
    public class MonsterGrade
    {
        public int Id { get; set; }
        public int MonsterId { get; set; }
        public byte GradeId { get; set; }
        public int GradeExp { get; set; }
        public short Level { get; set; }
        public short PaDodge { get; set; }
        public short PmDodge { get; set; }
        public short EarthResistance { get; set; }
        public short AirResistance { get; set; }
        public short FireResistance { get; set; }
        public short WaterResistance { get; set; }
        public short NeutralResistance { get; set; }
        public int LifePoints { get; set; }
        public short ActionPoints { get; set; }
        public short MovementPoints { get; set; }
        public short TackleEvade { get; set; }
        public short TackleBlock { get; set; }
        public short Strength { get; set; }
        public short Chance { get; set; }
        public short Vitality { get; set; }
        public short Wisdom { get; set; }
        public short Intelligence { get; set; }
        public short Agility { get; set; }
        public int Initiative { get { return Strength + Chance + Intelligence + Agility * (LifePoints / (50 + (Level * 5) + LifePoints)); } }
    }
}
