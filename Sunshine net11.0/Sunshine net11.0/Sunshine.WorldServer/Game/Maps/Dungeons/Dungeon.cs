using Sunshine.MySql.Database.World.Dungeons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Dungeons
{
    public class Dungeon
    {
        public Dungeon(DungeonSpawn record)
        {
            var parameters = record.Parameters == null || record.Parameters == "" ? new List<int>() :
                     record.Parameters.Split(',').Select(x => int.Parse(x)).ToList();
            Monsters = record.MonstersCSV != "" ? record.MonstersCSV.Split(',').Select(x => int.Parse(x)) : new List<int>();
            NextMap = parameters[0];
            NextCell = (short)parameters[1];
            NextDirection = parameters[2];
        }

        public IEnumerable<int> Monsters { get; set; }

        public int NextMap { get; set; }

        public short NextCell { get; set; }

        public int NextDirection { get; set; }
    }
}
