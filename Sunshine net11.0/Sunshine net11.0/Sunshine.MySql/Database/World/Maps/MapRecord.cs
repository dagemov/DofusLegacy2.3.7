using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Maps
{
    [Table("worlds_maps")]
    public class MapRecord
    {
        public int Id { get; set; }
        public int RelativeId { get; set; }
        public int Version { get; set; }
        public int MapType { get; set; }
        public int SubAreaId { get; set; }
        public int TopNeighbourId { get; set; }
        public int BottomNeighbourId { get; set; }
        public int LeftNeighbourId { get; set; }
        public int RightNeighbourId { get; set; }
        public int ShadowBonusOnEntities { get; set; }
        public int UseLowpassFilter { get; set; }
        public int UseReverb { get; set; }
        public int PresetId { get; set; }
        public string BlueCells { get; set; }
        public string RedCells { get; set; }
        public string Elements { get; set; }
        public bool IsInstance { get; set; }
        public bool IsBug { get; set; }
        public string ParametersCSV { get; set; }
    }
}
